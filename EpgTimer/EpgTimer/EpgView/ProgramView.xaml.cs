using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace EpgTimer.EpgView
{
    /// <summary>
    /// ProgramView.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgramView : PanelViewBase
    {
        protected override bool IsSingleClickOpen { get { return Settings.Instance.EpgInfoSingleClick; } }
        protected override double DragScroll { get { return Settings.Instance.DragScroll; } }
        protected override bool IsMouseScrollAuto { get { return Settings.Instance.MouseScrollAuto; } }
        protected override double ScrollSize { get { return Settings.Instance.ScrollSize; } }
        protected override bool IsPopEnabled { get { return Settings.Instance.EpgPopup == true; } }
        protected override bool PopOnOver { get { return Settings.Instance.EpgPopupMode != 1; } }
        protected override bool PopOnClick { get { return Settings.Instance.EpgPopupMode != 0; } }
        protected override FrameworkElement Popup { get { return popupItem; } }
        protected override ViewPanel PopPanel { get { return popupItemPanel; } }
        protected override double PopWidth { get { return Settings.Instance.ServiceWidth * Settings.Instance.EpgPopupWidth; } }

        private ReserveViewItem popInfoRes = null;

        protected override bool IsTooltipEnabled { get { return Settings.Instance.EpgToolTip == true; } }
        protected override int TooltipViweWait { get { return Settings.Instance.EpgToolTipViewWait; } }

        public ProgramView()
        {
            InitializeComponent();

            base.scroll = scrollViewer;
            base.cnvs = canvas;

            epgViewPanel.Background = CommonManager.Instance.EpgBackColor;
            epgViewPanel.SetBorderStyleFromSettings();
            epgViewPanel.Height = SystemParameters.VirtualScreenHeight;
            epgViewPanel.Width = SystemParameters.VirtualScreenWidth;
        }

        public override void ClearInfo()
        {
            base.ClearInfo();
            ClearReserveViewPanel();
            ClearEpgViewPanel();
            //デフォルト状態に戻す
            canvas.Children.Add(epgViewPanel);
        }
        private void ClearReserveViewPanel() { ClearPanel(typeof(Rectangle)); }
        private void ClearEpgViewPanel() { ClearPanel(typeof(EpgViewPanel)); }
        private void ClearPanel(Type t)
        {
            for (int i = 0; i < canvas.Children.Count; i++)
            {
                if (canvas.Children[i].GetType() == t)
                {
                    canvas.Children.RemoveAt(i--);
                }
            }
        }

        protected override void PopupClear()
        {
            base.PopupClear();
            popInfoRes = null;
        }
        protected override PanelItem GetPopupItem(Point cursorPos, bool onClick)
        {
            ProgramViewItem popInfo = GetProgramViewData(cursorPos);
            ReserveViewItem lastPopInfoRes = popInfoRes;
            popInfoRes = GetReserveViewData(cursorPos);

            if (Settings.Instance.EpgPopupMode == 2 && popInfoRes == null && (
                onClick == false && !(lastPopInfoRes == null && popInfo == lastPopInfo) ||
                onClick == true && lastPopInfo != null)) return null;

            //予約枠を通過したので同じ番組でもポップアップを書き直させる。
            if (lastPopInfoRes != popInfoRes)
            {
                base.PopupClear();
            }

            return popInfo;
        }
        protected override void SetPopupItemEx(PanelItem item)
        {
            (PopPanel.Item as ProgramViewItem).DrawHours = (item as ProgramViewItem).DrawHours;
            popupItemBorder.Visibility = Visibility.Collapsed;
            popupItemFillOnly.Visibility = Visibility.Collapsed;
            if (popInfoRes != null)
            {
                popupItemBorder.Visibility = Visibility.Visible;
                if (Settings.Instance.ReserveRectFillWithShadow == false) popupItemFillOnly.Visibility = Visibility.Visible;
                SetReserveBorderColor(popInfoRes, popupItemBorder, Settings.Instance.ReserveRectFillWithShadow ? null : popupItemFillOnly);
            }
        }

        protected override PanelItem GetTooltipItem(Point cursorPos)
        {
            return GetProgramViewData(cursorPos);
        }
        protected override void SetTooltip(PanelItem toolInfo)
        {
            var info = toolInfo as ProgramViewItem;
            if (info.TitleDrawErr == false && Settings.Instance.EpgToolTipNoViewOnly == true) return;

            Tooltip.ToolTip = ViewUtil.GetTooltipBlockStandard(CommonManager.ConvertProgramText(info.Data,
                Settings.Instance.EpgExtInfoTooltip == true ? EventInfoTextMode.All : EventInfoTextMode.BasicText));
        }

        public ReserveViewItem GetReserveViewData(Point cursorPos)
        {
            return canvas.Children.OfType<Rectangle>().Select(rs => rs.Tag).OfType<ReserveViewItem>().FirstOrDefault(pg => pg.IsPicked(cursorPos));
        }
        public ProgramViewItem GetProgramViewData(Point cursorPos)
        {
            return canvas.Children.OfType<EpgViewPanel>()
                .Where(panel => panel.Items != null && Canvas.GetLeft(panel) <= cursorPos.X && cursorPos.X < Canvas.GetLeft(panel) + panel.Width)
                .SelectMany(panel => panel.Items).OfType<ProgramViewItem>().FirstOrDefault(pg => pg.IsPicked(cursorPos));
        }

        private void SetReserveBorderColor(ReserveViewItem info, Rectangle rect, Rectangle fillOnlyRect = null)
        {
            rect.Stroke = info.BorderBrush;
            rect.Effect = new System.Windows.Media.Effects.DropShadowEffect() { BlurRadius = 10 };
            rect.StrokeThickness = 3;
            (fillOnlyRect ?? rect).Fill = info.BackColor;
        }
        public void SetReserveList(IEnumerable<ReserveViewItem> reserveList)
        {
            try
            {
                ClearReserveViewPanel();
                PopupClear();
                TooltipClear();

                var AddRect = new Func<ReserveViewItem, int, object, Rectangle>((info, zIdx, tag) =>
                {
                    var rect = new Rectangle();
                    rect.Width = info.Width;
                    rect.Height = info.Height;
                    rect.IsHitTestVisible = false;
                    rect.Tag = tag;
                    Canvas.SetLeft(rect, info.LeftPos);
                    Canvas.SetTop(rect, info.TopPos);
                    Canvas.SetZIndex(rect, zIdx);
                    canvas.Children.Add(rect);
                    return rect;
                });

                foreach (ReserveViewItem info in reserveList)
                {
                    var rect = AddRect(info, 10, info);
                    var fillOnlyRect = Settings.Instance.ReserveRectFillWithShadow ? null : AddRect(info, 9, null);
                    SetReserveBorderColor(info, rect, fillOnlyRect);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public Rect SetProgramList(List<ProgramViewItem> programList, double width, double height)
        {
            return SetProgramList(new PanelItem<List<ProgramViewItem>>(programList) { Width = width }.IntoList(), height);
        }
        public Rect SetProgramList(List<PanelItem<List<ProgramViewItem>>> programGroupList, double height)
        {
            try
            {
                ClearEpgViewPanel();

                //枠線の調整用
                double totalWidth = 0;
                height = ViewUtil.SnapsToDevicePixelsY(height + epgViewPanel.HeightMarginBottom, 2);
                foreach (var programList in programGroupList)
                {
                    var item = new EpgViewPanel();
                    item.Background = epgViewPanel.Background;
                    item.SetBorderStyleFromSettings();
                    item.Height = height;
                    item.Width = programList.Width;
                    Canvas.SetLeft(item, totalWidth);
                    item.Items = programList.Data;
                    item.InvalidateVisual();
                    canvas.Children.Add(item);
                    totalWidth += programList.Width;
                }

                canvas.Width = ViewUtil.SnapsToDevicePixelsX(totalWidth + epgViewPanel.WidthMarginRight, 2);
                canvas.Height = height;
                epgViewPanel.Width = Math.Max(canvas.Width, ViewUtil.SnapsToDevicePixelsX(SystemParameters.VirtualScreenWidth));
                epgViewPanel.Height = Math.Max(canvas.Height, ViewUtil.SnapsToDevicePixelsY(SystemParameters.VirtualScreenHeight));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return new Rect(0, 0, canvas.Width, canvas.Height);
        }
    }
}
