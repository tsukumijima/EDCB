using System;
using System.Windows.Media;

namespace EpgTimer
{
    public class ReserveViewItem : PanelItem<ReserveData>
    {
        public ReserveViewItem(ReserveData info) : base(info) { }

        public override Brush BackColor { get { return this.EpgBrushCache().ResFillColorList[BrushIdx()]; } }
        public override Brush BorderBrush { get { return this.EpgBrushCache().ResColorList[BrushIdx()]; } }
        protected virtual int BrushIdx()
        {
            if (Data is ReserveDataEnd)
            {
                return 9;
            }
            if (Data.IsEnabled == false)
            {
                return 2;
            }
            if (Data.OverlapMode == 2)
            {
                return 3;
            }
            if ((ViewMode == 1 || this.EpgStyle().EpgChangeBorderOnRecWeekOnly == false) && Data.IsOnRec())
            {
                return Data.IsManual ? 8 : 7;
            }
            if (Data.OverlapMode == 1)
            {
                return 4;
            }
            if (Data.IsAutoAddInvalid)
            {
                return 5;
            }
            if (Data.IsMultiple)
            {
                return 6;
            }
            return Data.IsManual ? 1 : 0;
        }
    }

    public class TunerReserveViewItem : ReserveViewItem
    {
        public TunerReserveViewItem(ReserveData info) : base(info) { }

        public override Brush BackColor { get { return ViewUtil.ReserveErrBrush(Data); } }
        public override Brush BorderBrush { get { return Settings.BrushCache.TunerResBorderColor[BrushIdx()]; } }
        protected override int BrushIdx()
        {
            if (Data.IsOnRec())
            {
                return Data.IsManual ? 4 : 3;
            }
            if (Data.IsEnabled == false)
            {
                return 2;
            }
            return Data.IsManual ? 1 : 0;
        }
        public Brush ServiceColor
        {
            get
            {
                return Settings.BrushCache.CustTunerServiceColor[Settings.Instance.TunerColorModeUse ? Data.RecSetting.Priority : 0];
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
