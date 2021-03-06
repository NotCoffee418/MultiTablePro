﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MultiTablePro.Data;

namespace MultiTablePro.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        TableManager ActiveTableManager { get; set; }
        private Timer watchOpenTablesTimer = null;


        public bool AskConfirmation(string request, MessageBoxResult defaultResult = MessageBoxResult.No)
        {
            MessageBoxResult dr = MessageBox.Show("Are you sure you want to " + request + "?", "Confirm", 
                MessageBoxButton.YesNo, MessageBoxImage.None, defaultResult);
            return dr == MessageBoxResult.Yes ? true : false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set title to product name (from API)
            Title = Config.Active.ActiveLicense.ProductName;

            // Set footer to include version
            versionInfoTxt.Text = $"MultiTable Pro v{Assembly.GetEntryAssembly().GetName().Version}";

            // Set DataContext
            DataContext = Config.Active;

            // Refresh the profile list & select Active
            RefreshProfileList(selectActive:true);

            // Auto minimize
            if (Config.Active.AutoMinimize)
                WindowState = WindowState.Minimized;

            // Start watching open tables
            watchOpenTablesTimer = new Timer(WatchOpenTables, null, 1000, 1000);

            // Register hotkeys
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            HotKeyHandler.RegisterHotKey(Config.Active.AsideHotKey, hWnd);
            ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(HotKeyHandler.HotkeyPressed);

            // Listen for license expiration events
            Config.Active.ActiveLicense.ExpirationEvent += ActiveLicense_ExpirationEvent;
        }

        private void ActiveLicense_ExpirationEvent(object sender, EventArgs e)
        {
            var args = (License.ExpirationEventArgs)e;
            if (args.IsExpired)
            {
                Logger.Log("Your license has expired. Application is shutting down within 30 seconds!", Logger.Status.Warning, showMessageBox:true);
                Thread.Sleep(30000);
                RestartApplication();
            }
            else Logger.Log($"Your license expires in {args.ExpiresIn.TotalMinutes}. Please upgrade your license or expect MultiTablePro to close at that time..", Logger.Status.Warning, showMessageBox: true);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Logger.Log("Clean shutdown");
            Thread.Sleep(100);
            App.Current.Properties["IsRunning"] = false;
            Hide();
            HotKeyHandler.UnregisterAllHotkeys();
            Thread.Sleep(2000); // Give threaded loops a few seconds to finish up            
        }

        #region Menu
        private void AdvancedConfigMi_Click(object sender, RoutedEventArgs e)
        {
            AdvancedSettingsWindow win = new AdvancedSettingsWindow();
            win.Show();
        }
        #endregion

        #region Status
        private void AutoStartCb_Checked(object sender, RoutedEventArgs e)
        {
            // Start table manager if AutoStart is checked after load
            if (IsLoaded && autoStartCb.IsChecked == true && 
                (ActiveTableManager == null || !ActiveTableManager.IsRunning))
                StartStop();
        }

        private void StartStopBtn_Click(object sender, RoutedEventArgs e)
        {
            StartStop();
        }

        private void StartStop()
        {
            // Start table manager
            if (ActiveTableManager == null)
                ActiveTableManager = new TableManager();

            if (ActiveTableManager.IsRunning)
            {
                ActiveTableManager.Stop();
                startStopBtn.Content = "Start";
                statusTxt.Text = "Not running";
            }
            else
            {
                ActiveTableManager.Start();
                startStopBtn.Content = "Stop";
                statusTxt.Text = "Running";
            }
        }
        #endregion

        #region Select Profile
        internal void RefreshProfileList(bool selectActive = false, Profile selectSpecific = null)
        {
            List<Profile> newProfileList = Profile.GetAllProfiles();
            if (newProfileList.Count == 0)
                Logger.Log("You have no profiles. This should be impossible. Please restart the application and contact the developer if this occurs again.", Logger.Status.Fatal);

            // Find old value, selectSpecific or find active profile
            Profile requestedSelection = null;
            Profile previousProfile = (Profile)profileSelectionCb.SelectedValue;
            if (selectActive)
            {
                requestedSelection = newProfileList.FirstOrDefault(p => p.FileName == Config.Active.ActiveProfile.FileName);
                if (requestedSelection == null) // Can happen when deleting a profile
                    requestedSelection = newProfileList[0];
            }
            else if (selectSpecific != null)
                requestedSelection = newProfileList.FirstOrDefault(p => p.Equals(selectSpecific));
            else if (profileSelectionCb.SelectedValue != null)
                requestedSelection = (Profile)profileSelectionCb.SelectedValue;

            // Bind new list
            profileSelectionCb.ItemsSource = null;
            profileSelectionCb.ItemsSource = newProfileList;

            // Select requested
            if (requestedSelection != null && newProfileList.Contains(requestedSelection))
                profileSelectionCb.SelectedIndex = newProfileList.FindIndex(p => p.Equals(requestedSelection));
            else if (newProfileList.Count > 0)
                profileSelectionCb.SelectedIndex = 0;

            // Activate edited profile if it was active previously
            if (requestedSelection != null && previousProfile != null && requestedSelection.Name == previousProfile.Name)
            {
                Config.Active.ActiveProfile = requestedSelection;
            }
        }

        private void ProfileSelectionCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                profilePreviewControl.DisplayProfile = (Profile)e.AddedItems[0];
        }

        private void EditProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile selectedProfile = (Profile)((Profile)profileSelectionCb.SelectedValue).Clone();
            if (selectedProfile.FileName == Properties.Settings.Default.DefaultProfileFileName)
            {
                Logger.Log("The default profile cannot be deleted. Try duplicating it instead.", Logger.Status.Info, true);
                return;
            }
            RequestProfileSetup(selectedProfile, SlotConfigHandler.SetupTypes.EditProfile);
        }

        private void NewProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = Profile.FromJson(Properties.Resources.profileEmpty);
            p.Name = "New Profile";
            RequestProfileSetup(p, SlotConfigHandler.SetupTypes.NewProfile);
        }

        private void DeleteProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile selectedProfile = (Profile)((Profile)profileSelectionCb.SelectedValue).Clone();
            if (selectedProfile.FileName == Properties.Settings.Default.DefaultProfileFileName)
            {
                Logger.Log("The default profile cannot be deleted.", Logger.Status.Info, true);
                return;
            }
            else if (!AskConfirmation($"DELETE profile '{selectedProfile}'"))
                return;

            string path = System.IO.Path.Combine(Config.DataDir, "Profiles", selectedProfile.FileName);
            try
            {
                System.IO.File.Delete(path);

                // Keeping this as reference - Don't do this
                // Changing the name dings an event which pushes the deleted item back into the profileSelectionCb
                //if (selectedProfile == Config.Active.ActiveProfile)
                //    Config.Active.ActiveProfile.Name += " (DELETED)";
            }
            catch
            {
                Logger.Log("An error occurred while removing the profile file. "+
                    "Do you have permission? Is the file open elsewhere?", Logger.Status.Error, true);
            }
            RefreshProfileList(selectActive:true);
        }

        private void DuplicateProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = (Profile)((Profile)profileSelectionCb.SelectedValue).Clone();
            RequestProfileSetup(p, SlotConfigHandler.SetupTypes.NewProfile);
        }

        private void ImportProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            // Request file from user
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = ".json";
            dlg.Filter = "Profile Files|*.json";
            if (dlg.ShowDialog() == false) // nullable
                return;

            // Generate profile, save it & refresh
            Profile p = Profile.GetProfileFromFile(dlg.FileName);
            p.SaveToFile();
            RefreshProfileList(selectSpecific:p);
        }

        private void ExportProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = (Profile)profileSelectionCb.SelectedValue;
            var dlg = new SaveFileDialog();
            dlg.FileName = p.FileName;
            dlg.DefaultExt = ".json";
            dlg.Filter = "Profile Files|*.json";
            if (dlg.ShowDialog() == false) // nullable
                return;
            File.WriteAllText(dlg.FileName, p.GetJson());
        }

        private void RequestProfileSetup(Profile p, SlotConfigHandler.SetupTypes setupType)
        {            
            bool cancel = false; // don't throw messagebox with lock on Slots, using bool instead
            lock (Config.Active.ActiveProfile.Slots)
            {
                // Prevent setup from running while setup is already running or during session
                cancel = Table.KnownTables.Count > 0;
            }

            if (cancel)
                Logger.Log("You cannot create or modify a profile with poker tables open or while already modifying a profile.", Logger.Status.Warning, true);
            else
            {
                var sch = new SlotConfigHandler(p, setupType);
                sch.ProfileSetupCompleted += Sch_ProfileSetupCompleted;
                sch.StartConfigHandler();
            }
        }

        private void Sch_ProfileSetupCompleted(object sender, EventArgs e)
        {
            var args = (ProfileSetupCompletedEventArgs)e;
            if (!args.IsSaved)
                return;

            // Get old file name
            string oldFileName = args.Profile.FileName; // changed by SaveToFile below

            // Write to file, refresh and select edited/created profile
            bool overwrite = args.SetupType == SlotConfigHandler.SetupTypes.EditProfile ? true : false;
            args.Profile.SaveToFile(overwrite);
            RefreshProfileList(selectSpecific:args.Profile);

            if (args.Profile.FileName != oldFileName)
                Config.Active.ActiveProfileFileName = args.Profile.FileName;
        }

        private void ActivateProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            Profile p = (Profile)profileSelectionCb.SelectedValue;
            Config.Active.ActiveProfile = p;
        }
        #endregion

        #region Auto Leave / Table Selection
        private void ApplyAutoLeaveSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            double autoLeaveVpip, autoLeaveHands;
            if (!double.TryParse(autoLeaveVpipTb.Text, out autoLeaveVpip) ||
                !double.TryParse(autoLeaveHandsTb.Text, out autoLeaveHands))
            {
                Logger.Log("Invalid input. Input must be integer (round number).", Logger.Status.Info, showMessageBox: true);
                return;
            }
            else
            {
                Config.Active.AutoLeaveVpip = Convert.ToInt32(autoLeaveVpip);
                Config.Active.AutoLeaveHands = Convert.ToInt32(autoLeaveHands);
            }
        }
        #endregion

        #region Open Tables
        private void WatchOpenTables(object state)
        {
            try
            {
                Dispatcher.BeginInvoke((Action)delegate () {
                    // Reset list
                    Table selectedTable = (Table)openTablesLv.SelectedValue;
                    openTablesLv.ItemsSource = null;
                    lock (Table.KnownTables)
                    {
                        if (Debugger.IsAttached)
                            openTablesLv.ItemsSource = Table.KnownTables.Where(t => t != null); // Show all tables including virtual
                        else openTablesLv.ItemsSource = Table.KnownTables.Where(t => !t.IsVirtual); // Only show real tables
                        if (selectedTable != null)
                        {
                            int index = openTablesLv.SelectedIndex = Table.KnownTables.FindIndex(t => t == selectedTable);
                            if (index != -1)
                                openTablesLv.SelectedIndex = index;
                        }
                    }
                });
            }
            catch (InvalidOperationException)
            {
                // An ItemsControl is inconsistent with its items source.
                // Just rewrite this whole thing.
                // I think it occurred when opening or closing a bunch of tables at once
                // I doubt the try catch will pick it up regardless..
            }
        }
        #endregion

        #region Extra Settings
        private void ChangeAsideHotKeyBtn_Click(object sender, RoutedEventArgs e)
        {
            var hkWin = new SetHotKeyWindow();
            hkWin.PropertyName = "AsideHotKey";
            hkWin.CurrentHotKey = Config.Active.AsideHotKey;
            hkWin.Show();
        }


        #endregion

        private void LicenseConfigMenu_Click(object sender, RoutedEventArgs e)
        {
            LicenseInfoWindow lWin = new LicenseInfoWindow();
            lWin.Show();
        }

        private void ImportConfigMi_Click(object sender, RoutedEventArgs e)
        {
            // Request file from user
            var dlg = new OpenFileDialog();
            dlg.DefaultExt = ".json";
            dlg.Filter = "MTP Config Files|*.json";
            if (dlg.ShowDialog() == false) // nullable
                return;

            // Generate profile, save it & refresh
            Config c = Config.FromFile(dlg.FileName);
            c.Save();
            Config.Active = c;
            Logger.Log("New config file imported. You may need to restart the program for changes to take effect.", Logger.Status.Info, showMessageBox:true);
        }

        private void ExportConfigMi_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog();
            dlg.FileName = "config-" + DateTime.Now.ToString("yyyy-MM-dd") + ".json";
            dlg.DefaultExt = ".json";
            dlg.Filter = "MTP Config Files|*.json";
            if (dlg.ShowDialog() == false) // nullable
                return;
            File.WriteAllText(dlg.FileName, Config.Active.GetJson());
            Logger.Log("User exported their config.");
        }

        private void ResetConfigMi_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to restore all your settings? This is irreversable and your settings will be lost unless you export them first.", 
                "Reset Config", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel) == MessageBoxResult.Yes)
            {
                Logger.Log("User manually reset config to defaults.");
                Config newConf = new Config();
                Config.Active = newConf;
                Config.Active.Save();
                Logger.Log("Config has been reset. You may need to restart the application for the changes to take effect.", Logger.Status.Info, showMessageBox: true);
            }            
        }

        private void OpenDiscord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discord.gg/s4QX4zE");
        }

        private void checkForUpdates_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Manually checking for updates");
            Updater.Run(force:true);
        }

        private void RestartWithLogger_Click(object sender, RoutedEventArgs e)
        {
            var mbr = MessageBox.Show(
                "WARNING: This should only be done if you wish to report a bug or have been asked to do so by support." + Environment.NewLine +
                "Be careful doing this during a session as may take some time for tables to be detected again after the restart.",
                "Restart with detailed logging?", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
            if (mbr == MessageBoxResult.Yes)
            {
                // Save config value
                Config.Active.EnableDetailedLogging = true;

                // Stop running & hide
                App.Current.Properties["IsRunning"] = false;
                Hide();

                // Restart
                RestartApplication();
            }            
        }

        private void RestartApplication()
        {
            // Create new process
            Process p = new Process();
            p.StartInfo.FileName = Assembly.GetExecutingAssembly().Location;
            p.StartInfo.Arguments = "-ignorealreadyrunning";
            p.Start();

            // Kill this application
            Application.Current.Shutdown();
        }
    }
}
