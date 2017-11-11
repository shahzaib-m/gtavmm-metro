using System.Windows;
using System.Windows.Controls;

namespace gtavmm_metro.Tabs.HomeUCTabs
{
    /// <summary>
    /// Interaction logic for OnlineUC.xaml
    /// </summary>
    public partial class OnlineUC : UserControl
    {
        private HomeUC ParentWindow { get; set; }

        public OnlineUC(HomeUC parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }


        private void CollapseGTAOTabSection_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.BlankTabItem.IsSelected = true;
        }
    }
}
