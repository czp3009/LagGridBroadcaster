// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;

namespace LagGridBroadcaster
{
    public class MeasureResult
    {
        public long EntityId { get; set; }
        public string EntityName { get; set; }
        public double MsPerTick { get; set; }
        public long PlayerId { get; set; }
        public string PlayerDisplayName { get;  set;}
        public long? FactionId { get;  set;}
        public string FactionName { get; set; }
    }

    public class MeasureResultsFile
    {
        public MeasureResult[] MeasureResults { get;  set; }
        public DateTime DateTime { get;  set; }
    }
}
