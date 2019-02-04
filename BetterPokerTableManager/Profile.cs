using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    internal class Profile : List<Slot>
    {
        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        #region Static
        public static Profile GetProfileFromFile(string path = "")
        {
            string activeProfileDir = Path.Combine(Config.DataDir, "Profiles");
            string activeProfilePath = Path.Combine(activeProfileDir, "ActiveProfile.json");

            // Ensure profiles dir exists, regardless of which file is loading
            if (!Directory.Exists(activeProfileDir))
                Directory.CreateDirectory(activeProfileDir);

            // If no path was given load default ActiveProfile or default profile
            if (path == "")
            {
                path = Path.Combine(path, "ActiveProfile.json");
                if (!File.Exists(path)) // Write default file if it doesn't exist
                    File.WriteAllText(path, Properties.Resources.profileDefault1920x1080);
            }

            // A path was given but the file does not exist
            // should be impossible unless user broke something
            if (!File.Exists(path))
            {
                Logger.Log($"Requested profile file does not exist at '{path}' - Loading default file instead", Logger.Status.Error, true);
                return Profile.GetEmptyProfile();
            }

            return Profile.FromJson(File.ReadAllText(path));
        }

        public static Profile FromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<Profile>(json);
            }
            catch
            {
                Logger.Log("Failed to deserialize profile. Corrupt or outdated profile file?", Logger.Status.Error, true);
                return null;
            }
        }

        public static Profile GetEmptyProfile()
        {
            return Profile.FromJson(Properties.Resources.profileEmpty);
        }
        #endregion
    }
}
