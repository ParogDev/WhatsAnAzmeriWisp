using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using WhatsAnAzmeriWisp.Rendering;

namespace WhatsAnAzmeriWisp;

// Settings panel: the generated SettingsMenu (tabs Overlay / Panel / Empowerment from the [Submenu] properties and
// their [Menu] labels) plus two hand-drawn parts it can't know about: the Empowerment explanation and a live Debug
// tree of the values being read (so a broken offset is obvious).
public sealed class WhatsAnAzmeriWispSettingsUi
{
    private readonly SettingsMenu _menu;
    private IReadOnlyList<DebugEntry>? _debug;

    // Azmeri wisp-teal palette.
    private static readonly Vector4 Accent = new(0.35f, 0.85f, 0.66f, 1f);
    private static readonly Vector4 LabelCol = new(0.88f, 0.88f, 0.90f, 1f);
    private static readonly Vector4 Muted = new(0.52f, 0.55f, 0.60f, 1f);
    private static readonly Vector4 Good = new(0.35f, 0.85f, 0.45f, 1f);
    private static readonly Vector4 TypeCol = new(0.4f, 0.42f, 0.48f, 1f);

    public WhatsAnAzmeriWispSettingsUi()
    {
        _menu = new SettingsMenu { Accent = Accent }
            .Tab("Empowerment", DrawEmpowermentIntro, first: true)
            .Tab("Debug", DrawDebugTab);
    }

    public void Draw(WhatsAnAzmeriWispSettings settings, IReadOnlyList<DebugEntry> debug)
    {
        _debug = debug;
        _menu.Draw(settings);
    }

    private void DrawEmpowermentIntro()
    {
        ImGui.PushTextWrapPos(0f);
        Text(Muted, "A wisp's empowerment rises as you kill spirit-touched monsters near it while "
            + "it roams. When it possesses a rare, that value is carried onto the possessed monster "
            + "(matched by the nearest wisp at the moment of possession). The value is read only while "
            + "the wisp roams; possessed monsters show the carried value.");
        ImGui.PopTextWrapPos();
        ImGui.Spacing();
    }

    private void DrawDebugTab()
    {
        var debug = _debug;
        int n = debug?.Count ?? 0;
        Text(Accent, "Live read state");
        ImGui.SameLine();
        Text(Muted, "tracked entities: " + n);
        Text(Muted, "If a value below looks like garbage, that read/offset is broken.");
        ImGui.Spacing();
        ImGui.Separator();

        if (n == 0)
        {
            ImGui.Spacing();
            Text(Muted, "(nothing in range -- walk into Azmeri content)");
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

    // text taken as-is (ImGui's Text* calls are printf-style and would eat a % in a value)
    private static void Text(Vector4 color, string text) => SettingsMenu.Text(color, text);

    private static string Animal(DebugEntry d)
    {
        if (string.IsNullOrEmpty(d.Animal)) return "";
        return d.Tier == Detection.Tier.Unknown ? d.Animal : d.Animal + "(" + d.Tier + ")";
    }

    private static void KvLine(string key, string value, string type, bool highlight = false)
    {
        Text(Muted, "    " + key + ":");
        ImGui.SameLine(220);
        Text(highlight ? Good : LabelCol, value);
        ImGui.SameLine();
        Text(TypeCol, "(" + type + ")");
    }
}
