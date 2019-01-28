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
            // Start manual table finder before logs to ensure loghandler doesn't try to run commands on unknown tables (in case BPTM is started during a session)
            //PSLogHandler.Start();
            //PSLogHandler.AnalyzeLine(new List<string>(){"table window 000E13DE has been destroyed"});
            //var win = new SlotConfigWindow(null, new Slot(Slot.ActivityUses.Active,500,0,0,0));
            //win.Show();
            Config c = new Config("");
            c.Slots.Add(new Slot(Slot.ActivityUses.Active, 500, 0, 0, 0));
            c.Slots.Add(new Slot(Slot.ActivityUses.Inactive, 0, 0, 0, 0));
            var test = new SlotConfigHandler(c);
            test.StartConfigHandler();
            test.ConfigSetupCompleted += Test_ConfigSetupCompleted;
        }

        // debug, kill me
        private void Test_ConfigSetupCompleted(object sender, EventArgs e)
        {
            ConfigSetupCompletedEventArgs args = (ConfigSetupCompletedEventArgs)e;
            var b = args.Config;
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
