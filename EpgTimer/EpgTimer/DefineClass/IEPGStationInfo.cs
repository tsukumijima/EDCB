using System;

namespace EpgTimer
{
    public class IEPGStationInfo : IDeepCloneObj
    {
        public string StationName { get; set; }
        public ulong Key { get; set; }
        public object DeepCloneObj() { return MemberwiseClone(); }
        public override string ToString() { return StationName; }
    }
}
