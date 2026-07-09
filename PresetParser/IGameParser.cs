using System.Collections.Generic;
using AnnoDesigner.Core.Presets.Models;

namespace PresetParser;

/// <summary>
/// Strategy interface for per-game preset parsing.
/// Each Anno version implements this to extract building data from its RDA/XML game files.
/// </summary>
public interface IGameParser
{
    /// <summary>
    /// The Anno version string this parser handles (e.g., "1404", "2070", "2205", "1800").
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Configures the parser with the base path to extracted game data.
    /// </summary>
    void Configure(string basePath, bool testMode = false);

    /// <summary>
    /// Parses game assets and returns all discovered building infos.
    /// </summary>
    List<IBuildingInfo> ParseBuildings();
}
