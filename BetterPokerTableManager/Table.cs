using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    internal class Table
    {
        public Table(IntPtr wHnd)
        {
            WindowHandle = wHnd;
            RegisterNewTable();
        }

        // Priority status of the table determines what TableManager should do with it.
        public enum Status
        {
            Closed = 0,
            OpenButNotJoined = 1,
            HandEndedOrNotInHand = 2,
            InHandNoActionRequired = 3,
            ActionRequired = 4,
            TimeRunningLow = 5,
        }
        static string[] statusNames = Enum.GetNames(typeof(Status));

        bool _isAside;
        IntPtr _windowHandle;
        Status _priority = Status.HandEndedOrNotInHand;
        DateTime _priorityChangedTime;

        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
            private set { _windowHandle = value; }
        }
        public bool IsAside
        {
            get { return _isAside; }
            set {
                _isAside = value;
                ActionQueue.Enqueue(this);
            }
        }
        public Status Priority
        {
            get { return _priority; }
            set {
                if (_priority != value)
                {
                    Logger.Log($"Changing priority of table {WindowHandle} from '{statusNames[(int)_priority]}' to '{statusNames[(int)value]}'");                    
                    _priority = value;
                    _priorityChangedTime = DateTime.Now;
                    ActionQueue.Enqueue(this);
                }                
            }
        }
        public DateTime PriorityChangedTime
        {
            get
            {
                if (_priorityChangedTime == DateTime.MinValue)
                    return DateTime.Now;
                else return _priorityChangedTime;
            }
        }

        #region Static
        public static List<Table> KnownTables = new List<Table>();
        public static ConcurrentQueue<Table> ActionQueue = new ConcurrentQueue<Table>();

        /// <summary>
        /// Finds a known table.
        /// </summary>
        /// <param name="wHnd">Search by window handle</param>
        /// <param name="makeMissing">Register the table if it's unknown?</param>
        /// <returns>null or Table matching the wHnd</returns>
        public static Table Find(IntPtr wHnd, bool registerMissing = true)
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
        public static void SetPriority(IntPtr wHnd, Status priority, bool registerMissing = false)
        {
            Table t = Find(wHnd, registerMissing);
            if (t == null && !registerMissing)
                Logger.Log($"SetPriority() failed to set priority to {statusNames[(int)priority]} on nonexistent table ({wHnd}). registerMissing = false.", Logger.Status.Warning);
            else t.Priority = priority;
        }
        #endregion

        /// <summary>
        /// Registers this table
        /// </summary>
        public void RegisterNewTable()
        {
            Logger.Log($"Registering new table ({WindowHandle}).");
            lock (KnownTables) {
                KnownTables.Add(this);
            }
            ActionQueue.Enqueue(this);
        }

        /// <summary>
        /// The table is closed. Go home.
        /// </summary>
        public void Close()
        {
            Priority = Status.Closed;
            Logger.Log($"Closing table ({WindowHandle}).");
            lock (KnownTables) {
                KnownTables.RemoveAll(t => t.WindowHandle == WindowHandle);
            }
        }
    }
}
