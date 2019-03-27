using System;
using System.Windows.Media;

namespace EpgTimer
{
    public class ReserveViewItem : PanelItem<ReserveData>
    {
        public ReserveViewItem(ReserveData info) : base(info) { }

        public override Brush BackColor
        {
            get
            {
                if (Data.IsEnabled == false)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[2];
                }
                if (Data is ReserveDataEnd)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[9];
                }
                if (Data.OverlapMode == 2)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[3];
                }
                if (Settings.Instance.EpgChangeBorderOnRec == true && Data.IsOnRec() == true)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[Data.IsWatchMode ? 8 : 7];
                }
                if (Data.OverlapMode == 1)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[4];
                }
                if (Data.IsAutoAddInvalid == true)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[5];
                }
                if (Data.IsMultiple == true)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[6];
                }
                if (Settings.Instance.EpgChangeBorderWatch == false && Data.IsManual == true ||
                        Settings.Instance.EpgChangeBorderWatch == true && Data.IsWatchMode == true)
                {
                    return CommonManager.Instance.CustEpgResFillColorList[1];
                }
                return CommonManager.Instance.CustEpgResFillColorList[0];
            }
        }
        public override Brush BorderBrush
        {
            get
            {
                if (Data.IsEnabled == false)
                {
                    return CommonManager.Instance.CustEpgResColorList[2];
                }
                if (Data is ReserveDataEnd)
                {
                    return CommonManager.Instance.CustEpgResColorList[9];
                }
                if (Data.OverlapMode == 2)
                {
                    return CommonManager.Instance.CustEpgResColorList[3];
                }
                if (Settings.Instance.EpgChangeBorderOnRec == true && Data.IsOnRec() == true)
                {
                    return CommonManager.Instance.CustEpgResColorList[Data.IsWatchMode ? 8 : 7];
                }
                if (Data.OverlapMode == 1)
                {
                    return CommonManager.Instance.CustEpgResColorList[4];
                }
                if (Data.IsAutoAddInvalid == true)
                {
                    return CommonManager.Instance.CustEpgResColorList[5];
                }
                if (Data.IsMultiple == true)
                {
                    return CommonManager.Instance.CustEpgResColorList[6];
                }
                if (Settings.Instance.EpgChangeBorderWatch == false && Data.IsManual == true ||
                        Settings.Instance.EpgChangeBorderWatch == true && Data.IsWatchMode == true)
                {
                    return CommonManager.Instance.CustEpgResColorList[1];
                }
                return CommonManager.Instance.CustEpgResColorList[0];
            }
        }
    }

    public class TunerReserveViewItem : ReserveViewItem
    {
        public TunerReserveViewItem(ReserveData info) : base(info) { }

        public override Brush BackColor { get { return ViewUtil.ReserveErrBrush(Data); } }
        public override Brush BorderBrush
        {
            get
            {
                if (Data.IsOnRec() == true)
                {
                    return CommonManager.Instance.TunerResBorderColor[Data.IsWatchMode ? 4 : 3];
                }
                if (Data.IsEnabled == false)
                {
                    return CommonManager.Instance.TunerResBorderColor[2];
                }
                if (Settings.Instance.TunerChangeBorderWatch == false && Data.IsManual == true ||
                        Settings.Instance.TunerChangeBorderWatch == true && Data.IsWatchMode == true)
                {
                    return CommonManager.Instance.TunerResBorderColor[1];
                }
                return CommonManager.Instance.TunerResBorderColor[0];
            }
        }
        public Brush ServiceColor
        {
            get
            {
                if (Settings.Instance.TunerColorModeUse == true)
                {
                    return CommonManager.Instance.CustTunerServiceColorPri[Data.RecSetting.Priority - 1];
                }
                return CommonManager.Instance.CustTunerServiceColor;
            }
        }
        public string Status
        {
            get
            {
                if (Data.IsOnRec() == true)
                {
                    if (Data.IsEnabled == false || Data.OverlapMode == 2)
                    {
                        return "放送中*";
                    }
                    string RecStr = Data.IsWatchMode == true ? "視聴中*" : "録画中*";
                    if (Data.OverlapMode == 1)
                    {
                        return "一部のみ" + RecStr;
                    }
                    return RecStr;
                }
                return "";
            }
        }
    }
}
