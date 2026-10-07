import Foundation
import CoreGraphics

// State independent of drawing and rope physics. Losing focus hides immediately;
// resume only after a short activation grace and two stable geometry samples.
struct PresentationGate {
    private var resumeAt = 0.0, stableSince = 0.0
    private var candidate: CGRect?
    private(set) var suspended = false
    mutating func suspend(_ now: Double) {
        suspended = true; resumeAt = now+0.15; candidate = nil; stableSince = now
    }
    mutating func permits(_ frame: CGRect?, eligible: Bool, now: Double) -> Bool {
        guard eligible, let frame = frame else {
            if !suspended { suspend(now) }
            candidate = nil
            return false
        }
        if !suspended { return true } // Normal live dragging has no geometry delay.
        if candidate != frame { candidate = frame; stableSince = now }
        guard now >= resumeAt && (now-stableSince >= 1.0/30 || now >= resumeAt+0.15) else { return false }
        suspended = false; return true
    }
}
