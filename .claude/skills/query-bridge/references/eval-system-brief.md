# Planning Brief: Arbitrary ExileApi Code Execution via CLI

Paste this into a fresh conversation to plan and implement an eval system for the AI Bridge plugin.

---

## Problem Statement

The Whats An AI Bridge plugin currently supports a fixed set of query types (`player`, `area`, `entities`, `stash`, `ui`, `mapdata`, `npcdialog`, `deep:<filter>`, recording commands). Each new data need requires modifying the plugin's `ProcessQuery` method to add a new keyword and serialization logic.

**Goal:** Accept arbitrary dotted-path expressions (or C# expressions) that walk the ExileApi object graph at runtime, enabling queries for ANY data the HUD can access without modifying plugin code.

## Two Approaches

### Approach 1: Reflection Property Walker (Recommended First Step)

Parse a dotted path string, walk properties via reflection, serialize the result.

**Example queries:**
```
eval:GameController.IngameState.Data.ServerData.PlayerStashTabs[0].Name
eval:GameController.Player.GetComponent<Life>().CurHP
eval:GameController.IngameState.IngameUi.NpcDialog.IsVisible
eval:GameController.IngameState.Data.ServerData.DialogDepth
```

**Implementation outline:**
1. Split path on `.` (handling `[]` indexing and `<>` generics)
2. Start from `GameController` (the plugin has access)
3. For each segment: use `Type.GetProperty()` / `Type.GetField()` to resolve
4. Special handling for `GetComponent<T>()` -- resolve `T` from ExileCore assemblies
5. Support array/list indexing: `[0]`, `[N]`
6. Support dictionary lookup: `["key"]`
7. Serialize the final value to JSON (primitives, or shallow object dump)

**Supported operations:**
- Property access: `obj.PropertyName`
- Field access: `obj.FieldName`
- Array/list indexing: `collection[0]`
- Dictionary lookup: `dict["key"]` or `dict[enumValue]`
- Generic method call: `GetComponent<Life>()` (restricted to known safe methods)

**Pros:** Simple, tight security boundary, covers ~90% of use cases
**Cons:** No filtering, no LINQ, no computed expressions

### Approach 2: Roslyn Scripting Engine (More Powerful)

Use `Microsoft.CodeAnalysis.CSharp.Scripting` to evaluate arbitrary C# expressions:

```
eval:GameController.Player.GetComponent<Buffs>().BuffsList.Where(b => b.Name.Contains("charge")).Select(b => new { b.Name, b.BuffCharges })
```

**Pros:** Full C# expression power, LINQ support
**Cons:** Large dependency (~20MB+), broader security surface, compilation overhead

## Security Constraints

Any eval system MUST enforce:
- **Read-only:** No property setters, no method calls with side effects
- **No private members:** Only public properties and fields
- **No arbitrary code execution:** Whitelist of allowed method calls (GetComponent, LINQ queries)
- **Timeout:** Kill evaluation after N milliseconds to prevent infinite loops
- **Namespace restriction:** Only ExileCore types, no System.IO / System.Net / System.Diagnostics
- **No file I/O or network access** from evaluated expressions

## What Exists Today

- **Bridge plugin:** `Plugins/Whats An AI Bridge/WhatsAnAiBridge.cs` -- has `ProcessQuery` method that routes query strings to handler methods
- **IPC protocol:** File-based (request.txt -> response.json), already handles arbitrary string queries
- **GameController access:** Plugin inherits from `BaseSettingsPlugin<>`, has `GameController` property
- **CLI skill:** `.claude/skills/query-bridge/` -- routes natural-language queries to bridge query strings

## Key Files to Read

1. `Plugins/Whats An AI Bridge/WhatsAnAiBridge.cs` -- current query routing and serialization
2. The ExileApi source (read-only reference, path in CLAUDE.md) for understanding the object graph:
   - `GameController.cs` -- root object
   - `IngameState.cs` -- game state tree
   - `ServerData.cs` -- server-side data (inventories, stash tabs, etc.)
   - `Entity.cs` and component classes -- entity data model

## Open Questions for Planning Session

1. **Approach:** Start with reflection walker, or go straight to Roslyn?
2. **Depth control:** How deep should automatic serialization go? (Shallow object dump vs recursive?)
3. **Caching:** Should resolved reflection paths be cached for repeated queries?
4. **Error reporting:** How to report type resolution failures, null references, invalid paths?
5. **Component discovery:** Should there be a `list-components` or `describe-type` meta-query?
6. **LINQ later:** If starting with reflection, design the path syntax to be forward-compatible with Roslyn upgrade?
7. **Testing:** How to test without a running game instance? Mock GameController?
