# Whats An Azmeri Wisp

ExileCore2 (PoE2) plugin that detects and tracks Azmeri Wisp / Tormented Spirit content and surfaces each wisp's empowerment.

## Plugin Context

Classifies nearby entities every Tick into Possessed rares/uniques, spirit-Touched monsters,
free-roaming Wisps, and summoned Spirit Animals, and draws color-coded world circles + minimap
marks. It reads a roaming wisp's empowerment (the value that scales with touched-monster kills) and
carries that value onto the rare the wisp possesses, so the player can tell juiced targets from
worthless ones. Empowerment and counts are shown in a branded on-screen readout panel; a tabbed
settings UI (Overlay / Panel / Power / Debug) exposes all options plus a live key/value/type debug
tree of what is being read.

## ExileCore2 vs the reference below

This is an **ExileCore2** plugin. The API reference further down is written for ExileCore (PoE1) and
is mostly accurate, but note these EC2 differences that this plugin actually uses:

- `.csproj`: `<TargetFramework>net8.0-windows</TargetFramework>`, references `$(exilecore2Package)\ExileCore2.dll`
  and `GameOffsets2.dll` (NOT `$(exapiPackage)`/`ExileCore.dll`). The EC2 host (halp2) runs on .NET 8 -
  a net10 plugin will not load (`ExileCore2.runtimeconfig.json` -> `tfm: net8.0`).
- Namespaces are `ExileCore2.*` / `GameOffsets2`. Aliases used: `Color = System.Drawing.Color`,
  `RectangleF = ExileCore2.Shared.RectangleF`, `Vector2N = System.Numerics.Vector2`.
- `Tick()` returns `void` (not `Job`).
- `Entity` exposes much directly: `Pos` (Vector3 world), `GridPos` (Vector2), `Buffs` (List<Buff>),
  `Stats` (Dictionary<GameStat,int>), `Rarity` (MonsterRarity), `Path`/`Metadata`, `DistancePlayer`.
- `Graphics.DrawBox(RectangleF, Color)` / `DrawFrame(RectangleF, Color, int)` take a RectangleF.
- Minimap: `Graphics.GridToMap(grid, grid, VisibleSubMap.Large)` -> screen point, then draw in screen
  space (`DrawPolyLine`). `DrawLineOnLargeMap` is obsolete in EC2.
- ASCII only in `.cs` string literals (Unicode renders as `?` on the HUD).
- DTO + Newtonsoft `JsonConvert` for any serialized data (e.g. `empower-stats.json`); never manual JSON.

## Architecture

| File | Purpose |
|------|---------|
| `WhatsAnAzmeriWisp.cs` | Main plugin -- Tick: a 20 Hz scan (one gather pass on cached path/type, then index daemons, then classify) and per-tick position refresh; carry-over, snapshots, lifecycle, DrawSettings |
| `WhatsAnAzmeriWispSettings.cs` | ISettings + [Submenu] groups (General/World/Minimap/Panel/Power) |
| `WhatsAnAzmeriWispSettingsUi.cs` | Tabbed ImGui settings UI (Overlay/Panel/Power/Debug), wisp-teal branding |
| `empower-stats.json` | Per-animal/tier empowerment stat map (explicit overrides; auto-detect is the fallback) |
| `Detection/WispCategory.cs` | enum None/Possessed/Touched/FreeWisp/SpiritAnimal |
| `Detection/Tier.cs` | Tier enum + tier-suffix list + AttrToTier (Dex->Vivid, Int->Primal, Str->Wild) |
| `Detection/PossessionInfo.cs` | one animal spirit affecting a host (multi-possession) |
| `Detection/TrackedWisp.cs` | mutable per-entity-id cache record |
| `Detection/DaemonInfo.cs` | riding-daemon info indexed by grid cell (tier + empowerment) |
| `Detection/EmpowerStatMap.cs` | loads empower-stats.json; self-calibrating baseline-diff empowerment reader |
| `Detection/WispClassifier.cs` | pure classification: Path/OMP mods/Buffs/Stats -> TrackedWisp |
| `Rendering/RenderSnapshot.cs` | immutable per-frame draw data + SummaryCounts + DebugEntry |
| `Rendering/WorldOverlay.cs` | world circles + labels (no empowerment text here) |
| `Rendering/MinimapRenderer.cs` | large-map cross marks via GridToMap |
| `Rendering/SummaryPanel.cs` | branded on-screen readout panel (this is where empowerment shows) |

