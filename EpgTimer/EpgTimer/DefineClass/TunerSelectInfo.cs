using System;

namespace EpgTimer
{
    public class TunerSelectInfo
    {
        public TunerSelectInfo(string name, uint id) { Name = name; ID = id; }
        public string Name { get; set; }
        public uint ID { get; set; }
        public override string ToString()
        {
            return ID == 0 ? "自動" : ("ID:" + ID.ToString("X8") + " (" + Name + ")");
        }
    }
}
