using System;
using System.Collections.Generic;
using System.IO;
using ExileCore2.Shared.Enums;
using Newtonsoft.Json;

namespace WhatsAnAzmeriWisp.Detection;

// JSON row: an explicit per-animal/tier empowerment stat override (exact, trusted).
public sealed class EmpowerStatEntry
{
    public string Animal = "";
    public string Tier = "";       // a tier name, or "*" for any tier
    public string StatName = "";   // must match a GameStat enum member name
    public string Display = "Empower";
}

public readonly struct EmpowerReading
{
    public readonly int Value;
    public readonly string Display;

    public EmpowerReading(int value, string display)
    {
        Value = value;
        Display = display;
    }
}

// Resolves a roaming wisp's empowerment magnitude. Two strategies, in order:
//  1. Explicit override from empower-stats.json (exact GameStat per animal/tier).
//  2. Self-calibrating baseline-diff: a fresh wisp has a fixed baseline stat set; a powered wisp
//     gains exactly one extra stat (the empowerment). We learn the baseline from non-powered wisps
//     and read the largest non-baseline stat on a powered one. This works for any animal with no
//     per-animal config. (Live-confirmed 2026-06-14: Owl powered added only LightningDamagePctFromRage.)
public sealed class EmpowerStatMap
{
    private readonly struct Row
    {
        public readonly string Animal;
        public readonly string Tier;
        public readonly GameStat Stat;
        public readonly string Display;

        public Row(string animal, string tier, GameStat stat, string display)
        {
            Animal = animal;
            Tier = tier;
            Stat = stat;
            Display = display;
        }
    }

    private readonly List<Row> _rows = new();
    private readonly HashSet<GameStat> _baseline = new();

    // Stat keys present on a FRESH (non-powered) roaming wisp -- live-captured baseline seed.
    private static readonly string[] BaselineSeed =
    {
        "BaseCannotBeDamaged", "Armour", "MovementVelocityPermyriad",
        "MainHandAttackSpeedPct", "OffHandAttackSpeedPct", "CastSpeedPctForScalingAndDisplay",
        "BaseCannotBeStunned", "DisplayEstimatedPhysicalDamageReductionPct", "IsHiddenMonster",
        "CombinedCastSpeedPct", "CombinedAttackSpeedPct", "CombinedMainHandAttackSpeedPct",
        "CombinedOffHandAttackSpeedPct", "FullManaThreshold",
        "LocalHitsWithThisWeaponFreezeAsThoughDamagePctFinal",
        "DisplayBattlemageCryExertedAttacksTriggerSupportedSpell",
        "HundredTimesReloadsPerSecond", "VirtualRemnantDurationMs",
    };

    public int ResolvedCount => _rows.Count;
    public int TotalCount { get; private set; }

    public void Load(string pluginDirectory, Action<string> log)
    {
        _rows.Clear();
        _baseline.Clear();

        foreach (var name in BaselineSeed)
            if (Enum.TryParse<GameStat>(name, out var gs)) _baseline.Add(gs);

        List<EmpowerStatEntry>? entries = null;
        try
        {
            var path = Path.Combine(pluginDirectory ?? "", "empower-stats.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                entries = JsonConvert.DeserializeObject<List<EmpowerStatEntry>>(json);
            }
        }
        catch (Exception ex)
        {
            log?.Invoke("Azmeri Wisp: failed to read empower-stats.json: " + ex.Message);
        }

        if (entries == null || entries.Count == 0)
        {
            entries = new List<EmpowerStatEntry>
            {
                new EmpowerStatEntry { Animal = "Owl", Tier = "Primal", StatName = "LightningDamagePctFromRage", Display = "Empower" },
            };
        }

        TotalCount = entries.Count;
        foreach (var e in entries)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.StatName)) continue;
            if (Enum.TryParse<GameStat>(e.StatName, out var stat))
            {
                _rows.Add(new Row(e.Animal ?? "", string.IsNullOrEmpty(e.Tier) ? "*" : e.Tier, stat,
                    string.IsNullOrEmpty(e.Display) ? "Empower" : e.Display));
            }
            else
            {
                log?.Invoke("Azmeri Wisp: empower stat '" + e.StatName + "' is not a GameStat member, skipping.");
            }
        }

        log?.Invoke("Azmeri Wisp: empower map " + _rows.Count + "/" + TotalCount
            + " explicit rows, " + _baseline.Count + " baseline stats (auto-detect on for other animals).");
    }

    // Learn the baseline stat keys from a fresh (non-powered) roaming wisp.
    public void ObserveBaseline(IReadOnlyDictionary<GameStat, int> stats)
    {
        if (stats == null) return;
        foreach (var kv in stats) _baseline.Add(kv.Key);
    }

    // Reads empowerment. powered=true enables the generic baseline-diff fallback (free wisps only).
    public EmpowerReading? Read(string animal, Tier tier, IReadOnlyDictionary<GameStat, int> stats, bool powered)
    {
        if (stats == null) return null;

        // 1. Explicit override.
        var tierName = tier.ToString();
        for (int i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r.Animal != "*" && !string.Equals(r.Animal, animal, StringComparison.OrdinalIgnoreCase)) continue;
            if (r.Tier != "*" && !string.Equals(r.Tier, tierName, StringComparison.OrdinalIgnoreCase)) continue;
            if (stats.TryGetValue(r.Stat, out var v)) return new EmpowerReading(v, r.Display);
        }

        // 2. Generic baseline-diff: largest stat key not seen on a fresh wisp.
        if (!powered) return null;
        int best = 0;
        bool found = false;
        foreach (var kv in stats)
        {
            if (_baseline.Contains(kv.Key)) continue;
            if (!found || kv.Value > best)
            {
                best = kv.Value;
                found = true;
            }
        }
        return found ? new EmpowerReading(best, "Empower") : (EmpowerReading?)null;
    }
}
