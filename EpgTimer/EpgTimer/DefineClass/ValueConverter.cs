using System;
using System.Windows;
using System.Windows.Data;

namespace EpgTimer
{
    public class BoolConverter : IValueConverter
    {
        protected virtual bool ToBool(object v) { return v is bool ? (bool)v : (v ?? "").ToString().Trim() != "0"; }
        public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            bool val = ToBool(v);
            if (t == typeof(Visibility)) return val ? Visibility.Visible : p == null ? Visibility.Hidden : Visibility.Collapsed;
            return val;
        }
        public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
        {
            if (t == typeof(Visibility)) v = (Visibility)v == Visibility.Visible;
            return ToBool(v);
        }
    }
    public class BoolInverter : BoolConverter { protected override bool ToBool(object v) { return !base.ToBool(v); } }
}
