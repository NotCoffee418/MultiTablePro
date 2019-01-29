using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BetterPokerTableManager
{
    public class Slot
    {
        public Slot() {
            // Default constructor required for Json deserialization
        }
        public Slot(ActivityUses activityUse, int x, int y, int width, int height)
        {
            ActivityUse = activityUse;
            X = x;
            Y = y;
            Width = Width;
            Height = height;
        }

        public enum ActivityUses
        {
            Inactive = 0,
            Active = 1,
            Aside = 2,
        }
        public enum Statuses
        {
            Undefined = 0,
            Free = 0,
            UsedByInactive = 1,
            UsedByActive = 2,
            UsedByPriority = 3,
        }
        static string[] activityUseNames = Enum.GetNames(typeof(ActivityUses));
        static string[] statusNames = Enum.GetNames(typeof(Statuses));

        private int _id;
        private int _priority;
        private ActivityUses _activityUses;

        [JsonIgnore]
        public int Id
        {
            get
            {
                if (_id == 0)
                    _id = GetHashCode();
                return _id;
            }
        }

        public int Priority // lower priority is used first
        { 
            get { return _priority; }
            set {
                var args = new SlotPriorityChangedEventArgs(_priority, value);
                _priority = value;
                if (SlotIdChangedEventHandler != null)
                    SlotIdChangedEventHandler(this, args);
            }
            
        } 
        
        public ActivityUses ActivityUse
        {
            get { return _activityUses; }
            set {
                var args = new ActivityUseChangedEventArgs(_activityUses, value);
                _activityUses = value;
                if (ActivityUseChangedEventHandler != null)
                    ActivityUseChangedEventHandler(this, args);                
            }
        }

        [JsonIgnore]
        public Statuses Status { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public event EventHandler SlotIdChangedEventHandler;
        public event EventHandler ActivityUseChangedEventHandler;
    }

    internal class SlotPriorityChangedEventArgs : EventArgs
    {
        public int OldPriority { get; internal set; }
        public int NewPriority { get; internal set; }
        public SlotPriorityChangedEventArgs(int oldPrio, int newPrio)
        {
            OldPriority = oldPrio;
            NewPriority = newPrio;
        }
    }

    internal class ActivityUseChangedEventArgs : EventArgs
    {
        public Slot.ActivityUses OldActivityUse { get; internal set; }
        public Slot.ActivityUses NewActivityUse { get; internal set; }
        public ActivityUseChangedEventArgs(Slot.ActivityUses oldActivityUse, Slot.ActivityUses newActivityUse)
        {
            OldActivityUse = oldActivityUse;
            NewActivityUse = newActivityUse;
        }
    }
}
