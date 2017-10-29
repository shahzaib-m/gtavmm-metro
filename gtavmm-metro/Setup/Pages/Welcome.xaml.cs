using System.Windows;
using System.Windows.Controls;

namespace gtavmm_metro.Setup.Pages
{
    /// <summary>
    /// Interaction logic for Welcome.xaml
    /// </summary>
    public partial class Welcome : UserControl
    {
        private SetupMainWindow ParentWindow { get; set; }

        public Welcome(SetupMainWindow parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.SetupContainer.Content = this.ParentWindow.GTAVDirectoryPage;
        }
    }
}
