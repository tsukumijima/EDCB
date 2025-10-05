using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EpgTimer
{
    public interface IRecWorkMainData
    {
        string DataTitle { get; }
        ulong DataID { get; }
    }
    public interface IBasicPgInfo : IRecWorkMainData
    {
        DateTime PgStartTime { get; }
        uint PgDurationSecond { get; }
        ulong Create64Key();
    }
    public static class IBasicPgInfoEx
    {
        //CurrentPgUID()は同一のEventIDの番組をチェックするが、こちらは放映時刻をチェックする。
        //プログラム予約が絡んでいる場合、結果が変わってくる。
        public static bool IsSamePg(this IBasicPgInfo data1, IBasicPgInfo data2)
        {
            if (data1 == null || data2 == null) return false;
            return data1.PgStartTime == data2.PgStartTime && data1.PgDurationSecond == data2.PgDurationSecond && data1.Create64Key() == data2.Create64Key();
        }
    }
    public interface IAutoAddTargetData : IBasicPgInfo
    {
        ulong Create64PgKey();
        ulong CurrentPgUID();
        bool IsOnAir(DateTime? time = null);
        bool IsOver(DateTime? time = null);
        int OnTime(DateTime? time = null);
        List<EpgAutoAddData> SearchEpgAutoAddList(bool? IsEnabled = null, bool ByFazy = false);
        List<ManualAutoAddData> SearchManualAutoAddList(bool? IsEnabled = null);
        List<EpgAutoAddData> GetEpgAutoAddList(bool? IsEnabled = null);
        List<ManualAutoAddData> GetManualAutoAddList(bool? IsEnabled = null);
    }

    public abstract class AutoAddTargetData : IAutoAddTargetData
    {
        public abstract string DataTitle { get; }
        public abstract ulong DataID { get;}
        public abstract DateTime PgStartTime { get; }
        public abstract uint PgDurationSecond { get; }
        public virtual ulong Create64Key() { return Create64PgKey() >> 16; }
        public abstract ulong Create64PgKey();
        public virtual ulong CurrentPgUID()
        {
            return CommonManager.CurrentPgUID(Create64PgKey(), PgStartTime);
        }
        public virtual bool IsOnAir(DateTime? time = null) { return OnTime(time) == 0; }
        public virtual bool IsOver(DateTime? time = null) { return OnTime(time) > 0; }
        /// <summary>-1:開始前、0:録画中、1:終了</summary>
        public virtual int OnTime(DateTime? time = null)
        {
            return onTime(PgStartTime, PgDurationSecond, time);
        }
        protected static int onTime(DateTime startTime, uint duration, DateTime? time = null)
        {
            if (startTime == DateTime.MaxValue) return -1;//startTime.AddSeconds()が通らない
            time = time ?? CommonUtil.EdcbNowEpg;
            return startTime.AddSeconds(duration) <= time ? 1 : startTime <= time ? 0 : -1;
        }

        public virtual List<EpgAutoAddData> SearchEpgAutoAddList(bool? IsEnabled = null, bool ByFazy = false)
        {
            return SearchEpgAutoAddHitList(this, IsEnabled, ByFazy);
        }
        public virtual List<ManualAutoAddData> SearchManualAutoAddList(bool? IsEnabled = null)
        {
            return GetManualAutoAddHitList(this, IsEnabled);
        }
        public virtual List<EpgAutoAddData> GetEpgAutoAddList(bool? IsEnabled = null)
        {
            return GetEpgAutoAddHitList(this, IsEnabled);
        }
        public virtual List<ManualAutoAddData> GetManualAutoAddList(bool? IsEnabled = null)
        {
            return GetManualAutoAddHitList(this, IsEnabled);
        }

        public static List<EpgAutoAddData> SearchEpgAutoAddHitList(IAutoAddTargetData info, bool? IsEnabled = null, bool ByFazy = false)
        {
            if (info == null) return new List<EpgAutoAddData>();
            //
            var list = GetEpgAutoAddHitList(info, IsEnabled);
            if (ByFazy == true)
            {
                list.AddRange(MenuUtil.FazySearchEpgAutoAddData(info.DataTitle, IsEnabled));
                list = list.Distinct().OrderBy(data => data.DataID).ToList();
            }
            return list;
        }
        public static List<EpgAutoAddData> GetEpgAutoAddHitList(IAutoAddTargetData info, bool? IsEnabled = null)
        {
            return CommonManager.Instance.DB.EpgAutoAddList.Values.GetAutoAddList(IsEnabled)
                .FindAll(data => data.CheckPgHit(info) == true);//info==nullでもOK
        }
        public static List<ManualAutoAddData> GetManualAutoAddHitList(IAutoAddTargetData info, bool? IsEnabled = null)
        {
            return CommonManager.Instance.DB.ManualAutoAddList.Values.GetAutoAddList(IsEnabled)
                .FindAll(data => data.CheckPgHit(info) == true);//info==nullでもOK
        }
    }
    public abstract class AutoAddTargetDataStable : AutoAddTargetData
    {
        protected ulong currentPgUID = 0;//DeepCopyでは無視
        public override ulong CurrentPgUID()
        {
            if (currentPgUID == 0) currentPgUID = base.CurrentPgUID();
            return currentPgUID;
        }
    }

    static class CtrlCmdDefEx
    {
        //CopyObj.csのジェネリックを使って定義している。
        public static bool EqualsTo(this IList<RecFileSetInfo> src, IList<RecFileSetInfo> dest) { return CopyObj.EqualsTo(src, dest, EqualsValue); }
        public static bool EqualsTo(this RecFileSetInfo src, RecFileSetInfo dest) { return CopyObj.EqualsTo(src, dest, EqualsValue); }
        public static bool EqualsValue(RecFileSetInfo src, RecFileSetInfo dest)
        {
            return src.RecFileName.Equals(dest.RecFileName, StringComparison.OrdinalIgnoreCase) == true
                && src.RecFolder.Equals(dest.RecFolder, StringComparison.OrdinalIgnoreCase) == true
                && src.RecNamePlugIn.Equals(dest.RecNamePlugIn, StringComparison.OrdinalIgnoreCase) == true
                && src.WritePlugIn.Equals(dest.WritePlugIn, StringComparison.OrdinalIgnoreCase) == true;
        }

        public static ReserveData ToReserveData(this EpgEventInfo epgInfo)
        {
            if (epgInfo == null) return null;
            var resInfo = new ReserveData();
            epgInfo.ToReserveData(ref resInfo);
            return resInfo;
        }

        public static bool ToReserveData(this EpgEventInfo epgInfo, ref ReserveData resInfo)
        {
            if (epgInfo == null || resInfo == null) return false;

            resInfo.Title = epgInfo.DataTitle;
            resInfo.StartTime = epgInfo.start_time;
            resInfo.StartTimeEpg = epgInfo.start_time;
            resInfo.DurationSecond = epgInfo.PgDurationSecond;
            resInfo.StationName = epgInfo.ServiceName;
            resInfo.OriginalNetworkID = epgInfo.original_network_id;
            resInfo.TransportStreamID = epgInfo.transport_stream_id;
            resInfo.ServiceID = epgInfo.service_id;
            resInfo.EventID = epgInfo.event_id;

            return true;
        }
        /*
        public static EpgEventInfo ToEpgEventInfo(this RecFileInfo recinfo)
        {
            return recinfo == null ? null : new EpgEventInfo
            {
                original_network_id = recinfo.OriginalNetworkID,
                transport_stream_id = recinfo.TransportStreamID,
                service_id = recinfo.ServiceID,
                event_id = recinfo.EventID,
                start_time = recinfo.StartTime,
                durationSec = recinfo.DurationSecond,
                StartTimeFlag = 1,
            };
        }
        */
        public static ReserveDataEnd ToReserveData(this RecFileInfo recinfo)
        {
            return recinfo == null ? null : new ReserveDataEnd
            {
                //ReserveID = recinfo.ID,副作用が多いので0固定
                StartTime = recinfo.StartTime,
                DurationSecond = recinfo.DurationSecond,
                OriginalNetworkID = recinfo.OriginalNetworkID,
                TransportStreamID = recinfo.TransportStreamID,
                ServiceID = recinfo.ServiceID,
                EventID = recinfo.EventID,
                //Title = recinfo.Title,
                //StationName = recinfo.ServiceName,
                //Comment = recinfo.Comment,
                //RecFileNameList = CommonUtil.ToList(recinfo.RecFilePath),
                //RecSetting.RecFolderList =,
            };
        }

        public static void RegulateData(this EpgSearchDateInfo info)
        {
            //早い終了時間を翌日のものとみなす
            int start = (info.startHour) * 60 + info.startMin;
            int end = (info.endHour) * 60 + info.endMin;
            while (end < start)
            {
                end += 24 * 60;
                info.endDayOfWeek = (byte)((info.endDayOfWeek + 1) % 7);
            }

            //28時間表示対応の処置。実際はシフトは1回で十分ではある。
            while (info.startHour >= 24) ShiftRecDayPart(1, ref info.startHour, ref info.startDayOfWeek);
            while (info.endHour >= 24) ShiftRecDayPart(1, ref info.endHour, ref info.endDayOfWeek);
        }
        private static void ShiftRecDayPart(int direction, ref ushort hour, ref byte weekFlg)
        {
            int shift_day = (direction >= 0 ? 1 : -1);
            hour = (ushort)((int)hour + -1 * shift_day * 24);
            weekFlg = (byte)((weekFlg + 7 + shift_day) % 7);
        }

        public static ulong Create64Key(this EpgEventData obj)
        {
            return CommonManager.Create64Key(obj.original_network_id, obj.transport_stream_id, obj.service_id);
        }
        public static ulong Create64PgKey(this EpgEventData obj)
        {
            return CommonManager.Create64PgKey(obj.original_network_id, obj.transport_stream_id, obj.service_id, obj.event_id);
        }

        public static List<ReserveData> GetReserveListFromPgUID(this IAutoAddTargetData data)
        {
            if (data == null) return null;
            ulong id = data.CurrentPgUID();
            return CommonManager.Instance.DB.ReserveList.Values.Where(info => info.CurrentPgUID() == id).ToList();
        }

        public static RecFileInfo GetRecinfoFromPgUID(this IAutoAddTargetData data)
        {
            List<RecFileInfo> list = data.GetRecListFromPgUID();
            return list == null ? null : list.FirstOrDefault();
        }
        public static List<RecFileInfo> GetRecListFromPgUID(this IAutoAddTargetData data)
        {
            if (data == null) return null;
            List<RecFileInfo> list = null;
            CommonManager.Instance.DB.RecFileUIDList.TryGetValue(data.CurrentPgUID(), out list);
            return list ?? new List<RecFileInfo>();
        }
    }
}
