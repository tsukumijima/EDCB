﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace EpgTimer.EpgView
{
    /// <summary>
    /// WeekDayView.xaml の相互作用ロジック
    /// </summary>
    public partial class WeekDayView : UserControl, IEpgSettingAccess, IEpgViewDataSet
    {
        public WeekDayView()
        {
            InitializeComponent();
        }

        public void ClearInfo()
        {
            stackPanel_day.Children.Clear();
        }

        public int EpgSettingIndex { get; private set; }
        public void SetViewData(EpgViewData data)
        {
            EpgSettingIndex = data.EpgSettingIndex;
            Background = this.EpgBrushCache().WeekdayBorderColor;
        }

        public void SetDay(List<DateTime> dayList)
        {
            {
                stackPanel_day.Children.Clear();
                foreach (DateTime time in dayList)
                {
                    TextBlock item = ViewUtil.GetPanelTextBlock(time.ToString("M/d\r\n(ddd)"));
                    item.Tag = time;
                    item.Width = this.EpgStyle().ServiceWidth - 1;

                    Color backgroundColor;
                    if (time.DayOfWeek == DayOfWeek.Saturday)
                    {
                        item.Foreground = Brushes.DarkBlue;
                        backgroundColor = Colors.Lavender;
                    }
                    else if (time.DayOfWeek == DayOfWeek.Sunday)
                    {
                        item.Foreground = Brushes.DarkRed;
                        backgroundColor = Colors.MistyRose;
                    }
                    else
                    {
                        item.Foreground = Brushes.Black;
                        backgroundColor = Colors.White;
                    }
                    item.Padding = new Thickness(0, 0, 0, 2);
                    item.VerticalAlignment = VerticalAlignment.Center;
                    item.FontWeight = FontWeights.Bold;

                    var grid = new UniformGrid();
                    grid.Background = this.EpgStyle().EpgGradationHeader ? (Brush)ColorDef.GradientBrush(backgroundColor, 0.8, 1.2) : new SolidColorBrush(backgroundColor);
                    grid.Background.Freeze();
                    grid.Margin = new Thickness(0, 1, 1, 1);
                    grid.Tag = time;
                    grid.Children.Add(item);
                    stackPanel_day.Children.Add(grid);
                }
                rect_day.Width = this.EpgStyle().ServiceWidth - 1;
                SetTodayMark();
            }
        }
        public void SetTodayMark()
        {
            var date = CommonUtil.EdcbNow.Date;
            var grid = stackPanel_day.Children.OfType<UniformGrid>().FirstOrDefault(grd => (DateTime)grd.Tag == date);
            rect_day.Visibility = grid == null ? Visibility.Collapsed : Visibility.Visible;
            if (grid != null)
            {
                rect_day.Stroke = ((TextBlock)grid.Children[0]).Foreground;
                rect_day.Margin = new Thickness { Left = 1 + this.EpgStyle().ServiceWidth * stackPanel_day.Children.IndexOf(grid) };
            }
        }
    }
}
