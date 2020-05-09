using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace EpgTimer.EpgView
{
    /// <summary>
    /// DateView.xaml の相互作用ロジック
    /// </summary>
    public partial class DateView : UserControl
    {
        public event Action<DateTime, bool> TimeButtonClick = (time, isDayMove) => { };

        public DateView()
        {
            InitializeComponent();
        }

        public void ClearInfo()
        {
            uniformGrid_main.Children.Clear();
        }

        int span = 6;
        public void SetTime(List<DateTime> timeList, EpgViewPeriod period)
        {
            ClearInfo();
            if (timeList.Any() == false) return;

            span = 6;
            DateTime start = CommonUtil.Max(timeList[0], period.Start);
            DateTime end = CommonUtil.Min(timeList[timeList.Count - 1], period.End);

            for (DateTime itemTime = start.Date; itemTime == start.Date || itemTime < end; itemTime += TimeSpan.FromDays(1))
            {
                var day = new Button();
                day.Padding = new Thickness(1);
                day.Content = itemTime.ToString("M/d(ddd)");
                if (itemTime.DayOfWeek == DayOfWeek.Saturday) day.Foreground = Brushes.Blue;
                if (itemTime.DayOfWeek == DayOfWeek.Sunday) day.Foreground = Brushes.Red;
                day.Tag = itemTime;
                day.Height = 21;
                day.VerticalAlignment = VerticalAlignment.Top;
                day.Click += (sender, e) => TimeButtonClick((DateTime)((Button)sender).Tag, true);//itemTimeはC#4以下でNG

                var uGrid = new UniformGrid();
                uGrid.Margin = new Thickness { Top = day.Height };
                uGrid.Rows = 1;
                for (int i = 0; i < 24; i += span)
                {
                    DateTime time = itemTime.AddHours(i);
                    var hour = new Button();
                    hour.Padding = new Thickness();
                    hour.Content = new TextBlock { Text = i.ToString() };
                    hour.Tag = time;
                    hour.Height = 21;
                    hour.Click += (sender, e) => TimeButtonClick((DateTime)((Button)sender).Tag, false);
                    hour.IsEnabled = start.AddHours(-span) < time && time <= end;
                    uGrid.Children.Add(hour);
                }

                //日付のマーキング。
                var rect = new Rectangle();
                rect.Stroke = day.Foreground;
                rect.StrokeThickness = 2;
                rect.RadiusX = 1;
                rect.RadiusY = 1;
                rect.Tag = itemTime;

                //別のUniformGridに並べるとSetScrollTime()の後装飾でずれる場合があるようなので、Gridでまとめる
                var grid = new Grid();
                grid.Children.Add(day);
                grid.Children.Add(uGrid);
                grid.Children.Add(rect);
                uniformGrid_main.Children.Add(grid);
            }
            columnDefinition.MaxWidth = uniformGrid_main.Children.Count * 120;
            SetTodayMark();
        }

        public void SetTodayMark()
        {
            var date = CommonUtil.EdcbNow.Date;
            foreach (Rectangle rect in uniformGrid_main.Children.OfType<Grid>().Select(grd => grd.Children[2]))
            {
                rect.Visibility = (DateTime)rect.Tag == date ? Visibility.Visible : Visibility.Hidden;
            }
        }
        public void SetScrollTime(DateTime time)
        {
            var DayBtns = uniformGrid_main.Children.OfType<Grid>().Select(grd => (Button)grd.Children[0]).ToList();
            var TimeBtns = uniformGrid_main.Children.OfType<Grid>().SelectMany(grd => ((Panel)grd.Children[1]).Children.OfType<Button>()).ToList();
            if (TimeBtns.Any() == false) return;
            time = CommonUtil.Max((DateTime)TimeBtns[0].Tag, CommonUtil.Min((DateTime)TimeBtns.Last().Tag, time));
            time = time.Date.AddHours(time.Hour - time.Hour % span);
            foreach (Button btn in DayBtns)
            {
                btn.FontWeight = (DateTime)btn.Tag == time.Date ? FontWeights.Bold : FontWeights.Normal;
            }
            foreach (Button btn in TimeBtns)
            {
                (btn.Content as TextBlock).TextDecorations = (DateTime)btn.Tag == time ? TextDecorations.Underline : null;
            }
        }
    }
}
