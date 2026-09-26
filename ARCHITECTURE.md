# Quark: Declared architecture

The checkable contract for this repo's structure. `conform` reads this file
and reports divergence; it never decides. Deep rationale lives in the documentation
(`docs/res/design.html`, `docs/res/style.html`); this file states the rules,
the documentation explains them. Code may run ahead on `genesis`: when a divergence
is deliberate, update this file in the same change.

## Layers (`Assets/Scripts/`, one-way dependency, top to bottom)

- `0 - ROOT`: static support, depends on nothing. Pieces: `Anima`, `Palette`,
  `Quantum`, `SpritePalette`, and `Charm/` (editor skin, with its `Editor/`).
- `1 - CORE`: abstract foundations. Domains: `Identity/`, `Composition/`,
  `Management/`.
- `2 - EXTENSIONS`: concrete gameplay, derives from CORE. Domains today:
  `Characters/`, `Props/`, `UI/`, `Interaction/`, `Hub/`.

A new domain is a subfolder inside its layer, never a new layer. Tiebreak:
fundamental and Unity-only goes to CORE; optional or externally dependent
goes to EXTENSIONS; when in doubt, EXTENSIONS (promoting is cheap, demoting
breaks).

## Composition model

- Bases: `Entity` (character), `Prop` (scene object with behavior), `Service`
  (bodiless system). Everything else is behavior composed onto one of them.
- Hierarchy: `Host` exposes the existing per-owner `Values`.
  `Host → Identifiable → Entity / Prop`;
  `Host → Service → GameManager → Atom` (default implementation).
- Pieces: `Addon<T>` (serialized behavior inside its owner, may nest),
  `Mod<T>` (scene component the owner detects in its hierarchy),
  `Nucleus<T>` (resolves addons, mods and lifecycle), `Values` (shared
  per-owner state).
- Lifecycle is owner-driven through Nucleus: `Awake → Hook` (list order,
  addons then mods), `Update → Handle` (only `Enabled`), `OnDestroy → Unhook`
  (forward: addons, then mods). Subclasses that need `Awake`/`OnDestroy` override and call base.

## Interaction contracts

- `Prop` stays abstract; `Interactable` is the concrete scene object (formerly
  `Item`, with the same script GUID). `Physical` and `Motion` are independent
  `Mod<Prop>` components; `Lock` is an optional `Addon<Prop>`.
- `Context` is a retained reference with immutable `Host Source/Target` and
  lazy typed data. Input reports Primary/Secondary and Press/Release. Use is
  a separate request. Result events get their own context and strength 0..1.
- `Values` uses feature-owned `Key<T>` identities and retains its typed cells.
  Two keys of the same payload type are distinct. `Key<T>.Default` is the
  single-service registration token; `GameManager.Find<T>()` uses it.
- `Entity.Birth/Death(Context)` dispatch explicit gameplay events independently
  of Unity creation/destruction.
- Primary grabs/releases. Secondary throws held free bodies. Use opens at
  progress <= 0.5 and closes above it. A successful grab cancels Motion;
  Use during a grab is ignored. Released joints hold their coordinate, with
  optional near-closed snap. Obstruction cancels movement without setting Lock.
- Hinge and slider bounds are authored on the joint: minimum closed, maximum
  open. Sliders use both signed ends of their symmetric linear limit. Lock
  accepts only a closed mechanism and restores the authored limits on unlock.
- Motion tweens Transform-only or authored kinematic targets; free dynamic
  bodies use Physical. Tween collision geometry uses primitives or convex
  meshes, including compound children, with swept checks before each move.
- Motor settings are cached before driving. Direct sibling references select
  one writer and one movement observer. Both motors guard custom Enabled and
  Unity activation; their local physics cleanup does not change Nucleus.
- `Hub : Service` lives outside removable Interaction. Each zone pairs a root
  with a generic MonoBehaviour requirement. Missing dependencies hide zones;
  present disabled components preserve the authored state. Scene composition,
  resources and missing serialized reference cleanup belong to the author.

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
- `2 - EXTENSIONS/Hub/` owns its scripts and the resources actually used by
  its zones. No generated map or empty asset catalogue.
- `Source/` at repo root: raw material Unity must not import.

## Script layout

Regions, fixed order, empty ones omitted: `FIELDS` / `LIFETIME` / `API` /
`MISC`. Blank line after `#region` and before `#endregion`. Non-serialized
variables next to their consumer, not in a top block. Nested types at the
end of the file. New scripts use one-word names.

## Branches

`genesis` is dev, `release` is stable (tags `v0.x.y`). Commits by the author
only, via GitHub Desktop.

## Known divergences (deliberate or pending)

- No `.asmdef` per layer yet: layer boundaries are convention, packaging is
  roadmap.
- `Charm` is listed as a CORE domain in the documentation but lives in `0 - ROOT`;
  `CharmEditor` sits in `1 - CORE/Composition/Editor/`. Pending unification.
- A stray `CLASSES` region exists in `Management/Addons/Audio.cs` and
  `Controls.cs`; the declared set has no such region.
