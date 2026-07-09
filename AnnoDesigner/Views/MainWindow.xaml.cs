using System;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnnoDesigner.CommandLine;
using AnnoDesigner.CommandLine.Arguments;
using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Extensions;
using AnnoDesigner.ViewModels;
using AnnoDesigner.Services;
using System.Linq;
using NLog;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace AnnoDesigner.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ICloseable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private static MainViewModel _mainViewModel;
        private readonly IAppSettings _appSettings;

        public new MainViewModel DataContext
        {
            get => base.DataContext as MainViewModel;
            set => base.DataContext = value;
        }

        private void AttachDocumentHandlers(MainViewModel vm)
        {
            if (vm == null) return;

            vm.Documents.CollectionChanged += Documents_CollectionChanged;

            // create existing documents
            foreach (var doc in vm.Documents)
            {
                AddLayoutDocument(doc);
            }
        }

        private void Documents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (DocumentViewModel doc in e.NewItems)
                {
                    AddLayoutDocument(doc);
                }
            }

            if (e.OldItems != null)
            {
                foreach (DocumentViewModel doc in e.OldItems)
                {
                    RemoveLayoutDocument(doc);
                }
            }
        }

        private void AddLayoutDocument(DocumentViewModel doc)
        {
            // Wrap canvas in ScrollViewer with scrollbar visibility binding
            var scrollViewer = new System.Windows.Controls.ScrollViewer
            {
                CanContentScroll = true,
                Focusable = false,
                Content = doc.Canvas
            };

            // Bind ScrollViewer scrollbar visibility to MainViewModel.ShowScrollbars
            var scrollbarBinding = new System.Windows.Data.Binding("ShowScrollbars")
            {
                Source = _mainViewModel
            };
            System.Windows.Data.BindingOperations.SetBinding(scrollViewer, System.Windows.Controls.ScrollViewer.HorizontalScrollBarVisibilityProperty, scrollbarBinding);
            System.Windows.Data.BindingOperations.SetBinding(scrollViewer, System.Windows.Controls.ScrollViewer.VerticalScrollBarVisibilityProperty, scrollbarBinding);

            var layoutDocument = new AvalonDock.Layout.LayoutDocument
            {
                ContentId = doc.DocumentId.ToString(),
                Content = scrollViewer,
                CanClose = true
            };

            // bind title with unsaved converter (use BindingOperations for non-FrameworkElement)
            var multi = new System.Windows.Data.MultiBinding { Converter = (System.Windows.Data.IMultiValueConverter)System.Windows.Application.Current.Resources["ConverterUnsavedChanges"] };
            multi.Bindings.Add(new System.Windows.Data.Binding("DocumentTitle") { Source = doc });
            multi.Bindings.Add(new System.Windows.Data.Binding("IsDirty") { Source = doc });
            System.Windows.Data.BindingOperations.SetBinding(layoutDocument, AvalonDock.Layout.LayoutContent.TitleProperty, multi);

            // synchronize IsSelected <-> Document.IsActive
            // layoutDocument may not expose a public dependency property for IsSelected, so synchronize via property changed notifications
            if (layoutDocument is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "IsSelected")
                    {
                        // set doc's IsActive when layout document selection changes
                        doc.IsActive = layoutDocument.IsSelected;
                    }
                };
            }

            doc.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DocumentViewModel.IsActive))
                {
                    layoutDocument.IsSelected = doc.IsActive;
                }
            };

            documentPane.Children.Add(layoutDocument);
        }

        private void RemoveLayoutDocument(DocumentViewModel doc)
        {
            var toRemove = documentPane.Children.FirstOrDefault(d => d.ContentId == doc.DocumentId.ToString());
            if (toRemove != null)
            {
                documentPane.Children.Remove(toRemove);
            }
        }

        #region Initialization

        public MainWindow(IAppSettings appSettingsToUse)
        {
            SystemThemeWatcher.Watch(this);
            InitializeComponent();

            _appSettings = appSettingsToUse;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _mainViewModel = DataContext;

            // hook document collection so tabs are created/removed dynamically
            AttachDocumentHandlers(_mainViewModel);

            App.DpiScale = VisualTreeHelper.GetDpi(this);

            DpiChanged += MainWindow_DpiChanged;

            _mainViewModel.LoadSettings();

            _mainViewModel.LoadAvailableIcons();

            //load presets before checking for updates
            _mainViewModel.LoadPresets();

            // check for updates on startup
            if (_appSettings.EnableAutomaticUpdateCheck)
            {
                //just fire and forget
                _ = _mainViewModel.PreferencesUpdateViewModel.CheckForUpdates(isAutomaticUpdateCheck: true);
            }

            // load color presets 
            //try
            //{
            //    ColorPresetsLoader loader = new ColorPresetsLoader();
            //    var defaultScheme = loader.LoadDefaultScheme();
            //    foreach (var curPredefinedColor in defaultScheme.Colors.GroupBy(x => x.Color).Select(x => x.Key))
            //    {
            //        //colorPicker.StandardColors.Add(new Xceed.Wpf.Toolkit.ColorItem(curPredefinedColor.Color, $"{curPredefinedColor.TargetTemplate}"));
            //        colorPicker.StandardColors.Add(new Xceed.Wpf.Toolkit.ColorItem(curPredefinedColor, curPredefinedColor.ToHex()));
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message, "Loading of the color presets failed");
            //}            

            // load file given by argument
            if (App.StartupArguments is OpenArgs startupArgs && !string.IsNullOrEmpty(startupArgs.FilePath))
            {
                using var _ = _mainViewModel.OpenFile(startupArgs.FilePath);
            }
            // export layout to image
            else if (App.StartupArguments is ExportArgs exportArgs &&
                     !string.IsNullOrEmpty(exportArgs.LayoutFilePath) &&
                     !string.IsNullOrEmpty(exportArgs.ExportedImageFilePath))
            {
                var layout = new LayoutLoader().LoadLayout(exportArgs.LayoutFilePath);
                _mainViewModel.ExportService.PrepareCanvasForRender(layout.Objects, [], Math.Max(exportArgs.Border, 0),
                    new Models.CanvasRenderSetting()
                    {
                        GridSize = exportArgs.GridSize,
                        RenderGrid =
                            exportArgs.RenderGrid ?? (!exportArgs.UseUserSettings || _appSettings.ShowGrid),
                        RenderIcon =
                            exportArgs.RenderIcon ?? (!exportArgs.UseUserSettings || _appSettings.ShowIcons),
                        RenderLabel =
                            exportArgs.RenderLabel ?? (!exportArgs.UseUserSettings || _appSettings.ShowLabels),
                        RenderStatistics =
                            exportArgs.RenderStatistics ??
                            (!exportArgs.UseUserSettings || _appSettings.StatsShowStats),
                        RenderVersion = exportArgs.RenderVersion ?? true,
                        RenderHarborBlockedArea =
                            exportArgs.RenderHarborBlockedArea ??
                            (exportArgs.UseUserSettings && _appSettings.ShowHarborBlockedArea),
                        RenderInfluences =
                            exportArgs.RenderInfluences ??
                            (exportArgs.UseUserSettings && _appSettings.ShowInfluences),
                        RenderPanorama =
                            exportArgs.RenderPanorama ?? (exportArgs.UseUserSettings && _appSettings.ShowPanorama),
                        RenderTrueInfluenceRange =
                            exportArgs.RenderTrueInfluenceRange ??
                            (exportArgs.UseUserSettings && _appSettings.ShowTrueInfluenceRange)
                    }).RenderToFile(exportArgs.ExportedImageFilePath);

                ConsoleManager.Show();
                Console.WriteLine($"Export completed: \"{exportArgs.LayoutFilePath}\"");
                ConsoleManager.Hide();

                Close();
            }
        }

        #endregion

        #region UI events

        private void MainWindow_DpiChanged(object sender, DpiChangedEventArgs e)
        {
            App.DpiScale = e.NewDpi;
        }

        #endregion

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            var ok = _mainViewModel?.AnnoCanvas?.CheckUnsavedChanges().ConfigureAwait(false).GetAwaiter().GetResult() ?? true;
            if (!ok)
            {
                e.Cancel = true;
                return;
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            _mainViewModel.MainWindowWindowState = WindowState;

            _mainViewModel.SaveSettings();

#if DEBUG
            var userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal)
                .FilePath;
            Logger.Trace($"saving settings: \"{userConfig}\"");
#endif
        }

        public void Close()
        {
            throw new NotImplementedException();
        }
    }
}