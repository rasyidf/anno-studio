using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AnnoDesigner.Controls.EditorCanvas.Content.Models;

namespace AnnoDesigner.Controls.EditorCanvas.Tooling
{
    /// <summary>
    /// Places preset-sized rectangular objects on the grid.
    /// Activated programmatically when a building preset is selected.
    /// Stays active for continuous placement until cancelled (right-click or Escape).
    /// </summary>
    public class PlacementTool : ITool
    {
        public string Name => "Placement";

        private readonly IInputElement _owner;
        private readonly Content.IObjectManager<CanvasObject> _objectManager;
        private readonly Action<IEnumerable<CanvasObject>> _setSelection;
        private readonly Action _invalidate;

        private Size _templateSize;
        private string _templateIdentifier = string.Empty;
        private bool _hasTemplate;
        private Point? _ghostPosition;
        private bool _canPlaceCurrent;

        public PlacementTool(
            Content.IObjectManager<CanvasObject> objectManager,
            IInputElement owner,
            Action<IEnumerable<CanvasObject>> setSelection,
            Action invalidate)
        {
            _objectManager = objectManager ?? throw new ArgumentNullException(nameof(objectManager));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _setSelection = setSelection ?? throw new ArgumentNullException(nameof(setSelection));
            _invalidate = invalidate ?? throw new ArgumentNullException(nameof(invalidate));
        }

        /// <summary>
        /// Configure what will be placed next.
        /// </summary>
        public void SetTemplate(Size size, string identifier)
        {
            _templateSize = size;
            _templateIdentifier = identifier ?? string.Empty;
            _templateObject = null;
            _hasTemplate = true;
            _ghostPosition = null;
        }

        /// <summary>
        /// Sets a full CanvasObject as the placement template (preserves color, icon, label, etc.)
        /// </summary>
        public void SetTemplate(CanvasObject template)
        {
            if (template == null) return;
            _templateSize = template.Bounds.Size;
            _templateIdentifier = template.Identifier ?? string.Empty;
            _templateObject = template;
            _hasTemplate = true;
            _ghostPosition = null;
        }

        private CanvasObject _templateObject;

        private EditorCanvas Canvas => _owner as EditorCanvas;

        private Point SnapToGrid(Point worldPoint)
        {
            return Canvas?.TransformService?.SnapToGrid(worldPoint) ?? worldPoint;
        }

        private Point ToWorld(Point screenPoint)
        {
            return Canvas?.ScreenToWorld(screenPoint) ?? screenPoint;
        }

        private bool ValidatePlacement(CanvasObject obj)
        {
            return Canvas?.PlacementValidator?.CanPlace(obj) ?? true;
        }

        public void Activate()
        {
            _ghostPosition = null;
            _canPlaceCurrent = true;
        }

        public void Deactivate()
        {
            _ghostPosition = null;
            _hasTemplate = false;
        }

        public void OnCancel()
        {
            _ghostPosition = null;
            _invalidate();
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e == null || !_hasTemplate) return;

            if (e.ChangedButton == MouseButton.Right)
            {
                // Cancel placement
                OnCancel();
                Canvas?.ToolManager?.DeactivateActive();
                return;
            }

            if (e.ChangedButton != MouseButton.Left) return;

            var world = ToWorld(e.GetPosition(_owner));
            var snapped = SnapToGrid(world);
            var bounds = new Rect(snapped, _templateSize);

            CanvasObject obj;
            if (_templateObject != null)
            {
                obj = _templateObject.Clone();
                obj.Bounds = bounds;
            }
            else
            {
                obj = new CanvasObject
                {
                    Bounds = bounds,
                    Identifier = _templateIdentifier,
                    ShapeType = "Rectangle",
                    IsSelectable = true
                };
            }

            bool force = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (!force && !ValidatePlacement(obj))
            {
                // ponytail: invalid placement, do nothing — ghost already shown red
                return;
            }

            if (Canvas != null) Canvas.AddObjectWithUndo(obj);
            else _objectManager.Add(obj);
            _setSelection(new[] { obj });
            _invalidate();
            // Stay active for continuous placement
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (e == null || !_hasTemplate) return;

            var world = ToWorld(e.GetPosition(_owner));
            _ghostPosition = SnapToGrid(world);

            // Pre-validate for ghost color
            var candidate = new CanvasObject
            {
                Bounds = new Rect(_ghostPosition.Value, _templateSize),
                Identifier = _templateIdentifier,
                ShapeType = "Rectangle",
                IsSelectable = true
            };
            _canPlaceCurrent = ValidatePlacement(candidate);

            _invalidate();
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            // Placement happens on MouseDown for snappier feel
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e == null) return;

            if (e.Key == Key.Escape)
            {
                OnCancel();
                Canvas?.ToolManager?.DeactivateActive();
                e.Handled = true;
            }
        }

        public void OnKeyUp(KeyEventArgs e)
        {
            // no-op
        }

        public void Render(DrawingContext dc)
        {
            if (!_hasTemplate || !_ghostPosition.HasValue) return;

            var ghostRect = new Rect(_ghostPosition.Value, _templateSize);

            Brush brush;
            Pen pen;

            if (_canPlaceCurrent)
            {
                brush = new SolidColorBrush(Color.FromArgb(60, 0, 120, 255));
                pen = new Pen(Brushes.DodgerBlue, 1) { DashStyle = DashStyles.Dash };
            }
            else
            {
                brush = new SolidColorBrush(Color.FromArgb(60, 255, 40, 40));
                pen = new Pen(Brushes.Red, 1) { DashStyle = DashStyles.Dash };
            }

            dc.DrawRectangle(brush, pen, ghostRect);
        }
    }
}
