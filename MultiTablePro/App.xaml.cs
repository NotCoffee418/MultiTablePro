using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MultiTablePro.UI;

namespace MultiTablePro
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start the updater
            Updater.Run();

            // Show MainWindow
            MainWindow win = new MainWindow();
            win.Show();
        }

        
    }
}