## Lifecycle Methods

| Method | Used for |
|--------|----------|
| `Initialise` | Set Name; load `empower-stats.json` via EmpowerStatMap |
| `Tick` | Two-pass scan of ValidEntitiesByType: index daemons, classify hosts/wisps; carry-over; build snapshots |
| `Render` | Occlusion guard; draw world overlay, minimap, readout panel |
| `AreaChange` | Clear all caches (tracked, daemons, wisp memory, carried) |
| `EntityRemoved` | Drop entity from caches |
| `DrawSettings` | Build live debug entries; draw the tabbed settings UI |

## Detection Signatures

- **Possessed** (rare/unique): OMP mods `SpiritOfThe<Animal>Possessed` (+ `SpiritPossessed<Attr>`),
  buff `tormented_spirit_power`. Multi-possession supported (collect all `SpiritOfThe*Possessed` mods).
- **Touched** (normal/magic): OMP mods `SpiritOfThe<Animal>Touched`; no possession buff.
- **Free wisp**: path `Metadata/Monsters/TormentedSpirits/TormentedSpiritofthe<Animal><Tier>` with
  NO `PossesedDaemon` suffix (engine misspelling, single 's').
- **Riding daemon**: same path + `PossesedDaemon` -- consumed into the per-cell daemon index, not drawn.
- **Tier**: read from the path or the `<Attr>` suffix. NEVER from `PossessedBy<Tier>Spirit` /
  `TouchedBy<Tier>Spirit` stats -- those names are offset from the real tier (boolean flags only).

## Empowerment

Empowerment is readable ONLY on a roaming wisp. The stat is `LightningDamagePctFromRage` (confirmed
shared by Owl and Stag). `EmpowerStatMap` resolves it via explicit JSON rows first, then a
self-calibrating baseline-diff: it learns a fresh (non-powered) wisp's baseline stat set and reads
whichever stat appears once the wisp is powered (`spirit_boost_tracker_buff`). New animals therefore
work with no per-animal config. When a wisp possesses a rare, its last empowerment is carried onto
that host, matched by the nearest wisp at the moment of possession (wisps path to the nearest rare).

## Settings (snapshot)

| Group | Setting | Type | Default |
|-------|---------|------|---------|
| (root) | Enable | ToggleNode | true |
| General | DrawDistance | RangeNode<int> | 120 (20-400) |
| World | Enabled | ToggleNode | true |
| World | ShowPossessed / ShowTouched / ShowWisp | ToggleNode | true |
| World | ShowSpiritAnimal | ToggleNode | false |
| World | PossessedColor / TouchedColor / WispColor / SpiritAnimalColor | ColorNode | per-category |
| World | PossessedRadius / TouchedRadius / WispRadius | RangeNode<int> | 70 / 50 / 50 |
| World | ShowLabel | ToggleNode | true |
| World | FontSize | RangeNode<int> | 16 (8-32) |
| Minimap | Enabled | ToggleNode | true |
| Minimap | Radius | RangeNode<float> | 8 (2-30) |
| Panel | Enabled | ToggleNode | true |
| Panel | X / Y | RangeNode<int> | 500 / 140 |
| Panel | Scale | RangeNode<float> | 1.0 (0.6-2.0) |
| Panel | ShowCounts / ShowList / ShowEmpower | ToggleNode | true |
| Panel | MaxRows | RangeNode<int> | 12 (1-40) |
| Power | HighValueThreshold | RangeNode<int> | 10000 (0-100000) |
| Power | HighValueColor | ColorNode | gold |

