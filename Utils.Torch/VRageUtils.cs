﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Havok;
using NLog;
using Sandbox;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Utils.General;
using VRage;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Utils.Torch
{
    internal static class VRageUtils
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public static ulong CurrentGameFrameCount => MySandboxGame.Static.SimulationFrameCounter;

        public static string ToShortString(this Vector3D self)
        {
            return $"X:{self.X:0.0} Y:{self.Y:0.0} Z:{self.Z:0.0}";
        }

        public static MyFaction GetOwnerFactionOrNull(this MyFactionCollection self, IMyCubeGrid grid)
        {
            if (grid.BigOwners.TryGetFirst(out var ownerId))
            {
                return self.GetPlayerFaction(ownerId);
            }

            return null;
        }

        public static ulong SteamId(this MyPlayer p)
        {
            return p.Id.SteamId;
        }

        public static long PlayerId(this MyPlayer p)
        {
            return p.Identity.IdentityId;
        }

        public static MyCubeGrid GetTopGrid(this IEnumerable<MyCubeGrid> group)
        {
            return group.MaxBy(g => g.Mass);
        }

        public static ISet<long> BigOwnersSet(this IEnumerable<MyCubeGrid> group)
        {
            return new HashSet<long>(group.SelectMany(g => g.BigOwners));
        }

        public static bool TryGetPlayerById(this MyPlayerCollection self, long id, out MyPlayer player)
        {
            player = null;
            return self.TryGetPlayerId(id, out var playerId) &&
                   self.TryGetPlayerById(playerId, out player);
        }

        public static bool IsConcealed(this IMyEntity entity)
        {
            // Concealment plugin uses `4` as a flag to prevent game from updating grids
            return (long)(entity.Flags & (EntityFlags)4) != 0;
        }

        public static MyCubeGrid GetBiggestGrid(this IEnumerable<MyCubeGrid> grids)
        {
            var myCubeGrid = (MyCubeGrid)null;
            var num = 0.0;
            foreach (var grid in grids)
            {
                var volume = grid.PositionComp.WorldAABB.Size.Volume;
                if (volume > num)
                {
                    num = volume;
                    myCubeGrid = grid;
                }
            }

            return myCubeGrid;
        }

        public static bool OwnsAll(this IMyPlayer player, IEnumerable<MyCubeGrid> grids)
        {
            // ownership check
            foreach (var grid in grids)
            {
                if (!grid.BigOwners.Any()) continue;
                if (!grid.BigOwners.Contains(player.IdentityId)) return false;
            }

            return true;
        }

        public static bool IsAllActionAllowed(this MyEntity self)
        {
            return MySessionComponentSafeZones.IsActionAllowed(self, MySafeZoneAction.All);
        }

        public static bool IsNormalPlayer(this IMyPlayer onlinePlayer)
        {
            return onlinePlayer.PromoteLevel == MyPromoteLevel.None;
        }

        public static bool IsAdmin(this IMyPlayer onlinePlayer)
        {
            return onlinePlayer.PromoteLevel >= MyPromoteLevel.Admin;
        }

        public static ulong GetAdminSteamIds()
        {
            if (!MySandboxGame.ConfigDedicated.Administrators.TryGetFirst(out var adminSteamIdStr)) return 0L;
            if (!ulong.TryParse(adminSteamIdStr, out var adminSteamId)) return 0L;
            return adminSteamId;
        }

        public static bool TryGetSteamId(this MyPlayerCollection self, long playerId, out ulong steamId)
        {
            if (self.TryGetPlayerById(playerId, out var player))
            {
                steamId = player.SteamId();
                return steamId != 0;
            }

            steamId = 0;
            return false;
        }

        public static bool IsAdminGrid(this IMyCubeGrid self)
        {
            foreach (var bigOwnerId in self.BigOwners)
            {
                var faction = MySession.Static.Factions.GetPlayerFaction(bigOwnerId);
                if (faction?.Tag != "ADM") return false;
            }

            return true;
        }

        /// <summary>
        /// Get the nearest parent object of given type searching up the hierarchy.
        /// </summary>
        /// <param name="entity">Entity to search up from.</param>
        /// <typeparam name="T">Type of the entity to search for.</typeparam>
        /// <returns>The nearest parent object of given type searched up from given entity if found, otherwise null.</returns>
        public static T GetParentEntityOfType<T>(this IMyEntity entity) where T : class, IMyEntity
        {
            while (entity != null)
            {
                if (entity is T match) return match;
                entity = entity.Parent;
            }

            return null;
        }

        public static bool IsTopMostParent<T>(this T entity) where T : class, IMyEntity
        {
            return entity.GetParentEntityOfType<T>() == entity;
        }

        public static bool IsSessionThread(this Thread self)
        {
            return self.ManagedThreadId == MySandboxGame.Static.UpdateThread.ManagedThreadId;
        }

        public static void ThrowIfNotSessionThread(this Thread self)
        {
            if (!self.IsSessionThread())
            {
                throw new Exception($"not main thread; yours: {self.ManagedThreadId}, main: {MySandboxGame.Static.UpdateThread.ManagedThreadId}");
            }
        }

        public static Task MoveToGameLoop(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MyAPIGateway.Utilities == null)
            {
                throw new InvalidOperationException("Attempted to move to game loop but game has stopped");
            }

            var taskSrc = new TaskCompletionSource<byte>();
            MyAPIGateway.Utilities.InvokeOnGameThread(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    taskSrc.SetResult(0);
                }
                catch (Exception e)
                {
                    taskSrc.SetException(e);
                }
            });

            return taskSrc.Task;
        }

        public static void SendAddGpsRequest(this MyGpsCollection self, long identityId, MyGps gps, bool playSound)
        {
            self.SendAddGpsRequest(identityId, ref gps, gps.EntityId, playSound);
        }

        public static bool TryGetFactionByPlayerId(this MyFactionCollection self, long playerId, out IMyFaction faction)
        {
            faction = MySession.Static.Factions.GetPlayerFaction(playerId);
            return faction != null;
        }

        public static bool TryGetPlayerByGrid(this MyPlayerCollection self, IMyCubeGrid grid, out MyPlayer player)
        {
            player = null;
            return grid.BigOwners.TryGetFirst(out var ownerId) &&
                   MySession.Static.Players.TryGetPlayerById(ownerId, out player);
        }

        public static bool IsNpcFaction(this MyFactionCollection self, string factionTag)
        {
            var faction = self.TryGetFactionByTag(factionTag);
            if (faction == null) return false;
            return faction.IsEveryoneNpc();
        }

        public static string GetPlayerFactionTag(this MyFactionCollection self, long playerId)
        {
            var faction = self.GetPlayerFaction(playerId);
            return faction?.Tag;
        }

        public static bool TryGetCubeGridById(long gridId, out MyCubeGrid grid)
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            if (!MyEntityIdentifier.TryGetEntity(gridId, out var entity))
            {
                grid = null;
                return false;
            }

            if (!(entity is MyCubeGrid g))
            {
                throw new Exception($"Not a grid: {gridId} -> {entity.GetType()}");
            }

            grid = g;
            return true;
        }

        public static long GetOwnerPlayerId(long gridId)
        {
            if (!Thread.CurrentThread.IsSessionThread())
            {
                throw new Exception("Not in the main thread");
            }

            if (TryGetCubeGridById(gridId, out var grid) &&
                grid.BigOwners.TryGetFirst(out var ownerId))
            {
                return ownerId;
            }

            return 0;
        }

        public static string GetPlayerNameOrElse(this MyPlayerCollection self, long playerId, string defaultPlayerName)
        {
            if (self.TryGetPlayerById(playerId, out var p))
            {
                return p.DisplayName;
            }

            return defaultPlayerName;
        }

        public static string GetEntityNameOrElse(long entityId, string defaultName)
        {
            return MyEntities.TryGetEntityById(entityId, out var e) ? e.DisplayName : defaultName;
        }

        public static async Task<(bool, MyCubeGrid)> TryGetSelectedGrid(this IMyPlayer self)
        {
            if (self.TryGetSeatedGrid(out var seatedGrid))
            {
                return (true, seatedGrid);
            }

            try
            {
                await MoveToGameLoop();

                if (self.TryGetGridLookedAt(out var lookedGrid))
                {
                    return (true, lookedGrid);
                }

                return (false, null);
            }
            finally
            {
                await TaskUtils.MoveToThreadPool();
            }
        }

        public static bool TryGetSelectedGrid(this IMyPlayer self, out MyCubeGrid selectedGrid)
        {
            return self.TryGetSeatedGrid(out selectedGrid) ||
                   self.TryGetGridLookedAt(out selectedGrid);
        }

        public static bool TryGetSeatedGrid(this IMyPlayer self, out MyCubeGrid selectedGrid)
        {
            var seat = self.Controller?.ControlledEntity?.Entity;
            selectedGrid = seat?.GetParentEntityOfType<MyCubeGrid>();
            return selectedGrid != null;
        }

        public static bool TryGetGridLookedAt(this IMyPlayer self, out MyCubeGrid selectedGrid)
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            var hits = new List<MyPhysics.HitInfo>();
            var from = self.GetPosition();
            var look = ((MyPlayer)self).Character.GetHeadMatrix(true).Forward;
            var to = from + look * 100;
            MyPhysics.CastRay(from, to, hits);

            foreach (var hit in hits)
            {
                var hitEntity = hit.HkHitInfo.GetHitEntity();
                Log.Info(hitEntity);
                if (hitEntity.GetParentEntityOfType<MyCubeGrid>() is { } grid)
                {
                    selectedGrid = grid;
                    return true;
                }
            }

            selectedGrid = null;
            return false;
        }

        public static bool IsSomeoneNpc(this MyFaction self)
        {
            foreach (var p in self.Members)
            {
                if (Sync.Players.IdentityIsNpc(p.Key)) return true;
            }

            return false;
        }

        public static bool IsFriendWith(this IMyPlayer self, long playerId)
        {
            var otherPlayerFaction = MySession.Static.Factions.GetPlayerFaction(playerId);
            return otherPlayerFaction.Members.ContainsKey(self.IdentityId);
        }

        public static async Task<(bool, T)> TryGetEntityByName<T>(string name) where T : IMyEntity
        {
            try
            {
                await MoveToGameLoop();

                if (MyEntities.TryGetEntityByName(name, out var entity) && entity is T typedEntity)
                {
                    return (true, typedEntity);
                }

                return (false, default);
            }
            finally
            {
                await TaskUtils.MoveToThreadPool();
            }
        }

        public static IEnumerable<IMyPlayer> GetBigOwnerPlayers(this MyCubeGrid self)
        {
            foreach (var bigOwnerId in self.BigOwners)
            {
                if (MySession.Static.Players.TryGetPlayerById(bigOwnerId, out var player))
                {
                    yield return player;
                }
            }
        }

        public static Task MoveToThreadPool(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var source = new TaskCompletionSource<byte>();
            MyAPIGateway.Parallel.Start(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    source.SetResult(0);
                }
                catch (Exception e)
                {
                    source.SetException(e);
                }
            });

            return source.Task;
        }

        public static IEnumerable<IMyEntity> GetEntities(this HkWorld world)
        {
            var entities = new List<IMyEntity>();
            var rigidbodies = world.RigidBodies.ToList();
            foreach (var rigidBody in rigidbodies)
            {
                var body = rigidBody.GetBody();
                var entity = body.Entity;
                entities.Add(entity);
            }

            return entities;
        }

        public static IEnumerable<MyCubeGrid> GetAllCubeGrids()
        {
            foreach (var group in MyCubeGridGroups.Static.Logical.Groups)
            foreach (var node in group.Nodes)
            {
                yield return node.NodeData;
            }
        }

        public static bool EveryFrame(int frameCount)
        {
            return MySession.Static.GameplayFrameCounter % frameCount == 0;
        }

        public static bool EverySeconds(double seconds)
        {
            return MySession.Static.GameplayFrameCounter % (int)(60 * seconds) == 0;
        }

        public static bool TypeNameEquals(this MyObjectBuilderType self, string typeName)
        {
            var selfTypeName = self.ToString(); // doesn't allocate
            for (var i = 0; i < typeName.Length; i++)
            {
                if (!selfTypeName.TryGetCharacterAt(i + 16, out var c0)) return false;
                var c1 = typeName[i];
                if (c1 != c0) return false;
            }

            return true;
        }

        public static double GetValueAtIndex(this Vector3D self, int index) => index switch
        {
            0 => self.X,
            1 => self.Y,
            2 => self.Z,
            _ => throw new IndexOutOfRangeException()
        };

        public static int GetValueAtIndex(this Vector3I self, int index) => index switch
        {
            0 => self.X,
            1 => self.Y,
            2 => self.Z,
            _ => throw new IndexOutOfRangeException()
        };

        public static void SetValueAtIndex(this ref Vector3D self, int index, double value)
        {
            switch (index)
            {
                case 0:
                    self.X = value;
                    return;
                case 1:
                    self.Y = value;
                    return;
                case 2:
                    self.Z = value;
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static void SetValueAtIndex(this ref Vector3I self, int index, int value)
        {
            switch (index)
            {
                case 0:
                    self.X = value;
                    return;
                case 1:
                    self.Y = value;
                    return;
                case 2:
                    self.Z = value;
                    return;
                default:
                    throw new IndexOutOfRangeException();
            }
        }

        public static (double Size, Vector3D Center) GetBound(IEnumerable<Vector3D> positions)
        {
            var minPos = positions.Aggregate(Vector3D.MaxValue, (s, n) => Vector3D.Min(s, n));
            var maxPos = positions.Aggregate(Vector3D.MinValue, (s, n) => Vector3D.Max(s, n));
            var size = Vector3D.Distance(minPos, maxPos);
            var center = (minPos + maxPos) / 2;
            return (size, center);
        }

        public static string MakeGpsString(string name, Vector3D coord, string color)
        {
            return $"GPS:{name}:{coord.X:0}:{coord.Y:0}:{coord.Z:0}:{color}:";
        }

        public static (string name, Vector3D coord, Color color) GetGpsFromString(string gpsStr)
        {
            var pattern = new Regex("GPS:(.+?):(.+?):(.+?):(.+?):(.+?):");
            var match = pattern.Match(gpsStr);
            return (
                match.Groups[1].Value,
                new Vector3D(
                    float.Parse(match.Groups[2].Value),
                    float.Parse(match.Groups[3].Value),
                    float.Parse(match.Groups[4].Value)),
                ParseColor(match.Groups[5].Value));
        }

        public static Vector3D GetPosition(this IMyEntity self)
        {
            return self.PositionComp.GetPosition();
        }

        public static Color ParseColor(string str)
        {
            var c = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(str)!;
            return new Color(c.R, c.G, c.B, c.A);
        }
    }
}