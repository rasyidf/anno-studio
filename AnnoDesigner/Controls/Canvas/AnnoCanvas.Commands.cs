using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AnnoDesigner.Controls.Canvas.Services;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Models;
using AnnoDesigner.CustomEventArgs;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using AnnoDesigner.Services.Undo.Operations;

namespace AnnoDesigner.Controls.Canvas;

public partial class AnnoCanvas
{
    public ICommand RotateCommand { get; internal set; }
    internal Hotkey rotateAllHotkey;
    internal ICommand rotateAllCommand;
    // Align/Distribute/Flip commands & hotkeys
    internal ICommand alignCommand;
    internal Hotkey alignLeftHotkey;
    internal Hotkey alignCenterHotkey;
    internal Hotkey alignRightHotkey;
    internal Hotkey alignTopHotkey;
    internal Hotkey alignMiddleHotkey;
    internal Hotkey alignBottomHotkey;

    internal ICommand distributeCommand;
    internal Hotkey distributeHorizontalHotkey;
    internal Hotkey distributeVerticalHotkey;

    internal ICommand flipCommand;
    internal Hotkey flipHorizontalHotkey;
    internal Hotkey flipVerticalHotkey;
    internal Hotkey zoomInHotkey;
    internal ICommand zoomInCommand;
    internal Hotkey zoomOutHotkey;
    internal ICommand zoomOutCommand;
    internal Hotkey zoomFitHotkey;
    internal ICommand zoomFitCommand;
    internal Hotkey zoomToSelectionHotkey;
    internal ICommand zoomToSelectionCommand;
    internal Hotkey cutHotkey;

    internal ICommand cutCommand;
    internal Hotkey copyHotkey;
    internal ICommand copyCommand;
    internal Hotkey pasteHotkey;
    internal ICommand pasteCommand;
    internal Hotkey deleteHotkey;
    internal ICommand deleteCommand;
    internal Hotkey duplicateHotkey;
    internal ICommand duplicateCommand;
    internal Hotkey deleteObjectUnderCursorHotkey;
    internal ICommand deleteObjectUnderCursorCommand;
    internal Hotkey undoHotkey;
    internal ICommand undoCommand;
    internal Hotkey redoHotkey;
    internal ICommand redoCommand;
    internal Hotkey selectAllSameIdentifierHotkey;
    internal ICommand selectAllSameIdentifierCommand;
    internal Hotkey enableDebugModeHotkey;
    internal ICommand enableDebugModeCommand;

    /// <summary>
    /// R key rotate
    /// </summary>
    internal Hotkey rotateHotkey1;
    /// <summary>
    /// MiddleClick rotate
    /// </summary>
    internal Hotkey rotateHotkey2;
    /// <summary>
    /// Holds event handlers for command executions.
    /// </summary>
    internal static readonly Dictionary<ICommand, Action<AnnoCanvas>> CommandExecuteMappings;

    public HotkeyCommandManager HotkeyCommandManager { get; set; }
    /// <summary>
    /// Creates event handlers for command executions and registers them at the CommandManager.
    /// </summary>
    static AnnoCanvas()
    {
        // create event handler mapping
        CommandExecuteMappings = new Dictionary<ICommand, Action<AnnoCanvas>>
        {
            { ApplicationCommands.New, async _ => await _.NewFile().ConfigureAwait(false) },
            { ApplicationCommands.Open, async _ => await _.OpenFile() },
            { ApplicationCommands.Save, _ => _.Save() },
            { ApplicationCommands.SaveAs, _ => _.SaveAs() }
        };

        // register event handlers for the specified commands
        foreach (var action in CommandExecuteMappings)
        {
            CommandManager.RegisterClassCommandBinding(typeof(AnnoCanvas), new CommandBinding(action.Key, ExecuteCommand));
        }

        // register Undo/Redo command bindings so ApplicationCommands.Undo and .Redo work with AnnoCanvas2
        CommandManager.RegisterClassCommandBinding(typeof(AnnoCanvas), new CommandBinding(ApplicationCommands.Undo, ExecuteUndoCommand, CanExecuteUndoCommand));
        CommandManager.RegisterClassCommandBinding(typeof(AnnoCanvas), new CommandBinding(ApplicationCommands.Redo, ExecuteRedoCommand, CanExecuteRedoCommand));
    }


