using System;
using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.EditorCanvas.Demo
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, string> _toolHints = new()
        {
            { "Selection", "Click to select • Drag to marquee" },
            { "RectSelect", "Drag to select within a box" },
            { "LassoSelect", "Sketch a freeform selection" },
            { "RectDraw", "Drag to place a rectangle" },
            { "LineDraw", "Drag to place a line" },
            { "PencilDraw", "Sketch freeform geometry" },
            { "Transform", "Drag handles to resize/move" },
            { "Duplicate", "Drag to copy selection" }
        };
        private const string DefaultHint = "Esc to cancel • Ctrl+D duplicate";

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Create sample objects with different Z orders
            var a = new CanvasObject { Bounds = new Rect(50, 50, 200, 120), ZIndex = 0, Identifier = "A" };
            var b = new CanvasObject { Bounds = new Rect(140, 90, 220, 160), ZIndex = 1, Identifier = "B" };
            var c = new CanvasObject { Bounds = new Rect(280, 40, 140, 200), ZIndex = 2, Identifier = "C" };

            editorCanvas.ObjectManager.Add(a);
            editorCanvas.ObjectManager.Add(b);
            editorCanvas.ObjectManager.Add(c);

            editorCanvas.SelectionChanged += OnSelectionChanged;
            editorCanvas.ToolChanged += OnToolChanged;
            // connect layer manager control
            try
            {
                layerManager.Renderer = editorCanvas.LayeredRenderer;
            }
            catch { }

            // update demo when transform changes (zoom/pan)
            if (editorCanvas.TransformService != null)
            {
                editorCanvas.TransformService.TransformChanged += (s, ea) => UpdateTransformStatus(editorCanvas.TransformService);
                UpdateTransformStatus(editorCanvas.TransformService);
            }

            editorCanvas.LayerManagerToggleRequested += () => layerManager.Visibility = layerManager.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            editorCanvas.AutoReturnToSelection = chkAutoReturn.IsChecked == true;

            UpdateStatus(editorCanvas.ToolManager?.ActiveTool?.Name, editorCanvas.SelectedObjects?.Count);

            // Force a redraw
            editorCanvas.InvalidateVisual();
        }

        private void UpdateTransformStatus(AnnoDesigner.Controls.EditorCanvas.Core.ITransformService? svc)
        {
            if (svc == null) return;
            Dispatcher?.Invoke(() =>
            {
                txtStatusZoom.Text = $"Zoom: {svc.Zoom:0.00}x";
                var pan = svc.Pan;
                txtStatusPan.Text = $"Pan: ({pan.X:0},{pan.Y:0})";
            });
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var rand = new System.Random();
            double x = rand.Next(20, 600);
            double y = rand.Next(20, 400);
            double w = rand.Next(60, 240);
            double h = rand.Next(40, 200);
            int z = rand.Next(0, 5);
            var obj = new CanvasObject { Bounds = new Rect(x, y, w, h), ZIndex = z, Identifier = System.Guid.NewGuid().ToString() };
            editorCanvas.ObjectManager.Add(obj);
            editorCanvas.InvalidateVisual();
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            var sel = editorCanvas.SelectedObject;
            if (sel != null)
            {
                editorCanvas.ObjectManager.Remove(sel);
                editorCanvas.ClearSelection();
                editorCanvas.InvalidateVisual();
            }
        }

        private void ChkGrid_Changed(object sender, RoutedEventArgs e)
        {
            editorCanvas?.ShowGrid = chkGrid.IsChecked == true;
            editorCanvas?.InvalidateVisual();
        }

        private void ChkGuides_Changed(object sender, RoutedEventArgs e)
        {
            editorCanvas?.ShowGuides = chkGuides.IsChecked == true;
            editorCanvas?.InvalidateVisual();
        }

        private void ChkAutoReturn_Changed(object sender, RoutedEventArgs e)
        {
            if (editorCanvas != null)
            {
                editorCanvas.AutoReturnToSelection = chkAutoReturn.IsChecked == true;
            }
        }

        private void OnToolButtonClick(object sender, RoutedEventArgs e)
        {
            var tag = (sender as FrameworkElement)?.Tag as string;
            if (string.IsNullOrWhiteSpace(tag) || editorCanvas?.ToolManager == null)
            {
                return;
            }

            if (editorCanvas.ToolManager.Activate(tag))
            {
                UpdateStatus(tag, editorCanvas.SelectedObjects?.Count);
            }
        }

        private void OnSelectionChanged(IReadOnlyList<CanvasObject> selected)
        {
            UpdateStatus(editorCanvas.ToolManager?.ActiveTool?.Name, selected?.Count);
        }

        private void OnToolChanged(AnnoDesigner.Controls.EditorCanvas.Tooling.ITool? tool)
        {
            UpdateStatus(tool?.Name, editorCanvas.SelectedObjects?.Count);
        }

        private void UpdateStatus(string toolName, int? selectionCount)
        {
            var resolvedTool = string.IsNullOrWhiteSpace(toolName) ? "None" : toolName;
            txtStatusTool.Text = $"Tool: {resolvedTool}";

            var count = selectionCount ?? 0;
            txtStatusSelection.Text = $"Selection: {count}";

            if (_toolHints.TryGetValue(resolvedTool, out var hint))
            {
                txtStatusHint.Text = hint;
            }
            else
            {
                txtStatusHint.Text = DefaultHint;
            }
        }
    }
}
