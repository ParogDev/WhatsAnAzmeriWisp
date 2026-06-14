namespace WhatsAnAzmeriWisp.Detection;

// What a tracked entity is, in Azmeri / Tormented Spirit terms.
public enum WispCategory
{
    None,
    Possessed,    // rare/unique host carrying SpiritOfThe<Animal>Possessed + tormented_spirit_power
    Touched,      // normal/magic carrying SpiritOfThe<Animal>Touched
    FreeWisp,     // roaming TormentedSpiritofthe<Animal><Tier> (no PossesedDaemon suffix)
    SpiritAnimal, // summoned Spiritofthe<Animal> minion
}
