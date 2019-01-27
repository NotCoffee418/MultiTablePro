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
        public SlotConfigWindow()
        {
            InitializeComponent();
        }


        private void ActivityUsesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            WindowAspectRatio.Register((Window)sender);
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
