using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace EpgTimer.Setting
{
    /// <summary>
    /// ColorSetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorSetWindow : Window
    {
        public ColorSetWindow(Color color, Visual owner = null)
        {
            InitializeComponent();

            List<ColorComboItem> colorReference = typeof(Brushes).GetProperties().Select(p => new ColorComboItem(p.Name, (Brush)p.GetValue(null, null))).ToList();
            comboBox_color.ItemsSource = colorReference;
            comboBox_color.SelectionChanged += (sender, e) => button_Set_Click(null, null);

            button_OK.Click += (sender, e) => DialogResult = true;
            button_cancel.Click += (sender, e) => DialogResult = false;
            slider_R.ValueChanged += (sender, e) => SetColor(GetColor());
            slider_G.ValueChanged += (sender, e) => SetColor(GetColor());
            slider_B.ValueChanged += (sender, e) => SetColor(GetColor());
            slider_A.ValueChanged += (sender, e) => SetColor(GetColor());
            textBox_R.TextChanged += (sender, e) => textBoxTextChanged(sender, slider_R);
            textBox_G.TextChanged += (sender, e) => textBoxTextChanged(sender, slider_G);
            textBox_B.TextChanged += (sender, e) => textBoxTextChanged(sender, slider_B);
            textBox_A.TextChanged += (sender, e) => textBoxTextChanged(sender, slider_A);

            this.Owner = CommonUtil.GetTopWindow(owner);
            SetColor(color);
        }

        private void textBoxTextChanged(object sender, Slider slider)
        {
            byte val;
            if (byte.TryParse((sender as TextBox).Text, out val) == true) slider.Value = val;
        }
        private void button_Set_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_color.SelectedIndex < 0) return;
            SetColor((Color)comboBox_color.SelectedValue);
        }

        bool selectionChanging = false;
        public void SetColor(Color argb)
        {
            if (selectionChanging == true) return;
            selectionChanging = true;
            try
            {
                textBox_R.Text = argb.R.ToString();
                textBox_G.Text = argb.G.ToString();
                textBox_B.Text = argb.B.ToString();
                textBox_A.Text = argb.A.ToString();

                rectangle_color.Fill = new SolidColorBrush(argb);
                SelectNearColor(comboBox_color, argb);
                label_Status.Text = ColorDef.ColorDiff((Color)comboBox_color.SelectedValue, argb) < 1 ? "現在の色" : "近い色";
            }
            finally { selectionChanging = false; }
        }
        public Color GetColor()
        {
            return Color.FromArgb((byte)slider_A.Value, (byte)slider_R.Value, (byte)slider_G.Value, (byte)slider_B.Value);
        }

        public static void SelectNearColor(ComboBox cmbo, Color c)
        {
            var items = cmbo.Items.OfType<ColorComboItem>().Where(item => item.Value is SolidColorBrush).ToList();
            if (items.Count == 0) return;
            cmbo.SelectedItem = items[ColorDef.SelectNearColor(items.Select(item => (item.Value as SolidColorBrush).Color), c)];
        }
    }
}
