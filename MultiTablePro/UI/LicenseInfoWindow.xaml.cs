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
using System.Windows.Shapes;
using MultiTablePro.Data;

namespace MultiTablePro.UI
{
    /// <summary>
    /// Interaction logic for LicenseInfoWindow.xaml
    /// </summary>
    public partial class LicenseInfoWindow : Window
    {
        public LicenseInfoWindow()
        {
            InitializeComponent();
            DataContext = Config.Active.ActiveLicense;            
        }

        private void CopyLicenseBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(Config.Active.ActiveLicense.Key);
        }
    }
}
