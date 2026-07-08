using System.Collections.Generic;
using System.Windows;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Core.Presets.Models;
using AnnoDesigner.Import;
using Microsoft.Win32;

namespace AnnoDesigner.Views
{
    /// <summary>
    /// Import window for Anno 117 (Pax Romana) savegame files.
    /// </summary>
    public partial class ImportSavegameWindow
    {
        private LayoutFile _layoutFile;
        private readonly BuildingPresets _presets;

        /// <summary>
        /// The objects imported from the selected island, or null if cancelled.
        /// </summary>
        public List<AnnoObject> ImportedObjects { get; private set; }

        public bool CanImport => IslandComboBox?.SelectedItem != null;

        public ImportSavegameWindow(BuildingPresets presets)
        {
            _presets = presets;
            InitializeComponent();
            DataContext = this;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Anno 117 Savegame (*.a8s)|*.a8s|All Files (*.*)|*.*",
                Title = "Select Anno 117 Savegame"
            };

            if (dialog.ShowDialog(this) == true)
            {
                FilePathTextBox.Text = dialog.FileName;
                LoadSavegame(dialog.FileName);
            }
        }

        private void LoadSavegame(string path)
        {
            try
            {
                var reader = new Anno117.SavegameReader();
                _layoutFile = reader.ImportLayout(path, _presets);

                SessionComboBox.Items.Clear();
                IslandComboBox.Items.Clear();

                if (_layoutFile.Sessions == null || _layoutFile.Sessions.Count == 0)
                {
                    // Fall back to flat objects list if no sessions
                    if (_layoutFile.Objects != null && _layoutFile.Objects.Count > 0)
                    {
                        ImportedObjects = _layoutFile.Objects;
                        SessionComboBox.Items.Add("Default");
                        SessionComboBox.SelectedIndex = 0;
                        IslandComboBox.Items.Add("All Buildings");
                        IslandComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show("No buildings found in savegame.", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    return;
                }

                foreach (var session in _layoutFile.Sessions)
                {
                    SessionComboBox.Items.Add(session.Name ?? "Unknown Session");
                }

                SessionComboBox.SelectedIndex = 0;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to read savegame:\n{ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SessionComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            IslandComboBox.Items.Clear();

            if (_layoutFile?.Sessions == null || SessionComboBox.SelectedIndex < 0)
                return;

            var session = _layoutFile.Sessions[SessionComboBox.SelectedIndex];
            if (session.Islands == null)
                return;

            foreach (var island in session.Islands)
            {
                IslandComboBox.Items.Add(island.Name ?? "Unknown Island");
            }

            if (IslandComboBox.Items.Count > 0)
                IslandComboBox.SelectedIndex = 0;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_layoutFile == null || SessionComboBox.SelectedIndex < 0 || IslandComboBox.SelectedIndex < 0)
                return;

            if (_layoutFile.Sessions != null && _layoutFile.Sessions.Count > 0)
            {
                var session = _layoutFile.Sessions[SessionComboBox.SelectedIndex];
                var island = session.Islands[IslandComboBox.SelectedIndex];
                ImportedObjects = island.Objects ?? new List<AnnoObject>();
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
