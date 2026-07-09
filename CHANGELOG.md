# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added
- **EditorCanvas Phase 1-3:** New canvas engine with layered rendering, 12+ drawing tools, diagonal road support
  - `CanvasObject` extended with `FillColor`, `IconName`, `Label`, `IsRoad`, `IsBorderless` properties
  - `AnnoEditorAdapter` full bidirectional sync of all AnnoObject properties
  - `ObjectsLayer` renders per-object colors, borderless mode, and centered label text
  - Serialization bridge: `LoadLayout(IEnumerable<AnnoObject>)`, `LoadLayoutFile()`, `GetAnnoObjectsForSave()`
  - `InfluenceRenderLayer` rewritten with proper world-space rendering (blue circles for range, green for radius)
  - `BlockedAreaRenderLayer` with direction-aware placement and hatched pattern fill
  - `IScrollInfo` implementation delegating to `TransformService`
  - Copy/Cut/Paste with internal clipboard and `CompositeOperation` for atomic undo
  - `DefaultPlacementValidator` checks `ObjectManager.GetObjectsInRegion` for collision detection
  - `SelectAllWithIdentifier()` via Ctrl+Shift+Click
  - `RoadPlacementTool` (353 LOC): constrained 45° angles, snap-to-endpoint, configurable width, preview rendering
  - `RoadSearchHelper.RasterizeSegment()` (Bresenham) + `PrepareConnectivityGraph()` for diagonal road BFS
- **PresetParser modularization:**
  - `IGameParser` interface and `GameParserBase` (193 LOC) with shared logic
  - Per-game parsers: `Anno1404Parser`, `Anno2070Parser`, `Anno2205Parser`, `Anno1800Parser`
  - `Anno117Parser` (355 LOC): full self-contained implementation (XML assets, localizations, build blocker, influence, classification)
  - `SplitPresets` utility: splits monolithic presets.json into per-game files with manifest
  - `PresetsManifest` model and `BuildingPresetsLoader.LoadFromManifest()` with selective game loading
- **MainViewModel services:**
  - `IExportService` / `ExportService` (284 LOC): extracted image export and clipboard copy
  - `IPresetApplicationService` / `PresetApplicationService` (362 LOC): extracted preset application logic
  - `RoadMergeHelper` (79 LOC): extracted road merge logic
  - `FileAssociationHelper` (68 LOC): extracted Windows registry file association
  - `ExportSettings` model for clean export parameter passing
- **Documentation:**
  - `docs/specs/EditorCanvas_Phase4_FinalSwap.md` — migration plan for final canvas swap

### Changed
- **BuildingInfoExtensions** consolidated: `GetOrderParameter()` and `ToAnnoObject(string)` moved from `AnnoDesigner.Extensions` to `AnnoDesigner.Core.Extensions` (single source of truth)
- **MainViewModel** reduced from 2152 → 1614 LOC (25% reduction) via service extraction
- **PresetParser Program.cs**: `ParseAssetsFile*` methods changed from `private` to `internal` for parser access
- **SharedResourceManager**: now tries `presets_manifest.json` first, falls back to monolithic `presets.json`
- **AnnoDesigner.csproj**: copies per-game preset files and manifest to output directory

### Removed
- `ColorPresetsDesigner/Models/RelayCommand.cs` — replaced by `AnnoDesigner.Core.Models.RelayCommand`
- `AnnoDesigner/Extensions/BuildingInfoExtensions.cs` — merged into Core
- `AnnoDesigner/Services/DocumentManager.cs` — unused dead code (MainViewModel handles lifecycle)

### Fixed
- `PresetsTreeViewModelTests` updated to import `AnnoDesigner.Core.Extensions` for `GetOrderParameter`
