using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    using EpgView;
    using BoxExchangeEdit;

    /// <summary>
    /// EpgListMainView.xaml の相互作用ロジック
    /// </summary>
    public partial class EpgListMainView : EpgViewBase
    {
        private static int? lastActivateClass = null;

        protected class StateListMain : StateBase
        {
            public HashSet<ulong> selectID = null;
            public ListViewSelectedKeeper selectList = null;
            public StateListMain() { }
            public StateListMain(EpgListMainView view) : base(view)
            {
                selectID = view.GetSelectID();
                selectList = new ListViewSelectedKeeper(null, false, item => (item as SearchItem).KeyID);
                selectList.StoreListViewSelected(view.listView_event);
            }
        }
        public override EpgViewState GetViewState() { return new StateListMain(this); }
        protected new StateListMain RestoreState { get { return restoreState as StateListMain ?? new StateListMain(); } }

        private ListViewController<SearchItem> lstCtrl;
        private List<ServiceViewItem> serviceList = new List<ServiceViewItem>();
        protected override ListBox DataListBox { get { return listView_event; } }

        public EpgListMainView()
        {
            InitializeComponent();

            //リストビュー関連の設定
            var list_columns = Resources["ReserveItemViewColumns"] as GridViewColumnList;
            list_columns.AddRange(Resources["RecSettingViewColumns"] as GridViewColumnList);

            lstCtrl = new ListViewController<SearchItem>(this);
            lstCtrl.SetSavePath(CommonUtil.NameOf(() => Settings.Instance.EpgListColumn)
                    , CommonUtil.NameOf(() => Settings.Instance.EpgListColumnHead)
                    , CommonUtil.NameOf(() => Settings.Instance.EpgListSortDirection));
            lstCtrl.SetViewSetting(listView_event, gridView_event, true, true, list_columns);
            lstCtrl.SetSelectedItemDoubleClick(EpgCmds.ShowDialog);

            //ステータス変更の設定
            lstCtrl.SetSelectionChangedEventHandler((sender, e) => this.UpdateStatus(1));

            //過去番組移動ボタン関係
            SetControlsPeriod(timeJumpView, timeMoveView, button_now);

            base.InitCommand();

            //コマンド集の初期化の続き
            mc.SetFuncGetSearchList(isAll => (isAll == true ? lstCtrl.dataList.ToList() : lstCtrl.GetSelectedItemsList()));
            mc.SetFuncSelectSingleSearchData((noChange) => lstCtrl.SelectSingleItem(noChange));
            mc.SetFuncReleaseSelectedData(() => listView_event.UnselectAll());

            //コマンド集に無いもの
            mc.AddReplaceCommand(EpgCmds.ChgOnOffCheck, (sender, e) => lstCtrl.ChgOnOffFromCheckbox(e.Parameter, EpgCmds.ChgOnOff));

            //コマンド集からコマンドを登録
            mc.ResetCommandBindings(this, listView_event.ContextMenu);

            //コンテキストメニューの設定
            listView_event.ContextMenu.Tag = (int)2;//setViewInfo.ViewMode;
            listView_event.ContextMenu.Opened += new RoutedEventHandler(mc.SupportContextMenuLoading);

            //その他の設定
            SelectableItem.Set_CheckBox_PreviewChanged(listBox_service, listBox_service_Click_SelectChange);
            this.IsVisibleChanged += (sender, e) => { if (IsVisible) lastActivateClass = this.GetHashCode(); };
        }
        protected override void RefreshMenuInfo()
        {
            base.RefreshMenuInfo();
            mBinds.ResetInputBindings(this, listView_event);
            mm.CtxmGenerateContextMenu(listView_event.ContextMenu, CtxmCode.EpgView, true);
        }

        protected override void UpdateStatusData(int mode = 0)
        {
            if (mode == 0) this.status[1] = ViewUtil.ConvertSearchItemStatus(lstCtrl.dataList, "番組数");
            List<SearchItem> sList = lstCtrl.GetSelectedItemsList();
            this.status[2] = sList.Count == 0 ? "" : ViewUtil.ConvertSearchItemStatus(sList, "　選択中");
        }

        protected override void ReloadReserveViewItem()
        {
            //予約チェック
            lstCtrl.dataList.SetReserveData();
            lstCtrl.RefreshListView(true);
            listView_event_SelectionChanged(null, null);
        }

        protected override void ReloadProgramViewItem()
        {
            try
            {
                //表示していたサービスの保存
                var lastSID = RestoreState.selectID ?? GetSelectID();

                listBox_service.ItemsSource = null;

                serviceList = serviceListOrderAdjust.Select(info => new ServiceViewItem(info)).ToList();
                serviceList.ForEach(item => item.IsSelected = lastSID.Contains(item.Key) == false);

                listBox_service.ItemsSource = serviceList;

                UpdateEventList(true);
                ReloadReserveInfoFlg = false;//リストビューでは処理済みになる
                MoveNowTime();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        private void UpdateEventList(bool scroll = false)
        {
            lstCtrl.ReloadInfoData(dataList =>
            {
                var selected = new HashSet<ulong>(serviceList.Where(item => item.IsSelected).Select(item => item.Key));
                foreach (var item in serviceEventList.Where(item => selected.Contains(item.serviceInfo.Key)))
                {
                    dataList.AddRange(item.eventList.Where(e => e.IsGroupMainEvent == true).ToSearchList(viewInfo.FilterEnded));
                }
                dataList.ForEach(d =>
                {
                    d.EpgSettingIndex = viewInfo.EpgSettingIndex;
                    d.Filtered = viewData.EventFilteredHash.Contains(d.KeyID);
                });
                return true;
            });

            if (RestoreState.selectList != null) RestoreState.selectList.RestoreListViewSelected(listView_event);

            if (scroll == true)
            {
                listView_event.ScrollIntoView(listView_event.SelectedItem);
            }
        }
        private HashSet<ulong> GetSelectID()
        {
            return new HashSet<ulong>(serviceList.Where(s => s.IsSelected == false).Select(s => s.Key));
        }

        private void UpdateServiceChecked()
        {
            UpdateEventList();
            UpdateStatus();
        }
        private void button_chkAll_Click(object sender, RoutedEventArgs e)
        {
            button_All_Click(true);
        }
        private void button_clearAll_Click(object sender, RoutedEventArgs e)
        {
            button_All_Click(false);
        }
        private void button_All_Click(bool selected)
        {
            listBox_service.ItemsSource = null;
            serviceList.ForEach(info => info.IsSelected = selected);
            listBox_service.ItemsSource = serviceList;
            UpdateServiceChecked();
        }
        private void listBox_service_Click_SelectChange()
        {
            if (listBox_service.SelectedIndex < 0) return;

            var SelectedList = listBox_service.SelectedItems.OfType<SelectableItem>().ToList();

            listBox_service.ItemsSource = null;
            SelectedList.ForEach(item => item.IsSelected = !item.IsSelected);
            listBox_service.ItemsSource = serviceList;

            listBox_service.SelectedItemsAdd(SelectedList);
            UpdateServiceChecked();
        }

        private void listView_event_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            richTextBox_eventInfo.Document.Blocks.Clear();
            if (listView_event.SelectedItem == null) return;
            //
            richTextBox_eventInfo.Document = CommonManager.ConvertDisplayText((listView_event.SelectedItem as SearchItem).EventInfo);
        }

        protected override void MoveNowTime()
        {
            listView_event.SelectedIndex = lstCtrl.dataList.FindIndex(item => item.EventInfo.PgStartTime >= CommonUtil.EdcbNowEpg);
            listView_event.ScrollIntoView(listView_event.SelectedItem);
        }

        public override void SaveViewData()
        {
            if (lastActivateClass == this.GetHashCode()) lstCtrl.SaveViewDataToSettings();
        }
    }
}
