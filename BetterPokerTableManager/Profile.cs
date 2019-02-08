using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    [Serializable]
    internal class Profile : IEquatable<Profile>, ICloneable, INotifyPropertyChanged
    {
        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;
        private string _name = "Unnamed Profile";

        public List<Slot> Slots = new List<Slot>();

        [JsonIgnore]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value.Intersect(System.IO.Path.GetInvalidPathChars()).Any() || value.Length > 32)
                    return; // invalid character - not updating

                _name = value;
                RaisePropertyChanged("Name");
            }
        }

        [JsonIgnore]
        public string FileName { get; set; }


        public string GetJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public void SaveToFile(bool overwrite = false)
        {
            string requestFileName = Name.Replace(" ", "_") + ".json";
            string oldFileName = FileName;

            // Determine filename on new file
            if (FileName == null || FileName == "")
                FileName = requestFileName;
            
            // Handle renaming
            if (overwrite)
            {
                // Renaming: overwrite assumes we're editing.
                // If the file name doesn't match the (new) name, delete the old one & update
                string oldPath = Path.Combine(Config.DataDir, "Profiles", oldFileName);
                if (File.Exists(oldPath))
                    File.Delete(oldPath);
            }

            // Regardless of overwrite, if filename changed, we don't want to overwrite profiles with the same name
            if (File.Exists(Path.Combine(Config.DataDir, "Profiles", requestFileName)))
            {
                // Determine new file name
                Regex previousHasDuplicate = new Regex(@"_(\d+)\.json");
                if (!previousHasDuplicate.IsMatch(requestFileName))
                    requestFileName = requestFileName.Replace(".json", "_2.json");

                // Loop until we reach an available number
                int lastDuplicate = int.Parse(previousHasDuplicate.Match(requestFileName).Groups[1].Value);
                while (File.Exists(Path.Combine(Config.DataDir, "Profiles", requestFileName)))
                {
                    requestFileName = requestFileName.Replace(lastDuplicate + ".json", (lastDuplicate + 1) + ".json");
                    lastDuplicate++;
                }
            }

            // Update FileName & Name
            Name = Path.GetFileNameWithoutExtension(requestFileName).Replace("_", " ");
            FileName = requestFileName;

            // Determine path & save
            string path = Path.Combine(Config.DataDir, "Profiles", FileName);
            File.WriteAllText(path, GetJson());
        }

        public override string ToString()
        {
            return Name;
        }
        
        public bool Equals(Profile other)
        {
            return Name == other.Name && Slots.SequenceEqual(other.Slots);
        }

        public static bool operator ==(Profile profile1, Profile profile2)
        {
            return EqualityComparer<Profile>.Default.Equals(profile1, profile2);
        }

        public static bool operator !=(Profile profile1, Profile profile2)
        {
            return !(profile1 == profile2);
        }

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public object Clone()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                if (this.GetType().IsSerializable)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, this);
                    stream.Position = 0;
                    return formatter.Deserialize(stream);
                }
                return null;
            }
        }


        #region Static
        public static Profile GetProfileFromFile(string path = "")
        {
            string activeProfileDir = Path.Combine(Config.DataDir, "Profiles");
            string starterProfilePath = Path.Combine(activeProfileDir, Properties.Settings.Default.DefaultProfileFileName);

            // Ensure profiles dir exists, regardless of which file is loading
            if (!Directory.Exists(activeProfileDir))
                Directory.CreateDirectory(activeProfileDir);

            // If no path was given load default ActiveProfile or default profile
            if (path == "" || path == starterProfilePath)
            {
                if (!File.Exists(starterProfilePath)) // Write default file if it doesn't exist
                    File.WriteAllText(starterProfilePath, Properties.Resources.profileDefault1920x1080);
                path = starterProfilePath;
            }

            // A path was given but the file does not exist
            // should be impossible unless user broke something
            if (!File.Exists(path))
            {
                Logger.Log($"Requested profile file does not exist at '{path}' - Loading default file instead", Logger.Status.Error, true);
                return Profile.GetEmptyProfile();
            }

            Profile p = Profile.FromJson(File.ReadAllText(path));
            p.FileName = Path.GetFileName(path);
            p.Name = Path.GetFileNameWithoutExtension(path.Replace("_", " "));
            return p;
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
            Profile p = Profile.FromJson(Properties.Resources.profileEmpty);
            p.Name = "New Profile";
            return p;
        }

        internal static List<Profile> GetAllProfiles()
        {
            // Create profile dir if it doesn't exist
            string profileDir = Path.Combine(Config.DataDir, "Profiles");
            if (!Directory.Exists(profileDir))
                Directory.CreateDirectory(profileDir);

            // Get all valid profiles
            List<Profile> profileList = new List<Profile>();
            foreach (string file in Directory.GetFiles(profileDir, "*.json"))
                profileList.Add(GetProfileFromFile(file));

            return profileList;
        }

        public override int GetHashCode()
        {
            var hashCode = -947667292;
            hashCode = hashCode * -1521134295 + EqualityComparer<List<Slot>>.Default.GetHashCode(Slots);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FileName);
            return hashCode;
        }

        #endregion
    }
}
