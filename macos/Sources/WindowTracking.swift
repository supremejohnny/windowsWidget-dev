import AppKit
import ApplicationServices
import CoreVideo

struct TargetWindow {
    let id: CGWindowID, pid: pid_t, label: String, frame: CGRect, title: String, onScreen: Bool
    init(id: CGWindowID, pid: pid_t, label: String, frame: CGRect, title: String = "", onScreen: Bool = true) {
        self.id = id; self.pid = pid; self.label = label; self.frame = frame; self.title = title; self.onScreen = onScreen
    }
}
func windowCandidates(_ list: [[String: Any]]) -> [TargetWindow] {
    list.compactMap { row in
        guard let id = row[kCGWindowNumber as String] as? UInt32,
              let pid = row[kCGWindowOwnerPID as String] as? Int32,
              let layer = row[kCGWindowLayer as String] as? Int, layer == 0,
              (row[kCGWindowAlpha as String] as? Double ?? 1) > 0.01,
              let bounds = row[kCGWindowBounds as String] as? NSDictionary,
              let frame = CGRect(dictionaryRepresentation: bounds), frame.width > 80, frame.height > 60 else { return nil }
        let owner = row[kCGWindowOwnerName as String] as? String ?? "App"
        let title = row[kCGWindowName as String] as? String ?? ""
        let onScreen = row[kCGWindowIsOnscreen as String] as? Bool ?? false
        let label = "\(owner) — \(title.isEmpty ? "Window #\(id)" : title)"+(onScreen ? "" : "（后台 / 其他桌面）")
        return TargetWindow(id: id, pid: pid, label: label, frame: frame, title: title, onScreen: onScreen)
    }
}
func availableWindows() -> [TargetWindow] {
    let rows = CGWindowListCopyWindowInfo([.optionAll, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    return windowCandidates(rows).compactMap { target in
        guard let app = NSRunningApplication(processIdentifier: target.pid),
              !app.isTerminated, app.activationPolicy != .prohibited else { return nil }
        // WindowServer's owner name can be the Electron helper's name rather
        // than the application name shown in the Dock (for example Codex).
        let owner = app.bundleIdentifier == "com.openai.codex" ? "Codex" :
            (app.localizedName ?? target.label.components(separatedBy: " — ")[0])
        let label = "\(owner) — \(target.title.isEmpty ? "Window #\(target.id)" : target.title)"+(target.onScreen ? "" : "（后台 / 其他桌面）")
        return TargetWindow(id: target.id, pid: target.pid, label: label, frame: target.frame, title: target.title, onScreen: target.onScreen)
    }
}
func visibleWindows() -> [TargetWindow] { availableWindows().filter { $0.onScreen } }

// Present one application, not every Electron/helper WindowServer surface.
// Preserve server ordering within each group for front-window resolution.
func applicationChoices(_ windows: [TargetWindow]) -> [TargetWindow] {
    var seen = Set<pid_t>()
    return windows.filter { seen.insert($0.pid).inserted }.sorted {
        applicationName($0).localizedStandardCompare(applicationName($1)) == .orderedAscending
    }
}
func applicationName(_ target: TargetWindow) -> String {
    target.label.components(separatedBy: " — ")[0]
}
func frontTarget(_ pid: pid_t) -> TargetWindow? {
    guard NSWorkspace.shared.frontmostApplication?.processIdentifier == pid else { return nil }
    let rows = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    let windows = windowCandidates(rows).filter { $0.pid == pid }
    // If permission already exists, focus disambiguates same-app main windows.
    // This never prompts and falls back to the frontmost visible server surface.
    if AccessibilityTracker.authorized {
        let app = AXUIElementCreateApplication(pid)
        AXUIElementSetMessagingTimeout(app, 0.03)
        var value: CFTypeRef?
        if AXUIElementCopyAttributeValue(app, kAXFocusedWindowAttribute as CFString, &value) == .success,
           let value = value, CFGetTypeID(value) == AXUIElementGetTypeID(),
           let rect = AccessibilityTracker.geometry(value as! AXUIElement) {
            let matches = windows.filter { abs($0.frame.minX-rect.minX) < 2 && abs($0.frame.minY-rect.minY) < 2 &&
                abs($0.frame.width-rect.width) < 2 && abs($0.frame.height-rect.height) < 2 }
            if matches.count == 1 { return matches[0] }
        }
    }
    return windows.first
}

// Read a single window rather than enumerate every application on every frame.
func targetWindow(_ id: CGWindowID) -> TargetWindow? {
    let rows = CGWindowListCopyWindowInfo(.optionIncludingWindow, id) as? [[String: Any]] ?? []
    guard let row = rows.first(where: { ($0[kCGWindowNumber as String] as? UInt32) == id }),
          let pid = row[kCGWindowOwnerPID as String] as? Int32,
          let bounds = row[kCGWindowBounds as String] as? NSDictionary,
          let frame = CGRect(dictionaryRepresentation: bounds) else { return nil }
    return TargetWindow(id: id, pid: pid, label: "", frame: frame, title: row[kCGWindowName as String] as? String ?? "", onScreen: row[kCGWindowIsOnscreen as String] as? Bool ?? false)
}

func windowOnScreen(_ id: CGWindowID) -> Bool {
    let rows = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    return rows.contains { ($0[kCGWindowNumber as String] as? UInt32) == id }
}

final class CGFrameSampler {
    let id: CGWindowID
    private let ownerPID: pid_t?
    private var accessibilityWindow: AXUIElement?
    private let sourceLock = NSLock()
    private var visibilityTime = 0.0, visible = false
    init(_ id: CGWindowID, accessibilityWindow: AXUIElement? = nil) {
        self.id = id; ownerPID = targetWindow(id)?.pid; self.accessibilityWindow = accessibilityWindow
    }
    func setAccessibilityWindow(_ window: AXUIElement) {
        sourceLock.lock(); accessibilityWindow = window; sourceLock.unlock()
    }
    // Called only on DisplayDriver's serial sampling queue.
    func sample() -> CGRect? {
        let now = ProcessInfo.processInfo.systemUptime
        if now-visibilityTime >= 0.066 { visible = windowOnScreen(id); visibilityTime = now }
        guard visible else { return nil }
        // Use the same geometry source as AX events. CG snapshots can lag behind AX,
        // so mixing them during a drag injects artificial forward/backward impulses.
        sourceLock.lock(); let window = accessibilityWindow; sourceLock.unlock()
        if let window = window { return AccessibilityTracker.geometry(window) }
        guard let target = targetWindow(id), target.pid == ownerPID else { return nil }
        return target.frame
    }
}

final class DisplayDriver {
    private var link: CVDisplayLink?
    private let lock = NSLock()
    private var queued = false, stopped = false
    private(set) var framePeriod = 1.0/60
    private let sampling = DispatchQueue(label: "WindowPet.windowGeometry", qos: .userInteractive)
    var read: (() -> CGRect?)?
    var receive: ((CGRect?, Double) -> Void)?
    var frame: (() -> Void)?
    init() {
        var created: CVDisplayLink?
        guard CVDisplayLinkCreateWithActiveCGDisplays(&created) == kCVReturnSuccess, let link = created else { return }
        self.link = link
        let period = CVDisplayLinkGetNominalOutputVideoRefreshPeriod(link)
        if period.timeValue > 0 && period.timeScale > 0 { framePeriod = Double(period.timeValue)/Double(period.timeScale) }
        CVDisplayLinkSetOutputCallback(link, { _, _, _, _, _, context in
            guard let context = context else { return kCVReturnSuccess }
            Unmanaged<DisplayDriver>.fromOpaque(context).takeUnretainedValue().enqueue()
            return kCVReturnSuccess
        }, Unmanaged.passUnretained(self).toOpaque())
    }
    private func enqueue() {
        lock.lock()
        if stopped || queued { lock.unlock(); return }
        queued = true; lock.unlock()
        sampling.async { [weak self] in
            guard let self = self else { return }
            let stamp = ProcessInfo.processInfo.systemUptime
            let geometry = self.read?()
            DispatchQueue.main.async { [weak self] in
                guard let self = self else { return }
                self.lock.lock(); self.queued = false; let stopped = self.stopped; self.lock.unlock()
                if !stopped {
                    if self.read != nil { self.receive?(geometry, stamp) }
                    self.frame?()
                }
            }
        }
    }
    func start() -> Bool { guard let link = link else { return false }; return CVDisplayLinkStart(link) == kCVReturnSuccess }
    func stop() {
        lock.lock(); stopped = true; lock.unlock()
        if let link = link { CVDisplayLinkStop(link) }
        frame = nil; receive = nil
    }
    deinit { stop() }
}

// AX has no public window-ID accessor. Bind only a unique, geometry-matched AX window
// in the known PID; ambiguous matches stay on the display-synchronized fallback.
final class AccessibilityTracker {
    static var authorized: Bool { AXIsProcessTrusted() }
    let window: AXUIElement
    private var observer: AXObserver?
    private var registered: [(AXUIElement, String)] = []
    private let application: AXUIElement
    enum Event { case geometry, focus, visibility, destroyed }
    var changed: ((Event) -> Void)?
    static func geometry(_ element: AXUIElement) -> CGRect? {
        var position: CFTypeRef?, size: CFTypeRef?
        guard AXUIElementCopyAttributeValue(element, kAXPositionAttribute as CFString, &position) == .success,
              AXUIElementCopyAttributeValue(element, kAXSizeAttribute as CFString, &size) == .success,
              let p = position, let s = size, CFGetTypeID(p) == AXValueGetTypeID(), CFGetTypeID(s) == AXValueGetTypeID() else { return nil }
        var point = CGPoint.zero, dimensions = CGSize.zero
        guard AXValueGetValue(p as! AXValue, .cgPoint, &point), AXValueGetValue(s as! AXValue, .cgSize, &dimensions) else { return nil }
        return CGRect(origin: point, size: dimensions)
    }
    init?(target: TargetWindow) {
        guard AXIsProcessTrusted() else { return nil } // Never prompts or changes user permissions.
        let app = AXUIElementCreateApplication(target.pid)
        application = app
        AXUIElementSetMessagingTimeout(app, 0.03)
        var value: CFTypeRef?
        guard AXUIElementCopyAttributeValue(app, kAXWindowsAttribute as CFString, &value) == .success,
              let windows = value as? [AXUIElement] else { return nil }
        let matches = windows.filter { element in
            if !target.title.isEmpty {
                var title: CFTypeRef?
                if AXUIElementCopyAttributeValue(element, kAXTitleAttribute as CFString, &title) == .success,
                   let title = title as? String, title != target.title { return false }
            }
            guard let r = Self.geometry(element) else { return false }
            return abs(r.minX-target.frame.minX) < 2 && abs(r.minY-target.frame.minY) < 2 &&
                abs(r.width-target.frame.width) < 2 && abs(r.height-target.frame.height) < 2
        }
        guard matches.count == 1 else { return nil }
        window = matches[0]; AXUIElementSetMessagingTimeout(window, 0.03)
        var created: AXObserver?
        guard AXObserverCreate(target.pid, { _, _, name, context in
            guard let context = context else { return }
            let tracker = Unmanaged<AccessibilityTracker>.fromOpaque(context).takeUnretainedValue()
            let event: Event
            switch name as String {
            case kAXUIElementDestroyedNotification: event = .destroyed
            case kAXFocusedWindowChangedNotification, kAXMainWindowChangedNotification: event = .focus
            case kAXWindowMiniaturizedNotification, kAXWindowDeminiaturizedNotification: event = .visibility
            default: event = .geometry
            }
            tracker.changed?(event)
        }, &created) == .success, let observer = created else { return nil }
        self.observer = observer
        for name in [kAXMovedNotification, kAXResizedNotification, kAXWindowMiniaturizedNotification, kAXWindowDeminiaturizedNotification, kAXUIElementDestroyedNotification] {
            if AXObserverAddNotification(observer, window, name as CFString, Unmanaged.passUnretained(self).toOpaque()) == .success { registered.append((window, name)) }
        }
        guard registered.contains(where: { $0.1 == kAXMovedNotification }) else { return nil }
        for name in [kAXFocusedWindowChangedNotification, kAXMainWindowChangedNotification] {
            if AXObserverAddNotification(observer, app, name as CFString, Unmanaged.passUnretained(self).toOpaque()) == .success { registered.append((app, name)) }
        }
        CFRunLoopAddSource(CFRunLoopGetMain(), AXObserverGetRunLoopSource(observer), .commonModes)
    }
    var geometry: CGRect? { Self.geometry(window) }
    var isFocused: Bool? {
        var value: CFTypeRef?
        guard AXUIElementCopyAttributeValue(application, kAXFocusedWindowAttribute as CFString, &value) == .success,
              let element = value, CFGetTypeID(element) == AXUIElementGetTypeID() else { return nil }
        return CFEqual(element, window)
    }
    deinit {
        if let observer = observer {
            for (element, name) in registered { AXObserverRemoveNotification(observer, element, name as CFString) }
            CFRunLoopRemoveSource(CFRunLoopGetMain(), AXObserverGetRunLoopSource(observer), .commonModes)
        }
    }
}
