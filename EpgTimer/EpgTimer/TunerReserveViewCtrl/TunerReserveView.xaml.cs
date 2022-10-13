using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace EpgTimer.TunerReserveViewCtrl
{
    /// <summary>
    /// TunerReserveView.xaml の相互作用ロジック
    /// </summary>
    public partial class TunerReserveView : PanelViewBase
    {
        protected override bool IsSingleClickOpen { get { return Settings.Instance.TunerInfoSingleClick; } }
        protected override double DragScroll { get { return Settings.Instance.TunerDragScroll; } }
        protected override bool IsMouseScrollAuto { get { return Settings.Instance.TunerMouseScrollAuto; } }
        protected override double ScrollSize { get { return Settings.Instance.TunerScrollSize; } }
        protected override bool IsMouseHorizontalScrollAuto { get { return Settings.Instance.TunerMouseHorizontalScrollAuto; } }
        protected override double HorizontalScrollSize { get { return Settings.Instance.TunerHorizontalScrollSize; } }
        protected override bool IsPopEnabled { get { return Settings.Instance.TunerPopup == true; } }
        protected override bool PopOnOver { get { return Settings.Instance.TunerPopupMode == 0; } }
        protected override bool PopOnClick { get { return Settings.Instance.TunerPopupMode == 1; } }
        protected override FrameworkElement Popup { get { return popupItem; } }
        protected override ViewPanel PopPanel { get { return popupItemPanel; } }
        protected override double PopWidth { get { return Settings.Instance.TunerWidth * Settings.Instance.TunerPopupWidth; } }

        protected override bool IsTooltipEnabled { get { return Settings.Instance.TunerToolTip == true; } }
        protected override int TooltipViweWait { get { return Settings.Instance.TunerToolTipViewWait; } }

        public TunerReserveView()
        {
            InitializeComponent();

            base.scroll = scrollViewer;
            base.cnvs = canvas;

            reserveViewPanel.Height = SystemParameters.VirtualScreenHeight;
            reserveViewPanel.Width = SystemParameters.VirtualScreenWidth;
        }

        public void SetReserveList(List<TunerReserveViewItem> reserveList, double width, double height)
        {
            try
            {
                reserveViewPanel.SetBorderStyleFromSettings();
                canvas.Width = ViewUtil.SnapsToDevicePixelsX(width + reserveViewPanel.WidthMarginRight, 2);//右端のチューナ列の線を描画するため+1。他の+1も同じ。;
                canvas.Height = ViewUtil.SnapsToDevicePixelsY(height + reserveViewPanel.HeightMarginBottom, 2);
                reserveViewPanel.Width = Math.Max(canvas.Width, ViewUtil.SnapsToDevicePixelsX(SystemParameters.VirtualScreenWidth));
                reserveViewPanel.Height = Math.Max(canvas.Height, ViewUtil.SnapsToDevicePixelsY(SystemParameters.VirtualScreenHeight));
                reserveViewPanel.Items = reserveList;
                reserveViewPanel.InvalidateVisual();

                PopupClear();
                TooltipClear();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        protected override PanelItem GetPopupItem(Point cursorPos, bool onClick)
        {
            if (reserveViewPanel.Items == null) return null;

            return reserveViewPanel.Items.FirstOrDefault(pg => pg.IsPicked(cursorPos));
        }

        protected override PanelItem GetTooltipItem(Point cursorPos)
        {
            return GetPopupItem(cursorPos, false);
        }
        protected override void SetTooltip(PanelItem toolInfo)
        {
            Tooltip.ToolTip = ViewUtil.GetTooltipBlockStandard(new ReserveItem((toolInfo as TunerReserveViewItem).Data)
                                    .ConvertInfoText(Settings.Instance.TunerToolTipMode));
        }
    }
}
