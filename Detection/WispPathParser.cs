using System;

namespace WhatsAnAzmeriWisp.Detection;

/// <summary>Pure PoE2 Azmeri path parsing kept separate from ExileCore2 entity access.</summary>
public static class WispPathParser
{
    private const string TormentedPrefix = "TormentedSpiritofthe";
    private const string SpiritPrefix = "Spiritofthe";
    private const string DaemonSuffix = "PossesedDaemon"; // engine misspelling (single 's')

    public static void ParseAnimalTierFromPath(string path, out string animal, out Tier tier)
    {
        animal = "";
        tier = Tier.Unknown;
        if (string.IsNullOrEmpty(path)) return;

        int i = path.IndexOf(TormentedPrefix, StringComparison.Ordinal);
        int prefixLen = TormentedPrefix.Length;
        if (i < 0)
        {
            i = path.IndexOf(SpiritPrefix, StringComparison.Ordinal);
            prefixLen = SpiritPrefix.Length;
        }
        if (i < 0) return;

        var tail = path.Substring(i + prefixLen);
        int at = tail.IndexOf('@');
        if (at >= 0) tail = tail.Substring(0, at);
        if (tail.EndsWith(DaemonSuffix, StringComparison.Ordinal))
            tail = tail.Substring(0, tail.Length - DaemonSuffix.Length);

        for (int t = 0; t < Tiers.Names.Length; t++)
        {
            var name = Tiers.Names[t];
            if (tail.EndsWith(name, StringComparison.Ordinal))
            {
                tier = Tiers.FromName(name);
                animal = tail.Substring(0, tail.Length - name.Length);
                return;
            }
        }

        animal = tail;
    }
}
