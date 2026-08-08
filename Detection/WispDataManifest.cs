using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WhatsAnAzmeriWisp.Detection;

public sealed class WispDataManifest
{
    public int SchemaVersion { get; set; }
    public string Game { get; set; } = "";
    public string ValidatedAgainstPatch { get; set; } = "";
    public string GeneratedAtUtc { get; set; } = "";
    public string Source { get; set; } = "";
    public List<string> SourceUrls { get; set; } = new();
    public string Coverage { get; set; } = "";

    public bool IsPoE2 => string.Equals(Game, "poe2", StringComparison.OrdinalIgnoreCase);

    public static WispDataManifest? Load(string pluginDirectory, out string message)
    {
        var path = Path.Combine(pluginDirectory ?? "", "data_manifest.json");
        try
        {
            var manifest = JsonConvert.DeserializeObject<WispDataManifest>(File.ReadAllText(path));
            if (manifest == null)
            {
                message = "Azmeri Wisp: data manifest is empty.";
                return null;
            }

            if (!manifest.IsPoE2)
            {
                message = $"Azmeri Wisp: rejected data manifest for game '{manifest.Game}'.";
                return null;
            }

            message = $"Azmeri Wisp: PoE2 {manifest.ValidatedAgainstPatch} data, generated {manifest.GeneratedAtUtc}; source {manifest.Source}";
            return manifest;
        }
        catch (Exception ex)
        {
            message = "Azmeri Wisp: failed to read data_manifest.json: " + ex.Message;
            return null;
        }
    }
}
