using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    using BoxExchangeEdit;

    /// <summary>
    /// EpgDataViewSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class EpgDataViewSetting : UserControl
    {
        private CustomEpgTabInfo info = new CustomEpgTabInfo();
        private EpgSearchKeyInfo searchKey = new EpgSearchKeyInfo();
        private RadioBtnSelect viewModeRadioBtns;
        private Dictionary<ulong, ServiceViewItem> servieceList;

        public EpgDataViewSetting()
        {
            InitializeComponent();

            try
            {
                comboBox_timeH_week.ItemsSource = Enumerable.Range(0, 24);
                comboBox_timeH_week.SelectedIndex = 4;

                servieceList = ChSet5.ChList.Values.Select(info => new ServiceViewItem(info)).ToDictionary(item => item.Key, item => item);
                var selectedList = ChSet5.ChListSelected.Select(info => servieceList[info.Key]).ToList();
                listBox_serviceDttv.ItemsSource = selectedList.Where(info => info.ServiceInfo.IsDttv == true);
                listBox_serviceBS.ItemsSource = selectedList.Where(info => info.ServiceInfo.IsBS == true);
                listBox_serviceCS.ItemsSource = selectedList.Where(info => info.ServiceInfo.IsCS == true);
                listBox_serviceOther.ItemsSource = selectedList.Where(info => info.ServiceInfo.IsOther == true);
                listBox_serviceAll.ItemsSource = selectedList;

                if (listBox_serviceDttv.Items.Count == 0) tab_serviceDttv.Visibility = Visibility.Collapsed;
                if (listBox_serviceBS.Items.Count == 0) tab_serviceBS.Visibility = Visibility.Collapsed;
                if (listBox_serviceCS.Items.Count == 0) tab_serviceCS.Visibility = Visibility.Collapsed;
                if (listBox_serviceOther.Items.Count == 0) tab_serviceOther.Visibility = Visibility.Collapsed;
                
                listBox_jyanru.ItemsSource = CommonManager.ContentKindList;

                viewModeRadioBtns = new RadioBtnSelect(radioButton_rate, radioButton_week, radioButton_list);
                viewModeRadioBtns.Value = 0;

                listBox_Button_Set();
                listBox_serviceView_ContextMenu_Set();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public void SetTryMode(bool tryMode)
        {
            checkBox_isVisible.IsEnabled = !tryMode;
            label_tabName.IsEnabled = !tryMode;
            textBox_tabName.SetReadOnlyWithEffect(tryMode);
        }

        /// <summary>
        /// デフォルト表示の設定値
        /// </summary>
        /// <param name="setInfo"></param>
        public void SetSetting(CustomEpgTabInfo setInfo)
        {
            info = setInfo.DeepClone();
            searchKey = setInfo.SearchKey.DeepClone();

            textBox_tabName.Text = setInfo.TabName;
            checkBox_isVisible.IsChecked = setInfo.IsVisible;
            viewModeRadioBtns.Value = setInfo.ViewMode;

            checkBox_noTimeView_rate.IsChecked = setInfo.NeedTimeOnlyBasic;
            checkBox_noTimeView_week.IsChecked = setInfo.NeedTimeOnlyWeek;
            comboBox_timeH_week.SelectedIndex = setInfo.StartTimeWeek;
            checkBox_searchMode.IsChecked = setInfo.SearchMode;
            checkBox_searchServiceFromView.IsChecked = setInfo.SearchGenreNoSyncView;
            checkBox_filterEnded.IsChecked = (setInfo.FilterEnded == true);

            listBox_serviceView.Items.AddItems(setInfo.ViewServiceList
                .Where(id => servieceList.ContainsKey(id) == true).Select(id => servieceList[id]));
            listBox_jyanruView.Items.AddItems(setInfo.ViewContentList.Select(data => CommonManager.ContentKindInfoForDisplay(data)));
            checkBox_notContent.IsChecked = setInfo.ViewNotContentFlag;
        }

        /// <summary>
        /// 設定値の取得
        /// </summary>
        /// <param name="setInfo"></param>
        public CustomEpgTabInfo GetSetting()
        {
            info.TabName = textBox_tabName.IsEnabled == true ? textBox_tabName.Text : info.TabName;
            info.IsVisible = checkBox_isVisible.IsEnabled == true ? checkBox_isVisible.IsChecked == true : info.IsVisible;
            info.ViewMode = viewModeRadioBtns.Value;

            info.NeedTimeOnlyBasic = (checkBox_noTimeView_rate.IsChecked == true);
            info.NeedTimeOnlyWeek = (checkBox_noTimeView_week.IsChecked == true);
            info.StartTimeWeek = comboBox_timeH_week.SelectedIndex;
            info.SearchMode = (checkBox_searchMode.IsChecked == true);
            info.SearchGenreNoSyncView = (checkBox_searchServiceFromView.IsChecked == true);
            info.FilterEnded = (checkBox_filterEnded.IsChecked == true);
            info.SearchKey = searchKey.DeepClone();
            info.SearchKey.serviceList.Clear();//不要なので削除

            info.ViewServiceList = listBox_serviceView.Items.OfType<ServiceViewItem>().Select(item => item.Key).ToList();
            info.ViewContentList = listBox_jyanruView.Items.OfType<ContentKindInfo>().Select(item => item.Data).DeepClone();
            info.ViewNotContentFlag = checkBox_notContent.IsChecked == true;

            return info.DeepClone();
        }

        //サービス選択関係は他でも使用するので
        private BoxExchangeEditor bxs;
        
        private void listBox_Button_Set()
        {
            bxs = new BoxExchangeEditor(null, this.listBox_serviceView, true, true, true, true);

            //サービス選択関係はソースの ListView が複数あるので、全ての ListViewItem にイベントを追加する。
            foreach (TabItem tab in tab_ServiceList.Items)
            {
                var box = tab.Content as ListView;
                bxs.sourceBoxAllowCancelAction(box);
                bxs.sourceBoxAllowDragDrop(box);
                bxs.sourceBoxAllowKeyAction(box);
                bxs.sourceBoxAllowDoubleClick(box);
            }
            //ソースのリストボックスは複数あるので、リストボックスが選択されたときに SourceBox の登録を行う
            tab_ServiceList.SelectionChanged += (sender, e) =>
            {
                try { bxs.SourceBox = ((sender as TabControl).SelectedItem as TabItem).Content as ListView; }
                catch { bxs.SourceBox = null; }
            };
            button_service_addAll.Click += new RoutedEventHandler(bxs.button_AddAll_Click);
            button_service_add.Click += new RoutedEventHandler(bxs.button_Add_Click);
            button_service_ins.Click += new RoutedEventHandler(bxs.button_Insert_Click);
            button_service_del.Click += new RoutedEventHandler(bxs.button_Delete_Click);
            button_service_delAll.Click += new RoutedEventHandler(bxs.button_DeleteAll_Click);
            button_service_top.Click += new RoutedEventHandler(bxs.button_Top_Click);
            button_service_up.Click += new RoutedEventHandler(bxs.button_Up_Click);
            button_service_down.Click += new RoutedEventHandler(bxs.button_Down_Click);
            button_service_bottom.Click += new RoutedEventHandler(bxs.button_Bottom_Click);

            //ジャンル選択関係
            var bxj = new BoxExchangeEditor(this.listBox_jyanru, this.listBox_jyanruView, true, true, true, true);
            button_jyanru_addAll.Click += new RoutedEventHandler(bxj.button_AddAll_Click);
            button_jyanru_add.Click += new RoutedEventHandler(bxj.button_Add_Click);
            button_jyanru_ins.Click += new RoutedEventHandler(bxj.button_Insert_Click);
            button_jyanru_del.Click += new RoutedEventHandler(bxj.button_Delete_Click);
            button_jyanru_delAll.Click += new RoutedEventHandler(bxj.button_DeleteAll_Click);
        }

        List<Tuple<int, int>> sortList;
        private void listBox_serviceView_ContextMenu_Set()
        {
            // 右クリックメニューにSIDのソートを登録
            var cm = new ContextMenu();
            var menuItemAsc = new MenuItem();
            menuItemAsc.Header = "サブチャンネルの結合表示を解除";
            menuItemAsc.ToolTip = "同一TSIDのサービスの結合表示が解除されるようServiceIDを昇順に並び替えます";
            menuItemAsc.Tag = 0;
            cm.Items.Insert(0, menuItemAsc);
            var menuItemDesc = new MenuItem();
            menuItemDesc.Header = "サブチャンネルを番組表で結合表示";
            menuItemDesc.ToolTip = "同一TSIDのサービスをServiceIDが逆順になるよう並べると番組表で結合表示される機能を使い、\r\nサブチャンネルを含めて1サービスの幅で表示します";
            menuItemDesc.Tag = 1;
            cm.Items.Insert(0, menuItemDesc);
            foreach (MenuItem item in cm.Items)
            {
                item.Click += listBox_serviceView_SidSort;
                ToolTipService.SetShowOnDisabled(item, true);
                ToolTipService.SetShowDuration(item, 20000);
            }
            listBox_serviceView.ContextMenu = cm;
            listBox_serviceView.ContextMenuOpening += listBox_serviceView_ContextMenu_Opening;
            listBox_serviceView.ContextMenuClosing += (s, e) => sortList = null;
        }

        private void listBox_serviceView_ContextMenu_Opening(object sender, ContextMenuEventArgs e)
        {
            var grpDic = new Dictionary<int, Tuple<int, int>>();
            var grpDicAdd = new Action<int, int>((first, end) =>
            {
                for (int i = first; i <= end; i++) grpDic.Add(i, new Tuple<int, int>(first, end));
            });
            var grpDicRemove = new Action<int, int>((first, end) =>
            {
                for (int i = first; i <= end; i++) grpDic.Remove(i);
            });

            //並べ替え可能なグループを抽出
            int itemIndex = 0, firstTsidIndex = 0;
            for (; itemIndex < listBox_serviceView.Items.Count - 1; itemIndex++)
            {
                // 同一TSIDが連続する部分を選択中の中から探す(散らばっているTSIDはまとめない)
                var a = listBox_serviceView.Items[itemIndex] as ServiceViewItem;
                var b = listBox_serviceView.Items[itemIndex + 1] as ServiceViewItem;
                if (a.ServiceInfo.TSID == b.ServiceInfo.TSID) continue;

                // 見つかった場合 firstTsidIndex < itemIndex になる
                if (itemIndex != firstTsidIndex) grpDicAdd(firstTsidIndex, itemIndex);
                firstTsidIndex = itemIndex + 1;
            }
            if (itemIndex != firstTsidIndex) grpDicAdd(firstTsidIndex, itemIndex);

            // 対象があるときのみソート可能になるようにする
            sortList = new List<Tuple<int, int>>();
            foreach (var item in listBox_serviceView.SelectedItems)
            {
                Tuple<int, int> data;
                if (grpDic.TryGetValue(listBox_serviceView.Items.IndexOf(item), out data) == true)
                {
                    sortList.Add(data);
                    grpDicRemove(data.Item1, data.Item2);
                }
            }

            bool isSortable = sortList.Count != 0;
            ((MenuItem)listBox_serviceView.ContextMenu.Items[0]).IsEnabled = isSortable;
            ((MenuItem)listBox_serviceView.ContextMenu.Items[1]).IsEnabled = isSortable;

            if (isSortable == false) return;

            //全選択時以外は選択状態を変更する
            if (listBox_serviceView.Items.Count == listBox_serviceView.SelectedItems.Count) return;

            listBox_serviceView.UnselectAll();
            foreach (var data in sortList)
            {
                for (int i = data.Item1; i <= data.Item2; i++)
                {
                    listBox_serviceView.SelectedItems.Add(listBox_serviceView.Items[i]);
                }
            }
        }

        //残りの追加イベント
        /// <summary>映像のみ全追加</summary>
        private void button_service_addVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ListBox listBox = bxs.SourceBox;
                if (listBox == null) return;

                listBox.UnselectAll();
                listBox.SelectedItemsAdd(listBox.Items.OfType<ServiceViewItem>().Where(info => info.ServiceInfo.IsVideo == true));
                button_service_add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        private void listBox_serviceView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Display_ServiceView(listBox_serviceView, textBox_serviceView1);
        }

        private void listBox_service_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Display_ServiceView(bxs.SourceBox, textBox_serviceView2);
        }

        private void listBox_serviceView_SidSort(object sender, RoutedEventArgs e)
        {
            // 昇順・逆順用コンパレーター
            var comparerator = ((sender as MenuItem).Tag as int?) == 0 ?
                new Func<ushort, ushort, bool>((a, b) => a <= b) : new Func<ushort, ushort, bool>((a, b) => a >= b);

            // 実質、高々3つの並べ替えなので Bubble Sort で十分
            var sort = new Action<int, int>((start, end) =>
            {
                for (int i = start; i < end; i++)
                {
                    for (int j = i + 1; j <= end; j++)
                    {
                        if (comparerator((listBox_serviceView.Items[j] as ServiceViewItem).ServiceInfo.SID, (listBox_serviceView.Items[i] as ServiceViewItem).ServiceInfo.SID))
                        {
                            var tmp = listBox_serviceView.Items[i];
                            listBox_serviceView.Items[i] = listBox_serviceView.Items[j];
                            listBox_serviceView.Items[j] = tmp;
                        }
                    }
                }
            });

            //選択状態を保存
            var listKeeper = new ListViewSelectedKeeper(listBox_serviceView, true, item => (item as ServiceViewItem).Key);

            //ソート
            sortList.ForEach(data => sort(data.Item1,data.Item2));

            //選択状態を復元
            listKeeper.RestoreListViewSelected();
        }

        private void Display_ServiceView(ListBox srclistBox, TextBox targetBox)
        {
            try
            {
                if (srclistBox == null || targetBox == null) return;

                targetBox.Text = "";
                if (srclistBox.SelectedItem == null) return;

                var info = (ServiceViewItem)srclistBox.SelectedItems[srclistBox.SelectedItems.Count - 1];
                targetBox.Text = info.ConvertInfoText();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        private void button_searchKey_Click(object sender, RoutedEventArgs e)
        {
            CustomEpgTabInfo tabInfo = this.GetSetting();

            var dlg = new SetSearchPresetWindow(this);
            dlg.SetSettingMode("検索条件");
            dlg.DataView.SetSearchKey(tabInfo.GetSearchKeyReloadEpg());
            if (dlg.ShowDialog() == true)
            {
                searchKey = dlg.DataView.GetSearchKey();

                //サービスリストは表示順を保持する
                var oldList = listBox_serviceView.Items.OfType<object>().ToList();
                var newList = searchKey.serviceList.Where(sv => servieceList.ContainsKey((ulong)sv) == true).Select(sv => servieceList[(ulong)sv]).ToList();
                listBox_serviceView.UnselectAll();
                listBox_serviceView.Items.RemoveItems(oldList.Where(sv => newList.Contains(sv) == false));
                listBox_serviceView.Items.AddItems(newList.Where(sv => oldList.Contains(sv) == false));

                //ジャンルリストの同期はオプションによる
                if (tabInfo.SearchGenreNoSyncView == false)
                {
                    var items = searchKey.contentList.Select(data => CommonManager.ContentKindInfoForDisplay(data));
                    listBox_jyanruView.Items.Clear();
                    listBox_jyanruView.Items.AddItems(items);
                    checkBox_notContent.IsChecked = searchKey.notContetFlag != 0;
                }
            }
        }
    }
}
