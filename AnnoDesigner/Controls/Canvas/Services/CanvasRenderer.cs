using System;
using System.Collections.Generic;
using System.Linq; 
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media; 
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Extensions;
using AnnoDesigner.Helper;
using AnnoDesigner.Models; 

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal class CanvasRenderer : ICanvasRenderer
    {
        private readonly AnnoCanvas _canvas;

        private DrawingGroup _drawingGroupPanoramaText = new DrawingGroup();
        private DrawingGroup _drawingGroupGridLines = new DrawingGroup();
        private DrawingGroup _drawingGroupObjects = new DrawingGroup();
        private DrawingGroup _drawingGroupSelectedObjectsInfluence = new DrawingGroup();
        private DrawingGroup _drawingGroupInfluence = new DrawingGroup();

        private Rect _lastViewPortAbsolute;
        private List<LayoutObject> _lastObjectsToDraw = [];
        private List<LayoutObject> _lastBorderlessObjectsToDraw = [];
        private List<LayoutObject> _lastBorderedObjectsToDraw = [];
        private QuadTree<LayoutObject> _lastPlacedObjects;



        private int _lastGridSize = -1;
        private double _lastGridWidth = -1;
        private double _lastGridHeight = -1;
        private DrawingGroup _drawingGroupObjectSelection = new DrawingGroup();
        private ICollection<LayoutObject> _lastSelectedObjects = new List<LayoutObject>();
        private int _lastObjectSelectionGridSize = -1;
        private Rect _lastSelectionRect = Rect.Empty;

        public CanvasRenderer(AnnoCanvas canvas)
        {
            _canvas = canvas;
        }

        public void Render(DrawingContext drawingContext)
        {
            // For the first extraction step we delegate rendering back to the control's
            // internal RenderCore method. Later we'll move logic here and remove RenderCore.
            this.RenderCore(drawingContext);
        }
        /// <summary>
        /// Internal rendering core. This is the original OnRender body moved here so it can be
        /// invoked by CanvasRenderer during migration. Future work will move this logic into
        /// the renderer implementation.
        /// </summary>
        /// <param name="drawingContext">context used for rendering</param>
        private void RenderCore(DrawingContext drawingContext)
        {
            var width = _canvas.RenderSize.Width;
            var height = _canvas.RenderSize.Height;
            _canvas._viewport.Width = _canvas._coordinateHelper.ScreenToGrid(width, _canvas._gridSize);
            _canvas._viewport.Height = _canvas._coordinateHelper.ScreenToGrid(height, _canvas._gridSize);

            if (_canvas.ScrollOwner != null && _canvas._invalidateScrollInfo)
            {
                _canvas.ScrollOwner?.InvalidateScrollInfo();
                _canvas._invalidateScrollInfo = false;
            }

            //use the negated value for the transform, as when we move the viewport (for example, if Top gets
            //increased by 1) we want the items to "shift" in the opposite direction to the movement of the viewport:
            /*
             |  +=+ = viewport
             |  [] = object
             |
             |  Object on edge of viewport.
             |
             |  1 +==[]=+
             |  2 |     |
             |  3 +=====+
             |  4
             |
             |  Viewport shifts down
             |
             |  1    []
             |  2 +=====+
             |  3 |     |
             |  4 +=====+
             |
             |  Relative to the viewport, the object has been shifted "up".
             */
            _canvas._viewportTransform.X = _canvas._coordinateHelper.GridToScreen(-_canvas._viewport.Left, _canvas._gridSize);
            _canvas._viewportTransform.Y = _canvas._coordinateHelper.GridToScreen(-_canvas._viewport.Top, _canvas._gridSize);

            // assure pixel perfect drawing using guidelines.
            // this value is cached and refreshed in LoadGridLineColor(), as it uses pen thickness in its calculation;
            drawingContext.PushGuidelineSet(_canvas._guidelineSet);

            // draw background
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, width, height));

            // delegate grid drawing & caching to renderer
            DrawGrid(drawingContext, width, height, _canvas._viewport.HorizontalAlignmentValue, _canvas._viewport.VerticalAlignmentValue, _canvas._gridSize, _canvas._isRenderingForced, _canvas._gridLinePen, _canvas._guidelineSet);

            //Push the transform after rendering everything that should not be translated.
            drawingContext.PushTransform(_canvas._viewportTransform);

            var viewPortAbsolute = _canvas._viewport.Absolute; //hot path optimization
            var objectsToDraw = _lastObjectsToDraw;
            var borderlessObjects = _lastBorderlessObjectsToDraw;
            var borderedObjects = _lastBorderedObjectsToDraw;
            var objectsChanged = false;

            if (_canvas._isRenderingForced ||
                _lastViewPortAbsolute != viewPortAbsolute ||
                _lastPlacedObjects != _canvas.PlacedObjects ||
                _canvas.CurrentMode == MouseMode.PlaceObjects ||
                _canvas.CurrentMode == MouseMode.DeleteObject)
            {
                objectsToDraw = [.. _canvas.PlacedObjects.GetItemsIntersecting(viewPortAbsolute)];
                _lastObjectsToDraw = objectsToDraw;
                _lastPlacedObjects = _canvas.PlacedObjects;
                _lastViewPortAbsolute = viewPortAbsolute;

                borderlessObjects = [.. objectsToDraw.Where(_ => _.WrappedAnnoObject.Borderless)];
                _lastBorderlessObjectsToDraw = borderlessObjects;
                borderedObjects = [.. objectsToDraw.Where(_ => !_.WrappedAnnoObject.Borderless)];
                _lastBorderedObjectsToDraw = borderedObjects;

                //quick fix deleting objects via keyboard instead of right click
                if (_canvas.CurrentMode == MouseMode.DeleteObject)
                {
                    _canvas.CurrentMode = MouseMode.Standard;
                }

                objectsChanged = true;
            }

            // delegate placed-objects drawing + caching to renderer
            DrawPlacedObjects(drawingContext, borderlessObjects, borderedObjects, _canvas._isRenderingForced || objectsChanged, _canvas._guidelineSet, _canvas._gridSize, _canvas._linePen, _canvas.RenderHarborBlockedArea, _canvas.RenderIcon, _canvas.RenderLabel, _canvas.Icons, _canvas.TYPEFACE, App.DpiScale.PixelsPerDip, _canvas._debugModeIsEnabled, _canvas._debugShowObjectPositions, _canvas._debugBrushLight);

            bool selectionWasRedrawn;
            // draw object selection around not ignored selected objects (renderer owns caching)
            if (_canvas.selectionContainsNotIgnoredObject)
            {
                selectionWasRedrawn = DrawSelectionGroup(drawingContext, _canvas.SelectedObjects.WithoutIgnoredObjects(), _canvas._isRenderingForced, _canvas._guidelineSet, _canvas._gridSize, _canvas._highlightPen, _canvas.CurrentMode == MouseMode.DragSelection, _canvas._selectionRect);
            }
            else
            {
                // except when only ignored objects are selected, in which case render their selection
                selectionWasRedrawn = DrawSelectionGroup(drawingContext, _canvas.SelectedObjects, _canvas._isRenderingForced, _canvas._guidelineSet, _canvas._gridSize, _canvas._highlightPen, _canvas.CurrentMode == MouseMode.DragSelection, _canvas._selectionRect);
            }

            if (_canvas.RenderPanorama)
            {
                DrawPanoramaText(drawingContext, objectsToDraw,
                 forceRedraw: _canvas._isRenderingForced || objectsChanged,
                 AnnoCanvas.IDENTIFIER_SKYSCRAPER, _canvas._regex_panorama, _canvas.TYPEFACE, _canvas.FontSize, App.DpiScale.PixelsPerDip, _canvas._guidelineSet, _canvas._coordinateHelper);

            }

            if (!_canvas.RenderInfluences)
            {
                if (!_canvas._hideInfluenceOnSelection)
                {
                    if (selectionWasRedrawn || _canvas._isRenderingForced)
                    {
                        // renderer takes care of caching & drawing selected-object influence visuals
                        DrawSelectedObjectsInfluence(drawingContext, _canvas.SelectedObjects, selectionWasRedrawn || _canvas._isRenderingForced, _canvas._guidelineSet, _canvas._gridSize, _canvas.RenderTrueInfluenceRange, _canvas.PlacedObjects, _canvas._influencedBrush, _canvas._influencedPen, _canvas._lightBrush, _canvas._radiusPen, _canvas._coordinateHelper);
                    }
                }
            }
            else
            {
                if (objectsChanged || _canvas._isRenderingForced)
                {
                    // renderer owns the influence group cache + drawing
                    DrawInfluenceGroup(drawingContext, objectsToDraw, viewPortAbsolute, objectsChanged || _canvas._isRenderingForced, _canvas._guidelineSet, _canvas._gridSize, _canvas.RenderTrueInfluenceRange, _canvas.PlacedObjects, _canvas._influencedBrush, _canvas._influencedPen, _canvas._lightBrush, _canvas._radiusPen, _canvas._coordinateHelper);
                }
            }

            if (_canvas.CurrentObjects.Count == 0)
            {
                // highlight object which is currently hovered, but not if some objects are being dragged
                if (_canvas.CurrentMode != MouseMode.DragSelection)
                {
                    var hoveredObj = _canvas.GetObjectAt(_canvas._mousePosition);
                    if (hoveredObj != null)
                    {
                        DrawHoverHighlight(drawingContext, hoveredObj, _canvas._highlightPen, _canvas._gridSize);
                    }
                }
            }
            else
            {
                // draw current object
                if (_canvas._mouseWithinControl)
                {
                    //Push a tranform to reverse the effects, as objects should be positioned correctly
                    //on the canvas with the included viewport offset, but we want them to render without the offset.
                    //If we just did drawingContext.Pop() here, the items would appear offset compared to where the mouse is, 
                    //as the Position of the objects have already been set to values relative to the viewport.
                    drawingContext.PushTransform(_canvas._viewportTransform.Inverse as TranslateTransform);

                    MoveCurrentObjectsToMouse();
                    // draw influence radius
                    RenderObjectInfluenceRadius(drawingContext, _canvas.CurrentObjects);
                    // draw influence range
                    RenderObjectInfluenceRange(drawingContext, _canvas.CurrentObjects);
                    // draw with transparency
                    RenderObjectList(drawingContext, _canvas.CurrentObjects, useTransparency: true);
                    drawingContext.Pop();
                }

            }
            //pop viewport transform
            drawingContext.Pop();

            // draw selection rect while dragging the mouse
            if (_canvas.CurrentMode == MouseMode.SelectionRect)
            {
                DrawSelectionRect(drawingContext, _canvas._lightBrush, _canvas._highlightPen, _canvas._selectionRect);
            }

            // draw debug information
            DrawDebugInfo(drawingContext, _canvas._viewportTransform, _canvas._guidelineSet, _canvas._gridSize,
                _canvas._debugModeIsEnabled, _canvas._debugShowQuadTreeViz, _canvas._penCache.GetPen(_canvas._debugBrushDark, 2),
                _canvas._debugBrushLight, _canvas._debugShowSelectionCollisionRect, _canvas._collisionRect, _canvas._coordinateHelper,
                _canvas._debugShowViewportRectCoordinates, _canvas._viewport.Absolute, App.DpiScale.PixelsPerDip, _canvas.TYPEFACE,
                _canvas._debugShowScrollableRectCoordinates, _canvas._scrollableBounds, _canvas._debugShowLayoutRectCoordinates,
                _canvas._layoutBounds, _canvas._debugShowObjectCount, _canvas.PlacedObjects.Count, _canvas._debugShowMouseGridCoordinates,
                _canvas._mousePosition, _canvas.CurrentMode == MouseMode.SelectionRect, _canvas._selectionRect,
                _canvas._debugShowSelectionRectCoordinates, _canvas._brushCache, _canvas._penCache, _canvas._viewport, _canvas._debugBrushDark);

            // pop back guidlines set
            drawingContext.Pop();

            _canvas._isRenderingForced = false;
        }




        public void DrawObjectSelection(DrawingContext drawingContext, ICollection<LayoutObject> objects, System.Windows.Media.Pen highlightPen, int gridSize)
        {
            foreach (var curLayoutObject in objects)
            {
                drawingContext.DrawRectangle(null, highlightPen, curLayoutObject.CalculateScreenRect(gridSize));
            }
        }

        public bool DrawSelectionGroup(DrawingContext drawingContext, ICollection<LayoutObject> objects, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, System.Windows.Media.Pen highlightPen, bool isDragSelection, Rect selectionRect)
        {
            var wasRedrawn = false;

            if (_lastSelectionRect == selectionRect && objects.Count == 0)
            {
                return wasRedrawn;
            }

            if (forceRedraw || _lastSelectedObjects != objects || _lastObjectSelectionGridSize != gridSize || isDragSelection || _lastSelectionRect != selectionRect)
            {
                if (_drawingGroupObjectSelection.IsFrozen)
                {
                    _drawingGroupObjectSelection = new DrawingGroup();
                }

                var context = _drawingGroupObjectSelection.Open();
                context.PushGuidelineSet(guidelineSet);

                // draw selection visuals via renderer
                DrawObjectSelection(context, objects, highlightPen, gridSize);

                context.Close();

                _lastObjectSelectionGridSize = gridSize;
                _lastSelectedObjects = objects;
                _lastSelectionRect = selectionRect;
                wasRedrawn = true;

                if (_drawingGroupObjectSelection.CanFreeze)
                {
                    _drawingGroupObjectSelection.Freeze();
                }
            }

            drawingContext.DrawDrawing(_drawingGroupObjectSelection);

            return wasRedrawn;
        }

        public void DrawHoverHighlight(DrawingContext drawingContext, LayoutObject hoveredObject, System.Windows.Media.Pen highlightPen, int gridSize)
        {
            if (hoveredObject == null)
            {
                return;
            }

            drawingContext.DrawRectangle(null, highlightPen, hoveredObject.CalculateScreenRect(gridSize));
        }

        public void DrawGrid(DrawingContext drawingContext, double width, double height, double horizontalAlignmentValue, double verticalAlignmentValue, int gridSize, bool forceRedraw, System.Windows.Media.Pen gridLinePen, GuidelineSet guidelineSet)
        {
            if (!_canvas.RenderGrid)
            {
                return;
            }

            if (forceRedraw || gridSize != _lastGridSize || width != _lastGridWidth || height != _lastGridHeight)
            {
                if (_drawingGroupGridLines.IsFrozen)
                {
                    _drawingGroupGridLines = new DrawingGroup();
                }

                var context = _drawingGroupGridLines.Open();
                context.PushGuidelineSet(guidelineSet);

                // vertical lines
                for (var i = horizontalAlignmentValue * gridSize; i < width; i += gridSize)
                {
                    context.DrawLine(gridLinePen, new Point(i, 0), new Point(i, height));
                }

                // horizontal lines
                for (var i = verticalAlignmentValue * gridSize; i < height; i += gridSize)
                {
                    context.DrawLine(gridLinePen, new Point(0, i), new Point(width, i));
                }

                context.Close();

                _lastGridSize = gridSize;
                _lastGridWidth = width;
                _lastGridHeight = height;

                if (_drawingGroupGridLines.CanFreeze)
                {
                    _drawingGroupGridLines.Freeze();
                }
            }

            drawingContext.DrawDrawing(_drawingGroupGridLines);
        }


        /// <summary>
        /// Renders the given AnnoObject to the given DrawingContext.
        /// </summary>
        /// <param name="drawingContext">context used for rendering</param>
        internal void RenderObjectList(DrawingContext drawingContext, List<LayoutObject> objects, bool useTransparency)
        {
            // Delegate object drawing to renderer and pass required parameters
            DrawObjectList(
                drawingContext,
                objects,
                useTransparency,
                _canvas.GridSize,
                _canvas._linePen,
                _canvas.RenderHarborBlockedArea,
                _canvas.RenderIcon,
                _canvas.RenderLabel,
                _canvas.Icons,
                _canvas.TYPEFACE,
                App.DpiScale.PixelsPerDip,
                _canvas._debugModeIsEnabled,
                _canvas._debugShowObjectPositions,
                _canvas._debugBrushLight);
        }





        /// <summary>
        /// Renders the influence radius of the given object and highlights other objects within range.
        /// </summary>
        /// <param name="drawingContext">context used for rendering</param>
        internal void RenderObjectInfluenceRadius(DrawingContext drawingContext, ICollection<LayoutObject> objects)
        {
            if (objects.Count == 0)
            {
                return;
            }

            // Delegate influence drawing to renderer (keeps existing logic and caches intact)
            DrawObjectInfluenceRadius(drawingContext, objects, _canvas._influencedBrush, _canvas._influencedPen, _canvas._lightBrush, _canvas._radiusPen, _canvas.GridSize);
        }

        /// <summary>
        /// Renders influence range of the given objects.
        /// If RenderTrueInfluenceRange is set to true, true influence range will be rendered and objects inside will be highlighted.
        /// Else maximum influence range will be rendered.
        /// </summary>
        internal void RenderObjectInfluenceRange(DrawingContext drawingContext, ICollection<LayoutObject> objects)
        {
            // Delegate heavy influence-range rendering into the renderer
            DrawObjectInfluenceRange(drawingContext, objects, _canvas.GridSize, _canvas.RenderTrueInfluenceRange, _canvas.PlacedObjects, _canvas._influencedBrush, _canvas._influencedPen, _canvas._lightBrush, _canvas._radiusPen, _canvas._coordinateHelper);
        }

        /// <summary>
        /// 
        /// </summary>
        public void DrawObjectList(DrawingContext drawingContext, List<LayoutObject> objects, bool useTransparency, int gridSize, System.Windows.Media.Pen linePen, bool renderHarborBlockedArea, bool renderIcon, bool renderLabel, Dictionary<string, IconImage> icons, Typeface typeface, double pixelsPerDip, bool debugModeEnabled, bool debugShowObjectPositions, System.Windows.Media.Brush debugBrushLight)
        {
            if (objects == null || objects.Count == 0)
            {
                return;
            }

            var linePenThickness = linePen.Thickness; // hot path optimization

            foreach (var curLayoutObject in objects)
            {
                var obj = curLayoutObject.WrappedAnnoObject;

                // draw object rectangle
                var objRect = curLayoutObject.CalculateScreenRect(gridSize);

                var brush = useTransparency ? curLayoutObject.TransparentBrush : curLayoutObject.RenderBrush;

                var borderPen = obj.Borderless ? curLayoutObject.GetBorderlessPen(brush, linePenThickness) : linePen;
                drawingContext.DrawRectangle(brush, borderPen, objRect);
                if (renderHarborBlockedArea)
                {
                    var objBlockedRect = curLayoutObject.CalculateBlockedScreenRect(gridSize);
                    if (objBlockedRect.HasValue)
                    {
                        drawingContext.DrawRectangle(curLayoutObject.BlockedAreaBrush, borderPen, objBlockedRect.Value);
                    }
                }

                // draw object icon if it is at least 2x2 cells
                var iconRendered = false;
                if (renderIcon && !string.IsNullOrEmpty(obj.Icon))
                {
                    var iconFound = false;

                    if (curLayoutObject.Icon is null)
                    {
                        var iconName = curLayoutObject.IconNameWithoutExtension;
                        if (icons.TryGetValue(iconName, out var iconImage))
                        {
                            curLayoutObject.Icon = iconImage;
                            iconFound = true;
                        }
                        else
                        {
                            var message = $"Icon file missing ({iconName}).";
                            // log or set status on canvas if desired
                        }
                    }
                    else
                    {
                        iconFound = true;
                    }

                    if (iconFound)
                    {
                        var iconRect = curLayoutObject.GetIconRect(gridSize);

                        drawingContext.DrawImage(curLayoutObject.Icon.Icon, iconRect);
                        iconRendered = true;
                    }
                }

                // draw object label
                if (renderLabel && !string.IsNullOrEmpty(obj.Label))
                {
                    var textAlignment = iconRendered ? TextAlignment.Left : TextAlignment.Center;
                    var text = curLayoutObject.GetFormattedText(textAlignment, Thread.CurrentThread.CurrentCulture,
                        typeface, pixelsPerDip, objRect.Width, objRect.Height);

                    var textLocation = objRect.TopLeft;
                    if (iconRendered)
                    {
                        textLocation.X += 3;
                        textLocation.Y += 2;
                    }
                    else
                    {
                        textLocation.Y += (objRect.Height - text.Height) / 2;
                    }

                    drawingContext.DrawText(text, textLocation);
                }

                if (debugModeEnabled && debugShowObjectPositions)
                {
                    var text = new FormattedText(obj.Position.ToString(), Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                        typeface, 12, debugBrushLight,
                        null, TextFormattingMode.Display, pixelsPerDip)
                    {
                        MaxTextWidth = objRect.Width,
                        MaxTextHeight = objRect.Width,
                        TextAlignment = TextAlignment.Left
                    };
                    var textLocation = objRect.BottomRight;
                    textLocation.X -= text.Width;
                    textLocation.Y -= text.Height;

                    drawingContext.DrawText(text, textLocation);
                }
            }
        }

        public void DrawPlacedObjects(DrawingContext drawingContext, List<LayoutObject> borderlessObjects, List<LayoutObject> borderedObjects, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, System.Windows.Media.Pen linePen, bool renderHarborBlockedArea, bool renderIcon, bool renderLabel, Dictionary<string, IconImage> icons, Typeface typeface, double pixelsPerDip, bool debugModeEnabled, bool debugShowObjectPositions, System.Windows.Media.Brush debugBrushLight)
        {
            if ((borderlessObjects == null || borderlessObjects.Count == 0) && (borderedObjects == null || borderedObjects.Count == 0))
            {
                return;
            }

            if (forceRedraw || _drawingGroupObjects.IsFrozen)
            {
                if (_drawingGroupObjects.IsFrozen)
                {
                    _drawingGroupObjects = new DrawingGroup();
                }

                var context = _drawingGroupObjects.Open();
                context.PushGuidelineSet(guidelineSet);

                // draw borderless first
                DrawObjectList(context, borderlessObjects, useTransparency: false, gridSize, linePen, renderHarborBlockedArea, renderIcon, renderLabel, icons, typeface, pixelsPerDip, debugModeEnabled, debugShowObjectPositions, debugBrushLight);

                // then bordered objects
                DrawObjectList(context, borderedObjects, useTransparency: false, gridSize, linePen, renderHarborBlockedArea, renderIcon, renderLabel, icons, typeface, pixelsPerDip, debugModeEnabled, debugShowObjectPositions, debugBrushLight);

                context.Close();

                if (_drawingGroupObjects.CanFreeze)
                {
                    _drawingGroupObjects.Freeze();
                }
            }

            drawingContext.DrawDrawing(_drawingGroupObjects);
        }

        public void DrawObjectInfluenceRadius(DrawingContext drawingContext, ICollection<LayoutObject> objects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, int gridSize)
        {
            if (objects.Count == 0)
            {
                return;
            }

            foreach (var curLayoutObject in objects)
            {
                if (curLayoutObject.WrappedAnnoObject.Radius >= 0.5)
                {
                    var radius = curLayoutObject.GetScreenRadius(gridSize);
                    var circle = curLayoutObject.GetInfluenceCircle(gridSize, radius);

                    var circleCenterX = circle.Center.X;
                    var circleCenterY = circle.Center.Y;

                    var influenceGridRect = curLayoutObject.GridInfluenceRadiusRect;

                    foreach (var curPlacedObject in _canvas.PlacedObjects.GetItemsIntersecting(influenceGridRect).WithoutIgnoredObjects())
                    {
                        var distance = curPlacedObject.GetScreenRectCenterPoint(gridSize);
                        distance.X -= circleCenterX;
                        distance.Y -= circleCenterY;
                        if ((distance.X * distance.X) + (distance.Y * distance.Y) <= radius * radius)
                        {
                            drawingContext.DrawRectangle(influencedBrush, influencedPen, curPlacedObject.CalculateScreenRect(gridSize));
                        }
                    }

                    drawingContext.DrawGeometry(lightBrush, radiusPen, circle);
                }
            }
        }

        public void DrawSelectedObjectsInfluence(DrawingContext drawingContext, ICollection<LayoutObject> selectedObjects, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, bool renderTrueInfluenceRange, Core.DataStructures.QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper)
        {
            if (!forceRedraw && _drawingGroupSelectedObjectsInfluence != null && !_drawingGroupSelectedObjectsInfluence.IsFrozen)
            {
                // not ready to refresh; fallback to redraw
            }

            if (forceRedraw || _drawingGroupSelectedObjectsInfluence.IsFrozen)
            {
                if (_drawingGroupSelectedObjectsInfluence.IsFrozen)
                {
                    _drawingGroupSelectedObjectsInfluence = new DrawingGroup();
                }

                var context = _drawingGroupSelectedObjectsInfluence.Open();
                context.PushGuidelineSet(guidelineSet);

                DrawObjectInfluenceRadius(context, selectedObjects, influencedBrush, influencedPen, lightBrush, radiusPen, gridSize);
                DrawObjectInfluenceRange(context, selectedObjects, gridSize, renderTrueInfluenceRange, placedObjects, influencedBrush, influencedPen, lightBrush, radiusPen, coordinateHelper);

                context.Close();

                if (_drawingGroupSelectedObjectsInfluence.CanFreeze)
                {
                    _drawingGroupSelectedObjectsInfluence.Freeze();
                }
            }

            if (selectedObjects.Count > 0)
            {
                drawingContext.DrawDrawing(_drawingGroupSelectedObjectsInfluence);
            }
        }

        public void DrawObjectInfluenceRange(DrawingContext drawingContext, ICollection<LayoutObject> objects, int gridSize, bool renderTrueInfluenceRange, Core.DataStructures.QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper)
        {
            if (objects.Count == 0 || !_canvas.RenderInfluences)
            {
                // for selected/current objects we still want to show influence
                if (_canvas.SelectedObjects.Count == 0 && _canvas.CurrentObjects.Count == 0)
                {
                    return;
                }
            }

            Moved2DArray<AnnoObject> gridDictionary = null;
            List<AnnoObject> placedAnnoObjects = null;

            if (renderTrueInfluenceRange && placedObjects.Count > 0)
            {
                var set = placedObjects.Concat(objects).ToHashSet();
                placedAnnoObjects = [.. set.Select(o => o.WrappedAnnoObject)];
                var placedObjectDictionary = set.ToDictionaryWithCapacity(o => o.WrappedAnnoObject);

                void Highlight(AnnoObject objectInRange)
                {
                    drawingContext.DrawRectangle(influencedBrush, influencedPen, placedObjectDictionary[objectInRange].CalculateScreenRect(gridSize));
                }

                gridDictionary = RoadSearchHelper.PrepareGridDictionary(placedAnnoObjects);
                _ = RoadSearchHelper.BreadthFirstSearch(
                    placedAnnoObjects,
                    objects.Select(o => o.WrappedAnnoObject).Where(o => o.InfluenceRange > 0.5),
                    o => (int)o.InfluenceRange + 1,
                    gridDictionary,
                    Highlight);
            }

            var geometries = new System.Collections.Concurrent.ConcurrentBag<(long index, StreamGeometry geometry)>();
            _ = System.Threading.Tasks.Parallel.ForEach(objects, (curLayoutObject, _, index) =>
            {
                if (curLayoutObject.WrappedAnnoObject.InfluenceRange > 0.5)
                {
                    var sg = new StreamGeometry();

                    using (var sgc = sg.Open())
                    {
                        if (renderTrueInfluenceRange)
                        {
                            DrawTrueInfluenceRangePolygon(curLayoutObject, sgc, gridDictionary, placedAnnoObjects, gridSize, coordinateHelper);
                        }
                        else
                        {
                            DrawInfluenceRangePolygon(curLayoutObject, sgc, gridSize, coordinateHelper);
                        }
                    }

                    if (sg.CanFreeze)
                    {
                        sg.Freeze();
                    }
                    geometries.Add((index, sg));
                }
            });

            foreach (var (_, geometry) in geometries.OrderBy(p => p.index))
            {
                drawingContext.DrawGeometry(lightBrush, radiusPen, geometry);
            }
        }

        public void DrawInfluenceGroup(DrawingContext drawingContext, ICollection<LayoutObject> objectsToDraw, Rect viewPortAbsolute, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, bool renderTrueInfluenceRange, Core.DataStructures.QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper)
        {
            if (objectsToDraw == null)
            {
                return;
            }

            if (forceRedraw || _drawingGroupInfluence.IsFrozen)
            {
                if (_drawingGroupInfluence.IsFrozen)
                {
                    _drawingGroupInfluence = new DrawingGroup();
                }

                var context = _drawingGroupInfluence.Open();
                context.PushGuidelineSet(guidelineSet);

                DrawObjectInfluenceRadius(context, objectsToDraw, influencedBrush, influencedPen, lightBrush, radiusPen, gridSize);
                DrawObjectInfluenceRange(context, objectsToDraw, gridSize, renderTrueInfluenceRange, placedObjects, influencedBrush, influencedPen, lightBrush, radiusPen, coordinateHelper);

                // retrieve offscreen objects whose influence affects viewport objects
                // use viewport passed in by caller (origin coordinates)
                var offscreenObjects = _canvas.PlacedObjects
                    .Where(_ => !viewPortAbsolute.Contains(_.GridRect) && (viewPortAbsolute.IntersectsWith(_.GridInfluenceRadiusRect) || viewPortAbsolute.IntersectsWith(_.GridInfluenceRangeRect)))
                    .ToList();

                DrawObjectInfluenceRadius(context, offscreenObjects, influencedBrush, influencedPen, lightBrush, radiusPen, gridSize);
                DrawObjectInfluenceRange(context, offscreenObjects, gridSize, renderTrueInfluenceRange, placedObjects, influencedBrush, influencedPen, lightBrush, radiusPen, coordinateHelper);

                context.Close();

                if (_drawingGroupInfluence.CanFreeze)
                {
                    _drawingGroupInfluence.Freeze();
                }
            }

            drawingContext.DrawDrawing(_drawingGroupInfluence);
        }

        private void DrawTrueInfluenceRangePolygon(LayoutObject curLayoutObject, StreamGeometryContext sgc, Moved2DArray<AnnoObject> gridDictionary, List<AnnoObject> placedAnnoObjects, int gridSize, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper)
        {
            var stroked = true;
            var smoothJoin = true;

            var geometryFill = true;
            var geometryStroke = true;

            var startObjects = new AnnoObject[1]
            {
                curLayoutObject.WrappedAnnoObject
            };

            var cellsInInfluenceRange = RoadSearchHelper.BreadthFirstSearch(
                placedAnnoObjects,
                startObjects,
                o => (int)o.InfluenceRange,
                gridDictionary);

            var points = PolygonBoundaryFinderHelper.GetBoundaryPoints(cellsInInfluenceRange);
            if (points.Count < 1)
            {
                return;
            }

            sgc.BeginFigure(coordinateHelper.GridToScreen(new Point(points[0].x + gridDictionary.Offset.x, points[0].y + gridDictionary.Offset.y), gridSize), geometryFill, geometryStroke);
            for (var i = 1; i < points.Count; i++)
            {
                sgc.LineTo(coordinateHelper.GridToScreen(new Point(points[i].x + gridDictionary.Offset.x, points[i].y + gridDictionary.Offset.y), gridSize), stroked, smoothJoin);
            }
        }

        private void DrawInfluenceRangePolygon(LayoutObject curLayoutObject, StreamGeometryContext sgc, int gridSize, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper)
        {
            var topLeftCorner = curLayoutObject.Position;
            var topRightCorner = new Point(curLayoutObject.Position.X + curLayoutObject.Size.Width, curLayoutObject.Position.Y);
            var bottomLeftCorner = new Point(curLayoutObject.Position.X, curLayoutObject.Position.Y + curLayoutObject.Size.Height);
            var bottomRightCorner = new Point(curLayoutObject.Position.X + curLayoutObject.Size.Width, curLayoutObject.Position.Y + curLayoutObject.Size.Height);

            var influenceRange = curLayoutObject.WrappedAnnoObject.InfluenceRange;

            var startPoint = new Point(topLeftCorner.X, topLeftCorner.Y - influenceRange);
            var stroked = true;
            var smoothJoin = true;

            var geometryFill = true;
            var geometryStroke = true;

            sgc.BeginFigure(coordinateHelper.GridToScreen(startPoint, gridSize), geometryFill, geometryStroke);

            // Draw in width of object
            sgc.LineTo(coordinateHelper.GridToScreen(new Point(topRightCorner.X, startPoint.Y), gridSize), stroked, smoothJoin);

            // quadrant 2
            startPoint = new Point(topRightCorner.X, topRightCorner.Y - influenceRange);
            var endPoint = new Point(topRightCorner.X + influenceRange, topRightCorner.Y);
            var currentPoint = new Point(startPoint.X, startPoint.Y);
            while (endPoint != currentPoint)
            {
                currentPoint = new Point(currentPoint.X, currentPoint.Y + 1);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
                currentPoint = new Point(currentPoint.X + 1, currentPoint.Y);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
            }

            startPoint = endPoint;
            sgc.LineTo(coordinateHelper.GridToScreen(new Point(startPoint.X, bottomRightCorner.Y), gridSize), stroked, smoothJoin);

            startPoint = new Point(startPoint.X, bottomRightCorner.Y);
            endPoint = new Point(bottomRightCorner.X, bottomRightCorner.Y + influenceRange);
            currentPoint = new Point(startPoint.X, startPoint.Y);
            while (endPoint != currentPoint)
            {
                currentPoint = new Point(currentPoint.X - 1, currentPoint.Y);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
                currentPoint = new Point(currentPoint.X, currentPoint.Y + 1);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
            }

            startPoint = endPoint;
            sgc.LineTo(coordinateHelper.GridToScreen(new Point(bottomLeftCorner.X, startPoint.Y), gridSize), stroked, smoothJoin);

            startPoint = new Point(bottomLeftCorner.X, startPoint.Y);
            endPoint = new Point(bottomLeftCorner.X - influenceRange, bottomRightCorner.Y);
            currentPoint = new Point(startPoint.X, startPoint.Y);
            while (endPoint != currentPoint)
            {
                currentPoint = new Point(currentPoint.X, currentPoint.Y - 1);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
                currentPoint = new Point(currentPoint.X - 1, currentPoint.Y);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
            }

            startPoint = endPoint;
            sgc.LineTo(coordinateHelper.GridToScreen(new Point(startPoint.X, topLeftCorner.Y), gridSize), stroked, smoothJoin);

            startPoint = new Point(startPoint.X, topLeftCorner.Y);
            endPoint = new Point(topLeftCorner.X, topLeftCorner.Y - influenceRange);
            currentPoint = new Point(startPoint.X, startPoint.Y);
            while (endPoint != currentPoint)
            {
                currentPoint = new Point(currentPoint.X + 1, currentPoint.Y);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
                currentPoint = new Point(currentPoint.X, currentPoint.Y - 1);
                sgc.LineTo(coordinateHelper.GridToScreen(currentPoint, gridSize), stroked, smoothJoin);
            }
        }

        public void DrawPanoramaText(DrawingContext drawingContext, List<LayoutObject> placedObjects, bool forceRedraw, string skyscraperIdentifier, Regex regexPanorama, Typeface typeface, double fontSize, double pixelsPerDip, GuidelineSet guidelineSet, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper)
        {
            if (placedObjects.Count == 0)
            {
                return;
            }

            if (!forceRedraw)
            {
                drawingContext.DrawDrawing(_drawingGroupPanoramaText);
                return;
            }

            if (_drawingGroupPanoramaText.IsFrozen)
            {
                _drawingGroupPanoramaText = new DrawingGroup();
            }

            var context = _drawingGroupPanoramaText.Open();
            context.PushGuidelineSet(guidelineSet);

            foreach (var curObject in placedObjects.FindAll(_ => _.Identifier.StartsWith(skyscraperIdentifier, StringComparison.OrdinalIgnoreCase)))
            {
                if (!regexPanorama.TryMatch(curObject.Identifier, out var match))
                {
                    continue;
                }

                var center = coordinateHelper.GetCenterPoint(curObject.GridRect);

                var tier = int.Parse(match.Groups["tier"].Value);
                var level = int.Parse(match.Groups["level"].Value);
                var radiusSquared = curObject.WrappedAnnoObject.Radius * curObject.WrappedAnnoObject.Radius;
                var panorama = level;

                foreach (var adjacentObject in _canvas.PlacedObjects.GetItemsIntersecting(curObject.GridInfluenceRadiusRect)
                    .Where(_ => _.Identifier.StartsWith(skyscraperIdentifier, StringComparison.OrdinalIgnoreCase)))
                {
                    if (adjacentObject == curObject)
                    {
                        continue;
                    }

                    if ((center - coordinateHelper.GetCenterPoint(adjacentObject.GridRect)).LengthSquared <= radiusSquared && regexPanorama.TryMatch(adjacentObject.Identifier, out var match2))
                    {
                        var tier2 = int.Parse(match2.Groups["tier"].Value);
                        var level2 = int.Parse(match2.Groups["level"].Value);
                        if (tier != tier2)
                        {
                            panorama += level >= level2 ? 1 : -1;
                        }
                        else
                        {
                            panorama += level > level2 ? 1 : -1;
                        }
                    }
                }

                if (curObject.LastPanorama != panorama || curObject.PanoramaText == null)
                {
                    curObject.LastPanorama = panorama;

                    var text = Math.Abs(panorama).ToString() + (panorama >= 0 ? "" : "-");

                    curObject.PanoramaText = new FormattedText(text, Thread.CurrentThread.CurrentUICulture,
                        FlowDirection.RightToLeft, typeface, fontSize, Brushes.Black, pixelsPerDip);
                }

                context.DrawText(curObject.PanoramaText, curObject.CalculateScreenRect(_canvas.GridSize).TopRight);
            }

            context.Close();

            if (_drawingGroupPanoramaText.CanFreeze)
            {
                _drawingGroupPanoramaText.Freeze();
            }

            drawingContext.DrawDrawing(_drawingGroupPanoramaText);
        }

        public void DrawCurrentObjects(DrawingContext drawingContext, TranslateTransform viewportTransform, GuidelineSet guidelineSet, int gridSize, bool renderTrueInfluenceRange, QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper, System.Windows.Media.Pen linePen, bool renderHarborBlockedArea, bool renderIcon, bool renderLabel, Dictionary<string, IconImage> icons, Typeface typeface, double pixelsPerDip, bool debugModeEnabled, bool debugShowObjectPositions, System.Windows.Media.Brush debugBrushLight, Point mousePosition, AnnoDesigner.Viewport viewport)
        {
            if (_canvas.CurrentObjects.Count == 0)
            {
                return;
            }

            // Push a transform to reverse the effects, as objects should be positioned correctly
            // on the canvas with the included viewport offset, but we want them to render without the offset.
            drawingContext.PushTransform(viewportTransform.Inverse as TranslateTransform);

            MoveCurrentObjectsToMouse();
            // draw influence radius
            DrawObjectInfluenceRadius(drawingContext, _canvas.CurrentObjects, influencedBrush, influencedPen, lightBrush, radiusPen, gridSize);
            // draw influence range
            DrawObjectInfluenceRange(drawingContext, _canvas.CurrentObjects, gridSize, renderTrueInfluenceRange, placedObjects, influencedBrush, influencedPen, lightBrush, radiusPen, coordinateHelper);
            // draw with transparency
            DrawObjectList(drawingContext, _canvas.CurrentObjects, useTransparency: true, gridSize, linePen, renderHarborBlockedArea, renderIcon, renderLabel, icons, typeface, pixelsPerDip, debugModeEnabled, debugShowObjectPositions, debugBrushLight);

            drawingContext.Pop();
        }

        public void DrawSelectionRect(DrawingContext drawingContext, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen highlightPen, Rect selectionRect)
        {
            drawingContext.DrawRectangle(lightBrush, highlightPen, selectionRect);
        }

        public void DrawDebugInfo(DrawingContext drawingContext, TranslateTransform viewportTransform, GuidelineSet guidelineSet, int gridSize, bool debugModeEnabled, bool debugShowQuadTreeViz, System.Windows.Media.Pen debugPen, System.Windows.Media.Brush debugBrushLight, bool debugShowSelectionCollisionRect, Rect collisionRect, AnnoDesigner.Models.Interface.ICoordinateHelper coordinateHelper, bool debugShowViewportRectCoordinates, Rect viewportAbsolute, double pixelsPerDip, Typeface typeface, bool debugShowScrollableRectCoordinates, Rect scrollableBounds, bool debugShowLayoutRectCoordinates, Rect layoutBounds, bool debugShowObjectCount, int placedObjectsCount, bool debugShowMouseGridCoordinates, Point mousePosition, bool isSelectionRectMode, Rect selectionRect, bool debugShowSelectionRectCoordinates, AnnoDesigner.Models.Interface.IBrushCache brushCache, AnnoDesigner.Models.Interface.IPenCache penCache, AnnoDesigner.Viewport viewport, System.Windows.Media.Brush debugBrushDark)
        {
            if (!debugModeEnabled)
            {
                return;
            }

            drawingContext.PushTransform(viewportTransform);
            if (debugShowQuadTreeViz)
            {
                var brush = Brushes.Transparent;
                foreach (var rect in _canvas.PlacedObjects.GetQuadrantRects())
                {
                    drawingContext.DrawRectangle(brush, debugPen, coordinateHelper.GridToScreen(rect, gridSize));
                }
            }

            if (debugShowSelectionCollisionRect)
            {
                var color = ((SolidColorBrush)debugBrushLight).Color;
                color.A = 0x08;
                var brush = brushCache.GetSolidBrush(color);
                var pen = penCache.GetPen(debugBrushLight, 1);
                var collisionRectScreen = coordinateHelper.GridToScreen(collisionRect, gridSize);
                drawingContext.DrawRectangle(brush, pen, collisionRectScreen);
            }

            // pop viewport transform
            drawingContext.Pop();
            var debugText = new List<FormattedText>(3);

            if (debugShowViewportRectCoordinates)
            {
                // The first time this is called, App.DpiScale is still 0 which causes this code to throw an error
                if (pixelsPerDip != 0)
                {
                    var top = viewportAbsolute.Top;
                    var left = viewportAbsolute.Left;
                    var h = viewportAbsolute.Height;
                    var w = viewportAbsolute.Width;
                    var text = new FormattedText($"Viewport: {left:F2}, {top:F2}, {w:F2}, {h:F2}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                                 typeface, 12, debugBrushLight, null, TextFormattingMode.Display, pixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Left
                    };
                    debugText.Add(text);
                }
            }

            if (debugShowScrollableRectCoordinates)
            {
                // The first time this is called, App.DpiScale is still 0 which causes this code to throw an error
                if (pixelsPerDip != 0)
                {
                    var top = scrollableBounds.Top;
                    var left = scrollableBounds.Left;
                    var h = scrollableBounds.Height;
                    var w = scrollableBounds.Width;
                    var text = new FormattedText($"Scrollable: {left:F2}, {top:F2}, {w:F2}, {h:F2}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                                 typeface, 12, debugBrushLight, null, TextFormattingMode.Display, pixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Left
                    };
                    debugText.Add(text);
                }
            }

            if (debugShowLayoutRectCoordinates)
            {
                // The first time this is called, App.DpiScale is still 0 which causes this code to throw an error
                if (pixelsPerDip != 0)
                {
                    var top = layoutBounds.Top;
                    var left = layoutBounds.Left;
                    var h = layoutBounds.Height;
                    var w = layoutBounds.Width;
                    var text = new FormattedText($"Layout: {left:F2}, {top:F2}, {w:F2}, {h:F2}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                                 typeface, 12, debugBrushLight, null, TextFormattingMode.Display, pixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Left
                    };
                    debugText.Add(text);
                }
            }

            if (debugShowObjectCount)
            {
                // The first time this is called, App.DpiScale is still 0 which causes this code to throw an error
                if (pixelsPerDip != 0)
                {
                    var text = new FormattedText($"{nameof(_canvas.PlacedObjects)}: {placedObjectsCount}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                                 typeface, 12, debugBrushLight, null, TextFormattingMode.Display, pixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Left
                    };
                    debugText.Add(text);
                }
            }

            for (var i = 0; i < debugText.Count; i++)
            {
                drawingContext.DrawText(debugText[i], new Point(5, (i * 15) + 5));
            }

            if (debugShowMouseGridCoordinates)
            {
                // The first time this is called, App.DpiScale is still 0 which causes this code to throw an error
                if (pixelsPerDip != 0)
                {
                    var gridPosition = coordinateHelper.ScreenToFractionalGrid(mousePosition, gridSize);
                    gridPosition = viewport.OriginToViewport(gridPosition);
                    var x = gridPosition.X;
                    var y = gridPosition.Y;
                    var text = new FormattedText($"{x:F2}, {y:F2}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                                                 typeface, 12, debugBrushLight, null, TextFormattingMode.Display, pixelsPerDip)
                    {
                        TextAlignment = TextAlignment.Left
                    };
                    var pos = mousePosition;
                    pos.X -= 5;
                    pos.Y += 15;
                    drawingContext.DrawText(text, pos);
                }
            }

            // draw selection rect coords last so they draw over the top of everything else
            if (isSelectionRectMode && debugShowSelectionRectCoordinates)
            {
                var rect = coordinateHelper.ScreenToGrid(selectionRect, gridSize);
                var top = rect.Top;
                var left = rect.Left;
                var h = rect.Height;
                var w = rect.Width;
                var text = new FormattedText($"{left:F2}, {top:F2}, {w:F2}, {h:F2}", Thread.CurrentThread.CurrentCulture, FlowDirection.LeftToRight,
                    typeface, 12, debugBrushLight,
                    null, TextFormattingMode.Display, pixelsPerDip)
                {
                    TextAlignment = TextAlignment.Left
                };
                var location = selectionRect.BottomRight;
                location.X -= text.Width;
                location.Y -= text.Height;
                drawingContext.DrawText(text, location);
            }
        }


        /// <summary>
        /// Moves the current object to the mouse position.
        /// </summary>
        public void MoveCurrentObjectsToMouse()
        {
            if (_canvas.CurrentObjects.Count == 0)
            {
                return;
            }

            if (_canvas.CurrentObjects.Count > 1)
            {
                //Get the center of the current selection
                var r = _canvas.CurrentObjects[0].GridRect;
                foreach (var obj in _canvas.CurrentObjects.Skip(1))
                {
                    r.Union(obj.GridRect);
                }

                var center = _canvas._coordinateHelper.GetCenterPoint(r);
                var mousePosition = _canvas._coordinateHelper.ScreenToFractionalGrid(_canvas._mousePosition, _canvas.GridSize);
                var dx = mousePosition.X - center.X;
                var dy = mousePosition.Y - center.Y;
                foreach (var obj in _canvas.CurrentObjects)
                {
                    var pos = obj.Position;
                    pos = _canvas._viewport.OriginToViewport(new Point(pos.X + dx, pos.Y + dy));
                    pos = new Point(Math.Floor(pos.X), Math.Floor(pos.Y));
                    obj.Position = pos;
                }
            }
            else
            {
                var pos = _canvas._coordinateHelper.ScreenToFractionalGrid(_canvas._mousePosition, _canvas.GridSize);
                var size = _canvas.CurrentObjects[0].Size;
                pos.X -= size.Width / 2;
                pos.Y -= size.Height / 2;
                pos = _canvas._viewport.OriginToViewport(pos);
                pos = new Point(Math.Round(pos.X, MidpointRounding.AwayFromZero), Math.Round(pos.Y, MidpointRounding.AwayFromZero));
                _canvas.CurrentObjects[0].Position = pos;
            }
        }

    }
}
