using System.Collections.Generic;
using VRageMath;

namespace Utils.Torch
{
    public sealed class GpsCleanerCollection
    {
        readonly Dictionary<long, GpsCleaner> _gpsCleaners;

        public GpsCleanerCollection()
        {
            _gpsCleaners = new Dictionary<long, GpsCleaner>();
        }

        public void Add(long playerId, string name, string description, Vector3D position, Color color)
        {
            if (!_gpsCleaners.TryGetValue(playerId, out var gpsCleaner))
            {
                _gpsCleaners[playerId] = gpsCleaner = new GpsCleaner(playerId);
            }

            gpsCleaner.Add(name, description, position, color);
        }

        public void Clear(long playerId)
        {
            if (_gpsCleaners.TryGetValue(playerId, out var gpsCleaner))
            {
                gpsCleaner.Clear();
                _gpsCleaners.Remove(playerId);
            }
        }
    }
}