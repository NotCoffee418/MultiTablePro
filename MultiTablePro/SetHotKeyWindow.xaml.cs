using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Threading;

namespace MultiTablePro
{
    /// <summary>
    /// Interaction logic for SetHotKeyWindow.xaml
    /// </summary>
    public partial class SetHotKeyWindow : Window
    {
        public SetHotKeyWindow()
        {
            InitializeComponent();
        }
        HotKey _currentHotKey = null;
        private HotKey hk;

        public string PropertyName { get; set; }
        internal HotKey CurrentHotKey {
            get {
                return _currentHotKey;
            }
            set {
                if (value == null)
                    _currentHotKey = new HotKey(Keys.A, ModifierKeys.None);
                else _currentHotKey = value;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Prepare the keys CB
            var _enumval = Enum.GetValues(typeof(Keys)).Cast<Keys>();
            keySelectionCb.ItemsSource = _enumval.ToList();

            // Set known values
            SetModifierUI(_currentHotKey.Modifier);
            SetKeyUI(_currentHotKey.Key);
        }

        private void SetKeyUI(Keys key)
        {
            keySelectionCb.SelectedIndex = keySelectionCb.Items.IndexOf(key);
        }

        private void SetModifierUI(ModifierKeys modifier)
        {
            switch (modifier)
            {
                case ModifierKeys.None:
                    noneRb.IsChecked = true;
                    break;
                case ModifierKeys.Alt:
                    altRb.IsChecked = true;
                    break;
                case ModifierKeys.Shift:
                    shiftRb.IsChecked = true;
                    break;
                case ModifierKeys.Control:
                    ctrlRb.IsChecked = true;
                    break;
            }
        }

        private void KeySelectionCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentHotKey.Key = (Keys)keySelectionCb.SelectedValue;
            Config.Active.RaisePropertyChanged(PropertyName);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void modifierRb_Checked(object sender, RoutedEventArgs e)
        {
            if (noneRb.IsChecked == true)
                CurrentHotKey.Modifier = ModifierKeys.None;
            else if (altRb.IsChecked == true)
                CurrentHotKey.Modifier = ModifierKeys.Alt;
            else if (shiftRb.IsChecked == true)
                CurrentHotKey.Modifier = ModifierKeys.Shift;
            else if (ctrlRb.IsChecked == true)
                CurrentHotKey.Modifier = ModifierKeys.Control;
            Config.Active.RaisePropertyChanged(PropertyName);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Config.Active.RaisePropertyChanged(PropertyName);
        }
    }
}
