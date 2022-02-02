using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Utils.Torch;
using Utils.Torch.Patches;
using VRage.Game.ModAPI;

namespace Utils.TorchEntityGps
{
    public sealed class PrefixedGpsCollection
    {
        readonly string _prefix;

        public PrefixedGpsCollection(string prefix)
        {
            _prefix = prefix;
        }

        static MyGpsCollection Native => MySession.Static.Gpss;

        // we use DisplayName because players can't manipulate/fake it in any way
        // but it takes up space so you should keep the prefix very short
        bool IsOurs(MyGps g)
        {
            return g.DisplayName.StartsWith(_prefix);
        }

        void MarkOurs(MyGps g)
        {
            if (IsOurs(g)) return;
            g.DisplayName = $"{_prefix}{g.DisplayName}";
        }

        public IEnumerable<(long IdentityId, MyGps Gps)> GetAllGpss()
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            foreach (var (identityId, gps) in Native.GetAllGpss())
            {
                if (gps == null) continue;

                if (IsOurs(gps))
                {
                    yield return (identityId, gps);
                }
            }
        }

        public IEnumerable<MyGps> GetPlayerGpss(long identityId)
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            var gpss = new List<IMyGps>();
            Native.GetGpsList(identityId, gpss);
            return gpss.Cast<MyGps>().Where(g => IsOurs(g));
        }

        public void SendDeleteAllGpss()
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            foreach (var (identityId, gps) in GetAllGpss().ToArray())
            {
                Native.SendDelete(identityId, gps.Hash);
            }
        }

        public void SendAddGps(long identityId, MyGps gps, bool playSound)
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            MarkOurs(gps);
            Native.SendAddGps(identityId, ref gps, gps.EntityId, playSound);
        }

        public void SendDeleteGps(long identityId, int gpsHash)
        {
            Thread.CurrentThread.ThrowIfNotSessionThread();

            var gps = Native.GetGps(gpsHash);
            if (!IsOurs(gps))
            {
                throw new Exception($"not ours: {gps.DisplayName}");
            }

            Native.SendDelete(identityId, gpsHash);
        }
    }
}