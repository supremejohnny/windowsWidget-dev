import AppKit

func check(_ ok: Bool, _ name: String) {
    if !ok { print("FAIL \(name)"); exit(1) }
    print("PASS \(name)")
}
let frame = CGRect(x: 100, y: 100, width: 600, height: 400)
let windows = [
    TargetWindow(id: 12, pid: 7, label: "Codex — foreground", frame: frame),
    TargetWindow(id: 13, pid: 7, label: "Codex — helper", frame: frame, onScreen: false),
    TargetWindow(id: 21, pid: 8, label: "微信 — main", frame: frame),
    TargetWindow(id: 14, pid: 7, label: "Codex — another desktop", frame: frame, onScreen: false),
    TargetWindow(id: 31, pid: 9, label: "Mail — fullscreen", frame: frame, onScreen: false)
]
let choices = applicationChoices(windows)
check(choices.count == 3, "multiple helper windows become one application choice")
check(choices.first(where: { $0.pid == 7 })?.id == 12, "server front order preserved within application")
check(choices.contains(where: { $0.pid == 9 }), "other desktop application remains selectable")
check(choices.map(applicationName).allSatisfy { !$0.contains("—") && !$0.contains("Window #") }, "application labels do not expose opaque window IDs")
let row: [String: Any] = [kCGWindowNumber as String: UInt32(42), kCGWindowOwnerPID as String: Int32(7),
                        kCGWindowLayer as String: 0, kCGWindowBounds as String: frame.dictionaryRepresentation,
                        kCGWindowAlpha as String: 0.0]
check(windowCandidates([row]).isEmpty, "transparent helper surfaces cannot become target")
var opaque = row; opaque[kCGWindowAlpha as String] = 1.0
check(windowCandidates([opaque]).count == 1, "untitled real windows remain available without screen recording permission")
