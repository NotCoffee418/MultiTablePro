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
        public SlotConfigHandler(Config config)
        {
            ActiveConfig = config;
        }
        public event EventHandler ConfigSetupCompleted;

        public Config ActiveConfig { get; set; }
        List<SlotConfigWindow> slotConfigWindows = new List<SlotConfigWindow>();


        public void StartConfigHandler()
        {
            foreach (Slot slot in ActiveConfig.Slots.OrderBy(s => s.Id))
                AddTable(slot);
        }

        internal void Save()
        {
            if (ConfigSetupCompleted != null)
                ConfigSetupCompleted(this, new ConfigSetupCompletedEventArgs(true, ActiveConfig));
        }

        internal void Cancel()
        {
            if (ConfigSetupCompleted != null)
                ConfigSetupCompleted(this, new ConfigSetupCompletedEventArgs(false, null));
        }

        internal void AddTable(Slot slot = null)
        {
            // Define slot if null
            if (slot == null)
            {
                slot = new Slot(Slot.ActivityUses.Inactive, 0, 0, 640, 473);
                ActiveConfig.Slots.Add(slot);
            }

            // Display new slotconfigwindow
            var scw = new SlotConfigWindow(this, slot);
            scw.Show();

            // Correct amount of windows for idCb, then add to list
            IncreaseMaxIdAllWindows();
            slotConfigWindows.Add(scw);

            // Set up idCb
            for (int i = 0; i < slotConfigWindows.Count; i++)
            {
                var cbi = new ComboBoxItem();
                cbi.Content = (i + 1).ToString();
                scw.idCb.Items.Add(cbi);
            }
            
            // Set correct idCb index
            try
            {                
                scw.idCb.SelectedIndex = scw.CurrentSlot.Id;
            }
            catch (IndexOutOfRangeException ioorEx)
            {
                Logger.Log($"SlotConfigHandler: {ioorEx.HResult} Failed to set ID combobox. " +
                    "Out of range - corrupt config file? Setting to auto", Logger.Status.Error);
                scw.idCb.SelectedIndex = 0;
            }

            // Setup ActivityUsesBox
            scw.ActivityUsesBox.ItemsSource = Enum.GetValues(typeof(Slot.ActivityUses)).Cast<Slot.ActivityUses>();
            scw.ActivityUsesBox.SelectedIndex = (int)slot.ActivityUse;

            // Validate change requests on ActivityUsesBox
            scw.ActivityUseChangedEventHandler += Scw_ActivityUseChangedEventHandler;
        }

        private void Scw_ActivityUseChangedEventHandler(object sender, EventArgs e)
        {
            var args = (ActivityUseChangedEventArgs)e;
            var win = (SlotConfigWindow)sender;

            // Stop listening to event on this window until validated (prevent stackoverflow)
            win.ActivityUseChangedEventHandler -= Scw_ActivityUseChangedEventHandler;

            // Counts before the change goes through
            int activeCount = ActiveConfig.Slots.Count(s => s.ActivityUse == Slot.ActivityUses.Active);
            int inactiveCount = ActiveConfig.Slots.Count(s => s.ActivityUse == Slot.ActivityUses.Inactive);

            // Check for manual user fault
            if (activeCount + inactiveCount < 2)
            {
                Logger.Log("SlotConfigHandler: Corrupted config file? You must have at least 1 active and 1 inactive slot.", 
                    Logger.Status.Fatal);
                return;
            }

            // Validate that minimum amount of activity slots is available. after remove
            if ((args.OldActivityUse == Slot.ActivityUses.Active && activeCount <= 1) ||
                (args.OldActivityUse == Slot.ActivityUses.Inactive && inactiveCount <= 1))
                // Revert
                win.ActivityUsesBox.SelectedIndex = (int)args.OldActivityUse;

            win.ActivityUseChangedEventHandler += Scw_ActivityUseChangedEventHandler;
        }

        internal void RemoveSlot(SlotConfigWindow scw)
        {
            ActiveConfig.Slots.RemoveAll(s => s.GetHashCode() == scw.CurrentSlot.GetHashCode()); // remove slot
            slotConfigWindows.RemoveAll(x => x.GetHashCode() == scw.GetHashCode()); // remove window
            // Lower possible max id in other tables
            // todo: Fix the ugly code
            ReduceMaxIdAllWindows(int.Parse((string)((ComboBoxItem)scw.idCb.SelectedItem).Content)); 
            // close happens in window class
        }

        // Increases the max id on all ID comboboxes in windows
        private void IncreaseMaxIdAllWindows()
        {
            foreach (var win in slotConfigWindows)
            {
                // todo: Yeah uhmm.. make this.. not horrible. Use bindings or something.
                var lastCbi = (ComboBoxItem)win.idCb.Items[win.idCb.Items.Count - 1];
                int newNum = (string)lastCbi.Content == "Auto" ? 1 : int.Parse((string)lastCbi.Content) + 1;
                var newCbi = new ComboBoxItem();
                newCbi.Content = newNum.ToString();
                win.idCb.Items.Add(newCbi);
            }
        }

        // Reduces the max id on all ID comboboxes in windows
        // Only call this on tables no longer listed in slotConfigWindows
        private void ReduceMaxIdAllWindows(int removedId)
        {
            // Reduce any ID's with a selection higher than removed table
            var windowsNeedReducing = slotConfigWindows.FindAll(w => w.idCb.SelectedIndex > removedId);
            foreach (var winRed in windowsNeedReducing)
                winRed.idCb.SelectedIndex--;

            // Remove last entry from all windows
            foreach (var win in slotConfigWindows)
            {
                var lastCbi = (ComboBoxItem)win.idCb.Items[win.idCb.Items.Count - 1];
                int last = (string)lastCbi.Content == "Auto" ? 0 : int.Parse((string)lastCbi.Content);
                if (last == 0)
                {
                    // Can happen on manually user modified config files
                    Logger.Log("SlotConfigHandler: ReduceMaxIdAllWindows failed, cannot go lower than 0", Logger.Status.Fatal);
                    return;
                }

                win.idCb.Items.RemoveAt(win.idCb.Items.Count - 1);
            }
        }
    }

    internal class ConfigSetupCompletedEventArgs : EventArgs
    {
        public bool IsSaved { get; internal set; }
        public Config Config { get; internal set; }
        public ConfigSetupCompletedEventArgs(bool isSaved, Config config)
        {
            IsSaved = isSaved;
            Config = config;
        }
    }

    internal class ActivityUseChangedEventArgs : EventArgs
    {
        public Slot RelevantSlot { get; internal set; }
        public Slot.ActivityUses OldActivityUse { get; internal set; }
        public Slot.ActivityUses NewActivityUse { get; internal set; }
        public ActivityUseChangedEventArgs(Slot relevantSlot, Slot.ActivityUses oldActivityUse, Slot.ActivityUses newActivityUse)
        {
            RelevantSlot = relevantSlot;
            OldActivityUse = oldActivityUse;
            NewActivityUse = newActivityUse;
        }
    }
}
