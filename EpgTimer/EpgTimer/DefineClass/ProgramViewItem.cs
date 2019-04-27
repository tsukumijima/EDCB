using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EpgTimer
{
    public class PanelItem : IEpgSettingAccess
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double LeftPos { get; set; }
        public double TopPos { get; set; }

        public bool IsPicked(Point cursorPos)
        {
            return LeftPos <= cursorPos.X && cursorPos.X < LeftPos + Width &&
                    TopPos <= cursorPos.Y && cursorPos.Y < TopPos + Height;
        }

        public PanelItem(object info) { Data = info; }
        public object Data { get; protected set; }
        public bool TitleDrawErr { get; set; }
        public bool Filtered { get; set; }
        public int EpgSettingIndex { get; set; }
        public int ViewMode { get; set; }
        public virtual Brush BackColor { get { return null; } }
        public virtual Brush BorderBrush { get { return null; } }
    }

    public class PanelItem<T> : PanelItem
    {
        public new T Data { get { return (T)base.Data; } }
        public PanelItem(T info) : base(info) { }
    }

    public static class PanelItemEx
    {
        public static List<T> GetHitDataList<T>(this IEnumerable<PanelItem<T>> list, Point cursorPos)
        {
            return list.Where(info => info != null && info.IsPicked(cursorPos)).Select(info => info.Data).ToList();
        }
        public static List<T> GetDataList<T>(this IEnumerable<PanelItem<T>> list)
        {
            return list.Where(info => info != null).Select(info => info.Data).ToList();
        }
        public static IEnumerable<T> GetNearDataList<T>(this IEnumerable<T> list, Point cursorPos) where T : PanelItem
        {
            return list.Where(info => info != null).OrderBy(info => Math.Abs(info.LeftPos + info.Width / 2 - cursorPos.X) + Math.Abs(info.TopPos + info.Height / 2 - cursorPos.Y));
        }
    }

    public class ProgramViewItem : PanelItem<EpgEventInfo>
    {
        public ProgramViewItem(EpgEventInfo info) : base(info) { }
        public bool DrawHours { get; set; }
        public override Brush BackColor { get { return ViewUtil.EpgDataContentBrush(Data, EpgSettingIndex, Filtered); } }
        public override Brush BorderBrush { get { return this.EpgBrushCache().BorderColor; } }
    }
}
