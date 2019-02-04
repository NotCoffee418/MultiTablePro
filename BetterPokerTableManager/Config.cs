using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BetterPokerTableManager
{
    /// <summary>
    /// Contains user-saved or default info about preferred table positions & related variables
    /// Loads from JSON file.
    /// </summary>
    internal class Config
    {
        public Config()
        {
            ActiveProfile = Profile.GetProfileFromFile(); // default or active
        }

        public Config(Profile profile)
        {
            ActiveProfile = profile;
        }

        [JsonIgnore] // Storing ActiveProfileFileName instead
        public Profile ActiveProfile { get; set; }

        // Default config is defined here
        string _activeProfileFileName = "";



        /// <summary>
        /// Stores the active profile name. Modifying it will update ActiveProfile
        /// </summary>
        public string ActiveProfileFileName
        {
            get
            {
                return _activeProfileFileName;
            }
            set
            {
                _activeProfileFileName = value;

                // Update active profile
                ActiveProfile = Profile.GetProfileFromFile(
                    Path.Combine(Config.DataDir, "Profiles", value)
                    );
            }
        }



        #region Static
        private static string _dataDir = "";

        [JsonIgnore]
        public static string DataDir
        {
            get
            {
                if (_dataDir == "")
                {
                    // \AppData\Local\BetterPokerTableManager
                    _dataDir = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData), "BetterPokerTableManager");
                    if (Debugger.IsAttached) // Debug subdir when debugging
                        _dataDir = Path.Combine(_dataDir, "Debug");
                    if (!Directory.Exists(_dataDir)) // Create directory if it doesn't exist
                        Directory.CreateDirectory(_dataDir);
                }
                return _dataDir;
            }
        }


        /// <summary>
        /// Use when user wants to import a custom config & make it active
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Config FromFile(string path = "")
        {
            if (path == "") // Load default config file if it's not found.
            {
                string defaultPath = Path.Combine(DataDir, "Config.json");
                if (!File.Exists(path)) // Write default file if it doesn't exist (first run)
                    new Config().Save();
            }
            else if (!File.Exists(path))
            {
                Logger.Log("Requested config file does not exist", Logger.Status.Error, true);
                return null;
            }

            return Config.FromJson(File.ReadAllText(path));
        }

        public static Config FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Config>(json);
            }
            catch
            {
                Logger.Log("Failed to deserialize config. Corrupt or outdated config file?", Logger.Status.Error, true);
                return null;
            }
        }
        #endregion



        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void Save()
        {
            if (!Directory.Exists(Config.DataDir)) // Create missing data directory
                Directory.CreateDirectory(Config.DataDir);
            File.WriteAllText(Path.Combine(DataDir, "Config.json"), GetJson());
        }

    }
}
