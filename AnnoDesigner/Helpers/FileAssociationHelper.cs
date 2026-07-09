using System.Runtime.InteropServices;
using AnnoDesigner.Core.Extensions;
using AnnoDesigner.Core.Services;
using AnnoDesigner.Models.Interface;
using Microsoft.Win32;

namespace AnnoDesigner.Helper
{
    public static class FileAssociationHelper
    {
        public static void RegisterExtension(string executablePath, IMessageBoxService messageBox, ILocalizationHelper localization)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // registers the anno_designer class type and adds the correct command string to pass a file argument to the application
                Registry.SetValue(Constants.FileAssociationRegistryKey, null,
                    $"\"{executablePath}\" open \"%1\"");
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\anno_designer\DefaultIcon", null,
                    $"\"{executablePath}\",0");
                // registers the .ad file extension to the anno_designer class
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\.ad", null, "anno_designer");

                ShowRegistrationMessageBox(messageBox, localization, isDeregistration: false);
            }
        }

        public static void UnregisterExtension(IMessageBoxService messageBox, ILocalizationHelper localization)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var regCheckAdFileExtension = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default)
                    .OpenSubKey(@"Software\Classes\anno_designer", false);
                if (regCheckAdFileExtension != null)
                {
                    // removes the registry entries when exists          
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\anno_designer");
                    Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\.ad");

                    ShowRegistrationMessageBox(messageBox, localization, isDeregistration: true);
                }
            }
        }

        public static void UpdateRegisteredExtension(string executablePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            if ($"\"{executablePath}\" \"%1\""
                .Equals(Registry.GetValue(Constants.FileAssociationRegistryKey, null, null)))
            {
                Registry.SetValue(Constants.FileAssociationRegistryKey, null,
                    $"\"{executablePath}\" open \"%1\"");
            }
        }

        private static void ShowRegistrationMessageBox(IMessageBoxService messageBox, ILocalizationHelper localization, bool isDeregistration)
        {
            var message = isDeregistration
                ? localization.GetLocalization("UnregisterFileExtensionSuccessful")
                : localization.GetLocalization("RegisterFileExtensionSuccessful");

            messageBox.ShowMessage(message, localization.GetLocalization("Successful"));
        }
    }
}
