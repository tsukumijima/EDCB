using System;

namespace EpgTimer
{
    public class ManualAutoAddDataItem : AutoAddDataItemT<ManualAutoAddData>
    {
        public ManualAutoAddDataItem() { }
        public ManualAutoAddDataItem(ManualAutoAddData item) : base(item) { }

        public ManualAutoAddData ManualAutoAddInfo { get { return (ManualAutoAddData)Data; } set { Data = value; } }
        public override bool IsManual { get { return true; } }

        public string DayOfWeek
        {
            get
            {
                string view = "";
                byte dayOfWeekFlag = GetWeekFlgMod();
                for (int i = 0; i < 7; i++)
                {
                    if ((dayOfWeekFlag & 0x01) != 0)
                    {
                        view += CommonManager.DayOfWeekArray[i];
                    }
                    dayOfWeekFlag >>= 1;
                }
                return view;
            }
        }
        public double DayOfWeekValue
        {
            get
            {
                int ret = 0;
                byte dayOfWeekFlag = GetWeekFlgMod();
                for (int i = 1; i <= 7; i++)
                {
                    if ((dayOfWeekFlag & 0x01) != 0)
                    {
                        ret = 10 * ret + i;
                    }
                    dayOfWeekFlag >>= 1;
                }
                return ret * Math.Pow(10, (7 - ret.ToString().Length));
            }
        }
        private byte GetWeekFlgMod()
        {
            if (Settings.Instance.LaterTimeUse == true && DateTime28.IsLateHour(ManualAutoAddInfo.PgStartTime.Hour) == true)
            {
                return ManualAutoAddData.ShiftWeekFlag(ManualAutoAddInfo.dayOfWeekFlag, -1);
            }
            return ManualAutoAddInfo.dayOfWeekFlag;
        }
        public string StartTime
        {
            get { return CommonManager.ConvertTimeText(ManualAutoAddInfo.PgStartTime, ManualAutoAddInfo.durationSecond, true, Settings.Instance.ResInfoNoSecond, true, true, Settings.Instance.ResInfoNoEnd); }
        }
        public uint StartTimeValue
        {
            get { return ManualAutoAddInfo.startTime; }
        }
        public string StartTimeShort
        {
            get { return CommonManager.ConvertTimeText(ManualAutoAddInfo.PgStartTime, ManualAutoAddInfo.durationSecond, true, true, true, true); }
        }
        public string Duration
        {
            get { return CommonManager.ConvertDurationText(ManualAutoAddInfo.PgDurationSecond, Settings.Instance.ResInfoNoDurSecond); }
        }
        public uint DurationValue
        {
            get { return ManualAutoAddInfo.PgDurationSecond; }
        }
        public override string NetworkName
        {
            get { return CommonManager.ConvertNetworkNameText(ManualAutoAddInfo.originalNetworkID); }
        }
        public override string ServiceName
        {
            get { return ManualAutoAddInfo.stationName; }
        }
        public override string ConvertInfoText(object param = null)
        {
            string view = "番組名 : " + EventName + "\r\n";
            view += "曜日 : " + DayOfWeek + "\r\n";
            view += "時間 : " + CommonManager.ConvertTimeText(ManualAutoAddInfo.PgStartTime, ManualAutoAddInfo.durationSecond, true, false, true, true) + "\r\n";
            view += "サービス : " + ServiceName + "(" + NetworkName + ")" + "\r\n";
            view += "自動登録 : " + CommonManager.ConvertIsEnableText(KeyEnabled) + "\r\n\r\n";

            view += "【録画設定】\r\n" + ConvertRecSettingText() + "\r\n\r\n";

            view += "プログラム自動予約ID : " + string.Format("{0} (0x{0:X})", DisplayID);
            return view;
        }
    }

}
