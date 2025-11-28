using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;

namespace AnnoStudio.Services;

/// <summary>
/// Service for managing presets (colors, icons, building definitions)
/// </summary>
public interface IPresetsService
{
    /// <summary>
    /// Loads all presets from storage
    /// </summary>
    Task LoadPresetsAsync();

    /// <summary>
    /// Gets all color presets
    /// </summary>
    IEnumerable<ColorPreset> GetColorPresets();

    /// <summary>
    /// Gets all icon presets
    /// </summary>
    IEnumerable<IconPreset> GetIconPresets();

    /// <summary>
    /// Gets a color preset by name
    /// </summary>
    ColorPreset? GetColorPreset(string name);

    /// <summary>
    /// Gets an icon preset by name
    /// </summary>
    IconPreset? GetIconPreset(string name);

    /// <summary>
    /// Saves color presets
    /// </summary>
    Task SaveColorPresetsAsync();

    /// <summary>
    /// Saves icon presets
    /// </summary>
    Task SaveIconPresetsAsync();

    /// <summary>
    /// Adds a custom color preset
    /// </summary>
    void AddColorPreset(ColorPreset preset);

    /// <summary>
    /// Adds a custom icon preset
    /// </summary>
    void AddIconPreset(IconPreset preset);
}

/// <summary>
/// Represents a color preset
/// </summary>
public class ColorPreset
{
    public string Name { get; set; } = string.Empty;
    public SKColor Color { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}

/// <summary>
/// Represents an icon preset
/// </summary>
public class IconPreset
{
    public string Name { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
}
