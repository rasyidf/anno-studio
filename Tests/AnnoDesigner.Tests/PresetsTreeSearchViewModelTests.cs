using AnnoDesigner.Models;
using AnnoDesigner.ViewModels;
using Xunit;

namespace AnnoDesigner.Tests
{
    public class PresetsTreeSearchViewModelTests
    {
        #region ctor tests

        [Fact]
        public void Ctor_ShouldSetDefaultValues()
        {
            // Arrange/Act
            var viewModel = new PresetsTreeSearchViewModel();

            // Assert
            Assert.False(viewModel.HasFocus);
            Assert.Empty(viewModel.SearchText);
            Assert.NotNull(viewModel.ClearSearchTextCommand);
            Assert.NotNull(viewModel.GotFocusCommand);
            Assert.NotNull(viewModel.LostFocusCommand);
            Assert.NotNull(viewModel.GameVersionFilterChangedCommand);
        }

        #endregion

        #region Debounced search tests

        [Fact]
        public async System.Threading.Tasks.Task SearchText_UpdatesDebouncedSearchTextAfterDelay()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();

            // Act
            viewModel.SearchText = "delayed";

            // Wait for debounce period in view model (+ margin)
            await System.Threading.Tasks.Task.Delay(400);

            // Assert
            Assert.Equal("delayed", viewModel.DebouncedSearchText);
        }

        #endregion

        #region GotFocusCommand tests

        [Fact]
        public void GotFocusCommand_ShouldCanExecute()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();

            // Act
            var result = viewModel.GotFocusCommand.CanExecute(null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GotFocusCommand_IsExecuted_ShouldSetHasFocusTrue()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();
            viewModel.HasFocus = false;

            // Act
            viewModel.GotFocusCommand.Execute(null);

            // Assert
            Assert.True(viewModel.HasFocus);
        }

        #endregion

        #region LostFocusCommand tests

        [Fact]
        public void LostFocusCommand_ShouldCanExecute()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();

            // Act
            var result = viewModel.LostFocusCommand.CanExecute(null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void LostFocusCommand_IsExecuted_ShouldSetHasFocusFalse()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();
            viewModel.HasFocus = true;

            // Act
            viewModel.LostFocusCommand.Execute(null);

            // Assert
            Assert.False(viewModel.HasFocus);
        }

        #endregion

        #region ClearSearchTextCommand tests

        [Fact]
        public void ClearSearchTextCommand_ShouldCanExecute()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();

            // Act
            var result = viewModel.ClearSearchTextCommand.CanExecute(null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ClearSearchTextCommand_IsExecuted_ShouldSetSearchTextEmpty()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();
            viewModel.SearchText = "dummy";

            // Act
            viewModel.ClearSearchTextCommand.Execute(null);

            // Assert
            Assert.Empty(viewModel.SearchText);
        }

        #endregion

        #region GameVersionFilterChangedCommand  tests

        [Fact]
        public void GameVersionFilterChangedCommand_IsExecutedWithGameVersionFilter_ShouldNegateIsSelected()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();

            var gameVersionFilter = new GameVersionFilter
            {
                IsSelected = true
            };

            // Act - command should not directly toggle the value (the UI does this),
            // it should only notify the view model that selection changed.
            viewModel.GameVersionFilterChangedCommand.Execute(gameVersionFilter);

            // Assert - IsSelected should remain unchanged (true)
            Assert.True(gameVersionFilter.IsSelected);
        }

        [Fact]
        public void GameVersionFilterChangedCommand_IsExecuted_ShouldRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();

            // Act/Assert
            Assert.PropertyChanged(viewModel,
                nameof(PresetsTreeSearchViewModel.SelectedGameVersionFilters),
                () => viewModel.GameVersionFilterChangedCommand.Execute(null));
        }

        #endregion

        #region SelectedGameVersionFilters  tests

        [Fact]
        public void SelectedGameVersionFilters_IsCalled_ShouldReturnCorrectCollection()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();
            viewModel.GameVersionFilters[0].IsSelected = true;

            // Act
            var result = viewModel.SelectedGameVersionFilters;

            // Assert
            _ = Assert.Single(result);
        }

        #endregion

        #region SelectedGameVersions  tests

        [Fact]
        public void SelectedGameVersions_IsCalled_ShouldSetIsSelectedForCorrectItems()
        {
            // Arrange
            var viewModel = new PresetsTreeSearchViewModel();
            viewModel.GameVersionFilters[0].IsSelected = false;
            viewModel.GameVersionFilters[1].IsSelected = false;

            // Act
            viewModel.SelectedGameVersions = viewModel.GameVersionFilters[0].Type | viewModel.GameVersionFilters[1].Type;

            // Assert
            Assert.True(viewModel.GameVersionFilters[0].IsSelected);
            Assert.True(viewModel.GameVersionFilters[1].IsSelected);
        }

        #endregion
    }
}
