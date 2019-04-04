using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MultiTablePro.Data;

namespace MultiTablePro.UI
{
    /// <summary>
    /// Interaction logic for ProfilePreviewControl.xaml
    /// </summary>
    public partial class ProfilePreviewControl : UserControl
    {
        public ProfilePreviewControl()
        {
            InitializeComponent();
        }

        Profile _displayProfile;

        internal Profile DisplayProfile {
            get { return _displayProfile; }
            set
            {
                _displayProfile = value;
                Refresh();
            }
        }


        private void Refresh()
        {
            // Determine ratio to draw to scale
            Rect workingArea = WpfScreen.GetWorkingAreaInfo();
            double padding = 10;
            double widthRatio = (Width - padding) / workingArea.Width;
            double heightRatio = (Height - padding) / workingArea.Height;
            double useRatio = widthRatio > heightRatio ? heightRatio : widthRatio;

            // Determine offsets (for multiscreens that start in the negative)
            // Happens when a monitor is to the left/top of the primary screen
            double xOffset = (workingArea.X * -1) + padding;
            double yOffset = (workingArea.Y * -1) + padding;

            // Clear any previous displays
            DrawArea.Children.Clear();

            // Draw slots
            DrawSlots(xOffset, yOffset, useRatio);

            // Draw screens
            DrawScreens(xOffset, yOffset, useRatio);
        }

        /// <summary>
        /// Draws a slot representation based on slot & ratio
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="sizeRatio"></param>
        private void DrawSlots(double xOffset, double yOffset, double sizeRatio)
        {
            var orderedSlots = DisplayProfile.Slots
                .OrderByDescending(s => s.Priority)
                .OrderBy(s => s.ActivityUse);
            foreach (Slot slot in orderedSlots)
            {
                Border result = new Border();
                result.BorderThickness = new Thickness(5);

                // Set border's color to identify the ActivityUse
                Color color = Color.FromRgb(255, 255, 255);
                switch (slot.ActivityUse)
                {
                    case Slot.ActivityUses.Active:
                        color = Color.FromRgb(0, 128, 0); // green
                        break;
                    case Slot.ActivityUses.Inactive:
                        color = Color.FromRgb(128, 0, 0); // green
                        break;
                    case Slot.ActivityUses.Aside:
                        color = Color.FromRgb(171, 145, 68); // brown/yellow
                        break;
                }
                result.BorderBrush = new SolidColorBrush(color);
                result.VerticalAlignment = VerticalAlignment.Top;
                result.HorizontalAlignment = HorizontalAlignment.Left;

                result.Margin = new Thickness(
                    (xOffset + slot.X) * sizeRatio, 
                    (yOffset + slot.Y) * sizeRatio, 
                    0, 0);
                result.Width = slot.Width * sizeRatio;
                result.Height = slot.Height * sizeRatio;

                DrawArea.Children.Add(result);
            }
        }


        private void DrawScreens(double xOffset, double yOffset, double sizeRatio)
        {
            foreach (var screen in WpfScreen.AllScreens())
            {
                Border result = new Border();
                result.BorderThickness = new Thickness(1);

                // Set border's color to identify the ActivityUse
                Color color = Color.FromRgb(255, 255, 255);
                result.BorderBrush = new SolidColorBrush(color);
                result.VerticalAlignment = VerticalAlignment.Top;
                result.HorizontalAlignment = HorizontalAlignment.Left;

                result.Margin = new Thickness(
                    (xOffset + screen.DeviceBounds.Left) * sizeRatio,
                    (yOffset + screen.DeviceBounds.Top) * sizeRatio,
                    0, 0);
                result.Width = screen.DeviceBounds.Width * sizeRatio;
                result.Height = screen.DeviceBounds.Height * sizeRatio;

                DrawArea.Children.Add(result);
            }
        }
    }
}
