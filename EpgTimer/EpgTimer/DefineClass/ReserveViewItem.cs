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
                return 7 + SelectBorder(this.EpgStyle().EpgChangeBorderMode);
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
            return SelectBorder(this.EpgStyle().EpgChangeBorderMode);
        }
        protected int SelectBorder(uint mode)
        {
            return mode == 0 && !Data.IsAutoAdded || mode == 1 && Data.IsManual ? 1 : 0;
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
                return 3 + SelectBorder(Settings.Instance.TunerChangeBorderMode);
            }
            if (Data.IsEnabled == false)
            {
                return 2;
            }
            return SelectBorder(Settings.Instance.TunerChangeBorderMode);
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
