using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BetterPokerTableManager
{
    class Logger
    {
        static Logger()
        {
            new Thread(() => StartLogWriter()).Start();
        }

        public enum Status
        {
            Info = 0,
            Warning = 1,
            Error = 2,
            Fatal = 3
        }
        static string[] statusNames = Enum.GetNames(typeof(Status));
        static Queue<string> writeQueue = new Queue<string>();

        public static void Log(string message, Status status = Status.Info)
        {
            if (Debugger.IsAttached || (int)status >= Properties.Settings.Default.LogLevel)
            {
                string entry = $"[{DateTime.Now.ToString("yyyy-MM-dd H:mm:ss")}][{statusNames[(int)status]}] {message}";
                writeQueue.Enqueue(entry);
                Debug.WriteLine(entry);
            }
            
            // Kill app on fatal errors (this is a bit dodgy but it'll do for now.)
            if (status == Status.Fatal)
            {
                Thread.Sleep(50); // give logwriter time to write
                App.Current.Properties["IsRunning"] = false; // Stop all loops
                MessageBox.Show(message, "Fatal error - closing application", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private static void StartLogWriter()
        {
            while ((bool)App.Current.Properties["IsRunning"])
            {
                if (writeQueue.Count == 0)
                    Thread.Sleep(25);
                else
                {
                    List<string> newLines = new List<string>();
                    while (writeQueue.Count > 0)
                        newLines.Add(writeQueue.Dequeue());
                    File.AppendAllLines("output.log", newLines);
                }
            }
        }
    }
}
