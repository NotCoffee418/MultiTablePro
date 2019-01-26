using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            /// The PS.com (and likely FT.com) client's log folder DOES NOT have the .COM extension in it's directory name. (tested & confirmed)

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
            bool loading = false;
            string currRead = "";

            // Multiline vars
            List<string> currReadList = new List<string>();
            Queue<string> reAnalysisQueue = new Queue<string>();

            // Reader on locked file
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    while (true)
                    {
                        currRead = sr.ReadLine();
                        // no line found
                        if (currRead == null)
                        {
                            Thread.Sleep(50); // Sleep on no activity
                            continue;
                        }

                        // Don't analyse old log entries. (loading)
                        if (loading)
                        {
                            if (rFindTimestamp.IsMatch(currRead) && DateTime.TryParseExact(currRead, // if line likely contains timestamp, can parse and new
                                    "[yyyy/MM/dd H:mm:ss]", CultureInfo.InvariantCulture, DateTimeStyles.None, out loadFoundTimeStamp)
                                    && loadFoundTimeStamp > startAnalysisTime)
                            {
                                loading = false;
                            }
                            else continue;
                        }

                        // Handle multiline & send in for analysis
                        currReadList.Add(currRead);
                        do // Loop to decide newline read or reanalysis
                        {
                            // Dequeue reanalysis before reading new line from file
                            if (reAnalysisQueue.Count() > 0)
                                currReadList.Add(reAnalysisQueue.Dequeue());

                            // Send in for analysis
                            currReadList = AnalyzeLine(currReadList, ref reAnalysisQueue);

                            // New list if no additional lines are requested
                            if (currReadList == null)
                                currReadList = new List<string>();
                        } while (reAnalysisQueue.Count() > 0); // if queue found something, analyse before reading newline from file
                    }
                }
            }
        }


        // Finds relevant lines
        private static Regex rUselessLines = new Regex(@"^_?Comm+|^->+|^\[+|^\<+|^[+]+|^Thread+|^SaG+|^BASEADDR+");
        private static Regex rFindTimestamp = new Regex(@"\[(.{19})\]");
        private static Regex rTableClose = new Regex(@"table window ([a-fA-F0-9]{8}) has been destroyed");
        private static Regex rUserFolded = new Regex(@"USR ACT button 'Fold' ([a-fA-F0-9]{8})");
        private static Regex rNewHandDealt = new Regex(@"MyPrivateCard 0: c[a-fA-F0-9]+ \[([a-fA-F0-9]+)\]+");
        private static Regex rNewTableFound = new Regex(@"table window ([a-fA-F0-9]{8}) has been created");
        private static Regex rFoldWasCheck = new Regex(@"USR ACT Check/Fold - Check confirmed [a-fA-F0-9]{8}");


        /// <summary>
        /// Analyses line & runs log-based commands
        /// </summary>
        /// <param name="lines">lines to analyse</param>
        /// <returns>null or input if more lines are needed</returns>
        public static List<string> AnalyzeLine(List<string> lines, ref Queue<string> reAnalysisQueue)
        {
            // Quickly skip common useless lines that start with:
            // [,<,+,->, Comm, _Comm and all obvious visible words.
            if (rUselessLines.IsMatch(lines[0]))
                return lines.Count() == 1 ? null : lines;

            // User has folded, make inactive
            // Example: USR ACT button 'Fold' 00300B96
            else if (rUserFolded.IsMatch(lines[0]))
            {
                if (lines.Count() == 1)
                {
                    return lines; // Request another line to confirm fold wasn't check
                }
                else if (!rFoldWasCheck.IsMatch(lines[1])) // Fold was not a check
                {
                    IntPtr wHnd = StrToIntPtr(rUserFolded.Match(lines[0]).Groups[1].Value);
                    Table t = Table.Find(wHnd);
                    if (t != null)
                        t.MakeInactive();
                    else Logger.Log("Attempting to make a table inactive that could not be found (user folded)", Logger.Status.Warning);

                    // Line was irrelevant to fold, requeue for analysis
                    reAnalysisQueue.Enqueue(lines[1]);
                }                
            }

            // Hand is over (new hand dealt), make inactive (if not already)
            // Example: MyPrivateCard 0: c21 [300B96]
            else if (rNewHandDealt.IsMatch(lines[0]))
            {
                Table t = Table.Find(StrToIntPtr(rNewHandDealt.Match(lines[0]).Groups[1].Value));
                if (t != null)
                    t.MakeInactive();
                else Logger.Log("Attempting to make a table inactive that could not be found (hand ended)", Logger.Status.Warning);
            }

            // Report that table has been closed
            // Example: table window 003717F8 has been destroyed
            else if (rTableClose.IsMatch(lines[0]))
            {
                IntPtr wHnd = StrToIntPtr(rTableClose.Match(lines[0]).Groups[1].Value);
                Table t = Table.Find(wHnd);
                if (t != null)
                    t.Close();
                else Logger.Log("Attempting to close a table that could not be found", Logger.Status.Warning);
            }

            // Report that a new table has been found
            // Example: table window 003717F8 has been created { theme: "nova.P7"; size: 105%; }
            else if (rNewTableFound.IsMatch(lines[0]))
            {
                IntPtr wHnd = StrToIntPtr(rNewTableFound.Match(lines[0]).Groups[1].Value);
                new Table(wHnd); // constructor does the rest
            }

            // todo: action required, priority table 

            return null;
        }

        private static IntPtr StrToIntPtr(string input)
        {
            return new IntPtr(Convert.ToInt32(input.Replace("0x",""), 16));
        }
    }
}
