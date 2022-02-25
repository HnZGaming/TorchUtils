using System.Collections.Generic;
using Sandbox.Game;
using VRageMath;

namespace Utils.Torch
{
    public sealed class GpsCleaner
    {
        readonly long _playerId;
        readonly HashSet<string> _names;

        public GpsCleaner(long playerId)
        {
            _playerId = playerId;
            _names = new HashSet<string>();
        }

        public void Add(string name, string description, Vector3D position, Color color)
        {
            MyVisualScriptLogicProvider.AddGPS(name, description, position, color, playerId: _playerId);
            _names.Add(name);
        }

        public void Clear()
        {
            foreach (var name in _names)
            {
                MyVisualScriptLogicProvider.RemoveGPS(name, _playerId);
            }

            _names.Clear();
        }
    }
}