# AnnoCanvas2 Refactoring: Task Tracker

## Overview
This tracker outlines the tasks for refactoring `AnnoCanvas2` to MVVM using CommunityToolkit.MVVM. Tasks are grouped by phase, with status, assignee, and notes.

## Phase 1: Preparation
- [x] **Install CommunityToolkit.MVVM**: Add NuGet package to AnnoDesigner project.
  - Status: Complete
  - Assignee: Developer
  - Notes: Ensure compatibility with .NET 10.

- [x] **Analyze Existing v2 ViewModel**: Review `AnnoCanvasViewModel.cs` for reuse.
  - Status: Complete
  - Assignee: Developer
  - Notes: Reviewed AnnoCanvasViewModel.cs in AnnoDesigner\Controls\v2\ViewModels\. It is comprehensive, inheriting from ObservableObject, with properties using [ObservableProperty], injected services, collections, methods, and commands using RelayCommand. Suitable for reuse in refactoring.

- [x] **Create New AnnoCanvasViewModel**: Inherit from `ObservableObject`, place in `AnnoDesigner\ViewModels\`.
  - Status: Complete
  - Assignee: Developer
  - Notes: AnnoCanvasViewModel already exists in AnnoDesigner\Controls\v2\ViewModels\, inheriting from ObservableObject with all required properties, methods, and commands implemented.

## Phase 2: Move Properties and Data
- [ ] **Move Rendering Properties**: `GridSize`, `RenderGrid`, `RenderInfluences`, etc., with `[ObservableProperty]`.
  - Status: Pending
  - Assignee: Developer
  - Notes: Update setters to call `OnPropertyChanged` via attribute.

- [ ] **Move Collections**: `PlacedObjects`, `SelectedObjects`, `CurrentObjects`.
  - Status: Pending
  - Assignee: Developer
  - Notes: Ensure thread-safety if needed.

- [ ] **Move State Properties**: `StatusMessage`, `LoadedFile`, `CurrentMode`.
  - Status: Pending
  - Assignee: Developer
  - Notes: Handle events via callbacks.

- [ ] **Move Services**: Inject `ILayoutModelService`, etc., into ViewModel constructor.
  - Status: Pending
  - Assignee: Developer
  - Notes: Update codebehind to pass services.

## Phase 3: Move Business Logic
- [ ] **Move Methods**: `TryPlaceCurrentObjects`, `Normalize`, `GetObjectAt`, etc.
  - Status: Pending
  - Assignee: Developer
  - Notes: Update calls to use ViewModel.

- [ ] **Move Commands**: Convert `ExecuteRotate`, etc., to `RelayCommand` in ViewModel.
  - Status: Pending
  - Assignee: Developer
  - Notes: Update hotkey setup.

- [ ] **Handle Events**: Add callback actions in ViewModel for `StatisticsUpdated`, etc.
  - Status: Pending
  - Assignee: Developer
  - Notes: Set in codebehind constructor.

## Phase 4: Refactor Codebehind
- [ ] **Add ViewModel Property**: `public AnnoCanvasViewModel ViewModel { get; private set; }`.
  - Status: Pending
  - Assignee: Developer
  - Notes: Initialize in constructor.

- [ ] **Delegate Event Handlers**: Update `OnMouseDown`, etc., to call ViewModel methods.
  - Status: Pending
  - Assignee: Developer
  - Notes: Keep UI-specific logic.

- [ ] **Update Rendering**: Modify `OnRender` to access ViewModel data.
  - Status: Pending
  - Assignee: Developer
  - Notes: Ensure performance.

- [ ] **Update IScrollInfo**: Access ViewModel viewport in implementations.
  - Status: Pending
  - Assignee: Developer
  - Notes: Minimal changes.

## Phase 5: Update XAML and Bindings
- [ ] **Add DataContext**: Set `DataContext` to ViewModel in XAML.
  - Status: Pending
  - Assignee: Developer
  - Notes: Or use RelativeSource bindings.

- [ ] **Bind Properties**: Bind UI elements to ViewModel properties.
  - Status: Pending
  - Assignee: Developer
  - Notes: Test bindings.

## Phase 6: Testing and Validation
- [ ] **Unit Tests for ViewModel**: Cover properties, methods, commands.
  - Status: Pending
  - Assignee: Developer
  - Notes: Use existing test structure.

- [ ] **UI Tests**: Ensure codebehind interactions work.
  - Status: Pending
  - Assignee: Developer
  - Notes: Manual testing.

- [ ] **Build Validation**: Run `run_build` after each major change.
  - Status: Pending
  - Assignee: Developer
  - Notes: Fix compilation errors.

- [ ] **Integration Tests**: Full workflow testing.
  - Status: Pending
  - Assignee: Developer
  - Notes: End-to-end.

## Phase 7: Cleanup and Documentation
- [ ] **Remove Old Code**: Clean up moved code from codebehind.
  - Status: Pending
  - Assignee: Developer
  - Notes: Ensure no dead code.

- [ ] **Update Comments**: Refresh XML docs.
  - Status: Pending
  - Assignee: Developer
  - Notes: Reflect new structure.

- [ ] **Final Review**: Code review for MVVM compliance.
  - Status: Pending
  - Assignee: Developer
  - Notes: Check against spec.

## Metrics
- **Total Tasks**: 20+
- **Estimated Time**: 20-30 hours
- **Progress**: 5% (start with Phase 1)

## Notes
- Use Git branches for incremental changes.
- Update this tracker as tasks complete.
- Reference TechnicalSpec.md for details.