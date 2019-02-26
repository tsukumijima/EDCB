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
                    ChSet5Item item = ChSet5.ChItem(this.Create64Key(), true, true);
                    serviceName = (transport_stream_id != item.TSID ? "[廃]" : "") + item.ServiceName;
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
        public EpgEventInfo GetGroupMainEvent()
        {
            if (IsGroupMainEvent == true) return this;
            if (EventGroupInfo.group_type != 1) return null;
            return EventGroupInfo.eventDataList.Select(data => MenuUtil.SearchEventInfo(data.Create64PgKey()))
                                    .FirstOrDefault(data => data != null && data.IsGroupMainEvent == true);
        }
    }

    public partial class EpgServiceInfo
    {
        public UInt64 Create64Key() { return CommonManager.Create64Key(ONID, TSID, SID); }

        public ChSet5Item ToItem(bool chSetReference = true)
        {
            return (chSetReference ? ChSet5.ChItem(Create64Key()) : null)
                    ?? new ChSet5Item
                    {
                        Key = Create64Key(),
                        NetworkName = network_name,
                        PartialFlag = partialReceptionFlag == 1,
                        ServiceName = service_name,
                        ServiceType = service_type,
                    };
        }

        public static EpgServiceInfo FromKey(UInt64 key)
        {
            ChSet5Item item = ChSet5.ChItem(key, true, true);
            EpgServiceInfo info = item.ToInfo();
            if (item.Key != key)
            {
                //TSID移動前のチャンネルだった場合
                info.TSID = (ushort)(key >> 16);
            }
            else if (string.IsNullOrEmpty(info.service_name))
            {
                //ChSet5で全く見つからず、キーだけが入って戻ってきた場合
                info.network_name = CommonManager.ConvertNetworkNameText(item.ONID);
                //info.partialReceptionFlag = 0;不明
                info.remote_control_key_id = item.RemoconID();
                info.service_name = "[不明]";
                info.service_provider_name = info.network_name;
                //info.service_type = 0x01;不明
                info.ts_name = info.network_name;
            }
            return info;
        }
    }
}