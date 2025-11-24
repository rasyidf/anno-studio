# AnnoCanvas2 Refactoring: Technical Specification

## Overview
This document outlines the technical specification for refactoring the `AnnoCanvas2` class in `AnnoDesigner\Controls\Canvas\AnnoCanvas.xaml.cs` to follow the MVVM (Model-View-ViewModel) pattern. The goal is to separate concerns by moving business logic from the codebehind to a dedicated ViewModel, resulting in a pure codebehind focused on UI interactions and rendering.

## Current State Analysis
- **Class Size**: `AnnoCanvas2` is approximately 2,000+ lines, mixing UI rendering, event handling, business logic, and data management.
- **Responsibilities**:
  - UI rendering (OnRender, RenderObjectList, etc.)
  - Event handling (OnMouseDown, OnMouseMove, etc.)
  - Business logic (TryPlaceCurrentObjects, Normalize, etc.)
  - Data properties (GridSize, PlacedObjects, etc.)
  - Commands (ExecuteRotate, ExecuteCopy, etc.)
- **Dependencies**: Injected services like `ILayoutModelService`, `ICoordinateHelper`, etc.
- **Existing v2 ViewModel**: A partial `AnnoCanvasViewModel` exists in `AnnoDesigner\Controls\v2\ViewModels\AnnoCanvasViewModel.cs`, which can be leveraged or migrated.

## Target Architecture
- **Codebehind (`AnnoCanvas2`)**: Inherits from `UserControl`. Handles UI-specific logic, event overrides, rendering, and `IScrollInfo` implementation. Minimal logic; delegates to ViewModel.
- **ViewModel (`AnnoCanvasViewModel`)**: Inherits from `ObservableObject` (CommunityToolkit.MVVM). Contains properties, collections, commands, and business methods. Implements `INotifyPropertyChanged` via base class.
- **MVVM Library**: Use CommunityToolkit.MVVM for:
  - `ObservableObject` as base for ViewModel.
  - `RelayCommand` for commands.
  - `ObservableProperty` attribute for auto-implementing properties.
- **Bindings**: WPF bindings for properties where possible; direct access in codebehind for rendering.
- **Events**: ViewModel uses callback actions to raise events in codebehind.
- **Services**: Injected into ViewModel constructor.

## Key Components
### ViewModel Properties
- Rendering flags: `GridSize`, `RenderGrid`, `RenderInfluences`, etc. (use `[ObservableProperty]`)
- Collections: `PlacedObjects`, `SelectedObjects`, `CurrentObjects`
- State: `StatusMessage`, `LoadedFile`, `CurrentMode`
- Services: Injected dependencies

### ViewModel Commands
- `RotateCommand`, `CopyCommand`, `PasteCommand`, etc., using `RelayCommand`
- Execute methods moved from codebehind

### Codebehind Responsibilities
- UI event handlers: Delegate to ViewModel methods or properties
- Rendering: Access ViewModel data for `OnRender`
- `IScrollInfo`: Keep implementation, access ViewModel viewport data
- Event raising: Set callbacks in ViewModel

### Interfaces and Inheritance
- ViewModel: `public class AnnoCanvasViewModel : ObservableObject`
- Codebehind: Retains `UserControl`, `IAnnoCanvas`, `IHotkeySource`, `IScrollInfo`
- No changes to public interfaces exposed by codebehind

## Implementation Steps
1. **Create/Update ViewModel**:
   - Inherit from `ObservableObject`
   - Move properties with `[ObservableProperty]`
   - Move business methods
   - Convert commands to `RelayCommand`
   - Add callback actions for events

2. **Refactor Codebehind**:
   - Add `AnnoCanvasViewModel ViewModel` property
   - Initialize ViewModel in constructor
   - Delegate event handlers to ViewModel
   - Update rendering to use ViewModel data
   - Set event callbacks

3. **Update XAML**:
   - Bind to ViewModel properties (e.g., `{Binding ViewModel.GridSize}`)

4. **Testing**:
   - Unit tests for ViewModel
   - UI tests for codebehind
   - Integration tests for bindings

## Benefits
- **Testability**: ViewModel can be unit-tested independently
- **Maintainability**: Clear separation of concerns
- **Reusability**: ViewModel logic can be reused
- **Performance**: No impact on rendering; logic isolated

## Risks and Mitigations
- **Rendering Dependency**: Ensure ViewModel data is accessible; mitigation: ViewModel exposes read-only properties
- **Event Handling**: Complex mouse logic; mitigation: Delegate to services like `IInputInteractionService`
- **Breaking Changes**: Public API unchanged; mitigation: Incremental refactoring with builds

## Dependencies
- CommunityToolkit.MVVM NuGet package
- Existing services and interfaces remain unchanged

## Success Criteria
- Codebehind < 500 lines, focused on UI
- ViewModel > 1,500 lines, containing all logic
- All existing functionality preserved
- Build passes without errors
- Unit test coverage > 80% for ViewModel</content>
<parameter name="filePath">docs/AnnoCanvasRefactoring_TechnicalSpec.md