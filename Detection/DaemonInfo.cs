using System;
using System.Numerics;

namespace WhatsAnAzmeriWisp.Detection;

// A possession daemon (the wisp's form while riding a host). Collected each tick and indexed by
// grid cell so a possessed host can pick up the empowerment magnitude + real per-animal tier of
// the spirit(s) riding it. The empowerment value (atlas "empowerment") scales with touched
// monsters slain before possession.
public struct DaemonInfo
{
    public string Animal;
    public Tier Tier;
    public bool HasEmpower;
    public int EmpowerValue;
    public string EmpowerLabel;
    public bool IsPowered;

    // Packs a grid position into a stable cell key. Daemon and host share the same grid position.
    public static long CellKey(Vector2 g)
    {
        long x = (int)MathF.Round(g.X);
        long y = (int)MathF.Round(g.Y);
        return (x << 32) ^ (y & 0xffffffffL);
    }
}
