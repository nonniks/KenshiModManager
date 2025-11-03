using System;
using System.IO;
using System.Text.Json;

namespace KenshiModManager.Core
{
    /// <summary>
    /// Application settings manager with schema versioning and migration support
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Current settings schema version. Increment when making breaking changes to settings structure.
        /// Version history:
        /// - 1: Initial schema (1.0.0 - 1.1.0)
        /// </summary>
        private const int CurrentSchemaVersion = 1;

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KenshiModManager",
            "settings.json"
        );

        /// <summary>
        /// Schema version for migration detection. DO NOT modify this manually.
        /// </summary>
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;

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
                        Console.WriteLine($"[AppSettings] - Schema version: {settings.SchemaVersion}");
                        Console.WriteLine($"[AppSettings] - CustomKenshiPath: {settings.CustomKenshiPath ?? "(not set)"}");
                        Console.WriteLine($"[AppSettings] - CustomModsPath: {settings.CustomModsPath ?? "(not set)"}");
                        Console.WriteLine($"[AppSettings] - CustomWorkshopPath: {settings.CustomWorkshopPath ?? "(not set)"}");
                        Console.WriteLine($"[AppSettings] - Language: {settings.Language ?? "(not set)"}");

                        // Perform migration if needed
                        if (settings.SchemaVersion < CurrentSchemaVersion)
                        {
                            Console.WriteLine($"[AppSettings] Migrating settings from schema v{settings.SchemaVersion} to v{CurrentSchemaVersion}");
                            bool migrationSuccess = MigrateSettings(settings);

                            if (migrationSuccess)
                            {
                                settings.SchemaVersion = CurrentSchemaVersion;
                                settings.Save();
                                Console.WriteLine("[AppSettings] Migration completed successfully");
                            }
                            else
                            {
                                Console.WriteLine("[AppSettings] Migration failed, some settings may have been reset");
                            }
                        }

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

                // Create backup of existing settings before overwriting
                if (File.Exists(SettingsPath))
                {
                    try
                    {
                        var backupPath = $"{SettingsPath}.bak";
                        File.Copy(SettingsPath, backupPath, overwrite: true);
                        Console.WriteLine($"[AppSettings] Created backup at {backupPath}");
                    }
                    catch (Exception backupEx)
                    {
                        Console.WriteLine($"[AppSettings] Warning: Could not create backup: {backupEx.Message}");
                    }
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

        /// <summary>
        /// Migrates settings from older schema versions to current schema.
        /// Add migration logic here when schema version is incremented.
        /// </summary>
        /// <param name="settings">Settings object to migrate</param>
        /// <returns>True if migration succeeded, false otherwise</returns>
        private static bool MigrateSettings(AppSettings settings)
        {
            try
            {
                int originalVersion = settings.SchemaVersion;

                // Migration chain: apply all intermediate migrations
                // Example for future migrations:
                // if (originalVersion < 2) { MigrateV1ToV2(settings); }
                // if (originalVersion < 3) { MigrateV2ToV3(settings); }

                // Currently at schema v1 - no migrations needed yet
                // This method is a template for future breaking changes

                Console.WriteLine($"[AppSettings] Migration from v{originalVersion} completed");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppSettings] Migration error: {ex.Message}");
                return false;
            }
        }

        // Example migration method template (for future use):
        // private static void MigrateV1ToV2(AppSettings settings)
        // {
        //     Console.WriteLine("[AppSettings] Applying migration: v1 -> v2");
        //     // Example: settings.NewField = ConvertOldField(settings.OldField);
        //     // Example: settings.RenamedField = settings.DeprecatedField;
        // }

        /// <summary>
        /// Returns the path where settings are stored.
        /// Useful for user support and debugging.
        /// </summary>
        public static string GetSettingsPath() => SettingsPath;

        /// <summary>
        /// Checks if settings file needs migration without loading it.
        /// Returns null if file doesn't exist or can't be read.
        /// </summary>
        public static int? GetStoredSchemaVersion()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                    return null;

                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                return settings?.SchemaVersion;
            }
            catch
            {
                return null;
            }
        }
    }
}
