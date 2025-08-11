using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    using BoxExchangeEdit;

    public class ListViewSelectedKeeper
    {
        //リスト番組表で全選択状態でチャンネル選択更新してしまったりしたときなどでも大丈夫なように、
        //一応選択数の上限を設定しておく。
        public uint MaxRestoreNum = 100;

        public ListBox lb = null;
        public List<ulong> oldItems = null;
        public bool allSelected = false;
        protected Func<object, ulong> getKey;

        public ListViewSelectedKeeper(ListBox listBox = null, bool DoStoringNow = false, Func<object, ulong> _key = null)
        {
            lb = listBox;
            getKey = _key ?? (info => (ulong)info.GetHashCode());
            if (DoStoringNow) StoreListViewSelected();
        }

        public void StoreListViewSelected(ListBox listBox = null)
        {
            listBox = listBox ?? lb;
            if (listBox != null && listBox.SelectedItem != null)
            {
                oldItems = listBox.SelectedItems.OfType<object>().Select(data => getKey(data)).ToList();
                allSelected = (oldItems.Count > 1 && oldItems.Count == listBox.Items.Count);
            }
        }

        public void RestoreListViewSelected(ListBox listBox = null)
        {
            try
            {
                listBox = listBox ?? lb;
                if (listBox != null && listBox.Items.Count != 0 && oldItems != null && oldItems.Count > 0)
                {
                    if (this.allSelected == true)
                    {
                        listBox.SelectAll();
                        return;
                    }

                    //このUnselectAll()は無いと正しく復元出来ない状況があり得る
                    listBox.UnselectAll();

                    //上限越えの場合は、選択を解除して終了。
                    if (oldItems.Count >= this.MaxRestoreNum) return;

                    var oldSet = new HashSet<ulong>(oldItems);
                    listBox.SelectedItemsAdd(listBox.Items.OfType<object>().Where(item => oldSet.Contains(getKey(item))));

                    //画面更新が入るので最後に実行する。SelectedItem==nullのときScrollIntoViewは何もしない。
                    listBox.ScrollIntoView(listBox.SelectedItem);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

    }

}
