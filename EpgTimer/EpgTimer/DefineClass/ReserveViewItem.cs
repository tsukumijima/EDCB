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
                int idx = 0;
                if (Data.IsEnabled == false)
                {
                    idx = 2;
                }
                else if (Data is ReserveDataEnd)
                {
                    idx = 9;
                }
                else if (Data.OverlapMode == 2)
                {
                    idx = 3;
                }
                else if (this.EpgStyle().EpgChangeBorderOnRec == true && Data.IsOnRec() == true)
                {
                    idx = Data.IsWatchMode ? 8 : 7;
                }
                else if (Data.OverlapMode == 1)
                {
                    idx = 4;
                }
                else if (Data.IsAutoAddInvalid)
                {
                    idx = 5;
                }
                else if (Data.IsMultiple)
                {
                    idx = 6;
                }
                else if (this.EpgStyle().EpgChangeBorderWatch == false && Data.IsManual == true ||
                        this.EpgStyle().EpgChangeBorderWatch == true && Data.IsWatchMode == true)
                {
                    idx = 1;
                }
                return this.EpgBrushCache().ResFillColorList[idx];
            }
        }
        public override Brush BorderBrush
        {
            get
            {
                int idx = 0;
                if (Data.IsEnabled == false)
                {
                    idx = 2;
                }
                else if (Data is ReserveDataEnd)
                {
                    idx = 9;
                }
                else if (Data.OverlapMode == 2)
                {
                    idx = 3;
                }
                else if (this.EpgStyle().EpgChangeBorderOnRec == true && Data.IsOnRec() == true)
                {
                    idx = Data.IsWatchMode ? 8 : 7;
                }
                else if (Data.OverlapMode == 1)
                {
                    idx = 4;
                }
                else if (Data.IsAutoAddInvalid)
                {
                    idx = 5;
                }
                else if (Data.IsMultiple)
                {
                    idx = 6;
                }
                else if (this.EpgStyle().EpgChangeBorderWatch == false && Data.IsManual == true ||
                        this.EpgStyle().EpgChangeBorderWatch == true && Data.IsWatchMode == true)
                {
                    idx = 1;
                }
                return this.EpgBrushCache().ResColorList[idx];
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
                int idx = 0;
                if (Data.IsOnRec())
                {
                    idx = Data.IsWatchMode ? 4 : 3;
                }
                else if (Data.IsEnabled == false)
                {
                    idx = 2;
                }
                else if (Settings.Instance.TunerChangeBorderWatch == false && Data.IsManual == true ||
                        Settings.Instance.TunerChangeBorderWatch == true && Data.IsWatchMode == true)
                {
                    idx = 1;
                }
                return Settings.BrushCache.TunerResBorderColor[idx];
            }
        }
        public Brush ServiceColor
        {
            get
            {
                if (Settings.Instance.TunerColorModeUse == true)
                {
                    return Settings.BrushCache.CustTunerServiceColorPri[Data.RecSetting.Priority - 1];
                }
                return Settings.BrushCache.CustTunerServiceColor;
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
