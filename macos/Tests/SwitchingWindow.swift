import AppKit

// Disposable regular application for real Stage Manager and fullscreen checks.
final class SwitchingDelegate: NSObject, NSApplicationDelegate {
    var main: NSWindow!, second: NSWindow?
    func applicationDidFinishLaunching(_ notification: Notification) {
        main = NSWindow(contentRect: NSRect(x: 300, y: 240, width: 660, height: 420), styleMask: [.titled, .closable, .miniaturizable, .resizable], backing: .buffered, defer: false)
        main.title = "Window Pet · switching probe"; main.isReleasedWhenClosed = false
        let content = main.contentView!
        let actions: [(String, Selector)] = [("置前主窗口", #selector(raiseMain)), ("放大 / 恢复", #selector(zoom)), ("全屏 / 退出全屏", #selector(fullscreen)), ("打开第二窗口", #selector(showSecond)), ("最小化主窗口", #selector(minimize))]
        for (i, action) in actions.enumerated() {
            let button = NSButton(title: action.0, target: self, action: action.1)
            button.bezelStyle = .rounded; button.frame = NSRect(x: 30, y: 350-Double(i)*55, width: 230, height: 36); content.addSubview(button)
        }
        main.makeKeyAndOrderFront(nil); NSApp.activate(ignoringOtherApps: true)
        let marker = Bundle.main.bundlePath+".window-id"
        try? String(main.windowNumber).write(toFile: marker, atomically: true, encoding: .utf8)
        let menu = NSMenu(), root = NSMenuItem(), app = NSMenu()
        app.addItem(withTitle: "退出测试窗口", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        root.submenu = app; menu.addItem(root); NSApp.mainMenu = menu
    }
    @objc func raiseMain() { if main.isMiniaturized { main.deminiaturize(nil) }; main.makeKeyAndOrderFront(nil); NSApp.activate(ignoringOtherApps: true) }
    @objc func zoom() { main.zoom(nil) }
    @objc func fullscreen() { main.toggleFullScreen(nil) }
    @objc func minimize() { main.miniaturize(nil) }
    @objc func showSecond() {
        if second == nil {
            second = NSWindow(contentRect: NSRect(x: 350, y: 350, width: 380, height: 200), styleMask: [.titled, .closable, .resizable], backing: .buffered, defer: false)
            second!.title = "Window Pet · secondary probe"; second!.isReleasedWhenClosed = false
            let restore = NSButton(title: "恢复主窗口", target: self, action: #selector(raiseMain))
            restore.bezelStyle = .rounded; restore.frame = NSRect(x: 40, y: 85, width: 250, height: 36); second!.contentView!.addSubview(restore)
        }
        second!.makeKeyAndOrderFront(nil)
    }
}
let app = NSApplication.shared
let delegate = SwitchingDelegate()
app.setActivationPolicy(.regular); app.delegate = delegate; app.run()
