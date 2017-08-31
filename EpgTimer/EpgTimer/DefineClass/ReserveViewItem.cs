using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EpgTimer
{
    public class ReserveViewItem : PanelItem<ReserveData>, IViewPanelItem
    {
        public ReserveViewItem(ReserveData info) : base(info) { }
        public ReserveData ReserveInfo { get { return Data; } protected set { Data = value; } }

        public virtual Brush BorderBrush
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsEnabled == false)
                    {
                        return CommonManager.Instance.CustEpgResColorList[2];
                    }
                    if (ReserveInfo.OverlapMode == 2)
                    {
                        return CommonManager.Instance.CustEpgResColorList[3];
                    }
                    if (ReserveInfo.OverlapMode == 1)
                    {
                        return CommonManager.Instance.CustEpgResColorList[4];
                    }
                    if (ReserveInfo.IsAutoAddInvalid == true)
                    {
                        return CommonManager.Instance.CustEpgResColorList[5];
                    }
                    if (ReserveInfo.IsMultiple == true)
                    {
                        return CommonManager.Instance.CustEpgResColorList[6];
                    }
                    if (ReserveInfo.IsManual == true)
                    {
                        return CommonManager.Instance.CustEpgResColorList[1];
                    }
                }
                return CommonManager.Instance.CustEpgResColorList[0];
            }
        }

        public virtual Brush FillBrush
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsEnabled == false)
                    {
                        return CommonManager.Instance.CustEpgResFillColorList[2];
                    }
                    if (ReserveInfo.OverlapMode == 2)
                    {
                        return CommonManager.Instance.CustEpgResFillColorList[3];
                    }
                    if (ReserveInfo.OverlapMode == 1)
                    {
                        return CommonManager.Instance.CustEpgResFillColorList[4];
                    }
                    if (ReserveInfo.IsAutoAddInvalid == true)
                    {
                        return CommonManager.Instance.CustEpgResFillColorList[5];
                    }
                    if (ReserveInfo.IsMultiple == true)
                    {
                        return CommonManager.Instance.CustEpgResFillColorList[6];
                    }
                    if (ReserveInfo.IsManual == true)
                    {
                        return CommonManager.Instance.CustEpgResFillColorList[1];
                    }
                }
                return CommonManager.Instance.CustEpgResFillColorList[0];
            }
        }
    }

    public class TunerReserveViewItem : ReserveViewItem
    {
        public TunerReserveViewItem(ReserveData info) : base(info) { }

        public override Brush BorderBrush
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsOnRec() == true)
                    {
                        return CommonManager.Instance.StatRecForeColor;
                    }
                    if (ReserveInfo.IsEnabled == false)
                    {
                        return CommonManager.Instance.TunerReserveOffBorderColor;
                    }
                    if (ReserveInfo.IsManual == true)
                    {
                        return CommonManager.Instance.TunerReserveProBorderColor;
                    }
                }
                return CommonManager.Instance.TunerReserveBorderColor;
            }
        }
        public Brush ForeColorPri
        {
            get
            {
                if (ReserveInfo == null) return Brushes.Black;

                return CommonManager.Instance.CustTunerServiceColorPri[ReserveInfo.RecSetting.Priority - 1];
            }
        }
        public Brush BackColor
        {
            get
            {
                return ViewUtil.ReserveErrBrush(ReserveInfo);
            }
        }
        public String Status
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsOnRec() == true)
                    {
                        if (ReserveInfo.IsEnabled == false || ReserveInfo.OverlapMode == 2)
                        {
                            return "放送中*";
                        }
                        if (ReserveInfo.OverlapMode == 1)
                        {
                            return "一部のみ録画中*";
                        }
                        return "録画中*";
                    }
                }
                return "";
            }
        }
    }

}
