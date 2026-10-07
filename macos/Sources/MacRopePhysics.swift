import Foundation

// Compare the same upper quarter at any simulation resolution. Include the
// interpolated boundary, rather than calling three nodes the upper portion.
func upperCordPoints(_ points: [Point]) -> [Point] {
    let boundary = Double(points.count-1)*0.25, index = Int(boundary)
    var upper = index > 0 ? Array(points[1...index]) : []
    let fraction = boundary-Double(index)
    if fraction > 0 {
        let a = points[index], b = points[index+1]
        upper.append(Point(x: a.x+(b.x-a.x)*fraction, y: a.y+(b.y-a.y)*fraction))
    }
    return upper
}

// Ten constrained, independently moving segments in anchor-local coordinates.
// Native geometry remains immediate; filtered frame acceleration drives inertia.
final class RopePhysics {
    static let segments = 10
    static let step = 1.0 / 120
    private static let motionGain = 0.60
    private static let velocityAlpha = 1-exp(-step/0.080)
    private static let accelerationAlpha = 1-exp(-step/0.045)
    let length: Double, spacing: Double
    private(set) var points: [Point]
    private let solver: VerletRopeSolver
    private var pendingX = 0.0, pendingY = 0.0, accumulator = 0.0
    private var velocityX = 0.0, velocityY = 0.0, accelerationX = 0.0, accelerationY = 0.0
    init(_ length: Double, _ x: Double = 300, _ y: Double = 52) {
        self.length = length; spacing = length/Double(Self.segments)
        let nodes = (0...Self.segments).map { RopeNode(x, y+Double($0)*length/Double(Self.segments)) }
        nodes[0].fixed = true
        // Distributed mass/gravity retain independent bending. Preserve the
        // approved mass density and damping when increasing resolution.
        for n in nodes { n.damping = 0.955 }
        // A ten-segment node has 6/10 of the old node mass; keep pendant
        // mass unchanged, so its inverse mass becomes 0.25 * 6/10.
        nodes[Self.segments].inverseMass = 0.25*6/Double(Self.segments)
        solver = VerletRopeSolver(nodes, spacing, 1000)
        solver.tolerance = 0.0004; solver.previousStep = Self.step; solver.maxSpeed = 240
        points = nodes.map { Point(x: $0.x, y: $0.y) }
    }
    func shift(_ dx: Double, _ dy: Double) { pendingX += dx; pendingY += dy }
    func poke(_ horizontal: Double) {
        // Impulse at the pendant only: the cord must respond through constraints.
        let end = solver.nodes[Self.segments]
        end.oldX -= horizontal*0.12
    }
    func update(_ delta: Double, _ x: Double = 300, _ y: Double = 52) {
        accumulator += max(0.001, min(0.05, delta))
        let rawX = pendingX/accumulator, rawY = pendingY/accumulator
        let shiftX = rawX*Self.step, shiftY = rawY*Self.step
        while accumulator+1e-12 >= Self.step {
            let alpha = Self.velocityAlpha
            let nextX = velocityX+alpha*(rawX-velocityX), nextY = velocityY+alpha*(rawY-velocityY)
            let ax = max(-1600, min(1600, (nextX-velocityX)/Self.step))
            let ay = max(-1600, min(1600, (nextY-velocityY)/Self.step))
            velocityX = nextX; velocityY = nextY
            let accelAlpha = Self.accelerationAlpha
            accelerationX += accelAlpha*(ax-accelerationX)
            accelerationY += accelAlpha*(ay-accelerationY)
            let root = solver.nodes[0]; root.x = x; root.y = y
            let forceX = -Self.motionGain*accelerationX, forceY = max(120, 980-Self.motionGain*accelerationY)
            for n in solver.nodes.dropFirst() {
                n.gravityX = forceX; n.gravityY = forceY
            }
            solver.update(Self.step)
            accumulator = max(0, accumulator-Self.step)
            pendingX -= shiftX; pendingY -= shiftY
        }
        for i in points.indices {
            let node = solver.nodes[i]
            points[i] = Point(x: node.x, y: node.y)
        }
        points[0] = Point(x: x, y: y)
    }
    var settled: Bool {
        for (i, n) in solver.nodes.enumerated().dropFirst() {
            if abs(n.x-solver.nodes[0].x) > 0.15 || abs(n.y-solver.nodes[0].y-Double(i)*spacing) > 0.15 ||
                hypot(n.x-n.oldX, n.y-n.oldY)/Self.step > 0.25 { return false }
        }
        return abs(velocityX)+abs(velocityY) < 0.5 && abs(accelerationX)+abs(accelerationY) < 2 &&
        abs(pendingX)+abs(pendingY) < 0.001
    }
}
