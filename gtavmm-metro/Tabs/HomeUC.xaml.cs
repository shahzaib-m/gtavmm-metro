using System.Windows.Controls;

using gtavmm_metro.Tabs.HomeUCTabs;

namespace gtavmm_metro.Tabs
{
    /// <summary>
    /// Interaction logic for HomeUC.xaml
    /// </summary>
    public partial class HomeUC : UserControl
    {
        private StoryModeUC StoryModeUserControl;
        private OnlineUC OnlineUserControl;

        public HomeUC()
        {
            InitializeComponent();
            this.AssignUCToTabs();
        }

        private void AssignUCToTabs()
        {
            this.StoryModeUserControl = new StoryModeUC(this);
            this.GTAVTabItem.Content = this.StoryModeUserControl;

            this.OnlineUserControl = new OnlineUC(this);
            this.GTAOnlineTabItem.Content = this.OnlineUserControl;
        }
    }
}
