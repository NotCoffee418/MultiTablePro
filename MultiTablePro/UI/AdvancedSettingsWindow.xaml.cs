using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using MultiTablePro.Data;

namespace MultiTablePro.UI
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsWindow.xaml
    /// </summary>
    public partial class AdvancedSettingsWindow : Window
    {
        public AdvancedSettingsWindow()
        {
            InitializeComponent();
            DataContext = Config.Active;
        }

        private void OpenLocksDirectory_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(System.IO.Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData), "MultiTablePro"));
        }
    }
}
