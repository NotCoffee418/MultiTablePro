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
            WindowHandle = wHnd;
            ActiveTables.Add(this);
        }

        IntPtr _windowHandle;

        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
            private set { _windowHandle = value; }
        }

        public static List<Table> ActiveTables = new List<Table>();

        public static Table Find(IntPtr wHnd)
        {
            return ActiveTables.FirstOrDefault(t => t.WindowHandle == wHnd);
        }

        /// <summary>
        /// Places the table in an available active position.
        /// </summary>
        public void MakeActive()
        {

        }

        /// <summary>
        /// Places the table in an available inactive position.
        /// </summary>
        public void MakeInactive()
        {

        }

        /// <summary>
        /// The table is closed. Go home.
        /// </summary>
        public void Close()
        {
            ActiveTables.RemoveAll(t => t.WindowHandle == WindowHandle);
        }
    }
}
