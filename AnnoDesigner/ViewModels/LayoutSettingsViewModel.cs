using System;
using AnnoDesigner.Core.Models;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AnnoDesigner.ViewModels
{
    public partial class LayoutSettingsViewModel : ObservableObject, INotifyPropertyChangedWithValues<object>
    {
        public event EventHandler<PropertyChangedWithValuesEventArgs<object>> PropertyChangedWithValues;
        public LayoutSettingsViewModel()
        {
            _layoutVersion = new Version(1, 0, 0, 0);
        }

        private Version _layoutVersion;

        public Version LayoutVersion
        {
            get => _layoutVersion;
            set
            {
                if (value is null)
                {
                    return;
                }

                    if (!EqualityComparer<Version>.Default.Equals(_layoutVersion, value))
                    {
                        // raise the old/new event first to preserve previous Notify behavior
                        PropertyChangedWithValues?.Invoke(this, new PropertyChangedWithValuesEventArgs<object>(nameof(LayoutVersion), _layoutVersion, value));

                        if (SetProperty(ref _layoutVersion, value))
                        {
                            OnPropertyChanged(nameof(LayoutVersionDisplayValue));
                        }
                    }
            }
        }

        public string LayoutVersionDisplayValue
        {
            get { return _layoutVersion.ToString(); }
            set
            {
                if (Version.TryParse(value, out var parsedVersion))
                {
                    LayoutVersion = parsedVersion;
                }
            }
        }
    }
}
