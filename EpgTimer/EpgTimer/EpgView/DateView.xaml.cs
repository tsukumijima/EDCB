using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
            uniformGrid_day.Children.Clear();
            uniformGrid_time.Children.Clear();
        }

        public void SetTime(List<DateTime> timeList)
        {
            ClearInfo();
            if (timeList.Any() == false) return;

            DateTime itemTime = timeList.First().AddHours(23).Date;
            while (itemTime <= timeList.Last())
            {
                var day = new Button();
                day.Padding = new Thickness(1);
                day.Content = itemTime.ToString("M\\/d(ddd)");
                if (itemTime.DayOfWeek == DayOfWeek.Saturday) day.Foreground = Brushes.Blue;
                if (itemTime.DayOfWeek == DayOfWeek.Sunday) day.Foreground = Brushes.Red;
                day.Tag = itemTime;
                day.Height = 21;
                day.Click += (sender, e) => TimeButtonClick((DateTime)((Button)sender).Tag, true);//itemTimeはC#4以下でNG
                uniformGrid_day.Children.Add(day);

                for (int i = 0; i <= 18; i += 6)
                {
                    var hour = new Button();
                    hour.Padding = new Thickness(1);
                    hour.Content = new TextBlock { Text = i.ToString() };
                    hour.Tag = itemTime.AddHours(i);
                    hour.Height = 21;
                    hour.Click += (sender, e) => TimeButtonClick((DateTime)((Button)sender).Tag, false);
                    uniformGrid_time.Children.Add(hour);
                }

                itemTime += TimeSpan.FromDays(1);
            }
            //columnDefinition.MinWidth = uniformGrid_time.Children.Count * 10;//scrollあるので効かない
            columnDefinition.MaxWidth = uniformGrid_time.Children.Count * 30;
        }

        public void SetNow(DateTime time)
        {
            var DayBtns = uniformGrid_day.Children.OfType<Button>();
            var TimeBtns = uniformGrid_time.Children.OfType<Button>();
            foreach (Button btn in DayBtns)
            {
                btn.FontWeight = (DateTime)(btn.Tag) == time.Date ? FontWeights.Bold : FontWeights.Normal ;
            }
            foreach (Button btn in TimeBtns)
            {
                (btn.Content as TextBlock).TextDecorations = (DateTime)(btn.Tag) <= time && time < ((DateTime)(btn.Tag)).AddHours(6) ? TextDecorations.Underline : null;
            }
            var time_first = TimeBtns.FirstOrDefault();
            if (time_first != null && time < (DateTime)time_first.Tag)
            {
                DayBtns.First().FontWeight = FontWeights.Bold;
                (time_first.Content as TextBlock).TextDecorations = TextDecorations.Underline;
            }
        }
    }
}
