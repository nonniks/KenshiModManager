using System;
using System.IO;
using System.Text.Json;

namespace KenshiModManager.Core
{
    /// <summary>
    /// Application settings manager
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KenshiModManager",
            "settings.json"
        );

        public string? LastSelectedPlaysetName { get; set; }
        public string? CustomKenshiPath { get; set; }
        public string? CustomModsPath { get; set; }
        public string? CustomWorkshopPath { get; set; }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Console.WriteLine($"[AppSettings] Loaded settings: LastPlayset={settings.LastSelectedPlaysetName}");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppSettings] Error loading settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(SettingsPath, json);
                Console.WriteLine($"[AppSettings] Saved settings: LastPlayset={LastSelectedPlaysetName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppSettings] Error saving settings: {ex.Message}");
            }
        }
    }
}
