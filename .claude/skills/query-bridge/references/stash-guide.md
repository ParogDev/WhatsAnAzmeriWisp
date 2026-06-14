# Stash Tab Guide

Stash-specific knowledge for working with the bridge's `stash` query and Stashie plugin configuration.

## Tab Type Enum

The `type` field in stash tab responses maps to the `InventoryTabType` enum:

| Value | Type |
|-------|------|
| 0 | Normal |
| 1 | Premium |
| 3 | Currency |
| 4 | Unique |
| 5 | Map |
| 6 | Divination |
| 7 | Quad |
| 8 | Essence |
| 9 | Fragment |
| 12 | Delve |
| 13 | Blight |
| 14 | Metamorph |
| 15 | Delirium |
| 16 | Folder |
| 17 | Flask |
| 18 | Gem |

Some types may appear as raw numbers if they are newer than the enum definition (e.g., type "19").

## visibleIndex vs index

- **index** -- raw position in the `PlayerStashTabs` array. Stable identifier but does not reflect display order.
- **visibleIndex** -- display order in the stash UI. This is what the player sees and what Stashie uses for arrow-key navigation.

Hidden tabs and folder tabs may have different index/visibleIndex relationships. Always use `visibleIndex` when referring to tab position in user-facing output.

## How Stashie Uses Stash Data

### AllStashNames

Stashie builds its tab name list from `StashElement.AllStashNames`, which returns tabs in display order. This list is what populates Stashie's dropdown menus and filter configuration.

### Stashie Index

The "Stashie Index" for a tab is its position in the display-ordered list (i.e., its `visibleIndex`). Stashie uses this for arrow-key navigation: it calculates how many times to press left/right arrow to reach the target tab from the current position.

### Duplicate Name Handling

When multiple tabs share the same name, Stashie appends a `(N)` suffix to disambiguate. For example, if three tabs are named "Dump":
- First (by visibleIndex): "Dump"
- Second: "Dump(1)"
- Third: "Dump(2)"

This suffix is added by Stashie internally and does not appear in the bridge response. When configuring Stashie rules that target specific tabs, account for this naming convention.

## Affinity Flags

Stash tabs can have item affinities set (Currency, Map, etc.) that auto-sort items. The `ServerStashTab.Affinity` property returns an `InventoryTabAffinity` flags enum:

| Bit | Flag |
|-----|------|
| 1 << 1 | Incubator |
| 1 << 3 | Currency |
| 1 << 4 | Unique |
| 1 << 5 | Map |
| 1 << 6 | DivinationCard |
| 1 << 7 | Settlers |
| 1 << 8 | Essence |
| 1 << 9 | Fragment |
| 1 << 10 | Sanctum |
| 1 << 12 | Delve |
| 1 << 13 | Blight |
| 1 << 14 | Ultimatum |
| 1 << 15 | Delirium |
| 1 << 17 | Flask |
| 1 << 18 | Gem |
| 1 << 21 | Ritual |

This is a `[Flags]` enum — a tab can have multiple affinities set. Check with bitwise AND:

```csharp
var tab = serverData.PlayerStashTabs[i];
bool hasCurrencyAffinity = (tab.Affinity & InventoryTabAffinity.Currency) != 0;
bool hasAnyAffinity = tab.Affinity != 0;
```

Some bits (0, 2, 11, 19, 20) are defined but unnamed — these may correspond to newer league content.

The `rawFlags` field in the bridge response contains the raw `InventoryTabFlags` byte, which encodes additional tab metadata (premium, public, remove-only, hidden, map-series).

## HUD Config Overwrite Warning

The HUD saves all plugin settings (including Stashie config) to disk on shutdown. **Never edit Stashie's settings JSON while the HUD is running** -- it will overwrite your changes on close. Before editing any Stashie config file:

1. Ask the user to close the HUD first
2. Make your edits
3. Tell the user they can restart the HUD

## Presenting Stash Data

When showing stash tab data to the user:
- Sort by `visibleIndex` (display order)
- Always show `visibleIndex` prominently -- this is the number users care about
- Flag notable properties: remove-only, public (trade-listed), hidden
- Format as a table for readability
- Tab colors are RGB 0-255 values

### Filtering Options

Support these presentation modes based on user request:
- **All tabs**: Show everything sorted by visibleIndex
- **Find by name**: Case-insensitive substring match on tab name
- **Filter by type**: Match on tab type (e.g., "Currency", "Quad")
- **Public tabs**: Only tabs where `isPublic` is true (trade-listed)
- **Raw JSON**: Return the unformatted response
