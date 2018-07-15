using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace EpgTimer
{
    public class ServiceViewItem : SelectableItemNWMode
    {
        public ServiceViewItem(ChSet5Item info)
        {
            ServiceInfo = info;
        }
        public readonly ChSet5Item ServiceInfo;
        public UInt64 Key
        { 
            get { return ServiceInfo.Key; }
        }
        public String NetworkName
        {
            get { return CommonManager.ConvertNetworkNameText(ServiceInfo.ONID); }
        }
        public String ServiceName
        { 
            get { return ServiceInfo.ServiceName; }
        }
        public String ServiceType
        {
            get { return CommonManager.ServiceTypeList[(byte)ServiceInfo.ServiceType]; }
        }
        public string IsVideo
        {
            get { return ServiceInfo.IsVideo == true ? "○" : ""; }
        }
        public string IsPartial
        {
            get { return ServiceInfo.PartialFlag == true ? "○" : ""; }
        }
        public TextBlock ToolTipView
        {
            get
            {
                if (Settings.Instance.NoToolTip == true) return null;
                //
                return ViewUtil.GetTooltipBlockStandard(ConvertInfoText());
            }
        }
        public string ConvertInfoText()
        {
            return "ServiceName : " + ServiceName + "\r\n" +
                "ServiceType : " + ServiceType + " (0x" + ServiceInfo.ServiceType.ToString("X2") + ")" + "\r\n" +
                CommonManager.Convert64KeyString(ServiceInfo.Key) + "\r\n" +
                "PartialReception : " + (ServiceInfo.PartialFlag == true ? "ワンセグ" : "-") + " (0x" + (ServiceInfo.PartialFlag ? 1 : 0).ToString("X2") + ")";
        }
        public override string ToString() { return ServiceName; }
    }
}
