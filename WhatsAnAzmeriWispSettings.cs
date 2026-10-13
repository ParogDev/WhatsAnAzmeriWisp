using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;
using Color = System.Drawing.Color;

namespace WhatsAnAzmeriWisp;

public class WhatsAnAzmeriWispSettings : ISettings
{
    [Menu("Enable", "Turn the whole plugin on or off")]
    public ToggleNode Enable { get; set; } = new(true);

    [Menu("General", "Settings shared by all overlays")]
    public GeneralSettings General { get; set; } = new();

    [Menu("World Overlay", "Circles and labels on wisps, possessed and touched monsters and spirit animals")]
    public WorldSettings World { get; set; } = new();

    [Menu("Map Marks", "Crosses on the large map for tracked entities")]
    public MinimapSettings Minimap { get; set; } = new();

    [Menu("Panel", "On-screen readout listing tracked wisps and their empowerment")]
    public PanelSettings Panel { get; set; } = new();

    [Menu("Empowerment", "When a possessed target counts as high-value")]
    public PowerSettings Power { get; set; } = new();
}

[Submenu]
public class GeneralSettings
{
    [Menu("Draw Distance", "Track and draw entities within this distance of the player, in grid units (20-400)")]
    public RangeNode<int> DrawDistance { get; set; } = new(120, 20, 400);
}

[Submenu]
public class WorldSettings
{
    [Menu("Enable World Overlay", "Master toggle for world circles and labels")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Show Possessed", "Rares/uniques possessed by a spirit (also filters map marks)")]
    public ToggleNode ShowPossessed { get; set; } = new(true);

    [Menu("Show Touched", "Spirit-touched normal monsters, the chain fodder (also filters map marks)")]
    public ToggleNode ShowTouched { get; set; } = new(true);

    [Menu("Show Wisps", "Free-roaming wisps (also filters map marks)")]
    public ToggleNode ShowWisp { get; set; } = new(true);

    [Menu("Show Spirit Animals", "Summoned spirit-animal minions (also filters map marks)")]
    public ToggleNode ShowSpiritAnimal { get; set; } = new(false);

    [Menu("Possessed Color", "Circle, label and map color for possessed monsters")]
    public ColorNode PossessedColor { get; set; } = new(Color.FromArgb(200, 255, 60, 230));

    [Menu("Touched Color", "Circle, label and map color for touched monsters")]
    public ColorNode TouchedColor { get; set; } = new(Color.FromArgb(180, 120, 200, 255));

    [Menu("Wisp Color", "Circle, label and map color for free wisps")]
    public ColorNode WispColor { get; set; } = new(Color.FromArgb(180, 120, 255, 120));

    [Menu("Spirit Animal Color", "Circle, label and map color for spirit animals")]
    public ColorNode SpiritAnimalColor { get; set; } = new(Color.FromArgb(140, 180, 180, 180));

    [Menu("Possessed Radius", "Filled circle radius for possessed monsters, in world units (20-200)")]
    public RangeNode<int> PossessedRadius { get; set; } = new(70, 20, 200);

    [Menu("Touched Radius", "Filled circle radius for touched monsters, in world units (20-200)")]
    public RangeNode<int> TouchedRadius { get; set; } = new(50, 20, 200);

    [Menu("Wisp Radius", "Filled circle radius for wisps and spirit animals, in world units (20-200)")]
    public RangeNode<int> WispRadius { get; set; } = new(50, 20, 200);

    [Menu("Text Labels", "Draw the category/animal label above entities")]
    public ToggleNode ShowLabel { get; set; } = new(true);

    [Menu("Label Font Size", "Label text size in px (8-32, 16 = normal)")]
    public RangeNode<int> FontSize { get; set; } = new(16, 8, 32);
}

[Submenu]
public class MinimapSettings
{
    [Menu("Map Marks", "Draw a cross on the large map for each shown entity")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Mark Size", "Half-length of each cross arm, in px (2-30)")]
    public RangeNode<float> Radius { get; set; } = new(8f, 2f, 30f);
}

[Submenu]
public class PanelSettings
{
    [Menu("Enable Panel", "Master toggle for the on-screen readout")]
    public ToggleNode Enabled { get; set; } = new(true);

    [Menu("Position X", "Left edge of the panel, in screen px")]
    public RangeNode<int> X { get; set; } = new(500, 0, 3840);

    [Menu("Position Y", "Top edge of the panel, in screen px")]
    public RangeNode<int> Y { get; set; } = new(140, 0, 2160);

    [Menu("Scale", "Panel text and size multiplier (0.6-2.0)")]
    public RangeNode<float> Scale { get; set; } = new(1f, 0.6f, 2f);

    [Menu("Show Counts", "Header line with per-category counts")]
    public ToggleNode ShowCounts { get; set; } = new(true);

    [Menu("Show List", "One row per tracked entity")]
    public ToggleNode ShowList { get; set; } = new(true);

    [Menu("Show Empowerment", "Show the empowerment value on each row")]
    public ToggleNode ShowEmpower { get; set; } = new(true);

    [Menu("Max Rows", "Most entity rows the list shows (1-40)")]
    public RangeNode<int> MaxRows { get; set; } = new(12, 1, 40);
}

[Submenu]
public class PowerSettings
{
    [Menu("Empowerment Threshold", "Possessed targets at or above this empowerment value are flagged high-value (0-100000)")]
    public RangeNode<int> HighValueThreshold { get; set; } = new(10000, 0, 100000);

    [Menu("High-Value Color", "Color used for high-value targets in the world, on the map and in the panel")]
    public ColorNode HighValueColor { get; set; } = new(Color.FromArgb(255, 255, 215, 0));
}
