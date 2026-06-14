# Query Catalog -- Full Response Schemas

Complete documentation of every bridge query type, response fields, and usage guidance.

---

## player

Returns player vitals, position, and active buffs.

**Request:** `player`

**Response fields:**
- `player.path` -- metadata path (e.g., `Metadata/Characters/...`)
- `player.hp`, `player.maxHp` -- current and max hit points
- `player.es`, `player.maxEs` -- current and max energy shield
- `player.mana`, `player.maxMana` -- current and max mana
- `player.pos` -- `[gridX, gridY]` grid position
- `player.buffs[]` -- array of buff objects: `{name, displayName, description?, charges, timer, stacks, maxTime, sourceEntityId, sourceSkillId, sourceName?}` (see Buff Object below)
- `player.skills[]` -- array of skill objects: `{id, name, internalName?, canBeUsed, isOnSkillBar, cooldown, isUsing}` (see Skill Object below)

**When to use:** Checking health/mana state, finding active buffs, getting player position for distance calculations. Use skills array to detect granted skills from items/buffs (e.g., HeadHunter stolen mods).

---

## area

Returns current zone information.

**Request:** `area`

**Response fields:**
- `area.name` -- zone display name (e.g., "The Mud Flats")
- `area.areaLevel` -- monster level of the zone
- `area.act` -- act number

**When to use:** Determining what zone the player is in, checking area level for level-gated logic.

---

## entities[:range]

Returns all nearby entities within range (default 200 units).

**Request:** `entities` or `entities:500` (custom range)

**Response fields:** Array of entity objects:
- `id` -- unique entity ID
- `type` -- entity type enum (Monster, WorldItem, Terrain, Effect, etc.)
- `path` -- metadata path (e.g., `Metadata/Monsters/AtlasExiles/AtlasExile1`)
- `name` -- display name from Render component
- `alive` -- boolean
- `hostile` -- boolean
- `rarity` -- Normal/Magic/Rare/Unique
- `dist` -- distance from player in grid units
- `pos` -- `[gridX, gridY]`
- `hp`, `maxHp` -- current and max life

Ordered by distance (closest first).

**When to use:** Surveying what is around the player.

---

## monsters

Returns only alive hostile monsters (subset of entities).

**Request:** `monsters`

**Response:** Same fields as `entities`, filtered to `alive && hostile && type == Monster`.

**When to use:** Combat analysis, threat assessment, boss detection.

---

## items

Returns only world items (drops on the ground).

**Request:** `items`

**Response:** Same fields as `entities`, filtered to `type == WorldItem`.

**When to use:** Checking what loot is on the ground.

---

## npcdialog

Returns NPC dialog state and visible dialog lines.

**Request:** `npcdialog`

**Response fields:**
- `npcDialog.isVisible` -- whether an NPC dialog is open
- `npcDialog.dialogDepth` -- server-side dialog depth counter
- `npcDialog.npcName` -- name of the NPC (when visible)
- `npcDialog.isLoreTalk` -- whether this is a lore conversation
- `npcDialog.lines[]` -- array of dialog line text strings

**When to use:** Detecting NPC interactions, reading quest dialog, automating dialog sequences.

---

## mapdata

Returns map stats, quest flags, and dialog depth.

**Request:** `mapdata`

**Response fields:**
- `mapData.dialogDepth` -- server dialog depth
- `mapData.mapStats` -- dictionary of map stat keys to values (up to 100 entries)
- `mapData.questFlags` -- dictionary of quest flag names to booleans (filtered to Djinn/OrderOfThe/Faridun flags, plus `_total` count)

**When to use:** Checking map modifiers, tracking quest progression, Djinn mechanic state.

---

## ui

Scans all visible UI panels with child text (2 levels deep).

**Request:** `ui`

