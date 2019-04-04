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
    /// Interaction logic for LicenseInputWindow.xaml
    /// </summary>
    public partial class LicenseInputWindow : Window
    {
        public LicenseInputWindow()
        {
            InitializeComponent();
        }

        internal License LastLicenseCheck { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (LastLicenseCheck != null)
            {
                // trial button visibility
                if (LastLicenseCheck.IsValid && LastLicenseCheck.IsValid)
                    cntTrialBtn.Visibility = Visibility.Visible;
                else cntTrialBtn.Visibility = Visibility.Collapsed;

                // License status
                string expiresAtDisplay = LastLicenseCheck.ExpiresAt.HasValue ? LastLicenseCheck.ExpiresAt.Value.ToString("yyyy-MM-dd") : "Never";
                statusMsg.Text = $"{LastLicenseCheck.LicenseStatusMessage} (Expires at {expiresAtDisplay})";
            }
        }

        private void TryKey(string key)
        {
            License lic = new License(key);
            if (lic.Validate())
            {
                lic.Save(); // Save the key to registry
                App.StartApplication(lic);
                Close();
            }
            else
            {
                statusMsg.Text = lic.LicenseStatusMessage;
            }
        }

        private void SetLicBtn_Click(object sender, RoutedEventArgs e)
        {
            TryKey(licenseKeyInputTxt.Text);
        }

        private void BuyBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://multitablepro.com/store/multitable-pro");
        }

        private void CntTrialBtn_Click(object sender, RoutedEventArgs e)
        {
            TryKey("TRIAL");
        }
    }
}
