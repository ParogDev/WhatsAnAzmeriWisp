using System.Collections.Generic;
using System.Numerics;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;

namespace WhatsAnAzmeriWisp.Detection;

// Mutable per-entity-id cache record. Dynamic fields are re-read every Tick (wisps change state
// and hop hosts -- a hop produces a NEW entity id).
public sealed class TrackedWisp
{
    public Entity Entity;
    public WispCategory Category;
    public MonsterRarity Rarity;

    // All animal spirits on a host (possessed or touched). Free wisp uses PrimaryAnimal/PrimaryTier.
    public readonly List<PossessionInfo> Possessions = new(2);

    public int PowerStacks;      // number of tormented_spirit_power buff instances on a host
    public bool IsPowered;       // free wisp has spirit_boost_tracker_buff
    public bool HasEmpower;      // empower stat resolved + present on a free wisp
    public int EmpowerValue;     // raw empower stat magnitude
    public string EmpowerLabel = "";

    public Vector3 WorldPos;
    public Vector2 GridPos;

    public string PrimaryAnimal = "";
    public Tier PrimaryTier;

    public TrackedWisp(Entity entity)
    {
        Entity = entity;
    }

    public bool IsValid => Entity?.IsValid == true && Entity.IsAlive;
}
