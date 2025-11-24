using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.DataStructures;
using AnnoDesigner.Models.Interface;

namespace AnnoDesigner.Controls.Canvas.Services
{
    internal interface ICanvasRenderer
    {
        void Render(DrawingContext drawingContext);
        void DrawObjectSelection(DrawingContext drawingContext, ICollection<LayoutObject> objects, System.Windows.Media.Pen highlightPen, int gridSize);
        void DrawObjectInfluenceRadius(DrawingContext drawingContext, ICollection<LayoutObject> objects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, int gridSize);
        void DrawObjectInfluenceRange(DrawingContext drawingContext, ICollection<LayoutObject> objects, int gridSize, bool renderTrueInfluenceRange, QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, ICoordinateHelper coordinateHelper);
        void DrawPanoramaText(DrawingContext drawingContext, List<LayoutObject> placedObjects, bool forceRedraw, string skyscraperIdentifier, Regex regexPanorama, Typeface typeface, double fontSize, double pixelsPerDip, GuidelineSet guidelineSet, ICoordinateHelper coordinateHelper);
        void DrawObjectList(DrawingContext drawingContext, List<LayoutObject> objects, bool useTransparency, int gridSize, System.Windows.Media.Pen linePen, bool renderHarborBlockedArea, bool renderIcon, bool renderLabel, Dictionary<string, IconImage> icons, Typeface typeface, double pixelsPerDip, bool debugModeEnabled, bool debugShowObjectPositions, System.Windows.Media.Brush debugBrushLight);

        // higher-level helpers that also own their internal drawing-group caching
        void DrawGrid(DrawingContext drawingContext, double width, double height, double horizontalAlignmentValue, double verticalAlignmentValue, int gridSize, bool forceRedraw, System.Windows.Media.Pen gridLinePen, GuidelineSet guidelineSet);

        void DrawPlacedObjects(DrawingContext drawingContext, List<LayoutObject> borderlessObjects, List<LayoutObject> borderedObjects, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, System.Windows.Media.Pen linePen, bool renderHarborBlockedArea, bool renderIcon, bool renderLabel, Dictionary<string, IconImage> icons, Typeface typeface, double pixelsPerDip, bool debugModeEnabled, bool debugShowObjectPositions, System.Windows.Media.Brush debugBrushLight);

        void DrawSelectedObjectsInfluence(DrawingContext drawingContext, ICollection<LayoutObject> selectedObjects, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, bool renderTrueInfluenceRange, QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, ICoordinateHelper coordinateHelper);

        void DrawInfluenceGroup(DrawingContext drawingContext, ICollection<LayoutObject> objectsToDraw, Rect viewPortAbsolute, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, bool renderTrueInfluenceRange, QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, ICoordinateHelper coordinateHelper);

        // selection visuals + caching; returns true if a redraw of the cached selection drawing was performed
        bool DrawSelectionGroup(DrawingContext drawingContext, ICollection<LayoutObject> objects, bool forceRedraw, GuidelineSet guidelineSet, int gridSize, System.Windows.Media.Pen highlightPen, bool isDragSelection, Rect selectionRect);

        void DrawHoverHighlight(DrawingContext drawingContext, LayoutObject hoveredObject, System.Windows.Media.Pen highlightPen, int gridSize);

        void DrawCurrentObjects(DrawingContext drawingContext, TranslateTransform viewportTransform, GuidelineSet guidelineSet, int gridSize, bool renderTrueInfluenceRange, QuadTree<LayoutObject> placedObjects, System.Windows.Media.Brush influencedBrush, System.Windows.Media.Pen influencedPen, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen radiusPen, ICoordinateHelper coordinateHelper, System.Windows.Media.Pen linePen, bool renderHarborBlockedArea, bool renderIcon, bool renderLabel, Dictionary<string, IconImage> icons, Typeface typeface, double pixelsPerDip, bool debugModeEnabled, bool debugShowObjectPositions, System.Windows.Media.Brush debugBrushLight, Point mousePosition, AnnoDesigner.Viewport viewport);

        void DrawSelectionRect(DrawingContext drawingContext, System.Windows.Media.Brush lightBrush, System.Windows.Media.Pen highlightPen, Rect selectionRect);

        void DrawDebugInfo(DrawingContext drawingContext, TranslateTransform viewportTransform, GuidelineSet guidelineSet, int gridSize, bool debugModeEnabled, bool debugShowQuadTreeViz, System.Windows.Media.Pen debugPen, System.Windows.Media.Brush debugBrushLight, bool debugShowSelectionCollisionRect, Rect collisionRect, ICoordinateHelper coordinateHelper, bool debugShowViewportRectCoordinates, Rect viewportAbsolute, double pixelsPerDip, Typeface typeface, bool debugShowScrollableRectCoordinates, Rect scrollableBounds, bool debugShowLayoutRectCoordinates, Rect layoutBounds, bool debugShowObjectCount, int placedObjectsCount, bool debugShowMouseGridCoordinates, Point mousePosition, bool isSelectionRectMode, Rect selectionRect, bool debugShowSelectionRectCoordinates, AnnoDesigner.Models.Interface.IBrushCache brushCache, AnnoDesigner.Models.Interface.IPenCache penCache, AnnoDesigner.Viewport viewport, System.Windows.Media.Brush debugBrushDark);
    
        void MoveCurrentObjectsToMouse();
    }
}
