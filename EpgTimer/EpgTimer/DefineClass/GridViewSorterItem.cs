using System;
using System.Reflection;

namespace EpgTimer
{
    public class GridViewSorterItem : IGridViewSorterItem
    {
        public virtual ulong KeyID { get { return (ulong)(this.GetHashCode()); } }
        public virtual ulong DisplayID { get { return KeyID; } }
        public string GetValuePropertyName(string key)
        {
            //ソート用の代替プロパティには"Value"を後ろに付けることにする。
            //呼び出し回数多くないのでとりあえずこれで。
            if (key == "ID") return "DisplayID";//"DisplayID"を保持しつつ、保存名("ID")はフォーク元に合わせておく。
            PropertyInfo pInfo = this.GetType().GetProperty(key + "Value");
            return pInfo == null ? key : pInfo.Name;
        }
    }
}
