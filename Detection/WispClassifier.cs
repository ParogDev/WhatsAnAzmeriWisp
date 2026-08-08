using System;
using System.Collections.Generic;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;

namespace WhatsAnAzmeriWisp.Detection;

// Pure classification: read an entity's Path / OMP mods / Buffs / Stats and fill a TrackedWisp.
// No drawing, no per-frame caching beyond what the caller supplies.
public static class WispClassifier
{
    private const string TormentedPrefix = "TormentedSpiritofthe";
    private const string SpiritPrefix = "Spiritofthe";
    private const string DaemonSuffix = "PossesedDaemon"; // engine misspelling (single 's')

    private const string PossessionBuff = "tormented_spirit_power";
    private const string PoweredWispBuff = "spirit_boost_tracker_buff";

    // Cheap pre-filter: is this entity worth a full Populate() this tick?
    public static bool IsRelevant(Entity e)
    {
        var path = e.Path;
        if (path != null && path.Contains("TormentedSpirits", StringComparison.Ordinal))
            return true;
        // Hosts (possessed/touched) are ordinary monsters; OMP mod scan happens in Populate.
        return e.Type == EntityType.Monster;
    }

    public static bool IsDaemon(Entity e)
    {
        var path = e.Path;
        return path != null && path.Contains(DaemonSuffix, StringComparison.Ordinal);
    }

    // Reads a riding daemon's animal/tier + empowerment magnitude (the value scales with touched
    // kills before possession). Empower is only readable for animals present in empower-stats.json.
    public static DaemonInfo ClassifyDaemon(Entity e, EmpowerStatMap map)
    {
        var info = new DaemonInfo { EmpowerLabel = "Empower" };
        WispPathParser.ParseAnimalTierFromPath(e.Path ?? "", out var animal, out var tier);
        info.Animal = animal;
        info.Tier = tier;
        info.IsPowered = HasBuff(e.Buffs, PoweredWispBuff);

        // Daemons carry sparse stats and (live-confirmed) no empowerment, so explicit-only here.
        var reading = map.Read(animal, tier, e.Stats, false);
        if (reading.HasValue)
        {
            info.HasEmpower = true;
            info.EmpowerValue = reading.Value.Value;
            info.EmpowerLabel = reading.Value.Display;
        }
        return info;
    }

    public static void Populate(TrackedWisp rec, Entity e, EmpowerStatMap map,
        Dictionary<long, List<DaemonInfo>> daemonsByCell)
    {
        rec.Possessions.Clear();
        rec.PowerStacks = 0;
        rec.IsPowered = false;
        rec.HasEmpower = false;
        rec.EmpowerValue = 0;
        rec.EmpowerLabel = "";
        rec.PrimaryAnimal = "";
        rec.PrimaryTier = Tier.Unknown;
        rec.Category = WispCategory.None;
        rec.Rarity = MonsterRarity.White;

        rec.WorldPos = e.Pos;
        rec.GridPos = e.GridPos;

        var path = e.Path ?? "";

        // A daemon riding an already-possessed host -- the host itself carries the readable mods,
        // so we do not draw the daemon. Discriminated by the PossesedDaemon suffix.
        if (path.Contains(DaemonSuffix, StringComparison.Ordinal))
            return;

        // Free roaming wisp: TormentedSpiritofthe<Animal><Tier>, no daemon suffix.
        if (path.Contains(TormentedPrefix, StringComparison.Ordinal))
        {
            WispPathParser.ParseAnimalTierFromPath(path, out var animal, out var tier);
            rec.PrimaryAnimal = animal;
            rec.PrimaryTier = tier;
            rec.Category = WispCategory.FreeWisp;

            var powered = HasBuff(e.Buffs, PoweredWispBuff);
            rec.IsPowered = powered;

            var sd = e.Stats;
            if (!powered) map.ObserveBaseline(sd); // learn the fresh-wisp baseline stat set

            var reading = map.Read(animal, tier, sd, powered);
            if (reading.HasValue)
            {
                rec.HasEmpower = true;
                rec.EmpowerValue = reading.Value.Value;
                rec.EmpowerLabel = reading.Value.Display;
            }
            return;
        }

        // Summoned spirit-animal minion: Spiritofthe<Animal> (no Tormented prefix, no tier/daemon).
        if (path.Contains(SpiritPrefix, StringComparison.Ordinal))
        {
            WispPathParser.ParseAnimalTierFromPath(path, out var animal, out _);
            rec.PrimaryAnimal = animal;
            rec.Category = WispCategory.SpiritAnimal;
            return;
        }

        // Possessed / touched host -- scan OMP mods.
        var omp = e.GetComponent<ObjectMagicProperties>();
        if (omp == null) return;
        var mods = omp.Mods;
        if (mods == null || mods.Count == 0)
        {
            // Touched can also be flagged by a stat with no mod parsed yet.
            if (StatFlag(e, "TouchedByPrimalSpirit"))
            {
                rec.Rarity = omp.Rarity;
                rec.Category = WispCategory.Touched;
            }
            return;
        }

        rec.Rarity = omp.Rarity;

        // First pass: collect attribute-derived tiers. In single-spirit cases this pins the tier;
        // in multi-possession we cannot reliably pair attr->animal here, so tier stays Unknown.
        var possTiers = new HashSet<Tier>();
        var touchTiers = new HashSet<Tier>();
        var possAnimals = new List<string>(2);
        var touchAnimals = new List<string>(2);

        for (int i = 0; i < mods.Count; i++)
        {
            var m = mods[i];
            if (m.StartsWith("SpiritPossessed", StringComparison.Ordinal))
            {
                var t = Tiers.AttrToTier(m.Substring("SpiritPossessed".Length));
                if (t != Tier.Unknown) possTiers.Add(t);
            }
            else if (m.StartsWith("SpiritTouched", StringComparison.Ordinal))
            {
                var t = Tiers.AttrToTier(m.Substring("SpiritTouched".Length));
                if (t != Tier.Unknown) touchTiers.Add(t);
            }
        }

        for (int i = 0; i < mods.Count; i++)
        {
            var m = mods[i];
            if (!m.StartsWith("SpiritOfThe", StringComparison.Ordinal)) continue;
            var rest = m.Substring("SpiritOfThe".Length);
            if (rest.EndsWith("Possessed", StringComparison.Ordinal))
                possAnimals.Add(rest.Substring(0, rest.Length - "Possessed".Length));
            else if (rest.EndsWith("Touched", StringComparison.Ordinal))
                touchAnimals.Add(rest.Substring(0, rest.Length - "Touched".Length));
        }

        if (possAnimals.Count > 0)
        {
            rec.Category = WispCategory.Possessed;
            var tier = possTiers.Count == 1 ? FirstOf(possTiers) : Tier.Unknown;
            for (int i = 0; i < possAnimals.Count; i++)
                rec.Possessions.Add(new PossessionInfo(possAnimals[i], tier));
            rec.PowerStacks = CountBuff(e.Buffs, PossessionBuff);
            AttachDaemons(rec, daemonsByCell);
        }
        else if (touchAnimals.Count > 0 || StatFlag(e, "TouchedByPrimalSpirit"))
        {
            rec.Category = WispCategory.Touched;
            var tier = touchTiers.Count == 1 ? FirstOf(touchTiers) : Tier.Unknown;
            if (touchAnimals.Count > 0)
            {
                for (int i = 0; i < touchAnimals.Count; i++)
                    rec.Possessions.Add(new PossessionInfo(touchAnimals[i], tier));
            }
        }

        if (rec.Possessions.Count > 0)
        {
            rec.PrimaryAnimal = rec.Possessions[0].Animal;
            rec.PrimaryTier = rec.Possessions[0].Tier;
        }
    }

