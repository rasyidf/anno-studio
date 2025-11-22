using AnnoDesigner.Helper;

namespace AnnoDesigner.Models.Interface
{
    public interface IHotkeySource
    {
        void RegisterHotkeys(HotkeyCommandManager manager);
        HotkeyCommandManager HotkeyCommandManager { get; set; }
    }
}
