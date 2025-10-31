using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using KenshiModManager.Views;

namespace KenshiModManager.Core
{
    public static class LocalizationManager
    {
        private static readonly string[] SupportedLanguages = { "en", "ru", "pt" };

        public static void Initialize(AppSettings settings)
        {
            string languageCode;

            if (!string.IsNullOrEmpty(settings.Language))
            {
                languageCode = settings.Language;
                Console.WriteLine($"[LocalizationManager] Using saved language: {languageCode}");
            }
            else
            {
                languageCode = GetSystemLanguageCode();
                Console.WriteLine($"[LocalizationManager] Auto-detected system language: {languageCode}");

                settings.Language = languageCode;
                settings.Save();
            }

            ApplyLanguage(languageCode);
        }

        public static void ApplyLanguage(string languageCode)
        {
            try
            {
                if (!IsLanguageSupported(languageCode))
                {
                    Console.WriteLine($"[LocalizationManager] Language '{languageCode}' not supported, falling back to English");
                    languageCode = "en";
                }

                CultureInfo culture = new CultureInfo(languageCode);

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                Console.WriteLine($"[LocalizationManager] Applied language: {culture.DisplayName} ({culture.Name})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalizationManager] Error applying language '{languageCode}': {ex.Message}");

                var fallbackCulture = new CultureInfo("en");
                Thread.CurrentThread.CurrentCulture = fallbackCulture;
                Thread.CurrentThread.CurrentUICulture = fallbackCulture;
            }
        }

        public static void ChangeLanguage(string languageCode, AppSettings settings)
        {
            if (Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == languageCode)
            {
                Console.WriteLine($"[LocalizationManager] Language already set to: {languageCode}");
                return;
            }

            ApplyLanguage(languageCode);

            settings.Language = languageCode;
            settings.Save();

            Console.WriteLine($"[LocalizationManager] Language changed to: {languageCode}");

            RequestUIRefresh();
        }

        private static string GetSystemLanguageCode()
        {
            try
            {
                var systemLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

                if (IsLanguageSupported(systemLanguage))
                    return systemLanguage;

                var cultureName = CultureInfo.CurrentUICulture.Name;
                if (cultureName.StartsWith("pt", StringComparison.OrdinalIgnoreCase))
                    return "pt";
                if (cultureName.StartsWith("ru", StringComparison.OrdinalIgnoreCase))
                    return "ru";

                return "en";
            }
            catch
            {
                return "en";
            }
        }

        public static bool IsLanguageSupported(string languageCode)
        {
            return Array.Exists(SupportedLanguages, lang =>
                lang.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetCurrentLanguage()
        {
            return Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
        }

        public static string GetLanguageDisplayName(string languageCode)
        {
            return languageCode.ToLower() switch
            {
                "en" => "English",
                "ru" => "Русский",
                "pt" => "Português",
                _ => languageCode
            };
        }

        private static void RequestUIRefresh()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                var app = Application.Current;
                var oldWindow = app?.MainWindow;

                if (oldWindow != null && app != null)
                {
                    var oldDataContext = oldWindow.DataContext;

                    var newWindow = new MainWindow();
                    newWindow.DataContext = oldDataContext;

                    
                    newWindow.Show();
                    app.MainWindow = newWindow;
                    oldWindow.Close();

                    Console.WriteLine("[LocalizationManager] Main window recreated with new language");
                }
            });
        }

        public static (string Code, string DisplayName)[] GetSupportedLanguages()
        {
            return new[]
            {
                ("en", "English"),
                ("ru", "Русский"),
                ("pt", "Português")
            };
        }
    }
}
