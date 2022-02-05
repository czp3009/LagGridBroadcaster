using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Profiler.Basics;
using Profiler.Core;
using Sandbox;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API.Managers;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace LagGridBroadcaster
{
    // ReSharper disable once StringLiteralTypo
    [Category("laggrids")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    // ReSharper disable once UnusedType.Global
    public class LagGridBroadcasterCommands : CommandModule
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private LagGridBroadcasterPlugin Plugin => (LagGridBroadcasterPlugin)Context.Plugin;
        private LagGridBroadcasterConfig Config => Plugin.Config;

        [Command("help", "Show help message")]
        [Permission(MyPromoteLevel.None)]
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public void Help()
        {
            var s = string.Join(Environment.NewLine, "Available commands:",
                "!laggrids help - Show help message",
                "!laggrids send [seconds] - Send the top X most lag grids to all players",
                "!laggrids list - List latest measure results",
                "!laggrids get - Get latest result of the grid you're currently controlling",
                "!laggrids cleangps - Cleans GPS markers created by LagGridBroadcaster"
            );
            Context.Respond(s);
        }

        /// <summary>
        /// Send the top X most lag grids to all players
        /// </summary>
        /// <param name="seconds">Measure how many seconds</param>
        [Command("send", "Send the top X most lag grids to all players")]
        [Permission(MyPromoteLevel.Admin)]
        public void Send(uint seconds = 15)
        {
            //validate
            if (seconds == 0)
            {
                Context.Respond("seconds must greater than zero");
                return;
            }

            async void ProfileAndCatch()
            {
                try
                {
                    var mask = new GameEntityMask();
                    using (var profiler = new GridProfiler(mask))
                    using (ProfilerResultQueue.Profile(profiler))
                    {
                        Context.Respond($"Started profiling grids, result in {seconds}s");

                        var startTick = MySandboxGame.Static.SimulationFrameCounter;
                        profiler.MarkStart();
                        await Task.Delay(TimeSpan.FromSeconds(seconds));

                        var result = profiler.GetResult();
                        var endTick = MySandboxGame.Static.SimulationFrameCounter;
                        var ticks = endTick - startTick;
                        Context.Respond($"Profiling finish in {ticks}ticks");
                        CleanGps();
                        OnProfilerRequestFinished(result, ticks);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Context.Respond($"Error occured: {e.Message}");
                }
            }

            ProfileAndCatch();
        }

        [Command("list", "List latest measure results")]
        [Permission(MyPromoteLevel.None)]
        public void List()
        {
            var hasResult = Plugin.LatestMeasureTime != null;
            var subtitle = hasResult ? $"{Plugin.LatestMeasureTime.ToString()} UTC" : "no results";
            var content = hasResult
                ? string.Join(Environment.NewLine, Plugin.LatestResults.Select(it => FormatResult(it.Value)))
                : string.Empty;
            if (Context.Player == null)
            {
                Context.Respond($"{subtitle}{Environment.NewLine}{content}");
                return;
            }

            SendDialog("Latest measure result", subtitle, content, Context.Player.SteamUserId);
        }

        [Command("get", "Get latest result of the grid you're currently controlling")]
        [Permission(MyPromoteLevel.None)]
        public void Get()
        {
            var entity = Context.Player?.Controller?.ControlledEntity?.Entity;
            if (entity == null)
            {
                Context.Respond("You must have a controlled entity to use this command");
                return;
            }

            if (!TryGetControllingGrid(entity, out var grid))
            {
                Context.Respond("You must be controlling a grid to use this command");
                return;
            }

            var result = Plugin.LatestResults?.GetValueOrDefault(grid.EntityId);
            if (result == null)
            {
                Context.Respond("No result for this grid");
                return;
            }

            Context.Respond(FormatResult(result));
        }

        // ReSharper disable once StringLiteralTypo
        [Command("cleangps", "Cleans GPS markers created by LagGridBroadcaster")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once MemberCanBePrivate.Global
        public void CleanGps()
        {
            if (MyAPIGateway.Session == null) return;
            Plugin.AddedGps.ForEach(pair =>
            {
                var (identityId, gpsList) = pair;
                MyAPIGateway.Session.GPS.GetGpsList(identityId).Intersect(gpsList, new GpsComparer())
                    .Where(it => it.DiscardAt != null)
                    .ForEach(it => MyAPIGateway.Session.GPS.RemoveGps(identityId, it));
                Plugin.AddedGps.Remove(identityId);
            });
        }

        private void OnProfilerRequestFinished(BaseProfilerResult<MyCubeGrid> result, ulong ticks)
        {
            var measureResults = result.MapKeys(myCubeGrid =>
                    MyCubeGridGroups.Static.Logical.GetGroupNodes(myCubeGrid).MaxBy(it => it.BlocksPCU)
                ).GetTopEntities()
                .Where(it => it.Entity.MainThreadTime != 0)
                .Select(it => new MeasureResult(it.Key, it.Entity, ticks))
                .Where(it => it.PlayerIdentityId != 0)
                .OrderByDescending(it => it.MainThreadTimePerTick)
                .ToList();
            var entityIdToResult = measureResults.ToDictionary(it => it.EntityId, it => it);
            Plugin.LatestResults = entityIdToResult;
            var now = DateTime.UtcNow;
            Plugin.LatestMeasureTime = now;

            //write to file
            if (Config.WriteToFile)
            {
                var resultFileName = Config.ResultFileName;
                if (resultFileName.Length == 0) resultFileName = LagGridBroadcasterConfig.ResultFileDefaultName;
                new Persistent<MeasureResultsAndTime>(
                    Path.Combine(Plugin.StoragePath, resultFileName),
                    new MeasureResultsAndTime(measureResults, now)
                ).Save();
                Log.Info("Measure results saved to file");
            }

            //global top x grids
            var noOutputWhileEmptyResult = Config.NoOutputWhileEmptyResult;
            var top = (int)Config.Top;
            if (top != 0)
            {
                var minMs = Config.MinMs;
                var minUs = Config.MinUs;
                var factionMemberDistance = Config.FactionMemberDistance;
                var globalTopResults = measureResults.Where(it => it.MainThreadTimePerTick >= minMs)
                    .Where(measureResult =>
                    {
                        if (factionMemberDistance == 0) return true;
                        //any faction member close to this grid
                        var factionMembers = measureResult.FactionId == null
                            ? new List<MyPlayer> { GetPlayerById(measureResult.PlayerIdentityId) }
                            : MySession.Static.Factions[measureResult.FactionId.Value].Members.Keys
                                .Select(GetPlayerById);
                        return factionMembers.Where(it => it != null).Any(it => //only online faction member
                            Vector3.Distance(measureResult.EntityCoords, it.GetPosition()) <= factionMemberDistance
                        );
                    })
                    .Take(top)
                    .ToList();
                if (globalTopResults.Count != 0 || !noOutputWhileEmptyResult)
                {
                    // ReSharper disable once UseStringInterpolation
                    SendMessage(string.Format("Global top {0} grids{1}:",
                        globalTopResults.Count == top ? top.ToString() : $"{globalTopResults.Count}/{Config.Top}",
                        minUs == 0 ? "" : $"(over {minUs}us)"
                    ));
                    globalTopResults.ForEach(it =>
                    {
                        //send chat message to all player
                        SendMessage(FormatResult(it));
                        //send gps to all players
                        Broadcast(it);
                    });
                }
            }

            //send factionTop to faction members
            var factionTop = (int)Config.FactionTop;
            if (factionTop != 0)
            {
                var playerIdentityIdToResult = new Dictionary<long, List<MeasureResult>>();
                var factionIdToResult = new Dictionary<long, List<MeasureResult>>();
                measureResults.ForEach(it =>
                {
                    if (it.FactionId == null)
                    {
                        playerIdentityIdToResult.AddOrUpdateList(it.PlayerIdentityId, it);
                    }
                    else
                    {
                        factionIdToResult.AddOrUpdateList(it.FactionId.Value, it);
                    }
                });

                MySession.Static.Players.GetOnlinePlayers()
                    .Where(it => it.IsRealPlayer)
                    .ForEach(player =>
                    {
                        var playerIdentityId = player.Identity.IdentityId;
                        var faction = MySession.Static.Factions.GetPlayerFaction(playerIdentityId);
                        IEnumerable<MeasureResult> factionTopResultsEnumerable;
                        if (faction == null) //player not join faction
                        {
                            factionTopResultsEnumerable = playerIdentityIdToResult.GetValueOrDefault(playerIdentityId)
                                ?.Take(factionTop);
                        }
                        else
                        {
                            factionTopResultsEnumerable = factionIdToResult.GetValueOrDefault(faction.FactionId)
                                ?.Take(factionTop);
                        }

                        var factionTopResults = factionTopResultsEnumerable?.ToList() ?? new List<MeasureResult>();
                        if (factionTopResults.Count == 0 && noOutputWhileEmptyResult) return;

                        var playerSteamId = player.Id.SteamId;
                        // ReSharper disable once UseStringInterpolation
                        SendMessage(string.Format("Faction top {0} grids:",
                            factionTopResults.Count < factionTop
                                ? $"{factionTopResults.Count}/{factionTop}"
                                : factionTop.ToString()
                        ), playerSteamId);
                        factionTopResults.ForEach(it =>
                            SendMessage(FormatResult(it), playerSteamId)
                        );
                    });
            }

            //send result of controlling grid to player
            if (Config.SendResultOfControllingGrid)
            {
                MySession.Static.Players.GetOnlinePlayers()
                    .Where(it => it.IsRealPlayer)
                    .ForEach(player =>
                    {
                        var entity = player?.Controller?.ControlledEntity?.Entity;
                        if (entity == null) return;
                        if (!TryGetControllingGrid(entity, out var grid)) return;
                        var measureResult = entityIdToResult.GetValueOrDefault(grid.EntityId);
                        if (measureResult == null) return;
                        SendMessage(
                            $"Your current controlling grid '{measureResult.EntityDisplayName}' took {FormatTime(measureResult.MainThreadTimePerTick)}",
                            player.Id.SteamId
                        );
                    });
            }
        }

        private void Broadcast(MeasureResult result)
        {
            if (MyAPIGateway.Session == null) return;
            var gpsName = FormatResult(result);
            var gps = new MyGps(new MyObjectBuilder_Gps.Entry
            {
                name = gpsName,
                DisplayName = gpsName,
                // ReSharper disable once PossibleInvalidOperationException
                coords = result.EntityCoords,
                showOnHud = true,
                color = Color.Purple,
                description = FormatResult(result),
                entityId = 0,
                isFinal = false
            });
            //to all online players
            MySession.Static.Players.GetOnlinePlayers().Where(it => it.IsRealPlayer)
                .ForEach(it =>
                {
                    var identityId = it.Identity.IdentityId;
                    MyAPIGateway.Session.GPS.AddGps(identityId, gps);
                    Plugin.AddedGps.AddOrUpdateList(identityId, gps);
                });
        }

        private void SendMessage(string message, ulong targetSteamId = 0)
        {
            Context.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>()
                ?.SendMessageAsOther("LagGridBroadcaster", message, Color.Red, targetSteamId);
        }

        private static void SendNotification(string message, int disappearTimeMs = 10000)
        {
            ModCommunication.SendMessageToClients(new NotificationMessage(message, disappearTimeMs, "Red"));
        }

        private static void SendNotificationTo(string message, ulong target, int disappearTimeMs = 10000)
        {
            ModCommunication.SendMessageTo(new NotificationMessage(message, disappearTimeMs, "Red"), target);
        }

        private static void SendDialog(string title, string subtitle, string content, ulong target)
        {
            ModCommunication.SendMessageTo(new DialogMessage(title, subtitle, content), target);
        }

        private static bool TryGetPlayerById(long id, out MyPlayer myPlayer)
        {
            if (MySession.Static.Players.TryGetPlayerId(id, out var playerId) &&
                MySession.Static.Players.TryGetPlayerById(playerId, out var player))
            {
                myPlayer = player;
                return true;
            }

            myPlayer = null;
            return false;
        }

        private static MyPlayer GetPlayerById(long id)
        {
            TryGetPlayerById(id, out var myPlayer);
            return myPlayer;
        }

        private static string FormatResult(MeasureResult result)
        {
            return $"{result.EntityDisplayName} ({FormatTime(result.MainThreadTimePerTick)})";
        }

        private static bool TryGetControllingGrid(IMyEntity entity, out MyCubeGrid grid)
        {
            MyCubeGrid myCubeGrid;
            var temp = entity;
            do
            {
                myCubeGrid = temp as MyCubeGrid;
                if (myCubeGrid != null)
                    break;
                temp = temp.Parent;
            } while (temp != null);

            if (myCubeGrid != null)
            {
                grid = myCubeGrid;
                return true;
            }

            grid = null;
            return false;
        }

        private static string FormatTime(double ms)
        {
            if (ms > 1000)
                return $"{ms / 1000:F3}s";
            if (ms > 1)
                return $"{ms:F3}ms";
            ms *= 1000;
            if (ms >= 1)
                return $"{ms:F0}us";
            ms *= 1000;
            return $"{ms:F0}ns";
        }
    }
}