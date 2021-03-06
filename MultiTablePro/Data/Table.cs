﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MultiTablePro.Data
{
    [Serializable]
    internal class Table : IEquatable<Table>
    {
        [DllImport("user32.dll")]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        public Table(IntPtr wHnd, bool isVirtual = false)
        {
            WindowHandle = wHnd;
            _isVirtual = isVirtual;
            RegisterNewTable();
        }

        // Priority status of the table determines what TableManager should do with it.
        public enum Status
        {
            Closed = 0,
            OpenButNotJoined = 1,
            NoActionRequired = 2,
            ActionRequired = 3,
            TimeRunningLow = 4,
        }
        static string[] statusNames = Enum.GetNames(typeof(Status));

        bool _isAside;
        IntPtr _windowHandle;
        Status _priority = Status.NoActionRequired;
        DateTime _priorityChangedTime;
        bool _isVirtual;
        string _name = "";
        double _bigBlind = 0.0;

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
        public Slot PreferredSlot { get; internal set; }
        public bool IsVirtual
        {
            get { return _isVirtual; }
        }
        public string Name
        {
            get
            {
                if (IsVirtual)
                    return "Virtual Table";
                else if (_name == "")
                    SetNameAndBigBlind();
                return _name;
            }
        }

        public double BigBlind
        {
            get
            {
                if (IsVirtual)
                    return 0;
                else if (_bigBlind == 0 && _name == "") // 0NL can happen on <$0.50 tourney-types
                    SetNameAndBigBlind();
                return _bigBlind;
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
            if (!IsVirtual)
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

        private void SetNameAndBigBlind()
        {
            const int nChars = 256;
            StringBuilder buff = new StringBuilder(nChars);
            if (GetWindowText(WindowHandle, buff, nChars) > 0)
            {
                string windowTitle = buff.ToString();

                // *** POKERSTARS *** //
                // eg Cash: Some Table Name #6 - 50/$0.05 Speelgeld - No Limit Hold'em - Logged In as Username
                // Tourney non-eng: Oefengeld No Limit Hold'em (Hyper, 10k) - Ciemne 50/100 Ante 10 - Turniej 645665 Stol 82 - Something jako Username
                // Sat: Sunday Million 13th Anniversary Sat: $2.20+R NLHE [Splash], 1 Seat Gtd - On-Demand! - Blinds $300/$600 Ante $60 - Tournament 2575163794 Table 3 - Logged In as Username123
                // Spin: PM 10000.00 NLHE Spin &Go - Blinds 10 / 20 - Tournament 45645656456 Table 54 - Logged in as Username
                // G1: Title, G3: SB, G4: BB, G6: Tourney/cash indication
                Regex rStarsWinTitle = new Regex(@"(.*) - (\D+)?([0-9]+[\.|\,]?[0-9]+?)\/\D?([0-9]+[\.|\,]?[0-9]+?)(\ .*)? - (.*)( - .*)?");               
                if (rStarsWinTitle.IsMatch(windowTitle))
                { 
                    // Match indicates the table is tourney, spin, sng+
                    // Tourney: Tournament 5454545345 Table 82
                    // Cash: No Limit Hold'em
                    Regex rIsTourney = new Regex(@"\S+ \d+ \S+ \d+");
                    var rMatch = rStarsWinTitle.Match(windowTitle);
                    _name = rMatch.Groups[1].Value;
                    if (rIsTourney.IsMatch(rMatch.Groups[5].Value))
                        _bigBlind = 0f; // 0 indicates tourney table, change if needed
                    else _bigBlind = double.Parse(rMatch.Groups[4].Value); // cash table
                    return;
                }

                // *** BWIN *** //
                // Cash: Bergen -  NL  Hold'em - $0.01/$0.02
                Regex rBwinWinTitle = new Regex(@"(.+) -  (NL|PL|FL)  .+ - \$(\d+?\.\d+)\/\$(\d+?\.\d+)");
                if (rBwinWinTitle.IsMatch(windowTitle))
                {
                    var rMatch = rBwinWinTitle.Match(windowTitle);
                    _name = rMatch.Groups[1].Value;
                    _bigBlind = double.Parse(rMatch.Groups[4].Value);
                    return;
                }

                Regex rBwinTourneyWinTitle = new Regex(@"(.+) \(\d+\) Table #\d+ -  (NL|PL)  .+ - \$(\d+?(\.\d+)?) Buy-in");
                // Tourney: Monster #02-Low: $5K Gtd [Deep, 8-Max] (203102130) Table #132 -  NL  Hold'em - $2.20 Buy-in
                // Spins: $1 SPINS (206024853) Table #1 -  NL  Hold'em - $1 Buy-in
                if (rBwinTourneyWinTitle.IsMatch(windowTitle))
                {
                    var rMatch = rBwinTourneyWinTitle.Match(windowTitle);
                    _name = rMatch.Groups[1].Value;
                    _bigBlind = double.Parse(rMatch.Groups[3].Value) / 100;
                    return;
                }
                

                // If we reached this point, log a warning and set to invalid title - failed to find it
                Logger.Log($"Table: SetNameAndBigBlind failed on table with name: {windowTitle}", Logger.Status.Warning);
                _name = "(UNKNOWN WINDOW TITLE OR NOT LOGGED IN)";
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Table);
        }

        public bool Equals(Table other)
        {
            return other != null &&
                   EqualityComparer<IntPtr>.Default.Equals(WindowHandle, other.WindowHandle);
        }

        public override int GetHashCode()
        {
            return 1407091763 + EqualityComparer<IntPtr>.Default.GetHashCode(WindowHandle);
        }

        public static bool operator ==(Table table1, Table table2)
        {
            return EqualityComparer<Table>.Default.Equals(table1, table2);
        }

        public static bool operator !=(Table table1, Table table2)
        {
            return !(table1 == table2);
        }

        public override string ToString()
        {
            // todo: gametype or currency is not properly reflected, just delete x NL from string after testing
            string result = $"{Name} - {Math.Round(BigBlind * 100)} NL" +
                (Priority >= Status.ActionRequired ? " - Action Required" : "");

            if (Debugger.IsAttached)
            {
                result += $" - {Priority}";
                result += IsAside ? " - IsAside" : "";
            }

            return result;
        }
    }
}
