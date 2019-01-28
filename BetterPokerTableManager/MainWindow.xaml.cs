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
            Config c = Config.FromJson(Properties.Resources.configDefault1920x1080);
            var test = new SlotConfigHandler(c);
            test.StartConfigHandler();
            test.ConfigSetupCompleted += Test_ConfigSetupCompleted;
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
