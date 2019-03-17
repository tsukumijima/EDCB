using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EpgTimer
{
    using EpgView;

    /// <summary>
    /// EpgView.xaml の相互作用ロジック
    /// </summary>
    public partial class EpgDataView : DataItemViewBase
    {
        bool IsSelected { get { return Visibility == Visibility.Visible; } }//タスクアイコン状態も含む
        IEnumerable<EpgTabItem> Tabs { get { return tabControl.Items.OfType<EpgTabItem>(); } }
        List<CustomEpgTabInfo> tabInfo = new List<CustomEpgTabInfo>();//Settingデータの参照を保持
        CustomEpgTabInfo get_tabInfo(string Uid) { return tabInfo.Find(ti => ti.Uid == Uid); }
        ContextMenu ctxm = new ContextMenu();//使用時に生成するとClearTabHeader()のタイミングが前後するので準備しておく。

        public EpgDataView()
        {
            InitializeComponent();

            //コンテキストメニューの設定
            ctxm.Unloaded += (s, e) => ClearTabHeader();
            tabControl.MouseRightButtonUp += EpgTabContextMenuOpen;
            grid_viewMode.PreviewMouseRightButtonUp += EpgTabContextMenuOpen;

            //コマンドだと細かく設定しないといけないパターンなのでこの辺りで一括指定
            this.PreviewKeyDown += (sender, e) => ViewUtil.OnKeyMoveNextReserve(sender, e, ActiveView);
        }

        /// <summary>選択されている番組表を返す</summary>
        public EpgViewBase ActiveView
        {
            get
            {
                var tab = tabControl.SelectedItem as EpgTabItem;
                return tab == null ? null : tab.view;
            }
        }

        //存在しないときは、本当に無いか、破棄されて保存済み
        public void SaveViewData() { foreach (var tb in Tabs) tb.SaveViewData(); }

        //メニューの更新。ストックにもフラグを立てる。
        public void RefreshMenu()
        {
            TabModeSet();
            foreach (var tb in Tabs) tb.UpdateMenu();
        }
        public void TabModeSet()
        {
            bool tabEnable = Settings.Instance.EpgNameTabEnabled == true;
            bool modEnable = Settings.Instance.EpgViewModeTabEnabled == true;
            tabControl.Visibility = tabEnable ? Visibility.Visible : Visibility.Collapsed;
            dummyTab.Visibility = tabEnable ? Visibility.Hidden : Visibility.Collapsed;
            grid_viewMode.Visibility = modEnable ? Visibility.Visible : Visibility.Collapsed;
            int m = tabEnable ? 5 : 0;
            grid_viewMode.Margin = new Thickness(tabEnable ? 0 : -4, 0, -5 - m, 5);
            grid_tab.Margin = new Thickness(m, -m, m, m);
        }

        /// <summary>予約情報の更新通知</summary>//ストックにもフラグを立てる。
        public void UpdateReserveInfo() { foreach (var tb in Tabs) tb.UpdateReserveInfo(); }

        /// <summary>EPGデータの再描画</summary>//ReloadInfo()とも、DataViewBaseのは未使用
        public override void UpdateInfo(bool epgReloaded = true)
        {
            ReloadInfoFlg = true;
            //不要ストックの破棄。EPGデータを更新しない設定の時は表示中以外のストックを放棄する。
            foreach (var tb in Tabs) tb.CleanUpContent(Settings.Instance.NgAutoEpgLoadNW);
            if (IsSelected == true || Settings.Instance.PrebuildEpg == true && epgReloaded == true)
            {
                ReloadInfo();
            }
            Dispatcher.BeginInvoke(new Action(() => GC.Collect()), DispatcherPriority.Input);
        }
        protected override void ReloadInfo()
        {
            if (ReloadInfoFlg == false) return;
            ReloadInfoFlg = false;

            //タブが無ければ生成。
            if (tabControl.Items.Count == 0) CreateTabItem();
            foreach (var tb in Tabs) tb.SetContent(true);
        }

        EpgViewState oldState = null;
        string oldID = null;
        /// <summary>設定の更新通知</summary>
        public void UpdateSetting(bool noRestoreState = false)
        {
            try
            {
                SaveViewData();

                //表示していた番組表の情報を保存
                var item = tabControl.SelectedItem as EpgTabItem;
                if (item != null)
                {
                    if (noRestoreState == false && item.view != null)
                    {
                        oldState = item.view.GetViewState();
                    }
                    oldID = item.Uid;
                }

                //一度全部削除して作り直す。
                //保存情報は次回のタブ作成時に復元する。
                tabInfo.Clear();
                tabControl.Items.Clear();
                //実際にEPGが読込まれたかと無関係に、操作感としてこれでいい。
                UpdateInfo(!Settings.Instance.NgAutoEpgLoadNW);
            }
            catch (Exception ex) { CommonUtil.DispatcherMsgBoxShow(ex.Message + "\r\n" + ex.StackTrace); }

            //UpdateInfo()はオプションによるガ非表示の時走らない。
            //データはここでクリアしてしまうので、現に表示されているもの以外は表示状態はリセットされる。
            //ただし、番組表(oldID)の選択そのものは保持する。
            oldState = null;
        }

        /// <summary>タブ生成</summary>
        private bool OnCreateTab = false;
        private void CreateTabItem()
        {
            OnCreateTab = true;//タブの初期選択対策。挙動が以前と変わってるような‥
            try
            {
                tabInfo = Settings.Instance.UseCustomEpgView == false ?
                    CommonManager.CreateDefaultTabInfo() : Settings.Instance.CustomEpgTabList.ToList();

                //とりあえず同じIDを探して表示してみる(中身は別物になってるかもしれないが、表示を試みる)。
                //標準・カスタム切り替えの際は、標準番組表が負のIDを与えられているので、このコードは走らない。
                foreach (CustomEpgTabInfo info in tabInfo.Where(info => info.IsVisible == true))
                {
                    tabControl.Items.Add(new EpgTabItem(info, this, oldID, info.Uid == oldID ? oldState : null));
                }
                if (tabControl.SelectedIndex < 0) tabControl.SelectedIndex = 0;
            }
            catch (Exception ex) { CommonUtil.DispatcherMsgBoxShow(ex.Message + "\r\n" + ex.StackTrace); }

            oldID = null;
            OnCreateTab = false;
        }

        private void epgView_ViewSettingClick(int param)
        {
            try
            {
                var tab = tabControl.SelectedItem as EpgTabItem;
                if (tab == null) return;

                if (param < -2 || 2 < param) return;

                CustomEpgTabInfo info = null;
                if (param == -1)
                {
                    //表示設定変更ダイアログから
                    var dlg = new EpgDataViewSettingWindow(tab.Info);
                    dlg.Owner = CommonUtil.GetTopWindow(this);
                    dlg.SetTryMode(Settings.Instance.UseCustomEpgView == false);
                    if (dlg.ShowDialog() == false)
                    { return; }

                    info = dlg.GetSetting();
                    if (info.Uid != tab.Uid) return;//保険

                    //設定の保存。
                    if (Settings.Instance.UseCustomEpgView == true && Settings.Instance.TryEpgSetting == false
                        && info.ID >= 0 && info.ID < tabInfo.Count && info.ID < Settings.Instance.CustomEpgTabList.Count)
                    {
                        tabInfo[info.ID] = info;
                        Settings.Instance.CustomEpgTabList[info.ID] = info;
                        Settings.SaveToXmlFile();
                        SettingWindow.UpdatesInfo("番組表関連の変更");
                    }

                    if (info.IsVisible == false)
                    {
                        tabControl.Items.Remove(tab);
                        return;
                    }
                }
                else if (param == -2)
                {
                    info = tabInfo.Find(tinfo => tinfo.ID == tab.Info.ID);
                    if (info == null) return;
                }

                //選択用タブの選択を切り替え。
                tab_viewMode_ChangeTabOnly(info != null ? info.ViewMode : param);
                tab.ChangeContent(info, param);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        protected override void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible == false) return;

            ReloadInfo();

            SearchJumpTargetProgram(BlackoutWindow.SelectedItem);
            //番組表タブが一つもないときなどのゴミ掃除
            Dispatcher.BeginInvoke(new Action(() => BlackoutWindow.Clear()), DispatcherPriority.Input);
        }

        /// <summary>予約一覧からのジャンプ先を番組表タブから探す</summary>
        public bool SearchJumpTargetProgram(SearchItem trg, bool dryrun = false)
        {
            try
            {
                if (trg == null) return false;

                var data = (AutoAddTargetData)trg.ReserveInfo ?? trg.EventInfo;
                if (data == null) return false;

                if (Tabs.Any() == false)//dryrun以外でここに来るときは本当にタブが無い
                {
                    var infoList = Settings.Instance.UseCustomEpgView == false ?
                        CommonManager.CreateDefaultTabInfo() : Settings.Instance.CustomEpgTabList.ToList();
                    return infoList.Where(info => info.IsVisible == true)
                        .SelectMany(info => CommonManager.Instance.DB.ExpandSpecialKey(info.ViewServiceList)).Contains(data.Create64Key());
                }
                var tab = Tabs.OrderBy(tb => tb.IsSelected ? 0 : 1).FirstOrDefault(tb =>
                {
                    bool ret = tb.HasKey(data.Create64Key());
                    if (ret == true && tb.view != null && tb.IsEpgLoaded == true)
                    {
                        return tb.view.IsEnabledJumpTab(trg);
                    }
                    return ret;
                });
                if (tab != null && dryrun == false) tab.IsSelected = true;
                return tab != null;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return false;
        }

        //表示切り替えタブ関係
        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //データの変更はEpgTabItem.OnSelected()が行うのでタブの見かけだけ変更する。
            if (tabControl.SelectedItem != null)
            {
                tab_viewMode_ChangeTabOnly((tabControl.SelectedItem as EpgTabItem).Info.ViewMode);
            }
        }
        private void tab_viewMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tab_viewMode_Changing == true) return;
            epgView_ViewSettingClick(tab_viewMode.SelectedIndex);
        }
        private bool tab_viewMode_Changing = false;
        private void tab_viewMode_ChangeTabOnly(int idx)
        {
            try
            {
                tab_viewMode_Changing = true;
                tab_viewMode.SelectedIndex = idx;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            tab_viewMode_Changing = false;
        }

        //番組表ヘッダ用のコンテキストメニュー関係
        private enum edvCmds { Setting, TabSetting, ResetAll, All, /*Delete,*/ DeleteAll, ModeChange, VisibleChange, NameTabChange, NameTabVisible, ViewModeTabVisible, MoveCheckedTab }
        public void EpgTabContextMenuOpen(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ClearTabHeader();//連続で表示される場合用
                ctxm.Items.Clear();
                e.Handled = true;

                //ヘッダでのオープンかどうか判定。TabControlに持たせているのでPlacementTargetは使えない。
                var tab = tabControl.GetPlacementItem() as EpgTabItem;
                if (sender == tabControl && tabControl.Items.Count != 0 && tab == null)
                {
                    //番組表エリアでは番組表が一つもないとき以外は表示しない
                    return;
                }

                ctxm.IsOpen = true;

                //メニュー追加用
                MenuItem menu1;
                Func<ItemsControl, bool, object, object, string, MenuItem> tabMenuAdd = (menu, en, cmds, header, uid) =>
                {
                    menu1 = new MenuItem { Header = header, IsEnabled = en, Uid = uid, Tag = cmds };
                    menu1.Click += new RoutedEventHandler(MenuCmdsExecute);
                    menu.Items.Add(menu1);
                    return menu1;
                };

                //操作用メニューの設定
                //メイン画面用
                if (this.IsVisible == false)
                {
                    tabMenuAdd(ctxm, true, edvCmds.Setting, "番組表全般の設定...(_O)", "");
                    return;
                }

                //番組表画面用
                bool noTab = tabControl.IsVisible == false;
                var trg = tab ?? tabControl.SelectedItem as EpgTabItem ?? new EpgTabItem() { Tag = "(番組表)" };

                //ビューモードサブメニュー
                var menu_vs = new MenuItem { Header = trg.Tag + " の表示モード(_V)", IsEnabled = trg.Uid != "", Uid = trg.Uid };
                for (int i = 0; i <= 2; i++)
                {
                    menu1 = tabMenuAdd(menu_vs, true, EpgCmds.ViewChgMode, CommonManager.ConvertViewModeText(i) + string.Format("(_{0})", i + 1), trg.Uid);
                    menu1.CommandParameter = new EpgCmdParam(null, 0, i);//コマンド自体は、menuの処理メソッドから走らせる。
                    menu1.IsChecked = trg.Uid == "" ? false : i == trg.Info.ViewMode;
                }
                menu_vs.Items.Add(new Separator());
                tabMenuAdd(menu_vs, true, EpgCmds.ViewChgSet, "表示設定(_S)...", trg.Uid);
                tabMenuAdd(menu_vs, true, EpgCmds.ViewChgReSet, "一時的な変更をクリア(_R)", trg.Uid);

                //番組表の操作メニュー
                var menu_tb = new MenuItem { Header = "番組表の操作(_E)" };
                if (noTab == false)
                {
                    menu1 = tabMenuAdd(menu_tb, true, edvCmds.ModeChange, (Settings.Instance.UseCustomEpgView == true ? "デフォルト" : "カスタマイズ") + "表示に切り替え(_M)", "");
                    menu1.ToolTip = "現在の表示 : " + (Settings.Instance.UseCustomEpgView == false ? "デフォルト" : "カスタマイズ") + "表示";
                    menu_tb.Items.Add(new Separator());
                    tabMenuAdd(menu_tb, tabInfo.Any(item => item.IsVisible == true) || Settings.Instance.UseCustomEpgView == false, edvCmds.ResetAll, "一時的な変更を全てクリア(_R)", "");
                    tabMenuAdd(menu_tb, tabInfo.Any(item => item.IsVisible == false), edvCmds.All, "全て表示(_A)", "");
                    tabMenuAdd(menu_tb, tabInfo.Any(item => item.IsVisible == true), edvCmds.DeleteAll, "全て非表示(_H)", "");
                    menu_tb.Items.Add(new Separator());
                }
                menu1 = tabMenuAdd(menu_tb, true, edvCmds.NameTabVisible, "表示項目タブ(_P)", "");
                menu1.IsChecked = Settings.Instance.EpgNameTabEnabled == true;
                menu1 = tabMenuAdd(menu_tb, true, edvCmds.ViewModeTabVisible, "表示モード切り替えタブ(_T)", "");
                menu1.IsChecked = Settings.Instance.EpgViewModeTabEnabled == true;
                if (noTab == false)
                {
                    menu_tb.Items.Add(new Separator());
                    menu1 = tabMenuAdd(menu_tb, true, edvCmds.MoveCheckedTab, "「表示」に切り替えたタブへ移動する(_C)", "");
                    menu1.IsChecked = Settings.Instance.EpgTabMoveCheckEnabled == true;
                }

                //メインメニュー
                ctxm.Items.Add(menu_vs);
                ctxm.Items.Add(menu_tb);
                tabMenuAdd(ctxm, true, sender is TabItem ? edvCmds.Setting : edvCmds.TabSetting,
                    (sender is TabItem ? "番組表全般" : "表示項目") + "の設定(_O)...", trg.Uid);
                //ctxm.Items.Add(new Separator());
                //tabMenuAdd(ctxm, trg.Uid != "", edvCmds.Delete, trg.Tag + " を非表示(_D)", trg.Uid);
                ctxm.Items.Add(new Separator());

                //番組表タブの項目追加。
                if (tabInfo.Count == 0)
                {
                    ctxm.Items.Add(new MenuItem { Header = "(番組表の設定がありません)", IsEnabled = false });
                }
                //番組表項目追加。
                if (noTab == false)
                {
                    //表示項目タブがある場合は、表示項目タブがあるものにチェックを入れる。
                    //メニュー実行時は表示項目タブのON/OFFを切り替える
                    tabInfo.ForEach(info =>
                    {
                        menu1 = tabMenuAdd(ctxm, true, edvCmds.VisibleChange, info.TabName, info.Uid);
                        menu1.IsChecked = info.IsVisible;
                        if (trg.Uid == info.Uid) menu1.FontWeight = FontWeights.Bold;
                    });
                }
                else
                {
                    //表示項目タブがない場合は、現在表示されているものにチェックを入れる。
                    //メニュー実行時は表示する番組表項目を切り替え
                    foreach (var info in tabInfo.Where(ti => ti.IsVisible == true))
                    {
                        menu1 = tabMenuAdd(ctxm, true, edvCmds.NameTabChange, info.TabName, info.Uid);
                        menu1.IsChecked = trg.Uid == info.Uid;
                    }
                }
                trg.Foreground = Brushes.Red;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        private void ClearTabHeader()
        {
            foreach (TabItem tab in tabControl.Items) tab.ClearValue(Control.ForegroundProperty);
        }

        private void MenuCmdsExecute(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectID = null;
                List<bool> infoBack = tabInfo.Select(info => info.IsVisible).ToList();
                var menu = sender as MenuItem;

                if (menu.Tag is RoutedUICommand)
                {
                    EpgTabItem tab = Tabs.FirstOrDefault(ti => ti.Uid == menu.Uid);
                    if (tab != null && tab.view != null)
                    {
                        (menu.Tag as RoutedUICommand).Execute(menu.CommandParameter, tab.view);
                    }
                    return;
                }
                switch (menu.Tag as edvCmds?)
                {
                    case edvCmds.Setting:
                        ViewUtil.MainWindow.OpenSettingDialog(SettingWindow.SettingMode.EpgSetting);
                        return;
                    case edvCmds.TabSetting:
                        ViewUtil.MainWindow.OpenSettingDialog(SettingWindow.SettingMode.EpgTabSetting, menu.Uid);
                        return;
                    case edvCmds.ResetAll:
                        this.UpdateSetting(true);
                        return;
                    case edvCmds.ModeChange:
                        Settings.Instance.UseCustomEpgView = !Settings.Instance.UseCustomEpgView;
                        SettingWindow.UpdatesInfo("番組表関連の変更");
                        this.UpdateSetting(true);
                        return;
                    case edvCmds.All:
                        tabInfo.ForEach(ti => ti.IsVisible = true);
                        break;
                    case edvCmds.DeleteAll:
                        tabInfo.ForEach(ti => ti.IsVisible = false);
                        break;
                    //case edvCmds.Delete://現在のところVisibleChangeと同じになる
                    case edvCmds.VisibleChange:
                        if (Settings.Instance.EpgTabMoveCheckEnabled == true || tabControl.Items.Count == 0)
                        {
                            selectID = menu.Uid;
                        }
                        var info = get_tabInfo(menu.Uid);
                        if (info != null) info.IsVisible = !info.IsVisible;
                        break;
                    case edvCmds.NameTabChange:
                        EpgTabItem tab = Tabs.FirstOrDefault(ti => ti.Uid == menu.Uid);
                        if (tab != null) tab.IsSelected = true;
                        return;
                    case edvCmds.NameTabVisible:
                        Settings.Instance.EpgNameTabEnabled = !Settings.Instance.EpgNameTabEnabled;
                        SettingWindow.UpdatesInfo("番組表関連の変更");
                        TabModeSet();
                        return;
                    case edvCmds.ViewModeTabVisible:
                        Settings.Instance.EpgViewModeTabEnabled = !Settings.Instance.EpgViewModeTabEnabled;
                        SettingWindow.UpdatesInfo("番組表関連の変更");
                        TabModeSet();
                        return;
                    case edvCmds.MoveCheckedTab:
                        Settings.Instance.EpgTabMoveCheckEnabled = !Settings.Instance.EpgTabMoveCheckEnabled;
                        SettingWindow.UpdatesInfo("番組表関連の変更");
                        return;
                }

                if (Settings.Instance.UseCustomEpgView == true)
                {
                    List<bool> infoNew = tabInfo.Select(info => info.IsVisible).ToList();
                    if (infoBack.Count != infoNew.Count || infoBack.Zip(infoNew, (v1, v2) => v1 ^ v2).Any(v => v) == true)
                    {
                        SettingWindow.UpdatesInfo("番組表関連の変更");
                    }
                }

                int pos = 0;
                foreach (var info in tabInfo)
                {
                    EpgTabItem tab = Tabs.FirstOrDefault(ti => ti.Uid == info.Uid);
                    if (info.IsVisible == false)
                    {
                        tabControl.Items.Remove(tab);
                    }
                    else
                    {
                        if (tab == null)
                        {
                            var tabItem = new EpgTabItem(info, this, selectID);
                            tabControl.Items.Insert(pos, tabItem);
                            tabItem.SetContent();
                        }
                        pos++;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        //EpgTabItemを内部クラスにしづらいので、データ部分だけ仮にまとめる
        public class EpgDataViewInfo
        {
            public EpgDataViewInfo(EpgDataView epgview) { epgView = epgview ?? new EpgDataView(); }
            EpgDataView epgView = null;
            public bool IsSelected { get { return epgView.IsSelected; } }
            public Grid grid { get { return epgView.grid_tab; } }
            public bool OnCreateTab { get { return epgView.OnCreateTab; } }
            public CustomEpgTabInfo TabInfo(string Uid) { return epgView.get_tabInfo(Uid); }
            public Action<int> viewSettingClick { get { return epgView.epgView_ViewSettingClick; } }
            public void TabItemsChanged() { ViewUtil.TabControlHeaderCopy(epgView.tabControl, epgView.dummyTab); }
        }
    }

    //TabItemとして振る舞うが、実際の表示は別途用意したGridを使用する
    public class EpgTabItem : TabItem
    {
        //共有データ。
        EpgDataView.EpgDataViewInfo epgView;
        EpgViewData viewData = new EpgViewData();
        public bool IsEpgLoaded { get { return viewData.IsEpgLoaded; } }
        public bool HasKey(UInt64 key) { return viewData.HasKey(key); }
        public CustomEpgTabInfo Info
        {
            get { return viewData.EpgTabInfo; }
            private set
            {
                viewData.EpgTabInfo = value.DeepClone();
                CustomEpgTabInfo org = epgView.TabInfo(value.Uid);
                org = org ?? value;
                base.Header = org.TabName;
                base.Tag = MenuUtil.DeleteAccessKey(org.TabName);
                base.Uid = org.Uid;
            }
        }

        //一度作った番組表の描画をストックしておく。
        private Grid grid = new Grid { Visibility = Visibility.Hidden };
        public EpgViewBase view { get { EpgViewBase v; vItems.TryGetValue(Info.ViewMode, out v); return v; } }
        private Dictionary<int, EpgViewBase> vItems = new Dictionary<int, EpgViewBase>();
        private List<EpgViewState> vStates = new List<EpgViewState> { null, null, null };

        public EpgTabItem() { }
        public EpgTabItem(CustomEpgTabInfo setInfo, EpgDataView epgview, string selectID = null, EpgViewState state = null)
        {
            //この番組表の表示エリアを登録
            epgView = new EpgDataView.EpgDataViewInfo(epgview);
            epgView.grid.Children.Add(grid);
            viewData.ViewSettingClick += epgView.viewSettingClick;

            //番組情報を登録
            Info = setInfo;
            if (state != null) Info.ViewMode = state.viewMode;
            if (state != null) vStates[state.viewMode] = state;

            if (base.Uid == selectID) IsSelected = true;
        }

        //更新関係。IsDisplay()によりタスクアイコン時でも表示しているものを更新する。
        private bool IsDisplay(int v) { return epgView.IsSelected == true && IsSelected == true && v == Info.ViewMode; }
        public void SaveViewData() { foreach (var v in vItems.Values) v.SaveViewData(); }//これは非表示でも実行
        public void UpdateMenu(bool refresh = true) { foreach (var v in vItems.Values) v.UpdateMenu(refresh); }
        public void UpdateReserveInfo() { foreach (var v in vItems) v.Value.UpdateReserveInfo(IsDisplay(v.Key)); }

        //更新時の動作
        //PrebuildEpg==false → 表示中の番組のみ構築
        //PrebuildEpg==true → 選択中の番組表のみ構築し、検索用に各番組表を内部に作成する
        //PrebuildEpgAll==true → 全番組表を構築する。表示モード切替タブが表示の場合は、全表示モード分を全て構築する
        private bool isStockView(int v, bool clear = false)
        {
            //細かく状態を保存・復元できれば、Clear時は全再構築でもいいが、今は少なくとも表示/選択中のものは保持する。
            return IsDisplay(v) == true || Settings.Instance.PrebuildEpg == true && (IsSelected == true && v == Info.ViewMode ||
                !clear && (v == Info.ViewMode || Settings.Instance.PrebuildEpgAll == true && Settings.Instance.EpgViewModeTabEnabled == true));
        }
        public void CleanUpContent(bool clear = false)
        {
            //ReloadInfo()後に再セットする。
            grid.Children.Clear();

            //現在表示されているものまたは事前構築に該当しているもの以外は表示状態を残して破棄
            vItems.Where(v => isStockView(v.Key, clear) == false).ToList().ForEach(v =>
            {
                v.Value.SaveViewData();
                EpgViewState state = v.Value.GetViewState();
                if (state != null) vStates[state.viewMode] = state;
                vItems.Remove(v.Key);
            });
        }
        public void SetContent(bool reloadInfo = false)
        {
            for (int mode = 0; mode < 3; mode++)
            {
                if (vItems.ContainsKey(mode) == false && isStockView(mode) == true)
                {
                    switch (mode)
                    {
                        case 1://1週間表示
                            vItems[mode] = new EpgWeekMainView();
                            break;
                        case 2://リスト表示
                            vItems[mode] = new EpgListMainView();
                            break;
                        default://標準ラテ欄表示
                            vItems[mode] = new EpgMainView();
                            break;
                    }
                    vItems[mode].SetViewData(viewData, mode);
                    vItems[mode].SetViewState(vStates[mode]);
                    vStates[mode] = null;
                    if (reloadInfo == false) vItems[mode].UpdateInfo();
                }
            }
            if (reloadInfo == true)
            {
                CleanUpContent();
                viewData.ClearEventList();
                foreach (var v in vItems) v.Value.UpdateInfo();
            }

            //ストックするものとRenderさせるものは異なる。PrebuildEpgAllのときだけ全てのRenderを走らせる。
            //この際タブの表示オプションに応じて選択する必要があるが、実際のRender(UpdateInfo())時には
            //不要なものは排除されているので細かく考慮する必要が無い。(PrebuildEpg==false時も同様)
            foreach (var v in vItems.Values)
            {
                v.Visibility = v == view ? Visibility.Visible : Visibility.Hidden;
                if (Settings.Instance.PrebuildEpgAll == true || IsSelected == true && v == view)
                {
                    if (grid.Children.Contains(v) == false)
                    {
                        grid.Children.Add(v);
                    }
                }
            }
        }
        //表示切り替えに対する処理
        public void ChangeContent(CustomEpgTabInfo setInfo = null, int mode = -1)
        {
            if (setInfo != null) Info = setInfo;
            if (mode >= 0) Info.ViewMode = mode;

            //表示中のものはデータを保存する
            if (view != null) view.SaveViewData();

            SetContent(setInfo != null);
        }

        //番組表の選択に対する処理。
        protected override void OnSelected(RoutedEventArgs e)
        {
            grid.Visibility = Visibility.Visible;
            base.OnSelected(e);
            if (epgView.OnCreateTab == false) SetContent();
        }
        protected override void OnUnselected(RoutedEventArgs e)
        {
            grid.Visibility = Visibility.Hidden;
            base.OnUnselected(e);
            if (epgView.OnCreateTab == false && view != null) view.SaveViewData();
        }
        //削除されたとき
        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            if (Parent == null)
            {
                epgView.grid.Children.Remove(grid);
                grid.Children.Clear();//無くても問題無いが、あると割とすぐGC.Collect()に乗っかる。
            }

            //EPGビューの高さ調整用
            epgView.TabItemsChanged();

            base.OnVisualParentChanged(oldParent);
        }
    }
}
