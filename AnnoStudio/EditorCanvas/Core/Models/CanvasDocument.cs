using System;
using System.Collections.Generic;
using AnnoStudio.EditorCanvas.Core.Interfaces;

namespace AnnoStudio.EditorCanvas.Core.Models;

/// <summary>
/// Represents a complete canvas document for serialization.
/// </summary>
public class CanvasDocument
{
    /// <summary>
    /// Document format version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Document metadata.
    /// </summary>
    public DocumentMetadata Metadata { get; set; } = new();

    /// <summary>
    /// Editor settings embedded in document.
    /// </summary>
    public EditorSettings Settings { get; set; } = new();

    /// <summary>
    /// All canvas objects.
    /// </summary>
    public List<ICanvasObject> Objects { get; set; } = new();

    /// <summary>
    /// Layer definitions.
    /// </summary>
    public List<LayerDefinition> Layers { get; set; } = new();
}

/// <summary>
/// Document metadata for tracking and organization.
/// </summary>
public class DocumentMetadata
{
    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime Created { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime Modified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Document author.
    /// </summary>
    public string Author { get; set; } = Environment.UserName;

    /// <summary>
    /// Document title.
    /// </summary>
    public string Title { get; set; } = "Untitled";

    /// <summary>
    /// Document description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Custom metadata tags.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();
}

/// <summary>
/// Layer definition for serialization.
/// </summary>
public class LayerDefinition
{
    /// <summary>
    /// Layer name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Layer visibility.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Layer opacity.
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Layer is locked.
    /// </summary>
    public bool IsLocked { get; set; } = false;
}
