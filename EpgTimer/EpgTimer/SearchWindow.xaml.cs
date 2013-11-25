﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections;

using CtrlCmdCLI;
using CtrlCmdCLI.Def;

namespace EpgTimer
{
    /// <summary>
    /// SearchWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchWindow : Window
    {
        private List<SearchItem> resultList = new List<SearchItem>();
        private CtrlCmdUtil cmd = CommonManager.Instance.CtrlCmd;

        //string _lastHeaderClicked = null;
        //ListSortDirection _lastDirection = ListSortDirection.Ascending;
        //string _lastHeaderClicked2 = null;
        //ListSortDirection _lastDirection2 = ListSortDirection.Ascending;

        private UInt32 autoAddID = 0;

        PopupWindow _popupWindow;

        public SearchWindow()
        {
            InitializeComponent();

            try
            {
                if (Settings.Instance.NoStyle == 0)
                {
                    ResourceDictionary rd = new ResourceDictionary();
                    rd.MergedDictionaries.Add(
                        Application.LoadComponent(new Uri("/PresentationFramework.Aero, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35;component/themes/aero.normalcolor.xaml", UriKind.Relative)) as ResourceDictionary
                        //Application.LoadComponent(new Uri("/PresentationFramework.Classic, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, ProcessorArchitecture=MSIL;component/themes/Classic.xaml", UriKind.Relative)) as ResourceDictionary
                        );
                    this.Resources = rd;
                }
                else
                {
                    button_search.Style = null;
                    button_add_reserve.Style = null;
                    button_add_epgAutoAdd.Style = null;
                    button_chg_epgAutoAdd.Style = null;
                }

                //ウインドウ位置の復元
                if (Settings.Instance.SearchWndTop != 0)
                {
                    this.Top = Settings.Instance.SearchWndTop;
                }
                if (Settings.Instance.SearchWndLeft != 0)
                {
                    this.Left = Settings.Instance.SearchWndLeft;
                }
                if (Settings.Instance.SearchWndWidth != 0)
                {
                    this.Width = Settings.Instance.SearchWndWidth;
                }
                if (Settings.Instance.SearchWndHeight != 0)
                {
                    this.Height = Settings.Instance.SearchWndHeight;
                }

                EpgSearchKeyInfo defKey = new EpgSearchKeyInfo();
                Settings.GetDefSearchSetting(ref defKey);

                searchKeyView.SetSearchKey(defKey);
            }
            catch
            {
            }
        }

        public void GetSearchKey(ref EpgSearchKeyInfo key)
        {
            searchKeyView.GetSearchKey(ref key);
        }

        public void SetSearchDefKey(EpgSearchKeyInfo key)
        {
            searchKeyView.SetSearchKey(key);
        }

        public void SetRecInfoDef(RecSettingData set)
        {
            recSettingView.SetDefSetting(set);
        }

        public void SetViewMode(UInt16 mode)
        {
            if (mode == 1)
            {
                button_add_reserve.Visibility = System.Windows.Visibility.Hidden;
                button_add_epgAutoAdd.Visibility = System.Windows.Visibility.Visible;
                button_chg_epgAutoAdd.Visibility = System.Windows.Visibility.Hidden;

                Title = "EPG予約条件";
            }
            else if (mode == 2)
            {
                button_add_reserve.Visibility = System.Windows.Visibility.Hidden;
                button_add_epgAutoAdd.Visibility = System.Windows.Visibility.Visible;
                button_chg_epgAutoAdd.Visibility = System.Windows.Visibility.Visible;

                Title = "EPG予約条件";
            }
            else
            {
                button_add_reserve.Visibility = System.Windows.Visibility.Visible;
                button_add_epgAutoAdd.Visibility = System.Windows.Visibility.Visible;
                button_chg_epgAutoAdd.Visibility = System.Windows.Visibility.Hidden;

                Title = "検索";
            }
        }

        public void SetChgAutoAddID(UInt32 id)
        {
            autoAddID = id;
        }

        private void tabItem_searchKey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchPg();
            }
        }

        private void button_search_Click(object sender, RoutedEventArgs e)
        {
            SearchPg();
        }

        private void SearchPg()
        {
            try
            {
                ICollectionView dataView = CollectionViewSource.GetDefaultView(listView_result.DataContext);
                if (dataView != null)
                {
                    dataView.SortDescriptions.Clear();
                    dataView.Refresh();
                }
                listView_result.DataContext = null;

                resultList.Clear();


                EpgSearchKeyInfo key = new EpgSearchKeyInfo();
                searchKeyView.GetSearchKey(ref key);
                List<EpgSearchKeyInfo> keyList = new List<EpgSearchKeyInfo>();

                keyList.Add(key);
                List<EpgEventInfo> list = new List<EpgEventInfo>();

                cmd.SendSearchPg(keyList, ref list);
                foreach (EpgEventInfo info in list)
                {
                    SearchItem item = new SearchItem();
                    item.EventInfo = info;

                    if (item.EventInfo.start_time.AddSeconds(item.EventInfo.durationSec) > DateTime.Now)
                    {
                        foreach (ReserveData info2 in CommonManager.Instance.DB.ReserveList.Values)
                        {
                            if (info.original_network_id == info2.OriginalNetworkID &&
                                info.transport_stream_id == info2.TransportStreamID &&
                                info.service_id == info2.ServiceID &&
                                info.event_id == info2.EventID)
                            {
                                item.ReserveInfo = info2;
                                break;
                            }
                        }
                        UInt64 serviceKey = CommonManager.Create64Key(info.original_network_id, info.transport_stream_id, info.service_id);
                        if (ChSet5.Instance.ChList.ContainsKey(serviceKey) == true)
                        {
                            item.ServiceName = ChSet5.Instance.ChList[serviceKey].ServiceName;
                        }
                        if (Settings.Instance.FixSearchResult)
                        {
                            item.EventInfo.ShortInfo.text_char = "省略";
                        }

                        resultList.Add(item);
                    }
                }

                listView_result.DataContext = resultList;
                //if (_lastHeaderClicked != null) {
                //    Sort(_lastHeaderClicked, _lastDirection);
                //} else {
                //    string header = ((Binding)gridView_result.Columns[1].DisplayMemberBinding).Path.Path;
                //    Sort(header, _lastDirection);
                //    _lastHeaderClicked = header;
                //}
                if (this.gridViewSorter.isExistSortParams)
                {
                    this.gridViewSorter.SortByMultiHeader(this.resultList);
                }
                else
                {
                    this.gridViewSorter.resetSortParams();
                    this.gridViewSorter.SortByMultiHeader(
                        this.resultList,
                        gridView_result.Columns[1].Header as GridViewColumnHeader);
                }

                searchKeyView.SaveSearchLog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void button_add_reserve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (listView_result.SelectedItem != null)
                {
                    SearchItem item = listView_result.SelectedItem as SearchItem;
                    EpgEventInfo eventInfo = item.EventInfo;

                    if (eventInfo.StartTimeFlag == 0)
                    {
                        MessageBox.Show("開始時間未定のため予約できません");
                        return;
                    }

                    ReserveData reserveInfo = new ReserveData();
                    if (eventInfo.ShortInfo != null)
                    {
                        reserveInfo.Title = eventInfo.ShortInfo.event_name;
                    }

                    reserveInfo.StartTime = eventInfo.start_time;
                    reserveInfo.StartTimeEpg = eventInfo.start_time;

                    if (eventInfo.DurationFlag == 0)
                    {
                        reserveInfo.DurationSecond = 10 * 60;
                    }
                    else
                    {
                        reserveInfo.DurationSecond = eventInfo.durationSec;
                    }

                    UInt64 key = CommonManager.Create64Key(eventInfo.original_network_id, eventInfo.transport_stream_id, eventInfo.service_id);
                    if (ChSet5.Instance.ChList.ContainsKey(key) == true)
                    {
                        reserveInfo.StationName = ChSet5.Instance.ChList[key].ServiceName;
                    }
                    reserveInfo.OriginalNetworkID = eventInfo.original_network_id;
                    reserveInfo.TransportStreamID = eventInfo.transport_stream_id;
                    reserveInfo.ServiceID = eventInfo.service_id;
                    reserveInfo.EventID = eventInfo.event_id;

                    RecSettingData setInfo = new RecSettingData();
                    recSettingView.GetRecSetting(ref setInfo);
                    reserveInfo.RecSetting = setInfo;

                    List<ReserveData> list = new List<ReserveData>();
                    list.Add(reserveInfo);
                    ErrCode err = (ErrCode)cmd.SendAddReserve(list);
                    if (err == ErrCode.CMD_ERR_CONNECT)
                    {
                        MessageBox.Show("サーバー または EpgTimerSrv に接続できませんでした。");
                    }
                    if (err == ErrCode.CMD_ERR_TIMEOUT)
                    {
                        MessageBox.Show("EpgTimerSrvとの接続にタイムアウトしました。");
                    }
                    if (err != ErrCode.CMD_SUCCESS)
                    {
                        MessageBox.Show("予約登録でエラーが発生しました。終了時間がすでに過ぎている可能性があります。");
                    }

                    CommonManager.Instance.DB.SetUpdateNotify((UInt32)UpdateNotifyItem.ReserveInfo);
                    CommonManager.Instance.DB.ReloadReserveInfo();
                    SearchPg();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void button_add_epgAutoAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EpgAutoAddData addItem = new EpgAutoAddData();
                EpgSearchKeyInfo searchKey = new EpgSearchKeyInfo();
                searchKeyView.GetSearchKey(ref searchKey);

                RecSettingData recSetKey = new RecSettingData();
                recSettingView.GetRecSetting(ref recSetKey);

                addItem.searchInfo = searchKey;
                addItem.recSetting = recSetKey;

                List<EpgAutoAddData> addList = new List<EpgAutoAddData>();
                addList.Add(addItem);

                if (cmd.SendAddEpgAutoAdd(addList) != 1)
                {
                    MessageBox.Show("追加に失敗しました");
                }
                else
                {
                    CommonManager.Instance.DB.SetUpdateNotify((UInt32)UpdateNotifyItem.ReserveInfo);
                    CommonManager.Instance.DB.ReloadReserveInfo();
                    SearchPg();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void button_chg_epgAutoAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EpgAutoAddData addItem = new EpgAutoAddData();
                addItem.dataID = autoAddID;
                EpgSearchKeyInfo searchKey = new EpgSearchKeyInfo();
                searchKeyView.GetSearchKey(ref searchKey);

                RecSettingData recSetKey = new RecSettingData();
                recSettingView.GetRecSetting(ref recSetKey);

                addItem.searchInfo = searchKey;
                addItem.recSetting = recSetKey;

                List<EpgAutoAddData> addList = new List<EpgAutoAddData>();
                addList.Add(addItem);

                if (cmd.SendChgEpgAutoAdd(addList) != 1)
                {
                    MessageBox.Show("変更に失敗しました");
                }
                else
                {
                    CommonManager.Instance.DB.SetUpdateNotify((UInt32)UpdateNotifyItem.ReserveInfo);
                    CommonManager.Instance.DB.ReloadReserveInfo();
                    SearchPg();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void listView_result_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (listView_result.SelectedItem != null)
                {
                    SearchItem item = listView_result.SelectedItem as SearchItem;
                    if (item.IsReserved == true)
                    {
                        ChangeReserve(item.ReserveInfo);
                    }
                    else
                    {
                        AddReserve(item.EventInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void ChangeReserve(ReserveData reserveInfo)
        {
            try
            {
                ChgReserveWindow dlg = new ChgReserveWindow();
                dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
                dlg.SetReserveInfo(reserveInfo);
                if (dlg.ShowDialog() == true)
                {
                    CommonManager.Instance.DB.SetUpdateNotify((UInt32)UpdateNotifyItem.ReserveInfo);
                    CommonManager.Instance.DB.ReloadReserveInfo();
                    SearchPg();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        private void AddReserve(EpgEventInfo eventInfo)
        {
            try
            {
                AddReserveEpgWindow dlg = new AddReserveEpgWindow();
                dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
                dlg.SetEventInfo(eventInfo);
                if (dlg.ShowDialog() == true)
                {
                    CommonManager.Instance.DB.SetUpdateNotify((UInt32)UpdateNotifyItem.ReserveInfo);
                    CommonManager.Instance.DB.ReloadReserveInfo();
                    SearchPg();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
            }
        }


        GridViewSorter<SearchItem> gridViewSorter = new GridViewSorter<SearchItem>();

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            //ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {

                    this.gridViewSorter.SortByMultiHeader(this.resultList, headerClicked);
                    listView_result.Items.Refresh();

                    //string header = "Reserved";
                    //if (headerClicked.Column.DisplayMemberBinding != null) {
                    //    header = ((Binding)headerClicked.Column.DisplayMemberBinding).Path.Path;
                    //}
                    //if (String.Compare(header, _lastHeaderClicked) != 0) {
                    //    direction = ListSortDirection.Ascending;
                    //    _lastHeaderClicked2 = _lastHeaderClicked;
                    //    _lastDirection2 = _lastDirection;
                    //} else {
                    //    if (_lastDirection == ListSortDirection.Ascending) {
                    //        direction = ListSortDirection.Descending;
                    //    } else {
                    //        direction = ListSortDirection.Ascending;
                    //    }
                    //}

                    //Sort(header, direction);

                    //_lastHeaderClicked = header;
                    //_lastDirection = direction;
                }
            }
        }

        //private void Sort(string sortBy, ListSortDirection direction) {
        //    try {
        //        ICollectionView dataView = CollectionViewSource.GetDefaultView(listView_result.DataContext);

        //        dataView.SortDescriptions.Clear();

        //        SortDescription sd = new SortDescription(sortBy, direction);
        //        dataView.SortDescriptions.Add(sd);
        //        if (_lastHeaderClicked2 != null) {
        //            if (String.Compare(sortBy, _lastHeaderClicked2) != 0) {
        //                SortDescription sd2 = new SortDescription(_lastHeaderClicked2, _lastDirection2);
        //                dataView.SortDescriptions.Add(sd2);
        //            }
        //        }
        //        dataView.Refresh();

        //        //Settings.Instance.ResColumnHead = sortBy;
        //        //Settings.Instance.ResSortDirection = direction;

        //    } catch (Exception ex) {
        //        MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
        //    }
        //}

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                if (this.Visibility == System.Windows.Visibility.Visible && this.Width > 0 && this.Height > 0)
                {
                    Settings.Instance.SearchWndWidth = this.Width;
                    Settings.Instance.SearchWndHeight = this.Height;
                }
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                if (this.Visibility == System.Windows.Visibility.Visible && this.Top > 0 && this.Left > 0)
                {
                    Settings.Instance.SearchWndTop = this.Top;
                    Settings.Instance.SearchWndLeft = this.Left;
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                switch (e.Key)
                {
                    case Key.F:
                        new BlackoutWindow(this).showWindow(this.button_search.Content.ToString());
                        this.button_search.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        break;
                    case Key.S:
                        new BlackoutWindow(this).showWindow(this.button_add_reserve.Content.ToString());
                        this.button_add_reserve.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        break;
                    case Key.A:
                        new BlackoutWindow(this).showWindow(this.button_add_epgAutoAdd.Content.ToString());
                        this.button_add_epgAutoAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        this.Close();
                        break;
                    case Key.C:
                        new BlackoutWindow(this).showWindow(this.button_chg_epgAutoAdd.Content.ToString());
                        this.button_chg_epgAutoAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        this.Close();
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.F2:
                        this.MenuItem_Click_Google(this, new RoutedEventArgs(Button.ClickEvent));
                        break;
                    case Key.F3:
                        this.MenuItem_Click_ProgramTable(this, new RoutedEventArgs(Button.ClickEvent));
                        break;
                    case Key.Escape:
                        if (this._popupWindow.IsVisible)
                        {
                            this._popupWindow.Hide();
                        }
                        else
                        {
                            this.Close();
                        }
                        break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.searchKeyView.ComboBox_andKey.Text))
            {
                this.searchKeyView.ComboBox_andKey.Focus();
            }
            else
            {
                this.SearchPg();
            }
            this._popupWindow = new PopupWindow(this);
        }

        void listView_result_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F2:
                    this.MenuItem_Click_Google(this, new RoutedEventArgs(Button.ClickEvent));
                    break;
                case Key.Enter:
                    this.MenuItem_Click_ShowDialog(listView_result.SelectedItem, new RoutedEventArgs());
                    break;
                case Key.Back:
                    this.MenuItem_Click_ChangeRecMode(listView_result.SelectedItem, new RoutedEventArgs());
                    break;
            }
        }

        private void MenuItem_Click_ShowDialog(object sender, RoutedEventArgs e)
        {
            if (listView_result.SelectedItem != null)
            {
                SearchItem item = listView_result.SelectedItem as SearchItem;
                if (item.IsReserved == true)
                {
                    ChangeReserve(item.ReserveInfo);
                }
                else
                {
                    AddReserve(item.EventInfo);
                }
            }
        }

        private void MenuItem_Click_ChangeRecMode(object sender, RoutedEventArgs e)
        {
            new BlackoutWindow(this).showWindow("予約←→無効");
            if (listView_result.SelectedItem != null)
            {
                SearchItem item = listView_result.SelectedItem as SearchItem;
                if (item.IsReserved == true)
                {
                    ChgReserveWindow dlg = new ChgReserveWindow();
                    dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;

                    if (item.ReserveInfo.RecSetting.RecMode == 5)
                    {
                        // 無効 => 予約
                        RecSettingData defSet = new RecSettingData();
                        Settings.GetDefRecSetting(0, ref defSet);
                        item.ReserveInfo.RecSetting.RecMode = defSet.RecMode;
                    }
                    else
                    {
                        //予約 => 無効
                        item.ReserveInfo.RecSetting.RecMode = 5;
                    }

                    dlg.SetReserveInfo(item.ReserveInfo);
                    dlg.button_chg_reserve.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                else
                {
                    AddReserveEpgWindow dlg = new AddReserveEpgWindow();
                    dlg.Owner = (Window)PresentationSource.FromVisual(this).RootVisual;
                    dlg.SetEventInfo(item.EventInfo);
                    dlg.button_add_reserve.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }
                //
                CommonManager.Instance.DB.SetUpdateNotify((UInt32)UpdateNotifyItem.ReserveInfo);
                CommonManager.Instance.DB.ReloadReserveInfo();
                SearchPg();
                //
                listView_result.SelectedItem = null;
            }
        }

        private void MenuItem_Click_ProgramDetail(object sender, RoutedEventArgs e)
        {
            SearchItem item1 = this.listView_result.SelectedItem as SearchItem;
            if (item1 != null)
            {
                this._popupWindow.show(item1.ProgramDetail);
            }
        }

        private void MenuItem_Click_Google(object sender, RoutedEventArgs e)
        {
            SearchItem item1 = this.listView_result.SelectedItem as SearchItem;
            if (item1 != null)
            {
                this._popupWindow.google(item1.EventName);
            }
        }

        private void MenuItem_Click_ProgramTable(object sender, RoutedEventArgs e)
        {
            SearchItem item1 = this.listView_result.SelectedItem as SearchItem;
            if (item1 != null)
            {
                BlackoutWindow.selectedSearchItem = item1;
                MainWindow mainWindow1 = this.Owner as MainWindow;
                if (mainWindow1 != null)
                {
                    if (BlackoutWindow.unvisibleSearchWindow != null)
                    {
                        // 非表示で保存するSearchWindowを1つに限定するため
                        this.Close();
                    }
                    else
                    {
                        this.Hide();
                        BlackoutWindow.blinkSearchButton_Start(
                            this,
                            mainWindow1.getSearchButton(true));
                    }
                    mainWindow1.moveTo_tabItem_epg();
                    mainWindow1.Hide(); // EpgDataView.UserControl_IsVisibleChangedイベントを発生させる
                    mainWindow1.Show();
                }
            }
        }

        private void Window_IsVisibleChanged_1(object sender, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mainWindow1 = this.Owner as MainWindow;
            if (this.IsVisible)
            {
                BlackoutWindow.blinkSearchButton_Stop(
                    this,
                    mainWindow1.getSearchButton(false));
            }
        }

        private void MenuItem_Click_Research(object sender, RoutedEventArgs e)
        {
            if (listView_result.SelectedItem != null)
            {
                SearchItem item = listView_result.SelectedItem as SearchItem;
                EpgSearchKeyInfo defKey = new EpgSearchKeyInfo();
                defKey.andKey = item.EventName;
                defKey.serviceList.Clear();

                foreach (ServiceItem info in searchKeyView.searchKeyDescView.listView_service.Items)
                {
                    if (info.ServiceName.Equals(item.ServiceName))
                    {
                        defKey.serviceList.Add((long)info.ID);
                    }
                }
                searchKeyView.SetSearchKey(defKey);

                button_search_Click(sender, e);
            }
        }

    }
}
