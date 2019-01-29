using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BetterPokerTableManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            App.Current.Properties["IsRunning"] = true;
            Logger.Log("--- Starting application ---");

            // debug
            PSLogHandler.Start();
            Config c = Config.FromJson(Properties.Resources.configEmpty);
            TableManager tm = new TableManager(c);
            tm.Start();


            /*
            // Spawn tables
            var t1 = Table.Find(new IntPtr(1), true);
            var t2 = Table.Find(new IntPtr(2), true);
            var t3 = Table.Find(new IntPtr(3), true);

            t1.Priority = Table.Status.ActionRequired;
            t1.Priority = Table.Status.HandEndedOrNotInHand;
            t2.Priority = Table.Status.ActionRequired;
            t3.Priority = Table.Status.ActionRequired;
            t1.Priority = Table.Status.ActionRequired;
            */

            //var test = new SlotConfigHandler(c);
            //test.StartConfigHandler();
            //test.ConfigSetupCompleted += Test_ConfigSetupCompleted;

        }

        // debug, kill me
        private void Test_ConfigSetupCompleted(object sender, EventArgs e)
        {
            var args = (ConfigSetupCompletedEventArgs)e;
            System.IO.File.WriteAllText("tmpconfig.txt", args.Config.GetJson());
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("Clean shutdown");
            Thread.Sleep(100);
            App.Current.Properties["IsRunning"] = false;
            Hide();
            Thread.Sleep(2000); // Give threaded loops a few seconds to finish up            
        }
    }
}
