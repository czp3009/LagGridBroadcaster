﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using NLog;
using Profiler.Core;
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

        private Persistent<LagGridBroadcasterConfig> _config;
        public LagGridBroadcasterConfig Config => _config.Data;

        private LagGridBroadcasterControl _control;
        public UserControl GetControl() => _control ?? (_control = new LagGridBroadcasterControl(this));

        //entityId to result
        public (long,ProfilerRequest.Result)[] LatestResults = Array.Empty<(long,ProfilerRequest.Result)>();

        public DateTime? LatestMeasureTime = null;

        public readonly ConcurrentDictionary<long, ICollection<IMyGps>> AddedGps =
            new ConcurrentDictionary<long, ICollection<IMyGps>>();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            SetupConfig();
        }

        private void SetupConfig()
        {
            var configFile = Path.Combine(StoragePath, $"{Name}.cfg");
            _config = Persistent<LagGridBroadcasterConfig>.Load(configFile);
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