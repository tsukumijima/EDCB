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
    public partial class ProgramView : PanelViewBase, IEpgSettingAccess, IEpgViewDataSet
    {
        protected override bool IsSingleClickOpen { get { return this.EpgStyle().EpgInfoSingleClick; } }
        protected override double DragScroll { get { return this.EpgStyle().DragScroll; } }
        protected override bool IsMouseScrollAuto { get { return this.EpgStyle().MouseScrollAuto; } }
        protected override double ScrollSize { get { return this.EpgStyle().ScrollSize; } }
        protected override bool IsMouseHorizontalScrollAuto { get { return this.EpgStyle().MouseHorizontalScrollAuto; } }
        protected override double HorizontalScrollSize { get { return this.EpgStyle().HorizontalScrollSize; } }
        protected override bool IsPopEnabled { get { return this.EpgStyle().EpgPopup == true; } }
        protected override bool PopOnOver { get { return this.EpgStyle().EpgPopupMode != 1; } }
        protected override bool PopOnClick { get { return this.EpgStyle().EpgPopupMode != 0; } }
        protected override FrameworkElement Popup { get { return popupItem; } }
        protected override ViewPanel PopPanel { get { return popupItemPanel; } }
        protected override double PopWidth { get { return this.EpgStyle().ServiceWidth * this.EpgStyle().EpgPopupWidth; } }

        private ReserveViewItem popInfoRes = null;

        protected override bool IsTooltipEnabled { get { return this.EpgStyle().EpgToolTip == true; } }
        protected override int TooltipViweWait { get { return this.EpgStyle().EpgToolTipViewWait; } }

        public ProgramView()
        {
            InitializeComponent();

            base.scroll = scrollViewer;
            base.cnvs = canvas;

            epgViewPanel.Height = SystemParameters.VirtualScreenHeight;
            epgViewPanel.Width = SystemParameters.VirtualScreenWidth;
        }

        protected EpgViewData viewData;
        public int EpgSettingIndex { get; private set; }
        public void SetViewData(EpgViewData data)
        {
            viewData = data;
            EpgSettingIndex = data.EpgSettingIndex;
            popupItemPanel.SetViewData(data);
            epgViewPanel.SetViewData(data);
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
            popInfoRes = GetReserveViewData(cursorPos).FirstOrDefault();

            if (this.EpgStyle().EpgPopupMode == 2 && popInfoRes == null && (
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
            (PopPanel.Item as ProgramViewItem).DrawHours |= (item as ProgramViewItem).DrawHours;
            (PopPanel.Item as ProgramViewItem).EpgSettingIndex = (item as ProgramViewItem).EpgSettingIndex;
            (PopPanel.Item as ProgramViewItem).ViewMode = (item as ProgramViewItem).ViewMode;
            //(PopPanel.Item as ProgramViewItem).Filtered = (item as ProgramViewItem).Filtered;//ポップアップ時は通常表示
            popupItemBorder.Visibility = Visibility.Collapsed;
            popupItemFillOnly.Visibility = Visibility.Collapsed;
            if (popInfoRes != null)
            {
                popupItemBorder.Visibility = Visibility.Visible;
                if (this.EpgStyle().ReserveRectFillWithShadow == false) popupItemFillOnly.Visibility = Visibility.Visible;
                SetReserveBorderColor(popInfoRes, popupItemBorder, this.EpgStyle().ReserveRectFillWithShadow ? null : popupItemFillOnly);
            }
        }

        protected override PanelItem GetTooltipItem(Point cursorPos)
        {
            return GetProgramViewData(cursorPos);
        }
        protected override void SetTooltip(PanelItem toolInfo)
        {
            var info = toolInfo as ProgramViewItem;
            if (info.TitleDrawErr == false && this.EpgStyle().EpgToolTipNoViewOnly == true) return;

            string text = CommonManager.ConvertProgramText(info.Data, EventInfoTextMode.BasicInfo)
                + CommonManager.ConvertProgramText(info.Data, EventInfoTextMode.BasicText);
            if (this.EpgStyle().EpgExtInfoTooltip)
            {
                text += CommonManager.TrimHyphenSpace(CommonManager.ConvertProgramText(info.Data, EventInfoTextMode.ExtendedText))
                    + CommonManager.ConvertProgramText(info.Data, EventInfoTextMode.PropertyInfo);
            }
            Tooltip.ToolTip = ViewUtil.GetTooltipBlockStandard(text.TrimEnd());
        }

        public IEnumerable<ReserveViewItem> GetReserveViewData(Point cursorPos)
        {
            return canvas.Children.OfType<Rectangle>().Select(rs => rs.Tag).OfType<ReserveViewItem>().Where(pg => pg.IsPicked(cursorPos)).Reverse();
        }
        public ProgramViewItem GetProgramViewData(Point cursorPos)
        {
            return canvas.Children.OfType<EpgViewPanel>()
                .Where(panel => panel.Items != null && Canvas.GetLeft(panel) <= cursorPos.X && cursorPos.X < Canvas.GetLeft(panel) + panel.Width)
                .SelectMany(panel => panel.Items).OfType<ProgramViewItem>().FirstOrDefault(pg => pg.IsPicked(cursorPos));
        }
        public ProgramViewItem GetProgramViewDataNear(Point cursorPos, double minY = double.MinValue, double maxY = double.MaxValue)
        {
            return canvas.Children.OfType<EpgViewPanel>()
                .Where(panel => panel.Items != null && Canvas.GetLeft(panel) <= cursorPos.X && cursorPos.X < Canvas.GetLeft(panel) + panel.Width)
                .SelectMany(panel => panel.Items).OfType<ProgramViewItem>()
                .Where(pg => pg.LeftPos <= cursorPos.X && cursorPos.X < pg.RightPos && (pg.TopPos < minY && maxY <= pg.BottomPos || minY <= pg.TopPos && pg.TopPos < maxY || minY <= pg.BottomPos && pg.BottomPos < maxY))
                .OrderBy(pg => pg.TopPos <= cursorPos.Y && cursorPos.Y < pg.BottomPos ? 0 : Math.Min(Math.Abs(pg.TopPos - cursorPos.Y), Math.Abs(pg.BottomPos - cursorPos.Y))).FirstOrDefault();
        }

        private void SetReserveBorderColor(ReserveViewItem info, Rectangle rect, Rectangle fillOnlyRect = null)
        {
            rect.Stroke = info.BorderBrush;
            rect.Effect = Settings.BrushCache.EpgBlurEffect;
            rect.StrokeThickness = 3;
            (fillOnlyRect ?? rect).Fill = info.BackColor;
            rect.StrokeDashArray = info.Data.IsWatchMode ? Settings.BrushCache.EpgDashArray : null;
            rect.StrokeDashCap = PenLineCap.Round;
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

                var sortList = reserveList.OrderBy(r => (long)r.LeftPos << 32 | r.Data.ReserveID).ToList();
                for (int i = 0; i < sortList.Count; i++)
                {
                    ReserveViewItem info = sortList[i];
                    for (int j = i - 1; j >= 0 && sortList[j].LeftPos == info.LeftPos; j--)
                    {
                        //ほかの枠を覆ってしまう場合は少しだけ縮める。判定には若干余裕を持たせる。
                        if (18 <= sortList[j].Width && sortList[j].Width <= info.Width
                            && info.TopPos - sortList[j].TopPos < 6 && sortList[j].BottomPos - info.BottomPos < 6
                            //あまりないと思うが、6px未満で並んでいる番組の除外。(EventIDでの判定はプログラム予約があるので不可)
                            && info.BottomPos > sortList[j].TopPos && sortList[j].BottomPos > info.TopPos)
                        {
                            info.Width = sortList[j].Width - 6;
                        }
                    }
                    var rect = AddRect(info, 10, info);
                    var fillOnlyRect = this.EpgStyle().ReserveRectFillWithShadow ? null : AddRect(info, 9, null);
                    SetReserveBorderColor(info, rect, fillOnlyRect);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
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
                    item.SetViewData(viewData);
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
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            return new Rect(0, 0, canvas.Width, canvas.Height);
        }
    }
}
