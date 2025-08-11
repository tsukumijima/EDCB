using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Reflection;
using System.Windows;

namespace EpgTimer
{
    public static class ColorDef
    {
        public static Color ColorFromName(string name)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(name);//#形式でも大丈夫
                //return (Color)typeof(Colors).GetProperty(name).GetValue(null, null);
            }
            catch
            {
                return Colors.White;
            }
        }
        //未使用
        //public static SolidColorBrush BrushFromName(string name) { return new SolidColorBrush(ColorFromName(name)); }

        public static Color FromUInt(uint value)
        {
            return Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value);
        }
        public static uint ToUInt(Color c)
        {
            return ((uint)c.A) << 24 | ((uint)c.R) << 16 | ((uint)c.G) << 8 | (uint)c.B;
        }

        public static LinearGradientBrush GradientBrush(Color color, double luminance = 0.94, double saturation = 1.2)
        {
            // 彩度を上げる
            int[] numbers = {color.R, color.G, color.B};
            double n1 = numbers.Max();
            double n2 = numbers.Min();
            double n3 = n1 / (n1 - n2);
            double r = (color.R - n1) * saturation + n1;
            double g = (color.G - n1) * saturation + n1;
            double b = (color.B - n1) * saturation + n1;
            r = Math.Max(r, 0);
            g = Math.Max(g, 0);
            b = Math.Max(b, 0);

            // 明るさを下げる
            double l1 = 0.298912 * color.R + 0.586611 * color.G + 0.114478 * color.B;
            double l2 = 0.298912 * r + 0.586611 * g + 0.114478 * b;
            double f = (l2 / l1) * luminance;
            r *= f;
            g *= f;
            b *= f;
            r = Math.Min(r, 255);
            g = Math.Min(g, 255);
            b = Math.Min(b, 255);

            var color2 = Color.FromArgb(color.A, (byte)r, (byte)g, (byte)b);
            
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0.5);
            brush.EndPoint = new Point(0, 1);
            brush.GradientStops.Add(new GradientStop(color, 0.0));
            brush.GradientStops.Add(new GradientStop(color2, 1.0));
            brush.Freeze();
            return brush;
        }

        //単純なRGB差。本当はlabとかいろいろあるけど今はこれで構わない
        public static double ColorDiff(Color c1, Color c2)
        {
            return Math.Abs(c1.A - c2.A) / 256.0 + Math.Abs(c1.R - c2.R) + Math.Abs(c1.G - c2.G) + Math.Abs(c1.B - c2.B);
        }
        public static int SelectNearColor(IEnumerable<Color> list, Color c)
        {
            var diffs = list.Select(c1 => ColorDiff(c1, c)).ToList();
            return diffs.IndexOf(diffs.Min());
        }
    }
}