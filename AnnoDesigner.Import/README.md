# AnnoDesigner.Import

This project contains the import functionality for loading Anno savegames and converting into building layouts in AnnoDesigner.

## Anno 117 Documentation

Comprehensive documentation for Anno 117 (Anno 8) data extraction and savegame import:

**[Anno117_Savegames.md](docs/Anno117_Savegames.md)**  
Savegame file format and import process
- `.a8s` savegame file structure
- Binary data parsing
- Session and island information extraction
- Building object data
- Farm field module system
- Road tile processing

**[Anno117_Assets.md](docs/Anno117_Assets.md)**  
Asset extraction and preset generation
- XML asset structure
- Building classification system
- Population tier resolution
- Module system for farm buildings
- BuildBlocker extraction from `.ifo` files
- Localization system
- Data extraction pipeline

## Usage

```csharp
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Import;

// Create savegame reader
var reader = new Anno117.SavegameReader();
BuildingPresets presets = ...; // for example JsonConvert.DeserializeObject<BuildingPresets>(File.ReadAllText(@"path/to/presets.json"));

// Read savegame file
var layout = reader.Load("path/to/savegame.a8s", presets);

// Access extracted data
foreach (var obj in layout.Objects)
{
    Console.WriteLine($"Building: {obj.Identifier} at ({obj.Position.X}, {obj.Position.Y})");
}
```

## Notes

This project uses:
- [RDAExplorer](https://github.com/jakobharder/RDAExplorer) (custom .NET 8 build without external `zlib.dll`)
- [FileDBReader](https://github.com/anno-mods/FileDBReader)
