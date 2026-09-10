# Quark: Declared architecture

The checkable contract for this repo's structure. `conform` reads this file
and reports divergence; it never decides. Deep rationale lives in the Bible
(`docs/res/design.html`, `docs/res/style.html`); this file states the rules,
the Bible explains them. Code may run ahead on `genesis`: when a divergence
is deliberate, update this file in the same change.

## Layers (`Assets/Scripts/`, one-way dependency, top to bottom)

- `0 - ROOT`: static support, depends on nothing. Pieces: `Anima`, `Palette`,
  `Quantum`, `SpritePalette`, and `Charm/` (editor skin, with its `Editor/`).
- `1 - CORE`: abstract foundations. Domains: `Identity/`, `Composition/`,
  `Management/`.
- `2 - EXTENSIONS`: concrete gameplay, derives from CORE. Domains today:
  `Characters/`, `Props/`, `UI/`, `Testing/`.

A new domain is a subfolder inside its layer, never a new layer. Tiebreak:
fundamental and Unity-only goes to CORE; optional or externally dependent
goes to EXTENSIONS; when in doubt, EXTENSIONS (promoting is cheap, demoting
breaks).

## Composition model

- Bases: `Entity` (character), `Prop` (scene object with behavior), `Service`
  (bodiless system). Everything else is behavior composed onto one of them.
- Hierarchy: `Identifiable` is the root of `Entity` and `Prop`;
  `Service → GameManager → Atom` (default implementation).
- Pieces: `Addon<T>` (serialized behavior inside its owner, may nest),
  `Mod<T>` (scene component the owner detects in its hierarchy),
  `Nucleus<T>` (resolves addons, mods and lifecycle), `Values` (shared
  per-owner state).
- Lifecycle is owner-driven through Nucleus: `Awake → Hook` (list order,
  addons then mods), `Update → Handle` (only `Enabled`), `OnDestroy → Unhook`
  (reverse). Subclasses that need `Awake`/`OnDestroy` override and call base.

## Placement conventions

- A parent addon `X.cs` keeps its nested addons in a sibling folder `X/`
  (e.g. `Movement.cs` + `Movement/`).
- Non-MonoBehaviour support types live in a `Classes/` subfolder of their
  domain. Composed behaviors live in `Addons/`, scene modifiers in `Mods/`.
- `Editor/` folders (any depth) are editor-only and excluded from build.
- `Assets/Plugins/`: imported third-party packages (TextMesh Pro).
- `Assets/Resources/`: name-loadable assets only.
- `Assets/_/`: non-script assets (`Audio/`, `Fonts/`, `Library/` for
  integrated third-party assets, `Materials/`, `Settings/`, `Sources/`).
- `Source/` at repo root: raw material Unity must not import.

## Script layout

Regions, fixed order, empty ones omitted: `FIELDS` / `LIFETIME` / `API` /
`MISC`. Blank line after `#region` and before `#endregion`. Non-serialized
variables next to their consumer, not in a top block. Nested types at the
end of the file.

## Branches

`genesis` is dev, `release` is stable (tags `v0.x.y`). Commits by the author
only, via GitHub Desktop.

## Known divergences (deliberate or pending)

- No `.asmdef` per layer yet: layer boundaries are convention, packaging is
  roadmap.
- `Charm` is listed as a CORE domain in the Bible but lives in `0 - ROOT`;
  `CharmEditor` sits in `1 - CORE/Composition/Editor/`. Pending unification.
- A stray `CLASSES` region exists in `Management/Addons/Audio.cs` and
  `Controls.cs`; the declared set has no such region.
