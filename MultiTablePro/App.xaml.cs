using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MultiTablePro.Data;
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

            // Notify application started
            App.Current.Properties["IsRunning"] = true;
            Logger.Log("--- Starting application ---");

            // Load config & install on first run
            // Config needs to be initialized before the license check
            Config.Active = Config.FromFile();

            // todo: RequestLicenseKeyWindow call goes here

            // Show MainWindow (todoDelete this when License Check window exists)
            MainWindow win = new MainWindow();
            win.Show();
        }

        
    }
}
