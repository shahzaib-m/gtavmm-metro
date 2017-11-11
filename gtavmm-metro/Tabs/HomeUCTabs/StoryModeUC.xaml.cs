using System.Windows;
using System.Windows.Controls;

namespace gtavmm_metro.Tabs.HomeUCTabs
{
    /// <summary>
    /// Interaction logic for StoryModeUC.xaml
    /// </summary>
    public partial class StoryModeUC : UserControl
    {
        private HomeUC ParentWindow { get; set; }

        public StoryModeUC(HomeUC parent)
        {
            this.ParentWindow = parent;
            InitializeComponent();
        }


        private void CollapseGTAVTabSection_Click(object sender, RoutedEventArgs e)
        {
            this.ParentWindow.BlankTabItem.IsSelected = true;
        }
    }
}
