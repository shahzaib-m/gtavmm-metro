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
            this.StoryModeUserControl = new StoryModeUC(this, scriptModsUC, assetModsUC);
            this.GTAVTabItem.Content = this.StoryModeUserControl;

            this.OnlineUserControl = new OnlineUC(this);
            this.GTAOnlineTabItem.Content = this.OnlineUserControl;
        }
    }
}
