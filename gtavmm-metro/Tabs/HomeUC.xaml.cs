using System.Windows.Controls;

using gtavmm_metro.Tabs.HomeUCTabs;

namespace gtavmm_metro.Tabs
{
    public partial class HomeUC : UserControl
    {
        public StoryModeUC StoryModeUserControl;
        public OnlineUC OnlineUserControl;

        public HomeUC(ScriptModsUC scriptModsUC, AssetModsUC assetModsUC)
        {
            InitializeComponent();
            this.AssignUCToTabs(scriptModsUC, assetModsUC);
        }

        private void AssignUCToTabs(ScriptModsUC scriptModsUC, AssetModsUC assetModsUC)
        {
            this.StoryModeUserControl = new StoryModeUC(scriptModsUC, assetModsUC);
            this.GTAVTabItem.Content = this.StoryModeUserControl;
            this.StoryModeUserControl.TabCollapseRequested += (s, e) => CollapseTab();

            this.OnlineUserControl = new OnlineUC();
            this.GTAOnlineTabItem.Content = this.OnlineUserControl;
            this.OnlineUserControl.TabCollapseRequested += (s, e) => CollapseTab();
        }

        private void CollapseTab()
        {
            this.BlankTabItem.IsSelected = true;
        }
    }
}
