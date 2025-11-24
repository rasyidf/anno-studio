using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Models;
using AnnoDesigner.Models.Interface;
using static AnnoDesigner.Core.CoreConstants;

namespace AnnoDesigner.ViewModels
{
    public class PresetsTreeSearchViewModel : Notify
    {
        private string _searchText;
        private bool _hasFocus;
        private ObservableCollection<GameVersionFilter> _gameVersionFilters;
        private bool _isUpdatingGameVersionFilter;

        public PresetsTreeSearchViewModel()
        {
            ClearSearchTextCommand = new RelayCommand(ClearSearchText);
            GotFocusCommand = new RelayCommand(GotFocus);
            LostFocusCommand = new RelayCommand(LostFocus);

            ClearGameVersionFilterCommand = new RelayCommand(ClearGameVersionFilter);
            HasFocus = false;
            SearchText = string.Empty;
            GameVersionFilters = [];
            SuggestionsList = new ObservableCollection<string>();
            InitGameVersionFilters();
        }

        private void InitGameVersionFilters()
        {
            UnsubscribeFromFilterChanges();

            var order = 0;
            foreach (var curGameVersion in Enum.GetValues<GameVersion>())
            {
                if (curGameVersion is GameVersion.Unknown or GameVersion.All)
                {
                    continue;
                }

                var newFilter = new GameVersionFilter
                {
                    Name = curGameVersion.ToString().Replace("Anno", "Anno "),
                    Type = curGameVersion,
                    Order = ++order
                };

                // 🔑 SUBSCRIBE TO THE NEW EVENT HERE
                newFilter.FilterStateChanged += OnFilterItemStateChanged;

                GameVersionFilters.Add(newFilter);
            }
        }

        private void UnsubscribeFromFilterChanges()
        {
            foreach (var filter in GameVersionFilters)
            {
                filter.FilterStateChanged -= OnFilterItemStateChanged;
            }
        }

        private void OnFilterItemStateChanged(object sender, EventArgs e)
        {
            if (_isUpdatingGameVersionFilter)
            {
                return;
            }

            // 🔑 THIS IS THE KEY FIX: Notify the binding system that the derived property has changed.
            OnPropertyChanged(nameof(SelectedGameVersionFilters));
        }

        public string SearchText
        {
            get { return _searchText; }
            set { _ = UpdateProperty(ref _searchText, value); }
        }

        public bool HasFocus
        {
            get { return _hasFocus; }
            set { _ = UpdateProperty(ref _hasFocus, value); }
        }

        public ObservableCollection<GameVersionFilter> GameVersionFilters
        {
            get { return _gameVersionFilters; }
            set { _ = UpdateProperty(ref _gameVersionFilters, value); }
        }

        public ObservableCollection<GameVersionFilter> SelectedGameVersionFilters
        {
            get { return new ObservableCollection<GameVersionFilter>(GameVersionFilters.Where(x => x.IsSelected)); }
        }

        public GameVersion SelectedGameVersions
        {
            set
            {
                try
                {
                    _isUpdatingGameVersionFilter = true;

                    foreach (var curFilter in GameVersionFilters)
                    {
                        curFilter.IsSelected = value.HasFlag(curFilter.Type);
                    }
                }
                finally
                {
                    _isUpdatingGameVersionFilter = false;

                    OnPropertyChanged(nameof(SelectedGameVersionFilters));
                }
            }
        }

        #region commands

        public ICommand ClearSearchTextCommand { get; private set; }

        //TODO: refactor to use interface ICanUpdateLayout -> currently TextBox does not implement it (create own control?)
        private void ClearSearchText(object param)
        {
            SearchText = string.Empty;

            if (param is ICanUpdateLayout updateable)
            {
                updateable.UpdateLayout();
            }
            else if (param is TextBox textBox)
            {
                //Debug.WriteLine($"+ IsFocused: {textBox.IsFocused} | IsKeyboardFocused: {textBox.IsKeyboardFocused} | IsKeyboardFocusWithin: {textBox.IsKeyboardFocusWithin} | CaretIndex: {textBox.CaretIndex}");

                //SearchText = string.Empty;

                //Debug.WriteLine($"++ IsFocused: {textBox.IsFocused} | IsKeyboardFocused: {textBox.IsKeyboardFocused} | IsKeyboardFocusWithin: {textBox.IsKeyboardFocusWithin} | CaretIndex: {textBox.CaretIndex}");

                _ = textBox.Focus();
                textBox.UpdateLayout();

                //Debug.WriteLine($"+++ IsFocused: {textBox.IsFocused} | IsKeyboardFocused: {textBox.IsKeyboardFocused} | IsKeyboardFocusWithin: {textBox.IsKeyboardFocusWithin} | CaretIndex: {textBox.CaretIndex}");
            }
        }
        public ICommand ClearGameVersionFilterCommand { get; private set; }

        private void ClearGameVersionFilter(object param)
        {
            try
            {
                _isUpdatingGameVersionFilter = true;

                // Set IsSelected to false for every filter item
                foreach (var filter in GameVersionFilters)
                {
                    filter.IsSelected = false;
                }
            }
            finally
            {
                _isUpdatingGameVersionFilter = false;
                // Notify the application that the selected filters have changed
                OnPropertyChanged(nameof(SelectedGameVersionFilters));
            }
        }

        public ICommand GotFocusCommand { get; private set; }

        private void GotFocus(object param)
        {
            HasFocus = true;
        }

        public ICommand LostFocusCommand { get; private set; }

        private void LostFocus(object param)
        {
            HasFocus = false;
        }

        public ICommand GameVersionFilterChangedCommand { get; private set; }

        #endregion

        private ObservableCollection<string> _suggestionsList;

        public ObservableCollection<string> SuggestionsList
        {
            get { return _suggestionsList; }
            set { _ = UpdateProperty(ref _suggestionsList, value); }
        }

        public static explicit operator PresetsTreeSearchViewModel(MainViewModel v)
        {
            throw new NotImplementedException();
        }
    }
}
