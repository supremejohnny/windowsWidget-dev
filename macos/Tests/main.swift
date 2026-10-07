import Foundation

func check(_ condition: Bool, _ message: String) {
    if !condition { fputs("FAIL: \(message)\n", stderr); exit(1) }
}
let csv = try String(contentsOfFile: "tests/fixtures/physics-rope-reference.csv")
var expected: [Int: [(Int, Double, Double)]] = [:]
for line in csv.split(separator: "\n").dropFirst() {
    let fields = line.split(separator: ",")
    expected[Int(fields[0])!, default: []].append((Int(fields[1])!, Double(fields[2])!, Double(fields[3])!))
}
let nodes = (0..<75).map { RopeNode(100+600*Double($0)/74, 240) }
for n in nodes { n.gravityY = 3000.88 }
nodes[0].fixed = true; nodes[74].fixed = true
let solver = VerletRopeSolver(nodes, 8, 500)
var largest = 0.0, samples = 0
for frame in 0...120 {
    nodes[0].x = 100+30*sin(Double(frame)*0.07); nodes[0].y = 240+15*sin(Double(frame)*0.05)
    solver.update(1.0/60)
    for (i, x, y) in expected[frame] ?? [] {
        largest = max(largest, abs(nodes[i].x-x), abs(nodes[i].y-y)); samples += 1
    }
}
check(samples == 450 && largest < 1e-6, "upstream parity")
print("PASS upstream fixture: \(samples) nodes, max error \(largest)")
for length in [55.0, 85, 130, 190] {
    let rope = LegacyRopePhysics(length)
    var peak = 0.0, bow = 0.0
    for frame in 0..<240 {
        if frame < 10 { rope.shift(6, 0) }
        rope.update(1.0/60)
        let end = rope.points.last!, root = rope.points[0], dx = end.x-root.x, dy = end.y-root.y
        peak = max(peak, abs(end.x-300))
        for p in rope.points { bow = max(bow, abs((p.x-root.x)*dy-(p.y-root.y)*dx)/hypot(dx, dy)) }
        var total = 0.0
        for i in 0..<12 { total += hypot(rope.points[i+1].x-rope.points[i].x, rope.points[i+1].y-rope.points[i].y) }
        check(abs(total-length) < 0.5, "rope length \(length)")
        check(rope.points[0].x == 300 && rope.points[0].y == 52, "fixed anchor")
    }
    check(peak > 5 && peak < 45 && bow > 2 && bow < 20 && rope.settled, "controlled lateral motion \(length)")
    let slow = LegacyRopePhysics(length)
    var prior = 0.0, slowBow = 0.0, slowSlack = 0.0
    for frame in 1...240 {
        let position = frame <= 180 ? 25*(1-cos(Double.pi*Double(frame)/180)) : 50
        slow.shift(position-prior, 0); prior = position; slow.update(1.0/60)
        let end = slow.points.last!, dx = end.x-300, dy = end.y-52, chord = hypot(dx, dy)
        slowSlack = max(slowSlack, length-chord)
        for p in slow.points { slowBow = max(slowBow, abs((p.x-300)*dy-(p.y-52)*dx)/chord) }
    }
    print("Slow drag \(Int(length))pt: bow \(slowBow), slack \(slowSlack)")
    check(slowBow < 2.5 && slowSlack < 0.1, "Windows slow eased drag contract")
    let steady = LegacyRopePhysics(length)
    for _ in 0..<300 { steady.shift(1.0/3, 0); steady.update(1.0/60) }
    check(abs(steady.points.last!.x-300) < 1.5, "Windows constant speed contract")
    let a = LegacyRopePhysics(length), b = LegacyRopePhysics(length), c = LegacyRopePhysics(length)
    a.poke(7); b.poke(7); c.poke(7)
    for frame in 0..<240 {
        if frame < 10 { a.shift(6, -3) }; a.update(1.0/60)
        for _ in 0..<2 { if frame < 10 { b.shift(3, -1.5) }; b.update(1.0/120) }
        for _ in 0..<4 { if frame < 10 { c.shift(1.5, -0.75) }; c.update(1.0/240) }
    }
    check(hypot(a.points.last!.x-b.points.last!.x, a.points.last!.y-b.points.last!.y) < 0.05, "60/120Hz consistency")
    check(hypot(a.points.last!.x-c.points.last!.x, a.points.last!.y-c.points.last!.y) < 0.05, "60/240Hz consistency")
    let slack = LegacyRopePhysics(length)
    var maximumSlack = 0.0
    for frame in 0..<240 {
        if frame < 12 { slack.shift(0, 10) }; slack.update(1.0/60)
        let end = slack.points.last!
        let chord = hypot(end.x-300, end.y-52)
        maximumSlack = max(maximumSlack, length-chord)
        check(chord <= length+0.1, "tension bound")
    }
    check(maximumSlack > 0.1 && slack.settled, "slack settles")
    print("PASS \(Int(length))pt rope: peak \(peak), bow \(bow), fixed step and slack")
}
let body = PetBodyPhysics(0, 85, 9500)
body.update(1.0/60, 0, 0)
for i in 1...8 { body.update(1.0/60, Double(i)*5, 0) }
check(body.angle > 0.01, "body lags accelerating pivot")
for _ in 0..<600 { body.update(1.0/60, 40, 0) }
check(body.settled && abs(body.angle-body.restAngle) < 0.008, "body returns to rest")
body.reset(); check(body.angle == body.restAngle, "body reset")
print("PASS body acceleration, settling, reset")

