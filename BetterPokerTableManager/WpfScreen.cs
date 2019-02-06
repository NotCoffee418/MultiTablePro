using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace BetterPokerTableManager
{
    public class WpfScreen
    {
        public static IEnumerable<WpfScreen> AllScreens()
        {
            foreach (Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                yield return new WpfScreen(screen);
            }
        }

        private static Tuple<int,int> _offScreenLocation = null;
        public static Tuple<int, int> OffScreenLocation
        {
            get
            {
                if (_offScreenLocation == null)
                {
                    var r = GetWorkingAreaInfo();
                    _offScreenLocation = new Tuple<int, int>(
                        Convert.ToInt32(r.X - 1320),
                        Convert.ToInt32(r.Y - 940));
                }
                return _offScreenLocation;
            }
        }

        /// <summary>
        /// Returns starting point (X,Y) of the screen and size (Width,Height)
        /// </summary>
        /// <returns></returns>
        public static Rect GetWorkingAreaInfo()
        {
            double lowX = 0;
            double lowY = 0;
            foreach (var screen in AllScreens())
            {
                if (screen.WorkingArea.Left < lowX)
                    lowX = screen.WorkingArea.Left;
                if (screen.WorkingArea.Top < lowY)
                    lowY = screen.WorkingArea.Top;
            }

            return new Rect(lowX, lowY, 
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
        }

        public static WpfScreen GetScreenFrom(Window window)
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(window);
            Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            WpfScreen wpfScreen = new WpfScreen(screen);
            return wpfScreen;
        }

        public static WpfScreen GetScreenFrom(Point point)
        {
            int x = (int)Math.Round(point.X);
            int y = (int)Math.Round(point.Y);

            // are x,y device-independent-pixels ??
            System.Drawing.Point drawingPoint = new System.Drawing.Point(x, y);
            Screen screen = System.Windows.Forms.Screen.FromPoint(drawingPoint);
            WpfScreen wpfScreen = new WpfScreen(screen);

            return wpfScreen;
        }

        public static WpfScreen Primary
        {
            get { return new WpfScreen(System.Windows.Forms.Screen.PrimaryScreen); }
        }

        private readonly Screen screen;

        internal WpfScreen(System.Windows.Forms.Screen screen)
        {
            this.screen = screen;
        }

        public Rect DeviceBounds
        {
            get { return this.GetRect(this.screen.Bounds); }
        }

        public Rect WorkingArea
        {
            get { return this.GetRect(this.screen.WorkingArea); }
        }

        private Rect GetRect(System.Drawing.Rectangle value)
        {
            // should x, y, width, height be device-independent-pixels ??
            return new Rect
            {
                X = value.X,
                Y = value.Y,
                Width = value.Width,
                Height = value.Height
            };
        }

        public bool IsPrimary
        {
            get { return this.screen.Primary; }
        }

        public string DeviceName
        {
            get { return this.screen.DeviceName; }
        }
    }
}
