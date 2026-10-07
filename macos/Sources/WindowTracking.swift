import AppKit
import ApplicationServices
import CoreVideo

// Read a single window rather than enumerate every application on every frame.
func targetWindow(_ id: CGWindowID) -> TargetWindow? {
    let rows = CGWindowListCopyWindowInfo(.optionIncludingWindow, id) as? [[String: Any]] ?? []
    guard let row = rows.first(where: { ($0[kCGWindowNumber as String] as? UInt32) == id }),
          let pid = row[kCGWindowOwnerPID as String] as? Int32,
          let bounds = row[kCGWindowBounds as String] as? NSDictionary,
          let frame = CGRect(dictionaryRepresentation: bounds) else { return nil }
    return TargetWindow(id: id, pid: pid, label: "", frame: frame)
}

func windowOnScreen(_ id: CGWindowID) -> Bool {
    let rows = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
    return rows.contains { ($0[kCGWindowNumber as String] as? UInt32) == id }
}

final class CGFrameSampler {
    let id: CGWindowID
    private var accessibilityWindow: AXUIElement?
    private let sourceLock = NSLock()
    private var visibilityTime = 0.0, visible = false
    init(_ id: CGWindowID, accessibilityWindow: AXUIElement? = nil) { self.id = id; self.accessibilityWindow = accessibilityWindow }
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
        return targetWindow(id)?.frame
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
    private var registered: [String] = []
    var changed: (() -> Void)?
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
        AXUIElementSetMessagingTimeout(app, 0.03)
        var value: CFTypeRef?
        guard AXUIElementCopyAttributeValue(app, kAXWindowsAttribute as CFString, &value) == .success,
              let windows = value as? [AXUIElement] else { return nil }
        let matches = windows.filter { element in
            guard let r = Self.geometry(element) else { return false }
            return abs(r.minX-target.frame.minX) < 2 && abs(r.minY-target.frame.minY) < 2 &&
                abs(r.width-target.frame.width) < 2 && abs(r.height-target.frame.height) < 2
        }
        guard matches.count == 1 else { return nil }
        window = matches[0]; AXUIElementSetMessagingTimeout(window, 0.03)
        var created: AXObserver?
        guard AXObserverCreate(target.pid, { _, _, _, context in
            guard let context = context else { return }
            Unmanaged<AccessibilityTracker>.fromOpaque(context).takeUnretainedValue().changed?()
        }, &created) == .success, let observer = created else { return nil }
        self.observer = observer
        for name in [kAXMovedNotification, kAXResizedNotification, kAXWindowMiniaturizedNotification, kAXWindowDeminiaturizedNotification, kAXUIElementDestroyedNotification] {
            if AXObserverAddNotification(observer, window, name as CFString, Unmanaged.passUnretained(self).toOpaque()) == .success { registered.append(name) }
        }
        guard registered.contains(kAXMovedNotification) else { return nil }
        CFRunLoopAddSource(CFRunLoopGetMain(), AXObserverGetRunLoopSource(observer), .commonModes)
    }
    var geometry: CGRect? { Self.geometry(window) }
    deinit {
        if let observer = observer {
            for name in registered { AXObserverRemoveNotification(observer, window, name as CFString) }
            CFRunLoopRemoveSource(CFRunLoopGetMain(), AXObserverGetRunLoopSource(observer), .commonModes)
        }
    }
}
