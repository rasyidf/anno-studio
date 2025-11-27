namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Main editor settings collection.
/// </summary>
public class EditorSettings
{
    /// <summary>
    /// Grid settings.
    /// </summary>
    public GridSettings Grid { get; set; } = new();

    /// <summary>
    /// Rendering settings.
    /// </summary>
    public RenderSettings Render { get; set; } = new();

    /// <summary>
    /// Tool-specific settings.
    /// </summary>
    public ToolSettings Tools { get; set; } = new();

    /// <summary>
    /// Keyboard shortcuts.
    /// </summary>
    public KeyBindings KeyBindings { get; set; } = new();

    /// <summary>
    /// Theme settings.
    /// </summary>
    public ThemeSettings Theme { get; set; } = new();

    /// <summary>
    /// Debugging options (development only).
    /// </summary>
    public DebugSettings Debug { get; set; } = new();
}

    /// <summary>
    /// Development/debugging settings for editor canvas.
    /// </summary>
    public class DebugSettings
    {
        /// <summary>
        /// Show debug overlay with pointer/grid information.
        /// </summary>
        public bool ShowOverlay { get; set; } = true;
    }

/// <summary>
/// Tool-specific settings.
/// </summary>
public class ToolSettings
{
    /// <summary>
    /// Default brush size for drawing tools.
    /// </summary>
    public float DefaultBrushSize { get; set; } = 2f;

    /// <summary>
    /// Default color for new objects.
    /// </summary>
    public string DefaultColor { get; set; } = "#000000";

    /// <summary>
    /// Whether to auto-switch to select tool after placing object.
    /// </summary>
    public bool AutoSwitchToSelect { get; set; } = true;

    /// <summary>
    /// Duplicate offset in pixels.
    /// </summary>
    public float DuplicateOffset { get; set; } = 10f;
}

/// <summary>
/// Keyboard shortcut bindings.
/// </summary>
public class KeyBindings
{
    public string Undo { get; set; } = "Ctrl+Z";
    public string Redo { get; set; } = "Ctrl+Y";
    public string Delete { get; set; } = "Delete";
    public string SelectAll { get; set; } = "Ctrl+A";
    public string Copy { get; set; } = "Ctrl+C";
    public string Paste { get; set; } = "Ctrl+V";
    public string Duplicate { get; set; } = "Ctrl+D";
    public string ZoomIn { get; set; } = "Ctrl+Plus";
    public string ZoomOut { get; set; } = "Ctrl+Minus";
    public string ZoomReset { get; set; } = "Ctrl+0";
    public string ToggleGrid { get; set; } = "Ctrl+G";
}

/// <summary>
/// Theme and visual appearance settings.
/// </summary>
public class ThemeSettings
{
    /// <summary>
    /// Dark mode enabled.
    /// </summary>
    public bool UseDarkMode { get; set; } = false;

    /// <summary>
    /// Accent color.
    /// </summary>
    public string AccentColor { get; set; } = "#0078D4";

    /// <summary>
    /// Font family for UI.
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// Font size for UI.
    /// </summary>
    public float FontSize { get; set; } = 12f;
}
