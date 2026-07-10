using System.Collections.Generic;

namespace AnnoDesigner.Core.Presets.Models;

/// <summary>
/// Manifest file that indexes per-game preset files.
/// Located at Presets/presets_manifest.json.
/// </summary>
public class PresetsManifest
{
    public string Version { get; set; }
    public List<GamePresetEntry> Games { get; set; } = new();
}

public class GamePresetEntry
{
    public string Game { get; set; }       // e.g. "Anno 1800"
    public string FileName { get; set; }   // e.g. "presets_1800.json"
    public string Version { get; set; }    // per-game preset version
    public int BuildingCount { get; set; }
}
