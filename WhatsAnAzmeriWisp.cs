using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using ExileCore2;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared;
using ExileCore2.Shared.Enums;
using WhatsAnAzmeriWisp.Detection;
using WhatsAnAzmeriWisp.Rendering;

namespace WhatsAnAzmeriWisp;

public class WhatsAnAzmeriWisp : BaseSettingsPlugin<WhatsAnAzmeriWispSettings>
{
    private readonly EmpowerStatMap _empower = new();
    private readonly WorldOverlay _worldOverlay = new();
    private readonly MinimapRenderer _minimapRenderer = new();
    private readonly SummaryPanel _summaryPanel = new();
    private readonly WhatsAnAzmeriWispSettingsUi _settingsUi = new();
    private readonly List<DebugEntry> _debug = new(32);

    // Per-entity-id cache. Re-read each tick; wisps hop hosts so ids are short-lived.
    private readonly Dictionary<uint, TrackedWisp> _tracked = new();
    private readonly HashSet<uint> _seen = new();

    // Riding daemons indexed by grid cell, rebuilt each tick so possessed hosts can pick up
    // their empowerment magnitude + real per-animal tier.
    private readonly Dictionary<long, List<DaemonInfo>> _daemonsByCell = new();

    // Carry-over: empowerment is only readable on a ROAMING wisp. We remember each roaming wisp's
    // last empowerment, and when a rare becomes possessed we attribute the nearest matching wisp's
    // value to it (the wisp pathed to the nearest rare to possess it). Assignment is sticky per host.
    private sealed class WispMemory
    {
        public string Animal = "";
        public Tier Tier;
        public int EmpowerValue;
        public string Label = "Empower";
        public Vector2 GridPos;
        public int Frame;
        public bool Consumed;
    }

    private readonly Dictionary<uint, WispMemory> _wispMemory = new();
    private readonly Dictionary<uint, (int Value, string Label)> _carried = new();
    private int _frame;
    private const int WispMemoryTtlFrames = 600;
    private const float CarryMatchMaxDist = 60f;

    private readonly List<RenderSnapshot> _snapshots = new(64);
    private SummaryCounts _counts;
    private readonly StringBuilder _sb = new(64);

    private bool _canRender;

    public override bool Initialise()
    {
        Name = "Whats An Azmeri Wisp";
        _empower.Load(DirectoryFullName, msg => LogMessage(msg, 5));
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        _tracked.Clear();
        _seen.Clear();
        _daemonsByCell.Clear();
        _wispMemory.Clear();
        _carried.Clear();
        _frame = 0;
        _snapshots.Clear();
        _counts.Reset();
    }

    public override void EntityRemoved(Entity entity)
    {
        if (entity == null) return;
        _tracked.Remove(entity.Id);
        _wispMemory.Remove(entity.Id);
        _carried.Remove(entity.Id);
    }

    public override void Tick()
    {
        _canRender = false;

        if (!Settings.Enable.Value || !GameController.InGame) return;
        var player = GameController.Player;
        if (player == null || !player.IsAlive) return;

        // Grace period guard.
        var pbuffs = player.Buffs;
        if (pbuffs != null)
        {
            for (int i = 0; i < pbuffs.Count; i++)
                if (pbuffs[i]?.Name == "grace_period")
                    return;
        }

        float maxDist = Settings.General.DrawDistance.Value;
        if (!float.IsFinite(maxDist) || maxDist <= 0) return;
        _seen.Clear();
        _daemonsByCell.Clear();

        var byType = GameController.EntityListWrapper.ValidEntitiesByType;

        // Pass A: index riding daemons by grid cell.
        foreach (var kv in byType)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsValid) continue;
                var distance = e.DistancePlayer;
                if (!float.IsFinite(distance) || distance > maxDist) continue;
                if (!WispClassifier.IsDaemon(e)) continue;

