using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EpgTimer.EpgView
{
    /// <summary>
    /// TimeView.xaml の相互作用ロジック
    /// </summary>
    public partial class TimeView : UserControl, IEpgSettingAccess, IEpgViewDataSet
    {
        private List<DateTime> canvasTimeList = new List<DateTime>();
        
        public TimeView()
        {
            InitializeComponent();
            scrollViewer.PreviewMouseWheel += new MouseWheelEventHandler((sende, e) => e.Handled = true);
        }

        public int EpgSettingIndex { get; private set; }
        public void SetViewData(EpgViewData data)
        {
            EpgSettingIndex = data.EpgSettingIndex;
            Background = this.EpgBrushCache().TimeBorderColor;
        }

        public void ClearMarker()
        {
            canvas.Children.Clear();
        }

        public void ClearInfo()
        {
            stackPanel_time.Children.Clear();
            ClearMarker();
            canvasTimeList.Clear();
            canvas.Height = 0;
            spacer.Text = "14/04";
        }

        public void SetTime(IEnumerable<DateTime> sortedTimeList, bool weekMode, bool tunerMode = false)
        {
            {
                ClearInfo();
                bool? use28 = Settings.Instance.LaterTimeUse == true ? null : (bool?)false;
                double h3L = (12 + 3) * 3;
                double h6L = h3L * 2;

                foreach (DateTime time1 in sortedTimeList)
                {
                    var timeMod = new DateTime28(time1, use28);
                    DateTime time = timeMod.DateTimeMod;
                    string HourMod = timeMod.HourMod.ToString();

                    canvasTimeList.Add(time1);
                    var item = ViewUtil.GetPanelTextBlock();
                    stackPanel_time.Children.Add(item);
                    item.Margin = new Thickness(1, 0, 1, 1);

                    if (tunerMode == false)
                    {
                        item.Foreground = this.EpgBrushCache().TimeFontColor;
                        item.Background = this.EpgBrushCache().TimeColorList[time1.Hour / 6];
                        item.Height = 60 * this.EpgStyle().MinHeight - item.Margin.Top - item.Margin.Bottom;
                        if (weekMode == false)
                        {
                            item.Inlines.Add(new Run(time.ToString("M/d\r\n")));
                            if (item.Height >= h3L)
                            {
                                var color = time.DayOfWeek == DayOfWeek.Sunday ? Brushes.Red : time.DayOfWeek == DayOfWeek.Saturday ? Brushes.Blue : item.Foreground;
                                var weekday = new Run(time.ToString("ddd")) { Foreground = color, FontWeight = FontWeights.Bold };
                                item.Inlines.AddRange(new Run[] { new Run("("), weekday, new Run(")") });
                            }
                        }
                        if (item.Height >= h3L) item.Inlines.Add(new LineBreak());
                        if (item.Height >= h6L) item.Inlines.Add(new LineBreak());
                        item.Inlines.Add(new Run(HourMod) { FontSize = 13, FontWeight = FontWeights.Bold });
                    }
                    else
                    {
                        item.Foreground = time.DayOfWeek == DayOfWeek.Sunday ? Brushes.Red : time.DayOfWeek == DayOfWeek.Saturday ? Brushes.Blue : Settings.BrushCache.TunerTimeFontColor;
                        item.Background = Settings.BrushCache.TunerTimeBackColor;
                        item.Height = 60 * Settings.Instance.TunerMinHeight - item.Margin.Top - item.Margin.Bottom;
                        item.Text = time.ToString("M/d\r\n" + (item.Height >= h3L ? "(ddd)\r\n" : ""))
                                                            + (item.Height >= h6L ? "\r\n" : "") + HourMod;
                    }
                }

                canvas.Height = 60 * this.EpgStyle().MinHeight * stackPanel_time.Children.Count;
            }
        }
        public void AddMarker(IEnumerable<KeyValuePair<DateTime, TimeSpan>> timeRanges, Brush brush)
        {
            if (canvasTimeList.Count > 0 && timeRanges.Any())
            {
                spacer.Text = "014/04";
                var canvasHeightPerHour = 60 * this.EpgStyle().MinHeight;
                var yRanges = new List<Tuple<double, double>>();
                foreach (KeyValuePair<DateTime, TimeSpan> timeRange in timeRanges)
                {
                    // 時間の範囲をY軸の範囲に変換する
                    DateTime startTime = timeRange.Key;
                    int index = canvasTimeList.BinarySearch(new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, 0, 0));
                    double y1 = canvasHeightPerHour * (index < 0 ? ~index : index + (startTime - canvasTimeList[index]).TotalMinutes / 60);
                    DateTime endTime = startTime + timeRange.Value;
                    index = canvasTimeList.BinarySearch(new DateTime(endTime.Year, endTime.Month, endTime.Day, endTime.Hour, 0, 0));
                    double y2 = canvasHeightPerHour * (index < 0 ? ~index : index + (endTime - canvasTimeList[index]).TotalMinutes / 60);
                    if (y1 < y2)
                    {
                        yRanges.Add(new Tuple<double, double>(y1, y2));
                    }
                }
                yRanges.Sort();

                var blurEffect = new System.Windows.Media.Effects.DropShadowEffect();
                blurEffect.BlurRadius = 10;
                blurEffect.ShadowDepth = 2;
                blurEffect.Freeze();
                for (int i = 0; i < yRanges.Count(); i++)
                {
                    var item = new Line();
                    item.X1 = 5;
                    item.X2 = 5;
                    item.Y1 = yRanges[i].Item1;
                    // 重なっていればつなげる
                    double y2 = yRanges[i].Item2;
                    for (; i + 1 < yRanges.Count() && y2 >= yRanges[i + 1].Item1; i++)
                    {
                        y2 = Math.Max(y2, yRanges[i + 1].Item2);
                    }
                    item.Y2 = y2;
                    item.Stroke = brush;
                    item.StrokeThickness = 3;
                    item.Effect = blurEffect;
                    canvas.Children.Add(item);
                    Canvas.SetZIndex(item, 10);
                }
            }
        }
    }
}