---

## Plugin Anatomy

Every plugin is a C# class library. The main class inherits `BaseSettingsPlugin<TSettings>` and the settings class implements `ISettings`.

Minimal `.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <OutputType>Library</OutputType>
    <UseWindowsForms>true</UseWindowsForms>
    <PlatformTarget>x64</PlatformTarget>
    <LangVersion>latest</LangVersion>
    <DebugType>embedded</DebugType>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="ExileCore">
      <HintPath>$(exapiPackage)\ExileCore.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="GameOffsets">
      <HintPath>$(exapiPackage)\GameOffsets.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="ImGui.NET" Version="1.90.0.1" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />
  </ItemGroup>
</Project>
```

### Plugin Lifecycle

| Method | When called | Notes |
|---|---|---|
| `Initialise()` | Once on load | Register hotkeys, wire up `OnPressed`/`OnValueChanged`, return `true` on success |
| `OnLoad()` | After Initialise | Load textures: `Graphics.InitImage("file.png")` |
| `AreaChange(AreaInstance area)` | Zone change | Clear cached entity lists here |
| `Tick()` | Every frame | Return `null` (no async job needed) or a `Job` for background work |
| `Render()` | Every frame | Draw overlays; check `Settings.Enable` and `GameController.InGame` |
| `EntityAdded(Entity entity)` | Entity enters range | Filter and cache relevant entities here |
| `EntityRemoved(Entity entity)` | Entity leaves range | Remove from caches |
| `DrawSettings()` | Settings panel open | Call `base.DrawSettings()` unless fully custom |

## GameController API

`GameController` is the main access point available in all plugin methods:

```csharp
// State checks
GameController.InGame
GameController.Game.IsInGameState

// Player
GameController.Player                          // local player Entity
GameController.Player.GetComponent<Positioned>().GridPosNum

// Area
GameController.Area.CurrentArea.IsPeaceful
GameController.Area.CurrentArea.Area.RawName
GameController.Area.CurrentArea.IsTown
GameController.Area.CurrentArea.IsHideout

// Entities -- prefer ValidEntitiesByType over Entities for filtered access
GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
GameController.Entities   // all entities, more expensive

// Ingame UI / state
GameController.Game.IngameState.IngameUi.Map.LargeMap.IsVisible
GameController.Game.IngameState.IngameUi.FullscreenPanels
GameController.Game.IngameState.IngameUi.OpenRightPanel
GameController.Game.IngameState.Camera        // Camera, WorldToScreen
GameController.Game.IngameState.Data          // terrain, server data, area dimensions
GameController.Game.IngameState.Data.RawPathfindingData     // int[][] walkability grid
GameController.Game.IngameState.Data.RawTerrainHeightData   // float[][] terrain height
GameController.Game.IngameState.Data.IsInsideAzmeriZone     // bool, Azmeri zone detection

// Game files (static data)
GameController.Files.BaseItemTypes.Translate(entity.Path)
GameController.Files.GemEffects.GetById(id)

// Window
GameController.Window.GetWindowRectangleTimeCache   // cached, use this not GetWindowRectangle()

// Inter-plugin
GameController.PluginBridge.SaveMethod("MyPlugin.Method", delegate)
GameController.PluginBridge.GetMethod<TDelegate>("OtherPlugin.Method")

// Timing
GameController.DeltaTime  // double, seconds since last frame
```

## Entity API

```csharp
entity.Path         // "Metadata/Monsters/..."
entity.Metadata     // same as Path for most entities
entity.Id           // uint, unique per session
entity.IsValid      // always check before extensive use
entity.IsAlive
entity.GridPosNum   // Vector2, 2D grid position
entity.PosNum       // Vector3, 3D world position
entity.Distance(otherEntity)
entity.DistancePlayer  // float, distance to local player
entity.Type         // EntityType enum
entity.GetComponent<Positioned>()     // null if not present
entity.GetComponent<Render>()
entity.GetComponent<Stats>()
entity.GetComponent<ObjectMagicProperties>()?.Mods
```

