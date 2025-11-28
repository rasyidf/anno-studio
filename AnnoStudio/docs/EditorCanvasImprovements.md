# Editor Canvas Improvements Documentation

## Overview
This document describes the improvements made to the Anno Studio editor canvas system for better keyboard shortcuts, context menus, and ViewModel integration.

## Key Improvements

### 1. Keyboard Shortcut Management

#### KeyboardShortcutManager Service
- **Location**: `AnnoStudio/EditorCanvas/Core/Services/KeyboardShortcutManager.cs`
- **Purpose**: Centralized management of keyboard shortcuts with conflict detection
- **Features**:
  - Register shortcuts with Key + KeyModifiers
  - Named shortcuts for easy reference
  - Automatic event handling
  - Clear and unregister capabilities

#### Default Keyboard Shortcuts

##### Edit Commands
- `Ctrl+Z` - Undo last action
- `Ctrl+Y` or `Ctrl+Shift+Z` - Redo last undone action
- `Ctrl+A` - Select all objects
- `Ctrl+D` - Duplicate selected objects
- `Delete` or `Backspace` - Delete selected objects
- `Escape` - Clear selection

##### Tool Shortcuts
- `V` - Select Tool (default)
- `S` - Stamp Tool (place buildings)
- `R` - Rectangle Tool (rectangular areas)
- `L` - Line Tool (straight lines)
- `P` - Pen/Draw Tool (free-hand drawing)

##### Navigation Shortcuts
- `Arrow Keys` - Nudge selected objects by 1 grid unit
- `Shift+Arrow Keys` - Nudge selected objects by 10 grid units

### 2. Context Menu System

#### ContextMenuService
- **Location**: `AnnoStudio/EditorCanvas/Core/Services/ContextMenuService.cs`
- **Purpose**: Manage extensible context menus for objects and canvas
- **Features**:
  - Separate object and canvas action registration
  - Conditional menu items based on context
  - Dynamic menu generation

#### Object Context Menu Actions
- **Delete** - Remove the selected object
- **Duplicate** - Create a copy of the object
- **Bring to Front** - Move object to top of rendering order
- **Send to Back** - Move object to bottom of rendering order
- **Properties...** - Show object properties panel

#### Canvas Context Menu Actions
- **Paste** - Paste from clipboard (when implemented)
- **Select All** - Select all objects on canvas

### 3. Enhanced EditorCanvas Integration

#### Improvements to EditorCanvas.axaml.cs
- Integrated `KeyboardShortcutManager` and `ContextMenuService`
- Enhanced keyboard event handling with priority system:
  1. Global shortcuts (Ctrl+Z, Ctrl+Y, etc.)
  2. Active tool-specific shortcuts
  3. Arrow key nudging
- Right-click context menu support
- Object manipulation methods:
  - `NudgeSelected()` - Move objects incrementally
  - `DuplicateSelected()` - Clone selected objects
  - `BringToFront()` / `SendToBack()` - Z-order management

### 4. ViewModel Architecture Improvements

#### CanvasIntegrationService
- **Location**: `AnnoStudio/Services/CanvasIntegrationService.cs`
- **Purpose**: Bridge between MainWindowViewModel and EditorCanvasViewModel
- **Features**:
  - Command routing (Undo, Redo, Delete, Select All, Duplicate)
  - Tool activation by name or shortcut key
  - State queries (CanUndo, CanRedo, HasSelection)
  - Zoom operations (ZoomIn, ZoomOut, ZoomReset, ZoomToFit)
  - Grid visibility toggle

#### EditorCanvasViewModel Updates
- Added `RegisterToolShortcuts()` method to automatically register tool shortcuts
- Added `ActivateTool(string toolName)` for programmatic tool switching
- Exposed `Canvas` property for direct access when needed
- Better integration with tool selection

#### MainWindowViewModel Integration
- Added `CanvasIntegrationService` dependency
- Command implementations now route through integration service
- CanExecute logic checks canvas state via integration service
- Active document changes update the integration service

### 5. Tool Enhancements

