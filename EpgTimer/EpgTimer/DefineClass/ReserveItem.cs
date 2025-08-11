using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace EpgTimer
{
    public class ReserveItem : SearchItem
    {
        public ReserveItem() { }
        public ReserveItem(ReserveData item) { ReserveInfo = item; }

        public override ulong KeyID { get { return ReserveInfo == null ? 0 : ReserveInfo.ReserveID; } }
        public override ulong DisplayID { get { return KeyID; } }
        public override object DataObj { get { return ReserveInfo; } }

        private bool initEventInfo = false;
        public override EpgEventInfo EventInfo
        {
            get
            {
                if (initEventInfo == false)
                {
                    if (ReserveInfo != null)
                    {
                        eventInfo = ReserveInfo.GetPgInfo();
                        initEventInfo = true;
                    }
                }
                return eventInfo;
            }
        }

        public override string EventName
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return ReserveInfo.Title;
            }
        }
        public override string ServiceName
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return ReserveInfo.StationName;
            }
        }
        public override string NetworkName
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return CommonManager.ConvertNetworkNameText(ReserveInfo.OriginalNetworkID);
            }
        }
        public override string StartTime
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return GetTimeStringReserveStyle(ReserveInfo.StartTime, ReserveInfo.DurationSecond);
            }
        }
        public override long StartTimeValue
        {
            get
            {
                if (ReserveInfo == null) return long.MinValue;
                //
                return ReserveInfo.StartTime.Ticks;
            }
        }
        public string StartTimeShort
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return CommonManager.ConvertTimeText(ReserveInfo.StartTime, ReserveInfo.DurationSecond, true, true);
            }
        }
        public override string Duration
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return GetDurationStringReserveStyle(ReserveInfo.DurationSecond);
            }
        }
        public override uint DurationValue
        {
            get
            {
                if (ReserveInfo == null) return uint.MinValue;
                //
                return ReserveInfo.DurationSecond;
            }
        }
        public override string ConvertInfoText(object param = null)
        {
            var mode = param is int ? (int)param : Settings.Instance.ReserveToolTipMode;
            if (mode == 1) return base.ConvertInfoText();

            if (ReserveInfo == null) return "";
            //
            string view = CommonManager.ConvertTimeText(ReserveInfo.StartTime, ReserveInfo.DurationSecond, false, false, false) + "\r\n";
            view += ServiceName + "(" + NetworkName + ")" + "\r\n";
            view += EventName + "\r\n\r\n";

            view += ConvertRecSettingText() + "\r\n";
            view += "使用予定チューナー : " + ReserveTuner + "\r\n";
            view += "予想サイズ : " + EstimatedRecSize + "\r\n";
            view += "予約状況 : " + Comment + "\r\n";
            List<string> errs = ErrComment;
            view += "エラー状況 : " + (errs.Count == 0 ? "なし" : string.Join(" ", errs.Select(s => "＊" + s))) + "\r\n\r\n";

            view += CommonManager.Convert64PGKeyString(ReserveInfo.Create64PgKey()) + "\r\n\r\n";

            view += "予約ID : " + string.Format("{0} (0x{0:X})", DisplayID);
            return view;
        }

        static string[] wiewString = { "", "", "無", "予+", "予+", "無+", "録*", "視*", "無*" };
        public override string Status
        {
            get
            {
                int index = 0;
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsOnAir() == true)
                    {
                        index = 3;
                    }
                    if (ReserveInfo.IsOnRec() == true)//マージンがあるので、IsOnAir==trueとは限らない
                    {
                        index = 6;
                    }
                    if (ReserveInfo.IsEnabled == false) //無効の判定
                    {
                        index += 2;
                    }
                    else if (ReserveInfo.IsWatchMode == true) //視聴中の判定
                    {
                        index += 1;
                    }
                }
                return wiewString[index];
            }
        }
        public override Brush StatusColor
        {
            get
            {
                int idx = 0;
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsOnRec() == true)
                    {
                        idx = ReserveInfo.IsWatchMode ? 3 : 1;
                    }
                    else if (ReserveInfo.IsOnAir() == true)
                    {
                        idx = 2;
                    }
                }
                return Settings.BrushCache.ResStatusColor[idx];
            }
        }
    }
}
