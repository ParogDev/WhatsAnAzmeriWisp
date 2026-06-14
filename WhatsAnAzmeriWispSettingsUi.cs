using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using ExileCore2.Shared.Nodes;
using WhatsAnAzmeriWisp.Rendering;
using Color = System.Drawing.Color;

namespace WhatsAnAzmeriWisp;

// Tabbed settings UI in the "Whats A ..." family style: accent tab bar, section headers, grouped
// toggles, and a live Debug tree of the values being read (so a broken offset is obvious).
public sealed class WhatsAnAzmeriWispSettingsUi
{
    private int _activeTab;

    private static readonly string[] Tabs = { "Overlay", "Panel", "Power", "Debug" };

    // Azmeri wisp-teal palette.
    private static uint Accent => Col(0.35f, 0.85f, 0.66f);
    private static uint LabelCol => Col(0.88f, 0.88f, 0.90f);
    private static uint Muted => Col(0.52f, 0.55f, 0.60f);
    private static uint Good => Col(0.35f, 0.85f, 0.45f);
    private static uint Warn => Col(0.95f, 0.78f, 0.25f);

    private const float LabelCol1 = 250f;

    public void Draw(WhatsAnAzmeriWispSettings settings, IReadOnlyList<DebugEntry> debug)
    {
        var contentMin = ImGui.GetCursorScreenPos();
        float contentW = ImGui.GetContentRegionAvail().X;
        var dl = ImGui.GetWindowDrawList();

        float tabW = contentW / Tabs.Length;
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tMin = new Vector2(contentMin.X + i * tabW, contentMin.Y);
            var tMax = new Vector2(contentMin.X + (i + 1) * tabW, contentMin.Y + 28f);
            ImGui.SetCursorScreenPos(tMin);
            ImGui.InvisibleButton($"##aw_tab_{i}", tMax - tMin);
            if (ImGui.IsItemClicked()) _activeTab = i;
            bool active = i == _activeTab;
            if (active)
                dl.AddLine(new Vector2(tMin.X, tMax.Y - 1), new Vector2(tMax.X, tMax.Y - 1), Accent, 2f);
            CenterText(dl, Tabs[i], (tMin + tMax) * 0.5f, active ? Accent : Muted);
        }

        ImGui.SetCursorScreenPos(new Vector2(contentMin.X, contentMin.Y + 34f));
        ImGui.BeginChild("##aw_content", new Vector2(contentW, ImGui.GetContentRegionAvail().Y - 4));

        switch (_activeTab)
        {
            case 0: DrawOverlayTab(settings); break;
            case 1: DrawPanelTab(settings); break;
            case 2: DrawPowerTab(settings); break;
            case 3: DrawDebugTab(debug); break;
        }

