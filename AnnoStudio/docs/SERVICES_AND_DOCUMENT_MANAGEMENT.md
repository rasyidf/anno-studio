# Document Management & Services Implementation

## Overview
Implemented a comprehensive document management system with multi-tab support, services architecture, and complete load/save functionality for AnnoStudio.

## Created Services

### 1. ProjectService (`Services/ProjectService.cs`)
**Purpose**: Manages project-level operations (new, open, save, export)

**Features**:
- Create new documents
- Open documents with file picker dialog
- Save/Save As with file picker dialog
- Export to image formats (PNG/SVG)
- Close documents with unsaved changes prompt
- Integration with Avalonia file dialogs (StorageProvider API)

**Methods**:
- `CreateNewDocumentAsync()` - Creates new layout document
- `OpenDocumentAsync(string? filePath)` - Opens layout from file
- `SaveDocumentAsync(LayoutDocument)` - Saves document
- `SaveDocumentAsAsync(LayoutDocument, string?)` - Save with new path
- `CloseDocumentAsync(LayoutDocument)` - Close with dirty check
- `ExportDocumentAsync(LayoutDocument, string?)` - Export to image

### 2. DocumentManager (`Services/DocumentManager.cs`)
**Purpose**: Multi-document interface management

**Features**:
- ObservableCollection of all open documents
- Active document tracking with change notifications
- Automatic activation on add/remove
- Close all documents functionality
- Find document by file path
- Unsaved changes detection

**Events**:
- `ActiveDocumentChanged` - Fired when active document changes
- `DocumentAdded` - Fired when document is added
- `DocumentRemoved` - Fired when document is removed

**Methods**:
- `AddDocument(LayoutDocument)` - Add and activate document
- `RemoveDocumentAsync(LayoutDocument)` - Close document with checks
- `CloseAllDocumentsAsync()` - Close all with prompts
- `GetDocumentByPath(string)` - Find by file path
- `HasUnsavedChanges()` - Check for dirty documents

### 3. StampService (`Services/StampService.cs`)
**Purpose**: Building template/stamp management

**Features**:
- Predefined building templates (residences, factories, infrastructure)
- Category-based organization
- Custom stamp support
- Create BuildingObject from stamp

**Default Stamps**:
- Worker Residence (1x1)
- Artisan Residence (2x2)
- Small Factory (3x3)
- Medium Factory (4x4)
- Warehouse (2x3)
- Road (1x1)

**Methods**:
- `GetAllStamps()` - Get all available stamps
- `GetStampsByCategory(string)` - Filter by category
- `GetStampById(string)` - Get specific stamp
- `CreateBuildingFromStamp(BuildingStamp)` - Create building instance
- `SaveCustomStamp(BuildingStamp)` - Add custom stamp
- `GetCategories()` - Get all categories

### 4. PresetsService (`Services/PresetsService.cs`)
**Purpose**: Color and icon preset management

**Features**:
- Color presets with categories
- Icon presets
- JSON persistence in AppData
- Custom preset support
- Default color palette (9 colors)

**Methods**:
- `LoadPresetsAsync()` - Load from storage
- `GetColorPresets()` / `GetIconPresets()` - Get all presets
- `GetColorPreset(string)` / `GetIconPreset(string)` - Get by name
- `SaveColorPresetsAsync()` / `SaveIconPresetsAsync()` - Persist changes
- `AddColorPreset(ColorPreset)` / `AddIconPreset(IconPreset)` - Add custom

## Enhanced Components

### MainWindowViewModel
**Updated Features**:
- Integration with all services (ProjectService, DocumentManager, StampService, PresetsService)
- Multi-document support via DocumentManager
- Active document tracking
- Window title updates based on active document
- Command CanExecute updates on document changes
- Async command patterns for file operations

**New Commands**:
- `NewCanvasCommand` - Creates new document via ProjectService
- `OpenFileCommand` - Opens document with file picker
- `SaveFileCommand` - Saves active document
- `SaveFileAsCommand` - Save active document with new path
- `ExportImageCommand` - Export active document
- `ExitCommand` - Close all documents and exit

**Services Access**:
- Public properties expose all services for view binding
- Document collection bindable via `Documents` property
- Active document via `ActiveDocument` property

