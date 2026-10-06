# Window Pet

A small Windows overlay pet that hangs from the upper-left corner of a selected window. Move the target window to swing the pet; click the pet to poke it. The anchor stays fixed to the target window.

## Build and run

```powershell
.\build.ps1
.\WindowPet.exe
```

This uses the C# compiler included with .NET Framework. No SDK or package download is needed on a standard Windows installation. Choose an open window, one of three placeholder pets, and rope length, then press **Attach**. Use **Refresh** to update the window list and **Detach** to remove the pet. Closing the control window exits.

## Implementation

- `SettingsForm` lists visible top-level windows by HWND and offers the three settings.
- `PetForm` is one transparent WinForms window. Its location is derived directly from the selected window's `GetWindowRect`. The anchor is rendered at a fixed pixel inside this overlay.
- A damped pendulum stores angle and angular velocity. Horizontal target motion adds angular velocity; clicking adds a small impulse and squash effect.
- The 440 × 390 overlay reserves space for maximum rope length and swing. It polls at 16 ms while moving and 66 ms at rest, and repaints only on changes. No image assets or browser engine are loaded.

The overlay is topmost and may appear above unrelated windows. Target foreground and z-order tracking and multi-window attachments are next architecture steps. The three pets are original placeholder drawings.

## Relevant open source

- [electron-overlay-window](https://github.com/SnosMe/electron-overlay-window): target discovery and overlay following (MIT); its single-overlay model is less suitable for future multi-window support.
- [ImOverlay-DX11](https://github.com/rabbanyhmm/ImOverlay-DX11): native multi-window transparent rendering and click-through patterns.
- [procmod/overlay](https://github.com/procmod/overlay): Rust/Direct3D target-following overlay; its input is fully click-through.
