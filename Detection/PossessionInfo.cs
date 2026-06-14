namespace WhatsAnAzmeriWisp.Detection;

// One animal spirit affecting a host. A host can carry several (multi-possession).
public readonly struct PossessionInfo
{
    public readonly string Animal;
    public readonly Tier Tier;

    public PossessionInfo(string animal, Tier tier)
    {
        Animal = animal;
        Tier = tier;
    }
}
