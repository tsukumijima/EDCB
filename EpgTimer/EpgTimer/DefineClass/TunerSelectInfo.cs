using System;

namespace EpgTimer
{
    public class TunerSelectInfo
    {
        public TunerSelectInfo(string name, UInt32 id) { Name = name; ID = id; }
        public string Name { get; set; }
        public UInt32 ID { get; set; }
        public override string ToString()
        {
            return ID == 0 ? "自動" : ("ID:" + ID.ToString("X8") + " (" + Name + ")");
        }
    }
}
