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
    /// Interaction logic for ProfilePreviewControl.xaml
    /// </summary>
    public partial class ProfilePreviewControl : UserControl
    {
        internal ProfilePreviewControl(Profile profile)
        {
            InitializeComponent();
            _displayProfile = profile;
        }

        Profile _displayProfile;

        internal Profile DisplayProfile { get { return _displayProfile; } }
    }
}
