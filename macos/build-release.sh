#!/bin/bash
set -euo pipefail
cd "$(dirname "$0")/.."
./macos/build.sh
app_path="$PWD/dist/macos/Window Pet.app"
release_version=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$app_path/Contents/Info.plist")
release_arch=$(uname -m)
release_dir="$PWD/dist/macos-release/v$release_version"
mkdir -p "$release_dir"
codesign --verify --strict "$app_path"
zip_name="WindowPet-$release_version-macos-$release_arch.zip"
ditto -c -k --sequesterRsrc --keepParent "$app_path" "$release_dir/$zip_name"
(cd "$release_dir" && shasum -a 256 "$zip_name" > SHA256SUMS.txt)
printf 'Release assets: %s\n' "$release_dir"
