using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace EpgTimer
{
    public class ViewPanel : Panel
    {
        public IEnumerable<PanelItem> Items { get; set; }
        public PanelItem Item { get { return Items == null ? null : Items.FirstOrDefault(); } set { Items = value.IntoList(); } }
        public bool PopUpMode { get; set; }

        public double MaxRenderHeight { get; protected set; }
        protected void SaveMaxRenderHeight(double val) { MaxRenderHeight = Math.Max(MaxRenderHeight, val); }
        public virtual double GetMaxRenderHeight(double? refHeight = null)
        {
            if (refHeight != null && Items != null) foreach (var item in Items) item.Height = (double)refHeight;
            m = ViewUtil.DeviceMatrix;
            CreateDrawTextList();
            return MaxRenderHeight;
        }

        protected double CulcRenderWidth(string text, ItemFont itemFont)
        {
            double width = 0;
            ushort glyphIndex;
            int glyphType;
            for (int i = 0; i < text.Length; i++)
            {
                width += itemFont.GlyphWidth(text, ref i, out glyphIndex, out glyphType);
            }
            return width;
        }

        protected double RenderText(List<Tuple<Brush, GlyphRun>> textDrawList, string text, ItemFont itemFont, double fontSize, Rect drawRect, double marginLeft, double margintTop, Brush fontColor, bool nowrap = false)
        {
            double lineHeight = ViewUtil.CulcLineHeight(fontSize);
            double x0 = drawRect.Left + marginLeft;
            double xMax = drawRect.Right;
            double y0 = drawRect.Top + margintTop - (lineHeight - fontSize);//行間オフセット
            double yMax = nowrap == true ? y0 : drawRect.Bottom;
            double y = y0;
            var getRenderHeight = new Func<double>(() => y - y0);

            for (int i = 0; i < text.Length;)
            {
                var glyphIndexes = new List<ushort>();
                var advanceWidths = new List<double>();
                int currentType = 0;
                double x = x0;
                double x1 = x0;//currentTypeの書き出しx座標
                y += lineHeight;

                var AddGlyphRun = new Action(() =>
                {
                    if (glyphIndexes.Count == 0) return;
                    var origin = new Point(Math.Round(x1 * m.M11) / m.M11 - selfLeft, Math.Round(y * m.M22) / m.M22 - selfTop);
                    textDrawList.Add(new Tuple<Brush, GlyphRun>(fontColor,
                        new GlyphRun(itemFont.GlyphType[currentType], 0, false, fontSize, glyphIndexes, origin, advanceWidths, null, null, null, null, null, null)));
                });

                for (; i < text.Length && text[i] != '\r' && text[i] != '\n'; i++)
                {
                    //この辞書検索が負荷の大部分を占めているのでテーブルルックアップする
                    ushort glyphIndex;
                    int glyphType;
                    double width = itemFont.GlyphWidth(text, ref i, out glyphIndex, out glyphType) * fontSize;

                    if (x + width > xMax)
                    {
                        AddGlyphRun();
                        if (y >= yMax) return getRenderHeight();//次の行無理

                        //次の行へ
                        glyphIndexes = new List<ushort>();
                        advanceWidths = new List<double>();
                        x = x1 = x0;
                        y += lineHeight;
                    }
                    else if (glyphType != currentType)
                    {
                        //フォントが変わった
                        AddGlyphRun();
                        glyphIndexes = new List<ushort>();
                        advanceWidths = new List<double>();
                        x1 = x;
                    }
                    currentType = glyphType;
                    glyphIndexes.Add(glyphIndex);
                    advanceWidths.Add(width);
                    x += width;
                }
                AddGlyphRun();
                if (y >= yMax) return getRenderHeight();//次の行無理

                i = text.IndexOf('\n', i);
                i = i < 0 ? text.Length : i + 1;
            }
            return getRenderHeight();
        }

        protected virtual Rect BorderRect(PanelItem info)
        {
            return new Rect(info.LeftPos - selfLeft, info.TopPos - selfTop, info.Width + borderThickness.Right, info.Height + borderThickness.Bottom);
        }
        protected virtual Pen BorderPen(PanelItem info)
        {
            return null;
        }
        protected virtual Rect BorderPenRect(PanelItem info)
        {
            return new Rect(info.LeftPos - selfLeft + borderMax / 2, info.TopPos - selfTop + borderMax / 2, info.Width + borderThickness.Right - borderMax, info.Height + borderThickness.Bottom - borderMax);
        }
        protected virtual Rect ContentRect(PanelItem info)
        {
            return new Rect(info.LeftPos - selfLeft + borderMargin.Left, info.TopPos - selfTop + borderMargin.Top, Math.Max(0, info.Width - borderMargin.Width), Math.Max(0, info.Height - borderMargin.Height));
        }
        protected virtual Rect TextRenderRect(PanelItem info)
        {
            return new Rect(info.LeftPos + txtMargin.Left, info.TopPos + txtMargin.Top, Math.Max(0, info.Width - txtMargin.Width), Math.Max(0, info.Height - txtMargin.Height));
        }

        protected Thickness borderThickness;
        protected Rect borderMargin;
        protected Rect txtMargin;
        protected double borderMax;
        public virtual void SetBorderStyleFromSettings() { }
        public void SetBorderStyle(double borderLeft, double borderTop, Thickness textPadding)
        {
            borderMax = CommonUtil.Max(1, borderLeft, borderTop);
            borderThickness.Left = borderLeft <= 1 ? Math.Max(0, borderLeft) : (borderLeft + 1) / 2;
            borderThickness.Top = borderTop <= 1 ? Math.Max(0, borderTop) : (borderTop + 1) / 2;
            borderThickness.Right = Math.Min(1, borderThickness.Left);//右へのはみ出し分
            borderThickness.Bottom = Math.Min(1, borderThickness.Top);//下へのはみ出し分
            borderMargin = new Rect(borderThickness.Left, borderThickness.Top, borderThickness.Left + Math.Max(0, borderThickness.Left - 1), borderThickness.Top + Math.Max(0, borderThickness.Top - 1));
            txtMargin = new Rect(borderMargin.Left + textPadding.Left, borderMargin.Top + textPadding.Top, borderMargin.Width + textPadding.Left + textPadding.Right, borderMargin.Height + textPadding.Top + textPadding.Bottom);
        }
        public virtual double WidthMarginRight { get { return borderThickness.Right; } }
        public virtual double HeightMarginBottom { get { return borderThickness.Bottom; } }

        protected virtual List<List<Tuple<Brush, GlyphRun>>> CreateDrawTextList()
        {
            MaxRenderHeight = 0;
            if (Items == null) return null;

            var textDrawLists = new List<List<Tuple<Brush, GlyphRun>>>();
            CreateDrawTextListMain(textDrawLists);
            MaxRenderHeight += txtMargin.Top + borderMargin.Height - borderMargin.Top;
            return textDrawLists;
        }
        protected virtual void CreateDrawTextListMain(List<List<Tuple<Brush, GlyphRun>>> textDrawLists) { }

        protected double selfLeft = 0;
        protected double selfTop = 0;
        protected Matrix m;
        protected override void OnRender(DrawingContext dc)
        {
            //右クリックメニュー用の背景描画もあるので必ず実行させる
            dc.DrawRectangle(Background, null, new Rect(RenderSize));

            selfLeft = Canvas.GetLeft(this);
            selfTop = Canvas.GetTop(this);
            if (double.IsNaN(selfLeft) == true) selfLeft = 0;
            if (double.IsNaN(selfTop) == true) selfTop = 0;
            m = ViewUtil.DeviceMatrix;

            List<List<Tuple<Brush, GlyphRun>>> textDrawLists = CreateDrawTextList();
            if (textDrawLists == null) return;

            int i = 0;
            foreach (PanelItem info in Items)
            {
                dc.DrawRectangle(info.BorderBrush, null, BorderRect(info));
                Pen borderPen = BorderPen(info);
                if (borderPen != null) dc.DrawRectangle(null, borderPen, BorderPenRect(info));
                Rect contentRect = ContentRect(info);
                dc.DrawRectangle(Background, null, contentRect);
                dc.DrawRectangle(info.BackColor, null, contentRect);
                dc.PushClip(new RectangleGeometry(contentRect));
                textDrawLists[i++].ForEach(item => dc.DrawGlyphRun(item.Item1, item.Item2));
                dc.Pop();
            }
        }
    }
    
    public class PanelViewBase : UserControl
    {
        public delegate void PanelViewClickHandler(object sender, Point cursorPos);
        public event PanelViewClickHandler RightClick = null;
        public event PanelViewClickHandler MouseClick = null;
        public event PanelViewClickHandler LeftDoubleClick = null;
        public event ScrollChangedEventHandler ScrollChanged = null;

        protected Point lastDownMousePos;
        protected double lastDownHOffset;
        protected double lastDownVOffset;
        protected bool isDrag = false;
        protected bool isDragMoved = false;
        protected HwndSource scrollViewerHwndSource;
        protected HwndSourceHook horizontalScrollMessageHook;

        protected virtual bool IsSingleClickOpen { get { return false; } }
        protected virtual double DragScroll { get { return 1; } }
        protected virtual bool IsMouseScrollAuto { get { return false; } }
        protected virtual double ScrollSize { get { return 240; } }
        protected virtual bool IsMouseHorizontalScrollAuto { get { return false; } }
        protected virtual double HorizontalScrollSize { get { return 150; } }

        protected virtual bool IsPopEnabled { get { return false; } }
        protected virtual bool PopOnOver { get { return false; } }
        protected virtual bool PopOnClick { get { return false; } }
        protected virtual PanelItem GetPopupItem(Point cursorPos, bool onClick) { return null; }
        protected virtual FrameworkElement Popup { get { return new FrameworkElement(); } }
        protected virtual ViewPanel PopPanel { get { return new ViewPanel(); } }
        protected PanelItem lastPopInfo = null;
        protected virtual double PopWidth { get { return 150; } }
        protected ScrollViewer scroll;
        protected Canvas cnvs;

        protected virtual bool IsTooltipEnabled { get { return false; } }
        protected virtual int TooltipViweWait { get { return 0; } }
        protected Rectangle Tooltip { get; private set; }
        protected PanelItem lastToolInfo = null;
        private DispatcherTimer toolTipTimer;//連続して出現するのを防止する
        protected virtual PanelItem GetTooltipItem(Point cursorPos) { return null; }

        public PanelViewBase()
        {
            toolTipTimer = new DispatcherTimer(DispatcherPriority.Normal);
            toolTipTimer.Tick += new EventHandler(toolTipTimer_Tick);

            Tooltip = new Rectangle();
            Tooltip.Fill = Brushes.Transparent;
            Tooltip.Stroke = Brushes.Transparent;
            //Tooltip.ToolTipClosing += new ToolTipEventHandler((sender, e) => TooltipClear());//何度でも出せるようにする
            ToolTipService.SetInitialShowDelay(Tooltip, 0);
        }
        public virtual void ClearInfo()
        {
            cnvs.ReleaseMouseCapture();
            isDrag = false;
            isDragMoved = false;

            cnvs.Height = 0;
            cnvs.Width = 0;

            PopupClear();
            TooltipClear();
        }

        protected virtual void PopupClear()
        {
            Popup.Visibility = Visibility.Hidden;
            lastPopInfo = null;
        }
        protected virtual void PopUpWork(bool onClick = false)
        {
            if (IsPopEnabled == false || PopOnOver == false && PopOnClick == false) return;

            try
            {
                Point cursorPos = Mouse.GetPosition(scroll);
                if (PopOnOver == false && onClick == true && lastPopInfo != null ||
                    cursorPos.X < 0 || cursorPos.Y < 0 ||
                    scroll.ViewportWidth < cursorPos.X || scroll.ViewportHeight < cursorPos.Y)
                {
                    PopupClear();
                    return;
                }

                PanelItem popInfo = GetPopupItem(Mouse.GetPosition(cnvs), onClick);
                if (popInfo != lastPopInfo)
                {
                    lastPopInfo = popInfo;

                    if (popInfo == null || PopOnOver == false && onClick == false)
                    {
                        PopupClear();
                        return;
                    }

                    SetPopupItem(popInfo);
                    Popup.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex) 
            {
                PopupClear();
                MessageBox.Show(ex.ToString());
            }
        }
        // PopUpの初期化
        protected void SetPopupItem(PanelItem popInfo)
        {
            PopPanel.PopUpMode = true;
            PopPanel.SetBorderStyleFromSettings();

            UpdatePopupPosition(popInfo);

            PopPanel.Item = Activator.CreateInstance(popInfo.GetType(), popInfo.Data) as PanelItem;

            Popup.Width = Math.Max(popInfo.Width, PopWidth) + PopPanel.WidthMarginRight;
            if (popInfo.TopPos < scroll.ContentVerticalOffset)
            {
                Popup.MinHeight = Math.Max(0, popInfo.BottomPos - scroll.ContentVerticalOffset);
                PopPanel.Item.DrawHours = true;
            }
            else
            {
                Popup.MinHeight = Math.Max(0, Math.Min(scroll.ContentVerticalOffset + scroll.ViewportHeight - popInfo.TopPos - PopPanel.HeightMarginBottom, popInfo.Height));
            }

            PopPanel.Item.Width = Popup.Width - PopPanel.WidthMarginRight;
            PopPanel.Item.Height = Math.Max(PopPanel.GetMaxRenderHeight(9999), Popup.MinHeight);
            Popup.Height = PopPanel.Item.Height + PopPanel.HeightMarginBottom;
            SetPopupItemEx(popInfo);

            UpdatePopupReDraw();
        }
        protected virtual void SetPopupItemEx(PanelItem popInfo) { }
        // PopUp が画面内に収まるように調整する
        protected void UpdatePopupPosition(PanelItem popInfo)
        {
            // offsetHが正のとき右にはみ出している
            double offsetH = popInfo.LeftPos + Popup.ActualWidth - (scroll.ContentHorizontalOffset + scroll.ViewportWidth);
            // 右にはみ出した分だけ左にずらす
            double left = popInfo.LeftPos - Math.Max(0, offsetH);
            // 左にはみ出てる場合はscrollエリアの左端から表示する
            Canvas.SetLeft(Popup, Math.Max(left, scroll.ContentHorizontalOffset));

            // offsetVが正のとき下にはみ出している
            double offsetV = popInfo.TopPos + Popup.ActualHeight - (scroll.ContentVerticalOffset + scroll.ViewportHeight);
            // 下にはみ出した分だけ上にずらす
            double top = popInfo.TopPos - Math.Max(0, offsetV);
            // 上にはみ出てる場合はscrollエリアの上端から表示する
            Canvas.SetTop(Popup, Math.Max(top, scroll.ContentVerticalOffset));

            UpdatePopupReDraw();
        }
        protected virtual void UpdatePopupReDraw()
        {
            if (PopPanel.Item == null) return;
            PopPanel.Item.LeftPos = Canvas.GetLeft(Popup);
            PopPanel.Item.TopPos = Canvas.GetTop(Popup);
            PopPanel.InvalidateVisual();
        }

        // PopUp の ActualWidth と ActualHeight を取得するために SizeChanged イベントを捕捉する
        protected virtual void popupItem_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (lastPopInfo != null) UpdatePopupPosition(lastPopInfo);
        }

        protected virtual void TooltipClear()
        {
            cnvs.Children.Remove(Tooltip);
            Tooltip.ToolTip = null;
            lastToolInfo = null;
            toolTipTimer.Stop();
        }
        protected virtual void TooltipWork()
        {
            if (IsTooltipEnabled == false) return;

            try
            {
                PanelItem toolInfo = GetTooltipItem(Mouse.GetPosition(cnvs));
                if (toolInfo != lastToolInfo)
                {
                    TooltipClear();
                    if (toolInfo == null) return;

                    lastToolInfo = toolInfo;

                    //ToolTipService.SetBetweenShowDelay()がいまいち思い通り動かないので、タイマーを挟む
                    toolTipTimer.Interval = TimeSpan.FromMilliseconds(TooltipViweWait);
                    toolTipTimer.Start();
                }
            }
            catch (Exception ex) 
            {
                TooltipClear();
                MessageBox.Show(ex.ToString()); 
            }
        }
        void toolTipTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                toolTipTimer.Stop();
                cnvs.Children.Remove(Tooltip);
                SetTooltipItem(lastToolInfo);
                cnvs.Children.Add(Tooltip);
            }
            catch (Exception ex)
            {
                TooltipClear();
                MessageBox.Show(ex.ToString());
            }
        }
        //Tooltipの初期化
        protected void SetTooltipItem(PanelItem toolInfo)
        {
            Tooltip.Width = toolInfo.Width;
            Tooltip.Height = toolInfo.Height;
            Canvas.SetLeft(Tooltip, toolInfo.LeftPos);
            Canvas.SetTop(Tooltip, toolInfo.TopPos);

            Tooltip.ToolTip = null;
            SetTooltip(toolInfo);
            return;
        }
        protected virtual void SetTooltip(PanelItem toolInfo) { }

        /// <summary>マウスホイールイベント呼び出し</summary>
        protected virtual void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta != 0)
            {
                //負のとき下方向
                double delta = IsMouseScrollAuto ? e.Delta : ScrollSize * (e.Delta < 0 ? -1 : 1);
                scroll.ScrollToVerticalOffset(scroll.VerticalOffset - delta);
            }
            e.Handled = true;
        }

        protected virtual void scrollViewer_MouseEnter(object sender, MouseEventArgs e)
        {
            if (horizontalScrollMessageHook == null && (IsMouseHorizontalScrollAuto || HorizontalScrollSize != 0))
            {
                scrollViewerHwndSource = PresentationSource.FromVisual(scroll) as HwndSource;
                if (scrollViewerHwndSource != null)
                {
                    horizontalScrollMessageHook = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
                    {
                        const int WM_MOUSEHWHEEL = 0x020E;
                        if (msg == WM_MOUSEHWHEEL)
                        {
                            toolTipTimer.Stop();
                            TooltipClear();

                            double delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                            if (delta != 0)
                            {
                                //負のとき左方向
                                delta = IsMouseHorizontalScrollAuto ? delta : HorizontalScrollSize * (delta < 0 ? -1 : 1);
                                scroll.ScrollToHorizontalOffset(scroll.HorizontalOffset + delta);
                            }
                            handled = true;
                        }
                        return IntPtr.Zero;
                    };
                    scrollViewerHwndSource.AddHook(horizontalScrollMessageHook);
                }
            }
        }

        protected virtual void scrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (horizontalScrollMessageHook != null)
            {
                scrollViewerHwndSource.RemoveHook(horizontalScrollMessageHook);
                horizontalScrollMessageHook = null;
                scrollViewerHwndSource = null;
            }
        }

        protected virtual void scrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            scroll.ScrollToHorizontalOffset(ViewUtil.SnapsToDevicePixelsX(scroll.HorizontalOffset));
            scroll.ScrollToVerticalOffset(ViewUtil.SnapsToDevicePixelsY(scroll.VerticalOffset));
            if (ScrollChanged != null) ScrollChanged(this, e);
        }

        public void view_ScrollChanged(ScrollViewer main_scroll, ScrollViewer v_scroll, ScrollViewer h_scroll)
        {
            try
            {
                //時間軸の表示もスクロール
                v_scroll.ScrollToVerticalOffset(main_scroll.VerticalOffset);
                //サービス名表示もスクロール
                h_scroll.ScrollToHorizontalOffset(main_scroll.HorizontalOffset);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        protected virtual void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed && isDrag == true)
                {
                    isDragMoved = true;

                    Point CursorPos = Mouse.GetPosition(null);
                    double MoveX = lastDownMousePos.X - CursorPos.X;
                    double MoveY = lastDownMousePos.Y - CursorPos.Y;

                    double OffsetH = 0;
                    double OffsetV = 0;
                    MoveX *= DragScroll;
                    MoveY *= DragScroll;
                    OffsetH = lastDownHOffset + MoveX;
                    OffsetV = lastDownVOffset + MoveY;
                    if (OffsetH < 0) OffsetH = 0;
                    if (OffsetV < 0) OffsetV = 0;

                    scroll.ScrollToHorizontalOffset(OffsetH);
                    scroll.ScrollToVerticalOffset(OffsetV);
                }
                else
                {
                    PopUpWork();
                    TooltipWork();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        protected virtual void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                lastDownMousePos = Mouse.GetPosition(null);
                lastDownHOffset = scroll.HorizontalOffset;
                lastDownVOffset = scroll.VerticalOffset;
                cnvs.CaptureMouse();
                isDrag = true;
                isDragMoved = false;

                if (e.ClickCount == 1)
                {
                    if (MouseClick != null) MouseClick(sender, Mouse.GetPosition(cnvs));
                }
                if (e.ClickCount == 2)
                {
                    if (LeftDoubleClick != null) LeftDoubleClick(sender, Mouse.GetPosition(cnvs));
                }
                else if (PopOnClick == true)
                {
                    PopUpWork(true);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        protected virtual void canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                cnvs.ReleaseMouseCapture();
                isDrag = false;
                if (isDragMoved == false)
                {
                    if (IsSingleClickOpen == true)
                    {
                        if (LeftDoubleClick != null) LeftDoubleClick(sender, Mouse.GetPosition(cnvs));
                    }
                }
                isDragMoved = false;
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        protected virtual void canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            cnvs.ReleaseMouseCapture();
            isDrag = false;
            lastDownMousePos = Mouse.GetPosition(null);
            lastDownHOffset = scroll.HorizontalOffset;
            lastDownVOffset = scroll.VerticalOffset;
            if (e.ClickCount == 1)
            {
                Point cursorPos = Mouse.GetPosition(cnvs);
                if (RightClick != null) RightClick(sender, cursorPos);
                if (MouseClick != null) MouseClick(sender, cursorPos);
            }
        }
        protected virtual void canvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (IsPopEnabled == true)
            {
                //右クリック時はポップアップを維持
                if (e.RightButton != MouseButtonState.Pressed)
                {
                    PopupClear();
                }
            }
            if (IsTooltipEnabled == true)
            {
                TooltipClear();
            }
        }

        public virtual void ScrollToFindItem(PanelItem target_item, JumpItemStyle style = JumpItemStyle.None)
        {
            try
            {
                if (target_item == null) return;

                if ((style & JumpItemStyle.PanelNoScroll) == 0)
                {
                    scroll.ScrollToHorizontalOffset(target_item.LeftPos - 100);
                    scroll.ScrollToVerticalOffset(target_item.TopPos - 100);
                }

                style &= ~JumpItemStyle.PanelNoScroll;
                if (style == JumpItemStyle.None) return;

                //マーキング要求のあるとき
                if (style == JumpItemStyle.JumpTo)
                {
                    var rect = new Rectangle();

                    rect.Stroke = Brushes.Red;
                    rect.StrokeThickness = 5;
                    rect.Opacity = 1;
                    rect.Fill = Brushes.Transparent;
                    rect.Effect = new System.Windows.Media.Effects.DropShadowEffect() { BlurRadius = 10 };

                    rect.Width = target_item.Width + 20;
                    rect.Height = target_item.Height + 20;
                    rect.IsHitTestVisible = false;

                    Canvas.SetLeft(rect, target_item.LeftPos - 10);
                    Canvas.SetTop(rect, target_item.TopPos - 10);
                    Canvas.SetZIndex(rect, 20);

                    // 一定時間枠を表示する
                    var notifyTimer = new System.Windows.Threading.DispatcherTimer();
                    notifyTimer.Interval = TimeSpan.FromSeconds(0.1);
                    int Brinks = 2 * 3;
                    cnvs.Children.Add(rect);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    notifyTimer.Tick += (sender, e) =>
                    {
                        if (sw.ElapsedMilliseconds > Settings.Instance.DisplayNotifyJumpTime * 1000)
                        {
                            notifyTimer.Stop();
                            cnvs.Children.Remove(rect);
                        }
                        else if (--Brinks >= 0)
                        {
                            rect.Visibility = (Brinks % 2) == 0 ? Visibility.Visible : Visibility.Hidden;
                        }
                    };
                    notifyTimer.Start();
                }
                else if (style == JumpItemStyle.MoveTo &&
                    (Popup.Visibility != Visibility.Visible || target_item.IsPicked(Mouse.GetPosition(cnvs)) == false))
                {
                    var rect = new Rectangle();

                    rect.Stroke = target_item.BorderBrush;
                    rect.StrokeThickness = 3;
                    rect.Opacity = 2;
                    rect.Fill = Brushes.Transparent;

                    rect.Width = target_item.Width + 7;
                    rect.Height = target_item.Height + 7;
                    rect.IsHitTestVisible = false;

                    Canvas.SetLeft(rect, target_item.LeftPos - 3);
                    Canvas.SetTop(rect, target_item.TopPos - 3);
                    Canvas.SetZIndex(rect, 20);

                    // 一定時間枠を表示する
                    var notifyTimer = new System.Windows.Threading.DispatcherTimer();
                    notifyTimer.Interval = TimeSpan.FromSeconds(0.1);
                    cnvs.Children.Add(rect);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    notifyTimer.Tick += (sender, e) =>
                    {
                        if (sw.ElapsedMilliseconds > Settings.Instance.DisplayNotifyJumpTime * 1000 || rect.Opacity < 0.2)
                        {
                            notifyTimer.Stop();
                            cnvs.Children.Remove(rect);
                        }
                        else
                        {
                            rect.Opacity *= 0.8;
                        }
                    };
                    notifyTimer.Start();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

    }
}
