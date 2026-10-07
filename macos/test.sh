#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p .test-output/macos
swiftc -O -whole-module-optimization macos/Sources/Physics.swift macos/Sources/MacRopePhysics.swift macos/Tests/main.swift -o .test-output/macos/PhysicsTests
.test-output/macos/PhysicsTests
if [[ "${1:-}" == "--ui" ]]; then
    ./macos/build.sh
    swiftc -O -target "$(uname -m)-apple-macos12.0" macos/Tests/MovingWindow.swift -framework AppKit -o .test-output/macos/MovingWindow
    export WINDOWPET_MOVEMENT_FIXTURE="$PWD/.test-output/macos/MovingWindow"
    report="$PWD/.test-output/macos/ui-smoke.txt"
    rm -f "$report"
    "dist/macos/Window Pet.app/Contents/MacOS/WindowPet" --smoke "$report"
    cat "$report"
    if rg -q '^FAIL' "$report"; then exit 1; fi
fi
