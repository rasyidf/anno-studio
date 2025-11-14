# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that an .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade AnnoDesigner.Core\AnnoDesigner.Core.csproj
4. Upgrade AnnoDesigner\AnnoDesigner.csproj
5. Upgrade PresetParser\PresetParser.csproj
6. Upgrade Tests\AnnoDesigner.Tests\AnnoDesigner.Tests.csproj
7. Upgrade Tests\PresetParser.Tests\PresetParser.Tests.csproj
8. Upgrade Tests\AnnoDesigner.Core.Tests\AnnoDesigner.Core.Tests.csproj

## Settings

This section contains settings and data used by execution steps.

### Excluded projects

Table below contains projects that do belong to the dependency graph for selected projects and should not be included in the upgrade.

| Project name                                   | Description                 |
|:-----------------------------------------------|:---------------------------:|


### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                | Current Version | New Version | Description                                   |
|:--------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.Xaml.Behaviors.Wpf                | 1.1.135         | 1.1.39      | Change to a compatible package version per analysis (incompatible with net10.0).
| Newtonsoft.Json                              | 13.0.3          | 13.0.4      | Recommended update (bug/security/compatibility update).
| Roslyn.System.IO.Abstractions.Analyzers     | 12.2.19         | 12.2.19     | Deprecated analyzer package; review and replace if necessary.


### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### AnnoDesigner.Core/AnnoDesigner.Core.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`.

NuGet packages changes:
  - `Newtonsoft.Json` should be updated from `13.0.3` to `13.0.4`.
  - `Roslyn.System.IO.Abstractions.Analyzers` is deprecated (current `12.2.19`); consider replacing or removing analyzer package.

Feature upgrades:
  - No other feature-specific changes detected by analysis.

Other changes:
  - Review any conditional compilation symbols or windows-specific APIs that might require adjustments for net10.0-windows.

#### AnnoDesigner/AnnoDesigner.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`.

NuGet packages changes:
  - `Microsoft.Xaml.Behaviors.Wpf` recommended change from `1.1.135` to `1.1.39` for compatibility.
  - `Roslyn.System.IO.Abstractions.Analyzers` is deprecated (current `12.2.19`); consider replacing or removing analyzer package.

Feature upgrades:
  - Inspect any WPF-specific APIs and third-party behaviors for compatibility with net10.0-windows.

Other changes:
  - Validate application startup and packaging for newer runtime.

#### PresetParser/PresetParser.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`.

NuGet packages changes:
  - `Roslyn.System.IO.Abstractions.Analyzers` is deprecated (current `12.2.19`); consider replacing or removing analyzer package.

Feature upgrades:
  - None detected.

Other changes:
  - Verify any OS-specific APIs.

#### Tests/AnnoDesigner.Tests/AnnoDesigner.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`.

NuGet packages changes:
  - `Roslyn.System.IO.Abstractions.Analyzers` is deprecated (current `12.2.19`); consider replacing or removing analyzer package.

Other changes:
  - Update test runners or adapters if they are pinned to older frameworks.

#### Tests/PresetParser.Tests/PresetParser.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`.

NuGet packages changes:
  - `Roslyn.System.IO.Abstractions.Analyzers` is deprecated (current `12.2.19`); consider replacing or removing analyzer package.

Other changes:
  - Update test runners or adapters if they are pinned to older frameworks.

#### Tests/AnnoDesigner.Core.Tests/AnnoDesigner.Core.Tests.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0-windows` to `net10.0-windows`.

NuGet packages changes:
  - `Roslyn.System.IO.Abstractions.Analyzers` is deprecated (current `12.2.19`); consider replacing or removing analyzer package.

Other changes:
  - Update test runners or adapters if they are pinned to older frameworks.

