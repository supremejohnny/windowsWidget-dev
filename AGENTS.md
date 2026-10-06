# Development and publication

- This directory is the development root for Window Pet.
- Source and full development history belong only in the private GitHub repository `supremejohnny/windowsWidget-dev`.
- Before pushing source, verify that origin is this repository and GitHub reports `private: true`. Never change it to public.
- The public binary distribution repository is `supremejohnny/windowWidget`. Its separate local checkout is `dist/public-repo/`.
- Only README, third-party notices and explicitly requested public documentation belong in the public repository. Upload compiled executables/checksums as Release assets. Never push development commits, source, tests or asset working files there.
- Build/test from this directory. `build-release.ps1` creates distribution artifacts without overwriting the root executable.
- Consult `docs/PUBLISHING.md` for release records and repository boundaries.
