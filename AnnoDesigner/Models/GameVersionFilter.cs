using System;
using System.Diagnostics;
using AnnoDesigner.Core.Models;
using static AnnoDesigner.Core.CoreConstants;

namespace AnnoDesigner.Models
{
    [DebuggerDisplay("{" + nameof(Name) + ",nq} ({" + nameof(IsSelected) + "})")]
    public class GameVersionFilter : Notify
    {
        private GameVersion _gameVersion;
        private string _name;
        private bool _isSelected;
        private int _order;

        public GameVersion Type
        {
            get { return _gameVersion; }
            set { _ = UpdateProperty(ref _gameVersion, value); }
        }

        public string Name
        {
            get { return _name; }
            set { _ = UpdateProperty(ref _name, value); }
        }
         
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (UpdateProperty(ref _isSelected, value))
                { 
                    OnFilterChanged();
                }
            }
        }
         
        public event EventHandler FilterStateChanged;

        protected virtual void OnFilterChanged()
        {
            FilterStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public int Order
        {
            get { return _order; }
            set { _ = UpdateProperty(ref _order, value); }
        }
    }
}
