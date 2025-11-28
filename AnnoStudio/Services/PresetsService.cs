using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SkiaSharp;

namespace AnnoStudio.Services;

/// <summary>
/// Implementation of presets service for managing colors and icons
/// </summary>
public class PresetsService : IPresetsService
{
    private readonly List<ColorPreset> _colorPresets;
    private readonly List<IconPreset> _iconPresets;
    private readonly string _presetsDirectory;

    public PresetsService()
    {
        _colorPresets = new List<ColorPreset>();
        _iconPresets = new List<IconPreset>();
        
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _presetsDirectory = Path.Combine(appData, "AnnoStudio", "Presets");
        Directory.CreateDirectory(_presetsDirectory);

        InitializeDefaultPresets();
    }

    public async Task LoadPresetsAsync()
    {
        await LoadColorPresetsAsync();
        await LoadIconPresetsAsync();
    }

    public IEnumerable<ColorPreset> GetColorPresets()
    {
        return _colorPresets;
    }

    public IEnumerable<IconPreset> GetIconPresets()
    {
        return _iconPresets;
    }

    public ColorPreset? GetColorPreset(string name)
    {
        return _colorPresets.FirstOrDefault(p => p.Name == name);
    }

    public IconPreset? GetIconPreset(string name)
    {
        return _iconPresets.FirstOrDefault(p => p.Name == name);
    }

    public async Task SaveColorPresetsAsync()
    {
        try
        {
            var filePath = Path.Combine(_presetsDirectory, "colors.json");
            var customPresets = _colorPresets.Where(p => p.IsCustom).ToList();
            
            var json = JsonSerializer.Serialize(customPresets, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving color presets: {ex.Message}");
        }
    }

    public async Task SaveIconPresetsAsync()
    {
        try
        {
            var filePath = Path.Combine(_presetsDirectory, "icons.json");
            var customPresets = _iconPresets.Where(p => p.IsCustom).ToList();
            
            var json = JsonSerializer.Serialize(customPresets, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving icon presets: {ex.Message}");
        }
    }

    public void AddColorPreset(ColorPreset preset)
    {
        preset.IsCustom = true;
        _colorPresets.RemoveAll(p => p.Name == preset.Name);
        _colorPresets.Add(preset);
    }

    public void AddIconPreset(IconPreset preset)
    {
        preset.IsCustom = true;
        _iconPresets.RemoveAll(p => p.Name == preset.Name);
        _iconPresets.Add(preset);
    }

    private async Task LoadColorPresetsAsync()
    {
        try
        {
            var filePath = Path.Combine(_presetsDirectory, "colors.json");
            if (!File.Exists(filePath))
                return;

            var json = await File.ReadAllTextAsync(filePath);
            var presets = JsonSerializer.Deserialize<List<ColorPreset>>(json);
            
            if (presets != null)
            {
                foreach (var preset in presets)
                {
                    preset.IsCustom = true;
                    _colorPresets.Add(preset);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading color presets: {ex.Message}");
        }
    }

    private async Task LoadIconPresetsAsync()
    {
        try
        {
            var filePath = Path.Combine(_presetsDirectory, "icons.json");
            if (!File.Exists(filePath))
                return;

            var json = await File.ReadAllTextAsync(filePath);
            var presets = JsonSerializer.Deserialize<List<IconPreset>>(json);
            
            if (presets != null)
            {
                foreach (var preset in presets)
                {
                    preset.IsCustom = true;
                    _iconPresets.Add(preset);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading icon presets: {ex.Message}");
        }
    }

    private void InitializeDefaultPresets()
    {
        // Default color presets
        _colorPresets.AddRange(new[]
        {
            new ColorPreset { Name = "Red", Color = new SKColor(255, 0, 0), Category = "Primary" },
            new ColorPreset { Name = "Green", Color = new SKColor(0, 255, 0), Category = "Primary" },
            new ColorPreset { Name = "Blue", Color = new SKColor(0, 0, 255), Category = "Primary" },
            new ColorPreset { Name = "Yellow", Color = new SKColor(255, 255, 0), Category = "Primary" },
            new ColorPreset { Name = "Orange", Color = new SKColor(255, 165, 0), Category = "Secondary" },
            new ColorPreset { Name = "Purple", Color = new SKColor(128, 0, 128), Category = "Secondary" },
            new ColorPreset { Name = "Gray", Color = new SKColor(128, 128, 128), Category = "Neutral" },
            new ColorPreset { Name = "White", Color = new SKColor(255, 255, 255), Category = "Neutral" },
            new ColorPreset { Name = "Black", Color = new SKColor(0, 0, 0), Category = "Neutral" }
        });
    }
}
