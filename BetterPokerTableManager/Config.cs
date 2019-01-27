using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    /// <summary>
    /// Contains user-saved or default info about preferred table positions & related variables
    /// Loads from JSON file.
    /// </summary>
    class Config
    {
        public Config(string json)
        {

        }

        #region Static
        public static Config LoadConfig(string path = "")
        {
            if (path == "") // Load default config file if it's not found.
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterPokerTableManager");
                if (Debugger.IsAttached) // Seperate directory for debugger
                    path = Path.Combine(path, "Debug", "Config");
                else path = Path.Combine(path, "Config");
                if (!Directory.Exists(path)) // Create missing data directory
                    Directory.CreateDirectory(path);
                path = Path.Combine(path, "config.json");
                if (!File.Exists(path)) // Write default file if it doesn't exist
                    File.WriteAllText(path, Properties.Resources.default_config);
            }

            return new Config(File.ReadAllText(path));
        }

        public static Config CreateNew(Config basedOn = null)
        {
            throw new NotImplementedException();
        }

        public string GetJson(Config config)
        {
            throw new NotImplementedException();
        }
        #endregion


    }
}
