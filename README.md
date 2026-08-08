# What's an Azmeri Wisp?

> See which Azmeri wisps and possessed rares are juiced -- and how much -- so you know what's worth killing.

Part of the **WhatsA** plugin family for ExileApi.

## What It Does

- Highlights **possessed** rares/uniques, **spirit-touched** monsters, free-roaming **wisps**, and summoned spirit animals in the world and on the minimap, each color-coded.
- Reads a roaming wisp's **empowerment** (the value that scales with touched-monster kills) and shows it live.
- **Carries that empowerment onto the rare a wisp possesses**, so you can tell a juiced possessed target from a worthless one at a glance.
- A clean on-screen **readout panel** lists everything nearby with its empowerment, and flags **high-value** targets in gold.

## Getting Started

1. Download and place in `Plugins/Source/Whats An Azmeri Wisp/`
2. HUD auto-compiles on next launch
3. Enable in plugin list
4. Open the settings and use the **Panel** tab to position the readout where you like

## Settings

| Setting | What it does |
|--------|--------------|
| Draw Distance | How far out entities are detected/drawn |
| World Overlay | Master toggle for in-world circles + labels |
| Show Possessed / Touched / Wisps / Spirit Animals | Per-category overlay toggles |
| Colors | Per-category circle/label colors |
| Circle Radius | Per-category circle size |
| Text Labels / Font Size | In-world label toggle and size |
| Minimap Marks / Mark Size | Crosses on the large map |
| Panel | On-screen readout: enable, counts, per-entity list, show empowerment, max rows, position, scale |
| Empowerment Threshold | Possessed targets at/above this are flagged high-value |
| High-Value Color | Color for high-value targets (default gold) |

<details>
<summary>Technical Details</summary>

ExileCore2 (PoE2) plugin targeting `net8.0-windows`. Classifies entities each Tick by scanning
`EntityListWrapper.ValidEntitiesByType` once, keyed by entity id (wisps hop hosts, producing new
ids). Detection signatures:

- **Possessed**: OMP mods `SpiritOfThe<Animal>Possessed` (+ `SpiritPossessed<Attr>`), buff
  `tormented_spirit_power`. Multi-possession is supported (a host can carry several spirits).
- **Touched**: OMP mods `SpiritOfThe<Animal>Touched`; no possession buff.
- **Free wisp**: path `Metadata/Monsters/TormentedSpirits/TormentedSpiritofthe<Animal><Tier>`
  (no `PossesedDaemon` suffix).
- **Tier** is read from the path / attr suffix, never from the offset `PossessedBy<Tier>Spirit` stat.

**Empowerment** is readable only on the roaming wisp. The empowerment stat (`LightningDamagePctFromRage`,
confirmed shared across animals) is resolved via `empower-stats.json` with a self-calibrating
baseline-diff fallback: the plugin learns a fresh wisp's baseline stat set and reads whichever stat
appears once the wisp is powered, so new animals work without per-animal configuration. When a wisp
possesses a rare, its last empowerment is attributed to that host (matched by the nearest wisp at the
moment of possession, since wisps path to the nearest rare).

The **Debug** settings tab shows a live `key: value (type)` tree of every value being read per tracked
entity, so a broken memory offset is obvious at a glance. Rendering is split from reads (no
`GetComponent` in `Render`); the readout panel and overlays draw from an immutable per-frame snapshot.

</details>

## About This Project

These plugins are built with AI-assisted development using Claude Code and the
ExileApiScaffolding (private development workspace) workspace.

The developer works professionally in cybersecurity and high-risk software --
AI compensates for a C# knowledge gap specifically, not engineering judgment.
Plugin data comes from the PoE Wiki and PoEDB data mining.

The focus is on UX: friction points and missing expected features that the
existing plugin ecosystem doesn't address. Every hour spent developing is an
hour not spent on league progression, so feedback is the best way to support
the project.

## WhatsA Plugin Family

| Plugin | Description |
|--------|-------------|
| [What's a Breakpoint?](https://github.com/ParogDev/WhatsABreakpoint) | Kinetic Fusillade attack speed breakpoint visualizer |
| [What's a Crowd Control?](https://github.com/ParogDev/WhatsACrowdControl) | OmniCC-style CC effect overlay with timers |
| [What's a Mirage?](https://github.com/ParogDev/WhatsAMirage) | League mechanic overlay for spawners, chests, and wishes |
| [What's a Tincture?](https://github.com/ParogDev/WhatsATincture) | Automated tincture management with burn stack tracking |
| [What's a Tooltip?](https://github.com/ParogDev/WhatsATooltip) | Shared rich tooltip service for WhatsA plugins |
| [What's an AI Bridge?](https://github.com/ParogDev/WhatsAnAiBridge) | File-based IPC for AI-assisted plugin development |
| [What's an Unbound Avatar?](https://github.com/ParogDev/WhatsAnUnboundAvatar) | Auto-activation for Avatar of the Wilds at 100 fury |
| **What's an Azmeri Wisp?** | Azmeri wisp / possession tracker with live empowerment readout |

Built with ExileApiScaffolding (private development workspace)

## Operation logic and status

`Tick` classifies valid entities from PoE2 paths, exact mod prefixes, buffs,
rarity, and `empower-stats.json`. It tracks free wisps, touched/possessed
monsters, spirit animals, and the roaming wisp's empowerment, then carries the
last empowerment onto the nearest possessed host for a short TTL. An immutable
snapshot feeds the world overlay, large-map marks, and summary panel; render
does not read components. Area changes/entity removal/hot reload clear state.

Build: **PASS**, classification **CURRENT_WITH_WARNINGS**; the current league's
paths and empowerment behavior need a live capture. See the [central PoE2 report](../../README.md)
and [audit](../../../docs/plugins/WhatsAnAzmeriWisp/AUDIT.md).
