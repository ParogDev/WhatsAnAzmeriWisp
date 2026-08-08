using System;
using WhatsAnAzmeriWisp.Detection;

var tests = new (string Name, Action Body)[]
{
    ("tormented path parses animal and tier", TormentedPathParsesAnimalAndTier),
    ("spirit path strips daemon suffix and instance id", SpiritPathStripsDaemonSuffixAndInstanceId),
    ("unknown path fails closed", UnknownPathFailsClosed),
    ("attribute mapping stays PoE2-specific", AttributeMappingStaysPoe2Specific),
};

var failures = 0;
foreach (var (name, body) in tests)
{
    try
    {
        body();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failures != 0)
    throw new InvalidOperationException($"{failures} Azmeri Wisp fixture test(s) failed.");

Console.WriteLine($"{tests.Length} Azmeri Wisp fixture tests passed.");

static void TormentedPathParsesAnimalAndTier()
{
    WispPathParser.ParseAnimalTierFromPath("Metadata/Monsters/TormentedSpiritoftheWolfVivid@79", out var animal, out var tier);
    Assert(animal == "Wolf", "animal");
    Assert(tier == Tier.Vivid, "tier");
}

static void SpiritPathStripsDaemonSuffixAndInstanceId()
{
    WispPathParser.ParseAnimalTierFromPath("Metadata/Monsters/SpiritoftheStagPossesedDaemon@12", out var animal, out var tier);
    Assert(animal == "Stag", "daemon animal");
    Assert(tier == Tier.Unknown, "daemon tier");
}

static void UnknownPathFailsClosed()
{
    WispPathParser.ParseAnimalTierFromPath("Metadata/Monsters/NotAWisp", out var animal, out var tier);
    Assert(animal == "" && tier == Tier.Unknown, "unknown path");
}

static void AttributeMappingStaysPoe2Specific()
{
    Assert(Tiers.AttrToTier("Dex") == Tier.Vivid, "Dex mapping");
    Assert(Tiers.AttrToTier("Int") == Tier.Primal, "Int mapping");
    Assert(Tiers.AttrToTier("Str") == Tier.Wild, "Str mapping");
    Assert(Tiers.AttrToTier("Chaos") == Tier.Unknown, "unknown mapping");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
