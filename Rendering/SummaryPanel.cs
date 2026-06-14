using System.Collections.Generic;
using ExileCore2;
using ExileCore2.Shared.Enums;
using WhatsAnAzmeriWisp.Detection;
using Vector2N = System.Numerics.Vector2;
using RectangleF = ExileCore2.Shared.RectangleF;
using Color = System.Drawing.Color;

namespace WhatsAnAzmeriWisp.Rendering;

// Branded on-screen readout: per-entity list (animal/tier + empowerment) grouped by priority,
// with a counts header. This is where the empowerment number is surfaced (not in the world).
public sealed class SummaryPanel
{
    private static readonly Color Accent = Color.FromArgb(255, 90, 215, 170);   // wisp teal
    private static readonly Color PanelBg = Color.FromArgb(214, 12, 14, 18);
    private static readonly Color HeaderText = Color.FromArgb(255, 235, 240, 245);
    private static readonly Color Dim = Color.FromArgb(255, 150, 158, 168);

    private readonly List<RenderSnapshot> _sorted = new(32);

    public void Render(Graphics graphics, WhatsAnAzmeriWispSettings settings,
        List<RenderSnapshot> snapshots, SummaryCounts counts)
    {
        var p = settings.Panel;
        int total = counts.Possessed + counts.Touched + counts.Wisps + counts.SpiritAnimals;
        if (total == 0) return;

        float scale = p.Scale.Value;
        using (graphics.SetTextScale(scale))
        {
            const float pad = 8f;
            float x = p.X.Value;
            float y = p.Y.Value;

            // Pre-measure to size the panel to its content.
            string title = "Azmeri Wisps";
            string countsLine = "Possessed " + counts.Possessed + "   Touched " + counts.Touched
                + "   Wisps " + counts.Wisps + "   Animals " + counts.SpiritAnimals;

            BuildSorted(snapshots);
            int rowCount = _sorted.Count < p.MaxRows.Value ? _sorted.Count : p.MaxRows.Value;

            float lineH = graphics.MeasureText("Ay").Y + 3f;
            float width = graphics.MeasureText(title).X;
            if (p.ShowCounts.Value) width = Max(width, graphics.MeasureText(countsLine).X);

            var rowTexts = new string[rowCount];
            if (p.ShowList.Value)
            {
                for (int i = 0; i < rowCount; i++)
                {
                    rowTexts[i] = RowText(_sorted[i], p.ShowEmpower.Value);
                    width = Max(width, graphics.MeasureText(rowTexts[i]).X);
                }
            }

            float height = lineH + (p.ShowCounts.Value ? lineH : 0f)
                + (p.ShowList.Value ? rowCount * lineH : 0f) + pad;
            float boxW = width + pad * 2;

            var rect = new RectangleF(x, y, boxW, height + pad);
            graphics.DrawBox(rect, PanelBg);
            graphics.DrawFrame(rect, Accent, 1);
            // accent header underline
            graphics.DrawLine(new Vector2N(x, y + lineH + 2), new Vector2N(x + boxW, y + lineH + 2),
                1f, Color.FromArgb(120, Accent.R, Accent.G, Accent.B));

            float cy = y + pad * 0.5f;
            float cx = x + pad;

            graphics.DrawText(title, new Vector2N(cx, cy), Accent, FontAlign.Left);
            cy += lineH;

            if (p.ShowCounts.Value)
            {
                graphics.DrawText(countsLine, new Vector2N(cx, cy), Dim, FontAlign.Left);
                cy += lineH;
                if (counts.HighValue > 0)
                {
                    // append a gold high-value tag to the right of counts
                    graphics.DrawText("  HV " + counts.HighValue,
                        new Vector2N(cx + graphics.MeasureText(countsLine).X, cy - lineH),
                        settings.Power.HighValueColor.Value, FontAlign.Left);
                }
            }

            if (p.ShowList.Value)
            {
                for (int i = 0; i < rowCount; i++)
                {
                    var s = _sorted[i];
                    var col = s.IsHighValue ? settings.Power.HighValueColor.Value : RowColor(settings, s.Category);
                    graphics.DrawText(rowTexts[i], new Vector2N(cx, cy), col, FontAlign.Left);
                    cy += lineH;
                }
            }
        }
    }

    private void BuildSorted(List<RenderSnapshot> snapshots)
    {
        _sorted.Clear();
        for (int i = 0; i < snapshots.Count; i++) _sorted.Add(snapshots[i]);
        _sorted.Sort(static (a, b) =>
        {
            int ka = SortKey(a), kb = SortKey(b);
            if (ka != kb) return ka - kb;
            return b.EmpowerValue - a.EmpowerValue; // higher empower first within a group
        });
    }

    private static int SortKey(RenderSnapshot s)
    {
        if (s.IsHighValue) return 0;
        switch (s.Category)
        {
            case WispCategory.Possessed: return 1;
            case WispCategory.FreeWisp: return 2;
            case WispCategory.Touched: return 3;
            default: return 4;
        }
    }

    private static string RowText(RenderSnapshot s, bool showEmpower)
    {
        string tag;
        switch (s.Category)
        {
            case WispCategory.Possessed: tag = "POSS"; break;
            case WispCategory.FreeWisp: tag = "WISP"; break;
            case WispCategory.Touched: tag = "TCHD"; break;
            default: tag = "ANML"; break;
        }
        var name = string.IsNullOrEmpty(s.ShortName) ? "Spirit" : s.ShortName;
        var row = tag + "  " + name;
        if (showEmpower && s.HasEmpower) row += "   E " + s.EmpowerValue;
        else if (s.IsPowered && s.Category == WispCategory.FreeWisp) row += "   *";
        return row;
    }

    private static Color RowColor(WhatsAnAzmeriWispSettings settings, WispCategory c)
    {
        switch (c)
        {
            case WispCategory.Possessed: return settings.World.PossessedColor.Value;
            case WispCategory.Touched: return settings.World.TouchedColor.Value;
            case WispCategory.FreeWisp: return settings.World.WispColor.Value;
            default: return settings.World.SpiritAnimalColor.Value;
        }
    }

    private static float Max(float a, float b) => a > b ? a : b;
}
