using System.Numerics;
using ExileCore2.Shared.Enums;
using WhatsAnAzmeriWisp.Detection;

namespace WhatsAnAzmeriWisp.Rendering;

// Immutable per-frame draw data. Built in Tick from TrackedWisp; renderers never touch components.
public readonly struct RenderSnapshot
{
    public readonly WispCategory Category;
    public readonly MonsterRarity Rarity;
    public readonly Vector3 WorldPos;
    public readonly Vector2 GridPos;
    public readonly string Label;      // full world label e.g. "POSSESSED: Owl(Primal), Stag(Vivid)"
    public readonly string ShortName;  // compact animals e.g. "Bear(Wild)" for the panel
    public readonly bool HasEmpower;
    public readonly int EmpowerValue;
    public readonly bool IsHighValue;
    public readonly bool IsPowered;

    public RenderSnapshot(WispCategory category, MonsterRarity rarity, Vector3 worldPos,
        Vector2 gridPos, string label, string shortName, bool hasEmpower, int empowerValue,
        bool isHighValue, bool isPowered)
    {
        Category = category;
        Rarity = rarity;
        WorldPos = worldPos;
        GridPos = gridPos;
        Label = label;
        ShortName = shortName;
        HasEmpower = hasEmpower;
        EmpowerValue = empowerValue;
        IsHighValue = isHighValue;
        IsPowered = isPowered;
    }
}

// Per-frame tallies for the panel header.
public struct SummaryCounts
{
    public int Possessed;
    public int Touched;
    public int Wisps;
    public int SpiritAnimals;
    public int HighValue;

    public void Reset()
    {
        Possessed = 0;
        Touched = 0;
        Wisps = 0;
        SpiritAnimals = 0;
        HighValue = 0;
    }
}

// Live read-state for one tracked entity, surfaced in the Debug tab so a broken memory offset
// (garbage value / missing field) is visible at a glance.
public sealed class DebugEntry
{
    public uint Id;
    public WispCategory Category;
    public string Animal = "";
    public Tier Tier;
    public MonsterRarity Rarity;
    public bool IsPowered;
    public bool HasEmpower;
    public int EmpowerValue;
    public string EmpowerLabel = "";
    public int PowerStacks;
    public bool Carried;
    public float Distance;
    public int PossessionCount;
}
