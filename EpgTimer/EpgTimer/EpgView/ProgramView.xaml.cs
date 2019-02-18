using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

        private List<ReserveViewItem> reserveList = null;
        private List<Rectangle> rectBorder = new List<Rectangle>();
        private ReserveViewItem popInfoRes = null;

        protected override bool IsTooltipEnabled { get { return Settings.Instance.EpgToolTip == true; } }
        protected override int TooltipViweWait { get { return Settings.Instance.EpgToolTipViewWait; } }

        public ProgramView()
        {
            InitializeComponent();

            base.scroll = scrollViewer;
            base.cnvs = canvas;

            epgViewPanel.ReplaceDictionaryNormal = CommonManager.ReplaceDictionaryNormal;
            epgViewPanel.ReplaceDictionaryTitle = CommonManager.ReplaceDictionaryTitle;
            epgViewPanel.Background = CommonManager.Instance.EpgBackColor;
            epgViewPanel.ExtInfoMode = Settings.Instance.EpgExtInfoTable;
            epgViewPanel.Height = ViewUtil.GetScreenHeightMax();
            epgViewPanel.Width = ViewUtil.GetScreenWidthMax();
            epgViewPanel.SetBorderStyleFromSettings();

            popupItemPanel.ReplaceDictionaryNormal = epgViewPanel.ReplaceDictionaryNormal;
            popupItemPanel.ReplaceDictionaryTitle = epgViewPanel.ReplaceDictionaryTitle;
            Canvas.SetLeft(popupItemPanel, 0);
        }

        public override void ClearInfo()
        {
            base.ClearInfo();
            reserveList = null;
            rectBorder.ForEach(item => canvas.Children.Remove(item));
            rectBorder.Clear();
            ClearEpgViewPanel();
        }
        private void ClearEpgViewPanel()
        {
            for (int i = 0; i < canvas.Children.Count; i++)
            {
                if (canvas.Children[i] is EpgViewPanel)
                {
                    canvas.Children.RemoveAt(i--);
                }
            }
            //デフォルト状態に戻す
            canvas.Children.Add(epgViewPanel);
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
            popInfoRes = reserveList == null ? null : reserveList.Find(pg => pg.IsPicked(cursorPos));

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
        protected override void SetPopup(PanelItem item)
        {
            //この番組だけのEpgViewPanelをつくる
            PopPanel.ExtInfoMode = Settings.Instance.EpgExtInfoPopup;
            SetPopPanel(item);

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

        public ProgramViewItem GetProgramViewData(Point cursorPos)
        {
            foreach (var childPanel in canvas.Children.OfType<EpgViewPanel>())
            {
                if (childPanel.Items != null && Canvas.GetLeft(childPanel) <= cursorPos.X && cursorPos.X < Canvas.GetLeft(childPanel) + childPanel.Width)
                {
                    return childPanel.Items.OfType<ProgramViewItem>().FirstOrDefault(pg => pg.IsPicked(cursorPos));
                }
            }

            return null;
        }

        private void SetReserveBorderColor(ReserveViewItem info, Rectangle rect, Rectangle fillOnlyRect = null)
        {
            rect.Stroke = info.BorderBrush;
            rect.Effect = new System.Windows.Media.Effects.DropShadowEffect() { BlurRadius = 10 };
            rect.StrokeThickness = 3;
            (fillOnlyRect ?? rect).Fill = info.BackColor;
        }
        public void SetReserveList(List<ReserveViewItem> resList)
        {
            try
            {
                reserveList = resList;
                rectBorder.ForEach(item => canvas.Children.Remove(item));
                rectBorder.Clear();

                foreach (ReserveViewItem info in reserveList)
                {
                    var rect = new Rectangle();
                    rect.Width = info.Width;
                    rect.Height = info.Height;
                    rect.IsHitTestVisible = false;
                    Canvas.SetLeft(rect, info.LeftPos);
                    Canvas.SetTop(rect, info.TopPos);
                    Canvas.SetZIndex(rect, 10);
                    canvas.Children.Add(rect);
                    rectBorder.Add(rect);

                    var fillOnlyRect = Settings.Instance.ReserveRectFillWithShadow ? null : new Rectangle();
                    if (fillOnlyRect != null)
                    {
                        fillOnlyRect.Width = info.Width;
                        fillOnlyRect.Height = info.Height;
                        fillOnlyRect.IsHitTestVisible = false;
                        Canvas.SetLeft(fillOnlyRect, info.LeftPos);
                        Canvas.SetTop(fillOnlyRect, info.TopPos);
                        Canvas.SetZIndex(fillOnlyRect, 9);
                        canvas.Children.Add(fillOnlyRect);
                        rectBorder.Add(fillOnlyRect);
                    }

                    SetReserveBorderColor(info, rect, fillOnlyRect);
                }

                PopUpWork();
                TooltipWork();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public Rect SetProgramList(List<ProgramViewItem> programList, double width, double height)
        {
            return SetProgramList(CommonUtil.ToList(new PanelItem<List<ProgramViewItem>>(programList) { Width = width }), height);
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
                    item.ReplaceDictionaryNormal = epgViewPanel.ReplaceDictionaryNormal;
                    item.ReplaceDictionaryTitle = epgViewPanel.ReplaceDictionaryTitle;
                    item.Background = epgViewPanel.Background;
                    item.SetBorderStyleFromSettings();
                    item.Height = height;
                    item.Width = programList.Width;
                    Canvas.SetLeft(item, totalWidth);
                    item.ExtInfoMode = epgViewPanel.ExtInfoMode;
                    item.Items = programList.Data;
                    item.InvalidateVisual();
                    canvas.Children.Add(item);
                    totalWidth += programList.Width;
                }

                canvas.Width = ViewUtil.SnapsToDevicePixelsX(totalWidth + epgViewPanel.WidthMarginRight, 2);
                canvas.Height = height;
                epgViewPanel.Width = Math.Max(canvas.Width, ViewUtil.SnapsToDevicePixelsX(ViewUtil.GetScreenWidthMax()));
                epgViewPanel.Height = Math.Max(canvas.Height, ViewUtil.SnapsToDevicePixelsY(ViewUtil.GetScreenHeightMax()));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return new Rect(0, 0, canvas.Width, canvas.Height);
        }
    }
}
