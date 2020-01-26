using System.Windows;
using System.Windows.Controls;

namespace LagGridBroadcaster
{
    public partial class LagGridBroadcasterControl : UserControl
    {
        private readonly LagGridBroadcasterPlugin _plugin;

        private LagGridBroadcasterControl()
        {
            InitializeComponent();
        }

        public LagGridBroadcasterControl(LagGridBroadcasterPlugin plugin) : this()
        {
            _plugin = plugin;
            DataContext = plugin.Config;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            _plugin.Save();
        }
    }
}