**Response fields:**
- `ui.dialogDepth` -- server dialog depth
- Named panel visibility booleans: `npcDialog`, `purchaseWindow`, `sellWindow`, `mapDeviceWindow`, `tradeWindow`, `popUpWindow`, `ritualWindow`, `villageRewardWindow`, `mercenaryEncounterWindow`, `zanaMissionChoice`
- `ui.leagueMechanicButtons` -- `{vis, cc}` (visible and child count)
- `ui.visibleChildren[]` -- array of `{i, cc, t?, ct?}` where:
  - `i` = child index
  - `cc` = child count
  - `t` = text if any
  - `ct` = array of child text strings

**When to use:** Detecting which UI windows are open, reading on-screen text, checking panel states.

---

## stash

Returns all stash tabs with metadata.

**Request:** `stash`

**Response fields:** Array of stash tab objects:
- `index` -- raw position in PlayerStashTabs array
- `name` -- display name (includes "(Remove-only)" suffix)
- `type` -- tab type enum (Normal, Premium, Currency, Map, etc.)
- `visibleIndex` -- display order in stash UI (used by Stashie for arrow-key navigation)
- `color` -- `{r, g, b}` tab color
- `isPremium`, `isPublic`, `isRemoveOnly`, `isHidden`, `isMapSeries` -- flag booleans
- `rawFlags` -- raw InventoryTabFlags byte
- `affinity` -- raw InventoryTabAffinity flags uint (0 = no affinity; see `stash-guide.md` for the full enum table)

**When to use:** Enumerating stash tabs, finding specific tab types, checking tab visibility for Stashie configuration.

For stash-specific details (tab type enums, Stashie integration, visibleIndex semantics), see `stash-guide.md`.

---

## deep:Filter[:range]

Deep component dump for entities whose path contains `Filter`.

**Request:** `deep:Strongbox` or `deep:AtlasExile:300`

**Response fields:**
- `filter` -- the filter string used
- `matchCount` -- number of matched entities
- `entities[]` -- array of deeply inspected entities:
  - Basic: `id`, `type`, `path`, `alive`, `hostile`, `rarity`, `dist`, `isValid`
  - `allComponents` -- list of all component type names
  - `render` -- `{name, pos:[x,y,z], bounds:[x,y,z]}`
  - `positioned` -- `{grid:[x,y], reaction, size, scale, rotation}`
  - `animated` -- `{baseEntityPath, baseEntityId}`
  - `stateMachine` -- `{canBeTarget, inTarget, states:{name:value,...}}`
  - `npc` -- `{hasIconOverhead, isIgnoreHidden, isMinMapLabelVisible}`
  - `life` -- `{hp, maxHp, es, maxEs}`
  - `targetable` -- `{isTargetable, isTargeted}`
  - `chest` -- `{isOpened, isLocked, isStrongbox, destroyAfterOpen, isLarge, stompable, openOnDamage}`
  - `omp` -- `{rarity, mods:[]}`
  - `minimapIcon` -- `{name, isVisible, isHide}`
  - `buffs[]` -- buff objects: `{name, displayName, description?, charges, timer, stacks, maxTime, sourceEntityId, sourceName?}`
  - `stats` -- dictionary of stat keys to values (truncated at MaxDeepStats setting)
  - Visual effects: Beam, GroundEffect, EffectPack, AnimationController

**When to use:** Investigating unknown entities, reverse-engineering boss mechanics, understanding entity component structure, finding stat keys for new plugin features.

---

## all

Combined dump: player + area + entities + npcdialog + mapdata.

**Request:** `all`

**Response:** Merges output of `player`, `area`, `entities`, `npcdialog`, and `mapdata` into a single JSON object.

**When to use:** Full game state snapshot. Large response -- prefer focused queries when you know what you need.

---

## playerstats

Full dump of all player GameStat values with no truncation.

**Request:** `playerstats`

**Response fields:**
- `stats` -- dictionary of all `GameStat` enum names to their integer values (can be 500+ entries)

**When to use:** Investigating which stats are active on the player, verifying effects from stolen mods (HeadHunter), or finding stat keys for new plugin features. Prefer `deep:` for general use -- this is for stat-specific investigation.

