using System.Windows;

namespace LagGridBroadcaster
{
    public partial class LagGridBroadcasterControl
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