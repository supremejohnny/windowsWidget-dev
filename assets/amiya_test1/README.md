# amiya_test1

- `reference.png`: user supplied source image.
- `character.png`: transparent character cutout generated from the source with the built-in image editor. The card background, text, border, and hanging chain were removed. The result is a visual interpretation, not an exact pixel extraction.

The application draws `character.png` at 160 × 148 pixels. The rope endpoint meets the top center of the hair. The character rotates about that top attachment using its alpha-weighted center of mass and moment of inertia. The build embeds `character.png` into the EXE; these files are only needed for development, not alongside the distributed executable.

This is Amiya from Arknights. No redistribution license for the character artwork was provided with the reference. The physics solver's MIT license does not license this artwork. Public release scope remains to be confirmed with the repository owner.
