using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    class Logger
    {
        public enum Status
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3
        }

        private static bool debugging = false;
        public static string SessionLog { get; private set; }

        public static void Log(string message, Status status = Status.Info)
        {
            if (debugging || (int)status >= Properties.Settings.Default.LogLevel)
            {
                string entry = String.Format("[{0}][{1}] {2}", DateTime.Now.ToString("yyyy-MM-dd H:mm:ss"), nameof(status), message);

                // add to sessionlog
                if (SessionLog != "")
                    SessionLog += Environment.NewLine;
                SessionLog += entry;

                // add to log file
                using (StreamWriter sw = File.AppendText("error.log"))
                {
                    //sw.WriteLine(entry);
                }
            }
        }
    }
}
