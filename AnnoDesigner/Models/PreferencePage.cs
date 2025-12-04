using AnnoDesigner.Core.Models;

namespace AnnoDesigner.Models
{
    public class PreferencePage : Notify
    {
        private string _headerKeyForTranslation;
        private string _name;
        private object _viewModel;

        public string HeaderKeyForTranslation
        {
            get { return _headerKeyForTranslation; }
            set { _ = UpdateProperty(ref _headerKeyForTranslation, value); }
        }

        public string Name
        {
            get { return _name; }
            set { _ = UpdateProperty(ref _name, value); }
        }

        public object ViewModel
        {
            get { return _viewModel; }
            set { _ = UpdateProperty(ref _viewModel, value); }
        }
    }
}
