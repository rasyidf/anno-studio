using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Objects;
using SkiaSharp;

namespace AnnoStudio.ViewModels;

/// <summary>
/// ViewModel for the properties panel
/// </summary>
public partial class PropertiesViewModel : ObservableObject
{
    [ObservableProperty]
    private ICanvasObject? _selectedObject;

    [ObservableProperty]
    private bool _hasSelection;

    // BuildingObject-specific properties
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _width = 1;

    [ObservableProperty]
    private int _height = 1;

    [ObservableProperty]
    private string _buildingType = string.Empty;

    [ObservableProperty]
    private float _positionX;

    [ObservableProperty]
    private float _positionY;

    [ObservableProperty]
    private float _rotation;

    public PropertiesViewModel()
    {
    }

    partial void OnSelectedObjectChanged(ICanvasObject? value)
    {
        HasSelection = value != null;

        if (value is BuildingObject building)
        {
            LoadBuildingProperties(building);
        }
        else
        {
            ClearProperties();
        }
    }

    partial void OnNameChanged(string value)
    {
        if (SelectedObject != null && !string.IsNullOrEmpty(value))
        {
            SelectedObject.Name = value;
        }
    }

    partial void OnWidthChanged(int value)
    {
        if (SelectedObject is BuildingObject building && value > 0)
        {
            building.Width = value;
        }
    }

    partial void OnHeightChanged(int value)
    {
        if (SelectedObject is BuildingObject building && value > 0)
        {
            building.Height = value;
        }
    }

    partial void OnBuildingTypeChanged(string value)
    {
        if (SelectedObject is BuildingObject building)
        {
            building.BuildingType = value;
        }
    }

    partial void OnPositionXChanged(float value)
    {
        if (SelectedObject != null)
        {
            var transform = SelectedObject.Transform;
            transform.Position = new SKPoint(value, transform.Position.Y);
            SelectedObject.Transform = transform;
        }
    }

    partial void OnPositionYChanged(float value)
    {
        if (SelectedObject != null)
        {
            var transform = SelectedObject.Transform;
            transform.Position = new SKPoint(transform.Position.X, value);
            SelectedObject.Transform = transform;
        }
    }

    partial void OnRotationChanged(float value)
    {
        if (SelectedObject != null)
        {
            var transform = SelectedObject.Transform;
            transform.Rotation = value;
            SelectedObject.Transform = transform;
        }
    }

    private void LoadBuildingProperties(BuildingObject building)
    {
        Name = building.Name;
        Width = building.Width;
        Height = building.Height;
        BuildingType = building.BuildingType;
        PositionX = building.Transform.Position.X;
        PositionY = building.Transform.Position.Y;
        Rotation = building.Transform.Rotation;
    }

    private void ClearProperties()
    {
        Name = string.Empty;
        Width = 1;
        Height = 1;
        BuildingType = string.Empty;
        PositionX = 0;
        PositionY = 0;
        Rotation = 0;
    }

    /// <summary>
    /// Updates the selected object from the canvas
    /// </summary>
    public void UpdateSelection(ICanvasObject? obj)
    {
        SelectedObject = obj;
    }
}
