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
            //Profile p = Profile.FromJson(Properties.Resources.profileEmpty);
            //Profile p = new Profile();
            //p.Add(new Slot(Slot.ActivityUses.Active, 0, 0, 400, 400));
            //p.Add(new Slot(Slot.ActivityUses.Inactive, 400, 0, 400, 400));

            Config c = Config.FromFile(); // loads default config
            //c.ActiveProfile = p;
            //TableManager tm = new TableManager(c);
            //tm.Start();


            PSLogHandler.Start();

            var test = new SlotConfigHandler(c);
            test.StartConfigHandler();
            test.ConfigSetupCompleted += Test_ConfigSetupCompleted;

        }

        // debug, kill me
        private void Test_ConfigSetupCompleted(object sender, EventArgs e)
        {
            var args = (ConfigSetupCompletedEventArgs)e;
            System.IO.File.WriteAllText("tmpconfig.txt", args.Config.ActiveProfile.GetJson());
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
