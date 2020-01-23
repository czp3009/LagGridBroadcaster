using System;
using System.IO;
using System.Windows.Controls;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;

namespace LagGridBroadcaster
{
    public class LagGridBroadcasterPlugin : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private LagGridBroadcasterControl _control;
        public UserControl  GetControl() => _control ?? (_control = new LagGridBroadcasterControl(this));

        private Persistent<LagGridBroadcasterConfig> _config;
        public LagGridBroadcasterConfig Config => _config.Data;

        private void SetupConfig() {
            var configFile = Path.Combine(StoragePath, $"{Name}.cfg");
            _config = Persistent<LagGridBroadcasterConfig>.Load(configFile);
        }
        
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();
        }

        public void Save()
        {
            try {
                _config.Save();
                Log.Info("Configuration Saved.");
            } catch (IOException e) {
                Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}