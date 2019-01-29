using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace BetterPokerTableManager
{
    internal class WindowManager
    {
        public WindowManager(Config activeConfig)
        {

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
        }

        /// <summary>
        /// Stops the window manager
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
        }

        // private property's vars
        private Config _activeConfig;

        // Properties
        public bool IsRunning { get; set; }
        public Config ActiveConfig
        {
            get
            {
                if (_activeConfig == null)
                    _activeConfig = Config.FromFile();
                return _activeConfig;
            }
            set { _activeConfig = value; }
        }

        private void ManageTables()
        {
            InitialTablePlacement(); // Place all tables in inactive slots

            // while app running and wMgr running
            Table changedTable;
            while ((bool)App.Current.Properties["IsRunning"] && IsRunning)
            {
                if (Table.ActionQueue.TryDequeue(out changedTable)) {
                    Thread.Sleep(25);
                    continue;
                } // implied else
            }
        }

        private void InitialTablePlacement()
        {
            lock (Table.KnownTables)
            {
                foreach (Table table in Table.KnownTables)
                    MoveToInactive(table);
            }
        }

        private void MoveToActive(Table table)
        {
            
        }

        private void MoveToInactive(Table table)
        {

        }

        private void MakeAside(Table table)
        {

        }

        private void MakeUnAside(Table table)
        {

        }

        /// <summary>
        /// The heart of the program <3
        /// </summary>
        /// <param name="activity">The slot type</param>
        /// <param name="status">Used to push aside low priority tables if needed</param>
        /// <returns></returns>
        private Slot GetAvailableSlot(Slot.ActivityUses slotType, Table.Status status)
        {
            Slot resultSlot = null;

            // Find all possible slots & order them
            var possibleSlots = ActiveConfig.Slots
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

            // Try to find an empty slot or stackable slot
            resultSlot = possibleSlots
                .Where(s => s.OccupiedBy.Count == 0 || s.CanStack)
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
                .Where(s => s.OccupiedBy.First().Priority <= Table.Status.InHandNoActionRequired)
                // Find lowest priority table to push aside
                .OrderBy(s => s.OccupiedBy.OrderBy(x => x.Priority).First())
                .FirstOrDefault();

            // Give up if there's no table to push or
            // Push the table we found to inactive & return the free slot
            if (resultSlot != null)
                MoveToInactive(resultSlot.OccupiedBy.First());

            return resultSlot;
        }
    }
}
