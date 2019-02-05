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
            Profile p = Profile.FromJson(Properties.Resources.profileEmpty);
            //Profile p = new Profile();
            //p.Slots.Add(new Slot(Slot.ActivityUses.Active, 0, 0, 400, 400));
            //p.Slots.Add(new Slot(Slot.ActivityUses.Inactive, 400, 0, 400, 400));

            Config c = Config.FromFile(); // loads default config
            //c.ActiveProfile = p;
            //TableManager tm = new TableManager(c);
            //tm.Start();


            //PSLogHandler.Start();

            var test = new SlotConfigHandler(p);
            //test.StartConfigHandler();
            test.ProfileSetupCompleted += Test_ProfileSetupCompleted;

            profilePreviewControl.DisplayProfile = c.ActiveProfile;
        }

        public void RefreshProfileList()
        {
            // Save old value (so it can be re-selected
            Profile previousSelection = null;
            if (profileSelectionCb.SelectedValue != null)
                previousSelection = (Profile)profileSelectionCb.SelectedValue;

            // Bind new list
            List<Profile> newProfileList = Profile.GetAllProfiles();
            profileSelectionCb.Items.Clear();
            profileSelectionCb.ItemsSource = newProfileList;

            // Select old value if possible
            if (previousSelection != null && newProfileList.Contains(previousSelection))
                profileSelectionCb.SelectedIndex = newProfileList.FindIndex(p => p.Equals(previousSelection));
            else if (newProfileList.Count > 0)
                profileSelectionCb.SelectedIndex = 0;
        }

        // debug, kill me
        private void Test_ProfileSetupCompleted(object sender, EventArgs e)
        {
            var args = (ProfileSetupCompletedEventArgs)e;
            System.IO.File.WriteAllText("tmpconfig.txt", args.Profile.GetJson());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshProfileList();
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
