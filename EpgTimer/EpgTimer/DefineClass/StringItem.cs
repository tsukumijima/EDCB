using System;
using System.Collections.Generic;
using System.Linq;

namespace EpgTimer
{
    //同じ文字列がListBoxで同じアイテム扱いになるのを回避するためのラッピングクラス。
    //その上で重複管理用にはEqualityComparerを使う。
    public class StringItem
    {
        public StringItem() { }
        public StringItem(string s) { Value = s; }
        public StringItem(StringItem item) { Value = item == null ? null : item.Value; }
        public string Value { get; set; }
        public override string ToString() { return Value; }

        public static IEnumerable<StringItem> Items(string s) { return new List<StringItem> { new StringItem(s) }; }
        public static IEnumerable<StringItem> Items(IEnumerable<string> list) { return list.Select(s => new StringItem(s)); }
        public static Func<object, object> Cloner { get { return obj => new StringItem(obj as StringItem); } }
        public static readonly StringItemComparer Comparator = new StringItemComparer();

        //重複アイテムの管理用
        public class StringItemComparer : EqualityComparer<object>
        {
            public override bool Equals(object x, object y)
            {
                return x is StringItem && y is StringItem && (x as StringItem).Value == (y as StringItem).Value;
            }
            public override int GetHashCode(object obj)
            {
                return obj is StringItem == false ? 0 : (obj as StringItem).Value.GetHashCode();
            }
        }
    }
    static public class StringItemEx
    {
        public static IEnumerable<string> Values(this IEnumerable<StringItem> itemlist)
        {
            return itemlist.Select(s => s.Value);
        }
        public static List<string> ValueList(this IEnumerable<StringItem> itemlist)
        {
            return itemlist.Values().ToList();
        }
    }
}
