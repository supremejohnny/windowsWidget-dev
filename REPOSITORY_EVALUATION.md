# Repository evaluation (2026-10-06)

All candidates were cloned outside this Git repository, under `C:\Users\Administrator\Desktop\windowWidget-references`. Each checkout is an independent repository.

| Repository | Local revision | Validation | Decision |
| --- | --- | --- | --- |
| [physics-rope](https://github.com/code4fukui/physics-rope) | `0cf6431` | Cloned outside the project; inspected MIT license and interactive demo; original JS generates 450 reference samples that match the C# port within 1e-6 px (observed error 9.66e-13 px) | Ported integration and quarter-distance correction into `VerletRopeSolver.cs`. Adapted the two-fixed-end demo to a free pendant end, zero rope gravity, weighted endpoint and moving-window damping. No browser or JS runtime in the application |
| [Unity-Verlet-Rope](https://github.com/jongallant/Unity-Verlet-Rope) | `b28f56e` | Read MIT license and `Rope.cs`; its implementation requires Unity types and scene objects | Evaluated for an earlier prototype; the current core uses the smaller physics-rope implementation |
| [electron-overlay-window](https://github.com/SnosMe/electron-overlay-window) | `22a4dd2` | `npm ci --ignore-scripts` and TypeScript `tsc --noEmit` pass | Native Windows source contains foreground and location event handling worth adapting later; direct package use would require Electron and add substantial memory |
| [ImOverlay-DX11](https://github.com/rabbanyhmm/ImOverlay-DX11) | `ffa59a3` | Inspected CMake and MIT license; no C++ toolchain on this host | Could serve a future native Direct3D renderer, but it requires Dear ImGui and D3D11; too large for the present pet |
| [procmod/overlay](https://github.com/procmod/overlay) | `9976216` | Inspected Cargo manifest and MIT license; Rust toolchain unavailable here | Useful reference for native window tracking; its click-through game overlay model would require interaction changes |

Our own `test.ps1` compiles and runs physics and GUI smoke tests. It verifies the fixed anchor, rope length, restrained curvature during slow dragging, visible segment bending during sideways reversals, absence of restoring rope gravity when endpoint gravity is disabled, vertical slack, frame-rate consistency, center-of-mass body rotation, poke response, pre-show placement, x/y tracking, the target-pet-other window order, and mouse-transparent drawing outside the pet's separate hit region.

The physics-rope reference test uses an unmodified copy of upstream Rope.js,
Vector2.js and lerp.js with both ends fixed, 75 points, damping 0.95 and 500
iterations. It moves the first anchor and compares six snapshots over 121
updates. The core test disables native pendant adaptations to isolate parity.
Fixtures can be regenerated with `node tests/GenerateRopeReference.mjs`.

On this host, a native 190 px rope profile with 3,000 moving frames took 0.141 ms
per 60 Hz frame (including two 120 Hz physics steps). The last step averaged 371
constraint iterations and peaked at 374, below its adaptive maximum of 2,000.
The live managed heap did not grow after collection. The GUI smoke test also
kept GDI handles at 43 before and after 300 redraws. These are local measurements,
not timing guarantees for other machines.
