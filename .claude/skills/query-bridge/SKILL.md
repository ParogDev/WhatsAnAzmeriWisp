---
name: query-bridge
description: >
  Query live game state from ExileApi via the AI Bridge plugin. Use whenever the user
  asks about their character (health, buffs, position), the area they're in, nearby
  entities/monsters/items, stash tabs, NPC dialogs, map stats, or UI panels. Also handles
  gameplay recording and playback. Trigger on: "what's my health", "what buffs do I have",
  "what's around me", "show my stash tabs", "start recording", "what dropped", or any
  request to inspect in-game state.
user_invocable: true
argument_description: A query type (player, stash, entities, etc.), natural-language question, or "help"
---

# Query Game State via AI Bridge

Query ANY live game data through the Whats An AI Bridge plugin's file-based IPC.

## Prerequisites

The **Whats An AI Bridge** plugin must be enabled in the HUD. It polls `request.txt` every 250ms and writes results to `response.json`. If the response file is missing or stale, ask the user to confirm the plugin is enabled.

## IPC Protocol

| Step | Action |
|------|--------|
| 1 | Write the query string to `claude-bridge\request.txt` |
| 2 | Say: "Querying game state -- hold still for a moment." |
| 3 | Read `claude-bridge\response.json` |
| 4 | Say: "Got it -- you can keep playing." |
| 5 | Parse and present the data |

The bridge deletes `request.txt` after reading it. A stale `request.txt` means the bridge is not running. All responses are UTF-8 JSON with `"query"` and `"timestamp"` fields.

## Natural-Language Routing

| User asks... | Query string |
|-------------|-------------|
| health, mana, ES, vitals | `player` |
| buffs, debuffs, charges | `player` |
| where am I, what zone | `area` |
| what's around me, nearby | `entities` |
| monsters, enemies, threats | `monsters` |
| drops, loot, items on ground | `items` |
| NPC talking, dialog | `npcdialog` |
| map mods, map stats | `mapdata` |
| quest flags, quest progress | `mapdata` |
| UI windows, panels open | `ui` |
| stash tabs, tab layout | `stash` |
| tell me about [entity] | `deep:<filter>` (ask for identifying path/name) |
| all player stats, stat investigation | `playerstats` |
| everything, full snapshot | `all` (full player + area + entities + NPC + map) |
| start recording | `record:start` |
| stop recording | `record:stop` |
| take a snapshot | `snapshot` (saves to file, returns metadata) |
| list recordings | `recording:list` |
| load recording [file] | `recording:load:<filename>` |
| show frame N | `recording:frame:<N>` |
| search recording for X | `recording:search:<term>` |
| recording summary | `recording:summary` |

If the user provides a known query type directly (e.g., "player", "stash", "deep:Strongbox"), use it as-is.

## Execution Steps

1. **Route**: Determine the query string from `$ARGUMENTS` or the user's natural-language request using the routing table above
2. **Write**: Write the query string to `claude-bridge\request.txt`
3. **Notify**: Tell the user to hold still briefly
4. **Read**: Read `claude-bridge\response.json`
5. **Validate**: If the file is missing or the timestamp is much older than now, the bridge may not be running. Ask: "Is the Whats An AI Bridge plugin enabled in the HUD?"
6. **Present**: Parse and present findings. Summarize key points rather than dumping raw JSON. Format tables for tabular data (stash tabs, entity lists)
7. **Resume**: Tell the user they can keep playing

## Supported Query Types (Quick Reference)

| Query | Returns |
|-------|---------|
| `player` | HP, ES, mana, position, active buffs |
| `area` | Zone name, area level, act |
| `entities[:range]` | All nearby entities (default 200 units) |
| `monsters` | Alive hostile monsters only |
| `items` | World items (ground drops) only |
| `npcdialog` | NPC dialog state and visible lines |
| `mapdata` | Map stats, quest flags, dialog depth |
| `ui` | Visible UI panels with child text |
| `stash` | All stash tabs with metadata |
| `deep:<filter>[:range]` | Deep component dump for matching entities |
| `playerstats` | Full player GameStat dump (no truncation, 500+ stats) |
| `all` | Combined player + area + entities + npcdialog + mapdata |

For detailed response schemas and field documentation, read `references/query-catalog.md`.
For stash-specific knowledge (tab types, Stashie integration, visibleIndex), read `references/stash-guide.md`.
For recording/playback commands, read `references/recording.md`.

## Handling "help"

If the user asks "help" or wants to know what queries are available, list all query types from the table above with one-line descriptions.

## Handling Unsupported Queries

If the user asks for data the bridge does not support (e.g., inventory contents, skill tree, passive points, equipped item mods):

1. Explain that this query type is not currently implemented
2. Describe what ExileApi data source would provide it (e.g., `GameController.IngameState.Data.ServerData.PlayerInventories`)
3. Outline what changes to `WhatsAnAiBridge.cs` would be needed (new query keyword, new Write method)
4. Mention that an eval system for arbitrary ExileApi queries is planned (see `references/eval-system-brief.md`)

## Important Notes

- The bridge runs in the plugin Render loop -- the HUD must be running and the plugin enabled
- Queries are point-in-time snapshots -- data changes constantly
- Entity positions, HP, and buffs are volatile
- Never send synthetic inputs -- all interaction is file-based (reading/writing bridge files only)
- For deep queries, provide a specific path filter to avoid massive responses
- The `all` query produces large responses; prefer focused queries when possible
- Recording JSONL files in `claude-bridge/recordings/` can get large at high capture rates

$ARGUMENTS
