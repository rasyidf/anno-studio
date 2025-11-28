using System.Linq;
using AnnoStudio.ViewModels;
using AnnoStudio.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Dock.Model.Core;

namespace AnnoStudio
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
                // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
                DisableAvaloniaDataAnnotationValidation();
                
                // Switch for playground-first development/testing.
                // Keep the standard MainWindow creation commented so we can quickly revert.
                // var mainWindow = new MainWindow();
                // desktop.MainWindow = mainWindow;
                // mainWindow.DataContext = new MainWindowViewModel(mainWindow);

                var playground = new Views.PlaygroundWindow();
                desktop.MainWindow = playground;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void DisableAvaloniaDataAnnotationValidation()
        {
            // Get an array of plugins to remove
            var dataValidationPluginsToRemove =
                BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

            // remove each entry found
            foreach (var plugin in dataValidationPluginsToRemove)
            {
                BindingPlugins.DataValidators.Remove(plugin);
            }
        }
    }
    
    public class DockViewLocator : IDataTemplate
    {
        public Control? Build(object? data)
        {
            if (data is IDockable dockable)
            {
                return dockable.Id switch
                {
                    "BuildingPresets" => new BuildingPresetsView(),
                    "Canvas" => new CanvasView(),
                    "Statistics" => new StatisticsView(),
                    "Properties" => new PropertiesView(),
                    "Toolbar" => new ToolbarView(),
                    _ => new TextBlock { Text = $"View not found for: {dockable.Id}" }
                };
            }
            return new TextBlock { Text = "Invalid data" };
        }

        public bool Match(object? data) => data is IDockable;
    }
}