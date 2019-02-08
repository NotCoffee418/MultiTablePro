using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace BetterPokerTableManager
{
    class SlotConfigHandler
    {
        /// <summary>
        /// Send a base config containing at least 1 of each activity type
        /// The input config will be modified and can be requested via .ActiveConfig
        /// </summary>
        /// <param name="config"></param>
        public SlotConfigHandler(Profile profile, SetupTypes setupType)
        {
            ActiveProfile = profile;
            SetupType = setupType;
            ProfileSetupCompleted += SlotConfigHandler_ProfileSetupCompleted;
        }

        public event EventHandler ProfileSetupCompleted;

        public Profile ActiveProfile { get; set; }
        List<SlotConfigWindow> slotConfigWindows = new List<SlotConfigWindow>();
        public SetupTypes SetupType { get; set; }

        public enum SetupTypes
        {
            NewProfile,
            EditProfile
        }


        public void StartConfigHandler()
        {
            foreach (Slot slot in ActiveProfile.Slots.OrderBy(s => s.Priority))
                AddTable(slot);
        }

        internal void Save()
        {
            if (ProfileSetupCompleted != null)
                ProfileSetupCompleted(this, new ProfileSetupCompletedEventArgs(true, ActiveProfile, SetupType));
        }

        internal void Cancel()
        {
            if (ProfileSetupCompleted != null)
                ProfileSetupCompleted(this, new ProfileSetupCompletedEventArgs(false, null, SetupType));
        }

        internal void AddTable(Slot slot = null)
        {
            // Define slot if null
            if (slot == null)
            {
                slot = new Slot(Slot.ActivityUses.Inactive, 0, 0, 480, 366);
                ActiveProfile.Slots.Add(slot);
            }

            Logger.Log("SlotConfigHandler: " +
                    $"AddTable() with slot {slot.Id}");

            // Display new slotconfigwindow
            var scw = new SlotConfigWindow(this, slot);
            scw.Show();

            // Correct amount of windows for prioCb, then add to list
            IncreaseMaxIdAllWindows();
            slotConfigWindows.Add(scw);

            // Set up idCb
            for (int i = 0; i < slotConfigWindows.Count; i++)
            {
                var cbi = new ComboBoxItem();
                cbi.Content = (i + 1).ToString();
                scw.prioCb.Items.Add(cbi);
            }
            
            // Set correct idCb index
            try
            {                
                scw.prioCb.SelectedIndex = scw.CurrentSlot.Priority;
            }
            catch (IndexOutOfRangeException ioorEx)
            {
                Logger.Log($"SlotConfigHandler: {ioorEx.HResult} Failed to set ID combobox. " +
                    "Out of range - corrupt config file? Setting to auto", Logger.Status.Error);
                scw.prioCb.SelectedIndex = 0;
            }

            // Setup ActivityUsesBox
            scw.ActivityUsesBox.ItemsSource = Enum.GetValues(typeof(Slot.ActivityUses)).Cast<Slot.ActivityUses>();
            scw.ActivityUsesBox.SelectedIndex = (int)slot.ActivityUse;

            // Validate change requests on ActivityUsesBox
            scw.CurrentSlot.ActivityUseChangedEventHandler += Scw_ActivityUseChangedEventHandler;
        }

        private void Scw_ActivityUseChangedEventHandler(object sender, EventArgs e)
        {
            var args = (ActivityUseChangedEventArgs)e;
            var win = slotConfigWindows.First(x => x.CurrentSlot.Id == ((Slot)sender).Id); // 

            // Stop listening to event on this window until validated (prevent stackoverflow)
            win.CurrentSlot.ActivityUseChangedEventHandler -= Scw_ActivityUseChangedEventHandler;

            // Counts after the change goes through
            int activeCount = ActiveProfile.Slots.Count(s => s.ActivityUse == Slot.ActivityUses.Active);
            int inactiveCount = ActiveProfile.Slots.Count(s => s.ActivityUse == Slot.ActivityUses.Inactive);


            // Validate that minimum amount of activity slots is available. after remove
            if ((args.OldActivityUse == Slot.ActivityUses.Active && activeCount == 0) ||
                (args.OldActivityUse == Slot.ActivityUses.Inactive && inactiveCount == 0))
            {
                // Revert & recount
                win.ActivityUsesBox.SelectedIndex = (int)args.OldActivityUse;
                activeCount = ActiveProfile.Slots.Count(s => s.ActivityUse == Slot.ActivityUses.Active);
                inactiveCount = ActiveProfile.Slots.Count(s => s.ActivityUse == Slot.ActivityUses.Inactive);

                Logger.Log("You must have at least one active and one inactive slot",
                    Logger.Status.Warning, showMessageBox: true);
            }

            // Check for manual user fault after validation
            if (activeCount + inactiveCount < 2)
            {
                Logger.Log("SlotConfigHandler: Corrupted config file? You must have at least 1 active and 1 inactive slot.",
                    Logger.Status.Fatal);
                return;
            }

            win.CurrentSlot.ActivityUseChangedEventHandler += Scw_ActivityUseChangedEventHandler;
        }

        internal bool RemoveSlot(SlotConfigWindow scw)
        {
            // Ensure at least 1 active & 1 inactive table stays alive
            if ((scw.CurrentSlot.ActivityUse == Slot.ActivityUses.Active || 
                scw.CurrentSlot.ActivityUse == Slot.ActivityUses.Inactive) &&
                ActiveProfile.Slots.Count(x => x.ActivityUse == scw.CurrentSlot.ActivityUse) == 1)
            {
                Logger.Log("You must have at least one active and one inactive slot. " +
                    "Press Cancel or Save to close the table setup instead",
                    Logger.Status.Warning, showMessageBox: true);
                return false; // don't allow close
            }

            Logger.Log("SlotConfigHandler: " +
                    $"RemoveSlot() scw {scw.GetHashCode()}");

            if (ActiveProfile.Slots.Count(s => s.Id == scw.CurrentSlot.Id) == 0)
                Logger.Log("SlotConfigHandler: " +
                    $"RemoveSlot() closing a table {scw.GetHashCode()} that's already removed. Called twice?", Logger.Status.Error);

            ActiveProfile.Slots.RemoveAll(s => s.Id == scw.CurrentSlot.Id); // remove slot
            slotConfigWindows.RemoveAll(x => x.GetHashCode() == scw.GetHashCode()); // remove window
            // Lower possible max id in other tables
            // todo: Fix the ugly code
            string dirtyIdString = (string)((ComboBoxItem)scw.prioCb.SelectedItem).Content;
            ReduceMaxIdAllWindows(dirtyIdString == "Auto" ? 0 :  int.Parse(dirtyIdString));
            // close happens in window class
            return true;
        }

        // Increases the max id on all ID comboboxes in windows
        private void IncreaseMaxIdAllWindows()
        {
            Logger.Log("SlotConfigHandler: " +
                    $"IncreaseMaxIdAllWindows()");
            foreach (var win in slotConfigWindows)
            {
                // todo: Yeah uhmm.. make this.. not horrible. Use bindings or something.
                var lastCbi = (ComboBoxItem)win.prioCb.Items[win.prioCb.Items.Count - 1];
                int newNum = (string)lastCbi.Content == "Auto" ? 1 : int.Parse((string)lastCbi.Content) + 1;
                var newCbi = new ComboBoxItem();
                newCbi.Content = newNum.ToString();
                win.prioCb.Items.Add(newCbi);
            }
        }

        // Reduces the max id on all ID comboboxes in windows
        // Only call this on tables no longer listed in slotConfigWindows
        private void ReduceMaxIdAllWindows(int removedId)
        {
            Logger.Log("SlotConfigHandler: " +
                    $"ReduceMaxIdAllWindows()");

            // Reduce any ID's with a selection higher than removed table
            var windowsNeedReducing = slotConfigWindows.FindAll(w => w.prioCb.SelectedIndex > removedId);
            foreach (var winRed in windowsNeedReducing)
                winRed.prioCb.SelectedIndex--;

            // Remove last entry from all windows
            foreach (var win in slotConfigWindows)
            {
                var lastCbi = (ComboBoxItem)win.prioCb.Items[win.prioCb.Items.Count - 1];
                int last = (string)lastCbi.Content == "Auto" ? 0 : int.Parse((string)lastCbi.Content);
                if (last == 0)
                {
                    // Can happen on manually user modified config files
                    Logger.Log("SlotConfigHandler: ReduceMaxIdAllWindows failed, cannot go lower than 0", Logger.Status.Fatal);
                    return;
                }

                win.prioCb.Items.RemoveAt(win.prioCb.Items.Count - 1);
            }
        }

        private void SlotConfigHandler_ProfileSetupCompleted(object sender, EventArgs e)
        {
            // Unregister tables from KnownTables
            Table.KnownTables.RemoveAll(t => t.IsVirtual);
        }
    }

    internal class ProfileSetupCompletedEventArgs : EventArgs
    {
        public ProfileSetupCompletedEventArgs(bool isSaved, Profile profile, SlotConfigHandler.SetupTypes setupType)
        {
            IsSaved = isSaved;
            Profile = profile;
            SetupType = setupType;
        }

        public bool IsSaved { get; internal set; }
        public Profile Profile { get; internal set; }
        public SlotConfigHandler.SetupTypes SetupType { get; internal set; }
    }
}
