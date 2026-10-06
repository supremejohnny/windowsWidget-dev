# Window Pet

A small Windows overlay pet attached to a selected corner of a window. Move the target window to swing the pet; click the pet to poke it. Click the single fixed anchor to tuck the pet away, leaving an 18 px static rope stub. Click again to bring the pet back.

## Build and run

```powershell
.\build.ps1
.\WindowPet.exe
```

This uses the C# compiler included with .NET Framework. No SDK or package download is needed on a standard Windows installation. Choose an open window, one of three placeholder pets, one of four corners (upper left, upper right, lower left, lower right), and a fixed rope length: short 55 px, medium 85 px, long 130 px, or extra long 190 px. Press **Attach**. Use **Refresh** to update the window list and **Detach** to remove the pet. Closing the control window exits.
Close a running WindowPet before rebuilding its executable.

Run `.\test.ps1` for the rope and window-behavior smoke tests.

## Implementation

- `SettingsForm` lists visible top-level windows by HWND and offers the three settings.
- `PetForm` is a transparent, mouse-transparent drawing window. Its location is derived directly from the selected window's `GetWindowRect`. The anchor is rendered at a fixed pixel inside this overlay.
- `HitForm` provides two tiny, near-invisible elliptical input regions: one following the pet body for pokes, and one fixed at the anchor for stowing and restoring. The rest of the drawing canvas passes mouse input to the application underneath.
- `RopePhysics` uses 16 segments, a fixed 120 Hz simulation step, and alternating distance-constraint sweeps. The root is pinned exactly on every simulation and render update. The heavier endpoint and damped motion soften abrupt window movement. Stowed mode runs no rope physics.
- Rope rendering uses a smooth cubic path with rounded ends and a fine inner highlight. `LayeredSurface` presents premultiplied pixel alpha, avoiding color-key fringes. One 520 × 420 bitmap and native drawing context are reused for the attachment lifetime and disposed on close.
- The 520 × 420 overlay reserves space for maximum rope length and swing. It is positioned and the pet is initialized before the first paint, avoiding the startup flash at the edge.
- The drawing layer and two small input regions remain attached to the target's normal window order, so other windows cover them. None takes focus. The app polls at 16 ms while moving and 66 ms at rest, and repaints only on changes. No image assets or browser engine are loaded.

Window stacking is synchronized on each tick, so a newly raised window can briefly expose the pet for at most one idle tick (66 ms). Event-driven z-order updates and multi-window attachments are later architecture steps. The three pets are original placeholder drawings.

## Relevant open source

- [electron-overlay-window](https://github.com/SnosMe/electron-overlay-window): target discovery and overlay following (MIT); its single-overlay model is less suitable for future multi-window support.
- [ImOverlay-DX11](https://github.com/rabbanyhmm/ImOverlay-DX11): native multi-window transparent rendering and click-through patterns.
- [procmod/overlay](https://github.com/procmod/overlay): Rust/Direct3D target-following overlay; its input is fully click-through.
- [Unity-Verlet-Rope](https://github.com/jongallant/Unity-Verlet-Rope): MIT rope reference. Its node-and-constraint idea informed the dependency-free implementation in `RopePhysics.cs`.

See [repo evaluation](REPOSITORY_EVALUATION.md) for local clones and validation results.
