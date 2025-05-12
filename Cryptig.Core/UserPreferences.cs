using System;
using System.IO;
using System.Text.Json;

namespace Cryptig.Core
{
    public class UserPreferences
    {
        public string Theme { get; set; } = "light";

        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Cryptig"
        );

        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");

        public static UserPreferences Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load preferences: {ex.Message}");
            }

            return new UserPreferences();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save preferences: {ex.Message}");
            }
        }
    }
}