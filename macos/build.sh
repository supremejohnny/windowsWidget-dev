#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
app="$PWD/dist/macos/Window Pet.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
swiftc -O -whole-module-optimization -target "$(uname -m)-apple-macos12.0" macos/Sources/Physics.swift macos/Sources/MacRopePhysics.swift macos/Sources/WindowTracking.swift macos/Sources/main.swift -framework AppKit -framework CoreGraphics -framework CoreVideo -framework ApplicationServices -o "$app/Contents/MacOS/WindowPet"
cp assets/amiya_test1/character.png "$app/Contents/Resources/character.png"
cp THIRD_PARTY_NOTICES.txt "$app/Contents/Resources/THIRD_PARTY_NOTICES.txt"
cp third_party/physics-rope/LICENSE "$app/Contents/Resources/physics-rope-LICENSE.txt"
cat > "$app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>WindowPet</string>
<key>CFBundleIdentifier</key><string>com.supremejohnny.windowpet.macos.prototype</string>
<key>CFBundleName</key><string>Window Pet</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>0.0.2</string>
<key>CFBundleVersion</key><string>2</string>
<key>LSMinimumSystemVersion</key><string>12.0</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST
codesign --force --sign - "$app"
printf 'Built: %s\n' "$app"
