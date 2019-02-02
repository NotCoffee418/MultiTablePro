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
            // Set log level
            LogLevel = (Status)Properties.Settings.Default.LogLevel;

            // Determine log file location
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BetterPokerTableManager");
            if (Debugger.IsAttached) // Seperate directory for debugger
                path = Path.Combine(path, "Debug");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            LogFilePath = Path.Combine(path, "output.log");

            // Start log writer
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
        static MessageBoxImage[] statusIcons = new MessageBoxImage[]
        {
            MessageBoxImage.Information,
            MessageBoxImage.Warning,
            MessageBoxImage.Error,
            MessageBoxImage.Stop,
        };

        static Queue<string> writeQueue = new Queue<string>();
        static string LogFilePath { get; set; }
        static Status LogLevel { get; set; }

        public static void Log(string message, Status status = Status.Info, bool showMessageBox = false)
        {
            if (Debugger.IsAttached || status >= LogLevel)
            {
                string entry = $"[{DateTime.Now.ToString("yyyy-MM-dd H:mm:ss.fff")}][{statusNames[(int)status]}] {message}";
                writeQueue.Enqueue(entry);
                Debug.WriteLine(entry);
            }
            
            // Kill app on fatal errors (this is a bit dodgy but it'll do for now.)
            if (status == Status.Fatal)
            {
                Thread.Sleep(50); // give logwriter time to write
                App.Current.Properties["IsRunning"] = false; // Stop all loops
                MessageBox.Show(message, "Fatal error - closing application", MessageBoxButton.OK, MessageBoxImage.Stop);
                Application.Current.Shutdown();
            }
            else if (showMessageBox)
                MessageBox.Show(message, statusNames[(int)status], MessageBoxButton.OK, statusIcons[(int)status]);
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
                    File.AppendAllLines(LogFilePath, newLines);
                }
            }
        }
    }
}
