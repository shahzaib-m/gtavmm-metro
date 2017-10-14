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
using MahApps.Metro.Controls;
using System.Threading;
using System.Windows.Media.Animation;
using System.Reflection;
using System.Diagnostics;
using System.Collections.ObjectModel;
using gtavmm_metro.Models;

namespace gtavmm_metro
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        ObservableCollection<Script_Mod> ScriptMods;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void MetroWindow_ContentRendered(object sender, EventArgs e)
        {
            await Init();   // initialize once the window is rendered
        }

        private async Task Init()
        {
            // perform all tasks (check files, load mods, etc.) here while the user is shown a progress ring
            await Task.Delay(1000);     // temp substitute for work delay

            await Task.Run(() => LoadScriptMods());
            await Task.Run(() => UserInteractionStartNow());    // enable the UI for the user when tasks finished.
        }

        private void LoadScriptMods()
        {
            ScriptMods = new ObservableCollection<Script_Mod>();
            // Dummy data
            ScriptMods.Add(new Script_Mod { Id = 1, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true });
            ScriptMods.Add(new Script_Mod { Id = 2, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false });
            ScriptMods.Add(new Script_Mod { Id = 1, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true });
            ScriptMods.Add(new Script_Mod { Id = 2, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false });
            ScriptMods.Add(new Script_Mod { Id = 1, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true });
            ScriptMods.Add(new Script_Mod { Id = 2, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false });
            ScriptMods.Add(new Script_Mod { Id = 1, Name = "MP Vehicles", Description = "Allows MP vehicles to be spawned in SP", IsEnabled = true });
            ScriptMods.Add(new Script_Mod { Id = 2, Name = "Turbo Boost", Description = "NFS like turbo boost for vehicles", IsEnabled = false });

            this.Dispatcher.Invoke(() =>    // needed as MainWindow control is being modified from a non-main thread
            {
                this.ScriptModsDataGrid.ItemsSource = ScriptMods;
            });
        }

        private void UserInteractionStartNow()
        {
            this.Dispatcher.Invoke(() =>    // needed as MainWindow control is being modified from a non-main thread
            {
                this.MainTabControl.IsEnabled = true;

                DoubleAnimation smooth_fadein = new DoubleAnimation(0.3, 1.0, new Duration(new TimeSpan(0, 0, 0, 0, 300)));
                this.MainTabControl.BeginAnimation(OpacityProperty, smooth_fadein);

                this.MainProgressRing.IsActive = false;
            });
        }

        public string AppNameAndVer
        {
            get
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                string major = version.Major.ToString();
                string minor = version.Minor.ToString();
                string build = version.Build.ToString();

                return String.Format("GTAV Mod Manager Metro {0}.{1}.{2}", major, minor, build);
            }
        }

        private void EditScriptModButton_Click(object sender, RoutedEventArgs e)
        {
            Script_Mod chosen_mod = ((FrameworkElement)sender).DataContext as Script_Mod; // the sender Script_Mod object from the datagrid
            MessageBox.Show("Sender Name: " + chosen_mod.Name);
        }
    }
}
