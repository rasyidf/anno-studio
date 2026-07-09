# Anno Studio — Backlog: Features & Fixes

## Features (15)

| # | Title | Area | Description | Effort | Priority |
|---|-------|------|-------------|--------|----------|
| F1 | EditorCanvas Phase 4 — Final Swap | Canvas | Replace old AnnoCanvas with EditorCanvas as primary. Implement IAnnoCanvas on adapter, swap XAML, feature flag, delete 2500+ LOC legacy code. See `docs/specs/EditorCanvas_Phase4_FinalSwap.md`. | L | P0 |
| F2 | Road-Network Influence Polygon | Canvas | Render true influence polygons via road flood-fill instead of circles. `ponytail:` stub exists in InfluenceRenderLayer. Integrate RoadSearchHelper connectivity graph. | L | P1 |
| F3 | Async Layout Loading with Progress | Data | `LayoutLoader.LoadLayout()` blocks UI for large files. Add `LoadLayoutAsync` with `CancellationToken` and `IProgress<T>`. | M | P1 |
| F4 | Layout File v5 with Compression | Data | Formalize v5 format with optional GZip compression and magic-byte header for auto-detection. Large city layouts (10MB+) benefit significantly. | M | P2 |
| F5 | Migrate Serialization to System.Text.Json | Data | Replace Newtonsoft.Json with STJ source generators. Faster, lower allocations, one less dependency. `JsonAppSettings` already uses STJ. | L | P2 |
| F6 | Drag-and-Drop Layout File Opening | UI | Add `AllowDrop` + drop handler on MainWindow to open `.ad` files via drag-and-drop. | S | P2 |
| F7 | In-App Toast/Notification System | UI | Centralized notification service using Wpf.Ui Snackbar/InfoBar for transient user feedback (save, export, errors). | M | P2 |
| F8 | Keyboard Shortcut Hints in Tooltips | UI | Surface keybindings in button tooltips (e.g., "Place Building (Enter)"). Standard in professional design tools. | M | P2 |
| F9 | Statistics Panel Copy/Export | UI | "Copy to Clipboard" button that formats statistics as text/markdown for Discord/Reddit sharing. | S | P3 |
| F10 | Responsive Panel Layout for Small Screens | UI | Collapsible side panels or adaptive breakpoints. Current fixed `DockWidth="320"` squeezes canvas on 1366×768. | L | P3 |
| F11 | SelectionTool Drag-Rectangle (EditorCanvas) | Canvas | Complete the TODO stubs in SelectionTool for rubber-band selection and Ctrl/Shift multi-select. | M | P1 |
| F12 | Cached Extent Bounding Box for Scrolling | Canvas | Replace O(n) extent calculation in IScrollInfo with incrementally-maintained min/max rect. | S | P2 |
| F13 | Automated Preset Validation in CI | Infra | CI step that deserializes all preset JSON files and checks for schema violations, duplicate GUIDs, missing icon refs. | S | P1 |
| F14 | Layout File Auto-Backup/Recovery | Data | Periodic auto-backup of active layout to `.bak` sidecar. Crash-safe save (write temp → atomic move). | S | P1 |
| F15 | Diagonal/Rotated Object Placement | Canvas | Full user-facing rotation support: rotation gizmo, 45° snapping, collision detection for rotated rects. (Future Anno 117 support.) | L | P3 |

---

## Fixes (15)

| # | Title | Area | Description | Effort | Priority |
|---|-------|------|-------------|--------|----------|
| B1 | Non-Atomic File Writes Risk Data Loss | Data | `SerializationHelper.SaveToFile()` truncates before writing. Crash mid-write = lost file. Fix: write to temp, then `File.Move`. | S | P0 |
| B2 | UpdateHelper Points to Wrong GitHub Repo | Infra | `GITHUB_USERNAME="AnnoDesigner"` and `GITHUB_PROJECTNAME="anno-designer"` — should be `rasyidf/anno-studio`. Updates will never find releases. | S | P0 |
| B3 | GitHub Actions Use Deprecated v3/v1.1 | CI | `checkout@v3`, `upload-artifact@v3`, `setup-msbuild@v1.1` all use EOL'd Node.js 16. Upgrade to v4+. Add `setup-dotnet` for .NET 10. | S | P0 |
| B4 | Compact.xaml Hardcoded Colors Break Dark Mode | UI | Light-mode colors (`#F7F7F7`, `#E0E0E0`) merged globally. Cards render unreadable in dark mode. Replace with `{DynamicResource}` theme brushes. | S | P0 |
| B5 | FormattedText Allocation Per Frame Per Object | Perf | `AnnoObjectsLayer` allocates `new FormattedText` every render frame for each labeled object. Use existing `LayoutObject._formattedText` cache. | S | P0 |
| B6 | IconRenderLayer Not Converting to Screen Coords | Canvas | Icons render at wrong position/size when zoomed. Grid rect not multiplied by gridSize. | S | P0 |
| B7 | ObjectManagerQuadTree Brute-Force Region Query | Perf | `GetObjectsInRegion()` is O(n) scan despite the name. Degrades with 1000+ objects. Integrate proper spatial subdivision. | M | P1 |
| B8 | Missing Accessibility Automation Properties | UI | Zero `AutomationProperties.Name` on interactive controls. Screen readers can't identify NumberBox, color picker, search box. | M | P1 |
| B9 | PropertiesPanel Grid Row/Column Mismatch | UI | Controls reference `Grid.Column="1"` but parent only has 1 column. Layout intent is silently ignored. | S | P1 |
| B10 | DrawingGroup Re-creation During Placement | Perf | During continuous placement, viewport culling always triggers full object list reconstruction 60×/sec. Use dirty flag + list reuse. | M | P1 |
| B11 | Zero Test Coverage for Import/Gamedata Modules | Test | `AnnoDesigner.Import` and `AnnoDesigner.Gamedata` have no tests. Complex binary parsers with zero regression detection. | M | P1 |
| B12 | AnnoEditorAdapter Per-Pixel Sync During Drag | Perf | Bounds setter fires Position/Size sync on every mouse pixel during drag. Cascade invalidates cached rects. Batch with begin/end. | M | P2 |
| B13 | Stale Version Constants in Directory.Build.targets | Tech | Dead variables (`NLogVersion=4.7.13`, etc.) shadow actual versions in Directory.Packages.props. Misleading for contributors. | S | P2 |
| B14 | StatusBar Overflow on Long Messages | UI | No `MaxWidth` or `TextTrimming` on StatusBarItem. Long file paths push other items off-screen. | S | P2 |
| B15 | StatisticsViewModel ShowSelectedBuildingList Not Observable | UI | Computed property without proper change notification. Collection changes bypass OnPropertyChanged calls. | S | P2 |

---

## Recommended Execution Order

### Sprint 1 — Critical Fixes (P0)
B1, B2, B3, B4, B5, B6 — all Size S, can be done in 1 day

### Sprint 2 — Canvas Completion (P0-P1)
F1 (Phase 4 swap), F11 (SelectionTool drag), B7 (QuadTree fix), B10 (placement perf)

### Sprint 3 — Data & Reliability (P1)
F3 (async loading), F14 (auto-backup), F13 (CI preset validation), B11 (import tests)

### Sprint 4 — UX Polish (P1-P2)
B8 (accessibility), B9 (grid fix), F6 (drag-drop), F7 (notifications)

### Sprint 5 — Future-Proofing (P2-P3)
F4 (v5 format), F5 (STJ migration), F12 (cached extents), F15 (rotation)