    private void SetupHotkeys()
    {
        //Commands
        RotateCommand = new RelayCommand(ExecuteRotate);
        rotateAllCommand = new RelayCommand(ExecuteRotateAll);
        cutCommand = new RelayCommand(ExecuteCut);
        copyCommand = new RelayCommand(ExecuteCopy);
        pasteCommand = new RelayCommand(ExecutePaste);
        deleteCommand = new RelayCommand(ExecuteDelete);
        duplicateCommand = new RelayCommand(ExecuteDuplicate);
        deleteObjectUnderCursorCommand = new RelayCommand(ExecuteDeleteObjectUnderCursor);
        undoCommand = new RelayCommand(ExecuteUndo);
        redoCommand = new RelayCommand(ExecuteRedo);
        enableDebugModeCommand = new RelayCommand(ExecuteEnableDebugMode);
        selectAllSameIdentifierCommand = new RelayCommand(ExecuteSelectAllSameIdentifier);
        // align/distribute/flip commands
        alignCommand = new RelayCommand(ExecuteAlign);
        distributeCommand = new RelayCommand(ExecuteDistribute);
        flipCommand = new RelayCommand(ExecuteFlip);
        // zoom commands
        zoomInCommand = new RelayCommand(ExecuteZoomIn);
        zoomOutCommand = new RelayCommand(ExecuteZoomOut);
        zoomFitCommand = new RelayCommand(ExecuteZoomFit);
        zoomToSelectionCommand = new RelayCommand(ExecuteZoomToSelection);

        //Set up default keybindings

        //for rotation with the r key.
        var rotateBinding1 = new InputBinding(RotateCommand, new PolyGesture(Key.R, ModifierKeys.None));
        rotateHotkey1 = new Hotkey("Rotate_1", rotateBinding1, ROTATE_LOCALIZATION_KEY);

        //for rotation with middle click
        var rotateBinding2 = new InputBinding(RotateCommand, new PolyGesture(ExtendedMouseAction.MiddleClick));
        rotateHotkey2 = new Hotkey("Rotate_2", rotateBinding2, ROTATE_LOCALIZATION_KEY);

        var rotateAllBinding = new InputBinding(rotateAllCommand, new PolyGesture(Key.R, ModifierKeys.Shift));
        rotateAllHotkey = new Hotkey(ROTATE_ALL_LOCALIZATION_KEY, rotateAllBinding, ROTATE_ALL_LOCALIZATION_KEY);

        var cutBinding = new InputBinding(cutCommand, new PolyGesture(Key.X, ModifierKeys.Control));
        cutHotkey = new Hotkey("Cut", cutBinding, "Cut");

        var copyBinding = new InputBinding(copyCommand, new PolyGesture(Key.C, ModifierKeys.Control));
        copyHotkey = new Hotkey(COPY_LOCALIZATION_KEY, copyBinding, COPY_LOCALIZATION_KEY);

        var pasteBinding = new InputBinding(pasteCommand, new PolyGesture(Key.V, ModifierKeys.Control));
        pasteHotkey = new Hotkey(PASTE_LOCALIZATION_KEY, pasteBinding, PASTE_LOCALIZATION_KEY);

        var deleteBinding = new InputBinding(deleteCommand, new PolyGesture(Key.Delete, ModifierKeys.None));
        deleteHotkey = new Hotkey(DELETE_LOCALIZATION_KEY, deleteBinding, DELETE_LOCALIZATION_KEY);

        var duplicateBinding = new InputBinding(duplicateCommand, new PolyGesture(ExtendedMouseAction.LeftDoubleClick, ModifierKeys.None));
        duplicateHotkey = new Hotkey(DUPLICATE_LOCALIZATION_KEY, duplicateBinding, DUPLICATE_LOCALIZATION_KEY);

        var deleteHoveredOjectBinding = new InputBinding(deleteObjectUnderCursorCommand, new PolyGesture(ExtendedMouseAction.RightClick, ModifierKeys.None));
        deleteObjectUnderCursorHotkey = new Hotkey(DELETE_OBJECT_UNDER_CURSOR_LOCALIZATION_KEY, deleteHoveredOjectBinding, DELETE_OBJECT_UNDER_CURSOR_LOCALIZATION_KEY);

        var undoBinding = new InputBinding(undoCommand, new PolyGesture(Key.Z, ModifierKeys.Control));
        undoHotkey = new Hotkey(UNDO_LOCALIZATION_KEY, undoBinding, UNDO_LOCALIZATION_KEY);

        var redoBinding = new InputBinding(redoCommand, new PolyGesture(Key.Y, ModifierKeys.Control));
        redoHotkey = new Hotkey(REDO_LOCALIZATION_KEY, redoBinding, REDO_LOCALIZATION_KEY);

        var enableDebugModeBinding = new InputBinding(enableDebugModeCommand, new PolyGesture(Key.D, ModifierKeys.Control | ModifierKeys.Shift));
        enableDebugModeHotkey = new Hotkey(ENABLE_DEBUG_MODE_LOCALIZATION_KEY, enableDebugModeBinding, ENABLE_DEBUG_MODE_LOCALIZATION_KEY);

        var selectAllSameIdentifierBinding = new InputBinding(selectAllSameIdentifierCommand, new PolyGesture(ExtendedMouseAction.LeftClick, ModifierKeys.Control | ModifierKeys.Shift));
        selectAllSameIdentifierHotkey = new Hotkey(SELECT_ALL_SAME_IDENTIFIER_LOCALIZATION_KEY, selectAllSameIdentifierBinding, SELECT_ALL_SAME_IDENTIFIER_LOCALIZATION_KEY);

        // Align default bindings (Ctrl+Alt + Arrow for edges, Ctrl+Alt+C/M for center/middle)
        var alignLeftBinding = new InputBinding(alignCommand, new PolyGesture(Key.Left, ModifierKeys.Control | ModifierKeys.Alt));
        alignLeftBinding.CommandParameter = AlignmentMode.Left.ToString();
        alignLeftHotkey = new Hotkey(ALIGN_LEFT_LOCALIZATION_KEY, alignLeftBinding, ALIGN_LEFT_LOCALIZATION_KEY);

        var alignRightBinding = new InputBinding(alignCommand, new PolyGesture(Key.Right, ModifierKeys.Control | ModifierKeys.Alt));
        alignRightBinding.CommandParameter = AlignmentMode.Right.ToString();
        alignRightHotkey = new Hotkey(ALIGN_RIGHT_LOCALIZATION_KEY, alignRightBinding, ALIGN_RIGHT_LOCALIZATION_KEY);

        var alignTopBinding = new InputBinding(alignCommand, new PolyGesture(Key.Up, ModifierKeys.Control | ModifierKeys.Alt));
        alignTopBinding.CommandParameter = AlignmentMode.Top.ToString();
        alignTopHotkey = new Hotkey(ALIGN_TOP_LOCALIZATION_KEY, alignTopBinding, ALIGN_TOP_LOCALIZATION_KEY);

        var alignBottomBinding = new InputBinding(alignCommand, new PolyGesture(Key.Down, ModifierKeys.Control | ModifierKeys.Alt));
        alignBottomBinding.CommandParameter = AlignmentMode.Bottom.ToString();
        alignBottomHotkey = new Hotkey(ALIGN_BOTTOM_LOCALIZATION_KEY, alignBottomBinding, ALIGN_BOTTOM_LOCALIZATION_KEY);

        var alignCenterBinding = new InputBinding(alignCommand, new PolyGesture(Key.C, ModifierKeys.Control | ModifierKeys.Alt));
        alignCenterBinding.CommandParameter = AlignmentMode.Center.ToString();
        alignCenterHotkey = new Hotkey(ALIGN_CENTER_LOCALIZATION_KEY, alignCenterBinding, ALIGN_CENTER_LOCALIZATION_KEY);

        var alignMiddleBinding = new InputBinding(alignCommand, new PolyGesture(Key.M, ModifierKeys.Control | ModifierKeys.Alt));
        alignMiddleBinding.CommandParameter = AlignmentMode.Middle.ToString();
        alignMiddleHotkey = new Hotkey(ALIGN_MIDDLE_LOCALIZATION_KEY, alignMiddleBinding, ALIGN_MIDDLE_LOCALIZATION_KEY);

        // Distribute defaults (Ctrl+Shift+H / Ctrl+Shift+V)
        var distributeHBinding = new InputBinding(distributeCommand, new PolyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Shift));
        distributeHBinding.CommandParameter = DistributionMode.Horizontal.ToString();
        distributeHorizontalHotkey = new Hotkey(DISTRIBUTE_HORIZONTAL_LOCALIZATION_KEY, distributeHBinding, DISTRIBUTE_HORIZONTAL_LOCALIZATION_KEY);

