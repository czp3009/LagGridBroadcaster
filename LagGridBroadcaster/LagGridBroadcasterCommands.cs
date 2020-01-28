using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using Profiler.Core;
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
    // ReSharper disable once UnusedType.Global
    public class LagGridBroadcasterCommands : CommandModule
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private LagGridBroadcasterPlugin Plugin => (LagGridBroadcasterPlugin) Context.Plugin;
        private LagGridBroadcasterConfig Config => Plugin.Config;

        /// <summary>
        /// Send the top X most lag grids to all players
        /// </summary>
        /// <param name="ticks">Measure how many ticks</param>
        [Command("send", "Send the top X most lag grids to all players")]
        [Permission(MyPromoteLevel.Admin)]
        // ReSharper disable once UnusedMember.Global
        public void Send(ulong ticks = 900)
        {
            //validate
            if (ticks == 0)
            {
                Context.Respond("ticks must greater than zero");
                return;
            }

            //change mask
            if (!ProfilerDataProxy.ChangeMask(null, null, null, MyModContext.BaseGame))
            {
                Context.Respond("Failed to change profiling mask.  There can only be one.");
                return;
            }

            //send request
            CleanGps();
            var profilerRequest = new ProfilerRequest(ProfilerRequestType.Grid, ticks);
            profilerRequest.OnFinished += (_, results) =>
            {
                try
                {
                    OnProfilerRequestFinished(results);
                }
                catch (Exception e)
                {
                    Context.Respond($"Error occured: {e.Message}");
                    Log.Error(e);
                }
            };
            Context.Respond($"Measure {ticks} ticks");
            ProfilerDataProxy.Submit(profilerRequest);
        }

        [Command("list", "List latest measure results")]
        [Permission(MyPromoteLevel.None)]
        // ReSharper disable once UnusedMember.Global
        public void List()
        {
            var subtitle = Plugin.LatestMeasureTime != null
                ? $"{Plugin.LatestMeasureTime.ToString()} UTC"
                : "no results";
            var content = string.Join(Environment.NewLine, Plugin.LatestResults.Select(it => FormatResult(it.Item2)));
            if (Context.Player == null)
            {
                Context.Respond($"{subtitle}{Environment.NewLine}{content}");
                return;
            }

            SendDialog("Latest measure result", subtitle, content, Context.Player.SteamUserId);
        }

        [Command("get", "Get latest result of the grid you're currently controlling")]
        [Permission(MyPromoteLevel.None)]
        // ReSharper disable once UnusedMember.Global
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

            var result = Plugin.LatestResults.FirstOrDefault(it => it.Item1 == grid.EntityId).Item2;
            if (result.Equals(default(ProfilerRequest.Result)))
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
                lock (gpsList)
                {
                    MyAPIGateway.Session.GPS.GetGpsList(identityId).Intersect(gpsList, new GpsComparer())
                        .Where(it => it.DiscardAt != null)
                        .ForEach(it => MyAPIGateway.Session.GPS.RemoveGps(identityId, it));
                    gpsList.Clear();
                }
            });
        }

        private void OnProfilerRequestFinished(IEnumerable<ProfilerRequest.Result> results)
        {
            var tuples = results.Where(it => it.MsPerTick > 0)
                .Where(it => it.Description != null) //i don't know why description can be null
                .Where(it => it.Position != null) //position may be null but i can't understand
                .Select(result =>
                {
                    var entityId = Convert.ToInt64(result.Description.SubstringAfter('='));
                    var grid = MyEntities.GetEntityById(entityId) as MyCubeGrid; //maybe null if entityId is 0
                    var gridOwner = grid?.BigOwners.FirstOrDefault(it => it != 0) ?? 0; //maybe 0
                    var identity = MySession.Static.Players.TryGetIdentity(gridOwner); //maybe null if gridOwner is 0
                    TryGetPlayerById(gridOwner, out var player); //may be null if gridOwner is 0
                    //maybe null if gridOwner is 0 or player not join faction
                    var faction = MySession.Static.Factions.GetPlayerFaction(gridOwner);
                    return (result, entityId, grid, gridOwner, identity, player, faction);
                })
                .Where(it => it.entityId != 0)
                .ToArray();
            var now = DateTime.UtcNow;
            Plugin.LatestResults = tuples.Select(it => (it.entityId, it.result)).ToArray();
            Plugin.LatestMeasureTime = now;

            //write to file
            if (Config.WriteToFile)
            {
                var measureResults = tuples.Select(it =>
                {
                    var (result, entityId, _, gridOwner, identity, _, faction) = it;
                    return new MeasureResult
                    {
                        EntityId = entityId, EntityName = result.Name, MsPerTick = result.MsPerTick,
                        PlayerId = gridOwner, PlayerDisplayName = identity?.DisplayName,
                        FactionId = faction?.FactionId, FactionName = faction?.Name
                    };
                }).ToArray();
                var measureResultsAndTime = new MeasureResultsAndTime
                {
                    MeasureResults = measureResults, DateTime = now
                };
                var persistent = new Persistent<MeasureResultsAndTime>(
                    Path.Combine(
                        Plugin.StoragePath,
                        Config.ResultFileName.Length != 0
                            ? Config.ResultFileName
                            : LagGridBroadcasterConfig.ResultFileDefaultName
                    ),
                    measureResultsAndTime
                );
                //simply run in thread pool
                Task.Run(() => persistent.Save());
            }

            var prepareToBroadcast = new List<ProfilerRequest.Result>();
            //send globalTop to all players
            if (Config.Top != 0)
            {
                var globalTop = tuples.Where(it => it.result.MsPerTick > Config.MinMs)
                    .Where(tuple =>
                    {
                        var distance = Config.FactionMemberDistance;
                        var (result, _, _, gridOwner, _, player, faction) = tuple;
                        if (distance == 0 || gridOwner == 0) return true;
                        //player not join faction
                        if (faction == null)
                            // ReSharper disable once PossibleInvalidOperationException
                            return player != null &&
                                   Vector3.Distance(result.Position.Value, player.GetPosition()) < distance;
                        //npc faction
                        if (faction.FactionType != MyFactionTypes.PlayerMade) return true;
                        return faction.Members.Keys.Any(it =>
                            TryGetPlayerById(it, out var outPlayer) &&
                            // ReSharper disable once PossibleInvalidOperationException
                            Vector3.Distance(result.Position.Value, outPlayer.GetPosition()) < distance
                        );
                    })
                    .Take((int) Config.Top)
                    .ToArray();
                // ReSharper disable once UseStringInterpolation
                SendMessage(string.Format("Global top {0} grids{1}:",
                    globalTop.Length == Config.Top ? Config.Top.ToString() : $"{globalTop.Length}/{Config.Top}",
                    Config.MinUs == 0 ? "" : $"(over {Config.MinUs}us)"
                ));
                globalTop.ForEach(it => SendMessage(FormatResult(it.result)));
                prepareToBroadcast.AddRange(globalTop.Select(it => it.result));
            }

            //send factionTop to faction members
            if (Config.FactionTop != 0)
            {
                var factionResults = new Dictionary<long, List<ProfilerRequest.Result>>(); //factionId to results
                var noFactionResults = new Dictionary<long, List<ProfilerRequest.Result>>(); //playerId to results
                tuples.Where(it => it.gridOwner != 0) //except Nobody' grid
                    .ForEach(tuple =>
                    {
                        var (result, _, _, gridOwner, _, _, faction) = tuple;
                        if (faction != null)
                            factionResults.AddOrUpdateList(faction.FactionId, result);
                        else
                            noFactionResults.AddOrUpdateList(gridOwner, result);
                    });
                MySession.Static.Players.GetOnlinePlayers().Where(it => it.IsRealPlayer)
                    .ForEach(player =>
                    {
                        var playerId = player.Identity.IdentityId;
                        var faction = MySession.Static.Factions.GetPlayerFaction(playerId);
                        //player may don't have any grid
                        var factionTopResults =
                            faction != null
                                ? factionResults.GetValueOrDefault(faction.FactionId) ??
                                  Enumerable.Empty<ProfilerRequest.Result>().ToList()
                                : noFactionResults.GetValueOrDefault(playerId) ??
                                  Enumerable.Empty<ProfilerRequest.Result>().ToList();
                        var steamId = player.Id.SteamId;
                        // ReSharper disable once UseStringInterpolation
                        SendMessage(string.Format("Faction top {0} grids:",
                            factionTopResults.Count < Config.FactionTop
                                ? $"{factionTopResults.Count}/{Config.FactionTop}"
                                : Config.FactionTop.ToString()
                        ), steamId);
                        factionTopResults.Take((int) Config.FactionTop).ForEach(it =>
                            SendMessage(FormatResult(it), steamId)
                        );
                        if (Config.MinUs != 0)
                        {
                            factionTopResults
                                .Where(it => it.MsPerTick < Config.MinMs && it.MsPerTick > Config.MinMs * 0.75)
                                .ForEach(it => SendNotificationTo(
                                    $"Grid '{it.Name}'({FormatTime(it.MsPerTick)}) in your faction very close to server limit({FormatTime(Config.PlayerMinMs)})",
                                    steamId
                                ));
                        }
                    });
            }

            //broadcast player's most lag grid
            if (Config.PlayerMinUs != 0)
            {
                var playerResultsLoopUp = tuples.Where(it => it.gridOwner != 0)
                    .ToLookup(it => it.gridOwner, it => it.result);
                var needBroadcast = new List<(MyPlayer, ProfilerRequest.Result)>();
                MySession.Static.Players.GetOnlinePlayers().Where(it => it.IsRealPlayer)
                    .ForEach(player =>
                    {
                        var playerResults = playerResultsLoopUp[player.Identity.IdentityId].ToArray();
                        var totalMs = playerResults.Sum(it => it.MsPerTick);
                        var steamId = player.Id.SteamId;
                        SendMessage(
                            $"Your grids total took {FormatTime(totalMs)}(server limit {FormatTime(Config.PlayerMinMs)})",
                            steamId
                        );
                        // ReSharper disable once InvertIf
                        if (totalMs > Config.PlayerMinMs && playerResults.Length > 0)
                        {
                            var mostLagGrid = playerResults[0];
                            SendMessage($"Your most lag grid '{mostLagGrid.Name}' will be broadcast", steamId);
                            needBroadcast.Add((player, mostLagGrid));
                        }
                    });
                needBroadcast.ForEach(tuple =>
                {
                    var (player, result) = tuple;
                    SendMessage(
                        $"Player '{player.DisplayName}' exceeded server limit, following grid will be broadcast:");
                    SendMessage(FormatResult(result));
                });
                prepareToBroadcast.AddRange(needBroadcast.Select(it => it.Item2));
            }

            var distinctPrepareToBroadcast = prepareToBroadcast.Distinct(new ResultComparer()).ToArray();
            if (distinctPrepareToBroadcast.Length != 0)
                SendNotification($"Total {distinctPrepareToBroadcast.Length} grids being broadcast");
            distinctPrepareToBroadcast.ForEach(Broadcast);
        }

        private void Broadcast(ProfilerRequest.Result result)
        {
            if (MyAPIGateway.Session == null) return;
            var gpsName = FormatResult(result);
            var gps = new MyGps(new MyObjectBuilder_Gps.Entry
            {
                name = gpsName,
                DisplayName = gpsName,
                // ReSharper disable once PossibleInvalidOperationException
                coords = result.Position.Value,
                showOnHud = true,
                color = Color.Purple,
                description = $"{result.Description} ({result.HitsPerTick:F1}{result.HitsUnit})",
                entityId = 0,
                isFinal = false
            });
            MySession.Static.Players.GetOnlinePlayers().Where(it => it.IsRealPlayer)
                .ForEach(it =>
                {
                    var identityId = it.Identity.IdentityId;
                    MyAPIGateway.Session.GPS.AddGps(identityId, gps);
                    var list = Plugin.AddedGps.GetOrAdd(identityId, _ => new List<IMyGps>());
                    lock (list) list.Add(gps);
                });
        }

        private void SendMessage(string message, ulong targetSteamId = 0)
        {
            Context.Torch.CurrentSession.Managers.GetManager<IChatManagerServer>()
                ?.SendMessageAsOther(null, message, null, targetSteamId);
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

        private static string FormatResult(ProfilerRequest.Result result)
        {
            return $"{result.Name} ({FormatTime(result.MsPerTick)})";
        }

        private static bool TryGetControllingGrid(IMyEntity entity, out MyCubeGrid grid)
        {
            MyCubeGrid myCubeGrid;
            var tmp = entity;
            do
            {
                myCubeGrid = tmp as MyCubeGrid;
                if (myCubeGrid != null)
                    break;
                tmp = tmp.Parent;
            } while (tmp != null);

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
            if (ms >= 1000)
                return $"{ms / 1000:F3}s";
            if (ms >= 1)
                return $"{ms:F3}ms";
            ms *= 1000;
            if (ms >= 1)
                return $"{ms:F0}us";
            ms *= 1000;
            return $"{ms:F0}ns";
        }
    }
}