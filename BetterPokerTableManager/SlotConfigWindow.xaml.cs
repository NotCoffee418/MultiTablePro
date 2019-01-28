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

namespace BetterPokerTableManager
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
            ActiveSlotConfigHandler = sch;
        }

        public Slot CurrentSlot { get; set; }
        private bool AllowRecordChanges { get; set; }
        private SlotConfigHandler ActiveSlotConfigHandler { get; set; }
        public event EventHandler ActivityUseChangedEventHandler;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set requested slot info
            Left = CurrentSlot.X;
            Top = CurrentSlot.Y;
            Width = CurrentSlot.Width;
            Height = CurrentSlot.Height;            

            // Allow any size/position changes to be recorded from this point on
            AllowRecordChanges = true;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            // Handle forced aspect ratio
            WindowAspectRatio.Register((Window)sender);
        }

        private void ActivityUsesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count == 0)
                return; // skip on load

            var args = new ActivityUseChangedEventArgs(CurrentSlot,
                (Slot.ActivityUses)e.RemovedItems[0], (Slot.ActivityUses)e.AddedItems[0]);
            if (ActivityUseChangedEventHandler != null)
                ActivityUseChangedEventHandler(this, args);
        }
        
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AllowRecordChanges)
            {
                CurrentSlot.Width = Convert.ToInt32(e.NewSize.Width);
                CurrentSlot.Height = Convert.ToInt32(e.NewSize.Width);
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
            ActiveSlotConfigHandler.AddTable();
        }

        private void RemoveSlotBtn_Click(object sender, RoutedEventArgs e)
        {
            ActiveSlotConfigHandler.RemoveSlot(this);
            Close();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            ActiveSlotConfigHandler.Save();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ActiveSlotConfigHandler.Cancel();
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
