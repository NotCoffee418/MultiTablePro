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
        public List<Slot> Slots = new List<Slot>();

        #region Static
        public static Config FromFile(string path = "")
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
                    File.WriteAllText(path, Properties.Resources.configDefault1920x1080);
            }
            else if (!File.Exists(path))
            {
                Logger.Log("Requested config file does not exist", Logger.Status.Error, true);
                return null;
            }

            return FromJson(File.ReadAllText(path));
        }

        public static Config GetEmpty()
        {
            return FromJson(Properties.Resources.configEmpty);
        }

        public static Config FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Config>(json);
            }
            catch
            {
                Logger.Log("Failed to deserialize config.", Logger.Status.Error, true);
                return null;
            }
        }
        #endregion

        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

    }
}
