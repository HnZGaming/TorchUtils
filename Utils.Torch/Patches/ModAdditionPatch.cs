using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Sandbox.Engine.Networking;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage.Game;

namespace Utils.Torch.Patches
{
    // https://torchapi.com/wiki/index.php/Things_you_can_do_in_your_plugin#Coupling_your_Plugin_with_a_Mod
    [PatchShim]
    public static class ModAdditionPatch
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static readonly List<ulong> _clientModIds = new();
        static readonly List<ulong> _bothModIds = new();

        public static void AddModForClientOnly(ulong modId)
        {
            _clientModIds.Add(modId);
            Log.Info($"AddModForClient({modId})");
        }

        public static void AddModForServerAndClient(ulong modId)
        {
            _bothModIds.Add(modId);
            Log.Info($"AddModForServerAndClient({modId})");
        }

        public static void Patch(PatchContext ctx)
        {
            Log.Info("Patch()");

            // client mods
            {
                var patchee = typeof(MySession).GetMethod("GetWorld", BindingFlags.Instance | BindingFlags.Public);
                var patcher = typeof(ModAdditionPatch).GetMethod(nameof(SuffixGetWorld), BindingFlags.Static | BindingFlags.NonPublic);
                ctx.GetPattern(patchee).Suffixes.Add(patcher);
            }

            // server and client mods
            {
                var patchee = typeof(MyLocalCache).GetMethod(nameof(MyLocalCache.LoadCheckpoint), BindingFlags.Static | BindingFlags.Public);
                var patcher = typeof(ModAdditionPatch).GetMethod(nameof(SuffixLoadCheckpoint), BindingFlags.Static | BindingFlags.NonPublic);
                ctx.GetPattern(patchee).Suffixes.Add(patcher);
            }
        }

        static void SuffixGetWorld(ref MyObjectBuilder_World __result)
        {
            foreach (var modId in _clientModIds)
            {
                TryAddMod(__result.Checkpoint.Mods, modId);
                Log.Info($"SuffixGetWorld({modId})");
            }
        }

        static void SuffixLoadCheckpoint(MyObjectBuilder_Checkpoint __result)
        {
            foreach (var modId in _bothModIds)
            {
                TryAddMod(__result.Mods, modId);
                Log.Info($"SuffixLoadCheckpoint({modId})");
            }
        }

        static void TryAddMod(this ICollection<MyObjectBuilder_Checkpoint.ModItem> self, ulong modId)
        {
            if (self.All(b => b.PublishedFileId != modId))
            {
                self.Add(ModItemUtils.Create(modId));
            }
        }
    }
}