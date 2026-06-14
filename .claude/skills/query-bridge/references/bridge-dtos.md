# Bridge DTO Schema Reference

Source: `Plugins/Whats An AI Bridge/BridgeDtos.cs`

All bridge responses use DTO classes serialized via `JsonConvert.SerializeObject()`. Null fields are omitted from output.

## BridgeResponse (root)

All query responses return this envelope. Only requested sections are populated.

| Field | Type | When present |
|-------|------|-------------|
| `player` | PlayerDto | query=all, player |
| `stats` | Dict<string,int> | query=playerstats |
| `buffProbe` | BuffProbeDto[] | query=buffprobe |
| `area` | AreaDto | query=all, area |
| `npcDialog` | NpcDialogDto | query=all, npcdialog |
| `mapData` | MapDataDto | query=all, mapdata |
| `ui` | UiDto | query=ui |
| `stashTabs` | StashTabDto[] | query=stash |
| `entities` | EntityDto[] | query=all, entities, monsters, items |
| `filter` | string | query=deep:* |
| `matchCount` | int | query=deep:* |
| `query` | string | always |
| `timestamp` | string (ISO 8601) | always |

## PlayerDto

| Field | Type | Notes |
|-------|------|-------|
| `path` | string | Entity path |
| `hp`, `maxHp` | int | Current/max HP |
| `es`, `maxEs` | int | Current/max ES |
| `mana`, `maxMana` | int | Current/max mana |
| `pos` | float[2] | Grid position [x, y] |
| `rotation` | float? | Only in snapshot/recording |
| `buffs` | BuffDto[]? | Active buffs |
| `skills` | SkillDto[]? | Actor skills |
| `actor` | ActorDto? | Only in snapshot/recording |

## BuffDto

| Field | Type |
|-------|------|
| `name` | string |
| `charges` | int |
| `timer` | float |
| `displayName` | string |
| `description` | string? (max 200 chars) |
| `stacks` | int |
| `maxTime` | float |
| `sourceEntityId` | uint |
| `sourceSkillId` | int |
| `sourceName` | string? |

## SkillDto

| Field | Type |
|-------|------|
| `id` | int |
| `name` | string |
| `internalName` | string? |
| `displayName` | string? |
| `canBeUsed` | bool |
| `isOnSkillBar` | bool |
| `cooldown` | float |
| `isUsing` | bool |
| `isUserSkill` | bool? |
| `isMine` | bool? |
| `totalUses` | int? |
| `cost` | int? |

## ActorDto

| Field | Type |
|-------|------|
| `actionId` | int |
| `action` | string |
| `animationId` | int |
| `animation` | string |
| `isMoving` | bool |
| `isAttacking` | bool |
| `currentAction` | CurrentActionDto? |

### CurrentActionDto

| Field | Type |
|-------|------|
| `skill` | string? |
| `destination` | int[2] |
| `targetId` | uint? |

## AreaDto

| Field | Type |
|-------|------|
| `name` | string? |
| `areaLevel` | int |
| `act` | int |

## NpcDialogDto

| Field | Type |
|-------|------|
| `isVisible` | bool |
| `dialogDepth` | int |
| `npcName` | string? |
| `isLoreTalk` | bool? |
| `lines` | string[]? |
| `error` | string? |

## MapDataDto

| Field | Type |
|-------|------|
| `dialogDepth` | int |
| `mapStats` | Dict<string,int>? |
| `questFlags` | QuestFlagsDto? |
| `error` | string? |

### QuestFlagsDto

Dynamic keys for quest flags matching Djinn/OrderOfThe/Faridun, plus `_total` count.

## UiDto

| Field | Type |
|-------|------|
| `dialogDepth` | int |
| `npcDialog` | bool |
| `purchaseWindow` | bool |
| `sellWindow` | bool |
| `mapDeviceWindow` | bool |
| `tradeWindow` | bool |
| `popUpWindow` | bool |
| `ritualWindow` | bool |
| `villageRewardWindow` | bool |
| `mercenaryEncounterWindow` | bool |
| `zanaMissionChoice` | bool |
| `leagueMechanicButtons` | {vis, cc}? |
| `visibleChildren` | UiChildDto[] |

### UiChildDto

| Field | Type |
|-------|------|
| `i` | int (index) |
| `cc` | int (child count) |
| `t` | string? (text, max 80) |
| `ct` | string[]? (child texts, max 60 each) |

## StashTabDto

| Field | Type |
|-------|------|
| `index` | int |
| `name` | string |
| `type` | string (InventoryTabType) |
| `visibleIndex` | int |
| `color` | {r, g, b} |
| `isPremium` | bool |
| `isPublic` | bool |
| `isRemoveOnly` | bool |
| `isHidden` | bool |
| `isMapSeries` | bool |
| `rawFlags` | byte |
| `affinity` | uint |

## EntityDto

Shallow entities have base fields only. Deep entities (`deep: true`) add component sub-objects.

### Base fields (always present)

| Field | Type |
|-------|------|
| `id` | uint |
| `type` | string (EntityType) |
| `path` | string |
| `name` | string? |
| `alive` | bool |
| `hostile` | bool |
| `rarity` | string (MonsterRarity) |
| `dist` | float |
| `pos` | float[2] |
| `hp`, `maxHp` | int |

### Deep fields (when `deep: true`)

| Field | Type |
|-------|------|
| `isValid` | bool? |
| `allComponents` | string[]? |
| `render` | {name?, pos[3], bounds[3]}? |
| `positioned` | {grid[2], reaction, size, scale, rotation, travelProgress}? |
| `animated` | {baseEntityPath, baseEntityId}? |
| `stateMachine` | {canBeTarget?, inTarget?, states{}}? |
| `npc` | {hasIconOverhead, isIgnoreHidden, isMinMapLabelVisible}? |
| `life` | {hp, maxHp, es, maxEs}? |
| `targetable` | {isTargetable, isTargeted}? |
| `chest` | {isOpened, isLocked, isStrongbox, ...}? |
| `omp` | {rarity, mods[]?}? |
| `minimapIcon` | {name, isVisible, isHide}? |
| `buffs` | BuffDto[]? |
| `stats` | {values..., _truncated?}? |
| `actor` | ActorDto? |

### Effect components (on any entity)

| Field | Type |
|-------|------|
| `beam` | {start[3], end[3]}? |
| `groundEffect` | {duration, maxDuration, scale, sizeIncrease}? |
| `hasEffectPack` | bool? |
| `animController` | {animId, stage, progress, speed}? |

## SnapshotResponse (recording frames)

| Field | Type |
|-------|------|
| `frame` | int |
| `timestamp` | string (ISO 8601) |
| `elapsedMs` | double |
| `area` | AreaDto |
| `player` | PlayerDto (with actor + rotation) |
| `entities` | EntityDto[] (auto-deep for elites/effects) |
| `_end` | true |

## BuffProbeDto (reverse-engineering)

| Field | Type |
|-------|------|
| `name` | string |
| `timer` | float |
| `address` | string (hex) |
| `raw` | Dict<offset, hex value> |
| `sv80` | {first, last, dataSize, dataInts[]?, statPairs[]?}? |
| `treeNode0` | Dict<offset, int>? |
| `treeNode1` | Dict<offset, int>? |
