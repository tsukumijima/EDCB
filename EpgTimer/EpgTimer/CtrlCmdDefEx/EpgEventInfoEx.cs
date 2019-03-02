using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpgTimer
{
    public partial class EpgEventInfo : AutoAddTargetDataStable
    {
        public override string DataTitle { get { return (ShortInfo == null ? "" : ShortInfo.event_name); } }
        public override ulong DataID { get { return CurrentPgUID(); } }
        public override DateTime PgStartTime { get { return StartTimeFlag != 0 ? start_time : DateTime.MaxValue; } }
        public override uint PgDurationSecond { get { return DurationFlag != 0 ? durationSec : 300; } }
        public override UInt64 Create64PgKey()
        {
            return CommonManager.Create64PgKey(original_network_id, transport_stream_id, service_id, event_id);
        }

        //過去番組関係用
        protected string serviceName = null;//DeepCopyでは無視
        public string ServiceName
        {
            get
            {
                if (serviceName == null)
                {
                    EpgServiceInfo item = ChSet5.ChItem(this.Create64Key(), true, true);
                    serviceName = (transport_stream_id != item.TSID ? "[廃]" : "") + item.service_name;
                }
                return serviceName;
            }
            set { serviceName = value; }
        }

        /// <summary>予約可能。StartTimeFlag != 0 && IsOver() != true</summary>
        public bool IsReservable
        {
            get { return StartTimeFlag != 0 && IsOver() != true; }
        }
        /// <summary>サービス2やサービス3の結合されるべきものはfalse </summary>
        public bool IsGroupMainEvent
        {
            get { return EventGroupInfo == null || EventGroupInfo.eventDataList.Any(data => data.Create64Key() == this.Create64Key()); }
        }
        /// <summary>サービス2やサービス3の結合されているもののメインイベント取得 </summary>
        public EpgEventInfo GetGroupMainEvent(Dictionary<UInt64, EpgEventInfo> currentList = null)
        {
            if (IsGroupMainEvent == true) return this;
            if (EventGroupInfo.group_type != 1) return null;
            return EventGroupInfo.eventDataList.Select(data =>
                MenuUtil.SearchEventInfo(CommonManager.CurrentPgUID(data.Create64PgKey(), PgStartTime), currentList))
                .FirstOrDefault(data => data != null && data.IsGroupMainEvent == true);
        }
    }
}