> EC2 note: this plugin reads `entity.Pos` / `entity.GridPos` / `entity.Buffs` / `entity.Stats` /
> `entity.Rarity` directly off the Entity rather than via `GetComponent`, and `GridPosNum`/`PosNum`
> do not exist on the EC2 Entity.

## Component Quick Reference

### Life
```csharp
var life = entity.GetComponent<Life>();
life.CurHP / life.MaxHP      // health
life.CurMana / life.MaxMana  // mana
life.CurES / life.MaxES      // energy shield
life.HPPercentage             // 0-100
```

### Buffs
```csharp
var buffs = entity.GetComponent<Buffs>();
buffs.BuffsList               // List<Buff>
buff.Name                     // internal string ID (e.g., "frozen", "shocked")
buff.DisplayName              // human-readable
buff.Timer                    // seconds remaining
buff.MaxTime                  // original duration
buff.Charges                  // stack count
```

### Positioned & Render
```csharp
var pos = entity.GetComponent<Positioned>();
pos.GridPosNum                // Vector2 grid coords

var render = entity.GetComponent<Render>();
render.Name                   // display name
render.Bounds                 // bounding box
```

### ObjectMagicProperties
```csharp
var omp = entity.GetComponent<ObjectMagicProperties>();
omp.Rarity                    // MonsterRarity enum
omp.Mods                      // List<string> mod names
```

### Stats
```csharp
var stats = entity.GetComponent<Stats>();
stats.StatDictionary          // Dictionary<GameStat, int>
```

### Other Common Components
- `Monster` -- monster-specific data
- `Player` -- player-specific data
- `Chest` -- chest state (IsOpened, IsStrongbox)
- `Portal` -- portal destination
- `WorldItem` -- ground item info
- `Targetable` -- whether entity can be targeted (isTargetable)
- `Base` -- item base type info (Name, ItemBaseName)
- `Mods` -- item mods (ItemMods, ImplicitMods, ItemRarity). Also exposes human-readable stat text:
  - `HumanStats` (List\<string\>) -- explicit stat lines as displayed in-game
  - `HumanCraftedStats` -- crafted stat lines
  - `HumanImpStats` -- implicit stat lines
  - `FracturedStats` -- fractured stat lines
  - `EnchantedStats` -- enchanted stat lines
  - `CrucibleStats` -- crucible stat lines
- `MapKey` -- map item data. `MapKey.Tier` (byte) returns map tier directly. `MapKey.Info` returns `MapKeyDat`. Replaces the old `Map` component.
- `AltarEntity` -- altar objects. Access altar mods via `TopUpsides`, `TopDownsides`, `BottomUpsides`, `BottomDownsides` (each `List<AltarMod>`). Each `AltarMod` has `.Mod` (ModRecord), `.StatValues` (List\<int\>).
- `Sockets` -- socket links, colors, count

### Inventory Helpers
```csharp
// SubInventories -- for complex stash tab types (Currency, Essence, Fragment, etc.)
var subInvs = stashInventory.SubInventories;  // List<Inventory>
// Each sub-inventory represents a compartment within a specialty stash tab.
```

## Settings Node Types