struct MotionMetrics {
    var end = 0.0, upper = 0.0, upperBow = 0.0, bow = 0.0, lengthError = 0.0
    mutating func sample(_ points: [Point], _ length: Double) {
        let root = points[0], tail = points.last!, dx = tail.x-root.x, dy = tail.y-root.y, chord = hypot(dx, dy)
        end = max(end, abs(dx))
        for p in upperCordPoints(points) {
            upper = max(upper, abs(p.x-root.x))
            upperBow = max(upperBow, abs((p.x-root.x)*dy-(p.y-root.y)*dx)/chord)
        }
        for p in points.dropFirst() { bow = max(bow, abs((p.x-root.x)*dy-(p.y-root.y)*dx)/chord) }
        let total = zip(points, points.dropFirst()).reduce(0.0) { $0+hypot($1.1.x-$1.0.x, $1.1.y-$1.0.y) }
        lengthError = max(lengthError, abs(total-length))
        check(points.allSatisfy { $0.x.isFinite && $0.y.isFinite }, "finite Mac cord")
        check(root.x == 300 && root.y == 52, "Mac pin fixed")
    }
}
for length in [55.0, 85, 130, 190] {
    let mac = RopePhysics(length), legacy = LegacyRopePhysics(length)
    var macMetrics = MotionMetrics(), legacyMetrics = MotionMetrics(), elapsed = 0.0, previous = 0.0
    let cadence = [1.0/60, 1.0/60, 1.0/30, 1.0/120]
    // Integer-pixel micro-drag with uneven callbacks and reversals, rather than
    // only the previous ideal, smooth 50-point trajectory.
    for frame in 0..<360 {
        let dt = cadence[frame % cadence.count]; elapsed += dt
        let position = frame < 240 ? (2*sin(4*Double.pi*elapsed)).rounded() : 0
        let dx = position-previous; previous = position
        mac.shift(dx, 0); legacy.shift(dx, 0); mac.update(dt); legacy.update(dt)
        macMetrics.sample(mac.points, length); legacyMetrics.sample(legacy.points, length)
    }
    check(macMetrics.end < 5 && macMetrics.upper < 1.5 && macMetrics.upperBow < 1.0, "micro movement must not amplify upper cord")
    check(macMetrics.lengthError < 0.005 && mac.settled, "micro cord length and settling")
    print("MAC \(Int(length))pt micro: endpoint \(macMetrics.end), upper \(macMetrics.upper), upper bow \(macMetrics.upperBow); legacy upper \(legacyMetrics.upper), upper bow \(legacyMetrics.upperBow)")
    let strong = RopePhysics(length)
    var strongMetrics = MotionMetrics(), releasedMetrics = MotionMetrics()
    for frame in 0..<360 {
        if frame < 6 { strong.shift(12, 0) }
        strong.update(1.0/60); strongMetrics.sample(strong.points, length)
        if frame >= 6 { releasedMetrics.sample(strong.points, length) }
    }
    check(strongMetrics.end > 8 && strongMetrics.end < length*0.59, "large drag still swings with a bound")
    // A rigid pendulum with a cosmetic curve must fail: independent cord
    // bending remains visible after the window has stopped, then decays.
    check(strongMetrics.bow > 2 && releasedMetrics.bow > 2 && strongMetrics.upperBow < length*0.025+2 && strongMetrics.lengthError < 0.005 && strong.settled, "independent cord curvature after release, bounded upper response and settling")
    let poked = RopePhysics(length); poked.poke(7)
    var pokeMetrics = MotionMetrics(), bendSigns = Set<Int>()
    for _ in 0..<480 {
        poked.update(1.0/60); pokeMetrics.sample(poked.points, length)
        let root = poked.points[0], middle = poked.points[RopePhysics.segments/2], tail = poked.points.last!
        let bend = ((middle.x-root.x)*(tail.y-root.y)-(middle.y-root.y)*(tail.x-root.x))/hypot(tail.x-root.x, tail.y-root.y)
        if abs(bend) > 0.2 { bendSigns.insert(bend > 0 ? 1 : -1) }
    }
    check(pokeMetrics.end > 3 && pokeMetrics.bow > 1 && bendSigns.count == 2 && poked.settled, "poke creates independently bending cord, rebound and recovery")
    let vertical = RopePhysics(length)
    var verticalMetrics = MotionMetrics()
    for frame in 0..<360 {
        if frame < 12 { vertical.shift(0, frame.isMultiple(of: 2) ? 2 : -2) }
        vertical.update(1.0/60); verticalMetrics.sample(vertical.points, length)
    }
    check(verticalMetrics.upper < 1 && verticalMetrics.lengthError < 0.005, "tiny vertical moves do not create sideways upper waves")
    let sixty = RopePhysics(length), oneTwenty = RopePhysics(length), twoForty = RopePhysics(length)
    var cadenceError = 0.0
    for frame in 0..<240 {
        let dx = frame < 10 ? 2.0 : 0
        sixty.shift(dx, 0); sixty.update(1.0/60)
        for _ in 0..<2 { oneTwenty.shift(dx/2, 0); oneTwenty.update(1.0/120) }
        for _ in 0..<4 { twoForty.shift(dx/4, 0); twoForty.update(1.0/240) }
        for (a, b) in zip(sixty.points, oneTwenty.points) { cadenceError = max(cadenceError, hypot(a.x-b.x, a.y-b.y)) }
        for (a, b) in zip(sixty.points, twoForty.points) { cadenceError = max(cadenceError, hypot(a.x-b.x, a.y-b.y)) }
    }
    check(cadenceError < 0.02, "Mac full-trajectory 60/120/240Hz response")
    print("PASS MAC \(Int(length))pt sharp drag peak \(strongMetrics.end), released bow \(releasedMetrics.bow), poke peak \(pokeMetrics.end), poke bow \(pokeMetrics.bow), rebound signs \(bendSigns.count), length error \(strongMetrics.lengthError), cadence error \(cadenceError)")
}