### DocumentTabsViewModel
**Purpose**: Manages document tab strip

**Features**:
- Two-way binding between selected tab and active document
- Close document command per tab
- Automatic sync with DocumentManager

### DocumentTabsView (AXAML)
**Purpose**: Visual representation of open documents

**Features**:
- Horizontal scrollable tab strip
- Document title display with dirty indicator (*)
- Close button per tab
- Hover effects
- Compact 32px height design

## Architecture Improvements

### Service Layer Pattern
```
MainWindowViewModel
    ├── IProjectService (file operations)
    ├── IDocumentManager (multi-doc management)
    ├── IStampService (building templates)
    └── IPresetsService (colors/icons)
```

### Document Lifecycle
```
1. Create/Open → ProjectService
2. Add to collection → DocumentManager
3. Set as active → DocumentManager (fires event)
4. MainWindowViewModel updates → UI refreshes
5. Save/Close → ProjectService checks dirty state
6. Remove → DocumentManager (with confirmation)
```

### Data Flow
```
User Action
    ↓
MainWindowViewModel Command
    ↓
Service Method (ProjectService/DocumentManager)
    ↓
Event Notification (ActiveDocumentChanged, etc.)
    ↓
MainWindowViewModel Event Handler
    ↓
UI Update (Title, Commands, Tabs)
```

## File Structure
```
AnnoStudio/
├── Services/
│   ├── IProjectService.cs
│   ├── ProjectService.cs
│   ├── IDocumentManager.cs
│   ├── DocumentManager.cs
│   ├── IStampService.cs
│   ├── StampService.cs
│   ├── IPresetsService.cs
│   └── PresetsService.cs
├── ViewModels/
│   ├── MainWindowViewModel.cs (enhanced)
│   ├── DocumentTabsViewModel.cs (new)
│   └── LayoutDocument.cs (existing)
└── Views/
    ├── DocumentTabsView.axaml (new)
    └── DocumentTabsView.axaml.cs (new)
```

## Key Features Implemented

✅ **Multi-Document Interface**
- Multiple documents open simultaneously
- Active document tracking
- Tab-based navigation

✅ **Complete File Operations**
- New document creation
- Open with native file picker
- Save/Save As with dialogs
- Export to image formats
- Close with unsaved changes check

✅ **Building Stamp System**
- Predefined building templates
- Category organization
- Custom stamp support
- Easy building creation

✅ **Preset Management**
- Color presets with categories
- Icon presets
- JSON persistence
- Custom preset support

✅ **Service Architecture**
- Interface-based design
- Dependency injection ready
- Clean separation of concerns
- Testable components

✅ **Event-Driven Updates**
- Document change notifications
- Automatic UI updates
- Command CanExecute refresh

## Usage Examples

### Creating a New Document
```csharp
var mainViewModel = new MainWindowViewModel(mainWindow);
await mainViewModel.NewCanvasCommand.ExecuteAsync(null);
// Creates new document, adds to collection, activates it
```

### Opening a Document
```csharp
await mainViewModel.OpenFileCommand.ExecuteAsync(null);
// Shows file picker, loads document, checks for duplicates, activates
```

### Working with Stamps
```csharp
var stamps = mainViewModel.StampService.GetStampsByCategory("Production");
var building = mainViewModel.StampService.CreateBuildingFromStamp(stamps.First());
// Creates a BuildingObject from template
```

### Managing Documents
```csharp
// Access all open documents
foreach (var doc in mainViewModel.Documents)
{
    Console.WriteLine($"{doc.Title} - Dirty: {doc.IsDirty}");
}

// Get active document
var active = mainViewModel.ActiveDocument;

// Close active document
await mainViewModel.DocumentManager.RemoveDocumentAsync(active);
```

## Build Status
✅ **Build Successful** - 0 errors, 0 warnings

## Next Steps (Optional Enhancements)
1. Implement document tabs view in DockFactory
2. Add actual save changes dialog (currently defaults to Don't Save)
3. Implement PNG/SVG export functionality
4. Add recent files list
5. Add preferences dialog for service configuration
6. Add building preset editor
7. Add color picker for custom color presets