**Pivot off fragile state tracking:** When code (e.g. a ReAgent script) counts stacks/stages via a packed buff field such as `Buff.FlaskSlot`, treat that as a coincidental signal -- it surfaces first because it is easy to find by eyeballing buff memory, but it breaks when an item/ascendancy changes the encoding (real case: Whirling Slash wind stage read FlaskSlot 2/5/7 normally but 2/8/9 with The Taming ring). Dump `playerstats` and look for a clean mechanic-named counter instead (e.g. `IsWithinAnAlliedWhirlingSlashSandstorm` = 1/2/3), then verify it live at each stage before committing. Note `Buff.FlaskSlot` is a raw Buff field the bridge serializer does not even expose -- only `playerstats` surfaces the stable alternative. In a ReAgent (ExileCore2) script the stable read is `State.Player.Stats[GameStat.<Name>].Value` (returns 0 when absent).

---

## Buff Object Schema

All buff arrays (`player.buffs[]`, entity `buffs[]`, deep entity `buffs[]`) use the same schema:

| Field | Type | Always present | Description |
|-------|------|---------------|-------------|
| `name` | string | yes | Internal buff ID (e.g., `stolen_mods_buff`, `herald_of_ice`) |
| `displayName` | string | yes | Human-readable name (e.g., "Herald of Ice", "Shroud Walker") |
| `description` | string | when non-empty | Buff description text (truncated to 200 chars) |
| `charges` | int | yes | Charge count for charge-stacking buffs |
| `timer` | float | yes | Seconds remaining (999999 for permanent/aura) |
| `stacks` | int | yes | Stack count (e.g., Withered stacks, poison stacks) |
| `maxTime` | float | yes | Total duration when applied (999999 for permanent auras) |
| `sourceEntityId` | int | yes | ID of the entity that applied this buff (0 = self/unknown) |
| `sourceSkillId` | int | yes | ID of the skill that granted this buff (correlate with `skills[].id`) |
| `sourceName` | string | when available | Display name of the source entity (omitted if entity despawned) |

**HeadHunter note:** Stolen mods from Headhunter all share `displayName: "Headhunter"` and the same generic description. To identify specific stolen mods (e.g., Shroud Walker teleport), compare the `skills[]` array with and without HH buffs active -- stolen mods grant additional skills to the player.

---

## Skill Object Schema

The `player.skills[]` array lists all skills available to the player, including skills granted by items, buffs, and stolen mods.

| Field | Type | Always present | Description |
|-------|------|---------------|-------------|
| `id` | int | yes | Skill ID (correlate with `buffs[].sourceSkillId`) |
| `name` | string | yes | Display name (may be empty for granted skills due to Name property bug) |
| `internalName` | string | when available | Internal name via ActiveSkill chain (e.g., "smoke_mine", "flicker_strike") |
| `displayName` | string | when available | Human-readable name via ActiveSkill chain (e.g., "Smoke Mine") |
| `canBeUsed` | bool | yes | Whether the skill can currently be activated |
| `isOnSkillBar` | bool | yes | Whether the skill is assigned to the hotbar |
| `cooldown` | float | yes | Current cooldown in seconds (0 if ready) |
| `isUsing` | bool | yes | Whether the skill is currently being used |
| `isUserSkill` | bool | yes | true = player-socketed gem, false = granted by item/buff/stolen mod |
| `isMine` | bool | yes | true = mine skill (detects Smoke Mine from Shroud Walker) |
| `totalUses` | int | yes | Total times this skill has been used |
| `cost` | int | yes | Mana cost |

**HeadHunter detection:** Shroud Walker (Ambushes) grants a Smoke Mine skill. Detect it proactively by checking for a skill where `internalName: "smoke_mine"` AND `isUserSkill: false` AND `isOnSkillBar: false`. The reactive fallback is watching for the `smoke_mine_movement_speed` buff (appears for 3s after each teleport).
