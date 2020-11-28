// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Collections.Generic;
using System.Linq;
using Profiler.Basics;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRageMath;

namespace LagGridBroadcaster
{
    public class MeasureResult
    {
        public MeasureResult()
        {
        }

        public MeasureResult(MyCubeGrid myCubeGrid, ProfilerEntry profilerEntry, ulong ticks)
        {
            EntityId = myCubeGrid.EntityId;
            EntityDisplayName = myCubeGrid.DisplayName;
            EntityCoords = myCubeGrid.PositionComp.WorldAABB.Center;
            MainThreadTimePerTick = profilerEntry.MainThreadTime / ticks;
            PlayerIdentityId = myCubeGrid.BigOwners.FirstOrDefault(it => it != 0);
            PlayerSteamId = MySession.Static.Players.TryGetSteamId(PlayerIdentityId);
            PlayerDisplayName = MySession.Static.Players.TryGetIdentity(PlayerIdentityId)?.DisplayName ?? "Nobody";
            var faction = MySession.Static.Factions.GetPlayerFaction(PlayerIdentityId);
            FactionId = faction?.FactionId;
            FactionName = faction?.Name;
        }

        public long EntityId { get; set; }
        public string EntityDisplayName { get; set; }
        public Vector3D EntityCoords { get; set; }
        public double MainThreadTimePerTick { get; set; }
        public long PlayerIdentityId { get; set; }
        public ulong PlayerSteamId { get; set; }
        public string PlayerDisplayName { get; set; }
        public long? FactionId { get; set; }
        public string FactionName { get; set; }
    }

    public class MeasureResultsAndTime
    {
        public MeasureResultsAndTime()
        {
        }

        public MeasureResultsAndTime(List<MeasureResult> measureResults, DateTime dateTime)
        {
            MeasureResults = measureResults;
            DateTime = dateTime;
        }

        public List<MeasureResult> MeasureResults { get; set; }
        public DateTime DateTime { get; set; }
    }
}