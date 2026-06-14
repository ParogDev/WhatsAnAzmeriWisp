using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Color = System.Drawing.Color;

namespace WhatsAnAzmeriWisp;

public class WhatsAnAzmeriWispSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new(true);
    public GeneralSettings General { get; set; } = new();
    public WorldSettings World { get; set; } = new();
    public MinimapSettings Minimap { get; set; } = new();
    public PanelSettings Panel { get; set; } = new();
    public PowerSettings Power { get; set; } = new();
}

[Submenu]
public class GeneralSettings
{
    public RangeNode<int> DrawDistance { get; set; } = new(120, 20, 400);
}

[Submenu]
public class WorldSettings
{
    public ToggleNode Enabled { get; set; } = new(true);

    public ToggleNode ShowPossessed { get; set; } = new(true);
    public ToggleNode ShowTouched { get; set; } = new(true);
    public ToggleNode ShowWisp { get; set; } = new(true);
    public ToggleNode ShowSpiritAnimal { get; set; } = new(false);

    public ColorNode PossessedColor { get; set; } = new(Color.FromArgb(200, 255, 60, 230));
    public ColorNode TouchedColor { get; set; } = new(Color.FromArgb(180, 120, 200, 255));
    public ColorNode WispColor { get; set; } = new(Color.FromArgb(180, 120, 255, 120));
    public ColorNode SpiritAnimalColor { get; set; } = new(Color.FromArgb(140, 180, 180, 180));

    public RangeNode<int> PossessedRadius { get; set; } = new(70, 20, 200);
    public RangeNode<int> TouchedRadius { get; set; } = new(50, 20, 200);
    public RangeNode<int> WispRadius { get; set; } = new(50, 20, 200);

    public ToggleNode ShowLabel { get; set; } = new(true);
    public RangeNode<int> FontSize { get; set; } = new(16, 8, 32);
}

[Submenu]
public class MinimapSettings
{
    public ToggleNode Enabled { get; set; } = new(true);
    public RangeNode<float> Radius { get; set; } = new(8f, 2f, 30f);
}

[Submenu]
public class PanelSettings
{
    public ToggleNode Enabled { get; set; } = new(true);
    public RangeNode<int> X { get; set; } = new(500, 0, 3840);
    public RangeNode<int> Y { get; set; } = new(140, 0, 2160);
    public RangeNode<float> Scale { get; set; } = new(1f, 0.6f, 2f);
    public ToggleNode ShowCounts { get; set; } = new(true);
    public ToggleNode ShowList { get; set; } = new(true);
    public ToggleNode ShowEmpower { get; set; } = new(true);
    public RangeNode<int> MaxRows { get; set; } = new(12, 1, 40);
}

[Submenu]
public class PowerSettings
{
    public RangeNode<int> HighValueThreshold { get; set; } = new(10000, 0, 100000);
    public ColorNode HighValueColor { get; set; } = new(Color.FromArgb(255, 255, 215, 0));
}
