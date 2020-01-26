using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace LagGridBroadcaster
{
    public class GpsComparer : IEqualityComparer<IMyGps>
    {
        public bool Equals(IMyGps x, IMyGps y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            return x.Hash == y.Hash;
        }

        public int GetHashCode(IMyGps obj)
        {
            return obj.Hash;
        }
    }
}