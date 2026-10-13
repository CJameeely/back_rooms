# Third-party assets

Models in `Models/` are from Kenney.nl, licensed **CC0 (public domain)** — no attribution
legally required, credited here anyway per the author's request:

- [Furniture Kit](https://opengameart.org/content/furniture-kit) — `floorFull.fbx`, `desk.fbx`,
  `chairDesk.fbx`, `sideTableDrawers.fbx`
- [Space Kit](https://opengameart.org/content/space-kit) — `metalStructure.fbx`, `console.fbx`

Original model textures/materials were not imported; these models are re-materialed in code
(`WhiteBoxModules.cs` / `GameplayBuilder.cs`) using the project's own white-box material set so
colors stay consistent with the rest of the level (amber = interactive, ice blue = clue, etc).

Textures in `Textures/` are tiling grid/panel maps applied as `_BaseMap` (tinted per-material via
`_BaseColor`) so surfaces read as machined panels instead of flat solid color:

- `Grid_Fine_Dark.png` is from Kenney's [Prototype Textures](https://opengameart.org/content/prototype-textures)
  pack, **CC0**. Used (tinted amber/blue) for the interactive and clue materials.
- `Grid_Fine.png` / `Grid_Panel.png` are generated in-house (plain white cell + gray grid line).
  Kenney's own light-on-light variant had too little contrast (~4/255) to read once lit and
  mipmapped at a distance — these use a wider delta (~70/255) so the grid stays visible on pale
  surfaces without darkening the overall tint.
