using System.Collections.Generic;
using AnnoStudio.EditorCanvas.Objects;

namespace AnnoStudio.Services;

/// <summary>
/// Service for managing building stamps/templates
/// </summary>
public interface IStampService
{
    /// <summary>
    /// Gets all available building stamps
    /// </summary>
    IEnumerable<BuildingStamp> GetAllStamps();

    /// <summary>
    /// Gets stamps filtered by category
    /// </summary>
    IEnumerable<BuildingStamp> GetStampsByCategory(string category);

    /// <summary>
    /// Gets a stamp by ID
    /// </summary>
    BuildingStamp? GetStampById(string id);

    /// <summary>
    /// Creates a new building from a stamp
    /// </summary>
    BuildingObject CreateBuildingFromStamp(BuildingStamp stamp);

    /// <summary>
    /// Saves a custom stamp
    /// </summary>
    void SaveCustomStamp(BuildingStamp stamp);

    /// <summary>
    /// Gets all available categories
    /// </summary>
    IEnumerable<string> GetCategories();
}

/// <summary>
/// Represents a building stamp/template
/// </summary>
public class BuildingStamp
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
    public string? IconPath { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
