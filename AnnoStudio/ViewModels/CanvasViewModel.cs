using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AnnoStudio.Services;
using AnnoStudio.EditorCanvas.Tools;
using AnnoStudio.EditorCanvas.Objects;
using AnnoStudio.EditorCanvas.Core.Interfaces;
using SkiaSharp;

namespace AnnoStudio.ViewModels
{
    /// <summary>
    /// ViewModel for the main canvas view that manages the document
    /// </summary>
    public partial class CanvasViewModel : ObservableObject
    {
        [ObservableProperty]
        private LayoutDocument? _document;
        
        [ObservableProperty]
        private BuildingStamp? _selectedStamp;

        /// <summary>
        /// Event raised when an object is selected on the canvas
        /// </summary>
        public event EventHandler<ICanvasObject?>? SelectionChanged;

        public CanvasViewModel()
        {
            // Document will be created and initialized when canvas control loads
            Document = new LayoutDocument();
        }

        partial void OnDocumentChanged(LayoutDocument? oldValue, LayoutDocument? newValue)
        {
            // Unsubscribe from old document's selection changes
            if (oldValue?.EditorViewModel != null)
            {
                oldValue.EditorViewModel.SelectedObjects.CollectionChanged -= OnSelectedObjectsChanged;
            }

            // Subscribe to new document's selection changes
            if (newValue?.EditorViewModel != null)
            {
                newValue.EditorViewModel.SelectedObjects.CollectionChanged += OnSelectedObjectsChanged;
            }
        }

        private void OnSelectedObjectsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Notify properties panel about selection change
            var selectedObject = Document?.EditorViewModel?.SelectedObjects.FirstOrDefault();
            SelectionChanged?.Invoke(this, selectedObject);
        }

        partial void OnSelectedStampChanged(BuildingStamp? value)
        {
            if (value != null && Document?.EditorViewModel != null)
            {
                SetStampTemplate(value);
            }
        }

        /// <summary>
        /// Initializes the canvas control reference for the document
        /// </summary>
        public void InitializeCanvas(AnnoStudio.EditorCanvas.Controls.EditorCanvas canvasControl)
        {
            Document?.Initialize(canvasControl);
        }
        
        /// <summary>
        /// Sets the stamp template for the StampTool and activates it
        /// </summary>
        private void SetStampTemplate(BuildingStamp stamp)
        {
            if (Document?.EditorViewModel == null)
                return;
                
            // Find the StampTool
            var stampTool = Document.EditorViewModel.Tools
                .OfType<StampTool>()
                .FirstOrDefault();
                
            if (stampTool != null)
            {
                // Create a template BuildingObject from the stamp
                var template = new BuildingObject
                {
                    Name = stamp.Name,
                    Width = stamp.Width,
                    Height = stamp.Height,
                    BuildingType = stamp.Properties.TryGetValue("BuildingType", out var type) ? type.ToString() ?? "Generic" : "Generic",
                    Color = SKColors.LightGray, // Default color, can be customized
                    // Icon will be null for now, can be loaded from IconPath later
                    Transform = new EditorCanvas.Core.Models.Transform2D
                    {
                        Position = new SKPoint(0, 0),
                        Rotation = 0,
                        Scale = new SKPoint(1, 1)
                    }
                };
                
                // Set the template on the stamp tool
                stampTool.SetTemplate(template);
                
                // Activate the stamp tool
                Document.EditorViewModel.SelectedTool = stampTool;
            }
        }
    }
}
