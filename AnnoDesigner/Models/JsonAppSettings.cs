using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using AnnoDesigner.Core.Models;

namespace AnnoDesigner.Models
{
    /// <summary>
    /// JSON-backed implementation of <see cref="IAppSettings"/>.
    /// Stores settings in %LocalAppData%/AnnoDesigner/settings.json.
    /// </summary>
    public sealed class JsonAppSettings : IAppSettings
    {
        private static readonly string SettingsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AnnoDesigner");

        private static readonly string SettingsFilePath =
            Path.Combine(SettingsDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly object _lock = new();
        private SettingsData _data;

        public event EventHandler SettingsChanged;

        public JsonAppSettings()
        {
            _data = CreateDefaults();
            Reload();
        }

        #region IAppSettings methods

        public void Save()
        {
            lock (_lock)
            {
                Directory.CreateDirectory(SettingsDirectory);
                var tempPath = SettingsFilePath + ".tmp";
                var json = JsonSerializer.Serialize(_data, JsonOptions);
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, SettingsFilePath, overwrite: true);
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reload()
        {
            lock (_lock)
            {
                if (File.Exists(SettingsFilePath))
                {
                    try
                    {
                        var json = File.ReadAllText(SettingsFilePath);
                        _data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions) ?? CreateDefaults();
                    }
                    catch
                    {
                        // ponytail: corrupted file → fall back to defaults rather than crash
                        _data = CreateDefaults();
                    }
                }
                else
                {
                    _data = CreateDefaults();
                }
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Reset()
        {
            lock (_lock)
            {
                _data = CreateDefaults();
                if (File.Exists(SettingsFilePath))
                {
                    File.Delete(SettingsFilePath);
                }
            }

            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Upgrade()
        {
            // ponytail: no-op — JSON format doesn't need migration like .settings files
        }

        #endregion

        #region IAppSettings properties

        public bool SettingsUpgradeNeeded
        {
            get => _data.SettingsUpgradeNeeded;
            set => _data.SettingsUpgradeNeeded = value;
        }

        public bool PromptedForAutoUpdateCheck
        {
            get => _data.PromptedForAutoUpdateCheck;
            set => _data.PromptedForAutoUpdateCheck = value;
        }

        public string SelectedLanguage
        {
            get => _data.SelectedLanguage;
            set => _data.SelectedLanguage = value;
        }

        public bool StatsShowStats
        {
            get => _data.StatsShowStats;
            set => _data.StatsShowStats = value;
        }

        public bool StatsShowBuildingCount
        {
            get => _data.StatsShowBuildingCount;
            set => _data.StatsShowBuildingCount = value;
        }

        public bool ShowPavedRoadsWarning
        {
            get => _data.ShowPavedRoadsWarning;
            set => _data.ShowPavedRoadsWarning = value;
        }

        public bool EnableAutomaticUpdateCheck
        {
            get => _data.EnableAutomaticUpdateCheck;
            set => _data.EnableAutomaticUpdateCheck = value;
        }

        public bool UseCurrentZoomOnExportedImageValue
        {
            get => _data.UseCurrentZoomOnExportedImageValue;
            set => _data.UseCurrentZoomOnExportedImageValue = value;
        }

        public bool RenderSelectionHighlightsOnExportedImageValue
        {
            get => _data.RenderSelectionHighlightsOnExportedImageValue;
            set => _data.RenderSelectionHighlightsOnExportedImageValue = value;
        }

        public bool ShowLabels
        {
            get => _data.ShowLabels;
            set => _data.ShowLabels = value;
        }

        public bool ShowIcons
        {
            get => _data.ShowIcons;
            set => _data.ShowIcons = value;
        }

        public bool ShowGrid
        {
            get => _data.ShowGrid;
            set => _data.ShowGrid = value;
        }

        public bool ShowTrueInfluenceRange
        {
            get => _data.ShowTrueInfluenceRange;
            set => _data.ShowTrueInfluenceRange = value;
        }

        public bool ShowInfluences
        {
            get => _data.ShowInfluences;
            set => _data.ShowInfluences = value;
        }

        public bool ShowHarborBlockedArea
        {
            get => _data.ShowHarborBlockedArea;
            set => _data.ShowHarborBlockedArea = value;
        }

        public bool ShowPanorama
        {
            get => _data.ShowPanorama;
            set => _data.ShowPanorama = value;
        }

        public bool IsPavedStreet
        {
            get => _data.IsPavedStreet;
            set => _data.IsPavedStreet = value;
        }

        public string TreeViewSearchText
        {
            get => _data.TreeViewSearchText;
            set => _data.TreeViewSearchText = value;
        }

        public string PresetsTreeGameVersionFilter
        {
            get => _data.PresetsTreeGameVersionFilter;
            set => _data.PresetsTreeGameVersionFilter = value;
        }

        public string PresetsTreeExpandedState
        {
            get => _data.PresetsTreeExpandedState;
            set => _data.PresetsTreeExpandedState = value;
        }

        public string PresetsTreeLastVersion
        {
            get => _data.PresetsTreeLastVersion;
            set => _data.PresetsTreeLastVersion = value;
        }

        public double MainWindowHeight
        {
            get => _data.MainWindowHeight;
            set => _data.MainWindowHeight = value;
        }

        public double MainWindowWidth
        {
            get => _data.MainWindowWidth;
            set => _data.MainWindowWidth = value;
        }

        public double MainWindowLeft
        {
            get => _data.MainWindowLeft;
            set => _data.MainWindowLeft = value;
        }

        public double MainWindowTop
        {
            get => _data.MainWindowTop;
            set => _data.MainWindowTop = value;
        }

        public WindowState MainWindowWindowState
        {
            get => _data.MainWindowWindowState;
            set => _data.MainWindowWindowState = value;
        }

        public bool UpdateSupportsPrerelease
        {
            get => _data.UpdateSupportsPrerelease;
            set => _data.UpdateSupportsPrerelease = value;
        }

        public bool ShowMultipleInstanceWarning
        {
            get => _data.ShowMultipleInstanceWarning;
            set => _data.ShowMultipleInstanceWarning = value;
        }

        public string HotkeyMappings
        {
            get => _data.HotkeyMappings;
            set => _data.HotkeyMappings = value;
        }

        public string RecentFiles
        {
            get => _data.RecentFiles;
            set => _data.RecentFiles = value;
        }

        public int MaxRecentFiles
        {
            get => _data.MaxRecentFiles;
            set => _data.MaxRecentFiles = value;
        }

        public string ColorGridLines
        {
            get => _data.ColorGridLines;
            set => _data.ColorGridLines = value;
        }

        public string ColorObjectBorderLines
        {
            get => _data.ColorObjectBorderLines;
            set => _data.ColorObjectBorderLines = value;
        }

        public bool UseZoomToPoint
        {
            get => _data.UseZoomToPoint;
            set => _data.UseZoomToPoint = value;
        }

        public bool HideInfluenceOnSelection
        {
            get => _data.HideInfluenceOnSelection;
            set => _data.HideInfluenceOnSelection = value;
        }

        public double ZoomSensitivityPercentage
        {
            get => _data.ZoomSensitivityPercentage;
            set => _data.ZoomSensitivityPercentage = value;
        }

        public bool InvertPanningDirection
        {
            get => _data.InvertPanningDirection;
            set => _data.InvertPanningDirection = value;
        }

        public bool InvertScrollingDirection
        {
            get => _data.InvertScrollingDirection;
            set => _data.InvertScrollingDirection = value;
        }

        public bool ShowScrollbars
        {
            get => _data.ShowScrollbars;
            set => _data.ShowScrollbars = value;
        }

        public bool IncludeRoadsInStatisticCalculation
        {
            get => _data.IncludeRoadsInStatisticCalculation;
            set => _data.IncludeRoadsInStatisticCalculation = value;
        }

        public bool RenderVersionOnExportedImageValue
        {
            get => _data.RenderVersionOnExportedImageValue;
            set => _data.RenderVersionOnExportedImageValue = value;
        }

        public string ThemePreference
        {
            get => _data.ThemePreference;
            set => _data.ThemePreference = value;
        }

        #endregion

        #region Defaults

        private static SettingsData CreateDefaults() => new()
        {
            SettingsUpgradeNeeded = false, // ponytail: JSON doesn't use upgrade, always false
            PromptedForAutoUpdateCheck = false,
            SelectedLanguage = "Default",
            StatsShowStats = true,
            StatsShowBuildingCount = true,
            ShowPavedRoadsWarning = false,
            EnableAutomaticUpdateCheck = true,
            UseCurrentZoomOnExportedImageValue = false,
            RenderSelectionHighlightsOnExportedImageValue = false,
            ShowLabels = true,
            ShowIcons = true,
            ShowGrid = true,
            ShowTrueInfluenceRange = false,
            ShowInfluences = false,
            ShowHarborBlockedArea = false,
            ShowPanorama = false,
            IsPavedStreet = false,
            TreeViewSearchText = string.Empty,
            PresetsTreeGameVersionFilter = string.Empty,
            PresetsTreeExpandedState = string.Empty,
            PresetsTreeLastVersion = string.Empty,
            MainWindowHeight = 800,
            MainWindowWidth = 1200,
            MainWindowLeft = 0,
            MainWindowTop = 0,
            MainWindowWindowState = WindowState.Normal,
            UpdateSupportsPrerelease = false,
            ShowMultipleInstanceWarning = true,
            HotkeyMappings = string.Empty,
            RecentFiles = string.Empty,
            MaxRecentFiles = 10,
            ColorGridLines = "{\"A\":255,\"R\":0,\"G\":0,\"B\":0}",
            ColorObjectBorderLines = "{\"A\":255,\"R\":0,\"G\":0,\"B\":0}",
            UseZoomToPoint = false,
            HideInfluenceOnSelection = false,
            ZoomSensitivityPercentage = 50,
            InvertPanningDirection = true,
            InvertScrollingDirection = false,
            ShowScrollbars = true,
            IncludeRoadsInStatisticCalculation = false,
            RenderVersionOnExportedImageValue = true,
            ThemePreference = "System"
        };

        #endregion

        #region SettingsData POCO

        private sealed class SettingsData
        {
            public bool SettingsUpgradeNeeded { get; set; }
            public bool PromptedForAutoUpdateCheck { get; set; }
            public string SelectedLanguage { get; set; } = "Default";
            public bool StatsShowStats { get; set; }
            public bool StatsShowBuildingCount { get; set; }
            public bool ShowPavedRoadsWarning { get; set; }
            public bool EnableAutomaticUpdateCheck { get; set; }
            public bool UseCurrentZoomOnExportedImageValue { get; set; }
            public bool RenderSelectionHighlightsOnExportedImageValue { get; set; }
            public bool ShowLabels { get; set; }
            public bool ShowIcons { get; set; }
            public bool ShowGrid { get; set; }
            public bool ShowTrueInfluenceRange { get; set; }
            public bool ShowInfluences { get; set; }
            public bool ShowHarborBlockedArea { get; set; }
            public bool ShowPanorama { get; set; }
            public bool IsPavedStreet { get; set; }
            public string TreeViewSearchText { get; set; } = string.Empty;
            public string PresetsTreeGameVersionFilter { get; set; } = string.Empty;
            public string PresetsTreeExpandedState { get; set; } = string.Empty;
            public string PresetsTreeLastVersion { get; set; } = string.Empty;
            public double MainWindowHeight { get; set; }
            public double MainWindowWidth { get; set; }
            public double MainWindowLeft { get; set; }
            public double MainWindowTop { get; set; }
            public WindowState MainWindowWindowState { get; set; }
            public bool UpdateSupportsPrerelease { get; set; }
            public bool ShowMultipleInstanceWarning { get; set; }
            public string HotkeyMappings { get; set; } = string.Empty;
            public string RecentFiles { get; set; } = string.Empty;
            public int MaxRecentFiles { get; set; }
            public string ColorGridLines { get; set; } = string.Empty;
            public string ColorObjectBorderLines { get; set; } = string.Empty;
            public bool UseZoomToPoint { get; set; }
            public bool HideInfluenceOnSelection { get; set; }
            public double ZoomSensitivityPercentage { get; set; }
            public bool InvertPanningDirection { get; set; }
            public bool InvertScrollingDirection { get; set; }
            public bool ShowScrollbars { get; set; }
            public bool IncludeRoadsInStatisticCalculation { get; set; }
            public bool RenderVersionOnExportedImageValue { get; set; }
            public string ThemePreference { get; set; } = "System";
        }

        #endregion
    }
}
