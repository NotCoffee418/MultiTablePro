using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using MultiTablePro.Data;
using MultiTablePro.UI;

namespace MultiTablePro
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Dictionary<string, string> startupArgs = CleanStartupArgs(e.Args);


            // Cancel if we're already running and ignorealreadyrunning is not a startup arg
            if (Process.GetProcessesByName("MultiTablePro").Count() > 1 && !startupArgs.ContainsKey("ignorealreadyrunning"))
            {
                Logger.Log("BPTM is already running. Try again in a few seconds if you just closed it.", Logger.Status.Warning, true);
                Application.Current.Shutdown();
            }

            // Start the updater
            Updater.Run();

            // Notify application started
            App.Current.Properties["IsRunning"] = true;
            Logger.Log($"--- MultiTable Pro v{Assembly.GetExecutingAssembly().GetName().Version.ToString()} Started ---");

            // Load config & install on first run
            // Config needs to be initialized before the license check
            Config.Active = Config.FromFile();

            // Warn user when debug logging is enabled - since it should only be enabled when collecting bug data
            if (Config.Active.EnableDetailedLogging)
                Logger.Log("Detailed logging is enabled. If you were not asked to enable this by support, please disable it under Config > Advanced Settings.",
                    Logger.Status.Warning, showMessageBox: true);

            // Validate license & either request license input or start
            License lic = License.GetKnownLicense();
            if (lic.Validate() && !lic.IsTrial)
                StartApplication(lic);
            else
            {
                LicenseInputWindow win = new LicenseInputWindow();
                win.LastLicenseCheck = lic;
                win.Show();
            }            
        }

        // Start the application after license is approved
        internal static void StartApplication(License lic)
        {
            Config.Active.ActiveLicense = lic;
            MainWindow win = new MainWindow();
            win.Show();
        }

        /// <summary>
        /// Allows for -paramKey -paramKey2 paramVal2 format
        /// Since it just splits by space by default.
        /// </summary>
        /// <returns>Dictionary - Key (without -) and Value ("true" when none given)</returns>
        private Dictionary<string, string> CleanStartupArgs(string[] args)
        {
            string lastKey = "";
            var result = new Dictionary<string, string>();

            // Loops each "argument"
            foreach (string part in args)
            {
                if (part[0] == '-') // keys start with -
                {
                    // Create key
                    string keyName = part.Remove(0, 1);
                    result.Add(keyName, "true");
                    lastKey = keyName;
                }
                else // values start without -
                {
                    // Set value to lastKey if lastkey is defined
                    // implied: you can't do these: "-key value value" or "value" or "keywithout-"
                    if (lastKey != "" && result.ContainsKey(lastKey))
                        result[lastKey] = part;

                    // clear lastKey (
                    lastKey = "";
                }
            }

            return result;
        }
    }
}
