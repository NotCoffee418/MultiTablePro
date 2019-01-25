using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    class PSLogHandler
    {
        public static void Start()
        {
            // Look for new files every 10 seconds since stars client can switch from log.0 to log.1 in the middle of a session.
            Timer timer = new Timer(WatchNewLogFiles, null, 0, 10000);
        }

        // Lists files that are currently being watched
        private static List<string> activeLogFiles = new List<string>();


        /// <summary>
        /// Starts a watcher threads for each PS log file (should be only PokerStars.log.0 and sometimes PokerStars.log.1)
        /// </summary>
        private static void WatchNewLogFiles(object state)
        {
            /// For future reference: FullTilt's log files are AppData\Local\FullTilt.xx\FullTilt.log.x - it's window titles work the same way
            /// The PS .com client's log folder DOES NOT have the .COM extension in it's directory name. (tested & confirmed)

            // Find PS folder for user's client
            string appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var PSDirs = Directory.GetDirectories(appDataLocal, "PokerStars*");
            var FTDirs = Directory.GetDirectories(appDataLocal, "FullTilt*");

            // Fatal error if no log files were found
            if (PSDirs.Count() == 0 && FTDirs.Count() == 0)
                Logger.Log("Could not find PokerStars log file. Please run PokerStars before trying to run BPTM.", Logger.Status.Fatal);
            else // Start watching any logfiles that weren't registered yet
            {
                // PokerStars logs
                foreach (var dir in PSDirs) // foreach in case of multiple locales
                {
                    string logFileBase = dir + "\\PokerStars.log.";
                    if (File.Exists(logFileBase + 0) && !activeLogFiles.Contains(logFileBase + 0))
                        new Thread(() => WatchLog(logFileBase + 0)).Start();
                    if (File.Exists(logFileBase + 1) && !activeLogFiles.Contains(logFileBase + 1))
                        new Thread(() => WatchLog(logFileBase + 1)).Start();
                }

                // Fulltilt logs
                foreach (var dir in FTDirs) // foreach in case of multiple locales
                {
                    string logFileBase = dir + "\\FullTilt.log.";
                    if (File.Exists(logFileBase + 0) && !activeLogFiles.Contains(logFileBase + 0))
                        new Thread(() => WatchLog(logFileBase + 0)).Start();
                    if (File.Exists(logFileBase + 1) && !activeLogFiles.Contains(logFileBase + 1))
                        new Thread(() => WatchLog(logFileBase + 1)).Start();
                }
            }
        }

        private static void WatchLog(string path)
        {
            // Register log file
            activeLogFiles.Add(path);

            // Loading vars
            DateTime startAnalysisTime = DateTime.Now;
            DateTime loadFoundTimeStamp = DateTime.MinValue;
            bool loading = true;
            string currRead = "";

            // Reader on locked file
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    while (true)
                    {
                        currRead = sr.ReadLine();
                        if (currRead != null) // new line found
                        {
                            if (loading) // Don't analyse old log entries.
                            {
                                if (currRead[0] == '[' && DateTime.TryParseExact(currRead, // if line likely contains timestamp, can parse and new
                                     "[yyyy/MM/dd H:mm:ss]", CultureInfo.InvariantCulture, DateTimeStyles.None, out loadFoundTimeStamp)
                                      && loadFoundTimeStamp > startAnalysisTime)
                                {
                                    loading = false;
                                }
                                else continue;
                            }

                            // Send in for analysis
                            Console.WriteLine(currRead);
                        }
                        else Thread.Sleep(50); // Sleep on no activity
                    }
                }
            }
        }
    }
}
