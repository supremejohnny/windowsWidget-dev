# physics-rope reference

Source: https://github.com/code4fukui/physics-rope
Revision: `0cf6431ebefafa771b56d1bc98ccf1222795cf25`
License: MIT, copyright 2018 James Wrightson (see LICENSE).

Rope.js, Vector2.js and lerp.js are unchanged copies used by the reference
parity test. They are not loaded by WindowPet.exe and add no JavaScript runtime
dependency to the application.

VerletRopeSolver.cs ports the integration and symmetric quarter-distance
constraint correction. Native adaptations: fixed 120 Hz timing, damping scaled
from its 60 Hz value and applied relative to window translation, inverse-mass
weighting for a heavier free pendant end, and a guard for coincident nodes.
The native solver stops once errors converge, with a larger maximum budget for
abrupt drags. Relative speed and inward endpoint rebound are bounded. RopePhysics.cs
supplies moving anchor positions and zero rope-node gravity; pendant gravity
remains at the free endpoint. Subpixel perturbation breaks the symmetry of
perfectly straight vertical compression without adding gravity or velocity.

The upstream demo fixes both ends. Its mass property adds a vertical acceleration
instead of changing inertial mass. The native port uses explicit endpoint gravity
and inverse mass to distinguish those two effects.

`tests/GenerateRopeReference.mjs` calls the unchanged original Rope.js to create
450 positions with both endpoints fixed. `tests/RopeReferenceSmoke.cs` checks the
C# core against these positions with native adaptations disabled. Observed maximum
error at the recorded revision: 9.66e-13 px.
