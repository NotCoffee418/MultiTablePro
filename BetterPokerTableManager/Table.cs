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
        }

        IntPtr _windowHandle;

        public IntPtr WindowHandle
        {
            get { return _windowHandle; }
            private set { _windowHandle = value; }
        }
    }
}
