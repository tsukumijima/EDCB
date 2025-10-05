using System;

namespace EpgTimer
{
    public class NWPresetItem : IDeepCloneObj
    {
        public string Name { get; set; }
        public string NWServerIP { get; set; }
        public uint NWServerPort { get; set; }
        public uint NWWaitPort { get; set; }
        public string NWMacAdd { get; set; }
        public override string ToString() { return Name; }

        public NWPresetItem() { }
        public NWPresetItem(string name, string ip, uint sport, uint wport, string mac)
        {
            Name = name;
            NWServerIP = ip;
            NWServerPort = sport;
            NWWaitPort = wport;
            NWMacAdd = mac;
        }
        public bool EqualsTo(NWPresetItem item, bool ignoreName = false)
        {
            return (ignoreName == true || Name == item.Name)
                && NWServerIP == item.NWServerIP && NWServerPort == item.NWServerPort
                && NWWaitPort == item.NWWaitPort && NWMacAdd == item.NWMacAdd;
        }
        public object DeepCloneObj() { return MemberwiseClone(); }
    }
}
