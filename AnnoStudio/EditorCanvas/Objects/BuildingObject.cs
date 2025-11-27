using System;
using System.Collections.Generic;
using SkiaSharp;
using AnnoStudio.EditorCanvas.Core.Base;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using AnnoStudio.EditorCanvas.Core.Models;

namespace AnnoStudio.EditorCanvas.Objects;

/// <summary>
/// Represents a building object for Anno layout design.
/// </summary>
public class BuildingObject : CanvasObjectBase
{
    private int _width = 1;
    private int _height = 1;
    private string _buildingType = string.Empty;
    private SKColor _color = SKColors.LightGray;
    private SKBitmap? _icon;

    public override string Type => "Building";

    /// <summary>
    /// Building width in grid cells.
    /// </summary>
    public int Width
    {
        get => _width;
        set => SetProperty(ref _width, Math.Max(1, value));
    }

    /// <summary>
    /// Building height in grid cells.
    /// </summary>
    public int Height
    {
        get => _height;
        set => SetProperty(ref _height, Math.Max(1, value));
    }

    /// <summary>
    /// Type of building (e.g., "Production", "Residence", "Public").
    /// </summary>
    public string BuildingType
    {
        get => _buildingType;
        set => SetProperty(ref _buildingType, value ?? string.Empty);
    }

    /// <summary>
    /// Base color for the building.
    /// </summary>
    public SKColor Color
    {
        get => _color;
        set => SetProperty(ref _color, value);
    }

    /// <summary>
    /// Building icon/texture.
    /// </summary>
    public SKBitmap? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public override SKRect Bounds
    {
        get
        {
            var size = new SKSize(Width * 16, Height * 16); // Assume 16px grid
            return new SKRect(
                Transform.Position.X,
                Transform.Position.Y,
                Transform.Position.X + size.Width,
                Transform.Position.Y + size.Height
            );
        }
    }

    public override bool HitTest(SKPoint point)
    {
        var bounds = Bounds;
        
        // Transform point to local space if object is transformed
        if (Transform.Rotation != 0 || Transform.Scale != new SKPoint(1, 1))
        {
            var inverse = Transform.Inverse();
            point = inverse.TransformPoint(point);
        }

        return bounds.Contains(point);
    }

    public override void Render(SKCanvas canvas, RenderContext context)
    {
        if (!IsVisible)
            return;

        canvas.Save();

        // Apply transform
        var matrix = Transform.ToMatrix();
        var matrixRef = matrix;
        canvas.Concat(ref matrixRef);

        using var paint = new SKPaint
        {
            IsAntialias = context.Settings.AntiAlias,
            FilterQuality = context.Settings.FilterQuality
        };

        var gridSize = context.GridSize;
        var rect = new SKRect(0, 0, Width * gridSize, Height * gridSize);

        // Draw background
        paint.Style = SKPaintStyle.Fill;
        paint.Color = Color;
        canvas.DrawRect(rect, paint);

        // Draw icon if available
        if (Icon != null)
        {
            canvas.DrawBitmap(Icon, rect, paint);
        }

        // Draw border
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 2;
        paint.Color = SKColors.Black;
        canvas.DrawRect(rect, paint);

        // Draw selection highlight if selected
        if (context.Selection?.IsSelected(this) == true)
        {
            paint.Color = context.Settings.SelectionColor;
            paint.StrokeWidth = context.Settings.SelectionThickness;
            var selectionRect = rect;
            selectionRect.Inflate(2, 2);
            canvas.DrawRect(selectionRect, paint);
        }

        canvas.Restore();
    }

    public override ICanvasObject Clone()
    {
        return new BuildingObject
        {
            Name = Name + " (Copy)",
            Width = Width,
            Height = Height,
            BuildingType = BuildingType,
            Color = Color,
            Icon = Icon,
            Transform = Transform,
            Layer = Layer,
            IsVisible = IsVisible,
            ZOrder = ZOrder
        };
    }

    public override Dictionary<string, object> GetProperties()
    {
        var properties = base.GetProperties();
        properties["Width"] = Width;
        properties["Height"] = Height;
        properties["BuildingType"] = BuildingType;
        properties["Color"] = Color.ToString();
        return properties;
    }

    public override void SetProperties(Dictionary<string, object> properties)
    {
        base.SetProperties(properties);

        if (properties.TryGetValue("Width", out var width))
            Width = Convert.ToInt32(width);

        if (properties.TryGetValue("Height", out var height))
            Height = Convert.ToInt32(height);

        if (properties.TryGetValue("BuildingType", out var buildingType))
            BuildingType = buildingType?.ToString() ?? string.Empty;

        if (properties.TryGetValue("Color", out var color) && color is string colorStr)
        {
            if (SKColor.TryParse(colorStr, out var parsedColor))
                Color = parsedColor;
        }
    }
}
