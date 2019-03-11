using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// StatusView.xaml の相互作用ロジック
    /// </summary>
    public partial class ListSearchView : UserControl
    {
        public static readonly DependencyProperty boxProperty =
            DependencyProperty.Register("TargetListBox", typeof(ListBox), typeof(ListSearchView));
        public ListBox TargetListBox { get { return GetValue(boxProperty) as ListBox; } set { SetValue(boxProperty, value); } }

        public ListSearchView() { InitializeComponent(); }

        private void text_SearchKey_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Handled == false && Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter)
            {
                if (e.IsDown == true) button_Jump_Click(null, null);
                e.Handled = true;//KeyUpでもEnterは阻止
            };
        }
        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (TargetListBox == null || TargetListBox.SelectedItem == null) return;
            text_SearchKey.Text = TargetListBox.SelectedItem.ToString();
        }
        private void button_Jump_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(text_SearchKey.Text) == true) return;
            if (TargetListBox == null || TargetListBox.Items.Count == 0) return;

            int idx = (TargetListBox.SelectedIndex + 1) % TargetListBox.Items.Count;
            var list = TargetListBox.Items.OfType<object>().Skip(idx).Concat(TargetListBox.Items.OfType<object>().Take(idx));

            string key = CommonManager.AdjustSearchText(text_SearchKey.Text.Trim());
            object hit = list.FirstOrDefault(item => CommonManager.AdjustSearchText(item.ToString()).Contains(key));

            if (hit != null)
            {
                TargetListBox.UnselectAll();
                TargetListBox.SelectedItem = hit;
                TargetListBox.ScrollIntoView(TargetListBox.SelectedItem);
            }
        }
    }
}
