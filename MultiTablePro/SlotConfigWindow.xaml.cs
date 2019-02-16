using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MultiTablePro
{
    /// <summary>
    /// Interaction logic for SlotConfigWindow.xaml
    /// </summary>
    public partial class SlotConfigWindow : Window
    {
        internal SlotConfigWindow(SlotConfigHandler sch, Slot currentSlot)
        {
            InitializeComponent();
            CurrentSlot = currentSlot;
            DataContext = CurrentSlot;
            profileNameTb.DataContext = sch.ActiveProfile;
            ActiveSlotConfigHandler = sch;
            sch.ProfileSetupCompleted += Sch_ProfileSetupCompleted;
            CurrentSlot.SlotIdChangedEventHandler += CurrentSlot_SlotIdChangedEventHandler;
            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    $"created with slot {currentSlot.Id}.");
        }

        private void CurrentSlot_SlotIdChangedEventHandler(object sender, EventArgs e)
        {
            var args = (SlotPriorityChangedEventArgs)e;
            var win = (Slot)sender;
            prioCb.SelectedIndex = args.NewPriority;
        }

        private void Sch_ProfileSetupCompleted(object sender, EventArgs e)
        {
            IsDone = true; // allows closing
            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    "Is done. Closing.");
            Close();
        }

        internal Slot CurrentSlot { get; set; }
        private bool AllowRecordChanges { get; set; }
        private bool IsDone { get; set; }
        private SlotConfigHandler ActiveSlotConfigHandler { get; set; }
        
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // Handle forced aspect ratio
            WindowAspectRatio.Register((Window)sender);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set requested slot info
            Width = CurrentSlot.Width;
            Height = CurrentSlot.Height;
            Left = CurrentSlot.X;
            Top = CurrentSlot.Y;
            CurrentSlot.PropertyChanged += CurrentSlot_PropertyChanged;


            // register this SlotConfigWindow as a virtual table
            IntPtr wHnd = new WindowInteropHelper(this).Handle;
            CurrentSlot.OccupiedBy.Add(new Table(wHnd, isVirtual: true));


            // Allow any size/position changes to be recorded from this point on
            AllowRecordChanges = true;
        }

        // Update position after usually manually input it
        private void CurrentSlot_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "X":
                    Left = CurrentSlot.X;
                    break;
                case "Y":
                    Top = CurrentSlot.Y;
                    break;
                case "Width":
                    Width = CurrentSlot.Width;
                    break;
                case "Height":
                    Height = CurrentSlot.Height;
                    break;
            }
        }

        private void ActivityUsesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0)
                return; // skip on load

            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    $"ActivityUsesBox SelectionChanged to {(int)(Slot.ActivityUses)e.AddedItems[0]}");
            CurrentSlot.ActivityUse = (Slot.ActivityUses)e.AddedItems[0];
        }
        
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AllowRecordChanges)
            {
                CurrentSlot.Width = Convert.ToInt32(e.NewSize.Width);
                CurrentSlot.Height = Convert.ToInt32(e.NewSize.Height);
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (AllowRecordChanges)
            {
                CurrentSlot.X = Convert.ToInt32(Left);
                CurrentSlot.Y = Convert.ToInt32(Top);
            }
        }

        private void AddSlotBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    "Requested add table.");

            // Make the new slot a slightly moved clone of the current slot
            var newSlot = new Slot();
            newSlot.X = CurrentSlot.X + 50;
            newSlot.Y = CurrentSlot.Y + 50;
            newSlot.Width = CurrentSlot.Width;
            newSlot.Height = CurrentSlot.Height;
            newSlot.ActivityUse = CurrentSlot.ActivityUse;
            newSlot.CanStack = CurrentSlot.CanStack;
            ActiveSlotConfigHandler.AddSlot(newSlot);
        }

        private void RemoveSlotBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveSlotConfigHandler.RemoveSlot(this))
            {
                Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    "Requested remove table. Permission granted.");
                IsDone = true; // give permission to close (else kill table runs twice
                Close();
            }
            else Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    "Requested remove table. Permission denied.");            
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    "Requested save.");
            ActiveSlotConfigHandler.Save();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    "Requested cancel");
            ActiveSlotConfigHandler.Cancel();
        }

        private void IdCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0)
                return; // skip on load

            // Save
            string sel = (string)((ComboBoxItem)e.AddedItems[0]).Content;
            if (sel == "Auto")
                sel = "0";

            Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                    $"IdCb SelectionChanged to {sel}");
            CurrentSlot.Priority = int.Parse(sel);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Close acts as remove table - cancel if not allowed
            if (!IsDone)
            {
                if (!ActiveSlotConfigHandler.RemoveSlot(this))
                {

                    Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                        "User attempted to close window. No permission to remove slot, canceling.");
                    e.Cancel = true;
                }
            }
            else
                Logger.Log($"SlotConfigWindow {GetHashCode()}: " +
                        "User attempted to close window. Permission granted, closing.");
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized || WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
                return;
            }
        }

    }

    internal class WindowAspectRatio
    {
        private double _ratio = 1.45;

        private WindowAspectRatio(Window window)
        {
            ((HwndSource)HwndSource.FromVisual(window)).AddHook(DragHook);
        }

        public static void Register(Window window)
        {
            new WindowAspectRatio(window);
        }

        internal enum WM
        {
            WINDOWPOSCHANGING = 0x0046,
        }

        [Flags()]
        public enum SWP
        {
            NoMove = 0x2,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        private IntPtr DragHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handeled)
        {
            if ((WM)msg == WM.WINDOWPOSCHANGING)
            {
                WINDOWPOS position = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                if ((position.flags & (int)SWP.NoMove) != 0 ||
                    HwndSource.FromHwnd(hwnd).RootVisual == null) return IntPtr.Zero;

                position.cy = Convert.ToInt32(position.cx * (1 / _ratio) + 32);

                Marshal.StructureToPtr(position, lParam, true);
                handeled = true;
            }

            return IntPtr.Zero;
        }
    }
}
