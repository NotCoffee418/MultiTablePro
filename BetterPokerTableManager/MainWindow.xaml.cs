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

namespace BetterPokerTableManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // debug
            // Start manual table finder before logs to ensure loghandler doesn't try to run commands on unknown tables (in case BPTM is started during a session)
            //PSLogHandler.Start();

            PSLogHandler.AnalyzeLine("table window 000E13DE has been destroyed");
        }
    }
}
