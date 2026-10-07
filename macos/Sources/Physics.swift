import Foundation

// Swift port of VerletRopeSolver.cs; upstream physics-rope by James Wrightson (MIT).
final class RopeNode {
    var x, y, oldX, oldY: Double
    var gravityX = 0.0, gravityY = 0.0, damping = 0.95, inverseMass = 1.0
    var fixed = false
    init(_ x: Double, _ y: Double) { self.x = x; self.y = y; oldX = x; oldY = y }
}
final class VerletRopeSolver {
    let nodes: [RopeNode], spacing: Double, iterations: Int
    var previousStep = 0.0, frameX = 0.0, frameY = 0.0, tolerance = 0.0, maxSpeed = 0.0
    init(_ nodes: [RopeNode], _ spacing: Double, _ iterations: Int) {
        self.nodes = nodes; self.spacing = spacing; self.iterations = iterations
    }
    func update(_ dt: Double) {
        let correction = previousStep > 0 ? dt / previousStep : 0
        for p in nodes where !p.fixed {
            let vx = p.x - p.oldX, vy = p.y - p.oldY
            p.oldX = p.x; p.oldY = p.y
            let damping = pow(p.damping, dt * 60) * correction
            var rx = vx - frameX * previousStep, ry = vy - frameY * previousStep
            let limit = maxSpeed * previousStep
            if maxSpeed > 0 && rx * rx + ry * ry > limit * limit {
                let scale = limit / sqrt(rx * rx + ry * ry); rx *= scale; ry *= scale
            }
            p.x += rx * damping + frameX * dt + p.gravityX * dt * dt
            p.y += ry * damping + frameY * dt + p.gravityY * dt * dt
        }
        for _ in 0..<iterations {
            var error = 0.0
            for i in nodes.indices where !nodes[i].fixed {
                if i + 1 < nodes.count { error = max(error, constrain(i, i + 1)) }
                if i > 0 { error = max(error, constrain(i, i - 1)) }
            }
            if tolerance > 0 && error < tolerance { break }
        }
        previousStep = dt
    }
    private func constrain(_ i: Int, _ j: Int) -> Double {
        let a = nodes[i], b = nodes[j], dx = b.x - a.x, dy = b.y - a.y, distance = sqrt(dx * dx + dy * dy)
        if distance < 1e-9 { return spacing }
        let correction = (distance - spacing) / distance * 0.25
        var aShare = 1.0, bShare = 1.0
        if !a.fixed && !b.fixed {
            let total = a.inverseMass + b.inverseMass
            aShare = 2 * a.inverseMass / total; bShare = 2 * b.inverseMass / total
        }
        if !a.fixed { a.x += dx * correction * aShare; a.y += dy * correction * aShare }
        if !b.fixed { b.x -= dx * correction * bShare; b.y -= dy * correction * bShare }
        return abs(distance - spacing)
    }
}
struct Point { var x: Double; var y: Double }
// Frozen Windows-style adapter, retained for comparison tests. The app uses Mac RopePhysics.
final class LegacyRopePhysics {
    static let segments = 12
    static let fixedStep: Float = 1.0 / 120
    static let step = Double(fixedStep)
    let solver: VerletRopeSolver, spacing: Double
    var points: [Point]
    private var offsetX = 0.0, offsetY = 0.0, simX = 0.0, simY = 0.0
    private var pendingX = 0.0, pendingY = 0.0
    private var accumulator: Float = 0
    init(_ length: Double, _ x: Double = 300, _ y: Double = 52) {
        let spacing = Double(Float(length) / Float(Self.segments))
        self.spacing = spacing
        let nodes = (0...Self.segments).map { RopeNode(Double(Float(x)), Double(Float(y) + Float($0) * Float(spacing))) }
        points = nodes.map { Point(x: $0.x, y: $0.y) }
        for n in nodes { n.damping = 0.90 }
        nodes[0].fixed = true
        nodes[Self.segments].inverseMass = 0.1
        nodes[Self.segments].damping = 0.93
        nodes[Self.segments].gravityY = 980
        solver = VerletRopeSolver(nodes, spacing, 2000)
        solver.tolerance = 0.004; solver.maxSpeed = 240; solver.previousStep = Self.step
    }
    func shift(_ dx: Double, _ dy: Double) {
        let dx = Double(Float(dx)), dy = Double(Float(dy))
        offsetX += dx; offsetY += dy; pendingX += dx; pendingY += dy
    }
    func poke(_ horizontal: Double) {
        solver.nodes[Self.segments].oldX -= horizontal * 0.12
        solver.nodes[Self.segments].oldY -= 0.12
    }
    func update(_ dt: Double, _ x: Double = 300, _ y: Double = 52) {
        accumulator += max(Float(0.001), min(Float(0.05), Float(dt)))
        let shiftX = pendingX / Double(accumulator) * Self.step, shiftY = pendingY / Double(accumulator) * Self.step
        while accumulator >= Self.fixedStep {
            simX += shiftX; simY += shiftY
            solver.nodes[0].x = x + simX; solver.nodes[0].y = y + simY
            solver.frameX = shiftX / Self.step; solver.frameY = shiftY / Self.step
            prepareSlack(); solver.update(Self.step)
            accumulator -= Self.fixedStep; pendingX -= shiftX; pendingY -= shiftY
        }
        for i in 1...Self.segments {
            points[i] = Point(x: Double(Float(solver.nodes[i].x - offsetX)), y: Double(Float(solver.nodes[i].y - offsetY)))
        }
        points[0] = Point(x: x, y: y)
    }
    private func prepareSlack() {
        let root = solver.nodes[0], end = solver.nodes[Self.segments]
        let dx = end.x - root.x, dy = end.y - root.y, chord = sqrt(dx * dx + dy * dy)
        if chord < 1e-6 { return }
        if abs(solver.frameX) + abs(solver.frameY) < 1 {
            var vx = (end.x - end.oldX) / Self.step, vy = (end.y - end.oldY) / Self.step
            let inward = (vx * dx + vy * dy) / chord
            if inward < -120 {
                vx -= (inward + 120) * dx / chord; vy -= (inward + 120) * dy / chord
                end.oldX = end.x - vx * Self.step; end.oldY = end.y - vy * Self.step
            }
        }
        if chord >= spacing * Double(Self.segments) - 0.02 { return }
        var bow = 0.0
        for i in 1..<Self.segments {
            let node = solver.nodes[i]
            bow = max(bow, abs((node.x-root.x)*dy-(node.y-root.y)*dx)/chord)
        }
        if bow < 0.001 {
            let middle = solver.nodes[Self.segments / 2], sx = -dy / chord * 0.01, sy = dx / chord * 0.01
            middle.x += sx; middle.oldX += sx; middle.y += sy; middle.oldY += sy
        }
    }
    // Match C# PointF/float adapter precision; solver arithmetic stays double.
    var settled: Bool {
        for i in 1...Self.segments {
            let n = solver.nodes[i]
            if abs(points[i].x - 300) > 2 || abs(points[i].y - (52 + Double(i)*spacing)) > 2 ||
                abs(n.x - n.oldX) > 0.06 || abs(n.y - n.oldY) > 0.06 { return false }
        }
        return abs(pendingX) < 0.001 && abs(pendingY) < 0.001
    }
}
final class PetBodyPhysics {
    let centerX, centerY, moment: Double
    var angle: Double, velocity = 0.0
    private var sampled = false, priorX = 0.0, priorY = 0.0, vx = 0.0, vy = 0.0, ax = 0.0, ay = 0.0
    var restAngle: Double { atan2(centerX, centerY) }
    var settled: Bool { abs(angle-restAngle) < 0.007 && abs(velocity) < 0.025 }
    init(_ x: Double, _ y: Double, _ moment: Double) {
        centerX = x; centerY = y; self.moment = moment; angle = atan2(x, y)
    }
    func reset() { angle = restAngle; velocity = 0; sampled = false; vx = 0; vy = 0; ax = 0; ay = 0 }
    // Stage/Space changes translate the pivot without applying an artificial
    // impulse. Preserve the pendant angle and angular velocity while paused.
    func rebasePivot(_ x: Double, _ y: Double) {
        sampled = true; priorX = x; priorY = y; vx = 0; vy = 0; ax = 0; ay = 0
    }
    func update(_ delta: Double, _ x: Double, _ y: Double) {
        let dt = max(0.001, min(0.05, delta))
        if !sampled { priorX = x; priorY = y; sampled = true }
        else {
            let dx = x-priorX, dy = y-priorY; priorX = x; priorY = y
            if abs(dx) > 250 || abs(dy) > 250 { vx = 0; vy = 0; ax = 0; ay = 0 }
            else {
                let alpha = 1-exp(-dt*16), nx = vx+alpha*(dx/dt-vx), ny = vy+alpha*(dy/dt-vy)
                let rx = max(-2200, min(2200, (nx-vx)/dt)), ry = max(-2200, min(2200, (ny-vy)/dt))
                vx = nx; vy = ny
                let a = 1-exp(-dt*14); ax += a*(rx-ax); ay += a*(ry-ay)
            }
        }
        let rx = centerX*cos(angle)-centerY*sin(angle), ry = centerX*sin(angle)+centerY*cos(angle)
        velocity += ((rx*(980-ay)+ry*ax)/moment-2.2*velocity)*dt
        velocity = max(-3, min(3, velocity)); angle += velocity*dt
        if angle < -0.65 { angle = -0.65; velocity = max(0, velocity) }
        if angle > 0.65 { angle = 0.65; velocity = min(0, velocity) }
    }
}
