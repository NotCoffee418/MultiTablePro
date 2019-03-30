using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace MultiTablePro
{
    /// <summary>
    /// Generic helper class
    /// </summary>
    class GHelper
    {
        private static ConcurrentBag<Thread> _threadBag = new ConcurrentBag<Thread>();
        public static void SafeThreadStart(ThreadStart method)
        {
            // Create the thread & start it
            Thread thread = new Thread(method);
            thread.Start();

            // Add to the list (so garbagecollection doesn't eat them)
            _threadBag.Add(thread);
        }

    }
}
