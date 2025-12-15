# Multi Document Tabbing (MDT) Technical Specification

**Version:** 1.1  
**Date:** 2025-11-27  
**Status:** Implemented (partial) — REWRITE REQUIRED  
**Target Framework:** .NET 10  
**Primary Contact:** Development Team

---

## Table of Contents


1. [Executive Summary](#1-executive-summary) — Overview of MDT goals, current state, and refactor motivation.
2. [Background and Motivation](#2-background-and-motivation) — Why MDT is needed, user pain points, and business value.
3. [Current Architecture Analysis](#3-current-architecture-analysis) — What’s implemented, key issues, and lessons learned from the current codebase.
4. [Proposed Architecture](#4-proposed-architecture) — High-level design, main components, and architectural principles for MDT.
5. [Document Model](#5-document-model) — Structure and responsibilities of DocumentViewModel and related interfaces.
6. [ViewModel Structure](#6-viewmodel-structure) — How ViewModels are organized, including DocumentManager and MainViewModel changes.
7. [Canvas Instance Management](#7-canvas-instance-management) — How canvases are created, owned, and managed per document.
8. [Services and Dependencies](#8-services-and-dependencies) — Service lifetimes, dependency injection, and resource sharing.
9. [UI/UX Design](#9-uiux-design) — Tabbed interface, context panels, and user interaction patterns.
10. [Implementation Phases](#10-implementation-phases) — Step-by-step plan for refactoring and feature rollout.
11. [Migration Path](#11-migration-path) — How to transition from SDI to MDT, including compatibility notes.
12. [Testing Strategy](#12-testing-strategy) — Unit, integration, and memory tests required for MDT reliability.
13. [Performance Considerations](#13-performance-considerations) — Memory, responsiveness, and resource management for multiple documents.
14. [Edge Cases and Constraints](#14-edge-cases-and-constraints) — Known limitations, tricky scenarios, and design constraints.
15. [Appendix](#15-appendix) — Supporting docs, references, and additional notes.

---

## 1. Executive Summary

This document describes the current state of Multi Document Tabbing (MDT) in Anno Designer, summarizes the concrete implementation that exists in the codebase today, and lays out a complete reimplementation (refactor) plan to consolidate, simplify, and harden the implementation.

Key point: MDT has been implemented (core classes, DI wiring and tests are present), but the current implementation contains inconsistencies and technical debt that make a clean, maintainable and efficient long-term solution difficult to maintain — we therefore plan a full reimplementation to deliver a robust design for MDT.

**Key Goals:**

- Support multiple open layouts simultaneously
- Maintain independent undo/redo stacks per document
- Preserve performance with multiple canvas instances
- Seamless integration with existing features (presets, statistics, building settings)
- Consistent user experience across document operations

---

## 2. Background and Motivation

### Current State

Anno Designer currently operates in Single Document Interface (SDI) mode:

- One layout file open at a time
- Single AnnoCanvas2 instance in MainWindow
- Single undo/redo stack
- File operations require closing the current layout

### User Pain Points

1. **Workflow Interruption:** Users must close current work to reference another layout
2. **Copy-Paste Limitations:** Cannot easily copy buildings between layouts
3. **Comparison Difficulty:** Side-by-side layout comparison requires multiple application instances
4. **Resource Inefficiency:** Multiple processes consume more system resources

### Business Value

- **Productivity:** 40-60% faster workflow for users working with multiple layouts
- **User Experience:** Modern tabbed interface matching contemporary design tools
- **Competitive Advantage:** Feature parity with professional design applications
- **Foundation:** Enables future features like split-view and layout comparison

---

## 3. Current Architecture Analysis

## 4. Proposed Architecture

### 4.1 Architecture Overview

```
.---------------------.
|     MainWindow      |
'---------------------'
          | Binds to
          |
.-----------------------.
|    MainViewModel      |
|-----------------------|
|  - DocumentManager    |
|  - SharedResourceManager|
'-----------------------'
          |
  .-------+---------------------------------------------------.
  |       |                                                   |
.--------------.                         .-----------------------.
| DocumentManager|                         | SharedResourceManager |
|--------------|                         |-----------------------|
| - ObsColl<DocVM>|                         | - BuildingPresets     |
| - ActiveDocument|                         | - Icons Dictionary    |
| - Methods(...) |                         | - IAppSettings        |
'--------------'                         '-----------------------'
          | Contains
          |
 .---------------------. .---------------------.
 | DocumentViewModel 1 | | DocumentViewModel N |
 |---------------------| |---------------------|
 | - DataContext for   | | - DataContext for   |
 |   DocumentView 1    | |   DocumentView N    |
 | - StatisticsVM      | | - StatisticsVM      |
 | - BuildingSettingsVM| | - BuildingSettingsVM|
 '---------------------' '---------------------'
```

### 4.2 Key Architectural Principles

1. **Document Encapsulation:** Each document maintains its own complete state
2. **Shared Resources:** Presets, icons, and settings shared across documents
3. **Active Document Pattern:** Context panels bind to active document only
4. **Service Lifetime Management:** Scoped services per document, singleton for shared
5. **Event Isolation:** Document events don't cross-contaminate
6. **Memory Efficiency:** Lazy loading and resource pooling where appropriate

---


## 6. ViewModel Structure

 
### 6.2 Updated MainViewModel

**Location:** `AnnoDesigner\ViewModels\MainViewModel.cs` (modifications)

```csharp
public partial class MainViewModel : Notify
{
    // NEW: Document management
    [ObservableProperty]
    private DocumentManager _documentManager;

    // CHANGED: Active document reference instead of direct canvas
    public IAnnoCanvas AnnoCanvas => DocumentManager?.ActiveDocument?.Canvas;

    // NEW: Active document convenience properties
    public StatisticsViewModel StatisticsViewModel => DocumentManager?.ActiveDocument?.Statistics;
    public BuildingSettingsViewModel BuildingSettingsViewModel => DocumentManager?.ActiveDocument?.BuildingSettings;
    public LayoutSettingsViewModel LayoutSettingsViewModel => DocumentManager?.ActiveDocument?.LayoutSettings;

    // Constructor updates
    public MainViewModel(
        ICommons commonsToUse,
        IAppSettings appSettingsToUse,
        IDocumentServicesFactory documentServicesFactory,
        ISharedResourceManager sharedResourceManager,
        // ... other dependencies
    )
    {
        // Initialize document manager
        DocumentManager = new DocumentManager(
            documentServicesFactory,
            sharedResourceManager
        );

        DocumentManager.PropertyChanged += OnDocumentManagerPropertyChanged;
        DocumentManager.StatusMessageChanged += OnDocumentStatusMessageChanged;

        // Create initial empty document
        DocumentManager.CreateNewDocument();

        // ... rest of initialization
    }

    // NEW: Handle active document changes
    private void OnDocumentManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentManager.ActiveDocument))
        {
            // Notify that dependent properties changed
            OnPropertyChanged(nameof(AnnoCanvas));
            OnPropertyChanged(nameof(StatisticsViewModel));
            OnPropertyChanged(nameof(BuildingSettingsViewModel));
            OnPropertyChanged(nameof(LayoutSettingsViewModel));

            UpdateCanvasBindings();
        }
    }

    // NEW: Document commands
    [RelayCommand]
    private void NewDocument()
    {
        DocumentManager.CreateNewDocument();
    }

    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        var filePath = await _fileDialogService.ShowOpenFileDialogAsync(
            Constants.SavedLayoutExtension,
            Constants.SaveOpenDialogFilter
        );

        if (!string.IsNullOrEmpty(filePath))
        {
            await DocumentManager.OpenDocumentAsync(filePath);
        }
    }

    [RelayCommand]
    private async Task CloseDocumentAsync()
    {
        if (DocumentManager.ActiveDocument != null)
        {
            await DocumentManager.CloseDocumentAsync(DocumentManager.ActiveDocument);
        }
    }

    [RelayCommand]
    private async Task CloseAllDocumentsAsync()
    {
        await DocumentManager.CloseAllDocumentsAsync();
    }
}
```

---

## 7. Canvas Instance Management

### 7.1 Canvas Creation Strategy

**Approach:** On-demand creation per document

```csharp
// Each DocumentViewModel creates its own AnnoCanvas2 instance
private void InitializeCanvas(BuildingPresets presets, Dictionary<string, IconImage> icons)
{
    Canvas = new AnnoCanvas2(
        buildingPresets: presets,        // SHARED reference
        icons: icons,                    // SHARED reference
        appSettings: _services.AppSettings,  // SHARED reference
        coordinateHelper: _services.CoordinateHelper,  // SHARED reference
        brushCache: _services.BrushCache,   // SHARED reference
        penCache: _services.PenCache,       // SHARED reference
        messageBoxService: _services.MessageBoxService,  // SHARED reference
        localizationHelper: _services.LocalizationHelper,  // SHARED reference
        undoManager: _services.CreateUndoManager(),  // SCOPED instance
        layoutFileServiceFactory: factory => new LayoutFileService(...),  // SCOPED instance
        clipboardService: _services.ClipboardService  // SHARED reference
    );
}
```

### 7.2 Resource Sharing Matrix

| Resource           | Sharing Strategy      | Justification                          |
| ------------------ | --------------------- | -------------------------------------- |
| BuildingPresets    | Shared (Singleton)    | Read-only data, no mutations           |
| Icons Dictionary   | Shared (Singleton)    | Read-only data, significant memory     |
| BrushCache         | Shared (Singleton)    | Thread-safe cache, memory optimization |
| PenCache           | Shared (Singleton)    | Thread-safe cache, memory optimization |
| UndoManager        | Scoped (Per Document) | Document-specific history              |
| PlacedObjects      | Scoped (Per Document) | Document-specific content              |
| SelectedObjects    | Scoped (Per Document) | Document-specific selection            |
| Viewport Transform | Scoped (Per Document) | Document-specific view state           |
| LayoutFileService  | Scoped (Per Document) | Encapsulates document I/O              |

### 7.3 Memory Management

**Strategy:** Active document optimization

```csharp
public class DocumentViewModel
{
    private bool _isRendered = false;

    partial void OnIsActiveChanged(bool value)
    {
        if (value)
        {
            // Document became active
            EnsureRendered();
        }
        else
        {
            // Document became inactive
            if (_services.AppSettings.UnloadInactiveDocuments)
            {
                UnloadNonEssentialResources();
            }
        }
    }

    private void EnsureRendered()
    {
        if (!_isRendered)
        {
            Canvas.ForceRendering();
            _isRendered = true;
        }
    }

    private void UnloadNonEssentialResources()
    {
        // Optional: Clear cached visual trees for inactive documents
        // to reduce memory footprint
        Canvas.ClearCachedVisuals();
    }
}
```

---

## 8. Services and Dependencies

### 8.1 Service Lifetime Scopes

```csharp
// Singleton services (application lifetime)
services.AddSingleton<IAppSettings, AppSettings>();
services.AddSingleton<ICommons, Commons>();
services.AddSingleton<IBrushCache, BrushCache>();
services.AddSingleton<IPenCache, PenCache>();
services.AddSingleton<ICoordinateHelper, CoordinateHelper>();
services.AddSingleton<IMessageBoxService, MessageBoxService>();
services.AddSingleton<ILocalizationHelper, LocalizationHelper>();
services.AddSingleton<IClipboardService, ClipboardService>();
services.AddSingleton<ISharedResourceManager, SharedResourceManager>();

// Scoped services (per document)
services.AddScoped<IUndoManager, UndoManager>();
services.AddScoped<ILayoutFileService, LayoutFileService>();

// Transient (created on-demand)
services.AddTransient<ILayoutLoader, LayoutLoader>();
```

### 8.2 SharedResourceManager

```csharp
public interface ISharedResourceManager
{
    BuildingPresets BuildingPresets { get; }
    Dictionary<string, IconImage> Icons { get; }
    ICoordinateHelper CoordinateHelper { get; }
    IBrushCache BrushCache { get; }
    IPenCache PenCache { get; }
    ILayoutLoader LayoutLoader { get; }
}

public class SharedResourceManager : ISharedResourceManager
{
    public SharedResourceManager(
        BuildingPresets buildingPresets,
        Dictionary<string, IconImage> icons,
        ICoordinateHelper coordinateHelper,
        IBrushCache brushCache,
        IPenCache penCache,
        ILayoutLoader layoutLoader)
    {
        BuildingPresets = buildingPresets ?? throw new ArgumentNullException(nameof(buildingPresets));
        Icons = icons ?? throw new ArgumentNullException(nameof(icons));
        CoordinateHelper = coordinateHelper ?? throw new ArgumentNullException(nameof(coordinateHelper));
        BrushCache = brushCache ?? throw new ArgumentNullException(nameof(brushCache));
        PenCache = penCache ?? throw new ArgumentNullException(nameof(penCache));
        LayoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
    }

    public BuildingPresets BuildingPresets { get; }
    public Dictionary<string, IconImage> Icons { get; }
    public ICoordinateHelper CoordinateHelper { get; }
    public IBrushCache BrushCache { get; }
    public IPenCache PenCache { get; }
    public ILayoutLoader LayoutLoader { get; }
}
```

---

## 9. UI/UX Design

### 9.1 Updated MainWindow XAML

**File:** `AnnoDesigner\Views\MainWindow.xaml` (modifications)

```xml
<ui:FluentWindow>
  <Grid>
    <DockingManager>
      <LayoutRoot>
        <LayoutPanel Orientation="Horizontal">
          <!-- Left: Building Presets (unchanged) -->
          <LayoutAnchorablePane DockWidth="320">
            <LayoutAnchorable ContentId="PresetsPane">
              <BuildingPresetsPanel />
            </LayoutAnchorable>
          </LayoutAnchorablePane>

          <!-- Center: MODIFIED - Document Pane with dynamic tabs -->
          <LayoutDocumentPane>
            <LayoutDocumentPane.ItemsSource>
              <Binding Path="DocumentManager.Documents" />
            </LayoutDocumentPane.ItemsSource>

            <LayoutDocumentPane.ItemContainerStyle>
              <Style TargetType="LayoutDocument">
                <Setter Property="Title" Value="{Binding DocumentTitle}" />
                <Setter Property="ContentId" Value="{Binding DocumentId}" />
                <Setter Property="IsSelected" Value="{Binding IsActive, Mode=TwoWay}" />
                <Setter Property="CanClose" Value="True" />
                <Setter Property="CloseCommand" Value="{Binding CloseCommand}" />
              </Style>
            </LayoutDocumentPane.ItemContainerStyle>

            <LayoutDocumentPane.ItemTemplate>
              <DataTemplate>
                <ScrollViewer
                  CanContentScroll="True"
                  Focusable="False"
                  HorizontalScrollBarVisibility="Auto"
                  VerticalScrollBarVisibility="Auto">
                  <ContentPresenter Content="{Binding Canvas}" />
                </ScrollViewer>
              </DataTemplate>
            </LayoutDocumentPane.ItemTemplate>
          </LayoutDocumentPane>

          <!-- Right: MODIFIED - Context-sensitive panels -->
          <LayoutPanel DockWidth="380" Orientation="Vertical">
            <LayoutAnchorablePane>
              <LayoutAnchorable ContentId="StatisticsPane">
                <StatisticsView DataContext="{Binding DocumentManager.ActiveDocument.Statistics}" />
              </LayoutAnchorable>
            </LayoutAnchorablePane>
            <LayoutAnchorablePane>
              <LayoutAnchorable ContentId="PropertiesPane">
                <PropertiesPanel DataContext="{Binding DocumentManager.ActiveDocument.BuildingSettings}" />
              </LayoutAnchorable>
            </LayoutAnchorablePane>
          </LayoutPanel>
        </LayoutPanel>
      </LayoutRoot>
    </DockingManager>
  </Grid>
</ui:FluentWindow>
```

### 9.2 Tab Header Template

```xml
<LayoutDocumentPane.ItemContainerStyle>
  <Style TargetType="LayoutDocument">
    <Setter Property="Title">
      <Setter.Value>
        <MultiBinding Converter="{StaticResource DocumentTitleConverter}">
          <Binding Path="DocumentTitle" />
          <Binding Path="IsDirty" />
        </MultiBinding>
      </Setter.Value>
    </Setter>
  </Style>
</LayoutDocumentPane.ItemContainerStyle>
```

**DocumentTitleConverter:**

```csharp
public class DocumentTitleConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 2)
        {
            return "Document";
        }

        var title = values[0] as string ?? "Untitled";
        var isDirty = values[1] is bool dirty && dirty;

        return isDirty ? $"*{title}" : title;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

### 9.3 Context Menu for Document Tabs

```xml
<LayoutDocumentPane.ContextMenu>
  <ContextMenu>
    <MenuItem Header="Close" Command="{Binding CloseCommand}" />
    <MenuItem Header="Close All But This" Command="{Binding CloseAllButThisCommand}" />
    <MenuItem Header="Close All" Command="{Binding DataContext.DocumentManager.CloseAllDocumentsCommand, RelativeSource={RelativeSource AncestorType=Window}}" />
    <Separator />
    <MenuItem Header="Copy File Path" Command="{Binding CopyFilePathCommand}" />
    <MenuItem Header="Open Containing Folder" Command="{Binding OpenContainingFolderCommand}" />
  </ContextMenu>
</LayoutDocumentPane.ContextMenu>
```

---

## 10. Reimplementation plan — phases and acceptance criteria

This project has an existing MDT implementation. The rewrite aims to remove implementation ambiguity (UI vs VM ownership), increase test coverage, and ensure deterministic performance and predictable memory usage.

Phases (high level)

- Phase A: Stabilize & Align (2 weeks)

  - Replace the DataTemplate-based canvas creation with a scheme that displays the view-model-owned `DocumentViewModel.Canvas` instance in the document content. Guarantee one visual instance per document.
  - Convert the DataTemplate or LayoutDocument wiring to display `DocumentViewModel.Canvas` directly (e.g. set LayoutDocument.Content to the Canvas object, or create a ContentPresenter bound to Canvas).
  - Add unit and integration tests verifying displayed canvas instance == DocumentViewModel.Canvas and proving event subscription correctness.

- Phase B: Feature parity & hardening (2 weeks)

  - Remove SDI compatibility setters in `MainViewModel` once migrated to MDT bindings.
  - Ensure file operations (New/Open/Save/SaveAs/Close) work per-document and update Recent Files integration.
  - Add tests for Save/Close flows under multiple documents.

- Phase C: Performance & memory (2 weeks)

  - Implement optional hibernation (unload visual tree) for inactive documents.
  - Add memory telemetry and a configuration to limit open documents.
  - Confirm performance targets with benchmark tests.

- Phase D: Polish, accessibility & docs (1 week)
  - Keyboard shortcuts and accessibility improvements
  - End-to-end acceptance tests and user docs

Acceptance criteria (must be satisfied before merge):

1. Display correctness: the UI renders the exact `DocumentViewModel.Canvas` instance for every open tab; no duplicate or orphaned canvas instances are created by XAML DataTemplates.
2. Semantic correctness: all context panels (Statistics, Properties, BuildingSettings) always reflect the DocumentManager.ActiveDocument and update immediately when the active document changes.
3. Per-document isolation: undo/redo, selection, PlacedObjects and isDirty state are scoped to each document and aren't shared between documents.
4. File operations: Open and Save commands work per document; opening a file when it's already open selects the tab rather than loading a duplicate.
5. Robust shutdown: application exit or window close prompts for unsaved documents across all open documents, and cancelling prevents shutdown.
6. Tests: unit tests cover DocumentViewModel and DocumentManager behaviors; integration tests validate the displayed canvas instance, active-document switching, unsaved-close behavior, and a memory stress test covering 10 documents.

Deliverables for each phase include code changes, tests, and documentation updates.

**Deliverables:**

- Optimized performance
- Complete documentation
- Beta-ready build

---

## 11. Migration Path

### 11.1 Backward Compatibility

**Strategy:** Maintain single-document mode as default, enable MDT as opt-in feature

```csharp
// AppSettings.cs
public bool EnableMultiDocumentTabbing { get; set; } = false;
public int MaxOpenDocuments { get; set; } = 10;
```

### 11.2 Migration Steps for Existing Code

**Step 1:** Replace direct canvas references

```csharp
// OLD
var objects = mainViewModel.AnnoCanvas.PlacedObjects;

// NEW
var objects = mainViewModel.DocumentManager.ActiveDocument?.Canvas.PlacedObjects ?? [];
```

**Step 2:** Update event subscriptions

```csharp
// OLD
mainViewModel.AnnoCanvas.StatisticsUpdated += Handler;

// NEW
mainViewModel.DocumentManager.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(DocumentManager.ActiveDocument))
    {
        if (mainViewModel.DocumentManager.ActiveDocument != null)
        {
            mainViewModel.DocumentManager.ActiveDocument.Canvas.StatisticsUpdated += Handler;
        }
    }
};
```

**Step 3:** Update command bindings

```xml
<!-- OLD -->
<Button Command="{Binding AnnoCanvas.RotateCommand}" />

<!-- NEW -->
<Button Command="{Binding DocumentManager.ActiveDocument.Canvas.RotateCommand}" />
```

---

## 12. Performance Considerations

### 12.1 Rendering Optimization

**Strategy:** Only render active document

```csharp
protected override void OnRender(DrawingContext drawingContext)
{
    if (!IsActive)
    {
        // Skip rendering for inactive documents
        return;
    }

    base.OnRender(drawingContext);
}
```

### 12.2 Memory Footprint

**Estimated Memory per Document:**

- AnnoCanvas2 instance: ~2MB
- QuadTree structure: ~1MB (for 500 objects)
- Undo/Redo stack: ~0.5MB (20 operations)
- ViewModels: ~0.5MB
- **Total:** ~4MB per document

**Mitigation Strategies:**

1. Limit maximum open documents (default: 10)
2. Warn user when approaching memory limits
3. Optional: Unload visual tree for inactive documents
4. Implement document hibernation for rarely used tabs

### 12.3 Rendering Performance Benchmarks

**Target Metrics:**

- Document switch time: < 100ms
- New document creation: < 200ms
- Rendering 1000 objects: < 50ms (unchanged from current)

---

## 13. Edge Cases and Constraints

### 13.1 Maximum Documents Limit

```csharp
public DocumentViewModel CreateNewDocument()
{
    if (Documents.Count >= _appSettings.MaxOpenDocuments)
    {
        _messageBoxService.ShowWarning(
            $"Maximum of {_appSettings.MaxOpenDocuments} documents can be open simultaneously.",
            "Document Limit Reached"
        );
        return null;
    }

    // ... create document
}
```

### 13.2 Duplicate File Handling

```csharp
public async Task<DocumentViewModel> OpenDocumentAsync(string filePath)
{
    // Check if file is already open
    var existing = Documents.FirstOrDefault(d =>
        string.Equals(d.FilePath, filePath, StringComparison.OrdinalIgnoreCase));

    if (existing != null)
    {
        ActiveDocument = existing;
        return existing;
    }

    // ... open new document
}
```

### 13.3 Cross-Document Building Copy

**Consideration:** Buildings have references to shared presets but document-specific state

```csharp
public void CopyBuildingsToDocument(
    IEnumerable<LayoutObject> buildings,
    DocumentViewModel targetDocument)
{
    // Deep clone buildings to avoid shared state issues
    var clonedBuildings = buildings.Select(b => new LayoutObject(
        new AnnoObject(b.WrappedAnnoObject),
        _coordinateHelper,
        _brushCache,
        _penCache
    ));

    targetDocument.Canvas.PlacedObjects.AddRange(clonedBuildings);
    targetDocument.Canvas.RaiseStatisticsUpdated(UpdateStatisticsEventArgs.All);
}
```

### 13.4 Application Shutdown

```csharp
private async void MainWindow_Closing(object sender, CancelEventArgs e)
{
    e.Cancel = true;

    var canClose = await _mainViewModel.DocumentManager.CloseAllDocumentsAsync();

    if (canClose)
    {
        e.Cancel = false;
        Application.Current.Shutdown();
    }
}
```

---

## 14. Appendix

### 14.0 Lessons Learned & Reimplementation Guidance

This section provides critical guidance for the future reimplementation effort based on analysis of the current partial implementation.

#### 14.0.1 Canvas Instance Management: Critical Best Practices

**Problem Observed:**
The current implementation creates canvas instances in DocumentViewModel constructor via `InitializeCanvas()`, but MainWindow.xaml's DataTemplate creates a NEW instance via `<cv2:AnnoCanvas2 DataContext="{Binding Canvas}"/>`.

**Root Cause:**
Misunderstanding of WPF DataTemplate behavior - templates INSTANTIATE controls, they don't DISPLAY existing instances.

**Correct Approach for Rewrite:**

```xml
<!-- WRONG (current implementation): Creates new canvas instance -->
<LayoutDocumentPane.ItemTemplate>
  <DataTemplate>
    <ScrollViewer>
      <cv2:AnnoCanvas2 DataContext="{Binding Canvas}"/>  <!-- WRONG: new instance -->
    </ScrollViewer>
  </DataTemplate>
</LayoutDocumentPane.ItemTemplate>

<!-- CORRECT (for rewrite): Displays existing canvas instance -->
<LayoutDocumentPane.ItemTemplate>
  <DataTemplate>
    <ScrollViewer>
      <ContentPresenter Content="{Binding Canvas}"/>  <!-- CORRECT: displays existing -->
    </ScrollViewer>
  </DataTemplate>
</LayoutDocumentPane.ItemTemplate>

<!-- ALTERNATIVE CORRECT (direct binding): -->
<LayoutDocument Title="{Binding DocumentTitle}">
  <LayoutDocument.Content>
    <Binding Path="Canvas"/>  <!-- Directly binds to existing instance -->
  </LayoutDocument.Content>
</LayoutDocument>
```

**Validation Test:**

```csharp
[Fact]
public void DisplayedCanvasInstance_MatchesViewModelCanvas()
{
    // This test MUST pass in the rewrite
    var mainWindow = GetMainWindow();
    var doc = mainWindow.DocumentManager.ActiveDocument;

    // Get the displayed canvas from visual tree
    var layoutDocument = FindLayoutDocumentForViewModel(mainWindow, doc);
    var displayedCanvas = FindVisualChild<AnnoCanvas2>(layoutDocument);

    // CRITICAL: They must be the SAME instance
    Assert.Same(doc.Canvas, displayedCanvas);
}
```

#### 14.0.2 Event Wiring Architecture

**Problem Observed:**
Two competing event subscription patterns:

1. `MainViewModel.AnnoCanvas` property setter wires events (SDI legacy)
2. `MainViewModel.DocumentManager_PropertyChanged` handles ActiveDocument changes (MDT new)

**Correct Approach for Rewrite:**

**Single Source of Truth Pattern:**

```csharp
public class MainViewModel
{
    private IAnnoCanvas _currentCanvas;

    private void OnActiveDocumentChanged(DocumentViewModel newActiveDoc)
    {
        // Unsubscribe from old canvas
        if (_currentCanvas != null)
        {
            _currentCanvas.StatisticsUpdated -= OnCanvasStatisticsUpdated;
            _currentCanvas.OnStatusMessageChanged -= OnCanvasStatusChanged;
            // ... all other events
        }

        // Subscribe to new canvas
        if (newActiveDoc?.Canvas != null)
        {
            _currentCanvas = newActiveDoc.Canvas;
            _currentCanvas.StatisticsUpdated += OnCanvasStatisticsUpdated;
            _currentCanvas.OnStatusMessageChanged += OnCanvasStatusChanged;
            // ... all other events
        }

        // Update dependent properties
        OnPropertyChanged(nameof(AnnoCanvas));
        OnPropertyChanged(nameof(StatisticsViewModel));
        OnPropertyChanged(nameof(BuildingSettingsViewModel));
    }
}
```

**Anti-Pattern to Avoid:**

```csharp
// DON'T DO THIS (creates dual subscription paths)
public IAnnoCanvas AnnoCanvas
{
    get => _currentCanvas;
    set
    {
        if (_currentCanvas != value)
        {
            // DON'T wire events in setter - use PropertyChanged handler instead
            _currentCanvas = value;
        }
    }
}
```

#### 14.0.3 Test Strategy: Integration Tests First

**Lesson Learned:**
Unit tests for DocumentViewModel and DocumentManager were created but integration tests proving the UI/ViewModel connection were missing.

**Recommended Test-Driven Approach for Rewrite:**

**Phase 1: Write Failing Integration Tests**

```csharp
public class MDT_IntegrationTests
{
    [Fact]
    public void Test01_DisplayedCanvas_IsViewModelOwnedInstance()
    {
        // Acceptance: UI displays DocumentViewModel.Canvas, not a copy
    }

    [Fact]
    public void Test02_ActiveDocumentSwitch_UpdatesContextPanels()
    {
        // Acceptance: Statistics/Properties panels show active doc data
    }

    [Fact]
    public void Test03_InactiveDocumentEvents_DoNotPropagate()
    {
        // Acceptance: Only active document raises events to MainViewModel
    }

    [Fact]
    public void Test04_DocumentDispose_UnsubscribesAllEvents()
    {
        // Acceptance: No memory leaks from event handlers
    }

    [Fact]
    public void Test05_TenDocuments_MemoryWithinLimits()
    {
        // Acceptance: 10 docs use < 100MB total
    }
}
```

**Phase 2: Implement Until Tests Pass**
Only when ALL integration tests pass should the feature be considered complete.

#### 14.0.4 Source Code Organization

**Current State (Scattered):**

- Technical spec: `docs/specs/MultiDocumentTabbing_TechnicalSpec.md` (design)
- Progress docs: Phase1_Summary, Phase2_Progress, Phase2_Complete, Tracker (6+ files)
- Code changes: Across ViewModels/, Services/, Tests/
- Related but separate: AnnoCanvasRefactoring specs (2 files)

**Recommended Structure for Rewrite:**

```
docs/
├── specs/
│   └── MultiDocumentTabbing_TechnicalSpec.md  (THIS FILE - single source of truth)
├── decisions/
│   └── MDT_ArchitectureDecisionLog.md  (why we chose X over Y)
└── progress/
    └── MDT_ImplementationLog.md  (daily progress, separate from spec)

Tests/
├── AnnoDesigner.Tests.Integration/  (NEW - integration tests)
│   └── MultiDocumentTabbing/
│       ├── CanvasInstanceTests.cs
│       ├── EventWiringTests.cs
│       ├── MemoryLeakTests.cs
│       └── UIBindingTests.cs
└── AnnoDesigner.Tests/  (existing - unit tests)
    ├── ViewModels/DocumentViewModelTests.cs
    └── Services/DocumentManagerTests.cs
```

**Separation of Concerns:**

- **Technical Spec (this file):** What to build, how it should work (timeless)
- **Architecture Decision Log:** Why we chose approach X (learning record)
- **Implementation Log:** What we did today (daily journal, ephemeral)

#### 14.0.5 Build & Compilation Strategy

**Problem Observed:**
Tests failed to compile because CommunityToolkit.Mvvm source generators hadn't run yet, leaving generated properties inaccessible.

**Solution for Rewrite:**

**Option A: Pre-build Main Project**

```xml
<!-- In Tests.csproj -->
<ItemGroup>
  <ProjectReference Include="..\AnnoDesigner\AnnoDesigner.csproj">
    <ReferenceOutputAssembly>true</ReferenceOutputAssembly>
    <OutputItemType>Analyzer</OutputItemType>  <!-- Ensures generators run first -->
  </ProjectReference>
</ItemGroup>
```

**Option B: Avoid Generated Properties in Tests**

```csharp
// Instead of testing generated properties directly:
// var title = doc.DocumentTitle;  // Generated property

// Test through public interface:
INotifyPropertyChanged inpc = doc;
inpc.PropertyChanged += (s, e) =>
{
    if (e.PropertyName == nameof(DocumentViewModel.DocumentTitle))
    {
        // Verify behavior without directly accessing generated property
    }
};
```

**Option C: Manual Properties for Testability**

```csharp
// If source generation causes issues, use manual implementation for critical properties
public partial class DocumentViewModel : ObservableObject
{
    // Manual property (always visible to tests)
    private string _documentTitle;
    public string DocumentTitle
    {
        get => _documentTitle;
        set => SetProperty(ref _documentTitle, value);
    }

    // Use [ObservableProperty] for less critical properties
    [ObservableProperty]
    private bool _isActive;
}
```

#### 14.0.6 Memory Management: Measured Reality vs Estimates

**Estimated in Original Spec:**

- AnnoCanvas2 instance: ~2MB
- QuadTree structure: ~1MB (for 500 objects)
- Undo/Redo stack: ~0.5MB
- ViewModels: ~0.5MB
- **Total Estimate:** ~4MB per document

**Action Required for Rewrite:**
MEASURE actual memory usage before finalizing limits:

```csharp
[Fact]
public void MeasureActualMemoryFootprint()
{
    var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

    var docs = new List<DocumentViewModel>();
    for (int i = 0; i < 10; i++)
    {
        var doc = CreateDocumentWithTypicalLayout(); // 100-500 objects
        docs.Add(doc);
    }

    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
    var memoryPerDoc = (finalMemory - initialMemory) / 10;

    _output.WriteLine($"Actual memory per document: {memoryPerDoc / 1024 / 1024:F2} MB");

    // Update spec with MEASURED value, not estimated
    Assert.True(memoryPerDoc < 10 * 1024 * 1024, "Document uses more than 10MB");
}
```

**Update Configuration Based on Measurements:**
If actual memory is 8MB/doc instead of 4MB/doc, adjust MaxOpenDocuments accordingly.

#### 14.0.7 Phased Rewrite Strategy

**Don't Repeat This Mistake:**
Original implementation attempted big-bang integration (all phases at once), leading to:

- UI/ViewModel mismatch discovered late
- No intermediate validation points
- Hard to isolate which change broke what

**Recommended Incremental Approach:**

**Milestone 1: Single Document in Tab (2 days)**

- Goal: Prove canvas instance rendering works
- Deliverable: MainWindow shows 1 tab with correct canvas instance
- Test: `DisplayedCanvasInstance_MatchesViewModelCanvas()` passes
- **DO NOT** proceed until this test passes

**Milestone 2: Two Documents, No Switching (1 day)**

- Goal: Prove multiple canvas instances coexist
- Deliverable: Can create 2nd document, both tabs visible
- Test: Both canvas instances are distinct and correct

**Milestone 3: Active Document Switching (2 days)**

- Goal: Prove event wiring switches correctly
- Deliverable: Clicking tabs updates context panels
- Test: `ActiveDocumentSwitch_UpdatesContextPanels()` passes

**Milestone 4: File Operations (3 days)**

- Goal: Open/Save/Close work per-document
- Deliverable: Full CRUD for documents
- Test: `DocumentLifecycle_Integration()` passes

**Milestone 5: Memory & Performance (2 days)**

- Goal: Prove no leaks, acceptable performance
- Deliverable: Can handle 10 documents without issues
- Test: `TenDocuments_MemoryWithinLimits()` passes

**Gate Between Milestones:**
ALL tests for Milestone N must pass before starting Milestone N+1.

#### 14.0.8 Critical Questions to Answer BEFORE Coding

**Question 1: Canvas Display Mechanism**

- [ ] Decision made: ContentPresenter vs Direct Content binding vs Custom control?
- [ ] Prototype validated: Does chosen approach display existing instance?
- [ ] Test written: Can we verify displayed instance == ViewModel instance?

**Question 2: Event Subscription Lifetime**

- [ ] Decision made: Subscribe in PropertyChanged handler or ViewModel constructor?
- [ ] Error handling: What if subscription throws? How to recover?
- [ ] Cleanup verified: Do we unsubscribe on document close?

**Question 3: Service Lifetime Boundaries**

- [ ] Decision made: Which services are truly singleton vs scoped?
- [ ] Memory measured: What's actual cost of scoped services?
- [ ] DI validated: Can we resolve DocumentViewModel correctly?

**Question 4: Testing Approach**

- [ ] Decision made: Unit tests only or integration tests required?
- [ ] Test environment: Can we instantiate MainWindow in tests?
- [ ] CI integration: Will tests run in build pipeline?

**Answer ALL questions with prototypes BEFORE starting full implementation.**

#### 14.0.9 Success Criteria Checklist

**DO NOT consider rewrite complete until:**

**Functional Requirements:**

- [ ] Can open 10 documents simultaneously
- [ ] Each document has independent undo/redo stack
- [ ] Clicking tab switches active document instantly (< 100ms)
- [ ] Context panels (Statistics, Properties) update on switch
- [ ] Closing dirty document prompts to save
- [ ] Copy-paste between documents works
- [ ] Recent files opens in new tabs
- [ ] Drag-drop files opens in new tabs
- [ ] Application shutdown checks all documents for unsaved changes

**Technical Requirements:**

- [ ] Integration test proves displayed canvas == ViewModel.Canvas
- [ ] Integration test proves only active document events propagate
- [ ] Memory test proves 10 documents < 100MB total
- [ ] No memory leaks detected (dispose test passes)
- [ ] Build completes without errors in < 2 minutes
- [ ] All unit tests pass (> 95% coverage for new code)
- [ ] All integration tests pass (100% coverage of critical paths)

**Code Quality Requirements:**

- [ ] All public APIs have XML documentation
- [ ] No duplicate code between SDI and MDT paths
- [ ] No compiler warnings
- [ ] No ReSharper/Rider warnings
- [ ] Code review completed by 2+ reviewers

**Documentation Requirements:**

- [ ] Technical spec updated with actual implementation
- [ ] Architecture decision log documents key choices
- [ ] User guide explains how to use MDT features
- [ ] Developer guide explains extension points

#### 14.0.10 Red Flags to Watch For

**During Rewrite, STOP and Re-evaluate if:**

🚩 **"We'll fix the test later"**

- Tests define acceptance criteria; if test doesn't pass, feature isn't done
- Action: Fix the code until test passes, or prove test is wrong

🚩 **"The DataTemplate should work, I don't understand why it doesn't"**

- WPF templates instantiate, they don't display
- Action: Switch to ContentPresenter immediately

🚩 **"Memory usage seems high but it's probably fine"**

- Estimates are often wrong; measure before shipping
- Action: Run memory profiler, update limits based on reality

🚩 **"This quick hack will work for now"**

- Technical debt compounds; temporary solutions become permanent
- Action: Do it right the first time, or schedule explicit refactor task

🚩 **"Let's just merge this and test in production"**

- Integration environment exists for a reason
- Action: Full test pass on dev/staging before merge

#### 14.0.11 Key Takeaways Summary

**Top 5 Lessons for Rewrite:**

1. **Canvas Display:** Use `ContentPresenter Content="{Binding Canvas}"`, NOT `<AnnoCanvas2 DataContext="{Binding Canvas}"/>` (DataTemplate creates new instances)

2. **Event Wiring:** Centralize in ONE place (ActiveDocument PropertyChanged handler), remove ALL setter-based wiring

3. **Test First:** Write integration tests defining acceptance criteria BEFORE implementing UI bindings

4. **Measure Reality:** Don't estimate memory/performance - measure with profiler and update limits accordingly

5. **Incremental Milestones:** Validate each milestone with passing tests before moving to next; don't attempt big-bang integration

**Document Maintenance:**

- Keep this technical spec focused on "what to build" (design)
- Maintain separate progress log for "what we did" (journal)
- Update this spec's "Lessons Learned" section after rewrite with actual findings

### 14.1 Glossary

- **MDT:** Multi Document Tabbing
- **SDI:** Single Document Interface
- **Active Document:** The currently focused/selected document tab
- **Document Context:** The scope of data and services specific to one document
- **Shared Resources:** Data or services shared across all documents (e.g., BuildingPresets)
- **Scoped Service:** Service instance unique to each document
- **Singleton Service:** Service instance shared across the entire application

### 14.4 Configuration Options

**AppSettings additions:**

```json
{
  "MultiDocumentTabbing": {
    "Enabled": true,
    "MaxOpenDocuments": 10,
    "UnloadInactiveDocuments": false,
    "WarnOnMemoryLimit": true,
    "MemoryLimitMB": 500
  }
}
```

---

## Document Revision History

| Version | Date       | Author           | Changes               |
| ------- | ---------- | ---------------- | --------------------- |
| 1.0     | 2025-01-XX | Development Team | Initial specification |

---

**END OF SPECIFICATION**
