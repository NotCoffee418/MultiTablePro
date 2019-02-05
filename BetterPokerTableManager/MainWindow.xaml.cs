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

            // debug
            //Profile p = Profile.FromJson(Properties.Resources.profileEmpty);
            //Profile p = new Profile();
            //p.Slots.Add(new Slot(Slot.ActivityUses.Active, 0, 0, 400, 400));
            //p.Slots.Add(new Slot(Slot.ActivityUses.Inactive, 400, 0, 400, 400));

            //TableManager tm = new TableManager(c);
            //tm.Start();


            //PSLogHandler.Start();

            //var test = new SlotConfigHandler(p);
            //test.StartConfigHandler();

        }

        private Config ActiveConfig { get; set; }

        public bool AskConfirmation(string request, MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            MessageBoxResult dr = MessageBox.Show("Are you sure you want to " + request + "?", "Confirm", 
                MessageBoxButton.YesNo, MessageBoxImage.None, defaultResult);
            return dr == MessageBoxResult.Yes ? true : false;
        }

        internal void RefreshProfileList(bool selectActive = false, Profile selectSpecific = null)
        {
            List<Profile> newProfileList = Profile.GetAllProfiles();

            // Find old value, selectSpecific or find active profile
            Profile requestedSelection = null;
            if (selectActive)
                requestedSelection = newProfileList.FirstOrDefault(p => p.FileName == ActiveConfig.ActiveProfileFileName);
            else if (selectSpecific != null)
                requestedSelection = newProfileList.FirstOrDefault(p => p.Equals(selectSpecific));
            else if (profileSelectionCb.SelectedValue != null)
                requestedSelection = (Profile)profileSelectionCb.SelectedValue;

            // Bind new list
            profileSelectionCb.ItemsSource = null;
            profileSelectionCb.ItemsSource = newProfileList;

            
            if (requestedSelection != null && newProfileList.Contains(requestedSelection))
                profileSelectionCb.SelectedIndex = newProfileList.FindIndex(p => p.Equals(requestedSelection));
            else if (newProfileList.Count > 0)
                profileSelectionCb.SelectedIndex = 0;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Notify application started
            App.Current.Properties["IsRunning"] = true;
            Logger.Log("--- Starting application ---");

            // Load config & install on first run
            ActiveConfig = Config.FromFile();
            DataContext = ActiveConfig;

            // Refresh the profile list & select Active
            RefreshProfileList(selectActive:true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("Clean shutdown");
            Thread.Sleep(100);
            App.Current.Properties["IsRunning"] = false;
            Hide();
            Thread.Sleep(2000); // Give threaded loops a few seconds to finish up            
        }

        #region Select Profile
        private void ProfileSelectionCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                profilePreviewControl.DisplayProfile = (Profile)e.AddedItems[0];
        }

        private void EditProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = (Profile)profileSelectionCb.SelectedValue;
            RequestProfileSetup(p, SlotConfigHandler.SetupTypes.EditProfile);
        }

        private void NewProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = Profile.FromJson(Properties.Resources.profileEmpty);
            p.Name = "New Profile";
            RequestProfileSetup(p, SlotConfigHandler.SetupTypes.NewProfile);
        }

        private void DeleteProfileBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DuplicateProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = (Profile)((Profile)profileSelectionCb.SelectedValue).Clone();
            RequestProfileSetup(p, SlotConfigHandler.SetupTypes.NewProfile);
        }

        private void ImportProfileBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExportProfileBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void RequestProfileSetup(Profile p, SlotConfigHandler.SetupTypes setupType)
        {
            var sch = new SlotConfigHandler(p, setupType);
            sch.ProfileSetupCompleted += Sch_ProfileSetupCompleted;
            sch.StartConfigHandler();
        }

        private void Sch_ProfileSetupCompleted(object sender, EventArgs e)
        {
            var args = (ProfileSetupCompletedEventArgs)e;
            if (!args.IsSaved)
                return;

            // Write to file, refresh and select edited/created profile
            bool overwrite = args.SetupType == SlotConfigHandler.SetupTypes.EditProfile ? true : false;
            args.Profile.SaveToFile(overwrite);
            RefreshProfileList(selectSpecific:args.Profile);
        }
        #endregion
    }
}
