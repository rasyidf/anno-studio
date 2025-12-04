using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using NLog;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AnnoDesigner.ViewModels
{
    public partial class GeneralSettingsViewModel : ObservableObject
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IAppSettings _appSettings;
        private readonly ICommons _commons;
        private readonly IRecentFilesHelper _recentFilesHelper;

        [ObservableProperty]
        private bool hideInfluenceOnSelection;

        [ObservableProperty]
        private bool useZoomToPoint;

        [ObservableProperty]
        private UserDefinedColor selectedGridLineColor;

        [ObservableProperty]
        private UserDefinedColor selectedObjectBorderLineColor;

        [ObservableProperty]
        private ObservableCollection<UserDefinedColor> gridLineColors;

        [ObservableProperty]
        private ObservableCollection<UserDefinedColor> objectBorderLineColors;

        [ObservableProperty]
        private bool isGridLineColorPickerVisible;

        [ObservableProperty]
        private bool isObjectBorderLineColorPickerVisible;

        [ObservableProperty]
        private Color? selectedCustomGridLineColor;

        [ObservableProperty]
        private Color? selectedCustomObjectBorderLineColor;

        [ObservableProperty]
        private double zoomSensitivityPercentage;

        [ObservableProperty]
        private bool invertPanningDirection;

        [ObservableProperty]
        private bool showScrollbars;

        [ObservableProperty]
        private bool invertScrollingDirection;

        [ObservableProperty]
        private bool includeRoadsInStatisticCalculation;

        [ObservableProperty]
        private int maxRecentFiles;

        [ObservableProperty]
        private ObservableCollection<Services.ThemePreference> themeOptions;

        [ObservableProperty]
        private Services.ThemePreference selectedTheme;

        public GeneralSettingsViewModel(IAppSettings appSettingsToUse, ICommons commonsToUse, IRecentFilesHelper recentFilesHelperToUse)
        {
            _appSettings = appSettingsToUse;
            _commons = commonsToUse;
            _commons.SelectedLanguageChanged += Commons_SelectedLanguageChanged;
            _recentFilesHelper = recentFilesHelperToUse;

            UseZoomToPoint = _appSettings.UseZoomToPoint;
            ZoomSensitivityPercentage = _appSettings.ZoomSensitivityPercentage;
            HideInfluenceOnSelection = _appSettings.HideInfluenceOnSelection;
            ShowScrollbars = _appSettings.ShowScrollbars;
            InvertPanningDirection = _appSettings.InvertPanningDirection;
            InvertScrollingDirection = _appSettings.InvertScrollingDirection;
            MaxRecentFiles = _appSettings.MaxRecentFiles;

            // Theme options
            ThemeOptions = new ObservableCollection<Services.ThemePreference>(
                Enum.GetValues<Services.ThemePreference>());
            if (Enum.TryParse<Services.ThemePreference>(_appSettings.ThemePreference, true, out var parsedTheme))
            {
                SelectedTheme = parsedTheme;
            }
            else
            {
                SelectedTheme = AnnoDesigner.Services.ThemePreference.System;
            }

            // Command generation via [RelayCommand] is used for the reset/clear actions.
            // Subscribe so generated command's CanExecute can be refreshed when recent files change.
            _recentFilesHelper.Updated += (_, __) =>
            {
                if (ClearRecentFilesCommand is IRelayCommand c)
                {
                    c.NotifyCanExecuteChanged();
                }
                if (ResetMaxRecentFilesCommand is IRelayCommand m)
                {
                    m.NotifyCanExecuteChanged();
                }
            };

            GridLineColors = new ObservableCollection<UserDefinedColor>();
            RefreshGridLineColors();
            var savedGridLineColor = SerializationHelper.LoadFromJsonString<UserDefinedColor>(_appSettings.ColorGridLines);
            if (savedGridLineColor is null)
            {
                SelectedGridLineColor = GridLineColors.First();
                SelectedCustomGridLineColor = SelectedGridLineColor.Color;
            }
            else
            {
                SelectedGridLineColor = GridLineColors.SingleOrDefault(x => x.Type == savedGridLineColor.Type);
                SelectedCustomGridLineColor = savedGridLineColor.Color;
            }

            ObjectBorderLineColors = new ObservableCollection<UserDefinedColor>();
            RefreshObjectBorderLineColors();
            var savedObjectBorderLineColor = SerializationHelper.LoadFromJsonString<UserDefinedColor>(_appSettings.ColorObjectBorderLines);
            if (savedObjectBorderLineColor is null)
            {
                SelectedObjectBorderLineColor = ObjectBorderLineColors.First();
                SelectedCustomObjectBorderLineColor = SelectedObjectBorderLineColor.Color;
            }
            else
            {
                SelectedObjectBorderLineColor = ObjectBorderLineColors.SingleOrDefault(x => x.Type == savedObjectBorderLineColor.Type);
                SelectedCustomObjectBorderLineColor = savedObjectBorderLineColor.Color;
            }
        }

        // ThemeOptions and SelectedTheme are generated by [ObservableProperty].

        private void Commons_SelectedLanguageChanged(object sender, EventArgs e)
        {
            var selectedGridLineColorType = SelectedGridLineColor.Type;
            GridLineColors.Clear();
            RefreshGridLineColors();
            SelectedGridLineColor = GridLineColors.SingleOrDefault(x => x.Type == selectedGridLineColorType);

            var selectedObjectBorderLineColorType = SelectedObjectBorderLineColor.Type;
            ObjectBorderLineColors.Clear();
            RefreshObjectBorderLineColors();
            SelectedObjectBorderLineColor = ObjectBorderLineColors.SingleOrDefault(x => x.Type == selectedObjectBorderLineColorType);
        }

        #region Color for grid lines

        private void RefreshGridLineColors()
        {
            foreach (var curColorType in Enum.GetValues<UserDefinedColorType>())
            {
                GridLineColors.Add(new UserDefinedColor
                {
                    Type = curColorType
                });
            }
        }

        // GridLineColors / SelectedGridLineColor / SelectedCustomGridLineColor / IsGridLineColorPickerVisible
        // are generated by [ObservableProperty]. Use the generated change partial methods below to attach behavior.

        private void UpdateGridLineColorVisibility()
        {
            if (SelectedGridLineColor is null)
            {
                IsGridLineColorPickerVisible = false;
                return;
            }

            IsGridLineColorPickerVisible = SelectedGridLineColor.Type switch
            {
                UserDefinedColorType.Custom => true,
                _ => false,
            };
        }

        private void SaveSelectedGridLineColor()
        {
            switch (SelectedGridLineColor.Type)
            {
                case UserDefinedColorType.Default:
                    SelectedGridLineColor.Color = Colors.Black;
                    break;
                case UserDefinedColorType.Light:
                    SelectedGridLineColor.Color = Colors.LightGray;
                    break;
                case UserDefinedColorType.Custom:
                    SelectedGridLineColor.Color = SelectedCustomGridLineColor ?? Colors.Black;
                    break;
                default:
                    break;
            }

            var json = SerializationHelper.SaveToJsonString(SelectedGridLineColor);
            _appSettings.ColorGridLines = json;
            _appSettings.Save();
        }

        #endregion

        #region Color for object border lines

        private void RefreshObjectBorderLineColors()
        {
            foreach (var curColorType in Enum.GetValues<UserDefinedColorType>())
            {
                ObjectBorderLineColors.Add(new UserDefinedColor
                {
                    Type = curColorType
                });
            }
        }

        // ObjectBorderLineColors / SelectedObjectBorderLineColor / SelectedCustomObjectBorderLineColor / IsObjectBorderLineColorPickerVisible
        // are generated by [ObservableProperty]. Use the generated change partial methods below to attach behavior.

        private void UpdateObjectBorderLineVisibility()
        {
            if (SelectedObjectBorderLineColor is null)
            {
                IsObjectBorderLineColorPickerVisible = false;
                return;
            }

            IsObjectBorderLineColorPickerVisible = SelectedObjectBorderLineColor.Type switch
            {
                UserDefinedColorType.Custom => true,
                _ => false,
            };
        }

        private void SaveSelectedObjectBorderLine()
        {
            switch (SelectedObjectBorderLineColor.Type)
            {
                case UserDefinedColorType.Default:
                    SelectedObjectBorderLineColor.Color = Colors.Black;
                    break;
                case UserDefinedColorType.Light:
                    SelectedObjectBorderLineColor.Color = Colors.LightGray;
                    break;
                case UserDefinedColorType.Custom:
                    SelectedObjectBorderLineColor.Color = SelectedCustomObjectBorderLineColor ?? Colors.Black;
                    break;
                default:
                    break;
            }

            var json = SerializationHelper.SaveToJsonString(SelectedObjectBorderLineColor);
            _appSettings.ColorObjectBorderLines = json;
            _appSettings.Save();
        }

        #endregion
 

        [RelayCommand(CanExecute = nameof(CanResetZoomSensitivity))]
        private void ResetZoomSensitivity()
        {
            ZoomSensitivityPercentage = Constants.ZoomSensitivityPercentageDefault;
        }

        private bool CanResetZoomSensitivity() => ZoomSensitivityPercentage != Constants.ZoomSensitivityPercentageDefault;

        [RelayCommand(CanExecute = nameof(CanResetMaxRecentFiles))]
        private void ResetMaxRecentFiles()
        {
            MaxRecentFiles = Constants.MaxRecentFiles;
        }

        private bool CanResetMaxRecentFiles() => MaxRecentFiles != Constants.MaxRecentFiles;

        [RelayCommand(CanExecute = nameof(CanClearRecentFiles))]
        private void ClearRecentFiles()
        {
            _recentFilesHelper.ClearRecentFiles();
        }

        private bool CanClearRecentFiles() => _recentFilesHelper.RecentFiles.Count > 0;

        // Partial change handlers to preserve original side effects of property setters
        partial void OnSelectedThemeChanged(Services.ThemePreference value)
        {
            _appSettings.ThemePreference = value.ToString();
            _appSettings.Save();
        }

        partial void OnSelectedGridLineColorChanged(UserDefinedColor value)
        {
            if (value != null)
            {
                UpdateGridLineColorVisibility();
                SaveSelectedGridLineColor();
            }
        }

        partial void OnSelectedCustomGridLineColorChanged(Color? value)
        {
            if (value != null && SelectedGridLineColor != null)
            {
                SelectedGridLineColor.Color = value.Value;
                SaveSelectedGridLineColor();
            }
        }

        partial void OnSelectedObjectBorderLineColorChanged(UserDefinedColor value)
        {
            if (value != null)
            {
                UpdateObjectBorderLineVisibility();
                SaveSelectedObjectBorderLine();
            }
        }

        partial void OnSelectedCustomObjectBorderLineColorChanged(Color? value)
        {
            if (value != null && SelectedObjectBorderLineColor != null)
            {
                SelectedObjectBorderLineColor.Color = value.Value;
                SaveSelectedObjectBorderLine();
            }
        }

        partial void OnHideInfluenceOnSelectionChanged(bool value)
        {
            _appSettings.HideInfluenceOnSelection = value;
            _appSettings.Save();
        }

        partial void OnZoomSensitivityPercentageChanged(double value)
        {
            _appSettings.ZoomSensitivityPercentage = value;
            _appSettings.Save();

            if (ResetZoomSensitivityCommand is IRelayCommand cmd) cmd.NotifyCanExecuteChanged();
        }

        partial void OnUseZoomToPointChanged(bool value)
        {
            _appSettings.UseZoomToPoint = value;
            _appSettings.Save();
        }

        partial void OnInvertScrollingDirectionChanged(bool value)
        {
            _appSettings.InvertScrollingDirection = value;
            _appSettings.Save();
        }

        partial void OnInvertPanningDirectionChanged(bool value)
        {
            _appSettings.InvertPanningDirection = value;
            _appSettings.Save();
        }

        partial void OnShowScrollbarsChanged(bool value)
        {
            _appSettings.ShowScrollbars = value;
            _appSettings.Save();
        }

        partial void OnIncludeRoadsInStatisticCalculationChanged(bool value)
        {
            _appSettings.IncludeRoadsInStatisticCalculation = value;
            _appSettings.Save();
        }

        partial void OnMaxRecentFilesChanged(int value)
        {
            _appSettings.MaxRecentFiles = value;
            _appSettings.Save();

            _recentFilesHelper.MaximumItemCount = value;

            if (ResetMaxRecentFilesCommand is IRelayCommand cmd) cmd.NotifyCanExecuteChanged();
        }
    }
}
