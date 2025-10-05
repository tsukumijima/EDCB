using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace EpgTimer
{
    public partial class ReserveData : AutoAddTargetData, IRecSetttingData
    {
        public override string DataTitle { get { return Title; } }
        public override ulong DataID { get { return ReserveID; } }
        public override DateTime PgStartTime { get { return StartTime; } }
        public override uint PgDurationSecond { get { return DurationSecond; } }
        public override ulong Create64PgKey()
        {
            return CommonManager.Create64PgKey(OriginalNetworkID, TransportStreamID, ServiceID, EventID);
        }
        public RecSettingData RecSettingInfo { get { return RecSetting; } set { RecSetting = value; } }
        public bool IsManual { get { return IsEpgReserve == false; } }

        public ReserveMode ReserveMode
        {
            get
            {
                if (IsAutoAdded == true)
                {
                    return IsEpgReserve == true ? ReserveMode.KeywordAuto : ReserveMode.ManualAuto;
                }
                else
                {
                    return IsEpgReserve == true ? ReserveMode.EPG : ReserveMode.Program;
                }

            }
        }
        public bool IsEpgReserve { get { return EventID != 0xFFFF; } }
        public bool IsAutoAdded { get { return Comment != "" && Comment.EndsWith("$", StringComparison.Ordinal) == false; } }
        public void ReleaseAutoAdd() { if (IsAutoAdded == true) Comment += "$"; }

        public bool IsEnabled { get { return RecSetting.IsEnable; } }
        public bool IsWatchMode { get { return RecSetting.RecMode == 4; } }

        public bool IsOnRec(DateTime? time = null) { return OnTime(time) == 0; }
        public override bool IsOnAir(DateTime? time = null) { return base.OnTime(time) == 0; }
        /// <summary>-1:開始前、0:録画中、1:終了</summary>
        public override int OnTime(DateTime? time = null)
        {
            return onTime(StartTimeActual, DurationActual, time);
        }
        public int OnTimeBaseOnAir(DateTime? time = null) { return base.OnTime(time); }

        public DateTime StartTimeActual
        {
            get { return StartTime.AddSeconds(StartMarginResActual * -1); }
        }
        public uint DurationActual
        {
            get { return (uint)Math.Max(0, DurationSecond + StartMarginResActual + EndMarginResActual); }
        }
        public virtual int StartMarginResActual
        {
            get { return (int)Math.Max(-DurationSecond, RecSetting.StartMarginActual); }
        }
        public virtual int EndMarginResActual
        {
            get { return (int)Math.Max(-DurationSecond, RecSetting.EndMarginActual); }
        }

        public EpgEventInfo GetPgInfo()
        {
            //後段のサーチは変更ダイアログからのプログラム予約検索用も考慮
            EpgEventInfo info = CommonManager.Instance.DB.GetReserveEventList(this);
            if (info == null)
            {
                info = MenuUtil.GetPgInfoLikeThatAll(this);

                //予約の番組情報を補填。不用な情報なら勝手に無視する。
                CommonManager.Instance.DB.AddReserveEventCache(this, info);
            }
            return info;
        }

        //AppendData 関係。ID(元データ)に対して一意の情報なので、データ自体はDB側。
        private ReserveDataAppend Append { get { return CommonManager.Instance.DB.GetReserveDataAppend(this); } }
        public bool IsAutoAddMissing
        {
            get
            {
                if (Settings.Instance.DisplayReserveAutoAddMissing == false) return false;
                return IsAutoAdded && Append.IsAutoAddMissing;
            }
        }
        public bool IsAutoAddInvalid
        {
            get
            {
                if (Settings.Instance.DisplayReserveAutoAddMissing == false) return false;
                return IsAutoAdded && Append.IsAutoAddInvalid;
            }
        }
        public bool IsMultiple
        {
            get
            {
                if (Settings.Instance.DisplayReserveMultiple == false) return false;
                return CommonManager.Instance.DB.IsReserveMulti(this);
            }
        }
        public override List<EpgAutoAddData> SearchEpgAutoAddList(bool? IsEnabled = null, bool ByFazy = false)
        {
            //プログラム予約の場合はそれっぽい番組を選んで、キーワード予約の検索にヒットしていたら選択する。
            var info = IsEpgReserve ? this as IAutoAddTargetData : this.GetPgInfo();
            return AutoAddTargetData.SearchEpgAutoAddHitList(info, IsEnabled, ByFazy);
        }
        public override List<EpgAutoAddData> GetEpgAutoAddList(bool? IsEnabled = null)
        {
            return IsEnabled == null ? Append.EpgAutoList : IsEnabled == true ? Append.EpgAutoListEnabled : Append.EpgAutoListDisabled;
        }
        public override List<ManualAutoAddData> GetManualAutoAddList(bool? IsEnabled = null)
        {
            return IsEnabled == null ? Append.ManualAutoList : IsEnabled == true ? Append.ManualAutoListEnabled : Append.ManualAutoListDisabled;
        }
    }

    public static class ReserveDataEx
    {
        public static ReserveData GetNextReserve(this List<ReserveData> resList, bool IsTargetOffRes = false)
        {
            ReserveData ret = null;
            long value = long.MaxValue;

            foreach (ReserveData data in resList)
            {
                if (IsTargetOffRes == true || data.IsEnabled == true)
                {
                    if (value > data.StartTime.ToBinary())
                    {
                        ret = data;
                        value = data.StartTime.ToBinary();
                    }
                }
            }

            return ret;
        }
    }

    //AutoAddAppendに依存するので生成時は注意
    public class ReserveDataAppend
    {
        public ReserveDataAppend()
        {
            EpgAutoList = new List<EpgAutoAddData>();
            EpgAutoListEnabled = new List<EpgAutoAddData>();
            ManualAutoList = new List<ManualAutoAddData>();
            ManualAutoListEnabled = new List<ManualAutoAddData>();
        }

        public bool IsAutoAddMissing { get; protected set; }
        public bool IsAutoAddInvalid { get; protected set; }
        public List<EpgAutoAddData> EpgAutoList { get; protected set; }
        public List<EpgAutoAddData> EpgAutoListEnabled { get; protected set; }
        public List<EpgAutoAddData> EpgAutoListDisabled { get { return EpgAutoList.GetAutoAddList(false); } }
        public List<ManualAutoAddData> ManualAutoList { get; protected set; }
        public List<ManualAutoAddData> ManualAutoListEnabled { get; protected set; }
        public List<ManualAutoAddData> ManualAutoListDisabled { get { return ManualAutoList.GetAutoAddList(false); } }

        //情報の更新をする。
        public void UpdateData()
        {
            EpgAutoListEnabled = EpgAutoList.GetAutoAddList(true);
            ManualAutoListEnabled = ManualAutoList.GetAutoAddList(true);
            IsAutoAddMissing = (EpgAutoList.Count + ManualAutoList.Count) == 0;
            IsAutoAddInvalid = (EpgAutoListEnabled.Count + ManualAutoListEnabled.Count) == 0;
        }
    }

    //録画済み(RecFileInfo用)
    public class ReserveDataEnd : ReserveData
    {
        public override int StartMarginResActual { get { return 0; } }
        public override int EndMarginResActual { get { return 0; } }
    }

}
