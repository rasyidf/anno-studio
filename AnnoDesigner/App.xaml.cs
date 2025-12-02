using System;
using System.CommandLine;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AnnoDesigner.CommandLine;
using AnnoDesigner.CommandLine.Arguments;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Helper;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Loader;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Core.RecentFiles;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using AnnoDesigner.Services;
using AnnoDesigner.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Targets;
using AnnoDesigner.Views;

namespace AnnoDesigner
{
    public partial class App : Application
    {
        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; } 

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string AppMutexName = "AnnoDesigner_SingleInstance_Mutex";
        private Mutex _appMutex;

        /// <summary>
        /// The DPI information for the current monitor.
        /// </summary>
        public static DpiScale DpiScale { get; set; }

        public new MainWindow MainWindow { get => base.MainWindow as MainWindow; set => base.MainWindow = value; }


        public App()
        {

            Services = ConfigureServices();

            this.InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException(e.ExceptionObject as Exception, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) => LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");

            TaskScheduler.UnobservedTaskException += (s, e) => LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
        }

        public static string ExecutablePath => Assembly.GetEntryAssembly().Location;
        public static string ApplicationPath => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static IProgramArgs StartupArguments { get; private set; }

        protected override async void OnStartup(StartupEventArgs e)
        {

            InitializeLogging();
            logger.Info($"program version: {Assembly.GetExecutingAssembly().GetName().Version}");

            var argParser = new ArgumentParser(new ConsoleManager.LazyConsole());
            StartupArguments = argParser.Parse(e.Args);

            if (StartupArguments is null)
            {
                ConsoleManager.Show();
                if (ConsoleManager.StartedWithoutConsole)
                {
                    Console.WriteLine("Press enter to exit");
                    Console.ReadLine();
                }
                ConsoleManager.Hide();
                Shutdown(0);
                return;
            }


            base.OnStartup(e);

            bool isNewInstance;
            _appMutex = new Mutex(true, AppMutexName, out isNewInstance);

            var appSettings = Services.GetRequiredService<IAppSettings>();
            var themeService = Services.GetRequiredService<ThemeService>();
            var updateHelper = Services.GetRequiredService<IUpdateHelper>();
            var messageBoxService = Services.GetRequiredService<IMessageBoxService>();
            var fileSystem = Services.GetRequiredService<IFileSystem>();

            try
            {
                appSettings.Reload();
                if (appSettings.SettingsUpgradeNeeded)
                {
                    appSettings.Upgrade();
                    appSettings.SettingsUpgradeNeeded = false;
                    appSettings.Save();
                }
            }
            catch (ConfigurationErrorsException ex)
            {
                HandleConfigCorruption(ex, messageBoxService, fileSystem, appSettings);
            }

            // WPF-UI: watch system theme and apply saved preference
            themeService.WatchSystemTheme(this);
            themeService.ApplyFromString(appSettings.ThemePreference);
            appSettings.SettingsChanged += (s, ev) =>
            {
                themeService.ApplyFromString(appSettings.ThemePreference);
            };

            if (!isNewInstance)
            {
                if (appSettings.ShowMultipleInstanceWarning && await updateHelper.AreUpdatedPresetsFilesPresentAsync())
                {
                    messageBoxService.ShowMessage(Localization.Localization.Instance.GetLocalization("WarningMultipleInstancesAreRunning"));
                }

            }
            else
            {
                await updateHelper.ReplaceUpdatedPresetsFilesAsync();
            }
             
            var commons = Services.GetRequiredService<ICommons>();

            MainViewModel.UpdateRegisteredExtension();

            MainWindow = Services.GetRequiredService<MainWindow>();

            var mainVM = Services.GetRequiredService<MainViewModel>();
            MainWindow.DataContext = mainVM;

            // Language Setup
            if (commons.LanguageCodeMap.ContainsKey(appSettings.SelectedLanguage))
            {
                commons.CurrentLanguage = appSettings.SelectedLanguage;
            }
            else
            {
                if (StartupArguments is not ExportArgs)
                {
                    var w = new Welcome();
                    w.DataContext = mainVM.WelcomeViewModel; 
                    w.ShowDialog();
                }
                else
                {
                    commons.CurrentLanguage = "English";
                }
            }

            MainWindow.Show();
        }

        private void InitializeLogging()
        {
            try
            {
                var configPath = Path.Combine(ApplicationPath, "nlog.config");
                if (File.Exists(configPath))
                {
                    LogManager.Setup().LoadConfigurationFromFile(configPath);
                }
                else
                {
                    LogManager.Setup().LoadConfigurationFromFile("nlog.config");
                }
            }
            catch
            {
                // ignore logging setup errors
            }
        }

