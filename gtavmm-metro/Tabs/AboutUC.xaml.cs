using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace gtavmm_metro.Tabs
{
    /// <summary>
    /// Interaction logic for AboutUC.xaml
    /// </summary>
    public partial class AboutUC : UserControl
    {
        public AboutUC()
        {
            InitializeComponent();
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
    }
}