    // Pulls the riding daemons' real per-animal tier and empowerment magnitude onto a possessed host.
    private static void AttachDaemons(TrackedWisp rec, Dictionary<long, List<DaemonInfo>> daemonsByCell)
    {
        if (daemonsByCell == null) return;
        if (!daemonsByCell.TryGetValue(DaemonInfo.CellKey(rec.GridPos), out var daemons) || daemons.Count == 0)
            return;

        int total = 0;
        bool any = false;
        string label = "Empower";

        for (int i = 0; i < rec.Possessions.Count; i++)
        {
            var pi = rec.Possessions[i];
            for (int j = 0; j < daemons.Count; j++)
            {
                var dm = daemons[j];
                if (!string.Equals(dm.Animal, pi.Animal, StringComparison.OrdinalIgnoreCase)) continue;
                if (pi.Tier == Tier.Unknown && dm.Tier != Tier.Unknown)
                    rec.Possessions[i] = new PossessionInfo(pi.Animal, dm.Tier);
                if (dm.HasEmpower)
                {
                    total += dm.EmpowerValue;
                    label = dm.EmpowerLabel;
                    any = true;
                }
                break;
            }
        }

        if (any)
        {
            rec.HasEmpower = true;
            rec.EmpowerValue = total;
            rec.EmpowerLabel = label;
        }
    }

    // Parses TormentedSpiritofthe<Animal><Tier> / Spiritofthe<Animal>, stripping any daemon suffix.
    public static void ParseAnimalTierFromPath(string path, out string animal, out Tier tier)
        => WispPathParser.ParseAnimalTierFromPath(path, out animal, out tier);

    private static Tier FirstOf(HashSet<Tier> set)
    {
        foreach (var t in set) return t;
        return Tier.Unknown;
    }

    private static bool HasBuff(List<Buff>? list, string name)
    {
        if (list == null) return false;
        for (int i = 0; i < list.Count; i++)
            if (list[i]?.Name == name) return true;
        return false;
    }

    private static int CountBuff(List<Buff>? list, string name)
    {
        if (list == null) return 0;
        int n = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i]?.Name == name) n++;
        return n;
    }

    private static bool StatFlag(Entity e, string statName)
    {
        if (!Enum.TryParse<GameStat>(statName, out var gs)) return false;
        var sd = e.Stats;
        return sd != null && sd.TryGetValue(gs, out var v) && v > 0;
    }
}