All tools now include:
- **Shortcut property** - Keyboard shortcut for activation
- **Cursor property** - Appropriate cursor for the tool
- **Enhanced descriptions** - Better tooltips and UI feedback

## Usage Examples

### Registering Custom Shortcuts

```csharp
// In EditorCanvas initialization
Shortcuts.RegisterShortcut(
    Key.G, 
    KeyModifiers.Control, 
    GroupSelected, 
    "Group", 
    "Group selected objects"
);
```

### Adding Context Menu Actions

```csharp
// Register object action
ContextMenus.RegisterObjectAction(
    "Rotate90", 
    "Rotate 90°",
    (obj, ctx) => RotateObject(obj, 90),
    obj => obj is BuildingObject,
    "rotate_icon"
);

// Register canvas action
ContextMenus.RegisterCanvasAction(
    "Import", 
    "Import...",
    ctx => ShowImportDialog(),
    ctx => true,
    "import_icon"
);
```

### Tool Switching via ViewModel

```csharp
// From MainWindowViewModel or other UI code
mainViewModel.CanvasIntegration.ActivateTool("Select");

// Or via keyboard shortcut
mainViewModel.CanvasIntegration.HandleToolShortcut(Key.V);
```

## Architecture Diagram

```
MainWindowViewModel
    ↓
CanvasIntegrationService ←→ EditorCanvasViewModel
    ↓                              ↓
EditorCanvas (Control)         ToolRegistry
    ↓                              ↓
KeyboardShortcutManager        Tools (Select, Stamp, etc.)
ContextMenuService
```

## Best Practices

### 1. Keyboard Shortcuts
- Keep shortcuts intuitive and consistent with industry standards
- Document all shortcuts in user-facing documentation
- Test for conflicts before registering

### 2. Context Menus
- Organize actions logically (edit, arrange, view, etc.)
- Use separators to group related actions
- Enable/disable items based on context
- Provide keyboard shortcuts for frequently used actions

### 3. ViewModel Communication
- Use `CanvasIntegrationService` for cross-ViewModel communication
- Keep business logic in services, not in controls
- Use events for loose coupling between components

### 4. Tool Development
- Inherit from `EditorToolBase`
- Override `Shortcut` property with appropriate KeyGesture
- Override `Cursor` property for visual feedback
- Implement `OnKeyDown` for tool-specific shortcuts

## Future Enhancements

### Potential Additions
1. **Customizable Shortcuts** - Allow users to remap keyboard shortcuts
2. **Macro Recording** - Record and replay sequences of actions
3. **Command Palette** - Quick command access via Ctrl+P
4. **Touch Gestures** - Support for touch-based devices
5. **Shortcut Cheat Sheet** - In-app keyboard shortcut reference
6. **Multi-object Operations** - Align, distribute, group/ungroup
7. **Clipboard Support** - Copy/paste between canvases and applications
8. **Snap to Objects** - Snap to edges/centers of nearby objects

### Technical Debt
- Implement proper clipboard serialization
- Add undo/redo support for all operations
- Improve object duplication to handle all object types
- Add indexed collection support for better Z-order management
- Implement event bus method for property panel updates

## Migration Guide

For existing code that directly interacted with EditorCanvas:

### Before
```csharp
canvas.ActiveTool = selectTool;
canvas.History.Undo();
```

### After
```csharp
// Via ViewModel
viewModel.ActivateTool("Select");
viewModel.UndoCommand.Execute(null);

// Or via Integration Service
integrationService.ActivateTool("Select");
integrationService.Undo();
```

## Testing Recommendations

1. **Unit Tests**
   - Test shortcut registration and conflict detection
   - Test context menu action execution
   - Test command routing through integration service

2. **Integration Tests**
   - Test tool switching via keyboard
   - Test multi-step workflows (select, duplicate, move)
   - Test undo/redo across different operations

3. **User Acceptance Tests**
   - Verify all shortcuts work as documented
   - Test context menus in various scenarios
   - Validate accessibility requirements

## Conclusion

These improvements provide a solid foundation for keyboard-driven workflow and better separation of concerns between UI and business logic. The extensible architecture allows for easy addition of new shortcuts, context menu actions, and tools without modifying core canvas code.
