using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace MultiTablePro
{
    internal class TableManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);


        public TableManager()
        {
            Config.Active.PropertyChanged += ActiveConfig_PropertyChanged;
        }

        #region ShowWindowCommands (todo: can't we import this or something?)
        internal enum ShowWindowCommands
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            Hide = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position.
            /// An application should specify this flag when displaying the window 
            /// for the first time.
            /// </summary>
            Normal = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
                          /// <summary>
                          /// Activates the window and displays it as a maximized window.
                          /// </summary>       
            ShowMaximized = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value 
            /// is similar to <see cref="Win32.ShowWindowCommand.Normal"/>, except 
            /// the window is not activated.
            /// </summary>
            ShowNoActivate = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position. 
            /// </summary>
            Show = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level 
            /// window in the Z order.
            /// </summary>
            Minimize = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to
            /// <see cref="Win32.ShowWindowCommand.ShowMinimized"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowMinNoActive = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is 
            /// similar to <see cref="Win32.ShowWindowCommand.Show"/>, except the 
            /// window is not activated.
            /// </summary>
            ShowNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or 
            /// maximized, the system restores it to its original size and position. 
            /// An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,
            /// <summary>
            /// Sets the show state based on the SW_* value specified in the 
            /// STARTUPINFO structure passed to the CreateProcess function by the 
            /// program that started the application.
            /// </summary>
            ShowDefault = 10,
            /// <summary>
            ///  <b>Windows 2000/XP:</b> Minimizes a window, even if the thread 
            /// that owns the window is not responding. This flag should only be 
            /// used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }
        #endregion

        // Used to move tables after other tables. (stealing an active slot)
        Queue<Tuple<Table, Slot, Slot>> delayedMoveQueue = new Queue<Tuple<Table, Slot, Slot>>();

        /// <summary>
        /// Starts the Window Manager
        /// </summary>
        public void Start()
        {
            lock (Table.ActionQueue) // Empty queue for new run
            {
                Table.ActionQueue = new ConcurrentQueue<Table>();
            }
            IsRunning = true;
            new Thread(() => ManageTables()).Start();
            if (Config.Active.ForceTablePosition)
                forceTablePositionTimer = new Timer(ForceTablePosition, null, 0, 500);

            // Start handlers
            PSLogHandler.Start();
            BwinHandler.Start();
        }

        /// <summary>
        /// Stops the window manager
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            if (forceTablePositionTimer != null)
            {
                forceTablePositionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                forceTablePositionTimer.Dispose();
            }
            PSLogHandler.Stop();
        }

        // private property's vars
        private Timer forceTablePositionTimer;

        // Properties
        public bool IsRunning { get; set; }

        private void InitialTablePlacement()
        {
            lock (Table.KnownTables)
            {
                foreach (Table table in Table.KnownTables)
                    TryMoveTable(table); // Move default activityuse
            }
        }


        /// <summary>
        /// The heart of the program
        /// Returns the best available slot based on the input requirements
        /// </summary>
        /// <param name="activity">The slot type</param>
        /// <param name="status">Used to push aside low priority tables if needed</param>
        /// <returns></returns>
        private Slot GetAvailableSlot(Slot.ActivityUses slotType, Table table)
        {
            Slot resultSlot = null;

            // Find all possible slots & order them
            var possibleSlots = Config.Active.ActiveProfile.Slots
                .Where(s => s.ActivityUse == slotType)  // Match the slot type (active/inactive)
                .OrderBy(s => s.OccupiedBy.Count)       // Pick the best slot
                .ThenBy(s => s.Priority)                // Pick user preferred slot
                .ThenBy(s => s.X)                       // Pick left-to-right instead
                .ThenBy(s => s.Y);

            // Handle situations where no slot for this use is found
            if (possibleSlots.Count() == 0) {
                if (slotType == Slot.ActivityUses.Aside)
                    Logger.Log("WindowManager: Attempting to move table aside but there is no aside slot. Add one in config.",
                        Logger.Status.Warning, showMessageBox:true);
                else Logger.Log($"WindowManager: An active or inactive slot was missing. Corrupt config?", Logger.Status.Fatal);

                return resultSlot;
            }

            // Try to find an empty or the current slot or stackable slot - filter by preferred slot
            resultSlot = possibleSlots
                .Where(s => s.OccupiedBy.Count == 0 || s.OccupiedBy.Contains(table) || s.CanStack)
                .OrderBy(s => s.OccupiedBy.Count) // Prefer empty slots regardless of preferred
                .ThenByDescending(s => table.PreferredSlot != null && s == table.PreferredSlot) // pick the table's preferred slot
                .ThenBy(s => s.OccupiedBy.Where(t => t.PreferredSlot == null).Count()) // Find a slot with a table without a preferred slot
                .FirstOrDefault();
            if (resultSlot != null)
                return resultSlot;

            // At this point only handle active tables
            if (slotType == Slot.ActivityUses.Aside)
            {
                Logger.Log($"WindowManager: Tried to find Aside slot but none were available.");
                return resultSlot; // No available aside slots, do nothing.
            }
            else if (slotType == Slot.ActivityUses.Inactive)
            {
                Logger.Log($"WindowManager: No inactive slot available. " +
                    "This should be impossible since Inactive slots must be stackable. Corrupt config?",
                    Logger.Status.Fatal);
                return resultSlot;
            }

            // Try to find an active slot with a low priority table and push it to Inactive.
            resultSlot = possibleSlots
                // Only tables with priority that's allowed to be pushed aside
                .Where(s => s.OccupiedBy.First().Priority <= Table.Status.NoActionRequired)
                // Find lowest priority table to push aside
                .OrderByDescending(s => s.OccupiedBy
                    .OrderBy(x => x.Priority)
                    .ThenByDescending(x => x.PriorityChangedTime)
                    .First().PriorityChangedTime
                )
                .FirstOrDefault();

            // Give up if there's no table to push or
            // Push the table we found to inactive & return the free slot
            if (resultSlot != null)
            {
                Table tableToMove = resultSlot.OccupiedBy.First();
                Slot anInactiveSlot = GetAvailableSlot(Slot.ActivityUses.Inactive, table);
                Slot previousSlot = Config.Active.ActiveProfile.Slots
                    .Where(s => s.OccupiedBy.Contains(tableToMove))
                    .FirstOrDefault();
                delayedMoveQueue.Enqueue(new Tuple<Table, Slot, Slot>(tableToMove, anInactiveSlot, previousSlot));
            }

            return resultSlot;
        }


        /// <summary>
        /// Determines where the table should be moved to & moves it.
        /// Returns false if it can't find a suitable slot.
        /// </summary>
        /// <param name="table">Table to move</param>
        /// <param name="toSlot">Assign a custom slot & override the default logic</param>
        /// <returns>true on success, false on fail</returns>
        private bool TryMoveTable(Table table)
        {
            
            // Count available active slots
            int freeActiveSlotsCount = Config.Active.ActiveProfile.Slots
                .Where(s => s.ActivityUse == Slot.ActivityUses.Active)
                .Where(s => s.OccupiedBy.Count == 0 || s.OccupiedBy.Contains(table)) // Ignore stackable actives & the table's current slot
                .Count();

            int freeAsideSlotsCount = Config.Active.ActiveProfile.Slots
                .Where(s => s.ActivityUse == Slot.ActivityUses.Aside)
                .Count();

            // Count tables in queue that require an active slot (excluding target)
            int tablesRequireActiveSlotCount = Table.ActionQueue
                .Where(t => t.WindowHandle != table.WindowHandle)
                .Where(t => t.Priority >= Table.Status.ActionRequired)
                .Count();

            // Find the slot the table is in currently
            Slot previousSlot = Config.Active.ActiveProfile.Slots
                .Where(s => s.OccupiedBy
                    .Where(t => t == table).Count() > 0)
                .FirstOrDefault();

            // Questions that keep the if statements below from bleeding your eyes
            bool canUseActiveSlot = freeActiveSlotsCount > 0 && tablesRequireActiveSlotCount == 0;
            bool isPriorityTable = table.Priority >= Table.Status.ActionRequired;
            bool isNewTable = previousSlot == null;
            bool wasMadeUnaside = !isNewTable && previousSlot.ActivityUse == Slot.ActivityUses.Aside;


            // Determine slot type the table should occupy
            Slot.ActivityUses? activity = null;
            if (table.IsAside)
                activity = Slot.ActivityUses.Aside;

            // Priority table or when there free slots get an active
            else if (isPriorityTable || canUseActiveSlot) // todo: user setting goes here
                activity = Slot.ActivityUses.Active;
            
            // Was made unaside or low prio
            else if (!isPriorityTable || wasMadeUnaside || isNewTable) // todo: User setting also goes here
                activity = Slot.ActivityUses.Inactive;

            // Unblind closed table from any slot it occupies
            if (table.Priority == Table.Status.Closed)
            {
                var slotsWithClosed = Config.Active.ActiveProfile.Slots.
                    Where(s => s.OccupiedBy.Contains(table));
                var amountOfTimesDuplicateTableWasFoundInSingleSlot = slotsWithClosed // I'm not even trying anymore...
                    .GroupBy(s => s.OccupiedBy).Select(grp => new
                    {
                        Value = grp.Key,
                        Count = grp.Count()
                    })
                    .Where(g => g.Count > 1).Count();
                
                // count can be 0 if user closes before application has ever moved it
                // It should never be > 1. If it does, there's a bug.
                if (slotsWithClosed.Count() > 1 || amountOfTimesDuplicateTableWasFoundInSingleSlot > 0) // is table in multiple slots || table duplicate in one slot.ObbupiedBy
                    // Schrodingers Cat indicates tables were somehow not moved as intended resulting in a table occupying multiple slots at once.
                    // This issue is only relevant if i break something, user does something unexpected with profiles, or stacking breaks things when we enable it.
                    // Request that the user report it since it's hard to find a cause of any strange table movement without the exception.
                    Logger.Log("Schrodingers Cat. Please report this issue to the developer.", 
                        Logger.Status.Warning, true);

                // Remove any/all instances of the table from slots
                foreach (var slot in slotsWithClosed)
                    slot.UnbindTable(table);

                return true;
            }

            // No need to move to inactive (since inactive can be set even when we're not in the hand) or to slots of the same type, claim success
            else if (activity == null || (!isNewTable && activity == previousSlot.ActivityUse)) 
            {
                Logger.Log($"TableManager: Found no reason to move table ({table.WindowHandle}). Moving on.");
                return true;
            }

            // Don't attempt to move tables aside when no aside slots are available
            else if (activity == Slot.ActivityUses.Aside && freeAsideSlotsCount == 0)
            {
                Logger.Log("Table manager: Cannot move table ({table.WindowHandle}) aside. No free aside slots");
                return true;
            }


            // Find a suitable slot & move table if possible
            Slot toSlot = GetAvailableSlot((Slot.ActivityUses)activity, table);
            if (toSlot == null)
            {
                if (table.IsAside)
                {
                    Logger.Log($"TableManager: Requested aside for table {table.WindowHandle}. None were available - cancel aside.");
                    table.IsAside = false;
                    return true;
                }
                else
                {
                    Logger.Log($"TableManager: Failed to find an available slot for table {table.WindowHandle}");
                    return false;
                }
            }
            else if (!isNewTable && toSlot.ActivityUse == previousSlot.ActivityUse)
            {
                Logger.Log($"TableManager: Table {table.WindowHandle} is already in the best slot. Doing nothing.");
                return true;
            }
            else // Or move table and return true
            {
                Logger.Log($"TableManager: Determined that table ({table.WindowHandle}) " +
                    $"should be moved to slot ({toSlot.Id}). Moving.");
                MoveTable(table, toSlot, previousSlot);
                return true;
            }
        }

        /// <summary>
        /// Forcibly moves table to a specified slot.
        /// Handles actual window movement as well.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="toSlot"></param>
        private void MoveTable(Table table, Slot toSlot, Slot previousSlot)
        {
            Logger.Log($"TableManager: Moving table ({table.WindowHandle}) to (Activity:{toSlot.ActivityUse}) " +
                $"slot ({toSlot.Id}) at ({toSlot.X},{toSlot.Y},{toSlot.Width},{toSlot.Height})");
            
            // If the table or stars client was closed since it was enqueued, don't move it.
            if (table.Priority == Table.Status.Closed)
            {
                Logger.Log($"TableManager: Attempting to move a closed table ({table.WindowHandle}). Is cancelled.");
                return;
            }

            // Update preferred active slot
            if (toSlot.ActivityUse == Slot.ActivityUses.Active)
                table.PreferredSlot = toSlot;

            // Move the table
            try
            {
                // normalize
                ShowWindow(table.WindowHandle, ShowWindowCommands.Restore);
                ShowWindow(table.WindowHandle, ShowWindowCommands.Normal);

                // Move table offscreen to resize (fix for annoying flicker) - only when size changes
                if (previousSlot != null && (previousSlot.Width != toSlot.Width || previousSlot.Height != toSlot.Height))
                {
                    MoveWindow(table.WindowHandle, Convert.ToInt32(WpfScreen.OffScreenLocation.Item1),
                    Convert.ToInt32(WpfScreen.OffScreenLocation.Item2), previousSlot.Width, previousSlot.Height, true);
                    Thread.Sleep(Config.Active.TableMovementDelay); // If flicker persists, 200 ms did the trick last time - EDIT: Flicker is gone, why?
                }                

                // Move the window
                MoveWindow(table.WindowHandle, 
                    toSlot.X, toSlot.Y, toSlot.Width, toSlot.Height, true);

                // Bring to foreground if table requires action
                if (table.Priority >= Table.Status.ActionRequired)
                    ShowWindow(table.WindowHandle, ShowWindowCommands.Show);

                // Remove the table from any previous Slot it was in
                lock (Config.Active.ActiveProfile)
                {
                    // Find the table's old slot
                    var foundSlot = Config.Active.ActiveProfile.Slots.FirstOrDefault(
                        s => s.OccupiedBy.FirstOrDefault(
                            t => t.WindowHandle == table.WindowHandle) != null
                        );

                    // Unlist the table from the slot, if any
                    if (foundSlot != null)
                        foundSlot.UnbindTable(table);
                }

                // List the table to be in the new Slot
                toSlot.BindTable(table);

                // Run the delayed movement queue
                while (delayedMoveQueue.Count > 0)
                {
                    // Move table in queue
                    var tableFromQueue = delayedMoveQueue.Dequeue();
                    MoveTable(tableFromQueue.Item1, tableFromQueue.Item2, tableFromQueue.Item3);

                    // Also move any double stacked tables when an inactive slot is available
                    if (Config.Active.PreferSpreadOverStack)
                    {
                        // if there's a free inactive slot
                        var freeInactiveSlots = Config.Active.ActiveProfile.Slots.Where(s => s.ActivityUse == Slot.ActivityUses.Inactive && s.OccupiedBy.Count == 0);
                        if (freeInactiveSlots.Count() > 0)
                        {
                            // Find a slot with a table that should be moved, if any
                            Slot slotWithUnnessecarilyStackedTable = Config.Active.ActiveProfile.Slots
                                .Where(s => s.ActivityUse == Slot.ActivityUses.Inactive)
                                .Where(s => s.OccupiedBy.Count > 1)
                                .FirstOrDefault();

                            // if we found a slot, move the most approperiate table
                            if (slotWithUnnessecarilyStackedTable != null)
                            {
                                Table bestTableToMove = slotWithUnnessecarilyStackedTable.OccupiedBy.
                                    OrderByDescending(t => t.PriorityChangedTime)
                                    .FirstOrDefault();
                                delayedMoveQueue.Enqueue(new Tuple<Table, Slot, Slot>(
                                    bestTableToMove,
                                    freeInactiveSlots.First(),
                                    slotWithUnnessecarilyStackedTable));
                            }

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("WindowHandler: Error while moving table: " + ex.Message, Logger.Status.Error);
            }
        }

        private void ManageTables()
        {
            // Place all tables in approperiate slots
            InitialTablePlacement();

            // Our temp vars for the loop
            Table changedTable = null;
            int lastQueueCount = 0;

            // while app running and wMgr running
            while (IsRunning && (bool)App.Current.Properties["IsRunning"])
            {
                Thread.Sleep(25); // Wait a tick

                // The queue is empty
                if (Table.ActionQueue.IsEmpty)
                    continue;

                lock (Table.ActionQueue)
                {
                    // We already tried to move. Wait for another table to do something before retrying.
                    if (lastQueueCount == Table.ActionQueue.Count())
                    {
                        var activeRequestCount = Table.ActionQueue
                            .Where(t => !t.IsAside && t.Priority >= Table.Status.ActionRequired)
                            .Count();

                        // This should only apply to active tables - bump aside and inactive requests up the queue
                        if (activeRequestCount != Table.ActionQueue.Count()) 
                        {
                            // Determine new priority
                            List<Table> newOrder = new List<Table>();
                            foreach (Table queuedTable in Table.ActionQueue)
                            {
                                if (queuedTable.IsAside || queuedTable.IsAside)
                                    newOrder.Add(queuedTable);
                                else newOrder.Insert(0, queuedTable);
                            }

                            // requeue everything in new order
                            Table garbage; // No Clear() in ConcurrentQueue.
                            while (!Table.ActionQueue.IsEmpty)
                                Table.ActionQueue.TryDequeue(out garbage);
                            foreach (var t in newOrder)
                                Table.ActionQueue.Enqueue(t);

                            // Set lastQueueCount to 0 to indicate we should try again
                            lastQueueCount = 0;
                        }
                        else continue;
                    }
                        
                }

                // Queue has a table, put it in changedTable
                if (!Table.ActionQueue.TryPeek(out changedTable))
                    continue;

                // try to move the table, reset temp vars on success
                if (TryMoveTable(changedTable))
                {
                    changedTable = null;
                    lastQueueCount = -1;
                    Table.ActionQueue.TryDequeue(out changedTable); // Remove it from the queue
                }

                // Report that we tried to move the table but couldn't
                else lock (Table.ActionQueue) { lastQueueCount = Table.ActionQueue.Count(); }
            }
        }

        private void ForceTablePosition(object state)
        {
            if (IsRunning && Config.Active.ForceTablePosition)
            {
                lock (Table.KnownTables)
                {
                    foreach (var table in Table.KnownTables.Where(t => !t.IsVirtual))
                    {
                        Slot toSlot = Config.Active.ActiveProfile.Slots.Where(s => s.OccupiedBy.Contains(table)).FirstOrDefault();
                        if (toSlot == null)
                            return;

                        try
                        {
                            // normalize
                            ShowWindow(table.WindowHandle, ShowWindowCommands.Restore);
                            ShowWindow(table.WindowHandle, ShowWindowCommands.Normal);

                            // Move the window
                            MoveWindow(table.WindowHandle,
                                toSlot.X, toSlot.Y, toSlot.Width, toSlot.Height, true);
                        }
                        catch
                        {
                            Logger.Log("Table {table.WindowHandle} was closed unexpectedly during ForceTablePosition.");
                        }
                    }
                }                
            }
        }
        
        private void ActiveConfig_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // A new profile was selected, re-initialize
            if (IsRunning && e.PropertyName == "ActiveProfile" && Config.Active.ActiveProfile != null)
                InitialTablePlacement();

            // ForceTablePosition changed - update the timer acoordingly
            else if (e.PropertyName == "ForceTablePosition")
            {
                if (Config.Active.ForceTablePosition && forceTablePositionTimer == null)
                {
                    forceTablePositionTimer = new Timer(ForceTablePosition, null, 0, 500);
                }
                else if (!Config.Active.ForceTablePosition && forceTablePositionTimer != null)
                {
                    forceTablePositionTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    forceTablePositionTimer.Dispose();
                }
            }
        }
    }
}
