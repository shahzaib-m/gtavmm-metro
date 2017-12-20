using System;
using System.Reflection;
using System.Windows.Controls;

namespace gtavmm_metro.Tabs
{
    public partial class AboutUC : UserControl
    {
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

        public AboutUC()
        {
            InitializeComponent();
        }
    }
}
