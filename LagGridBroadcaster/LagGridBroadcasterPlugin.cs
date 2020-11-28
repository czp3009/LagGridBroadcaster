using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using VRage.Game.ModAPI;

namespace LagGridBroadcaster
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LagGridBroadcasterPlugin : TorchPluginBase, IWpfPlugin
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public readonly ConcurrentDictionary<long, List<IMyGps>> AddedGps =
            new ConcurrentDictionary<long, List<IMyGps>>();

        private Persistent<LagGridBroadcasterConfig> _config;

        private LagGridBroadcasterControl _control;

        public DateTime? LatestMeasureTime = null;

        //entityId to result
        public Dictionary<long, MeasureResult> LatestResults = null;
        public LagGridBroadcasterConfig Config => _config.Data;
        public UserControl GetControl() => _control ?? (_control = new LagGridBroadcasterControl(this));

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();
        }

        private void SetupConfig()
        {
            var configFilePath = Path.Combine(StoragePath, $"{Name}.cfg");
            _config = Persistent<LagGridBroadcasterConfig>.Load(configFilePath);
        }

        public void Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }
    }
}