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
        public event RoutedEventHandler TimeButtonClick = null;

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
            try
            {
                ClearInfo();
                if (timeList.Any() != true) return;

                DateTime itemTime = timeList.First().Date;
                while (itemTime <= timeList.Last())
                {
                    var day = new Button();
                    day.Padding = new Thickness(1);
                    day.Content = itemTime.ToString("M/d(ddd)");
                    if (itemTime.DayOfWeek == DayOfWeek.Saturday)
                    {
                        day.Foreground = Brushes.Blue;
                    }
                    else if (itemTime.DayOfWeek == DayOfWeek.Sunday)
                    {
                        day.Foreground = Brushes.Red;
                    }
                    day.DataContext = itemTime;
                    day.Click += new RoutedEventHandler(button_time_Click);
                    uniformGrid_day.Children.Add(day);

                    for (int i = 6; i <= 18; i += 6)
                    {
                        var hour = new Button();
                        hour.Padding = new Thickness(1);
                        hour.Content = i.ToString();
                        hour.DataContext = itemTime.AddHours(i);
                        hour.Click += new RoutedEventHandler(button_time_Click);
                        uniformGrid_time.Children.Add(hour);
                    }

                    itemTime += TimeSpan.FromDays(1);
                }
            
                columnDefinition.MinWidth = uniformGrid_time.Children.Count * 15;
                columnDefinition.MaxWidth = uniformGrid_time.Children.Count * 40;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        void button_time_Click(object sender, RoutedEventArgs e)
        {
            if (TimeButtonClick != null)
            {
                TimeButtonClick(sender, e);
            }
        }
    }
}
