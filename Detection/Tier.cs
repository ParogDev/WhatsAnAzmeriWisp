using System;

namespace WhatsAnAzmeriWisp.Detection;

// Wisp display tier. Read from the entity PATH or the SpiritPossessed/Touched<Attr> suffix.
// NEVER read it from the PossessedBy<Tier>Spirit / TouchedBy<Tier>Spirit stat names -- those are
// offset from the real tier and must be treated as boolean flags only.
public enum Tier
{
    Unknown,
    Vivid,
    Primal,
    Wild,
    Sacred,
}

public static class Tiers
{
    // Order matters for path-suffix matching. Add new tiers here as they are observed.
    public static readonly string[] Names = { "Vivid", "Primal", "Wild", "Sacred" };

    // Stat-group attribute suffix -> tier. Confirmed live: Dex=Vivid, Int=Primal. Str=Wild expected.
    public static Tier AttrToTier(string attr)
    {
        switch (attr)
        {
            case "Dex": return Tier.Vivid;
            case "Int": return Tier.Primal;
            case "Str": return Tier.Wild;
            default: return Tier.Unknown;
        }
    }

    public static Tier FromName(string name)
    {
        return Enum.TryParse<Tier>(name, out var t) ? t : Tier.Unknown;
    }
}
