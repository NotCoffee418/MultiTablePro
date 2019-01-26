using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    class Table
    {
        public Table(IntPtr wHnd)
        {
            Logger.Log($"Creating new table {wHnd}");
            WindowHandle = wHnd;
            KnownTables.Add(this);            
        }

        IntPtr _windowHandle;

        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
            private set { _windowHandle = value; }
        }
        public bool IsAside { get; set; }
        public bool IsActive { get; set; }

        public static List<Table> KnownTables = new List<Table>();
        public static Queue<Table> ActionQueue = new Queue<Table>();

        /// <summary>
        /// Finds a known table.
        /// </summary>
        /// <param name="wHnd">Search by window handle</param>
        /// <param name="makeMissing">Register the table if it's unknown?</param>
        /// <returns>null or Table matching the wHnd</returns>
        public static Table Find(IntPtr wHnd, bool registerMissing = false)
        {
            Table table = KnownTables.FirstOrDefault(t => t.WindowHandle == wHnd);
            if (table == null && registerMissing)
            {
                table = new Table(wHnd);
                Logger.Log($"Table.Find() could not find table ({wHnd}). " +
                    "Registering missing table. Likely a result of tables running before BPTM started.", Logger.Status.Warning);
            }
            return table;
        }

        /// <summary>
        /// Places the table in an available active position or adds it to the action queue.
        /// </summary>
        /// <param name="fromQueue"></param>
        /// <returns></returns>
        public bool MakeActive(bool fromQueue = false)
        {
            if (IsAside || IsActive)
            {
                Logger.Log($"Attempting to make {WindowHandle} active. Is already active or aside.");
                return false;
            }
            if (!fromQueue)
                ActionQueue.Enqueue(this);

            return true;
        }

        /// <summary>
        /// Places the table in an available inactive position.
        /// </summary>
        public bool MakeInactive()
        {
            if (IsAside || !IsActive)
            {
                Logger.Log($"Attempting to make {WindowHandle} inactive. Is already inactive or aside.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Triggered by hotkey, puts a table in a seperate aside slot. Useful to take notes or view the action. 
        /// Aside tables need to be manually made UnAside before they return to their normal rotation. 
        /// </summary>
        public bool MakeAside()
        {

            return true;
        }

        /// <summary>
        /// Triggered by hotkey, puts a table in a seperate aside slot. Useful to take notes or view the action. 
        /// Aside tables need to be manually made UnAside before they return to their normal rotation.
        /// </summary>
        public bool MakeUnAside()
        {

            return true;
        }

        /// <summary>
        /// This should trigger when to act time is running low.
        /// Table will be made active, failing that, table will be put aside
        /// </summary>
        public bool MakePriority()
        {

            return true;
        }

        /// <summary>
        /// The table is closed. Go home.
        /// </summary>
        public void Close()
        {
            Logger.Log($"Closing table {WindowHandle}.");
            KnownTables.RemoveAll(t => t.WindowHandle == WindowHandle);
        }
    }
}
