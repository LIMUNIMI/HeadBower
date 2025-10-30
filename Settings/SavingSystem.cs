using System.Text.Json;
using System.IO;

namespace HeadBower.Settings
{
    public class SavingSystem
    {
        public const string DEFAULTFILENAME = "Settings";
        private readonly JsonSerializerOptions _jsonOptions;

        public SavingSystem(string settingsFilename = DEFAULTFILENAME)
        {
            SettingsFilename = settingsFilename;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        private string SettingsFilename { get; set; }

        /// <summary>
        /// Saves settings to a JSON file.
        /// </summary>
        /// <param name="settings">The settings object to save.</param>
        /// <returns>0 if successful, 1 if an error occurred.</returns>
        public int SaveSettings(UserSettings settings)
        {
            try
            {
                string filePath = SettingsFilename + ".json";
                string json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(filePath, json);
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        /// <summary>
        /// Loads settings from a JSON file.
        /// </summary>
        /// <returns>The loaded settings, or default settings if the file doesn't exist or an error occurs.</returns>
        public UserSettings LoadSettings()
        {
            try
            {
                string filePath = SettingsFilename + ".json";
                
                if (!File.Exists(filePath))
                {
                    return new DefaultSettings();
                }

                string json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<UserSettings>(json, _jsonOptions);
                
                return settings ?? new DefaultSettings();
            }
            catch
            {
                return new DefaultSettings();
            }
        }
    }
}