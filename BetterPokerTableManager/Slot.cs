using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;

namespace BetterPokerTableManager
{
    [Serializable]
    internal class Slot : IEquatable<Slot>, INotifyPropertyChanged
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
        static string[] activityUseNames = Enum.GetNames(typeof(ActivityUses));

        int _id;
        int _priority;
        int _x;
        int _y;
        int _width;
        int _height;
        bool _canStack;
        ActivityUses _activityUses = ActivityUses.Inactive;
        private static Size dpiFactor = new Size(1.0, 1.0);
        private static bool isInitialized;

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

                // Force CanStack on Inactive slots
                if (value == ActivityUses.Inactive)
                    CanStack = true;
                else CanStack = false; // todo: REMOVE THIS!! Add checkbox in SlotConfigWindow

                // trigger event
                if (ActivityUseChangedEventHandler != null)
                    ActivityUseChangedEventHandler(this, args);                
            }
        }

        [JsonIgnore]
        internal List<Table> OccupiedBy = new List<Table>();

        
        public int X
        {
            get { return _x; }

            set
            {
                if (IsNewLocationValid("X", value))
                {
                    _x = value;
                    RaisePropertyChanged("X");
                }
            }
        }
        public int Y
        {
            get { return _y; }
            set
            {
                if (IsNewLocationValid("Y", value))
                {
                    _y = value;
                    RaisePropertyChanged("Y");
                }
            }
        }
        public int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                RaisePropertyChanged("Width");
            }
        }
        public int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                RaisePropertyChanged("Height");
            }
        }
        public bool CanStack
        {
            get { return _canStack; }
            set
            {
                _canStack = value;
                RaisePropertyChanged("CanStack");
            }
        }

        [JsonIgnore]
        public static IEnumerable<Rect> WorkingAreas
        {
            get
            {
                return
                    Screen.AllScreens.Select(
                        screen =>
                        new Rect(
                            screen.WorkingArea.Left * dpiFactor.Width,
                            screen.WorkingArea.Top * dpiFactor.Height,
                            screen.WorkingArea.Width * dpiFactor.Width,
                            screen.WorkingArea.Height * dpiFactor.Height));
            }
        }

        public event EventHandler SlotIdChangedEventHandler;
        public event EventHandler ActivityUseChangedEventHandler;
        public event PropertyChangedEventHandler PropertyChanged;

        public void BindTable(Table table)
        {
            table.PreferredSlot = this;
            lock (OccupiedBy)
            {
                OccupiedBy.Add(table);
            }
        }

        public void UnbindTable(Table table)
        {
            table.PreferredSlot = null;
            lock (OccupiedBy)
            {
                OccupiedBy.RemoveAll(t => t.WindowHandle == table.WindowHandle);
            }
        }

        public static void TryInitialize(Visual visual)
        {
            if (isInitialized)
            {
                return;
            }

            var ps = PresentationSource.FromVisual(visual);
            if (ps == null)
            {
                return;
            }

            var ct = ps.CompositionTarget;
            if (ct == null)
            {
                return;
            }

            var m = ct.TransformToDevice;
            dpiFactor = new Size(m.M11, m.M22);
            isInitialized = true;
        }

        /// <summary>
        /// Determines wether the proposed new slot location is valid on scree
        /// </summary>
        /// <param name="propertyName">Property that changed</param>
        /// <param name="value">Value of that property</param>
        /// <returns></returns>
        private bool IsNewLocationValid(string propertyName, int value)
        {
            if (OccupiedBy.Count == 0 || !OccupiedBy[0].IsVirtual)
                return true;

            // Initialize the virtual table window
            TryInitialize((Window)HwndSource.FromHwnd(OccupiedBy[0].WindowHandle).RootVisual);

            // See if new location is valid
            Rect windowRectangle = new Rect(
                propertyName == "X" ? value : X,
                propertyName == "Y" ? value : Y,
                propertyName == "Width" ? value : Width,
                propertyName == "Height" ? value : Height
                );

            foreach (var workingArea in Slot.WorkingAreas)
            {
                var intersection = Rect.Intersect(windowRectangle, workingArea);
                var minVisible = new Size(30.0, 30.0);
                if (intersection.Width >= minVisible.Width &&
                    intersection.Height >= minVisible.Height)
                {
                    return true;
                }
            }

            return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null || !(obj is Slot))
                return false;
            else
                return Id == ((Slot)obj).Id;
        }

        public bool Equals(Slot other)
        {
            return other != null &&
                   Priority == other.Priority &&
                   ActivityUse == other.ActivityUse &&
                   X == other.X &&
                   Y == other.Y &&
                   Width == other.Width &&
                   Height == other.Height &&
                   CanStack == other.CanStack;
        }

        public override int GetHashCode()
        {
            var hashCode = 1477292275;
            hashCode = hashCode * -1521134295 + Priority.GetHashCode();
            hashCode = hashCode * -1521134295 + ActivityUse.GetHashCode();
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Width.GetHashCode();
            hashCode = hashCode * -1521134295 + Height.GetHashCode();
            hashCode = hashCode * -1521134295 + CanStack.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(Slot slot1, Slot slot2)
        {
            return EqualityComparer<Slot>.Default.Equals(slot1, slot2);
        }

        public static bool operator !=(Slot slot1, Slot slot2)
        {
            return !(slot1 == slot2);
        }

        public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
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