        ImGui.EndChild();
    }

    private void DrawOverlayTab(WhatsAnAzmeriWispSettings s)
    {
        ImGui.Spacing();
        SliderRow("Draw Distance", s.General.DrawDistance, "grid");

        SectionHeader("World Overlay");
        ToggleRow("Enable World Overlay", s.World.Enabled, "Master toggle for world circles + labels");
        ToggleRow("Show Possessed", s.World.ShowPossessed, "Rares/uniques possessed by a spirit");
        ToggleRow("Show Touched", s.World.ShowTouched, "Spirit-influenced trash (the chain fodder)");
        ToggleRow("Show Wisps", s.World.ShowWisp, "Free-roaming wisps");
        ToggleRow("Show Spirit Animals", s.World.ShowSpiritAnimal, "Summoned spirit-animal minions");
        ToggleRow("Text Labels", s.World.ShowLabel, "Draw the category/animal label above entities");
        SliderRow("Label Font Size", s.World.FontSize, "px");

        SectionHeader("Colors");
        ColorRow("Possessed", s.World.PossessedColor);
        ColorRow("Touched", s.World.TouchedColor);
        ColorRow("Wisp", s.World.WispColor);
        ColorRow("Spirit Animal", s.World.SpiritAnimalColor);

        SectionHeader("Circle Radius");
        SliderRow("Possessed Radius", s.World.PossessedRadius, "px");
        SliderRow("Touched Radius", s.World.TouchedRadius, "px");
        SliderRow("Wisp Radius", s.World.WispRadius, "px");

        SectionHeader("Minimap");
        ToggleRow("Minimap Marks", s.Minimap.Enabled, "Draw a cross on the large map");
        SliderRowF("Mark Size", s.Minimap.Radius, "px");
    }

    private void DrawPanelTab(WhatsAnAzmeriWispSettings s)
    {
        ImGui.Spacing();
        ImGui.TextColored(ToVec(Muted), "On-screen readout listing tracked wisps and their empowerment.");
        ImGui.Spacing();

        ToggleRow("Enable Panel", s.Panel.Enabled, "Master toggle for the on-screen readout");
        ToggleRow("Show Counts", s.Panel.ShowCounts, "Header line with per-category counts");
        ToggleRow("Show List", s.Panel.ShowList, "Per-entity rows");
        ToggleRow("Show Empowerment", s.Panel.ShowEmpower, "Show the empowerment value on each row");
        SliderRow("Max Rows", s.Panel.MaxRows, "rows");

        SectionHeader("Placement");
        SliderRow("Position X", s.Panel.X, "px");
        SliderRow("Position Y", s.Panel.Y, "px");
        SliderRowF("Scale", s.Panel.Scale, "x");
    }

    private void DrawPowerTab(WhatsAnAzmeriWispSettings s)
    {
        ImGui.Spacing();
        ImGui.TextColored(ToVec(Accent), "Empowerment");
        ImGui.TextWrapped("A wisp's empowerment rises as you kill spirit-touched monsters near it while "
            + "it roams. When it possesses a rare, that value is carried onto the possessed monster "
            + "(matched by the nearest wisp at the moment of possession). The value is read only while "
            + "the wisp roams; possessed monsters show the carried value.");
        ImGui.Spacing();

        SectionHeader("High-Value Flag");
        ImGui.TextColored(ToVec(Muted), "Possessed targets at/above this empowerment are flagged high-value.");
        SliderRow("Empowerment Threshold", s.Power.HighValueThreshold, "");
        ColorRow("High-Value Color", s.Power.HighValueColor);
    }

    private void DrawDebugTab(IReadOnlyList<DebugEntry> debug)
    {
        ImGui.Spacing();
        int n = debug?.Count ?? 0;
        ImGui.TextColored(ToVec(Accent), "Live read state");
        ImGui.SameLine();
        ImGui.TextColored(ToVec(Muted), "tracked entities: " + n);
        ImGui.TextColored(ToVec(Muted), "If a value below looks like garbage, that read/offset is broken.");
        ImGui.Spacing();
        ImGui.Separator();

        if (n == 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(ToVec(Muted), "(nothing in range -- walk into Azmeri content)");
            return;
        }

        for (int i = 0; i < n; i++)
        {
            var d = debug![i];
            var header = "id " + d.Id + "  -  " + d.Category + "  " + Animal(d);
            if (ImGui.CollapsingHeader(header + "##aw_dbg_" + d.Id))
            {
                KvLine("Category", d.Category.ToString(), "WispCategory");
                KvLine("Animal", string.IsNullOrEmpty(d.Animal) ? "(none)" : d.Animal, "string");
                KvLine("Tier", d.Tier.ToString(), "Tier");
                KvLine("Rarity", d.Rarity.ToString(), "MonsterRarity");
                KvLine("IsPowered", d.IsPowered.ToString(), "bool");
                KvLine("HasEmpower", d.HasEmpower.ToString(), "bool");
                KvLine("EmpowerValue", d.EmpowerValue.ToString(), "int", d.HasEmpower);
                KvLine("EmpowerLabel", string.IsNullOrEmpty(d.EmpowerLabel) ? "(none)" : d.EmpowerLabel, "string");
                KvLine("PowerStacks", d.PowerStacks.ToString(), "int");
                KvLine("Carried", d.Carried.ToString(), "bool");
                KvLine("PossessionCount", d.PossessionCount.ToString(), "int");
                KvLine("Distance", d.Distance.ToString("F0"), "float");
            }
        }
    }

    private static string Animal(DebugEntry d)
    {
        if (string.IsNullOrEmpty(d.Animal)) return "";
        return d.Tier == Detection.Tier.Unknown ? d.Animal : d.Animal + "(" + d.Tier + ")";
    }

    private static void KvLine(string key, string value, string type, bool highlight = false)
    {
        ImGui.TextColored(ToVec(Muted), "    " + key + ":");
        ImGui.SameLine(220);
        ImGui.TextColored(ToVec(highlight ? Good : LabelCol), value);
        ImGui.SameLine();
        ImGui.TextColored(ToVec(Col(0.4f, 0.42f, 0.48f)), "(" + type + ")");
    }

    // ---- widgets ----

    private static void SectionHeader(string title)
    {
        ImGui.Spacing();
        ImGui.TextColored(ToVec(Accent), title);
        ImGui.Separator();
    }

    private static void ToggleRow(string label, ToggleNode node, string tooltip)
    {
        bool v = node.Value;
        ImGui.TextColored(ToVec(LabelCol), label);
        if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        ImGui.SameLine(LabelCol1);
        if (ImGui.Checkbox("##" + label, ref v)) node.Value = v;
    }

    private static void SliderRow(string label, RangeNode<int> node, string suffix)
    {
        int v = node.Value;
        ImGui.TextColored(ToVec(LabelCol), label);
        ImGui.SameLine(LabelCol1);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 48);
        if (ImGui.SliderInt("##" + label, ref v, node.Min, node.Max)) node.Value = v;
        if (!string.IsNullOrEmpty(suffix)) { ImGui.SameLine(); ImGui.TextColored(ToVec(Muted), suffix); }
    }

    private static void SliderRowF(string label, RangeNode<float> node, string suffix)
    {
        float v = node.Value;
        ImGui.TextColored(ToVec(LabelCol), label);
        ImGui.SameLine(LabelCol1);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 48);
        if (ImGui.SliderFloat("##" + label, ref v, node.Min, node.Max)) node.Value = v;
        if (!string.IsNullOrEmpty(suffix)) { ImGui.SameLine(); ImGui.TextColored(ToVec(Muted), suffix); }
    }

    private static void ColorRow(string label, ColorNode node)
    {
        var c = node.Value;
        var v4 = new Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
        ImGui.TextColored(ToVec(LabelCol), label);
        ImGui.SameLine(LabelCol1);
        ImGui.SetNextItemWidth(220);
        if (ImGui.ColorEdit4("##" + label, ref v4, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.NoInputs))
            node.Value = Color.FromArgb((int)(v4.W * 255), (int)(v4.X * 255), (int)(v4.Y * 255), (int)(v4.Z * 255));
    }

    private static void CenterText(ImDrawListPtr dl, string text, Vector2 center, uint color)
    {
        var sz = ImGui.CalcTextSize(text);
        dl.AddText(center - sz * 0.5f, color, text);
    }

    private static uint Col(float r, float g, float b, float a = 1f)
        => ImGui.GetColorU32(new Vector4(r, g, b, a));

    private static Vector4 ToVec(uint col) => ImGui.ColorConvertU32ToFloat4(col);
}
