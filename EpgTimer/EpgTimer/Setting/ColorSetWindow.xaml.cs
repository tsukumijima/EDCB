using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer.Setting
{
    /// <summary>
    /// ColorSetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorSetWindow : Window
    {
        public static readonly DependencyProperty AProperty = DependencyProperty.Register("A", typeof(byte), typeof(ColorSetWindow), new PropertyMetadata(Colors.White.A, PropertyChanged));
        public static readonly DependencyProperty RProperty = DependencyProperty.Register("R", typeof(byte), typeof(ColorSetWindow), new PropertyMetadata(Colors.White.R, PropertyChanged));
        public static readonly DependencyProperty GProperty = DependencyProperty.Register("G", typeof(byte), typeof(ColorSetWindow), new PropertyMetadata(Colors.White.G, PropertyChanged));
        public static readonly DependencyProperty BProperty = DependencyProperty.Register("B", typeof(byte), typeof(ColorSetWindow), new PropertyMetadata(Colors.White.B, PropertyChanged));
        public byte A { get { return (byte)GetValue(AProperty); } set { SetValue(AProperty, value); } }
        public byte R { get { return (byte)GetValue(RProperty); } set { SetValue(RProperty, value); } }
        public byte G { get { return (byte)GetValue(GProperty); } set { SetValue(GProperty, value); } }
        public byte B { get { return (byte)GetValue(BProperty); } set { SetValue(BProperty, value); } }
        public Color GetColor() { return Color.FromArgb(A, R, G, B); }
        public void SetColor(Color c) { A = c.A; R = c.R; G = c.G; B = c.B; }

        public ColorSetWindow(Color color, Visual owner = null)
        {
            InitializeComponent();

            DataContext = this;
            comboBox_color.ItemsSource = typeof(Brushes).GetProperties().Select(p => new ColorComboItem(p.Name, (Brush)p.GetValue(null, null)));
            comboBox_color.SelectedIndex = 0;
            button_OK.Click += (sender, e) => DialogResult = true;
            button_cancel.Click += (sender, e) => DialogResult = false;

            this.Owner = CommonUtil.GetTopWindow(owner);
            SetColor(color);
        }

        private static void PropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var win = (ColorSetWindow)sender;
            Color c = win.GetColor();
            win.rectangle_color.Fill = new SolidColorBrush(c);
            win.comboBox_color.SelectionChanged -= win.SelectedColor_Changed;
            try { SelectNearColor(win.comboBox_color, c); }
            finally { win.comboBox_color.SelectionChanged += win.SelectedColor_Changed; }
            win.label_Status.Text = ColorDef.ColorDiff((Color)win.comboBox_color.SelectedValue, c) < 1 ? "現在の色" : "近い色";
        }
        private void SelectedColor_Changed(object sender, RoutedEventArgs e)
        {
            if (comboBox_color.SelectedValue != null) SetColor((Color)comboBox_color.SelectedValue);
        }

        public static void SelectNearColor(ComboBox cmbo, Color c)
        {
            var items = cmbo.Items.OfType<ColorComboItem>().Where(item => item.Value is SolidColorBrush).ToList();
            if (items.Count == 0) return;
            cmbo.SelectedItem = items[ColorDef.SelectNearColor(items.Select(item => (item.Value as SolidColorBrush).Color), c)];
        }
    }
}
