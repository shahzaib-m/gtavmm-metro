using System;
using System.Windows;
using System.Windows.Controls;

namespace gtavmm_metro.Setup.Pages
{
    public partial class WelcomeUC : UserControl
    {
        public event EventHandler GoForwardRequested;

        public WelcomeUC()
        {
            InitializeComponent();
        }

        private void GoForward_Click(object sender, RoutedEventArgs e) => GoForwardRequested?.Invoke(this, null);
    }
}
