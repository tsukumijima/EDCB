using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EpgTimer
{
    public class BoolConverter : IValueConverter
    {
        protected virtual bool ToBool(object v) { return v is bool ? (bool)v : v is Visibility ? (Visibility)v == Visibility.Visible : (v ?? 0).ToString() != "0"; }
        public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            if (t == typeof(Visibility)) return ToBool(v) ? Visibility.Visible : p == null ? Visibility.Hidden : Visibility.Collapsed;
            return ToBool(v);
        }
        public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            if (t == typeof(Visibility)) v = (Visibility)v == Visibility.Visible;
            return t == typeof(bool) ? ToBool(v) : (object)(byte)(ToBool(v) ? 1 : 0);
        }
    }
    public class BoolInverter : BoolConverter { protected override bool ToBool(object v) { return !base.ToBool(v); } }

    public class RadioButtonTagConverter : IValueConverter
    {
        public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            var btn = p as RadioButton;
            return v == null || p == null ? false : btn.Tag as string == v.ToString();
        }
        public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            var btn = p as RadioButton;
            int ret;
            return (v as bool?) == true && int.TryParse(btn.Tag as string, out ret) ? (object)ret : null;
        }

        //設定用のメソッド
        private static RadioButtonTagConverter radioButtonCnv = new RadioButtonTagConverter();
        public static void SetBindingButton(RadioButton btn, string path)
        {
            var binding = new Binding(path) { Converter = radioButtonCnv, ConverterParameter = btn };
            btn.SetBinding(RadioButton.IsCheckedProperty, binding);
        }
        public static void SetBindingButtons(string path, Panel pnl)
        {
            foreach (var btn in pnl.Children.OfType<RadioButton>())
            {
                SetBindingButton(btn, path);
            }
        }
    }

    //ValidationRuleはパラメータ渡すのが面倒なので代用
    //パラメータは、カンマ区切りで、最大値、最小値
    public class ValueChecker : IValueConverter
    {
        public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
#if DEBUG
            ConvertBack(null, v.GetType(), p, c);//パラメータ指定のエラーチェック用
#endif
            return v;
        }
        public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            string[] param = (p as string).Split(',').Select(s => s.Trim().ToLower()).ToArray();
            double max = MyParse(param[0], t, true);
            double min = MyParse(param[1], t, false);
            double val;
            if (v == null || double.TryParse(v as string, out val) == false || val > max || val < min) return null;
            return v;
        }
        private double MyParse(string s, Type t, bool isMax)
        {
            if (s == "" || s == (isMax ? "max" : "min"))
            {
                return System.Convert.ToDouble(t.GetField(isMax ? "MaxValue" : "MinValue").GetValue(null));
            }
            return double.Parse(s);
        }
    }

    /// <summary>パラメータ(p)に指定の文字列のリスト(;区切)から、参照(v)位置の文字列を選択する。</summary>
    public class StringSelector : IValueConverter
    {
        public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            return ((string)p).Split(';')[(uint)v];
        }
        public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            return null;
        }
    }
}