```csharp
public class MySettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public RangeNode<int> SomeRange { get; set; } = new RangeNode<int>(10, 1, 100);
    public RangeNode<float> SomeFloat { get; set; } = new RangeNode<float>(1.5f, 0f, 10f);
    public ColorNode SomeColor { get; set; } = new ColorNode(Color.White);
    public HotkeyNodeV2 SomeKey { get; set; } = Keys.F5;
    // Methods:
    //   .IsPressed()    -> bool, true while key is held down
    //   .PressedOnce()  -> bool, true only on first frame of press (edge-triggered)
    //   .Value          -> Keys enum value
    // Wire in Initialise():
    //   Input.RegisterKey(Settings.SomeKey.Value);
    //   Settings.SomeKey.OnValueChanged += () => Input.RegisterKey(Settings.SomeKey.Value);
    public ButtonNode SomeButton { get; set; }   // wire OnPressed in constructor
    public TextNode SomeText { get; set; } = new TextNode("default");
    public EmptyNode SomeHeader { get; set; }     // visual separator in settings

    [Menu("Display Name")]
    public ToggleNode WithLabel { get; set; } = new ToggleNode(true);

    [Menu("Category Header", parentIndex: 100)]
    public EmptyNode CategoryNode { get; set; }

    [Menu("Sub Option", parentIndex: 101, index: 100)]
    public ToggleNode SubOption { get; set; } = new ToggleNode(false);

    [IgnoreMenu]
    public SomeType NotShownInSettings { get; set; }
}

[Submenu]
public class NestedSection
{
    public ToggleNode SubOption { get; set; } = new ToggleNode(false);
}
```

Register hotkeys in `Initialise()`:
```csharp
Input.RegisterKey(Settings.SomeKey.Value);
Settings.SomeKey.OnValueChanged += () => Input.RegisterKey(Settings.SomeKey.Value);
```

## Graphics API

```csharp
// Rectangles -- use System.Numerics.Vector2 for positions
Graphics.DrawBox(topLeft, bottomRight, color, rounding);
Graphics.DrawFrame(topLeft, bottomRight, color, borderWidth, segments, rounding);

// Text
Graphics.DrawText("text", position, color);
Graphics.DrawTextWithBackground("text", position, textColor, alignment, bgColor);
var size = Graphics.MeasureText("text");
using (Graphics.SetTextScale(1.5f)) { /* draw at scale */ }

// World / map lines
Graphics.DrawLineInWorld(gridPos1, gridPos2, width, color);
Graphics.DrawLineOnLargeMap(gridPos1, gridPos2, width, color);

// Circles
Graphics.DrawFilledCircleInWorld(worldPos, radius, color);

// Textures (load in OnLoad, draw in Render)
Graphics.InitImage("myimage.png");            // from plugin directory
Graphics.InitImage("key", fullPath);          // with explicit path
Graphics.DrawImage("key", rectF, color);      // RectangleF from SharpDX
var texId = Graphics.GetTextureId("key");     // IntPtr for ImGui
Graphics.HasImage("key");                     // check if loaded

// Camera projection
var screenPos = GameController.Game.IngameState.Camera.WorldToScreen(entity.PosNum);
```

> EC2 note: `DrawBox`/`DrawFrame` here take a `RectangleF` + `Color` (+ int thickness for DrawFrame).
> For minimap, use `Graphics.GridToMap(grid, grid, VisibleSubMap.Large)` then screen-space draws;
> `DrawLineOnLargeMap` is obsolete.

## Performance Rules

ExileApi plugins run every frame (60+ fps). The game process is memory-mapped, so every property access that isn't cached may read from game memory.

- **NEVER** call `GetComponent<T>()` in `Render()` -- do it in `Tick()`, store in fields
- **NEVER** iterate `GameController.Entities` -- use `EntityListWrapper.ValidEntitiesByType`
- **NEVER** call `entity.Path` per-frame -- cache in `EntityAdded()` via `SetHudComponent`
- Use `EntityAdded`/`EntityRemoved` to maintain filtered entity lists
- Use `TimeCache<T>` for expensive operations that don't need per-frame freshness
- Clear state in `AreaChange()`, not per-tick
- Use cached accessors (`GetWindowRectangleTimeCache`, `GetClientRectCache`)
- Check `entity.IsValid` and `entity.IsAlive` before processing
- Check `screenPos == Vector2.Zero` after `WorldToScreen()`
- Load textures in `OnLoad()`, never in `Render()`
- Separate data reads (`Tick`) from drawing (`Render`) -- never GetComponent in Render
- Use `_canRender` flags to skip `Render()` entirely when nothing to draw
- Guard early in `Tick()`: InGame, IsAlive, area checks (town/hideout) before work

