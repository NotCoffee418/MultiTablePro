using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace BetterPokerTableManager
{
    internal class TableManager
    {
        public TableManager() { }
        public TableManager(Config activeConfig)
        {
            ActiveConfig = activeConfig;
        }

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
            forceTablePositionTimer = new Timer(ForceTablePosition, null, 0, 500);
        }

        /// <summary>
        /// Stops the window manager
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            forceTablePositionTimer.Dispose();
        }

        // private property's vars
        private Config _activeConfig;
        private Timer forceTablePositionTimer;

        // Properties
        public bool IsRunning { get; set; }
        public Config ActiveConfig
        {
            get
            {
                if (_activeConfig == null)
                    _activeConfig = Config.FromFile(); // load defaults
                return _activeConfig;
            }
            set { _activeConfig = value; }
        }

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
            var possibleSlots = ActiveConfig.ActiveProfile.Slots
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
                .OrderByDescending(s => table.PreferredSlot != null && s == table.PreferredSlot)
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
                .OrderBy(s => s.OccupiedBy.OrderBy(x => x.Priority).First())
                .FirstOrDefault();

            // Give up if there's no table to push or
            // Push the table we found to inactive & return the free slot
            if (resultSlot != null)
            {
                Table tableToMove = resultSlot.OccupiedBy.First();
                Slot anInactiveSlot = GetAvailableSlot(Slot.ActivityUses.Inactive, table);
                MoveTable(tableToMove, anInactiveSlot);
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
            int freeActiveSlotsCount = ActiveConfig.ActiveProfile.Slots
                .Where(s => s.ActivityUse == Slot.ActivityUses.Active)
                .Where(s => s.OccupiedBy.Count == 0 || s.OccupiedBy.Contains(table)) // Ignore stackable actives & the table's current slot
                .Count();

            // Count tables in queue that require an active slot (excluding target)
            int tablesRequireActiveSlotCount = Table.ActionQueue
                .Where(t => t.WindowHandle != table.WindowHandle)
                .Where(t => t.Priority >= Table.Status.ActionRequired)
                .Count();

            // Find the slot the table is in currently
            Slot previousSlot = ActiveConfig.ActiveProfile.Slots
                .Where(s => s.OccupiedBy
                    .Where(t => t == table).Count() > 0)
                .FirstOrDefault();

            // Questions that keep the if statements below from bleeding your eyes
            bool canUseActiveSlot = freeActiveSlotsCount > 0 && tablesRequireActiveSlotCount == 0;
            bool isPriorityTable = table.Priority >= Table.Status.ActionRequired;
            bool isNewTable = previousSlot == null;
            bool wasMadeUnaside = !isNewTable && previousSlot.ActivityUse == Slot.ActivityUses.Active;

            // Determine slot type the table should occupy
            Slot.ActivityUses? activity = null;
            if (table.IsAside)
                activity = Slot.ActivityUses.Aside;

            // Priority table or when there free slots get an active
            else if (isPriorityTable || canUseActiveSlot) // todo: user setting goes here
                activity = Slot.ActivityUses.Active;
            
            // Was made unaside or low prio
            else if ((wasMadeUnaside || isPriorityTable) || isNewTable) // todo: User setting also goes here
                activity = Slot.ActivityUses.Inactive;

            // No need to move to inactive or to slots of the same type, claim success
            if (activity == null || (!isNewTable && activity == previousSlot.ActivityUse)) 
            {
                Logger.Log($"TableManager: Found no reason to move table ({table.WindowHandle}). Moving on.");
                return true;
            }


            // Find a suitable slot & move table if possible
            Slot toSlot = GetAvailableSlot((Slot.ActivityUses)activity, table);
            if (toSlot == null)
            {
                Logger.Log($"TableManager: Failed to find an available slot for table {table.WindowHandle}"); 
                return false;
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
                MoveTable(table, toSlot);
                return true;
            }
        }

        /// <summary>
        /// Forcibly moves table to a specified slot.
        /// Handles actual window movement as well.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="toSlot"></param>
        private void MoveTable(Table table, Slot toSlot)
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
                WindowHandler.ShowWindow(table.WindowHandle, WindowHandler.ShowWindowCommands.Restore);
                WindowHandler.ShowWindow(table.WindowHandle, WindowHandler.ShowWindowCommands.Normal);

                // Move the window
                WindowHandler.MoveWindow(table.WindowHandle, 
                    toSlot.X, toSlot.Y, toSlot.Width, toSlot.Height, true);

                // Bring to foreground if table requires action
                if (table.Priority >= Table.Status.ActionRequired)
                    WindowHandler.ShowWindow(table.WindowHandle, WindowHandler.ShowWindowCommands.Show);

                // Remove the table from any previous Slot it was in
                lock (ActiveConfig.ActiveProfile)
                {
                    // Find the table's old slot
                    var foundSlot = ActiveConfig.ActiveProfile.Slots.FirstOrDefault(
                        s => s.OccupiedBy.FirstOrDefault(
                            t => t.WindowHandle == table.WindowHandle) != null
                        );

                    // Unlist the table from the slot, if any
                    if (foundSlot != null)
                        foundSlot.UnbindTable(table);
                }

                // List the table to be in the new Slot
                toSlot.BindTable(table);
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

                // We already tried to move. Wait for another table to do something before retrying.
                lock (Table.ActionQueue) // not sure if .Count() is thread-safe
                {
                    if (lastQueueCount == Table.ActionQueue.Count())
                        continue;
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
            if (IsRunning && ActiveConfig.ForceTablePosition)
            {
                lock (Table.KnownTables)
                {
                    foreach (var table in Table.KnownTables.Where(t => !t.IsVirtual))
                    {
                        Slot toSlot = ActiveConfig.ActiveProfile.Slots.Where(s => s.OccupiedBy.Contains(table)).FirstOrDefault();
                        if (toSlot == null)
                            return;

                        try
                        {
                            // normalize
                            WindowHandler.ShowWindow(table.WindowHandle, WindowHandler.ShowWindowCommands.Restore);
                            WindowHandler.ShowWindow(table.WindowHandle, WindowHandler.ShowWindowCommands.Normal);

                            // Move the window
                            WindowHandler.MoveWindow(table.WindowHandle,
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
    }
}
