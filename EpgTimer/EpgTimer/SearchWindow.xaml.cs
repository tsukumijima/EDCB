using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// SearchWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchWindow : SearchWindowBase
    {
        public static event ViewUpdatedHandler ViewReserveUpdated = null;
        
        protected override DataItemViewBase DataView { get { return mainWindow.autoAddView.epgAutoAddView; } }
        protected override string AutoAddString { get { return "キーワード予約"; } }

        private ListViewController<SearchItem> lstCtrl;
        private CmdExeReserve mc; //予約系コマンド集

        private EpgViewPeriodDef prdef;
        private bool ArcSearch = false;
        private bool Searched = false;

        static SearchWindow()
        {
            //追加時の選択用
            mainWindow.autoAddView.epgAutoAddView.ViewUpdated += SearchWindow.UpdatesViewSelection;
        }
        public SearchWindow(EpgAutoAddData data = null, AutoAddMode mode = AutoAddMode.Find)
            : base(data, mode)
        {
            InitializeComponent();

            try
            {
                buttonID = "検索";
                base.SetParam(true, checkBox_windowPinned, checkBox_dataReplace);

                //スプリッタ位置設定。操作不可能な値をセットしないよう努める。
                if (Settings.Instance.SearchWndTabsHeight > grid_Tabs.Height.Value)
                {
                    grid_Tabs.Height = new GridLength(Math.Min(Settings.Instance.SearchWndTabsHeight, Height));
                }
                if (Settings.Instance.SearchWndJunreHeight >= 0)
                {
                    searchKeyView.grid_Junre.Height = new GridLength(Settings.Instance.SearchWndJunreHeight);
                }

                //リストビュー関連の設定
                var list_columns = Resources["ReserveItemViewColumns"] as GridViewColumnList;
                list_columns.AddRange(Resources["RecSettingViewColumns"] as GridViewColumnList);

                lstCtrl = new ListViewController<SearchItem>(this);
                lstCtrl.SetSavePath(CommonUtil.NameOf(() => Settings.Instance.SearchWndColumn)
                    , CommonUtil.NameOf(() => Settings.Instance.SearchColumnHead)
                    , CommonUtil.NameOf(() => Settings.Instance.SearchSortDirection));
                lstCtrl.SetViewSetting(listView_result, gridView_result, true, true, list_columns);
                lstCtrl.SetSelectedItemDoubleClick(EpgCmds.ShowDialog);

                //ステータス変更の設定
                lstCtrl.SetSelectionChangedEventHandler((sender, e) => this.UpdateStatus(1));

                //最初にコマンド集の初期化
                mc = new CmdExeReserve(this);
                mc.SetFuncGetSearchList(isAll => (isAll == true ? lstCtrl.dataList.ToList() : lstCtrl.GetSelectedItemsList()));
                mc.SetFuncSelectSingleSearchData((noChange) => lstCtrl.SelectSingleItem(noChange));
                mc.SetFuncReleaseSelectedData(() => listView_result.UnselectAll());
                mc.SetFuncGetRecSetting(() => recSettingView.GetRecSetting());
                mc.SetFuncGetSearchKey(() => searchKeyView.GetSearchKey());

                //コマンド集に無いもの
                mc.AddReplaceCommand(EpgCmds.ReSearch, mc_Research);
                mc.AddReplaceCommand(EpgCmds.ReSearch2, mc_Research);
                mc.AddReplaceCommand(EpgCmds.Search, button_search_Click);
                mc.AddReplaceCommand(EpgCmds.AddInDialog, autoadd_add);
                mc.AddReplaceCommand(EpgCmds.ChangeInDialog, autoadd_chg, (sender, e) => e.CanExecute = winMode == AutoAddMode.Change);
                mc.AddReplaceCommand(EpgCmds.DeleteInDialog, autoadd_del1, (sender, e) => e.CanExecute = winMode == AutoAddMode.Change);
                mc.AddReplaceCommand(EpgCmds.Delete2InDialog, autoadd_del2, (sender, e) => e.CanExecute = winMode == AutoAddMode.Change);
                mc.AddReplaceCommand(EpgCmds.BackItem, (sender, e) => MoveViewNextItem(-1));
                mc.AddReplaceCommand(EpgCmds.NextItem, (sender, e) => MoveViewNextItem(1));
                mc.AddReplaceCommand(EpgCmds.Cancel, (sender, e) => this.Close());
                mc.AddReplaceCommand(EpgCmds.ChgOnOffCheck, (sender, e) => lstCtrl.ChgOnOffFromCheckbox(e.Parameter, EpgCmds.ChgOnOff));

                //コマンド集を振り替えるもの
                mc.AddReplaceCommand(EpgCmds.JumpReserve, (sender, e) => mc_JumpTab(CtxmCode.ReserveView));
                mc.AddReplaceCommand(EpgCmds.JumpRecInfo, (sender, e) => mc_JumpTab(lstCtrl.SelectSingleItem(true).IsReserved ? CtxmCode.ReserveView : CtxmCode.RecInfoView));
                mc.AddReplaceCommand(EpgCmds.JumpTuner, (sender, e) => mc_JumpTab(CtxmCode.TunerReserveView));
                mc.AddReplaceCommand(EpgCmds.JumpTable, (sender, e) => mc_JumpTab(CtxmCode.EpgView));

                //コマンド集からコマンドを登録。
                mc.ResetCommandBindings(this, listView_result.ContextMenu);

                //コンテキストメニューを開く時の設定
                listView_result.ContextMenu.Opened += new RoutedEventHandler(mc.SupportContextMenuLoading);

                //ボタンの設定
                mBinds.View = CtxmCode.SearchWindow;
                mBinds.SetCommandToButton(button_search, EpgCmds.Search);
                mBinds.SetCommandToButton(button_add_reserve, EpgCmds.AddReserve);
                mBinds.SetCommandToButton(button_delall_reserve, EpgCmds.DeleteAll);
                mBinds.SetCommandToButton(button_add_epgAutoAdd, EpgCmds.AddInDialog);
                mBinds.SetCommandToButton(button_chg_epgAutoAdd, EpgCmds.ChangeInDialog);
                mBinds.SetCommandToButton(button_del_epgAutoAdd, EpgCmds.DeleteInDialog);
                mBinds.SetCommandToButton(button_del2_epgAutoAdd, EpgCmds.Delete2InDialog);
                mBinds.SetCommandToButton(button_up_epgAutoAdd, EpgCmds.BackItem);
                mBinds.SetCommandToButton(button_down_epgAutoAdd, EpgCmds.NextItem);
                mBinds.SetCommandToButton(button_cancel, EpgCmds.Cancel);

                //メニューの作成、ショートカットの登録
                RefreshMenu();

                //予約ウィンドウからのリスト検索、ジャンプ関連の対応
                DataListView = new AutoAddWinListView(listView_result);
                this.grid_main.Children.Add(DataListView);

                //その他のショートカット(検索ダイアログ固有の設定)。
                searchKeyView.InputBindings.Add(new InputBinding(EpgCmds.Search, new KeyGesture(Key.Enter)));
                listView_result.PreviewKeyDown += (sender, e) => ViewUtil.OnKeyMoveNextReserve(sender, e, DataListView);

                //録画設定タブ関係の設定
                recSettingView.SelectedPresetChanged += SetRecSettingTabHeader;
                recSettingTabHeader.MouseRightButtonUp += recSettingView.OpenPresetSelectMenuOnMouseEvent;

                //過去番組検索
                SetSearchPeriod();

                //ステータスバーの登録
                StatusManager.RegisterStatusbar(this.statusBar, this);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        public override void RefreshMenu()
        {
            mc.EpgInfoOpenMode = Settings.Instance.SearchEpgInfoOpenMode;
            mBinds.ResetInputBindings(this, listView_result);
            mm.CtxmGenerateContextMenu(listView_result.ContextMenu, CtxmCode.SearchWindow, true);
        }

        public EpgSearchKeyInfo GetSearchKey()
        {
            return searchKeyView.GetSearchKey();
        }
        public void SetSearchKey(EpgSearchKeyInfo key)
        {
            searchKeyView.SetSearchKey(key);
        }
        public RecSettingData GetRecSetting()
        {
            return recSettingView.GetRecSetting();
        }
        public void SetRecSetting(RecSettingData set)
        {
            recSettingView.SetDefSetting(set);
        }
        public override AutoAddData GetData()
        {
            var data = new EpgAutoAddData();
            data.dataID = (uint)dataID;
            data.searchInfo = GetSearchKey();
            data.recSetting = GetRecSetting();
            return data;
        }
        protected override bool SetData(EpgAutoAddData data)
        {
            if (data == null) return false;

            dataID = data.dataID;
            SetSearchKey(data.searchInfo);
            SetRecSetting(data.recSetting);
            return true;
        }
        public override void ChangeData(object data)
        {
            base.ChangeData(data);
            SearchPg();
        }
        public void SetRecSettingTabHeader(bool SimpleChanged = true)
        {
            recSettingTabHeader.Text = "録画設定" + recSettingView.GetRecSettingHeaderString(SimpleChanged);
        }

        private void button_search_Click(object sender, ExecutedRoutedEventArgs e)
        {
            SearchPg(true);
            if (Settings.Instance.UseLastSearchKey == true)
            {
                Settings.Instance.DefSearchKey = GetSearchKey();
                SettingWindow.UpdatesInfo("検索/キーワード予約ダイアログの前回検索条件変更");
            }
            StatusManager.StatusNotifySet(true, "検索を実行");
        }
        private void SearchPg(bool addSearchLog = false)
        {
            if (addSearchLog == true) searchKeyView.AddSearchLog();

            lstCtrl.ReloadInfoData(dataList =>
            {
                EpgSearchKeyInfo key = GetSearchKey();
                key.keyDisabledFlag = 0; //無効解除

                ArcSearch = searchKeyView.checkBox_noArcSearch.IsChecked == false && Settings.Instance.EpgSettingList[0].EpgArcDefaultDays > 0;
                EpgViewPeriod period = IsJumpPanelOpened ? SearchPeriod : ArcSearch ? prdef.DefPeriod : null;

                if (period != null) period.StrictLoad = true;
                var list = CommonManager.Instance.DB.SearchPg(key.IntoList(), period);
                dataList.AddRange(list.ToSearchList(period == null));

                //起動直後用。実際には過去検索して無くても必要な場合があるが、あまり重要ではないので無視する。
                if (ArcSearch) CommonManager.Instance.DB.ReloadRecFileInfo();

                return true;
            });

            UpdateStatus();
            SetRecSettingTabHeader(false);
            SetWindowTitle();
            RefreshMoveButtonStatus();
            Searched = true;
        }
        public override void SetWindowTitle()
        {
            this.Title = ViewUtil.WindowTitleText(searchKeyView.comboBox_andKey.Text, (winMode == AutoAddMode.Find ? "検索" : "キーワード自動予約登録"));
        }
        private void UpdateStatus(int mode = 0)
        {
            string s1 = null;
            string s2 = "";
            if (mode == 0) s1 = ViewUtil.ConvertSearchItemStatus(lstCtrl.dataList, "検索数");
            if (Settings.Instance.DisplayStatus == true)
            {
                List<SearchItem> sList = lstCtrl.GetSelectedItemsList();
                s2 = sList.Count == 0 ? "" : ViewUtil.ConvertSearchItemStatus(sList, "　選択中");
            }
            this.statusBar.SetText(s1, s2);
        }
        private void RefreshReserveInfo()
        {
            lstCtrl.dataList.SetReserveData();
            lstCtrl.RefreshListView(true);
            if (ViewReserveUpdated != null) ViewReserveUpdated(this.DataListView, true);
            UpdateStatus();
        }

        //proc 0:追加、1:変更、2:削除、3:予約ごと削除
        protected override int CheckAutoAddChange(ExecutedRoutedEventArgs e, int proc)
        {
            int ret = base.CheckAutoAddChange(e, proc);

            //データの更新。最初のキャンセルを過ぎていたら画面を更新する。
            if (ret >= 0) SearchPg();

            if (ret != 0) return ret;

            if (proc < 2 && Settings.Instance.CautionManyChange == true && searchKeyView.checkBox_keyDisabled.IsChecked != true)
            {
                if (MenuUtil.CautionManyMessage(lstCtrl.dataList.GetNoReserveList().Count, "予約追加の確認") == false)
                { return 2; }
            }

            return 0;
        }

        private void mc_JumpTab(CtxmCode trg_code)
        {
            JumpTabAndHide(trg_code, mc.GetJumpTabItem(trg_code));
        }
        private void mc_Research(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (listView_result.SelectedItem != null)
                {
                    SearchItem item = lstCtrl.SelectSingleItem();
                    EpgSearchKeyInfo defKey = MenuUtil.SendAutoAddKey(item.EventInfo, CmdExeUtil.IsKeyGesture(e), GetSearchKey());
                    
                    if (e.Command == EpgCmds.ReSearch)
                    {
                        SetSearchKey(defKey);
                        SearchPg();
                    }
                    else
                    {
                        WriteWindowSaveData();

                        var dlg = new SearchWindow(mode: winMode == AutoAddMode.Change ? AutoAddMode.NewAdd : winMode);
                        if (Settings.Instance.MenuSet.CancelAutoAddOff == true)
                        {
                            defKey.keyDisabledFlag = 0;
                        }
                        dlg.SetSearchKey(defKey);
                        dlg.SetRecSetting(this.GetRecSetting());
                        dlg.Show();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if ((winMode == AutoAddMode.Find || winMode == AutoAddMode.NewAdd) && string.IsNullOrEmpty(searchKeyView.comboBox_andKey.Text))
            {
                this.searchKeyView.comboBox_andKey.Focus();
                SetRecSettingTabHeader(true);
            }
            else
            {
                this.SearchPg();
            }
        }

        protected override void WriteWindowSaveData()
        {
            lstCtrl.SaveViewDataToSettings();
            base.WriteWindowSaveData();
            Settings.Instance.SearchWndTabsHeight = grid_Tabs.Height.Value;
            Settings.Instance.SearchWndJunreHeight = Math.Min(searchKeyView.grid_Junre.ActualHeight, searchKeyView.grid_Filter.ActualHeight - 6);
            Settings.Instance.ArcSearch = searchKeyView.checkBox_noArcSearch.IsChecked == false;
        }

        private bool RefreshReserveInfoFlg = false;
        public override void UpdateInfo(bool reload = true)
        {
            RefreshReserveInfoFlg = true;
            base.UpdateInfo(reload);
        }
        protected override void ReloadInfo()
        {
            //再検索はCtrlCmdを使うので、アクティブウィンドウでだけ実行させる。
            if (ReloadInfoFlg == true && this.IsActive == true)
            {
                RefreshPeriod();
                SearchPg();
                ReloadInfoFlg = false;
                RefreshReserveInfoFlg = false;
            }
            //表示の更新は見えてれば実行する。
            if (RefreshReserveInfoFlg == true && this.IsVisible == true && (this.WindowState != WindowState.Minimized || this.IsActive == true))
            {
                RefreshPeriod();
                recSettingView.RefreshView();
                RefreshMoveButtonStatus();
                RefreshReserveInfo();
                RefreshReserveInfoFlg = false;
            }
        }
        public static void UpdatesRecinfo()
        {
            foreach (var win in Application.Current.Windows.OfType<SearchWindow>())
            {
                if (win.ArcSearch == true) win.UpdateInfo(false);
            }
        }

        //過去番組移動関係
        private bool IsJumpPanelOpened { get{ return panel_timeJump.Visibility == Visibility.Visible; }}
        private EpgViewPeriod SearchPeriod { get { return timeJumpView.GetDate(); } }
        private void SetSearchPeriod()
        {
            RefreshPeriod();
            searchKeyView.checkBox_noArcSearch.IsChecked = !Settings.Instance.ArcSearch;
            searchKeyView.checkBox_noArcSearch.Checked += (sender, e) => { if (Searched) SearchPg(); };
            searchKeyView.checkBox_noArcSearch.Unchecked += (sender, e) => { if (Searched) SearchPg(); };
            timeJumpView.SetSearchMode();
            timeJumpView.JumpDateClick += pr => SearchPg();
            timeJumpView.DateChanged += RefreshMoveButtonStatus;
            button_Panel.Click += (sender, e) => button_Panel_Click();
            button_Prev.Click += (sender, e) => button_Time_Click(MoveTimeTarget(-1));
            button_Next.Click += (sender, e) => button_Time_Click(MoveTimeTarget(1));
            button_Reset.Click += (sender, e) => button_Time_Click(prdef.DefPeriod);
            button_Prev.ToolTipOpening += (sender, e) => button_Time_Tooltip(button_Prev, e, -1);
            button_Next.ToolTipOpening += (sender, e) => button_Time_Tooltip(button_Next, e, 1);
            button_Reset.ToolTipOpening += (sender, e) => button_Reset.ToolTip = prdef.DefPeriod.ConvertText(prdef.DefPeriod.End);
        }
        private void RefreshPeriod()
        {
            prdef = new EpgViewPeriodDef(Settings.Instance.EpgSettingList[0]);
            timeJumpView.RefreshPeriod();
            EpgViewPeriod pr = timeJumpView.GetDate();
            if (pr.Equals(prdef.DefPeriod) == false) pr.Days = prdef.InitMoveDays;
            timeJumpView.SetDate(pr);
        }
        private void button_Panel_Click()
        {
            panel_timeJump.Visibility = IsJumpPanelOpened ? Visibility.Collapsed : Visibility.Visible;
            button_Panel.Content = IsJumpPanelOpened ? "↑" : "↓";
            RefreshMoveButtonStatus();
        }
        private void button_Time_Click(EpgViewPeriod period)
        {
            timeJumpView.SetDate(period);
            SearchPg();
        }
        private void button_Time_Tooltip(Button btn, ToolTipEventArgs e, int mode)
        {
            e.Handled = !btn.IsEnabled;
            btn.ToolTip = MoveTimeTarget(mode).ConvertText(DateTime.MaxValue);
        }
        private EpgViewPeriod MoveTimeTarget(int mode)
        {
            EpgViewPeriod pr = timeJumpView.GetDate();
            if (pr.Equals(prdef.DefPeriod)) pr.Days = prdef.InitMoveDays;
            pr.Start += TimeSpan.FromDays(mode * (int)pr.Days);
            return pr;
        }
        private void RefreshMoveButtonStatus()
        {
            searchKeyView.checkBox_noArcSearch.IsEnabled = !IsJumpPanelOpened;
            if (IsJumpPanelOpened == false) return;
            button_Prev.IsEnabled = SearchPeriod.Start > CommonManager.Instance.DB.EventTimeMin;
            button_Next.IsEnabled = SearchPeriod.End < prdef.DefPeriod.End;
            timeJumpView.SetDate(null, CommonManager.Instance.DB.EventTimeMin);
        }
    }
    public class SearchWindowBase : AutoAddWindow<SearchWindow, EpgAutoAddData>
    {
        public SearchWindowBase() { }//デザイナ用
        public SearchWindowBase(EpgAutoAddData data = null, AutoAddMode mode = AutoAddMode.Find) : base(data, mode) { }
    }

    public enum AutoAddMode { Find, NewAdd, Change }
    public class AutoAddWindow<T, S> : HideableWindow<T> where S : AutoAddData
    {
        protected UInt64 dataID = 0;
        protected override UInt64 DataID { get { return dataID; } }
        protected override IEnumerable<KeyValuePair<UInt64, object>> DataRefList { get { return AutoAddData.GetDBManagerList(typeof(S)).Select(d => new KeyValuePair<UInt64, object>(d.DataID, d)); } }
        //予約ウィンドウからのリスト検索、ジャンプ関連の対応
        public AutoAddWinListView DataListView { get; protected set; }
        public class AutoAddWinListView : DataItemViewBase
        {
            public ListBox listbox;
            public AutoAddWinListView(ListBox lb) { listbox = lb; }
            protected override ListBox DataListBox { get { return listbox; } }
        }

        protected virtual string AutoAddString { get { return ""; } }
        protected AutoAddData autoAddData { get { return AutoAddData.AutoAddList(typeof(S), (uint)dataID); } }

        public AutoAddWindow(S data = null, AutoAddMode mode = AutoAddMode.Find)
        {
            this.Loaded += (sender, e) => { SetData(data); SetViewMode(data != null && DataID == 0 ? AutoAddMode.NewAdd : mode); UpdateViewSelection(); };
        }

        protected AutoAddMode winMode = AutoAddMode.Find;
        protected void SetViewMode(AutoAddMode mode)
        { 
            winMode = mode;
            SetWindowTitle();
            if (mode != AutoAddMode.Change) dataID = 0;
        }
        public virtual void SetWindowTitle() { }

        public virtual AutoAddData GetData() { return null; }
        protected virtual bool SetData(S data) { return false; }

        //検索の更新がある
        public override void ChangeData(object data)
        {
            if (SetData(data as S) == false) return;
            SetViewMode(DataID == 0 ? AutoAddMode.NewAdd : AutoAddMode.Change);
        }

        //proc 0:追加、1:変更、2:削除、3:予約ごと削除
        static string[] cmdMsg = new string[] { "追加", "変更", "削除", "予約ごと削除" };
        protected virtual int CheckAutoAddChange(ExecutedRoutedEventArgs e, int proc)
        {
            if (proc != 3)
            {
                if (CmdExeUtil.IsMessageBeforeCommand(e) == true)
                {
                    if (MessageBox.Show(AutoAddString + "を" + cmdMsg[proc] + "します。\r\nよろしいですか？", cmdMsg[proc] + "の確認", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    { return -2; }
                }
            }
            else
            {
                if (CmdExeUtil.CheckAllProcCancel(e, autoAddData.IntoList(), true) == true)
                { return -1; }
            }

            if (proc != 0)
            {
                if (autoAddData == null)
                {
                    MessageBox.Show("項目がありません。\r\n" + "既に削除されています。", "データエラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    SetViewMode(AutoAddMode.NewAdd);
                    return 1;
                }
            }

            return 0;
        }

        protected void autoadd_add(object sender, ExecutedRoutedEventArgs e) { autoadd_add_chg(e, 0); }
        protected void autoadd_chg(object sender, ExecutedRoutedEventArgs e) { autoadd_add_chg(e, 1); }
        protected void autoadd_add_chg(ExecutedRoutedEventArgs e, int code)
        {
            bool ret = false;
            try
            {
                AutoAddData data = GetData();
                if (data != null && CheckAutoAddChange(e, code) == 0)
                {
                    if (code == 0)
                    {
                        ret = MenuUtil.AutoAddAdd(data.IntoList());
                        if (ret == true)
                        {
                            //割り当てられたIDが欲しいだけなのでEpgTimer内のもろもろは再構築せず、Srvからデータだけ取得する。
                            SetData(AutoAddData.GetAutoAddListSrv(typeof(S)).LastOrDefault() as S);
                            SetViewMode(AutoAddMode.Change);
                        }
                    }
                    else
                    {
                        ret = MenuUtil.AutoAddChange(data.IntoList());
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            StatusManager.StatusNotifySet(ret, AutoAddString + "を" + cmdMsg[code]);
        }

        protected void autoadd_del1(object sender, ExecutedRoutedEventArgs e) { autoadd_del(e, 2); }
        protected void autoadd_del2(object sender, ExecutedRoutedEventArgs e) { autoadd_del(e, 3); }
        protected void autoadd_del(ExecutedRoutedEventArgs e, int code)
        {
            bool ret = false;
            try
            {
                if (CheckAutoAddChange(e, code) == 0)
                {
                    if (code == 2)
                    {
                        ret = MenuUtil.AutoAddDelete(autoAddData.IntoList());
                    }
                    else
                    {
                        ret = MenuUtil.AutoAddDelete(autoAddData.IntoList(), true, true);
                    }

                    if (ret == true)
                    {
                        SetViewMode(AutoAddMode.NewAdd);
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            StatusManager.StatusNotifySet(ret, AutoAddString + "を" + cmdMsg[code]);
        }

        public static void UpdatesAutoAddViewOrderChanged(Dictionary<ulong, ulong> changeIDTable)
        {
            foreach (var win in Application.Current.Windows.OfType<AutoAddWindow<T, S>>())
            {
                win.UpdateAutoAddViewOrderChanged(changeIDTable);
            }
        }
        protected void UpdateAutoAddViewOrderChanged(Dictionary<ulong, ulong> changeIDTable)
        {
            if (dataID == 0) return;

            if (changeIDTable.ContainsKey(dataID) == false)
            {
                //ID無くなった
                SetViewMode(AutoAddMode.NewAdd);
            }
            else
            {
                //新しいIDに変更
                dataID = changeIDTable[dataID];
            }
        }
    }
}
