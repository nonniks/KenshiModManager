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
        public string? Language { get; set; }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        Console.WriteLine("[AppSettings] Settings file is empty, creating default settings");
                        var defaultSettings = new AppSettings();
                        defaultSettings.Save();
                        return defaultSettings;
                    }

                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Console.WriteLine($"[AppSettings] Loaded settings from {SettingsPath}");
                        Console.WriteLine($"[AppSettings] - CustomKenshiPath: {settings.CustomKenshiPath ?? "(not set)"}");
                        Console.WriteLine($"[AppSettings] - CustomModsPath: {settings.CustomModsPath ?? "(not set)"}");
                        Console.WriteLine($"[AppSettings] - CustomWorkshopPath: {settings.CustomWorkshopPath ?? "(not set)"}");
                        Console.WriteLine($"[AppSettings] - Language: {settings.Language ?? "(not set)"}");
                        return settings;
                    }
                }
                else
                {
                    Console.WriteLine($"[AppSettings] Settings file not found at {SettingsPath}, creating default settings");
                    var defaultSettings = new AppSettings();
                    defaultSettings.Save();
                    return defaultSettings;
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[AppSettings] Corrupted settings file detected: {jsonEx.Message}");
                Console.WriteLine("[AppSettings] Backing up corrupted file and creating new settings");

                try
                {
                    if (File.Exists(SettingsPath))
                    {
                        var backupPath = $"{SettingsPath}.corrupted.{DateTime.Now:yyyyMMddHHmmss}";
                        File.Copy(SettingsPath, backupPath, true);
                        Console.WriteLine($"[AppSettings] Corrupted file backed up to: {backupPath}");
                    }
                }
                catch (Exception backupEx)
                {
                    Console.WriteLine($"[AppSettings] Could not backup corrupted file: {backupEx.Message}");
                }

                var defaultSettings = new AppSettings();
                defaultSettings.Save();
                return defaultSettings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppSettings] Unexpected error loading settings: {ex.Message}");
            }

            Console.WriteLine("[AppSettings] Returning default settings");
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

                // Atomic write: write to temp file, then move
                var tempPath = SettingsPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, SettingsPath, overwrite: true);

                Console.WriteLine($"[AppSettings] Saved settings to {SettingsPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppSettings] Error saving settings: {ex.Message}");
            }
        }
    }
}
