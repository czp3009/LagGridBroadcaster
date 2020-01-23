using System;
using System.Linq;
using Profiler.Core;
using Sandbox.Game.Entities;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace LagGridBroadcaster
{
    [Category("laggrids")]
    public class LagGridBroadcasterCommands : CommandModule
    {
        private LagGridBroadcasterConfig Config => ((LagGridBroadcasterPlugin) Context.Plugin).Config;

        /// <summary>
        /// Send GPS of top X most lag grids to all players
        /// </summary>
        /// <param name="ticks">Measure how many ticks</param>
        [Command("send", "Sends the top X most lag grids to all players")]
        [Permission(MyPromoteLevel.Admin)]
        public void Send(ulong ticks = 900)
        {
            
        }

        private static string FormatTime(double ms)
        {
            if (ms > 1000)
                return $"{ms / 1000:F3}s";
            if (ms > 1)
                return $"{ms:F3}ms";
            ms *= 1000;
            if (ms > 1)
                return $"{ms:F0}us";
            ms *= 1000;
            return $"{ms:F0}ns";
        }
    }
}