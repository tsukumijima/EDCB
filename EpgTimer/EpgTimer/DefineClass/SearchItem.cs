using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace EpgTimer
{
    public class SearchItem : RecSettingItem, IEpgSettingAccess
    {
        protected EpgEventInfo eventInfo = null;
        public virtual EpgEventInfo EventInfo { get { return eventInfo; } set { eventInfo = value; } }
        public ReserveData ReserveInfo { get; set; }
        public int EpgSettingIndex { get; set; }
        public bool Filtered { get; set; }

        public SearchItem() { }
        public SearchItem(EpgEventInfo item) { eventInfo = item; }

        public override void Reset()
        {
            reserveTuner = null;
            base.Reset();
        }

        public override ulong KeyID { get { return EventInfo == null ? 0 : EventInfo.CurrentPgUID(); } }
        public override ulong DisplayID { get { return EventInfo == null ? 0 : EventInfo.Create64PgKey(); } }
        public override object DataObj { get { return EventInfo; } }
        public bool IsReserved { get { return (ReserveInfo != null); } }
        public override RecSettingData RecSettingInfo { get { return ReserveInfo != null ? ReserveInfo.RecSetting : null; } }
        public override bool IsManual { get { return ReserveInfo != null ? ReserveInfo.IsManual : false; } }
        public override bool PresetResCompare { get { return true; } }

        public virtual string EventName
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return EventInfo.DataTitle;
            }
        }
        public virtual string EventNameValue
        {
            get { return Settings.Instance.TrimSortTitle == true ? MenuUtil.TrimKeyword(EventName) : EventName; }
        }
        public virtual string ServiceName
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return EventInfo.ServiceName;
            }
        }
        public virtual string NetworkName
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return CommonManager.ConvertNetworkNameText(EventInfo.original_network_id);
            }
        }
        public virtual string StartTime
        {
            get
            {
                if (EventInfo == null) return "";
                if (EventInfo.StartTimeFlag == 0) return "未定";
                //
                return GetTimeStringReserveStyle(EventInfo.start_time, EventInfo.durationSec);
            }
        }
        public static string GetTimeStringReserveStyle(DateTime time, uint durationSecond)
        {
            return CommonManager.ConvertTimeText(time, durationSecond, Settings.Instance.ResInfoNoYear, Settings.Instance.ResInfoNoSecond, isNoEnd: Settings.Instance.ResInfoNoEnd);
        }
        public virtual long StartTimeValue
        {
            get
            {
                if (EventInfo == null || EventInfo.StartTimeFlag == 0) return long.MinValue;
                //
                return EventInfo.start_time.Ticks;
            }
        }
        /// <summary>
        /// 番組長
        /// </summary>
        public virtual string Duration
        {
            get
            {
                if (EventInfo == null) return "";
                if (EventInfo.DurationFlag == 0) return "不明";
                //
                return GetDurationStringReserveStyle(EventInfo.durationSec);
            }
        }
        public static string GetDurationStringReserveStyle(uint durationSecond)
        {
            return CommonManager.ConvertDurationText(durationSecond, Settings.Instance.ResInfoNoDurSecond);
        }
        public virtual uint DurationValue
        {
            get
            {
                if (EventInfo == null || EventInfo.DurationFlag == 0) return uint.MinValue;
                //
                return EventInfo.durationSec;
            }
        }
        /// <summary>
        /// 番組内容
        /// </summary>
        public string ProgramContent
        {
            get
            {
                if (EventInfo == null) return "";
                //詳細情報しか持ってないイベントはそれを表示する。文字数は基本情報に合わせて80字。
                var s1 = EventInfo.ShortInfo == null ? "" : EventInfo.ShortInfo.text_char;
                var s2 = EventInfo.ExtInfo == null ? "" : CommonManager.TrimHyphenSpace(EventInfo.ExtInfo.text_char);
                return (s1 + " " + s2.Substring(0, Math.Min(Math.Max(0, 80 - s1.Length), s2.Length))).Trim().Replace("\r\n", " ");
            }
        }
        public string JyanruKey
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return CommonManager.ConvertJyanruText(EventInfo);
            }
        }
        public string Attrib
        {
            get
            {
                if (EventInfo == null) return "";
                //
                return CommonManager.ConvertAttribText(EventInfo);
            }
        }
        public bool IsEnabled
        {
            set
            {
                EpgCmds.ChgOnOffCheck.Execute(this, null);
            }
            get
            {
                if (ReserveInfo == null) return false;
                //
                return ReserveInfo.IsEnabled;
            }
        }
        public string Comment
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return (ReserveInfo.IsMultiple == true ? "[重複EPG]" : "")
                    + (ReserveInfo.IsAutoAddMissing == true ? "不明な" : ReserveInfo.IsAutoAddInvalid == true ? "無効の" : "")
                    + CommentBase;
            }
        }
        public string CommentBase
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                if (ReserveInfo.IsAutoAdded == false)
                {
                    return "個別予約(" + (ReserveInfo.IsEpgReserve == true ? "EPG" : "プログラム") + ")";
                }
                else
                {
                    string s = ReserveInfo.Comment;
                    return (s.StartsWith("EPG自動予約(") == true ? "キーワード予約(" + s.Substring(8) : s);
                }
            }
        }
        public List<string> ErrComment
        {
            get
            {
                var s = new List<string>();
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.OverlapMode == 2)
                    {
                        s.Add("チューナー不足(録画不可)");
                    }
                    if (ReserveInfo.DurationActual <= 0)
                    {
                        s.Add("不可マージン設定(録画不可)");
                    }
                    if (ReserveInfo.OverlapMode == 1)
                    {
                        s.Add("チューナー不足(一部録画)");
                    }
                    if (ReserveInfo.IsAutoAddMissing == true)
                    {
                        s.Add("不明な自動予約登録による予約");
                    }
                    else if (ReserveInfo.IsAutoAddInvalid == true)
                    {
                        s.Add("無効の自動予約登録による予約");
                    }
                    if (ReserveInfo.IsMultiple == true)
                    {
                        s.Add("重複したEPG予約");
                    }
                }
                return s;
            }
        }
        public string RecFileName
        {
            get { return RecFileNameList.FirstOrDefault() ?? ""; }
        }
        public List<string> RecFileNameList
        {
            get
            {
                if (ReserveInfo == null) return new List<string>();
                //
                return ReserveInfo.RecFileNameList;
            }
        }
        public string IsProgram
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return ReserveInfo.IsEpgReserve == false ? "はい" : "いいえ";
            }
        }
        private string reserveTuner = null;
        public string ReserveTuner
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                if (reserveTuner == null)
                {
                    TunerReserveInfo info = CommonManager.Instance.DB.TunerReserveList.Values.Where(r => r.reserveList.Contains(ReserveInfo.ReserveID)).FirstOrDefault();
                    uint tID = info == null ? 0xFFFFFFFF : info.tunerID;
                    string tName = ReserveInfo.IsEnabled == false ? "無効予約" : info == null ? "不明" : info.tunerName;
                    reserveTuner = new TunerSelectInfo(tName, tID).ToString();
                }
                return reserveTuner;
            }
        }
        private double _estimatedRecSize = -1;
        public string EstimatedRecSize
        {
            get { return EstimatedRecSizeValue <= 0 ? "" : _estimatedRecSize.ToString("0.0 GB"); }
        }
        public double EstimatedRecSizeValue
        {
            get
            {
                if (ReserveInfo == null) return -1;
                //
                if (_estimatedRecSize < 0)
                {
                    _estimatedRecSize = 0;
                    if (ReserveInfo.IsWatchMode == false)
                    {
                        int bitrate = 0;
                        for (int i = 0; bitrate <= 0; i++)
                        {
                            string key = CommonManager.Create64Key((ushort)(i > 2 ? 0xFFFF : ReserveInfo.OriginalNetworkID),
                                                                   (ushort)(i > 1 ? 0xFFFF : ReserveInfo.TransportStreamID),
                                                                   (ushort)(i > 0 ? 0xFFFF : ReserveInfo.ServiceID)).ToString("X12");
                            bitrate = IniFileHandler.GetPrivateProfileInt("BITRATE", key, 0, SettingPath.BitrateIniPath);
                            bitrate = bitrate <= 0 && i == 3 ? 19456 : bitrate;
                        }
                        _estimatedRecSize = (double)Math.Max(bitrate / 8 * 1000 * ReserveInfo.DurationActual, 0) / 1024 / 1024 / 1024;
                    }
                }
                return _estimatedRecSize;
            }
        }
        public override string MarginStart
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return CustomTimeFormat(ReserveInfo.StartMarginResActual * -1);
            }
        }
        public override double MarginStartValue
        {
            get
            {
                if (ReserveInfo == null) return double.MinValue;
                //
                return CustomMarginValue(ReserveInfo.StartMarginResActual * -1);
            }
        }
        public override string MarginEnd
        {
            get
            {
                if (ReserveInfo == null) return "";
                //
                return CustomTimeFormat(ReserveInfo.EndMarginResActual);
            }
        }
        public override double MarginEndValue
        {
            get
            {
                if (ReserveInfo == null) return double.MinValue;
                //
                return CustomMarginValue(ReserveInfo.EndMarginResActual);
            }
        }
        public override string ConvertInfoText(object param = null)
        {
            return CommonManager.ConvertProgramText(EventInfo).TrimEnd();
        }
        public virtual string Status
        {
            get
            {
                if (EventInfo != null)
                {
                    if (IsReserved == true)
                    {
                        string ret = new ReserveItem(ReserveInfo).Status;
                        return ret != "" ? ret : ReserveInfo.IsWatchMode ? "視" : "予";
                    }
                    if (EventInfo.GetRecinfoFromPgUID() != null)
                    {
                        return "済";//放映中でも「済」なら優先する
                    }
                    if (EventInfo.IsOver() == true)
                    {
                        return "--";
                    }
                    if (EventInfo.IsOnAir() == true)
                    {
                        return "放";
                    }
                }
                return "";
            }
        }
        public virtual Brush StatusColor
        {
            get
            {
                int idx = 0;
                if (EventInfo != null)
                {
                    if (IsReserved == true)
                    {
                        return new ReserveItem(ReserveInfo).StatusColor;
                    }
                    else if (EventInfo.IsOnAir() == true)
                    {
                        idx = 2;
                    }
                    else if (EventInfo.GetRecinfoFromPgUID() != null)
                    {
                        idx = 4;//色は放映中を優先する
                    }
                }
                return Settings.BrushCache.ResStatusColor[idx];
            }
        }
        public override Brush ForeColor
        {
            get
            {
                //番組表へジャンプ時の強調表示
                if (NowJumpingTable != 0 || ReserveInfo == null) return base.ForeColor;
                //
                return Settings.BrushCache.RecModeForeColor[ReserveInfo.IsEnabled ? ReserveInfo.RecSetting.RecMode : 5];
            }
        }
        public double Opacity
        {
            get { return Filtered ? 0.7 : 1.0; }
        }
        public override Brush BackColor
        {
            get
            {
                //番組表へジャンプ時の強調表示
                if (NowJumpingTable != 0 || ReserveInfo == null) return base.BackColor;

                //通常表示
                return ViewUtil.ReserveErrBrush(ReserveInfo);
            }
        }
        public override Brush BackColor2
        {
            get { return ViewUtil.ReserveErrBrush(ReserveInfo, true); }
        }
        public override Brush BorderBrush
        {
            get { return Settings.Instance.ListRuledLineContent ? BorderBrushLeft : base.BorderBrush; }
        }
        public override Brush BorderBrushLeft
        {
            get { return ViewUtil.EpgDataContentBrush(EventInfo, EpgSettingIndex); }
        }
    }

    public static class SearchItemEx
    {
        public static List<EpgEventInfo> GetEventList(this IEnumerable<SearchItem> list)
        {
            return list.Where(item => item != null).Select(item => item.EventInfo).ToList();
        }
        public static List<EpgEventInfo> GetNoReserveList(this IEnumerable<SearchItem> list)
        {
            return list.Where(item => item != null && item.IsReserved == false).Select(item => item.EventInfo).ToList();
        }
        public static List<ReserveData> GetReserveList(this IEnumerable<SearchItem> list)
        {
            return list.Where(item => item != null && item.IsReserved == true).Select(item => item.ReserveInfo).ToList();
        }
        //public static bool HasReserved(this IEnumerable<SearchItem> list)
        //{
        //    return list.Any(info => item != null && item.IsReserved == false);
        //}
        //public static bool HasNoReserved(this IEnumerable<SearchItem> list)
        //{
        //    return list.Any(info => item != null && item.IsReserved == true);
        //}
        public static List<SearchItem> ToSearchList(this IEnumerable<EpgEventInfo> eventList, bool isExceptEnded = false, DateTime? keyTime = null)
        {
            if (eventList == null) return new List<SearchItem>();
            //
            var list = eventList.Where(info => isExceptEnded == false || info.IsOver(keyTime) == false)
                                .Select(info => new SearchItem(info)).ToList();
            list.SetReserveData();
            return list;
        }
        public static void SetReserveData(this ICollection<SearchItem> list)
        {
            var listKeys = new Dictionary<ulong, SearchItem>();
            var delList = new List<SearchItem>();
            foreach (SearchItem item in list)
            {
                ulong key = item.EventInfo.CurrentPgUID();
                if (listKeys.ContainsKey(key) == true)
                {
                    delList.Add(item);
                    continue;
                }
                listKeys.Add(key, item);
                item.ReserveInfo = null;
                item.Reset();
            }
            delList.ForEach(item => list.Remove(item));

            SearchItem setItem;
            foreach (ReserveData data in CommonManager.Instance.DB.ReserveList.Values)
            {
                if (listKeys.TryGetValue(data.CurrentPgUID(), out setItem))
                {
                    if (setItem.IsReserved == true)
                    {
                        setItem = new SearchItem(setItem.EventInfo);
                        list.Add(setItem);
                    }
                    setItem.ReserveInfo = data;
                }
            }
        }

    }
}
