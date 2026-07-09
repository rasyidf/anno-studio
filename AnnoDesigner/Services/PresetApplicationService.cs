using System;
using System.Collections.ObjectModel;
using System.IO.Abstractions;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Helper;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.ViewModels;
using NLog;

namespace AnnoDesigner.Services;

/// <summary>
/// Takes a preset AnnoObject, resolves its color/icon, and updates UI state + sets it on canvas.
/// </summary>
public interface IPresetApplicationService
{
    /// <summary>
    /// Takes a preset AnnoObject, resolves its color/icon, and updates UI state + sets it on canvas.
    /// </summary>
    void ApplyPreset(AnnoObject selectedItem);

    /// <summary>
    /// Reads current BuildingSettingsViewModel state and constructs+applies the object to canvas.
    /// </summary>
    void ApplyCurrentObject();

    /// <summary>
    /// Syncs UI (BuildingSettingsViewModel properties) from a LayoutObject.
    /// </summary>
    void UpdateUiFromObject(LayoutObject layoutObject);
}

/// <summary>
/// Extracted from MainViewModel to isolate preset application logic.
/// </summary>
public class PresetApplicationService : IPresetApplicationService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Func<BuildingSettingsViewModel> _getBuildingSettings;
    private readonly Func<BuildingPresets> _getBuildingPresets;
    private readonly Func<IAnnoCanvas> _getCanvas;
    private readonly ICoordinateHelper _coordinateHelper;
    private readonly IBrushCache _brushCache;
    private readonly IPenCache _penCache;
    private readonly IFileSystem _fileSystem;
    private readonly IMessageBoxService _messageBoxService;
    private readonly ILocalizationHelper _localizationHelper;
    private readonly Func<IconImage> _getSelectedIcon;
    private readonly Action<IconImage> _setSelectedIcon;
    private readonly Func<ObservableCollection<IconImage>> _getAvailableIcons;
    private readonly Func<IconImage> _getNoIconItem;

    public PresetApplicationService(
        Func<BuildingSettingsViewModel> getBuildingSettings,
        Func<BuildingPresets> getBuildingPresets,
        Func<IAnnoCanvas> getCanvas,
        ICoordinateHelper coordinateHelper,
        IBrushCache brushCache,
        IPenCache penCache,
        IFileSystem fileSystem,
        IMessageBoxService messageBoxService,
        ILocalizationHelper localizationHelper,
        Func<IconImage> getSelectedIcon,
        Action<IconImage> setSelectedIcon,
        Func<ObservableCollection<IconImage>> getAvailableIcons,
        Func<IconImage> getNoIconItem)
    {
        _getBuildingSettings = getBuildingSettings;
        _getBuildingPresets = getBuildingPresets;
        _getCanvas = getCanvas;
        _coordinateHelper = coordinateHelper;
        _brushCache = brushCache;
        _penCache = penCache;
        _fileSystem = fileSystem;
        _messageBoxService = messageBoxService;
        _localizationHelper = localizationHelper;
        _getSelectedIcon = getSelectedIcon;
        _setSelectedIcon = setSelectedIcon;
        _getAvailableIcons = getAvailableIcons;
        _getNoIconItem = getNoIconItem;
    }

    public void ApplyPreset(AnnoObject selectedItem)
    {
        try
        {
            if (selectedItem == null) return;
            var copySelectedItem = new AnnoObject(selectedItem);
            copySelectedItem.Color = ColorPresetsHelper.Instance.GetPredefinedColor(copySelectedItem) ??
                                     _getBuildingSettings().SelectedColor ?? Colors.Red;

            // Ensure icon is present on the copied preset for proper UI preview and placement
            if (string.IsNullOrWhiteSpace(copySelectedItem.Icon))
            {
                var id = copySelectedItem.Identifier;
                var template = copySelectedItem.Template;
                var buildingInfo = _getBuildingPresets()?.Buildings.FirstOrDefault(b =>
                    (!string.IsNullOrWhiteSpace(copySelectedItem.Identifier) && string.Equals(b.Identifier, copySelectedItem.Identifier, StringComparison.OrdinalIgnoreCase))

                );
                if (!string.IsNullOrWhiteSpace(buildingInfo?.IconFileName))
                {
                    copySelectedItem.Icon = _fileSystem.Path.GetFileNameWithoutExtension(buildingInfo.IconFileName);
                }
            }
            UpdateUiFromObject(new LayoutObject(copySelectedItem, _coordinateHelper, _brushCache, _penCache));

            ApplyCurrentObject();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error applying preset.");
            _messageBoxService.ShowError(
                _localizationHelper.GetLocalization("LayoutLoadingError"),
                _localizationHelper.GetLocalization("Error"));
        }
    }

    public void ApplyCurrentObject()
    {
        var buildingSettings = _getBuildingSettings();
        var selectedIcon = _getSelectedIcon();
        var noIconItem = _getNoIconItem();

        // parse user inputs and create new object
        var renameBuildingIdentifier = buildingSettings.BuildingName;
        var textBoxText = "UnknownObject";
        var obj = new AnnoObject
        {
            Size = new Size(buildingSettings.BuildingWidth, buildingSettings.BuildingHeight),
            Color = buildingSettings.SelectedColor ?? Colors.Red,
            Label =
                buildingSettings.IsEnableLabelChecked
                    ? buildingSettings.BuildingName
                    : string.Empty,
            Icon = selectedIcon == noIconItem ? null : selectedIcon.Name,
            Radius = buildingSettings.BuildingRadius,
            InfluenceRange = buildingSettings.BuildingInfluenceRange,
            PavedStreet = buildingSettings.IsPavedStreet,
            Borderless = buildingSettings.IsBorderlessChecked,
            Road = buildingSettings.IsRoadChecked,
            Identifier = buildingSettings.BuildingIdentifier,
            Template = buildingSettings.BuildingTemplate,
            BlockedAreaLength = buildingSettings.BuildingBlockedAreaLength,
            BlockedAreaWidth = buildingSettings.BuildingBlockedAreaWidth,
            Direction = buildingSettings.BuildingDirection
        };

        var objIconFileName = "";
        //Parse the Icon path into something we can check.
        if (!string.IsNullOrWhiteSpace(obj.Icon))
        {
            if (obj.Icon.StartsWith("A5_"))
            {
                objIconFileName = obj.Icon[3..] + ".png"; //when Anno 2070, it use not A5_ in the original naming.
            }
            else
            {
                objIconFileName = obj.Icon + ".png";
            }
        }

        // do some sanity checks
        if (obj.Size is { Width: > 0, Height: > 0 } && obj.Radius >= 0)
        {
            //gets icons and origin building info
            var buildingInfo = _getBuildingPresets()?.Buildings.FirstOrDefault(_ =>
                _.IconFileName?.Equals(objIconFileName, StringComparison.OrdinalIgnoreCase) ?? false);

            if (buildingInfo != null)
            {
                // If the UI hasn't selected an icon (e.g. preset didn't set obj.Icon), try to set SelectedIcon from the preset's IconFileName
                try
                {
                    if (string.IsNullOrWhiteSpace(selectedIcon?.Name) || selectedIcon == noIconItem)
                    {
                        if (!string.IsNullOrWhiteSpace(buildingInfo.IconFileName))
                        {
                            var iconNameNoExt = _fileSystem.Path.GetFileNameWithoutExtension(buildingInfo.IconFileName);
                            var foundIconImage = _getAvailableIcons().SingleOrDefault(x =>
                                x.Name.Equals(iconNameNoExt, StringComparison.OrdinalIgnoreCase));
                            _setSelectedIcon(foundIconImage ?? noIconItem);
                        }
                    }
                }
                catch { }

                //Put in the Blocked Area if there is one
                if (buildingInfo.BlockedAreaLength > 0)
                {
                    obj.BlockedAreaLength = buildingInfo.BlockedAreaLength;
                    obj.BlockedAreaWidth = buildingInfo.BlockedAreaWidth;
                    obj.Direction = buildingInfo.Direction;
                }

                //if user entered a new Label Name (as in renaming existing building or naming own building) then name and identifier will be renamed
                if (buildingSettings.BuildingRealName != buildingSettings.BuildingName)
                {
                    obj.Identifier = renameBuildingIdentifier;
                    obj.Template = renameBuildingIdentifier;
                }
            }
            else
            {
                //if no Identifier is found or if user entered a new Label Name (as in renaming existing building or naming own building) then name and identifier will be renamed
                if (string.IsNullOrWhiteSpace(buildingSettings.BuildingIdentifier) ||
                    buildingSettings.BuildingRealName != buildingSettings.BuildingName)
                {
                    if (!string.IsNullOrWhiteSpace(renameBuildingIdentifier))
                    {
                        obj.Identifier = renameBuildingIdentifier;
                        obj.Template = renameBuildingIdentifier;
                    }
                    else
                    {
                        obj.Identifier = textBoxText;
                    }
                }
            }

            // Ensure the object has a valid icon string even if UI selection didn't resolve
            if (string.IsNullOrWhiteSpace(obj.Icon))
            {
                string iconCandidate = null;
                if (selectedIcon != null && selectedIcon != noIconItem)
                {
                    iconCandidate = selectedIcon.Name;
                }
                else if (buildingInfo != null && !string.IsNullOrWhiteSpace(buildingInfo.IconFileName))
                {
                    iconCandidate = _fileSystem.Path.GetFileNameWithoutExtension(buildingInfo.IconFileName);
                }
                if (!string.IsNullOrWhiteSpace(iconCandidate))
                {
                    obj.Icon = iconCandidate;
                }
            }

            var canvas = _getCanvas();
            if (canvas == null)
            {
                Logger.Warn("AnnoCanvas is null; cannot apply current object.");
                return;
            }

            canvas.SetCurrentObject(new LayoutObject(obj, _coordinateHelper, _brushCache, _penCache));
        }
        else
        {
            throw new Exception("Invalid building configuration.");
        }
    }

    public void UpdateUiFromObject(LayoutObject layoutObject)
    {
        var obj = layoutObject?.WrappedAnnoObject;
        if (obj == null)
        {
            return;
        }
        if (layoutObject == null)
        {
            return;
        }

        var buildingSettings = _getBuildingSettings();
        var noIconItem = _getNoIconItem();

        // size
        buildingSettings.BuildingWidth = (int)layoutObject.Size.Width;
        buildingSettings.BuildingHeight = (int)layoutObject.Size.Height;
        // color
        buildingSettings.SelectedColor = layoutObject.Color;
        // label
        buildingSettings.BuildingName = obj.Label;
        buildingSettings.BuildingRealName = obj.Label;
        // Identifier
        buildingSettings.BuildingIdentifier = layoutObject.Identifier;
        // Template
        buildingSettings.BuildingTemplate = obj.Template;
        // icon
        try
        {
            if (string.IsNullOrWhiteSpace(obj.Icon))
            {
                _setSelectedIcon(noIconItem);
            }
            else
            {
                var foundIconImage = _getAvailableIcons().FirstOrDefault(x =>
                    x.Name.Equals(_fileSystem.Path.GetFileNameWithoutExtension(obj.Icon),
                        StringComparison.OrdinalIgnoreCase));
                _setSelectedIcon(foundIconImage ?? noIconItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error finding {nameof(IconImage)} for value \"{obj.Icon}\".{Environment.NewLine}{ex}");

            _setSelectedIcon(noIconItem);
        }

        // radius
        buildingSettings.BuildingRadius = obj.Radius;
        //InfluenceRange
        if (!buildingSettings.IsPavedStreet)
        {
            buildingSettings.BuildingInfluenceRange = obj.InfluenceRange;
        }
        else
        {
            _ = buildingSettings.GetDistanceRange(true,
                _getBuildingPresets()?.Buildings.FirstOrDefault(_ =>
                    _.Identifier == buildingSettings.BuildingIdentifier));
        }

        //Set Influence Type
        if (obj.Radius > 0 && obj.InfluenceRange > 0)
        {
            //Building uses both a radius and an influence
            //Has to be set manually 
            buildingSettings.SelectedBuildingInfluence =
                buildingSettings.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.Both);
        }
        else if (obj.Radius > 0)
        {
            buildingSettings.SelectedBuildingInfluence =
                buildingSettings.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.Radius);
        }
        else if (obj.InfluenceRange > 0)
        {
            buildingSettings.SelectedBuildingInfluence =
                buildingSettings.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.Distance);

            if (obj.PavedStreet)
            {
                buildingSettings.IsPavedStreet = obj.PavedStreet;
            }
        }
        else
        {
            buildingSettings.SelectedBuildingInfluence =
                buildingSettings.BuildingInfluences.Single(x => x.Type == BuildingInfluenceType.None);
        }

        // flags            
        //BuildingSettingsViewModel.IsEnableLabelChecked = !string.IsNullOrEmpty(obj.Label);
        buildingSettings.IsBorderlessChecked = obj.Borderless;
        buildingSettings.IsRoadChecked = obj.Road;
    }
}
