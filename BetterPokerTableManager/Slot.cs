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
        private ActivityUses _activityUses;

        public int Id // lower Id is used first
        { 
            get { return _id; }
            set {
                var args = new SlotIdChangedEventArgs(_id, value);
                _id = value;
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

        public Statuses Status { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public event EventHandler SlotIdChangedEventHandler;
        public event EventHandler ActivityUseChangedEventHandler;
    }

    internal class SlotIdChangedEventArgs : EventArgs
    {
        public int OldId { get; internal set; }
        public int NewId { get; internal set; }
        public SlotIdChangedEventArgs(int oldId, int newId)
        {
            OldId = oldId;
            NewId = newId;
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
