# Multi Document Tabbing (MDT) Technical Specification

**Version:** 1.1  
**Date:** 2025-11-27  
**Status:** Implemented (partial) — REWRITE REQUIRED  
**Target Framework:** .NET 10  
**Primary Contact:** Development Team

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Background and Motivation](#2-background-and-motivation)
3. [Current Architecture Analysis](#3-current-architecture-analysis)
4. [Proposed Architecture](#4-proposed-architecture)
5. [Document Model](#5-document-model)
6. [ViewModel Structure](#6-viewmodel-structure)
7. [Canvas Instance Management](#7-canvas-instance-management)
8. [Services and Dependencies](#8-services-and-dependencies)
9. [UI/UX Design](#9-uiux-design)
10. [Implementation Phases](#10-implementation-phases)
11. [Migration Path](#11-migration-path)
12. [Testing Strategy](#12-testing-strategy)
13. [Performance Considerations](#13-performance-considerations)
14. [Edge Cases and Constraints](#14-edge-cases-and-constraints)
15. [Appendix](#15-appendix)

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

### 3.0 Implementation inventory & mismatch summary (as of repository state)

This section provides a comprehensive analysis of the current MDT implementation state, documenting what has been built, what works, what doesn't, and critical lessons learned for the future reimplementation.

#### 3.0.1 Implementation Discovery Summary

Implemented (found in repo):
- DocumentViewModel: `AnnoDesigner/ViewModels/DocumentViewModel.cs` — manages a per-document AnnoCanvas2 instance, undo manager, document state and document-scoped viewmodels.
- DocumentManager: `AnnoDesigner/Services/DocumentManager.cs` + `IDocumentManager.cs` — documents collection, ActiveDocument, open/close/closeAll lifecycle.
- DocumentServicesFactory + DocumentServices: `AnnoDesigner/Services/DocumentServicesFactory.cs` — scoped services factory, CreateUndoManager and shared service references.
- SharedResourceManager: `AnnoDesigner/Services/SharedResourceManager.cs` — presets, icons, caches and LayoutLoader; helper LoadDocumentInto method.
- Integration wiring: `MainViewModel` has DocumentManager and subscribes to ActiveDocument changes; `MainWindow.xaml` binds DockingManager.DocumentsSource to DocumentManager.Documents and ActiveContent to ActiveDocument.
- AnnoCanvas2: `AnnoDesigner/Controls/Canvas/AnnoCanvas.xaml.cs` — the UI control used as per-document surface.
- Tests: unit tests exist for DocumentViewModel and DocumentManager.

#### 3.0.2 Critical Issues Discovered

**Issue #1: UI/ViewModel Canvas Instance Mismatch (CRITICAL)**

- UI vs ViewModel mismatch: `MainWindow.xaml`'s DataTemplate for `DocumentViewModel` instantiates its own `AnnoCanvas2` control instead of rendering the `DocumentViewModel.Canvas` instance. This can cause duplicate/incorrect canvas instances being displayed versus the canvas instance the view model owns and wires up.
- **Root Cause:** DataTemplate in MainWindow.xaml creates new canvas via `<cv2:AnnoCanvas2 DataContext="{Binding Canvas}"/>` instead of displaying the actual `DocumentViewModel.Canvas` property instance
- **Impact:** Event handlers wired in DocumentViewModel may fire on wrong instance; displayed canvas may not be the one the ViewModel manages
- **Evidence:** See `Tests\AnnoDesigner.Tests\ViewModels\DocumentViewModelTests.cs` and `Tests\AnnoDesigner.Tests\Services\DocumentManagerTests.cs` which were created but have compilation errors

**Issue #2: Residual SDI Compatibility Code**

- Residual SDI compat code: MainViewModel exposes an `AnnoCanvas` property with a setter that wires event handlers to maintain backward compatibility; this duplicate path should be removed in the final rewrite.
- **Location:** `MainViewModel.AnnoCanvas` property setter
- **Impact:** Two code paths for canvas event wiring (old setter-based, new DocumentManager-based) causing confusion and potential bugs
- **Recommendation:** Remove setter once migrated to pure MDT

**Issue #3: Missing Integration Test Coverage**

- Limited integration tests: while unit tests exist, a set of integration tests verifying that the displayed canvas instance equals DocumentViewModel.Canvas, per-document event wiring, and context-panel updating on ActiveDocument changes are missing or incomplete.
- **Required Tests:**
    1. Visual tree validation: ensure displayed canvas instance == DocumentViewModel.Canvas
    2. Active document switching: verify context panels (Statistics, BuildingSettings, LayoutSettings) update when ActiveDocument changes
    3. Event subscription verification: confirm only active document events propagate to MainViewModel
    4. Memory leak detection: validate Dispose() properly cleans up all document resources

#### 3.0.3 Test File Analysis

From the changed files, we discovered:

**DocumentManagerTests.cs** (newly created):
- 13 test methods covering document lifecycle (create, open, close, closeAll)
- Tests document collection management and ActiveDocument switching
- **Compilation Issue:** Uses mocks that reference interfaces requiring additional using directives
- **Status:** Framework complete, needs namespace fixes

**DocumentViewModelTests.cs** (newly created):
- 15 test methods covering initialization, canvas creation, event handling, IsDirty tracking
- Tests SaveAsync, CheckUnsavedChangesAsync, Dispose pattern
- **Compilation Issue:** CommunityToolkit.Mvvm source-generated properties not visible during test compilation
- **Status:** Framework complete, needs full rebuild with source generators enabled

**Lesson Learned:** Source generation timing requires careful build order; tests referencing generated properties must wait for generator completion.

#### 3.0.4 Documentation Files Created

The following documentation was created during initial implementation:
- `docs/doc/CommandLineParameters.md` - CLI documentation (not MDT-related)
- `docs/doc/Enable_DebugMode.md` - Debug mode documentation (not MDT-related)
- `docs/doc/Optimize_images.md` - Image optimization guide (not MDT-related)
- `docs/doc/PredefinedColors.md` - Color preset documentation (not MDT-related)
- `docs/doc/Release_Workflow.md` - Release process documentation (not MDT-related)
- `docs/specs/AnnoCanvasRefactoring_TechnicalSpec.md` - Separate refactoring spec for AnnoCanvas2 MVVM conversion
- `docs/specs/AnnoCanvasRefactoring_Tracker.md` - Tracker for canvas refactoring (related but separate initiative)
- `docs/specs/MultiDocumentTabbing_Phase1_Summary.md` - Summary of Phase 1 completion (progress doc, not technical)
- `docs/specs/MultiDocumentTabbing_Phase2_Progress.md` - Phase 2 progress tracking (progress doc)
- `docs/specs/MultiDocumentTabbing_Phase2_Complete.md` - Phase 2 completion summary (progress doc)
- `docs/specs/MultiDocumentTabbing_Tracker.md` - Task tracking document (progress doc)
- `docs/specs/Phase1_Complete_ReadyForPhase2.md` - Phase transition document (progress doc)

**Lesson Learned:** The original spec morphed into multiple progress-tracking documents. For the rewrite, maintain a single authoritative technical spec and separate progress tracking from technical design.

#### 3.0.5 Key Architectural Findings

**What Works:**
1. ✅ DocumentViewModel properly encapsulates per-document state
2. ✅ DocumentManager ObservableCollection pattern works for tab synchronization
3. ✅ Service lifetime scoping (singleton shared, scoped per-doc) is sound
4. ✅ Disposal pattern implementation prevents memory leaks (when properly invoked)
5. ✅ AvalonDock DocumentsSource binding integrates cleanly with MVVM

**What Doesn't Work:**
1. ❌ DataTemplate-based canvas instantiation breaks ViewModel ownership
2. ❌ Dual code paths (SDI setter + MDT events) cause confusion
3. ❌ Integration tests incomplete - no verification of displayed instance
4. ❌ Test compilation blocked by source generation timing
5. ❌ No memory stress testing (e.g., open 10 documents, measure actual footprint)

**Critical Lessons for Rewrite:**
1. **Canvas Display Strategy:** Use ContentPresenter or direct Content binding to DocumentViewModel.Canvas, NOT DataTemplate instantiation
2. **Event Wiring:** Centralize in DocumentManager.ActiveDocument PropertyChanged handler; remove all setter-based wiring
3. **Testing Strategy:** Build integration tests FIRST to define acceptance criteria, then implement
4. **Build Process:** Configure source generators to run before test compilation or structure tests to avoid generated properties
5. **Documentation:** Keep technical spec focused on "what to build" and separate from "what was built" progress tracking

Outcome: the repository already contains a lot of the intended MDT functionality — the rewrite will focus on addressing the UI/ViewModel mismatch, improving test coverage, and consolidating the implementation into a clean, well-tested, maintainable architecture.

### 3.1 MainWindow Structure

**File:** `AnnoDesigner\Views\MainWindow.xaml`

Current layout hierarchy:
```xml
<ui:FluentWindow>
  <Grid>
    <DockingManager>
      <LayoutRoot>
        <LayoutPanel Orientation="Horizontal">
          <!-- Left: Building Presets Panel -->
          <LayoutAnchorablePane DockWidth="320">
            <LayoutAnchorable ContentId="PresetsPane">
              <BuildingPresetsPanel />
            </LayoutAnchorable>
          </LayoutAnchorablePane>

          <!-- Center: Single Document -->
          <LayoutDocumentPane>
            <LayoutDocument ContentId="MainDocument2">
              <ScrollViewer>
                <AnnoCanvas2 x:Name="annoCanvas" />
              </ScrollViewer>
            </LayoutDocument>
          </LayoutDocumentPane>

          <!-- Right: Statistics & Properties -->
          <LayoutPanel DockWidth="380">
            <LayoutAnchorablePane>
              <LayoutAnchorable ContentId="StatisticsPane">
                <StatisticsView />
              </LayoutAnchorable>
            </LayoutAnchorablePane>
            <LayoutAnchorablePane>
              <LayoutAnchorable ContentId="PropertiesPane">
                <PropertiesPanel />
              </LayoutAnchorable>
            </LayoutAnchorablePane>
          </LayoutPanel>
        </LayoutPanel>
      </LayoutRoot>
    </DockingManager>
  </Grid>
</ui:FluentWindow>
```

**Key Issues:**
- Single `AnnoCanvas2` instance directly referenced in XAML
- Direct binding to `MainViewModel.AnnoCanvas` property
- No document collection or active document concept

### 3.2 MainViewModel Structure

**File:** `AnnoDesigner\ViewModels\MainViewModel.cs`

Current properties:
```csharp
public class MainViewModel : Notify
{
    private IAnnoCanvas _annoCanvas; // Single instance
    
    public IAnnoCanvas AnnoCanvas { get; set; }
    
    // Canvas rendering properties (applied to single canvas)
    public bool CanvasShowGrid { get; set; }
    public bool CanvasShowIcons { get; set; }
    public bool CanvasShowLabels { get; set; }
    
    // File operations
    public Task OpenFile(string filePath, bool forceLoad = false)
    public void SaveFile(string filePath)
    public void OpenLayout(LayoutFile layout)
    
    // Commands
    public ICommand LoadLayoutFromJsonCommand { get; }
    public ICommand ExportImageCommand { get; }
    public ICommand CopyLayoutToClipboardCommand { get; }
    
    // Statistics tied to single canvas
    public StatisticsViewModel StatisticsViewModel { get; }
    
    // Building settings tied to single canvas
    public BuildingSettingsViewModel BuildingSettingsViewModel { get; }
}
```

**Key Dependencies:**
- Direct coupling to single canvas instance
- Statistics and building settings assume single active document
- File operations directly modify the single canvas

### 3.3 AnnoCanvas2 Architecture

**File:** `AnnoDesigner\Controls\Canvas\AnnoCanvas.xaml.cs`

Key responsibilities:
```csharp
public class AnnoCanvas2 : FrameworkElement, IAnnoCanvas, IScrollInfo
{
    // State
    public QuadTree<LayoutObject> PlacedObjects { get; set; }
    public HashSet<LayoutObject> SelectedObjects { get; set; }
    public List<LayoutObject> CurrentObjects { get; }
    public string LoadedFile { get; set; }
    
    // Rendering
    public int GridSize { get; set; }
    public bool RenderGrid { get; set; }
    public bool RenderInfluences { get; set; }
    public bool RenderIcon { get; set; }
    public bool RenderLabel { get; set; }
    
    // Services (injected)
    internal ILayoutLoader _layoutLoader;
    internal ILayoutFileService _layoutFileService;
    internal ICoordinateHelper _coordinateHelper;
    internal IBrushCache _brushCache;
    internal IPenCache _penCache;
    internal IMessageBoxService _messageBoxService;
    internal ILocalizationHelper _localizationHelper;
    private TransformService _transformService;
    private LayoutModelService _layoutModelService;
    private SelectionService _selectionService;
    
    // Undo/Redo
    public IUndoManager UndoManager { get; }
    
    // Events
    public event EventHandler<UpdateStatisticsEventArgs> StatisticsUpdated;
    public event EventHandler<FileLoadedEventArgs> OnLoadedFileChanged;
    public event Action<string> OnStatusMessageChanged;
}
```

**Shared Resources:**
- `BuildingPresets` - Shared across all documents
- `Icons` - Shared across all documents
- Service instances - Can be shared or per-document

**Document-Specific State:**
- `PlacedObjects` - Unique per document
- `SelectedObjects` - Unique per document
- `LoadedFile` - Unique per document
- `UndoManager` - Should be unique per document
- Viewport/Transform state - Unique per document

---

## 4. Proposed Architecture

### 4.1 Architecture Overview

```
???????????????????????????????????????????????????????????????????
?                         MainWindow                               ?
?  ?????????????????????????????????????????????????????????????  ?
?  ?                    MainViewModel                           ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?  ?         DocumentManager                             ?  ?  ?
?  ?  ?  - ObservableCollection<DocumentViewModel>          ?  ?  ?
?  ?  ?  - DocumentViewModel ActiveDocument                 ?  ?  ?
?  ?  ?  - CreateDocument()                                 ?  ?  ?
?  ?  ?  - CloseDocument(DocumentViewModel)                 ?  ?  ?
?  ?  ?  - SwitchActiveDocument(DocumentViewModel)          ?  ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?                                                             ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?  ?         SharedResourceManager                       ?  ?  ?
?  ?  ?  - BuildingPresets                                  ?  ?  ?
?  ?  ?  - Icons Dictionary                                 ?  ?  ?
?  ?  ?  - IAppSettings                                     ?  ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                   ?
?  ?????????????????????????????????????????????????????????????  ?
?  ?              LayoutDocumentPane (AvalonDock)              ?  ?
?  ?  ????????????????????????????????????????????????????    ?  ?
?  ?  ? DocumentView 1 ? DocumentView 2 ? DocumentView N ?    ?  ?
?  ?  ?  ????????????  ?  ????????????  ?  ????????????  ?    ?  ?
?  ?  ?  ?AnnoCanvas?  ?  ?AnnoCanvas?  ?  ?AnnoCanvas?  ?    ?  ?
?  ?  ?  ?Instance 1?  ?  ?Instance 2?  ?  ?Instance N?  ?    ?  ?
?  ?  ?  ????????????  ?  ????????????  ?  ????????????  ?    ?  ?
?  ?  ?                ?                ?                ?    ?  ?
?  ?  ?  DataContext:  ?  DataContext:  ?  DataContext:  ?    ?  ?
?  ?  ?  DocViewModel1 ?  DocViewModel2 ?  DocViewModelN ?    ?  ?
?  ?  ????????????????????????????????????????????????????    ?  ?
?  ?????????????????????????????????????????????????????????????  ?
?                                                                   ?
?  ?????????????????????????????????????????????????????????????  ?
?  ?                  Context Panels (Right Side)               ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?  ? StatisticsView                                      ?  ?  ?
?  ?  ?   DataContext: ActiveDocument.StatisticsViewModel   ?  ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?  ? PropertiesPanel                                     ?  ?  ?
?  ?  ?   DataContext: ActiveDocument.BuildingSettings VM   ?  ?  ?
?  ?  ???????????????????????????????????????????????????????  ?  ?
?  ?????????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????????
```

### 4.2 Key Architectural Principles

1. **Document Encapsulation:** Each document maintains its own complete state
2. **Shared Resources:** Presets, icons, and settings shared across documents
3. **Active Document Pattern:** Context panels bind to active document only
4. **Service Lifetime Management:** Scoped services per document, singleton for shared
5. **Event Isolation:** Document events don't cross-contaminate
6. **Memory Efficiency:** Lazy loading and resource pooling where appropriate

---

## 5. Document Model

### 5.1 DocumentViewModel

**Location:** `AnnoDesigner\ViewModels\DocumentViewModel.cs`

```csharp
using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services.Undo;

namespace AnnoDesigner.ViewModels
{
    /// <summary>
    /// Represents a single document (layout) in the multi-document interface.
    /// Each instance manages its own canvas, undo stack, and document-specific state.
    /// </summary>
    public partial class DocumentViewModel : ObservableObject
    {
        #region Fields
        
        private readonly IDocumentServices _services;
        
        [ObservableProperty]
        private string _documentTitle;
        
        [ObservableProperty]
        private string _filePath;
        
        [ObservableProperty]
        private bool _isDirty;
        
        [ObservableProperty]
        private bool _isActive;
        
        [ObservableProperty]
        private Guid _documentId;
        
        [ObservableProperty]
        private IAnnoCanvas _canvas;
        
        [ObservableProperty]
        private StatisticsViewModel _statistics;
        
        [ObservableProperty]
        private BuildingSettingsViewModel _buildingSettings;
        
        [ObservableProperty]
        private LayoutSettingsViewModel _layoutSettings;
        
        #endregion
        
        #region Constructor
        
        public DocumentViewModel(
            IDocumentServices services,
            BuildingPresets buildingPresets,
            Dictionary<string, IconImage> icons)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _documentId = Guid.NewGuid();
            _documentTitle = "Untitled";
            
            InitializeCanvas(buildingPresets, icons);
            InitializeViewModels();
            RegisterEventHandlers();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeCanvas(
            BuildingPresets buildingPresets, 
            Dictionary<string, IconImage> icons)
        {
            // Create new canvas instance with scoped services
            Canvas = new AnnoCanvas2(
                buildingPresets,
                icons,
                _services.AppSettings,
                _services.CoordinateHelper,
                _services.BrushCache,
                _services.PenCache,
                _services.MessageBoxService,
                _services.LocalizationHelper,
                _services.CreateUndoManager(), // Scoped per document
                layoutFileServiceFactory => new LayoutFileService(
                    layoutFileServiceFactory,
                    _services.MessageBoxService,
                    _services.LocalizationHelper),
                _services.ClipboardService
            );
            
            // Apply default rendering settings
            Canvas.RenderGrid = _services.AppSettings.ShowGrid;
            Canvas.RenderIcon = _services.AppSettings.ShowIcons;
            Canvas.RenderLabel = _services.AppSettings.ShowLabels;
            Canvas.RenderInfluences = _services.AppSettings.ShowInfluences;
            Canvas.RenderTrueInfluenceRange = _services.AppSettings.ShowTrueInfluenceRange;
            Canvas.RenderHarborBlockedArea = _services.AppSettings.ShowHarborBlockedArea;
            Canvas.RenderPanorama = _services.AppSettings.ShowPanorama;
        }
        
        private void InitializeViewModels()
        {
            // Create document-specific view models
            Statistics = new StatisticsViewModel(
                _services.LocalizationHelper,
                _services.Commons,
                _services.AppSettings
            );
            
            BuildingSettings = new BuildingSettingsViewModel(
                _services.AppSettings,
                _services.MessageBoxService,
                _services.LocalizationHelper
            );
            BuildingSettings.AnnoCanvasToUse = Canvas;
            
            LayoutSettings = new LayoutSettingsViewModel();
        }
        
        private void RegisterEventHandlers()
        {
            Canvas.StatisticsUpdated += OnCanvasStatisticsUpdated;
            Canvas.OnLoadedFileChanged += OnCanvasLoadedFileChanged;
            Canvas.OnStatusMessageChanged += OnCanvasStatusMessageChanged;
            Canvas.UndoManager.PropertyChanged += OnUndoManagerPropertyChanged;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnCanvasStatisticsUpdated(object sender, UpdateStatisticsEventArgs e)
        {
            _ = Statistics.UpdateStatisticsAsync(
                e.Mode,
                Canvas.PlacedObjects.ToList(),
                Canvas.SelectedObjects,
                Canvas.BuildingPresets
            );
        }
        
        private void OnCanvasLoadedFileChanged(object sender, FileLoadedEventArgs e)
        {
            FilePath = e.FilePath;
            var fileName = string.IsNullOrWhiteSpace(e.FilePath) 
                ? "Untitled" 
                : System.IO.Path.GetFileName(e.FilePath);
                
            DocumentTitle = e.Layout?.LayoutVersion != null
                ? $"{fileName} ({e.Layout.LayoutVersion})"
                : fileName;
                
            IsDirty = false;
        }
        
        private void OnCanvasStatusMessageChanged(string message)
        {
            StatusMessageChanged?.Invoke(this, message);
        }
        
        private void OnUndoManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IUndoManager.IsDirty))
            {
                IsDirty = Canvas.UndoManager.IsDirty;
            }
        }
        
        #endregion
        
        #region Events
        
        public event EventHandler<string> StatusMessageChanged;
        public event EventHandler CloseRequested;
        
        #endregion
        
        #region Commands
        
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                await SaveAsAsync();
            }
            else
            {
                await _services.LayoutService.SaveLayoutAsync(Canvas, FilePath);
                IsDirty = false;
            }
        }
        
        [RelayCommand]
        private async Task SaveAsAsync()
        {
            var filePath = await _services.FileDialogService.ShowSaveFileDialogAsync(
                Constants.SavedLayoutExtension,
                Constants.SaveOpenDialogFilter
            );
            
            if (!string.IsNullOrEmpty(filePath))
            {
                await _services.LayoutService.SaveLayoutAsync(Canvas, filePath);
                FilePath = filePath;
                IsDirty = false;
            }
        }
        
        [RelayCommand]
        private async Task<bool> CheckUnsavedChangesAsync()
        {
            if (!IsDirty)
            {
                return true;
            }
            
            var result = await _services.MessageBoxService.ShowQuestionWithCancel(
                null,
                _services.LocalizationHelper.GetLocalization("SaveUnsavedChanges"),
                _services.LocalizationHelper.GetLocalization("UnsavedChanged")
            );
            
            if (result == null)
            {
                return false; // Cancel
            }
            
            if (result.Value)
            {
                await SaveAsync();
            }
            
            return true;
        }
        
        [RelayCommand]
        private async Task CloseAsync()
        {
            if (await CheckUnsavedChangesAsync())
            {
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        
        #endregion
        
        #region Cleanup
        
        public void Dispose()
        {
            // Unregister event handlers
            if (Canvas != null)
            {
                Canvas.StatisticsUpdated -= OnCanvasStatisticsUpdated;
                Canvas.OnLoadedFileChanged -= OnCanvasLoadedFileChanged;
                Canvas.OnStatusMessageChanged -= OnCanvasStatusMessageChanged;
            }
            
            // Dispose of scoped services
            _services.Dispose();
        }
        
        #endregion
    }
}
```

### 5.2 IDocumentServices Interface

**Location:** `AnnoDesigner\Services\IDocumentServices.cs`

```csharp
using System;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Services.Undo;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Provides scoped and shared services for a single document instance.
    /// </summary>
    public interface IDocumentServices : IDisposable
    {
        // Scoped services (unique per document)
        IUndoManager CreateUndoManager();
        
        // Shared services (singleton across application)
        IAppSettings AppSettings { get; }
        ICoordinateHelper CoordinateHelper { get; }
        IBrushCache BrushCache { get; }
        IPenCache PenCache { get; }
        IMessageBoxService MessageBoxService { get; }
        ILocalizationHelper LocalizationHelper { get; }
        IClipboardService ClipboardService { get; }
        ICommons Commons { get; }
        ILayoutService LayoutService { get; }
        IFileDialogService FileDialogService { get; }
    }
}
```

---

## 6. ViewModel Structure

### 6.1 DocumentManager

**Location:** `AnnoDesigner\Services\DocumentManager.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.ViewModels;

namespace AnnoDesigner.Services
{
    /// <summary>
    /// Manages the collection of open documents and active document state.
    /// </summary>
    public partial class DocumentManager : ObservableObject
    {
        private readonly IDocumentServicesFactory _servicesFactory;
        private readonly ISharedResourceManager _sharedResources;
        
        [ObservableProperty]
        private ObservableCollection<DocumentViewModel> _documents;
        
        [ObservableProperty]
        private DocumentViewModel _activeDocument;
        
        public DocumentManager(
            IDocumentServicesFactory servicesFactory,
            ISharedResourceManager sharedResources)
        {
            _servicesFactory = servicesFactory ?? throw new ArgumentNullException(nameof(servicesFactory));
            _sharedResources = sharedResources ?? throw new ArgumentNullException(nameof(sharedResources));
            
            Documents = new ObservableCollection<DocumentViewModel>();
        }
        
        /// <summary>
        /// Creates a new empty document.
        /// </summary>
        public DocumentViewModel CreateNewDocument()
        {
            var services = _servicesFactory.CreateDocumentServices();
            var document = new DocumentViewModel(
                services,
                _sharedResources.BuildingPresets,
                _sharedResources.Icons
            );
            
            document.CloseRequested += OnDocumentCloseRequested;
            document.StatusMessageChanged += OnDocumentStatusMessageChanged;
            
            Documents.Add(document);
            ActiveDocument = document;
            
            return document;
        }
        
        /// <summary>
        /// Opens a layout file as a new document.
        /// </summary>
        public async Task<DocumentViewModel> OpenDocumentAsync(string filePath)
        {
            var document = CreateNewDocument();
            
            try
            {
                var layout = await _sharedResources.LayoutLoader.LoadLayoutAsync(filePath);
                document.Canvas.PlacedObjects.Clear();
                document.Canvas.PlacedObjects.AddRange(
                    layout.Objects.Select(obj => new LayoutObject(
                        obj,
                        _sharedResources.CoordinateHelper,
                        _sharedResources.BrushCache,
                        _sharedResources.PenCache
                    ))
                );
                
                document.FilePath = filePath;
                document.LayoutSettings.LayoutVersion = layout.LayoutVersion;
                document.Canvas.Normalize(1);
                document.Canvas.ResetViewport();
                document.IsDirty = false;
                
                return document;
            }
            catch
            {
                await CloseDocumentAsync(document);
                throw;
            }
        }
        
        /// <summary>
        /// Closes a document after checking for unsaved changes.
        /// </summary>
        public async Task<bool> CloseDocumentAsync(DocumentViewModel document)
        {
            if (document == null)
            {
                return false;
            }
            
            if (!await document.CheckUnsavedChangesAsync())
            {
                return false;
            }
            
            document.CloseRequested -= OnDocumentCloseRequested;
            document.StatusMessageChanged -= OnDocumentStatusMessageChanged;
            
            Documents.Remove(document);
            document.Dispose();
            
            // Set new active document
            if (Documents.Count > 0)
            {
                ActiveDocument = Documents.Last();
            }
            else
            {
                ActiveDocument = null;
            }
            
            return true;
        }
        
        /// <summary>
        /// Closes all documents.
        /// </summary>
        public async Task<bool> CloseAllDocumentsAsync()
        {
            var documentsToClose = Documents.ToList();
            
            foreach (var document in documentsToClose)
            {
                if (!await CloseDocumentAsync(document))
                {
                    return false; // User cancelled
                }
            }
            
            return true;
        }
        
        private async void OnDocumentCloseRequested(object sender, EventArgs e)
        {
            if (sender is DocumentViewModel document)
            {
                await CloseDocumentAsync(document);
            }
        }
        
        private void OnDocumentStatusMessageChanged(object sender, string message)
        {
            // Propagate to main view model
            StatusMessageChanged?.Invoke(this, message);
        }
        
        public event EventHandler<string> StatusMessageChanged;
    }
}
```

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

| Resource | Sharing Strategy | Justification |
|----------|-----------------|---------------|
| BuildingPresets | Shared (Singleton) | Read-only data, no mutations |
| Icons Dictionary | Shared (Singleton) | Read-only data, significant memory |
| BrushCache | Shared (Singleton) | Thread-safe cache, memory optimization |
| PenCache | Shared (Singleton) | Thread-safe cache, memory optimization |
| UndoManager | Scoped (Per Document) | Document-specific history |
| PlacedObjects | Scoped (Per Document) | Document-specific content |
| SelectedObjects | Scoped (Per Document) | Document-specific selection |
| Viewport Transform | Scoped (Per Document) | Document-specific view state |
| LayoutFileService | Scoped (Per Document) | Encapsulates document I/O |

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

## 12. Testing Strategy

### 12.1 Unit Tests

**DocumentViewModel Tests:**
```csharp
[Fact]
public void DocumentViewModel_Initialize_CreatesCanvas()
{
    // Arrange
    var services = CreateMockServices();
    var presets = CreateMockPresets();
    var icons = CreateMockIcons();
    
    // Act
    var document = new DocumentViewModel(services, presets, icons);
    
    // Assert
    Assert.NotNull(document.Canvas);
    Assert.NotNull(document.Statistics);
    Assert.NotNull(document.BuildingSettings);
}

[Fact]
public async Task DocumentViewModel_SaveAsync_UpdatesIsDirty()
{
    // Arrange
    var document = CreateTestDocument();
    document.IsDirty = true;
    
    // Act
    await document.SaveAsAsync();
    
    // Assert
    Assert.False(document.IsDirty);
}
```

**DocumentManager Tests:**
```csharp
[Fact]
public void DocumentManager_CreateNewDocument_AddsToCollection()
{
    // Arrange
    var manager = CreateTestDocumentManager();
    
    // Act
    var document = manager.CreateNewDocument();
    
    // Assert
    Assert.Contains(document, manager.Documents);
    Assert.Equal(document, manager.ActiveDocument);
}

[Fact]
public async Task DocumentManager_CloseDocument_RemovesFromCollection()
{
    // Arrange
    var manager = CreateTestDocumentManager();
    var document = manager.CreateNewDocument();
    
    // Act
    var closed = await manager.CloseDocumentAsync(document);
    
    // Assert
    Assert.True(closed);
    Assert.DoesNotContain(document, manager.Documents);
}
```

### 12.2 Integration Tests

**Multi-Document Workflow:**
```csharp
[Fact]
public async Task MultiDocument_OpenMultipleFiles_AllDisplayed()
{
    // Arrange
    var viewModel = CreateMainViewModel();
    var files = new[] { "layout1.ad", "layout2.ad", "layout3.ad" };
    
    // Act
    foreach (var file in files)
    {
        await viewModel.DocumentManager.OpenDocumentAsync(file);
    }
    
    // Assert
    Assert.Equal(files.Length, viewModel.DocumentManager.Documents.Count);
}

[Fact]
public async Task MultiDocument_SwitchActiveDocument_UpdatesContextPanels()
{
    // Arrange
    var viewModel = CreateMainViewModel();
    var doc1 = await viewModel.DocumentManager.OpenDocumentAsync("layout1.ad");
    var doc2 = await viewModel.DocumentManager.OpenDocumentAsync("layout2.ad");
    
    // Act
    viewModel.DocumentManager.ActiveDocument = doc1;
    
    // Assert
    Assert.Equal(doc1.Statistics, viewModel.StatisticsViewModel);
    Assert.Equal(doc1.BuildingSettings, viewModel.BuildingSettingsViewModel);
}
```

### 12.3 Performance Tests

**Memory Usage:**
```csharp
[Fact]
public void MemoryUsage_TenDocuments_WithinAcceptableRange()
{
    // Arrange
    var manager = CreateTestDocumentManager();
    var initialMemory = GC.GetTotalMemory(true);
    
    // Act
    for (int i = 0; i < 10; i++)
    {
        var doc = manager.CreateNewDocument();
        // Add typical layout content
        AddTestLayout(doc.Canvas, objectCount: 100);
    }
    
    var finalMemory = GC.GetTotalMemory(true);
    var memoryIncrease = finalMemory - initialMemory;
    
    // Assert
    // Assuming ~10MB per document is acceptable
    Assert.True(memoryIncrease < 100 * 1024 * 1024); // 100MB total
}
```

---

## 13. Performance Considerations

### 13.1 Rendering Optimization

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

### 13.2 Memory Footprint

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

### 13.3 Rendering Performance Benchmarks

**Target Metrics:**
- Document switch time: < 100ms
- New document creation: < 200ms
- Rendering 1000 objects: < 50ms (unchanged from current)

---

## 14. Edge Cases and Constraints

### 14.1 Maximum Documents Limit

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

### 14.2 Duplicate File Handling

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

### 14.3 Cross-Document Building Copy

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

### 14.4 Application Shutdown

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

## 15. Appendix

### 15.0 Lessons Learned & Reimplementation Guidance

This section provides critical guidance for the future reimplementation effort based on analysis of the current partial implementation.

#### 15.0.1 Canvas Instance Management: Critical Best Practices

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

#### 15.0.2 Event Wiring Architecture

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

#### 15.0.3 Test Strategy: Integration Tests First

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

#### 15.0.4 Source Code Organization

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

#### 15.0.5 Build & Compilation Strategy

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

#### 15.0.6 Memory Management: Measured Reality vs Estimates

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

#### 15.0.7 Phased Rewrite Strategy

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

#### 15.0.8 Critical Questions to Answer BEFORE Coding

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

#### 15.0.9 Success Criteria Checklist

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

#### 15.0.10 Red Flags to Watch For

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

#### 15.0.11 Key Takeaways Summary

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

### 15.1 Glossary

- **MDT:** Multi Document Tabbing
- **SDI:** Single Document Interface
- **Active Document:** The currently focused/selected document tab
- **Document Context:** The scope of data and services specific to one document
- **Shared Resources:** Data or services shared across all documents (e.g., BuildingPresets)
- **Scoped Service:** Service instance unique to each document
- **Singleton Service:** Service instance shared across the entire application

### 15.2 References

- [AvalonDock Documentation](https://github.com/Dirkster99/AvalonDock)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [WPF MVVM Pattern](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview)
- [Dependency Injection in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

### 15.3 File Structure

```
AnnoDesigner/
??? ViewModels/
?   ??? MainViewModel.cs (modified)
?   ??? DocumentViewModel.cs (new)
?   ??? ... (existing)
??? Services/
?   ??? DocumentManager.cs (new)
?   ??? IDocumentServices.cs (new)
?   ??? DocumentServicesFactory.cs (new)
?   ??? ISharedResourceManager.cs (new)
?   ??? SharedResourceManager.cs (new)
??? Views/
?   ??? MainWindow.xaml (modified)
?   ??? ... (existing)
??? Converters/
?   ??? DocumentTitleConverter.cs (new)
??? Tests/
    ??? DocumentViewModelTests.cs (new)
    ??? DocumentManagerTests.cs (new)
    ??? MultiDocumentIntegrationTests.cs (new)
```

### 15.4 Configuration Options

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

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-01-XX | Development Team | Initial specification |

---

**END OF SPECIFICATION**
