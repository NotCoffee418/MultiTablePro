using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTablePro
{
    class BwinHandler
    {
        private static bool? IsRunning { get; set; }
        private static Timer timer = null;


        public static void Start()
        {
            if (IsRunning == null) // First call to start
            {
                IsRunning = true;
                timer = new Timer(FindNewTables, null, 0, 1000);
            }
            else if (IsRunning == false) // Restart
            {
                // Restarts watch on all known log files
                IsRunning = true;
            }
            else
            {
                IsRunning = false;
            }
        }

        public static void Stop()
        {
            // Loop theads will stop, effectively ending the watch
            IsRunning = false;
        }
        
        private static void FindNewTables(object state)
        {
            // Kill if bwin is not running
            Process[] p = Process.GetProcessesByName("bwinbe"); // todo: broaden this with PP and bwin wildcard searches
            if (p.Count() == 0)
                return;

            // Find all poker tables (bwin table class is #32770 - window title contains $ (other applications also use this class name)
            var tableHandles = WHelper.EnumAllWindows(IntPtr.Zero, "#32770").Where(hWnd => WHelper.GetWindowTitle(hWnd).Contains("$"));
            lock (Table.KnownTables)
            {
                // Register any new tables & start watching them
                foreach (var handle in tableHandles.Where(h => Table.Find(h, false) == null))
                {
                    // Verify that it is indeed a table window (tourney lobbeys will also be found
                    // Check for random element that only exists in tables
                    if (WHelper.EnumAllWindows(handle, "Static").Where(hWnd => WHelper.GetWindowTitle(hWnd).Contains("Fold to any bet")).Count() > 0)
                    {
                        // Register the table
                        string winTitle = WHelper.GetWindowTitle(handle);
                        Logger.Log($"Registering new bwin table: {handle} - " + winTitle);
                        Table table = new Table(handle);
                        new Thread(() => WatchTable(table)).Start();
                    }
                }
            }            
        }
        
        private static void WatchTable(Table table)
        {
            bool tableIsClosed = false;
            IntPtr foldHandle = IntPtr.Zero;
            //IntPtr checkCallHandle = IntPtr.Zero;
            //IntPtr betRaiseHandle = IntPtr.Zero;

            // Wait for buttons to initially appear & define their handles
            while (foldHandle == IntPtr.Zero && (bool)IsRunning && (bool)App.Current.Properties["IsRunning"])
            {
                // Check if window was destroyed
                if (!WHelper.IsWindow(table.WindowHandle))
                {
                    table.Close();
                    tableIsClosed = true;
                    break;
                }

                // List table elements
                var afxWnd90uElements = WHelper.EnumAllWindows(table.WindowHandle, "AfxWnd90u");
                var afxWnd90uWinTitles = WHelper.GetAllWindowTitles(afxWnd90uElements);

                // Try to find button handles
                if (afxWnd90uWinTitles.Where(x => x.Value == "Fold ").Count() > 0) {
                    foldHandle = afxWnd90uWinTitles.Where(x => x.Value == "Fold ").FirstOrDefault().Key;
                    //checkCallHandle = afxWnd90uWinTitles.Where(x => x.Value == "Check" || x.Value.Contains("Call")).FirstOrDefault().Key;
                    //betRaiseHandle = afxWnd90uWinTitles.Where(x => x.Value.Contains("Bet") || x.Value.Contains("Raise")).FirstOrDefault().Key;
                    Logger.Log($"Found buttons for {table.WindowHandle} - F:{foldHandle}");// C:{checkCallHandle} B:{betRaiseHandle}");
                    Table.SetPriority(table.WindowHandle, Table.Status.ActionRequired); // Call action required for the first time                    
                }
                else
                {
                    // Wait & try again
                    Thread.Sleep(500);
                }
            }

            // Watch for action changes
            bool lastVisibleState = false;
            if (!tableIsClosed)
                while ((bool)IsRunning && (bool)App.Current.Properties["IsRunning"])
                {
                    // Change priority if shown status changed
                    bool newVisibleState = WHelper.IsWindowVisible(foldHandle);
                    if (newVisibleState != lastVisibleState)
                    {
                        // Check if action required or fold button turned into "I am back" button
                        if (lastVisibleState && !WHelper.GetWindowTitle(foldHandle).Contains("Fold"))
                        {
                            table.Priority = Table.Status.OpenButNotJoined; // Sitting out
                        }
                        else
                        {
                            table.Priority = newVisibleState ?
                            Table.Status.ActionRequired : Table.Status.NoActionRequired;
                        }
                    
                        // Set last visible state to current one
                        lastVisibleState = newVisibleState;
                    }                

                    // Check if window was destroyed
                    if (!lastVisibleState && !WHelper.IsWindow(foldHandle))
                    {
                        table.Close();
                        break;
                    }

                    // Sleep
                    Thread.Sleep(100);
                }            
            }

      
    }
}
