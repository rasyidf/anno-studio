using System;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AnnoDesigner.Services
{
    public enum ThemePreference
    {
        System,
        Light,
        Dark
    }

    public class ThemeService
    {
        public void WatchSystemTheme(Application app)
        {
            SystemThemeWatcher.Watch(app.MainWindow);
        }

        public void ApplySystemTheme()
        {
            ApplicationThemeManager.ApplySystemTheme();
        }

        public void Apply(ThemePreference preference, WindowBackdropType backdrop = WindowBackdropType.Mica)
        {
            switch (preference)
            {
                case ThemePreference.Light: 
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, backdrop);
                    break;
                case ThemePreference.Dark: 
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, backdrop);
                    break; 
                default: 
                    ApplicationThemeManager.ApplySystemTheme();
                    break;
            }
             
        }

        public void ApplyFromString(string preference, WindowBackdropType backdrop = WindowBackdropType.Mica)
        {
            if (Enum.TryParse<ThemePreference>(preference, true, out var parsed))
            {
                Apply(parsed, backdrop);
            }
            else
            {
                Apply(ThemePreference.System, backdrop);
            }
        }

        public ApplicationTheme GetSystemTheme()
        {
            return ApplicationThemeManager.GetAppTheme();
        }

        public ApplicationTheme GetCurrentTheme()
        {
            return ApplicationThemeManager.GetAppTheme();
        }
    }
}
