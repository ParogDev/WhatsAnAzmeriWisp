using System.Collections.Generic;
using ExileCore2;
using ExileCore2.Shared.Enums;
using WhatsAnAzmeriWisp.Detection;
using Vector2N = System.Numerics.Vector2;
using Color = System.Drawing.Color;

namespace WhatsAnAzmeriWisp.Rendering;

// Draws world-space circles + a short category label per classified entity.
// Empowerment magnitude is intentionally NOT drawn here -- it lives in the readout panel.
public sealed class WorldOverlay
{
    public void Render(Graphics graphics, GameController gc, WhatsAnAzmeriWispSettings settings,
        List<RenderSnapshot> snapshots)
    {
        var camera = gc.IngameState.Camera;
        var world = settings.World;

        for (int i = 0; i < snapshots.Count; i++)
        {
            var s = snapshots[i];
            if (!CategoryEnabled(world, s.Category)) continue;

            var color = s.IsHighValue ? settings.Power.HighValueColor.Value : CategoryColor(world, s.Category);
            float radius = CategoryRadius(world, s.Category);
            if (!IsFinite(s.WorldPos) || !float.IsFinite(radius) || radius <= 0) continue;

            graphics.DrawFilledCircleInWorld(s.WorldPos, radius, color);

            if (!world.ShowLabel.Value) continue;

            var screen = camera.WorldToScreen(s.WorldPos);
            if (!IsFinite(screen)) continue;

            using (graphics.SetTextScale(world.FontSize.Value / 16f))
                graphics.DrawText(s.Label, new Vector2N(screen.X, screen.Y - 30), color, FontAlign.Center);
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

    private static float CategoryRadius(WorldSettings w, WispCategory c)
    {
        switch (c)
        {
            case WispCategory.Possessed: return w.PossessedRadius.Value;
            case WispCategory.Touched: return w.TouchedRadius.Value;
            default: return w.WispRadius.Value;
        }
    }

    private static bool IsFinite(Vector2N value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y);
    }

    private static bool IsFinite(System.Numerics.Vector3 value)
    {
        return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
    }
}
