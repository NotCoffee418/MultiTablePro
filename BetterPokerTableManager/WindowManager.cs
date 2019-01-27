using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterPokerTableManager
{
    class WindowManager
    {
        public static void Run()
        {
            new Thread(() => ManageTables()).Start();
        }
        
        private static Config _activeConfig;
        static Config ActiveConfig
        {
            get
            {
                if (_activeConfig == null)
                    _activeConfig = Config.LoadConfig();
                return _activeConfig;
            }
            set { _activeConfig = value; }
        }

        private static void ManageTables()
        {
            while ((bool)App.Current.Properties["IsRunning"])
            {
                if (Table.ActionQueue.Count == 0) {
                    Thread.Sleep(25);
                    continue;
                } // implied else
                

            }
        }
    }
}
