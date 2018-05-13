using System;

namespace EpgTimer
{
    public class InfoSearchSettingData : IDeepCloneObj
    {
        private string searchWord = "";
        public string SearchWord { get { return searchWord ?? ""; } set { searchWord = value; } }
        public bool RegExp { get; set; }
        public bool TitleOnly { get; set; }
        public bool ReserveInfo { get; set; }
        public bool RecInfo { get; set; }
        public bool EpgAutoAddInfo { get; set; }
        public bool ManualAutoAddInfo { get; set; }

        public InfoSearchSettingData() { }
        public InfoSearchSettingData(bool defaultSetting)
        {
            if (defaultSetting == false) return;
            RegExp = false;
            TitleOnly = true;
            ReserveInfo = true;
            RecInfo = true;
            EpgAutoAddInfo = true;
            ManualAutoAddInfo = true;
        }

        public object DeepCloneObj() { return MemberwiseClone(); }
    }
}
