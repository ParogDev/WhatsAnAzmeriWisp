using System.Collections.Generic;
using System.Numerics;
using ExileCore2;
using ExileCore2.Shared.Enums;
using WhatsAnAzmeriWisp.Detection;
using Vector2N = System.Numerics.Vector2;
using Color = System.Drawing.Color;

namespace WhatsAnAzmeriWisp.Rendering;

// Draws a small cross on the large map at each classified entity's grid position.
// Uses GridToMap (grid -> screen) + screen-space lines; DrawLineOnLargeMap is obsolete in EC2.
public sealed class MinimapRenderer
{
    public void Render(Graphics graphics, GameController gc, WhatsAnAzmeriWispSettings settings,
        List<RenderSnapshot> snapshots)
    {
        if (!gc.IngameState.IngameUi.Map.LargeMap.IsVisible) return;

        var world = settings.World;
        float size = settings.Minimap.Radius.Value;

        for (int i = 0; i < snapshots.Count; i++)
        {
            var s = snapshots[i];
            if (!CategoryEnabled(world, s.Category)) continue;

            var color = s.IsHighValue ? settings.Power.HighValueColor.Value : CategoryColor(world, s.Category);

            var c = graphics.GridToMap(s.GridPos, s.GridPos, VisibleSubMap.Large);
            if (c == Vector2N.Zero) continue;

            graphics.DrawPolyLine(new[] { c + new Vector2(-size, 0), c + new Vector2(size, 0) }, color, 2f);
            graphics.DrawPolyLine(new[] { c + new Vector2(0, -size), c + new Vector2(0, size) }, color, 2f);
        }
    }

    private static bool CategoryEnabled(WorldSettings w, WispCategory c)
    {
        switch (c)
        {
            case WispCategory.Possessed: return w.ShowPossessed.Value;
            case WispCategory.Touched: return w.ShowTouched.Value;
            case WispCategory.FreeWisp: return w.ShowWisp.Value;
            case WispCategory.SpiritAnimal: return w.ShowSpiritAnimal.Value;
            default: return false;
        }
    }

    private static Color CategoryColor(WorldSettings w, WispCategory c)
    {
        switch (c)
        {
            case WispCategory.Possessed: return w.PossessedColor.Value;
            case WispCategory.Touched: return w.TouchedColor.Value;
            case WispCategory.FreeWisp: return w.WispColor.Value;
            default: return w.SpiritAnimalColor.Value;
        }
    }
}
