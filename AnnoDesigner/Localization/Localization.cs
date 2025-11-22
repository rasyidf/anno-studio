using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using NLog;
using System.IO;
using Newtonsoft.Json;

namespace AnnoDesigner.Localization
{
    public class Localization : Notify, ILocalizationHelper
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private ICommons _commons;

        private static readonly Lock _initLock = new();

        #region ctor

        private static readonly Lazy<Localization> lazy = new(() => new Localization());

        public static Localization Instance => lazy.Value;

        static Localization()
        {
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                Init(Commons.Instance);
            }
        }

        private Localization() { }

        #endregion

        private static IDictionary<string, IDictionary<string, string>> TranslationsRaw { get; set; }

        private string SelectedLanguageCode => _commons?.CurrentLanguageCode ?? "eng";

        public static IDictionary<string, string> Translations
        {
            get
            {
                if (TranslationsRaw == null)
                {
                    return new Dictionary<string, string>();
                }

                if (TranslationsRaw.TryGetValue(Instance.SelectedLanguageCode, out var dict) && dict != null)
                {
                    return dict;
                }

                // fallback to english if selected language not available
                if (TranslationsRaw.TryGetValue("eng", out var engDict) && engDict != null)
                {
                    return engDict;
                }

                return new Dictionary<string, string>();
            }
        }

        public static IDictionary<string, string> InstanceTranslations => Translations;

        private static IDictionary<string, IDictionary<string, string>> LoadTranslationsFromJson()
        {
            var translations = new Dictionary<string, IDictionary<string, string>>();
            try
            {
                var localesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "locales");
                if (!Directory.Exists(localesPath))
                {
                    _logger.Warn($"Locales directory not found: {localesPath}");
                    return null;
                }

                var jsonFiles = Directory.GetFiles(localesPath, "*.json");
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var languageCode = Path.GetFileNameWithoutExtension(file);
                        var jsonContent = File.ReadAllText(file);
                        var langTranslations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);
                        if (langTranslations != null)
                        {
                            translations[languageCode] = langTranslations;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Error loading translation file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error loading translations from JSON");
                return null;
            }
            return translations;
        }

        public static void Init(ICommons commons)
        {
            if (TranslationsRaw != null)
            {
                return;
            }

            lock (_initLock)
            {
                if (TranslationsRaw != null)
                {
                    return;
                }

                TranslationsRaw = LoadTranslationsFromJson() ?? new Dictionary<string, IDictionary<string, string>>();

                Instance._commons = commons;
                Instance.Commons_SelectedLanguageChanged(null, null);
                if (commons != null)
                {
                    commons.SelectedLanguageChanged += Instance.Commons_SelectedLanguageChanged;
                }
            }
        }

        private void Commons_SelectedLanguageChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Translations));
            OnPropertyChanged(nameof(InstanceTranslations));
        }

        private static string NormalizeKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            // keep compatibility with existing keys which remove spaces
            return key.Trim().Replace(" ", string.Empty);
        }

        public string GetLocalization(string valueToTranslate)
        {
            return GetLocalization(valueToTranslate, null);
        }

        public string GetLocalization(string valueToTranslate, string languageCode = null)
        {
            if (string.IsNullOrWhiteSpace(valueToTranslate))
            {
                return valueToTranslate;
            }

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                languageCode = SelectedLanguageCode;
            }

            // use english as default language
            if (_commons != null && !_commons.LanguageCodeMap.ContainsValue(languageCode) || TranslationsRaw == null || !TranslationsRaw.ContainsKey(languageCode))
            {
                _logger.Trace($"language ({languageCode}) has no translations or is not supported");
                languageCode = "eng";
            }

            var normalizedKey = NormalizeKey(valueToTranslate);

            try
            {
                if (TranslationsRaw != null && TranslationsRaw.TryGetValue(languageCode, out var langDict) && langDict != null && langDict.TryGetValue(normalizedKey, out var foundLocalization))
                {
                    return foundLocalization;
                }

                _logger.Trace($"try to set localization to english for: \"{valueToTranslate}\"");

                if (TranslationsRaw != null && TranslationsRaw.TryGetValue("eng", out var englishDict) && englishDict != null && englishDict.TryGetValue(normalizedKey, out var engLocalization))
                {
                    return engLocalization;
                }

                _logger.Trace($"found no localization (\"eng\") and ({languageCode}) for: \"{valueToTranslate}\"");
                return valueToTranslate;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"error getting localization ({languageCode}) for: \"{valueToTranslate}\"");
                return valueToTranslate;
            }
        }
    }
}