        private IServiceProvider ConfigureServices()
        {
            var commons = Commons.Instance;
            var appSettings = AppSettings.Instance;
            Localization.Localization.Init(commons);  

            var _services = new ServiceCollection() 
                .AddSingleton<ICommons>(commons)
                .AddSingleton<IAppSettings>(appSettings)
                .AddSingleton<ThemeService>()
                .AddTransient<IMessageBoxService, MessageBoxService>()
                .AddSingleton<ILocalizationHelper>(AnnoDesigner.Localization.Localization.Instance)
                .AddTransient<IFileSystem, FileSystem>()
                .AddTransient<IConsole, ConsoleManager.LazyConsole>()
                .AddTransient<ITreeLocalizationLoader, TreeLocalizationLoader>()
                 
                .AddTransient<IUpdateHelper>(sp => new UpdateHelper(
                    ApplicationPath,
                    sp.GetRequiredService<IAppSettings>(),
                    sp.GetRequiredService<IMessageBoxService>(),
                    sp.GetRequiredService<ILocalizationHelper>()))
                // canvas file services
                .AddTransient<Controls.Canvas.Services.Contracts.IFileDialogService, Controls.Canvas.Services.FileDialogService>()
                // factory that produces an ILayoutFileService for an IUndoManager. AnnoCanvas has its own UndoManager
                // so we register a factory that will be used by the control to create a per-canvas LayoutFileService wired
                // with the app-wide IMessageBoxService and ILocalizationHelper.
                .AddTransient<Func<Services.Undo.IUndoManager, Controls.Canvas.Services.ILayoutFileService>>(sp =>
                    undo => new Controls.Canvas.Services.LayoutFileService(
                        undo,
                        sp.GetRequiredService<IMessageBoxService>(),
                        sp.GetRequiredService<ILocalizationHelper>(),
                        sp.GetService<Controls.Canvas.Services.Contracts.IFileDialogService>()))
                 
                .AddTransient<RecentFilesAppSettingsSerializer>()
                 
                .AddTransient<IRecentFilesHelper>(sp => {
                    var settings = sp.GetRequiredService<IAppSettings>();
                    var serializer = sp.GetRequiredService<RecentFilesAppSettingsSerializer>();
                    var fs = sp.GetRequiredService<IFileSystem>();
                    return new RecentFilesHelper(serializer, fs, settings.MaxRecentFiles);
                })
                 
                .AddSingleton<MainViewModel>()
                 
                .AddSingleton<MainWindow>()

                .BuildServiceProvider();
            return _services;
        }

        private void HandleConfigCorruption(ConfigurationErrorsException ex, IMessageBoxService msg, IFileSystem fs, IAppSettings settings)
        {
            logger.Error(ex, "Error upgrading settings.");
            msg.ShowError(Localization.Localization.Instance.GetLocalization("ErrorUpgradingSettings"));

            var fileName = ex.Filename;
            if (string.IsNullOrEmpty(fileName) && ex.InnerException is ConfigurationErrorsException inner)
            {
                fileName = inner.Filename;
            }

            if (!string.IsNullOrEmpty(fileName) && fs.File.Exists(fileName))
            {
                fs.File.Delete(fileName);
            }
            settings.Reload();
        }

        private void LogUnhandledException(Exception ex, string @event)
        {
            logger.Error(ex, @event);
            ShowMessageWithUnexpectedError(false);

            // Ensure MainWindow is actually created before accessing it
            if (MainWindow?.DataContext is MainViewModel vm)
            {
                vm.AnnoCanvas?.CheckUnsavedChangesBeforeCrash();
            }

            Environment.Exit(-1);
        }

        public static void ShowMessageWithUnexpectedError(bool exitProgram = true)
        {
            var message = "An unhandled exception occurred.";

            var fileTarget = LogManager.Configuration?.FindTargetByName("MainLogger") as FileTarget;
            var logFile = fileTarget?.FileName.Render(new LogEventInfo());

            if (!string.IsNullOrWhiteSpace(logFile))
            {
                var fullPath = Path.Combine(ApplicationPath, logFile);
                if (File.Exists(fullPath))
                {
                    message += $"{Environment.NewLine}{Environment.NewLine}Details in \"{fullPath}\".";
                }
            }

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            if (exitProgram)
            {
                Environment.Exit(-1);
            }
        }

    }
}