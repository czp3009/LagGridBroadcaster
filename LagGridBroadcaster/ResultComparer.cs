using System.Collections.Generic;
using Profiler.Core;

namespace LagGridBroadcaster
{
    internal class ResultComparer : IEqualityComparer<ProfilerRequest.Result>
    {
        public bool Equals(ProfilerRequest.Result x, ProfilerRequest.Result y)
        {
            return x.Description == y.Description;
        }

        public int GetHashCode(ProfilerRequest.Result obj)
        {
            return obj.Description.GetHashCode();
        }
    }
}