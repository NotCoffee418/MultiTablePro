using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BetterPokerTableManager
{
    /// <summary>
    /// Contains user-saved or default info about preferred table positions & related variables
    /// Loads from JSON file.
    /// </summary>
    internal class Config : INotifyPropertyChanged
    {
        internal Config()
        {
            new Timer(StartSaving, null, 1000, 0);
            PropertyChanged += Config_PropertyChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Default config is defined here
        private bool savingAllowed = false;
        Profile _activeProfile = null;
        string _activeProfileFileName = Properties.Settings.Default.DefaultProfileFileName;
        private bool _forceTablePosition = true;
        private bool _autoStart = true;
        private bool _autoMinimize = false;
        private int _autoLeaveEnabled;
        private int _autoLeaveVpip = 15;
        private int _autoLeaveHands = 20;
        private bool _preferSpreadOverStack = true;


        [JsonIgnore] // Storing ActiveProfileFileName to file instead
        public Profile ActiveProfile {
            get {
                if (_activeProfile == null)
                    _activeProfile = Profile.GetProfileFromFile(Path.Combine(Config.DataDir, "Profiles", _activeProfileFileName));
                return _activeProfile;
            }
            set
            {
                _activeProfile = value;
                ActiveProfileFileName = value.FileName;
                RaisePropertyChanged("ActiveProfile");
            }
        }

        /// <summary>
        /// Stores the active profile name. Modifying it will update ActiveProfile
        /// </summary>
        public string ActiveProfileFileName
        {
            get
            {
                // Ensure default profile exists & load it if no profile is set
                // This will occur when first running the application and (unfortunately) JsonDeserialize also calls get
                if (ActiveProfile == null && _activeProfileFileName == Properties.Settings.Default.DefaultProfileFileName)
                {
                    ActiveProfile = Profile.GetProfileFromFile(
                    Path.Combine(Config.DataDir, "Profiles", _activeProfileFileName)
                    );
                }
                return _activeProfileFileName;
            }
            set
            {
                _activeProfileFileName = value;
                RaisePropertyChanged("ActiveProfileFileName");
            }
        }

        public bool ForceTablePosition
        {
            get { return _forceTablePosition; }
            set {
                _forceTablePosition = value;
                RaisePropertyChanged("ForceTablePosition");
            }
        }
        public bool AutoStart
        {
            get { return _autoStart; }
            set {
                _autoStart = value;
                RaisePropertyChanged("AutoStart");
            }
        }
        public bool AutoMinimize
        {
            get { return _autoMinimize; }
            set {
                _autoMinimize = value;
                RaisePropertyChanged("AutoMinimize");
            }
        }
        public int AutoLeaveEnabled
        {
            get { return _autoLeaveEnabled; }
            set
            {
                _autoLeaveEnabled = value;
                RaisePropertyChanged("AutoLeaveEnabled");
            }
        }
        public int AutoLeaveVpip
        {
            get { return _autoLeaveVpip; }
            set {
                _autoLeaveVpip = value;
                RaisePropertyChanged("AutoLeaveVpip");
            }
        }
        public int AutoLeaveHands
        {
            get { return _autoLeaveHands; }
            set {
                _autoLeaveHands = value;
                RaisePropertyChanged("AutoLeaveHands");
            }
        }
        public bool PreferSpreadOverStack
        {
            get { return _preferSpreadOverStack; }
            set
            {
                _preferSpreadOverStack = value;
                RaisePropertyChanged("PreferStackOverSpread");
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
                path = Path.Combine(DataDir, "Config.json");
                if (!File.Exists(path)) // Write default file if it doesn't exist (first run)
                    new Config().Save();
            }
            else if (!File.Exists(path))
            {
                Logger.Log("Requested config file does not exist", Logger.Status.Error, true);
                return new Config();
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

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        // Prevents connfig from saving while deserializing
        // todo: find a better way
        private void StartSaving(object state)
        {
            savingAllowed = true;
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // A propery has changed, save
            if (savingAllowed)
                Save();
        }

    }
}
