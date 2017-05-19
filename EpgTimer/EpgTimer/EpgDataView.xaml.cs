using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace EpgTimer
{
    /// <summary>
    /// EpgView.xaml の相互作用ロジック
    /// </summary>
    public partial class EpgDataView : EpgTimer.UserCtrlView.DataViewBase
    {
        private List<CustomEpgTabInfo> tabInfo = new List<CustomEpgTabInfo>();//Settingデータの参照を保持

        public EpgDataView()
        {
            InitializeComponent();
            base.noStatus = true;

            tabControl.ContextMenu = new ContextMenu();
            tabControl.ContextMenuOpening += new ContextMenuEventHandler(TabContextMenuOpening);
            tabControl.ContextMenu.Opened += new RoutedEventHandler(TabContextMenuOpened);
            tabControl.ContextMenu.Unloaded += new RoutedEventHandler((sender, e) => ClearTabHeader());
        }

        private List<EpgDataViewItem> Views
        {
            get { return tabControl.Items.OfType<TabItem>().Select(item => item.Content).OfType<EpgDataViewItem>().ToList(); }
        }

        //メニューの更新
        public void RefreshMenu()
        {
            this.Views.ForEach(view => view.RefreshMenu());
        }

        public void SaveViewData()
        {
            //存在しないときは、本当に無いか、破棄されて保存済み
            this.Views.ForEach(view => view.SaveViewData());
        }

        /// <summary>EPGデータの再描画</summary>
        protected override bool ReloadInfoData()
        {
            //タブが無ければ生成、あれば更新
            if (tabControl.Items.Count == 0)
            {
                return CreateTabItem();
            }

            this.Views.ForEach(view => view.UpdateInfo());
            return true;
        }

        /// <summary>予約情報の更新通知</summary>
        public void UpdateReserveInfo(bool reload = true)
        {
            this.Views.ForEach(view => view.UpdateReserveInfo(reload));
        }

        CustomEpgTabInfo oldInfo = null;
        object oldState = null;
        string oldID = null;
        /// <summary>設定の更新通知</summary>
        public void UpdateSetting(bool noRestoreState = false)
        {
            try
            {
                SaveViewData();

                //表示していた番組表の情報を保存
                var item = tabControl.SelectedItem as TabItem;
                if (item != null)
                {
                    if (noRestoreState == false)
                    {
                        var view = item.Content as EpgDataViewItem;
                        oldInfo = view.GetViewMode();
                        oldState = view.GetViewState();
                    }
                    oldID = item.Uid;
                }

                //一度全部削除して作り直す。
                //保存情報は次回のタブ作成時に復元する。
                tabInfo.Clear();
                tabControl.Items.Clear();
                UpdateInfo();
            }
            catch (Exception ex) { CommonUtil.DispatcherMsgBoxShow(ex.Message + "\r\n" + ex.StackTrace); }

            //UpdateInfo()は非表示の時走らない。
            //データはここでクリアしてしまうので、現に表示されているもの以外は表示状態はリセットされる。
            //ただし、番組表(oldID)の選択そのものは保持する。
            oldInfo = null;
            oldState = null;
        }

        /// <summary>タブ生成</summary>
        private bool CreateTabItem()
        {
            try
            {
                tabInfo = Settings.Instance.UseCustomEpgView == false ?
                    CommonManager.Instance.CreateDefaultTabInfo() : Settings.Instance.CustomEpgTabList.ToList();

                foreach (CustomEpgTabInfo info in tabInfo.Where(info => info.IsVisible == true))
                {
                    //とりあえず同じIDを探して表示してみる(中身は別物になってるかもしれないが、とりあえず表示を試みる)。
                    //標準・カスタム切り替えの際は、標準番組表が負のIDを与えられているので、このコードは走らない。
                    object state = null;
                    CustomEpgTabInfo setInfo = info;
                    if (info.Uid == oldID && oldInfo != null)
                    {
                        setInfo = info.Clone();
                        setInfo.ViewMode = oldInfo.ViewMode;
                        state = oldState;
                    }

                    tabControl.Items.Add(CreateTab(setInfo, oldID, state));
                }
                if (tabControl.SelectedIndex == -1)
                {
                    tabControl.SelectedIndex = 0;
                }
            }
            catch (Exception ex) { CommonUtil.DispatcherMsgBoxShow(ex.Message + "\r\n" + ex.StackTrace); }

            oldID = null;
            return true;
        }

        private TabItem CreateTab(CustomEpgTabInfo info, string selectID = null, object state = null)
        {
            var epgView = new EpgDataViewItem();
            epgView.ViewSettingClick += new EpgView.ViewSettingClickHandler(epgView_ViewSettingClick);
            epgView.SetViewMode(info, state);

            var tabItem = new TabItem();
            tabItem.Header = info.TabName;
            tabItem.Tag = info.TabName;
            tabItem.Content = epgView;
            tabItem.Uid = info.Uid;
            if (tabItem.Uid == selectID) tabItem.IsSelected = true;

            return tabItem;
        }

        private void epgView_ViewSettingClick(object sender, CustomEpgTabInfo info)
        {
            try
            {
                var item = sender as EpgDataViewItem;

                //表示設定変更ダイアログから
                if (info == null)
                {
                    var dlg = new EpgDataViewSettingWindow(item.GetViewMode());
                    dlg.Owner = CommonUtil.GetTopWindow(this);
                    dlg.SetTryMode(Settings.Instance.UseCustomEpgView == false);
                    if (dlg.ShowDialog() == false)
                    { return; }

                    info = dlg.GetSetting();
                    TabItem tab = tabControl.Items.OfType<TabItem>().FirstOrDefault(ti => ti.Uid == info.Uid);
                    if (tab == null) return;

                    //設定の保存。ちゃんとチェックするならダイアログ前後でGetTabInfo()をシリアライズだが、まず無い。
                    if (Settings.Instance.UseCustomEpgView == true && Settings.Instance.TryEpgSetting == false
                        && info.ID >= 0 && info.ID < tabInfo.Count && info.ID < Settings.Instance.CustomEpgTabList.Count)
                    {
                        tabInfo[info.ID] = info;
                        Settings.Instance.CustomEpgTabList[info.ID] = info;
                        Settings.SaveToXmlFile();
                    }

                    if (info.IsVisible == false)
                    {
                        tabControl.Items.Remove(tab);
                        return;
                    }
                    tab.Header = info.TabName;
                    tab.Tag = info.TabName;
                }
                else if (info.ViewMode == -1)
                {
                    info = tabInfo.Find(tinfo => tinfo.ID == info.ID);
                    if (info == null) return;
                }

                item.SetViewMode(info);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        protected override void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.UserControl_IsVisibleChanged(sender, e);
            if (this.IsVisible == true) this.searchJumpTargetProgram();//EPG更新後に探す
        }

        /// <summary>予約一覧からのジャンプ先を番組表タブから探す</summary>
        void searchJumpTargetProgram()
        {
            try
            {
                UInt64 serviceKey_Target1 = BlackoutWindow.Create64Key();
                if (serviceKey_Target1 == 0) return;

                foreach (TabItem tabItem1 in tabControl.Items)
                {
                    var epgView1 = tabItem1.Content as EpgDataViewItem;
                    foreach (UInt64 serviceKey_OnTab1 in epgView1.GetViewMode().ViewServiceList)
                    {
                        if (serviceKey_Target1 == serviceKey_OnTab1)
                        {
                            tabItem1.IsSelected = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        //番組表ヘッダ用のコンテキストメニュー関係
        private enum edvCmds { Setting, ResetAll, All, /*Delete,*/ DeleteAll, ModeChange, VisibleChange }
        private void TabContextMenuOpened(object sender, RoutedEventArgs e)
        {
            //チラつき防止関係。なお、ContextMenuOpeningだとチラつかないが、ListViewのと競合する。
            tabControl.ContextMenu.IsOpen = tabControl.ContextMenu.Visibility == Visibility.Visible;
        }
        private void TabContextMenuOpening(object sender, RoutedEventArgs e)
        {
            try
            {
                ContextMenu ctxm = tabControl.ContextMenu;
                ctxm.Visibility = Visibility.Visible;
                ctxm.Items.Clear();

                //ヘッダでのオープンかどうか判定。TabControlに持たせているのでPlacementTargetは使えない。
                var trg = tabControl.GetPlacementItem() as TabItem ?? new TabItem() { Tag = "(番組表)" };
                if (trg.Uid == "" && tabControl.Items.Count != 0)
                {
                    //番組表が一つもないとき以外は表示しない
                    ctxm.Visibility = Visibility.Hidden;
                    return;
                }

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
                //ビューモードサブメニュー
                var menu_vs = new MenuItem { Header = trg.Tag + " の表示モード(_V)", IsEnabled = trg.Uid != "", Uid = trg.Uid };
                tabMenuAdd(menu_vs, true, EpgCmds.ViewChgSet, "表示設定...(_S)", trg.Uid);
                tabMenuAdd(menu_vs, true, EpgCmds.ViewChgReSet, "一時的な変更をクリア(_R)", trg.Uid);
                menu_vs.Items.Add(new Separator());
                for (int i = 0; i <= 2; i++)
                {
                    menu1 = tabMenuAdd(menu_vs, true, EpgCmds.ViewChgMode, CommonManager.ConvertViewModeText(i) + string.Format(" (_{0})", i + 1), trg.Uid);
                    menu1.CommandParameter = new EpgCmdParam(null, 0, i);//コマンド自体は、menuの処理メソッドから走らせる。
                    menu1.IsChecked = trg.Uid == "" ? false : i == (trg.Content as EpgDataViewItem).GetViewMode().ViewMode;
                }

                //番組表の操作メニュー
                var menu_tb = new MenuItem { Header = "番組表の操作(_E)" };
                menu1 = tabMenuAdd(menu_tb, true, edvCmds.ModeChange, (Settings.Instance.UseCustomEpgView == true ? "デフォルト" : "カスタマイズ") + "表示に切り替え(_M)", "");
                menu1.ToolTip = "現在の表示 : " + (Settings.Instance.UseCustomEpgView == false ? "デフォルト" : "カスタマイズ") + "表示";
                menu_tb.Items.Add(new Separator());
                tabMenuAdd(menu_tb, tabInfo.Any(item => item.IsVisible == true) || Settings.Instance.UseCustomEpgView == false, edvCmds.ResetAll, "一時的な変更を全てクリア(_R)", "");
                tabMenuAdd(menu_tb, tabInfo.Any(item => item.IsVisible == false), edvCmds.All, "全て表示(_A)", "");
                tabMenuAdd(menu_tb, tabInfo.Any(item => item.IsVisible == true), edvCmds.DeleteAll, "全て非表示(_H)", "");

                ctxm.Items.Add(menu_vs);
                ctxm.Items.Add(menu_tb);
                tabMenuAdd(ctxm, true, edvCmds.Setting, "番組表の設定...(_O)", trg.Uid);
                //ctxm.Items.Add(new Separator());
                //tabMenuAdd(ctxm, trg.Uid != "", edvCmds.Delete, trg.Tag + " を非表示(_D)", trg.Uid);
                ctxm.Items.Add(new Separator());

                if (tabInfo.Count == 0)
                {
                    ctxm.Items.Add(new MenuItem { Header = "(番組表の設定がありません)", IsEnabled = false });
                }

                //番組表タブの項目追加。表示されているものにチェックを入れる。
                tabInfo.ForEach(info =>
                {
                    menu1 = tabMenuAdd(ctxm, true, edvCmds.VisibleChange, info.TabName, info.Uid);
                    menu1.IsChecked = info.IsVisible;
                    if (trg.Uid == info.Uid)
                    {
                        menu1.Header = new TextBlock() { Text = info.TabName as string, FontWeight = FontWeights.Bold }; ;
                    }
                });

                ClearTabHeader();
                if (trg.Uid != "")
                {
                    trg.Header = new TextBlock() { Text = trg.Header as string, Foreground = Brushes.Red };
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        private void ClearTabHeader()
        {
            foreach (TabItem tab in tabControl.Items) tab.Header = tab.Tag;
        }

        private void MenuCmdsExecute(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectID = null;
                var menu = sender as MenuItem;

                if (menu.Tag is RoutedUICommand)
                {
                    TabItem tab = tabControl.Items.OfType<TabItem>().FirstOrDefault(ti => ti.Uid == menu.Uid);
                    if (tab != null)
                    {
                        (menu.Tag as RoutedUICommand).Execute(menu.CommandParameter, (tab.Content as EpgDataViewItem).viewCtrl);
                    }
                    return;
                }
                switch (menu.Tag as edvCmds?)
                {
                    case edvCmds.Setting:
                        ViewUtil.MainWindow.OpenSettingDialog(SettingWindow.SettingMode.EpgSetting, menu.Uid);
                        return;
                    case edvCmds.ResetAll:
                        this.UpdateSetting(true);
                        return;
                    case edvCmds.ModeChange:
                        Settings.Instance.UseCustomEpgView = !Settings.Instance.UseCustomEpgView;
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
                        var info = tabInfo.Find(ti => ti.Uid == menu.Uid);
                        if (info != null) info.IsVisible = !info.IsVisible;
                        break;
                }

                int pos = 0;
                foreach (var info in tabInfo)
                {
                    TabItem tab = tabControl.Items.OfType<TabItem>().FirstOrDefault(ti => ti.Uid == info.Uid);
                    if (info.IsVisible == false)
                    {
                        tabControl.Items.Remove(tab);
                    }
                    else
                    {
                        if (tab == null)
                        {
                            tabControl.Items.Insert(pos, CreateTab(info, selectID));
                        }
                        pos++;
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
    }
}
