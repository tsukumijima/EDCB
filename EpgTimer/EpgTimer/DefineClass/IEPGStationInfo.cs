using System;

namespace EpgTimer
{
    public class IEPGStationInfo : IDeepCloneObj
    {
        public String StationName { get; set; }
        public UInt64 Key { get; set; }
        public object DeepCloneObj() { return MemberwiseClone(); }
        public override string ToString() { return StationName; }
    }
}
