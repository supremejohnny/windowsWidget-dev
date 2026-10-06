# Repository evaluation (2026-10-06)

All candidates were cloned outside this Git repository, under `C:\Users\Administrator\Desktop\windowWidget-references`. Each checkout is an independent repository.

| Repository | Local revision | Validation | Decision |
| --- | --- | --- | --- |
| [Unity-Verlet-Rope](https://github.com/jongallant/Unity-Verlet-Rope) | `b28f56e` | Read MIT license and `Rope.cs`; its implementation requires Unity types and scene objects | Adapted its node and distance-constraint approach into our dependency-free `RopePhysics.cs`; local physics smoke test passes |
| [electron-overlay-window](https://github.com/SnosMe/electron-overlay-window) | `22a4dd2` | `npm ci --ignore-scripts` and TypeScript `tsc --noEmit` pass | Native Windows source contains foreground and location event handling worth adapting later; direct package use would require Electron and add substantial memory |
| [ImOverlay-DX11](https://github.com/rabbanyhmm/ImOverlay-DX11) | `ffa59a3` | Inspected CMake and MIT license; no C++ toolchain on this host | Could serve a future native Direct3D renderer, but it requires Dear ImGui and D3D11; too large for the present pet |
| [procmod/overlay](https://github.com/procmod/overlay) | `9976216` | Inspected Cargo manifest and MIT license; Rust toolchain unavailable here | Useful reference for native window tracking; its click-through game overlay model would require interaction changes |

Our own `test.ps1` compiles and runs physics and GUI smoke tests. It verifies the fixed anchor, segment lengths, poke response, pre-show placement, x/y tracking, and hiding when another window becomes foreground.
