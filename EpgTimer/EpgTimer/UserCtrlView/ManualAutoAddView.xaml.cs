﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using CtrlCmdCLI;
using CtrlCmdCLI.Def;

namespace EpgTimer
{
    /// <summary>
    /// ManualAutoAddView.xaml の相互作用ロジック
    /// </summary>
    public partial class ManualAutoAddView : UserControl
    {
        private CtrlCmdUtil cmd = CommonManager.Instance.CtrlCmd;
        private MenuUtil mutil = CommonManager.Instance.MUtil;
        private List<ManualAutoAddDataItem> resultList = new List<ManualAutoAddDataItem>();
        private bool ReloadInfo = true;

        private Dictionary<String, GridViewColumn> columnList = new Dictionary<String, GridViewColumn>();

        public ManualAutoAddView()
        {
            InitializeComponent();
            try
            {
                if (Settings.Instance.NoStyle == 1)
                {
                    button_add.Style = null;
                    button_del.Style = null;
                    button_del2.Style = null;
                    button_change.Style = null;
                }

                foreach (GridViewColumn info in gridView_key.Columns)
                {
                    GridViewColumnHeader header = info.Header as GridViewColumnHeader;
                    columnList.Add((string)header.Tag, info);
                }
                gridView_key.Columns.Clear();

                foreach (ListColumnInfo info in Settings.Instance.AutoAddManualColumn)
                {
                    columnList[info.Tag].Width = info.Width;
                    gridView_key.Columns.Add(columnList[info.Tag]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        public void SaveSize()
        {
            try
            {
                Settings.Instance.AutoAddManualColumn.Clear();
                foreach (GridViewColumn info in gridView_key.Columns)
                {
                    GridViewColumnHeader header = info.Header as GridViewColumnHeader;

                    Settings.Instance.AutoAddManualColumn.Add(new ListColumnInfo((String)header.Tag, info.Width));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// リストの更新通知
        /// </summary>
        public void UpdateInfo()
        {
            if (this.IsVisible == true)
            {
                ReloadInfoData();
                ReloadInfo = false;
            }
            else
            {
                ReloadInfo = true;
            }
        }
        
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReloadInfo == true && this.IsVisible == true)
            {
                ReloadInfoData();
                ReloadInfo = false;
            }
        }

        private bool ReloadInfoData()
        {
            try
            {
                listView_key.DataContext = null;
                resultList.Clear();

                if (CommonManager.Instance.NWMode == true)
                {
                    if (CommonManager.Instance.NW.IsConnected == false)
                    {
                        return false;
                    }
                }
                ErrCode err = CommonManager.Instance.DB.ReloadManualAutoAddInfo();
                if (CommonManager.CmdErrMsgTypical(err, "情報の取得", this) == false)
                {
                    return false;
                }

                foreach (ManualAutoAddData info in CommonManager.Instance.DB.ManualAutoAddList.Values)
                {
                    ManualAutoAddDataItem item = new ManualAutoAddDataItem(info);
                    resultList.Add(item);
                }

                listView_key.DataContext = resultList;
            }
            catch (Exception ex)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                }), null);
                return false;
            }
            return true;
        }

        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            AddManualAutoAddWindow dlg = new AddManualAutoAddWindow();
            dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
            dlg.ShowDialog();
        }

        private void button_del_Click(object sender, RoutedEventArgs e)
        {
            if (listView_key.SelectedItems.Count == 0) return;

            var dataIDList = GetSelectedItemsList().Select(info => info.ManualAutoAddInfo.dataID).ToList();
            cmd.SendDelManualAdd(dataIDList);
        }

        private void button_del2_Click(object sender, RoutedEventArgs e)
        {
            if (listView_key.SelectedItems.Count == 0) return;

            string text1 = "予約項目ごと削除してよろしいですか?　[削除アイテム数: " + listView_key.SelectedItems.Count + "]\r\n"
                            + "(サービス、時間の一致した予約が削除されます)\r\n\r\n";
            GetSelectedItemsList().ForEach(info => text1 += " ・ " + info.Title + "\r\n");

            string caption1 = "[予約ごと削除]の確認";
            if (MessageBox.Show(text1, caption1, MessageBoxButton.OKCancel,
                MessageBoxImage.Exclamation, MessageBoxResult.OK) != MessageBoxResult.OK)
            {
                return;
            }

            //EpgTimerSrvでの自動予約登録の実行タイミングに左右されず確実に予約を削除するため、
            //先に自動予約登録項目を削除する。

            //自動予約登録項目のリストを保持
            List<ManualAutoAddDataItem> autoaddlist = GetSelectedItemsList();

            button_del_Click(sender, e);

            try
            {
                //配下の予約の削除
                var dellist = new List<ReserveData>();
                autoaddlist.ForEach(info => dellist.AddRange(info.ManualAutoAddInfo.GetReserveList()));
                dellist = dellist.Distinct().ToList();

                mutil.ReserveDelete(dellist);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }

        }

        private List<ManualAutoAddDataItem> GetSelectedItemsList()
        {
            return listView_key.SelectedItems.Cast<ManualAutoAddDataItem>().ToList();
        }

        private void button_change_Click(object sender, RoutedEventArgs e)
        {
            if (listView_key.SelectedItem != null)
            {
                ManualAutoAddDataItem info = SelectSingleItem();
                AddManualAutoAddWindow dlg = new AddManualAutoAddWindow();
                dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
                dlg.SetChangeMode(true);
                dlg.SetDefaultSetting(info.ManualAutoAddInfo);
                dlg.ShowDialog();
            }
        }

        private ManualAutoAddDataItem SelectSingleItem()
        {
            return mutil.SelectSingleItem<ManualAutoAddDataItem>(listView_key);
        }

        private void listView_key_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.button_change.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ReloadInfo == true && this.IsVisible == true)
            {
                if (ReloadInfoData() == true)
                {
                    ReloadInfo = false;
                }
            }
        }

        private void ContextMenu_Header_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            try
            {
                foreach (MenuItem item in listView_key.ContextMenu.Items)
                {
                    item.IsChecked = false;
                    foreach (ListColumnInfo info in Settings.Instance.AutoAddManualColumn)
                    {
                        if (info.Tag.CompareTo(item.Name) == 0)
                        {
                            item.IsChecked = true;
                            break;
                        }
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void headerSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MenuItem menuItem = sender as MenuItem;
                if (menuItem.IsChecked == true)
                {

                    Settings.Instance.AutoAddManualColumn.Add(new ListColumnInfo(menuItem.Name, Double.NaN));
                    gridView_key.Columns.Add(columnList[menuItem.Name]);
                }
                else
                {
                    foreach (ListColumnInfo info in Settings.Instance.AutoAddManualColumn)
                    {
                        if (info.Tag.CompareTo(menuItem.Name) == 0)
                        {
                            Settings.Instance.AutoAddManualColumn.Remove(info);
                            gridView_key.Columns.Remove(columnList[menuItem.Name]);
                            break;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }
    }
}