## Mandatory Safety Guards

Every plugin MUST implement these guards. They are not optional.

### Spawn Immunity (Grace Period)

No action should be taken during spawn immunity that would cause it to end. Skip all input sending, processing, and rendering during grace period:

```csharp
// In Tick(), after getting buffs list:
for (int i = 0; i < buffs.Count; i++)
{
    if (buffs[i].Name == "grace_period")
        return;
}
```

The buff name is `"grace_period"`. This covers the intangibility window after entering a zone. Any plugin that sends inputs (key presses, mouse clicks), triggers abilities, or renders overlays MUST check this. Sending input during grace period breaks spawn immunity and can kill the player.

### Input Safety (CanSendInput Pattern)

Any plugin that sends synthetic key presses MUST guard against sending input when the game cannot properly receive it. Use this pattern:

```csharp
private bool CanSendInput()
{
    try
    {
        if (!GameController.Window.IsForeground())
            return false;

        var ingameUi = GameController.IngameState.IngameUi;

        // Chat open -- keystrokes would type into chat
        if (ingameUi.ChatPanel?.ChatTitlePanel?.IsVisible == true)
            return false;

        // Fullscreen panels (skill tree, atlas, syndicate board)
        if (ingameUi.FullscreenPanels.Any(x => x.IsVisible))
            return false;

        // Large panels (vendor, trade, map device, crafting bench)
        if (ingameUi.LargePanels.Any(x => x.IsVisible))
            return false;

        return true;
    }
    catch
    {
        return false;
    }
}
```

Always check `CanSendInput()` before any `Input.KeyPress` coroutine. Combine with a `DateTime _lastKeyPressAt` field and a configurable cooldown (e.g., `ToggleCooldownMs`) to prevent rapid-fire key spam.

### UI Panel Occlusion

No rendering should be done when a UI window is open that the overlay would interfere with. If the plugin cannot avoid rendering where the UI panel is displayed, skip rendering entirely:

```csharp
// In Render(), early guard:
var ingameUi = GameController.Game.IngameState.IngameUi;
if (ingameUi.FullscreenPanels.Any(x => x.IsVisible)) return;
if (ingameUi.LargePanels.Any(x => x.IsVisible)) return;
```

For plugins that render at fixed screen positions, also consider side panels:
```csharp
if (ingameUi.OpenLeftPanel.IsVisible) return;   // inventory etc.
if (ingameUi.OpenRightPanel.IsVisible) return;  // stash, maps etc.
```

Requires `using System.Linq;` for `.Any()`.

Panel types:
- **FullscreenPanels**: Skill tree, Atlas tree, Syndicate board -- cover entire screen
- **LargePanels**: Vendor, Trade, Map Device, Crafting Bench, Ritual -- large modal windows
- **OpenLeftPanel / OpenRightPanel**: Inventory, Stash -- side panels

## C# Best Practices for Plugin Development

- Use file-scoped namespaces
- Seal leaf classes for virtual dispatch optimization
- Object-pool reusable instances to avoid per-frame GC pressure
- Use Dictionary/HashSet lookups instead of per-frame string comparisons
- Use switch expressions for tier/type mappings
- Use readonly fields for immutable configuration
- Any plugin with 4+ settings should use a dedicated `<PluginName>SettingsUi` class

## Coordinate Systems

- **Grid** (`GridPosNum`, `Vector2`): tile-based map coordinates. Used for minimap drawing.
- **World** (`PosNum`, `Vector3`): 3D world coordinates. Convert to screen with `Camera.WorldToScreen`.
- **Screen** (`Vector2`): pixel coordinates. Used for ImGui and `Graphics.DrawText/DrawFrame`.
- GridToWorld multiplier: `250f / 23f`