                var info = WispClassifier.ClassifyDaemon(e, _empower);
                var key = DaemonInfo.CellKey(e.GridPos);
                if (!_daemonsByCell.TryGetValue(key, out var bucket))
                {
                    bucket = new List<DaemonInfo>(2);
                    _daemonsByCell[key] = bucket;
                }
                bucket.Add(info);
            }
        }

        // Pass B: classify hosts / free wisps / spirit animals (daemons already consumed).
        foreach (var kv in byType)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || !e.IsValid) continue;
                var distance = e.DistancePlayer;
                if (!float.IsFinite(distance) || distance > maxDist) continue;
                if (WispClassifier.IsDaemon(e)) continue;
                if (!WispClassifier.IsRelevant(e)) continue;

                if (!_tracked.TryGetValue(e.Id, out var rec))
                {
                    rec = new TrackedWisp(e);
                    _tracked[e.Id] = rec;
                }

                WispClassifier.Populate(rec, e, _empower, _daemonsByCell);
                if (rec.Category == WispCategory.None)
                {
                    _tracked.Remove(e.Id);
                    continue;
                }
                _seen.Add(e.Id);
            }
        }

        // Prune anything not seen this tick (host hop, left range, died).
        if (_tracked.Count != _seen.Count)
        {
            var stale = _tracked.Keys.Where(k => !_seen.Contains(k)).ToList();
            for (int i = 0; i < stale.Count; i++)
                _tracked.Remove(stale[i]);
        }

        _frame++;
        UpdateCarryOver();
        RebuildSnapshots();
        _canRender = true;
    }

    // Remembers roaming wisp empowerment and attributes it to newly possessed rares.
    private void UpdateCarryOver()
    {
        // 1. Record/refresh roaming wisps that expose an empowerment value.
        foreach (var kv in _tracked)
        {
            var w = kv.Value;
            if (!w.IsValid || w.Category != WispCategory.FreeWisp || !w.HasEmpower) continue;

            if (!_wispMemory.TryGetValue(kv.Key, out var m))
            {
                m = new WispMemory();
                _wispMemory[kv.Key] = m;
            }
            m.Animal = w.PrimaryAnimal;
            m.Tier = w.PrimaryTier;
            m.EmpowerValue = w.EmpowerValue;
            m.Label = w.EmpowerLabel;
            m.GridPos = w.GridPos;
            m.Frame = _frame;
        }

        // 2. Prune stale memory (wisp long gone without being matched).
        if (_wispMemory.Count > 0)
        {
            var expired = _wispMemory.Where(kv => _frame - kv.Value.Frame > WispMemoryTtlFrames)
                .Select(kv => kv.Key).ToList();
            for (int i = 0; i < expired.Count; i++)
                _wispMemory.Remove(expired[i]);
        }

        // 3. Attribute the nearest matching roaming-wisp value to possessed rares that lack one.
        foreach (var kv in _tracked)
        {
            var w = kv.Value;
            if (w.Category != WispCategory.Possessed) continue;
            if (w.HasEmpower) continue;            // daemon already supplied a value
            if (_carried.ContainsKey(kv.Key)) continue; // sticky once assigned

            WispMemory? best = null;
            float bestDist = CarryMatchMaxDist;

            foreach (var mkv in _wispMemory)
            {
                var m = mkv.Value;
                if (m.Consumed) continue;
                // Skip wisps still visibly roaming -- they haven't possessed anything yet.
                if (_tracked.TryGetValue(mkv.Key, out var live) && live.IsValid
                    && live.Category == WispCategory.FreeWisp) continue;
                if (!HostMatchesAnimal(w, m.Animal)) continue;

                float d = Vector2.Distance(m.GridPos, w.GridPos);
                if (!float.IsFinite(d)) continue;
                if (d <= bestDist)
                {
                    bestDist = d;
                    best = m;
                }
            }

            if (best != null)
            {
                _carried[kv.Key] = (best.EmpowerValue, best.Label);
                best.Consumed = true;
                LogMessage("Azmeri Wisp: carried empowerment " + best.EmpowerValue + " (" + best.Animal
                    + ") to possessed host " + kv.Key + " dist " + (int)bestDist, 5);
            }
        }
    }

    private static bool HostMatchesAnimal(TrackedWisp host, string animal)
    {
        for (int i = 0; i < host.Possessions.Count; i++)
            if (string.Equals(host.Possessions[i].Animal, animal, System.StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    private void RebuildSnapshots()
    {
        _snapshots.Clear();
        _counts.Reset();

        foreach (var kv in _tracked)
        {
            var w = kv.Value;
            if (!w.IsValid) continue;

            switch (w.Category)
            {
                case WispCategory.Possessed: _counts.Possessed++; break;
                case WispCategory.Touched: _counts.Touched++; break;
                case WispCategory.FreeWisp: _counts.Wisps++; break;
                case WispCategory.SpiritAnimal: _counts.SpiritAnimals++; break;
            }

            // Effective empowerment: live (wisp/daemon) value, else a carried-over value.
            bool hasEmpower = w.HasEmpower;
            int empowerValue = w.EmpowerValue;
            string empowerLabel = w.EmpowerLabel;
            if (!hasEmpower && w.Category == WispCategory.Possessed
                && _carried.TryGetValue(kv.Key, out var carried))
            {
                hasEmpower = true;
                empowerValue = carried.Value;
                empowerLabel = carried.Label;
            }

            bool highValue = w.Category == WispCategory.Possessed
                && (w.Rarity == MonsterRarity.Unique
                    || (w.Rarity == MonsterRarity.Rare && w.PowerStacks >= 2)
                    || (hasEmpower && empowerValue >= Settings.Power.HighValueThreshold.Value));
            if (highValue) _counts.HighValue++;

            _snapshots.Add(new RenderSnapshot(
                w.Category, w.Rarity, w.WorldPos, w.GridPos,
                BuildLabel(w), BuildShortName(w), hasEmpower, empowerValue,
                highValue, w.IsPowered));
        }
    }

    private string BuildShortName(TrackedWisp w)
    {
        _sb.Clear();
        if (w.Category == WispCategory.Possessed || w.Category == WispCategory.Touched)
            AppendAnimals(w);
        else
            _sb.Append(AnimalTier(w.PrimaryAnimal, w.PrimaryTier));
        return _sb.ToString();
    }

    private string BuildLabel(TrackedWisp w)
    {
        _sb.Clear();
        switch (w.Category)
        {
            case WispCategory.Possessed:
                _sb.Append("POSSESSED: ");
                AppendAnimals(w);
                break;
            case WispCategory.Touched:
                _sb.Append("TOUCHED: ");
                AppendAnimals(w);
                break;
            case WispCategory.FreeWisp:
                _sb.Append("WISP ");
                _sb.Append(AnimalTier(w.PrimaryAnimal, w.PrimaryTier));
                break;
            case WispCategory.SpiritAnimal:
                _sb.Append("Spirit ");
                _sb.Append(w.PrimaryAnimal);
                break;
        }
        return _sb.ToString();
    }

    private void AppendAnimals(TrackedWisp w)
    {
        if (w.Possessions.Count == 0)
        {
            _sb.Append("Spirit");
            return;
        }
        for (int i = 0; i < w.Possessions.Count; i++)
        {
            if (i > 0) _sb.Append(", ");
            var p = w.Possessions[i];
            _sb.Append(AnimalTier(p.Animal, p.Tier));
        }
    }

    private static string AnimalTier(string animal, Tier tier)
    {
        if (string.IsNullOrEmpty(animal)) animal = "Spirit";
        return tier == Tier.Unknown ? animal : animal + "(" + tier + ")";
    }

    public override void Render()
    {
        if (!_canRender || !Settings.Enable.Value || !GameController.InGame) return;

        var ingameUi = GameController.IngameState.IngameUi;
        if (ingameUi.FullscreenPanels.Any(x => x.IsVisible)) return;

        if (Settings.World.Enabled.Value)
            _worldOverlay.Render(Graphics, GameController, Settings, _snapshots);

        if (Settings.Minimap.Enabled.Value)
            _minimapRenderer.Render(Graphics, GameController, Settings, _snapshots);

        if (Settings.Panel.Enabled.Value)
            _summaryPanel.Render(Graphics, Settings, _snapshots, _counts);
    }

    public override void DrawSettings()
    {
        _debug.Clear();
        foreach (var kv in _tracked)
        {
            var w = kv.Value;
            if (!w.IsValid) continue;

            bool carried = _carried.TryGetValue(kv.Key, out var c);
            bool hasEmpower = w.HasEmpower || carried;
            int empowerValue = w.HasEmpower ? w.EmpowerValue : (carried ? c.Value : 0);

            _debug.Add(new DebugEntry
            {
                Id = kv.Key,
                Category = w.Category,
                Animal = w.PrimaryAnimal,
                Tier = w.PrimaryTier,
                Rarity = w.Rarity,
                IsPowered = w.IsPowered,
                HasEmpower = hasEmpower,
                EmpowerValue = empowerValue,
                EmpowerLabel = w.EmpowerLabel,
                PowerStacks = w.PowerStacks,
                Carried = carried,
                Distance = w.Entity?.DistancePlayer ?? 0f,
                PossessionCount = w.Possessions.Count,
            });
        }

        _settingsUi.Draw(Settings, _debug);
    }

    public override void OnPluginDestroyForHotReload()
    {
        ClearState();
        base.OnPluginDestroyForHotReload();
    }

    public override void Dispose()
    {
        ClearState();
        base.Dispose();
    }

    private void ClearState()
    {
        _canRender = false;
        _tracked.Clear();
        _seen.Clear();
        _daemonsByCell.Clear();
        _wispMemory.Clear();
        _carried.Clear();
        _snapshots.Clear();
        _counts.Reset();
    }
}
