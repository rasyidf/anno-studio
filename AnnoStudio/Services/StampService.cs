using System;
using System.Collections.Generic;
using System.Linq;
using AnnoStudio.EditorCanvas.Objects;
using SkiaSharp;

namespace AnnoStudio.Services;

/// <summary>
/// Implementation of stamp service for managing building templates
/// </summary>
public class StampService : IStampService
{
    private readonly List<BuildingStamp> _stamps;
    private readonly Dictionary<string, List<BuildingStamp>> _categoryCache;

    public StampService()
    {
        _stamps = new List<BuildingStamp>();
        _categoryCache = new Dictionary<string, List<BuildingStamp>>();
        InitializeDefaultStamps();
    }

    public IEnumerable<BuildingStamp> GetAllStamps()
    {
        return _stamps;
    }

    public IEnumerable<BuildingStamp> GetStampsByCategory(string category)
    {
        if (_categoryCache.TryGetValue(category, out var stamps))
            return stamps;

        var filtered = _stamps.Where(s => s.Category == category).ToList();
        _categoryCache[category] = filtered;
        return filtered;
    }

    public BuildingStamp? GetStampById(string id)
    {
        return _stamps.FirstOrDefault(s => s.Id == id);
    }

    public BuildingObject CreateBuildingFromStamp(BuildingStamp stamp)
    {
        var building = new BuildingObject
        {
            Width = stamp.Width,
            Height = stamp.Height,
            BuildingType = stamp.Category
        };

        // Copy other properties from stamp (name, description, etc.)
        // These can be stored as metadata
        
        return building;
    }

    public void SaveCustomStamp(BuildingStamp stamp)
    {
        // Remove existing stamp with same ID
        _stamps.RemoveAll(s => s.Id == stamp.Id);
        
        // Add new stamp
        _stamps.Add(stamp);

        // Clear category cache to rebuild
        _categoryCache.Clear();
    }

    public IEnumerable<string> GetCategories()
    {
        return _stamps.Select(s => s.Category).Distinct().OrderBy(c => c);
    }

    private void InitializeDefaultStamps()
    {
        // Add default building stamps
        _stamps.Add(new BuildingStamp
        {
            Id = "residence_1x1",
            Name = "Worker Residence",
            Category = "Residences",
            Width = 1,
            Height = 1,
            Description = "Small worker residence"
        });

        _stamps.Add(new BuildingStamp
        {
            Id = "residence_2x2",
            Name = "Artisan Residence",
            Category = "Residences",
            Width = 2,
            Height = 2,
            Description = "Medium artisan residence"
        });

        _stamps.Add(new BuildingStamp
        {
            Id = "factory_3x3",
            Name = "Small Factory",
            Category = "Production",
            Width = 3,
            Height = 3,
            Description = "Small production building"
        });

        _stamps.Add(new BuildingStamp
        {
            Id = "factory_4x4",
            Name = "Medium Factory",
            Category = "Production",
            Width = 4,
            Height = 4,
            Description = "Medium production building"
        });

        _stamps.Add(new BuildingStamp
        {
            Id = "warehouse_2x3",
            Name = "Warehouse",
            Category = "Infrastructure",
            Width = 2,
            Height = 3,
            Description = "Storage warehouse"
        });

        _stamps.Add(new BuildingStamp
        {
            Id = "road_1x1",
            Name = "Road",
            Category = "Infrastructure",
            Width = 1,
            Height = 1,
            Description = "Road tile"
        });
    }
}
