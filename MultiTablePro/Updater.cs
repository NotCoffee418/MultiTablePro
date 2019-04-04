using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using AutoUpdaterDotNET;
using Newtonsoft.Json;

namespace MultiTablePro
{
    class Updater
    {
        internal static void Run(bool force = false)
        {
            try
            {
                AutoUpdater.ParseUpdateInfoEvent += AutoUpdaterOnParseUpdateInfoEvent;                
                AutoUpdater.Start("https://multitablepro.com/api/get_latest_version/multitable-pro/internal", Assembly.GetExecutingAssembly());
                if (force)
                    AutoUpdater.ReportErrors = true;                    
            }
            catch (Exception ex)
            {
                Logger.Log("Updater error: " + ex.Message);
            }

        }

        private static void AutoUpdaterOnParseUpdateInfoEvent(ParseUpdateInfoEventArgs args)
        {
            dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
            
            // Handle errors
            if (json.errors.Count > 0)
            {
                foreach (string error in json.errors)
                    Logger.Log($"Updater Error: {error}", Logger.Status.Error);
                return;
            }
            else if (json.result.requested_version == null)
            {
                Logger.Log("Updater Error: No versions were found on this branch.", Logger.Status.Error);
                return;
            }

            // Set update info
            string currentVersionString = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                Mandatory = false,
                CurrentVersion = json.result.requested_version.version,
                ChangelogURL = "https://multitablepro.com/api/get_changelog_html/multitable-pro/" +
                    $"{json.result.requested_version.branch}/{currentVersionString}",                
                DownloadURL = "https://multitablepro.com/download/multitable-pro/" + 
                    $"{json.result.requested_version.branch}/{json.result.requested_version.version}/update"
            };
        }
    }
}