        var distributeVBinding = new InputBinding(distributeCommand, new PolyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Shift));
        distributeVBinding.CommandParameter = DistributionMode.Vertical.ToString();
        distributeVerticalHotkey = new Hotkey(DISTRIBUTE_VERTICAL_LOCALIZATION_KEY, distributeVBinding, DISTRIBUTE_VERTICAL_LOCALIZATION_KEY);

        // Flip defaults (Ctrl+Alt+H / Ctrl+Alt+V)
        var flipHBinding = new InputBinding(flipCommand, new PolyGesture(Key.H, ModifierKeys.Control | ModifierKeys.Alt));
        flipHBinding.CommandParameter = FlipDirection.Horizontal.ToString();
        flipHorizontalHotkey = new Hotkey(FLIP_HORIZONTAL_LOCALIZATION_KEY, flipHBinding, FLIP_HORIZONTAL_LOCALIZATION_KEY);

        var flipVBinding = new InputBinding(flipCommand, new PolyGesture(Key.V, ModifierKeys.Control | ModifierKeys.Alt));
        flipVBinding.CommandParameter = FlipDirection.Vertical.ToString();
        flipVerticalHotkey = new Hotkey(FLIP_VERTICAL_LOCALIZATION_KEY, flipVBinding, FLIP_VERTICAL_LOCALIZATION_KEY);

        // zoom bindings matching MainMenuControl.xaml defaults
        var zoomInBinding = new InputBinding(zoomInCommand, new PolyGesture(Key.OemPlus, ModifierKeys.Control));
        zoomInHotkey = new Hotkey("ZoomIn", zoomInBinding, "ZoomIn");

        var zoomOutBinding = new InputBinding(zoomOutCommand, new PolyGesture(Key.OemMinus, ModifierKeys.Control));
        zoomOutHotkey = new Hotkey("ZoomOut", zoomOutBinding, "ZoomOut");

        var zoomFitBinding = new InputBinding(zoomFitCommand, new PolyGesture(Key.D0, ModifierKeys.Control));
        zoomFitHotkey = new Hotkey("ZoomFit", zoomFitBinding, "FitToScreen");

        var zoomToSelectionBinding = new InputBinding(zoomToSelectionCommand, new PolyGesture(Key.D0, ModifierKeys.Control | ModifierKeys.Shift));
        zoomToSelectionHotkey = new Hotkey("ZoomToSelection", zoomToSelectionBinding, "ZoomToSelection");

        //We specifically do not add the `InputBinding`s to the `InputBindingCollection` of `AnnoCanvas`, as if we did that,
        //`InputBinding.Gesture.Matches()` would be fired for *every* event - MouseWheel, MouseDown, KeyUp, KeyDown, MouseMove etc
        //which we don't want, as it produces a noticeable performance impact.
    }


    /// <summary>
    /// Registers hotkeys with the <see cref="HotkeyCommandManager"/>.
    /// </summary>
    /// <param name="manager"></param>
    public void RegisterHotkeys(HotkeyCommandManager manager)
    {

        ArgumentNullException.ThrowIfNull(manager);

        HotkeyCommandManager = manager;

        manager.AddHotkey(rotateHotkey1);
        manager.AddHotkey(rotateHotkey2);
        manager.AddHotkey(rotateAllHotkey);
        manager.AddHotkey(cutHotkey);
        manager.AddHotkey(copyHotkey);
        manager.AddHotkey(pasteHotkey);
        manager.AddHotkey(deleteHotkey);
        manager.AddHotkey(duplicateHotkey);
        manager.AddHotkey(deleteObjectUnderCursorHotkey);
        manager.AddHotkey(undoHotkey);
        manager.AddHotkey(redoHotkey);
        manager.AddHotkey(enableDebugModeHotkey);
        manager.AddHotkey(selectAllSameIdentifierHotkey);
        // zoom hotkeys
        manager.AddHotkey(zoomInHotkey);
        manager.AddHotkey(zoomOutHotkey);
        manager.AddHotkey(zoomFitHotkey);
        manager.AddHotkey(zoomToSelectionHotkey);
        // align hotkeys
        manager.AddHotkey(alignLeftHotkey);
        manager.AddHotkey(alignCenterHotkey);
        manager.AddHotkey(alignRightHotkey);
        manager.AddHotkey(alignTopHotkey);
        manager.AddHotkey(alignMiddleHotkey);
        manager.AddHotkey(alignBottomHotkey);
        // distribute hotkeys
        manager.AddHotkey(distributeHorizontalHotkey);
        manager.AddHotkey(distributeVerticalHotkey);
        // flip hotkeys
        manager.AddHotkey(flipHorizontalHotkey);
        manager.AddHotkey(flipVerticalHotkey);
    }

    private static void ExecuteUndoCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is AnnoCanvas canvas)
        {
            canvas.ExecuteUndo(null);
            e.Handled = true;
        }
    }

    private static void CanExecuteUndoCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        if (sender is AnnoCanvas canvas && canvas.UndoManager != null)
        {
            if (canvas.UndoManager is AnnoDesigner.Services.Undo.UndoManager um)
            {
                e.CanExecute = um.UndoStack.Count > 0;
            }
            else
            {
                e.CanExecute = false;
            }
        }
        else
        {
            e.CanExecute = false;
        }
    }

    private static void ExecuteRedoCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is AnnoCanvas canvas)
        {
            canvas.ExecuteRedo(null);
            e.Handled = true;
        }
    }

    private static void CanExecuteRedoCommand(object sender, CanExecuteRoutedEventArgs e)
    {
        if (sender is AnnoCanvas canvas && canvas.UndoManager != null)
        {
            if (canvas.UndoManager is AnnoDesigner.Services.Undo.UndoManager um)
            {
                e.CanExecute = um.RedoStack.Count > 0;
            }
            else
            {
                e.CanExecute = false;
            }
        }
        else
        {
            e.CanExecute = false;
        }
    }
    internal static void ExecuteCommand(object sender, ExecutedRoutedEventArgs e)
    {
        if (sender is AnnoCanvas canvas && CommandExecuteMappings.TryGetValue(e.Command, out var value))
        {
            value.Invoke(canvas);
            e.Handled = true;
        }
    }

    internal void ExecuteRotate(object param)
    {
        if (CurrentObjects.Count == 1)
        {
            CurrentObjects[0].Size = _coordinateHelper.Rotate(CurrentObjects[0].Size);
            CurrentObjects[0].Direction = _coordinateHelper.Rotate(CurrentObjects[0].Direction);
        }
        else if (CurrentObjects.Count > 1)
        {
            _transformService.Rotate(CurrentObjects).Consume();
        }
        else
        {
            //Count == 0;
            //Rotate from selected objects
            CurrentObjects = CloneLayoutObjects(SelectedObjects);
            _transformService.Rotate(CurrentObjects).Consume();
        }
    }

    internal void ExecuteZoomIn(object param)
    {
        var newSize = GridSize * 2;
        if (newSize > Constants.GridStepMax) newSize = Constants.GridStepMax;
        GridSize = newSize;
    }

    internal void ExecuteZoomOut(object param)
    {
        var newSize = Math.Max(Constants.GridStepMin, GridSize / 2);
        GridSize = newSize;
    }

    internal void ExecuteZoomFit(object param)
    {
        try
        {
            if (PlacedObjects == null || PlacedObjects.Count == 0) return;
            var bounds = ComputeBoundingRect(PlacedObjects);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var availableWidth = App.Current?.MainWindow?.ActualWidth ?? 800;
            var availableHeight = App.Current?.MainWindow?.ActualHeight ?? 600;
            var padding = 20;
            availableWidth = Math.Max(100, availableWidth - padding);
            availableHeight = Math.Max(100, availableHeight - padding);

            var gridSizeForWidth = (int)Math.Floor(availableWidth / bounds.Width);
            var gridSizeForHeight = (int)Math.Floor(availableHeight / bounds.Height);
            var targetGridSize = Math.Min(gridSizeForWidth, gridSizeForHeight);

            if (targetGridSize < Constants.GridStepMin) targetGridSize = Constants.GridStepMin;
            if (targetGridSize > Constants.GridStepMax) targetGridSize = Constants.GridStepMax;

            GridSize = targetGridSize;
            CenterViewportOnRect(bounds);
        }
        catch { }
    }

    internal void ExecuteZoomToSelection(object param)
    {
        try
        {
            if (SelectedObjects == null || SelectedObjects.Count == 0) return;
            var bounds = ComputeBoundingRect(SelectedObjects);
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            var availableWidth = App.Current?.MainWindow?.ActualWidth ?? 800;
            var availableHeight = App.Current?.MainWindow?.ActualHeight ?? 600;
            var padding = 20;
            availableWidth = Math.Max(100, availableWidth - padding);
            availableHeight = Math.Max(100, availableHeight - padding);

            var gridSizeForWidth = (int)Math.Floor(availableWidth / bounds.Width);
            var gridSizeForHeight = (int)Math.Floor(availableHeight / bounds.Height);
            var targetGridSize = Math.Min(gridSizeForWidth, gridSizeForHeight);

            if (targetGridSize < Constants.GridStepMin) targetGridSize = Constants.GridStepMin;
            if (targetGridSize > Constants.GridStepMax) targetGridSize = Constants.GridStepMax;

            GridSize = targetGridSize;
            CenterViewportOnRect(bounds);
        }
        catch { }
    }

    internal void ExecuteAlign(object param)
    {
        if (SelectedObjects == null) return;
        var selected = SelectedObjects.OfType<LayoutObject>().ToList();
        if (selected == null || selected.Count < 1) return;

        if (!Enum.TryParse<AlignmentMode>(param?.ToString() ?? string.Empty, true, out var mode)) return;

        var oldBounds = selected.Select(o => (obj: o, rect: o.Bounds)).ToList();

        UndoManager.AsSingleUndoableOperation(() =>
        {
            _transformationService.Align(selected.Cast<object>(), mode);

            var newBounds = selected.Select(o => (obj: o, rect: o.Bounds)).ToList();
            var moveOp = new MoveObjectsOperation<LayoutObject>
            {
                QuadTree = PlacedObjects,
                ObjectPropertyValues = newBounds.Select(nb => (nb.obj, oldBounds.First(o => ReferenceEquals(o.obj, nb.obj)).rect, nb.rect))
            };
            UndoManager.RegisterOperation(moveOp);
        });

        ForceRendering();
        StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
    }

    internal void ExecuteDistribute(object param)
    {
        if (SelectedObjects == null) return;
        var selected = SelectedObjects.OfType<LayoutObject>().ToList();
        if (selected == null || selected.Count < 3) return;

        if (!Enum.TryParse<DistributionMode>(param?.ToString() ?? string.Empty, true, out var mode)) return;

        var oldBounds = selected.Select(o => (obj: o, rect: o.Bounds)).ToList();

        UndoManager.AsSingleUndoableOperation(() =>
        {
            _transformationService.Distribute(selected.Cast<object>(), mode);

            if (GridSize > 0)
            {
                var g = GridSize;
                foreach (var o in selected)
                {
                    var snappedX = Math.Round(o.Position.X / g) * g;
                    var snappedY = Math.Round(o.Position.Y / g) * g;
                    o.Position = new System.Windows.Point(snappedX, snappedY);
                }
            }

            var newBounds = selected.Select(o => (obj: o, rect: o.Bounds)).ToList();
            var moveOp = new MoveObjectsOperation<LayoutObject>
            {
                QuadTree = PlacedObjects,
                ObjectPropertyValues = newBounds.Select(nb => (nb.obj, oldBounds.First(o => ReferenceEquals(o.obj, nb.obj)).rect, nb.rect))
            };
            UndoManager.RegisterOperation(moveOp);
        });

        ForceRendering();
        StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
    }

    internal void ExecuteFlip(object param)
    {
        if (SelectedObjects == null) return;
        var selected = SelectedObjects.OfType<LayoutObject>().ToList();
        if (selected == null || selected.Count == 0) return;

        if (!Enum.TryParse<FlipDirection>(param?.ToString() ?? string.Empty, true, out var dir)) return;

        var oldBounds = selected.Select(o => (obj: o, rect: o.Bounds)).ToList();

        UndoManager.AsSingleUndoableOperation(() =>
        {
            _transformationService.Flip(selected.Cast<object>(), dir);

            var newBounds = selected.Select(o => (obj: o, rect: o.Bounds)).ToList();
            var moveOp = new MoveObjectsOperation<LayoutObject>
            {
                QuadTree = PlacedObjects,
                ObjectPropertyValues = newBounds.Select(nb => (nb.obj, oldBounds.First(o => ReferenceEquals(o.obj, nb.obj)).rect, nb.rect))
            };
            UndoManager.RegisterOperation(moveOp);
        });

        ForceRendering();
        StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
    }

    internal void ExecuteRotateAll(object param)
    {
        UndoManager.AsSingleUndoableOperation(() =>
        {
            var placedObjects = PlacedObjects.ToList();
            UndoManager.RegisterOperation(new MoveObjectsOperation<LayoutObject>()
            {
                QuadTree = PlacedObjects,
                ObjectPropertyValues = PlacedObjects.Select(obj => (obj, obj.Bounds, _coordinateHelper.Rotate(obj.Bounds))).ToList()
            });

            foreach (var (item, oldRect) in _transformService.Rotate(placedObjects))
            {
                PlacedObjects.ReIndex(item, oldRect);
            }
            Normalize(1);
        });
    }


    internal void ExecuteCut(object param)
    {
        if (SelectedObjects.Count != 0)
        {
            ClipboardService.Copy(SelectedObjects.Select(x => x.WrappedAnnoObject));

            UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
            {
                Objects = [.. SelectedObjects],
                Collection = PlacedObjects
            });

            // remove all currently selected objects from the grid and clear selection    
            foreach (var item in SelectedObjects)
            {
                _ = PlacedObjects.Remove(item);
            }
            SelectedObjects.Clear();
            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
            CurrentMode = MouseMode.DeleteObject;
        }
    }


    internal void ExecuteCopy(object param)
    {
        if (SelectedObjects.Count != 0)
        {
            ClipboardService.Copy(SelectedObjects.Select(x => x.WrappedAnnoObject));

            var localizedMessage = SelectedObjects.Count == 1 ? _localizationHelper.GetLocalization("ItemCopied") : _localizationHelper.GetLocalization("ItemsCopied");
            StatusMessage = $"{SelectedObjects.Count} {localizedMessage}";
        }
    }

    internal void ExecutePaste(object param)
    {
        var objects = ClipboardService.Paste();
        if (objects.Count > 0)
        {
            CurrentObjects = [.. objects.Select(x => new LayoutObject(x, _coordinateHelper, _brushCache, _penCache))];
        }
    }

    internal void ExecuteDelete(object param)
    {
        UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
        {
            Objects = [.. SelectedObjects],
            Collection = PlacedObjects
        });

        // remove all currently selected objects from the grid and clear selection    
        foreach (var item in SelectedObjects)
        {
            _ = PlacedObjects.Remove(item);
        }
        SelectedObjects.Clear();
        StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
        CurrentMode = MouseMode.DeleteObject;
    }

    internal void ExecuteDuplicate(object param)
    {
        var obj = GetObjectAt(_mousePosition);
        if (obj != null)
        {
            CurrentObjects.Clear();
            CurrentObjects.Add(new LayoutObject(new AnnoObject(obj.WrappedAnnoObject), _coordinateHelper, _brushCache, _penCache));
            OnCurrentObjectChanged(obj);
        }
    }

    internal void ExecuteDeleteObjectUnderCursor(object param)
    {
        if (CurrentObjects.Count == 0)
        {
            var obj = GetObjectAt(_mousePosition);
            if (obj != null)
            {
                // Remove object, only ever remove a single object this way.
                UndoManager.RegisterOperation(new RemoveObjectsOperation<LayoutObject>()
                {
                    Objects =
                    [
                        obj
                    ],
                    Collection = PlacedObjects
                });

                _ = PlacedObjects.Remove(obj);
                _selectionService.RemoveSelectedObject(obj);
                RecalculateSelectionContainsNotIgnoredObject();
                StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
                CurrentMode = MouseMode.DeleteObject;

                InvalidateVisual();
            }
        }
    }

    internal void ExecuteUndo(object param)
    {
        if (CommandHistory != null)
        {
            CommandHistory.Undo();
        }
        else
        {
            UndoManager?.Undo();
        }

        ForceRendering();
        StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
    }

    internal void ExecuteRedo(object param)
    {
        if (CommandHistory != null)
        {
            CommandHistory.Redo();
        }
        else
        {
            UndoManager?.Redo();
        }

        ForceRendering();
        StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
    }

    internal void ExecuteSelectAllSameIdentifier(object param)
    {
        //select all objects with same identifier as object under mouse cursor
        var objectToCheck = GetObjectAt(_mousePosition);
        if (objectToCheck != null)
        {
            CurrentMode = MouseMode.SelectSameIdentifier;

            if (SelectedObjects.Contains(objectToCheck))
            {
                _selectionService.RemoveSelectedObject(objectToCheck, includeSameObjects: true);
            }
            else
            {
                _selectionService.AddSelectedObject(objectToCheck, includeSameObjects: true);
            }

            RecalculateSelectionContainsNotIgnoredObject();
            ForceRendering();
            StatisticsUpdated?.Invoke(this, UpdateStatisticsEventArgs.All);
        }
    }

    internal void ExecuteEnableDebugMode(object param)
    {
        _debugModeIsEnabled = !_debugModeIsEnabled;
        ForceRendering();
    }

}