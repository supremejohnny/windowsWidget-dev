import AppKit

// Separate, disposable target process: exercises the same CG/AX route as real apps.
final class FixtureDelegate: NSObject, NSApplicationDelegate {
    var window: NSWindow!, timer: Timer?
    var started = ProcessInfo.processInfo.systemUptime
    func applicationDidFinishLaunching(_ notification: Notification) {
        window = NSWindow(contentRect: NSRect(x: 300, y: 300, width: 420, height: 220), styleMask: [.titled], backing: .buffered, defer: false)
        window.title = "Window Pet · movement test fixture"; window.isReleasedWhenClosed = false; window.animationBehavior = .none
        window.orderFront(nil)
        try! String(window.windowNumber).write(toFile: CommandLine.arguments[1], atomically: true, encoding: .utf8)
        started = ProcessInfo.processInfo.systemUptime
        timer = Timer(timeInterval: 1.0/120, repeats: true) { [weak self] _ in
            guard let self = self else { return }
            let t = max(0, ProcessInfo.processInfo.systemUptime-self.started-0.5)
            if CommandLine.arguments.contains("--micro") {
                let x = t < 2 ? (2*sin(4*Double.pi*t)).rounded() : 0
                let y = t < 2 ? sin(6*Double.pi*t).rounded() : 0
                self.window.setFrameOrigin(NSPoint(x: 300+x, y: 300+y))
                return
            }
            if CommandLine.arguments.contains("--slow") {
                let phase = min(t, 3)
                self.window.setFrameOrigin(NSPoint(x: 300+25*(1-cos(Double.pi*phase/3)), y: 300))
                return
            }
            self.window.setFrameOrigin(NSPoint(x: 300+80*sin(t*3), y: 300+35*sin(t*2)))
            if CommandLine.arguments.contains("--raise") { self.window.orderFront(nil) }
        }
        RunLoop.main.add(timer!, forMode: .common)
        DispatchQueue.main.asyncAfter(deadline: .now()+8) { NSApp.terminate(nil) }
    }
}
let fixture = NSApplication.shared
let fixtureDelegate = FixtureDelegate()
fixture.setActivationPolicy(.accessory); fixture.delegate = fixtureDelegate; fixture.run()
