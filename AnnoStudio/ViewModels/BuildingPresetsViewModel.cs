using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoStudio.Services;

namespace AnnoStudio.ViewModels;

/// <summary>
/// ViewModel for the building presets panel
/// </summary>
public partial class BuildingPresetsViewModel : ObservableObject
{
    private readonly IStampService _stampService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private BuildingStamp? _selectedStamp;

    [ObservableProperty]
    private string? _selectedCategory;

    public ObservableCollection<CategoryGroup> Categories { get; } = new();
    public ObservableCollection<BuildingStamp> FilteredStamps { get; } = new();

    public BuildingPresetsViewModel(IStampService stampService)
    {
        _stampService = stampService;
        LoadCategories();
        UpdateFilteredStamps();
    }

    partial void OnSearchTextChanged(string value)
    {
        UpdateFilteredStamps();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        UpdateFilteredStamps();
    }

    partial void OnSelectedStampChanged(BuildingStamp? value)
    {
        if (value != null)
        {
            // Notify that a stamp was selected for placement
            StampSelectedForPlacement?.Invoke(this, value);
        }
    }

    public event EventHandler<BuildingStamp>? StampSelectedForPlacement;

    private void LoadCategories()
    {
        Categories.Clear();
        
        var categories = _stampService.GetCategories().ToList();
        
        // Add "All" category
        Categories.Add(new CategoryGroup
        {
            Name = "All",
            DisplayName = "All Buildings"
        });

        foreach (var category in categories)
        {
            var stamps = _stampService.GetStampsByCategory(category).ToList();
            Categories.Add(new CategoryGroup
            {
                Name = category,
                DisplayName = category,
                StampCount = stamps.Count
            });
        }
    }

    private void UpdateFilteredStamps()
    {
        FilteredStamps.Clear();

        var stamps = string.IsNullOrEmpty(SelectedCategory) || SelectedCategory == "All"
            ? _stampService.GetAllStamps()
            : _stampService.GetStampsByCategory(SelectedCategory);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            stamps = stamps.Where(s => 
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var stamp in stamps)
        {
            FilteredStamps.Add(stamp);
        }
    }

    [RelayCommand]
    private void SelectStamp(BuildingStamp stamp)
    {
        SelectedStamp = stamp;
    }

    [RelayCommand]
    private void PlaceStamp(BuildingStamp stamp)
    {
        SelectedStamp = stamp;
        // The canvas will pick this up via the event or binding
    }
}

/// <summary>
/// Represents a category group for the tree view
/// </summary>
public class CategoryGroup
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int StampCount { get; set; }
    public string DisplayText => StampCount > 0 ? $"{DisplayName} ({StampCount})" : DisplayName;
}
