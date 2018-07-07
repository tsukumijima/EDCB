using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// SearchWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class InfoSearchWindow : InfoSearchWindowBase
    {
        private InfoSearchSettingData SearchInfo { get { return (InfoSearchSettingData)DataContext; } }

        private ListViewController<InfoSearchItem> lstCtrl;
        private CmdExe<InfoSearchItem> mc;

        private static Dictionary<Type, CtxmCode> dic_Type_Code = new Dictionary<Type, CtxmCode>
        {
            { typeof(InfoSearchItem), CtxmCode.InfoSearchWindow},
            { typeof(ReserveData), CtxmCode.ReserveView},
            { typeof(RecFileInfo), CtxmCode.RecInfoView},
            { typeof(EpgAutoAddData), CtxmCode.EpgAutoAddView},
            { typeof(ManualAutoAddData), CtxmCode.ManualAutoAddView}
        };
        private Dictionary<Type, CmdExeBase> dic_mc = new Dictionary<Type, CmdExeBase>();
        private Dictionary<Type, MenuBinds> dic_mBinds = new Dictionary<Type, MenuBinds>();
        private Type selectedType = typeof(InfoSearchItem);

        private bool startSearch = false;
        private bool noSearchSettingSave = false;

        public InfoSearchWindow(string searchWord = null) { InitWindow(null, searchWord); }
        public InfoSearchWindow(InfoSearchSettingData searchInfo, bool noSave = false) { InitWindow(searchInfo, null, noSave); }
        public void InitWindow(InfoSearchSettingData searchInfo, string searchWord = null, bool noSave = false)
        {
            InitializeComponent();

            try
            {
                buttonID = "予約情報検索";
                base.SetParam(false, checkBox_windowPinned);

                //リストビュー関連の設定
                var list_columns = Resources["ReserveItemViewColumns"] as GridViewColumnList;
                list_columns.AddRange(Resources["RecSettingViewColumns"] as GridViewColumnList);
                list_columns.RenameHeader("StartTime", "日時/次の予約");
                list_columns.RenameHeader("EventName", "番組名/ANDキーワード");
                list_columns.RenameHeader("ProgramContent", "番組内容/その他情報");
                list_columns.RenameHeader("IsEnabled", "有効・プロテクト", "有効・無効/プロテクト切替え");
                list_columns.RenameHeader("Comment", "予約/録画状況");
                list_columns.RenameHeader("RecFileName", "予定/録画ファイル名");

                lstCtrl = new ListViewController<InfoSearchItem>(this);
                lstCtrl.SetSavePath(CommonUtil.NameOf(() => Settings.Instance.InfoSearchWndColumn)
                    , CommonUtil.NameOf(() => Settings.Instance.InfoSearchColumnHead)
                    , CommonUtil.NameOf(() => Settings.Instance.InfoSearchSortDirection));
                lstCtrl.SetViewSetting(listView_result, gridView_result, true, true, list_columns);
                lstCtrl.SetSelectedItemDoubleClick((sender, e) =>
                {
                    var cmd = (selectedType == typeof(RecFileInfo) && Settings.Instance.PlayDClick == true) ? EpgCmds.Play : EpgCmds.ShowDialog;
                    cmd.Execute(sender, listView_result);
                });

                //ステータス変更の設定
                lstCtrl.SetSelectionChangedEventHandler((sender, e) => this.UpdateStatus(1));

                //最初にコマンド集の初期化
                mc = new CmdExe<InfoSearchItem>(this);
                mc.SetFuncGetDataList(isAll => isAll == true ? lstCtrl.dataList : lstCtrl.GetSelectedItemsList());
                mc.SetFuncSelectSingleData((noChange) => lstCtrl.SelectSingleItem(noChange));
                mc.SetFuncReleaseSelectedData(() => listView_result.UnselectAll());
                
                //コマンド集に無いもの
                mc.AddReplaceCommand(EpgCmds.JumpListView, mc_JumpListView);
                mc.AddReplaceCommand(EpgCmds.ReSearch, mc_ReSearch);
                mc.AddReplaceCommand(EpgCmds.ReSearch2, mc_ReSearch);
                mc.AddReplaceCommand(EpgCmds.Search, mc_Search);
                mc.AddReplaceCommand(EpgCmds.Cancel, (sender, e) => this.Close());
                mc.AddReplaceCommand(EpgCmds.ChgOnOffCheck, (sender, e) => lstCtrl.ChgOnOffFromCheckbox(e.Parameter, EpgCmds.ChgOnOff));

                //コマンド集を振り替えるもの
                mc.AddReplaceCommand(EpgCmds.ShowDialog, mc_ShowDialog);//Enterキーからの実行が無ければ省略できる
                mc.AddReplaceCommand(EpgCmds.ChgOnOff, mc_ChgOnOff);
                mc.AddReplaceCommand(EpgCmds.Delete, mc_Delete);

                //ボタンの設定
                mBinds.View = CtxmCode.InfoSearchWindow;
                mBinds.SetCommandToButton(button_search, EpgCmds.Search);
                mBinds.AddInputCommand(EpgCmds.Cancel);//ショートカット登録

                //コンテキストメニューを開く時の設定
                listView_result.ContextMenu.Opened += (sender, e) => dic_mc[selectedType].SupportContextMenuLoading(sender, e);

                //タイプごとの個別コマンド処理用データの設定
                dic_mc.Add(typeof(InfoSearchItem), mc);
                dic_mc.Add(typeof(ReserveData), new CmdExeReserve(this));
                dic_mc.Add(typeof(RecFileInfo), new CmdExeRecinfo(this));
                dic_mc.Add(typeof(EpgAutoAddData), new CmdExeEpgAutoAdd(this));
                dic_mc.Add(typeof(ManualAutoAddData), new CmdExeManualAutoAdd(this));
                foreach (var data in dic_mc.Values.Skip(1))
                {
                    data.SetFuncGetDataList(isAll =>
                    {
                        return (isAll == true ? lstCtrl.dataList : lstCtrl.GetSelectedItemsList()).Select(d => d.Data);
                    });
                    data.SetFuncSelectSingleData(noChange =>
                    {
                        InfoSearchItem item = lstCtrl.SelectSingleItem(noChange);
                        return item == null ? null : item.Data;
                    });
                    data.SetFuncReleaseSelectedData(() => listView_result.UnselectAll());
                    data.AddReplaceCommand(EpgCmds.ChgOnOff, mc_ChgOnOff);
                    data.AddReplaceCommand(EpgCmds.JumpReserve, (sender, e) => mc_JumpTab(CtxmCode.ReserveView));
                    data.AddReplaceCommand(EpgCmds.JumpTuner, (sender, e) => mc_JumpTab(CtxmCode.TunerReserveView));
                    data.AddReplaceCommand(EpgCmds.JumpTable, (sender, e) => mc_JumpTab(CtxmCode.EpgView));
                }

                //タイプごとのショートカット情報を登録
                dic_mBinds.Add(typeof(InfoSearchItem), mBinds);
                dic_mBinds.Add(typeof(ReserveData), new MenuBinds { View = CtxmCode.ReserveView });
                dic_mBinds.Add(typeof(RecFileInfo), new MenuBinds { View = CtxmCode.RecInfoView });
                dic_mBinds.Add(typeof(EpgAutoAddData), new MenuBinds { View = CtxmCode.EpgAutoAddView });
                dic_mBinds.Add(typeof(ManualAutoAddData), new MenuBinds { View = CtxmCode.ManualAutoAddView });

                //メニューの作成、ショートカットの登録
                this.RefreshMenu();

                //選択状態に合わせてコマンドなどをセットするようにする。
                //lstCtrl.SetSelectionChangedEventHandler()は遅延実行なので使わない。
                this.listView_result.SelectionChanged += (sender, e) => ResetMenu();

                //その他設定
                checkBox_ShowToolTip.IsChecked = Settings.Instance.InfoSearchItemTooltip;
                DataContext = (searchInfo ?? Settings.Instance.InfoSearchData).DeepCloneObj();
                if (searchWord != null) SearchInfo.SearchWord = searchWord;
                startSearch = searchInfo != null || searchWord != null;
                noSearchSettingSave = noSave;

                //ステータスバーの登録
                StatusManager.RegisterStatusbar(this.statusBar, this);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public override void RefreshMenu() { ResetMenu(true); }
        public void ResetMenu(bool forceReset = false)
        {
            foreach (var mc in dic_mc.Values) mc.EpgInfoOpenMode = Settings.Instance.SearchEpgInfoOpenMode;
            var list = lstCtrl.GetSelectedItemsList().Select(item => item.Data).OfType<IRecWorkMainData>().ToList();
            bool isSame = list.All(item => list[0].GetType() == item.GetType());
            Type changedType = (list.Count == 0 || isSame == false) ? typeof(InfoSearchItem) : list[0].GetType();
            if (forceReset == true || selectedType != changedType)
            {
                //InfoSearch専用のコマンド・ショートカットはそのままにしておく or 上書きされずそのままになる
                dic_mc[changedType].ResetCommandBindings(this, listView_result.ContextMenu);
                if (selectedType != typeof(InfoSearchItem))
                {
                    dic_mBinds[selectedType].DeleteInputBindings(this, listView_result);
                }
                dic_mBinds[changedType].ResetInputBindings(this, listView_result);
                mm.CtxmGenerateContextMenuInfoSearch(listView_result.ContextMenu, dic_Type_Code[changedType]);
                selectedType = changedType;//最後
            }
        }

        private void mc_Search(object sender, ExecutedRoutedEventArgs e)
        {
            Search();
            StatusManager.StatusNotifySet(true, "検索を実行");
        }
        private void Search()
        {
            try
            {
                lstCtrl.ReloadInfoData(dataList =>
                {
                    var targetItems = new List<IRecWorkMainData>();
                    if (SearchInfo.ReserveInfo == true)
                    {
                        targetItems.AddRange(CommonManager.Instance.DB.ReserveList.Values);
                    }
                    if (SearchInfo.RecInfo == true)
                    {
                        //起動直後は読み込んでない場合がある。
                        CommonManager.Instance.DB.ReloadRecFileInfo();

                        //詳細情報が必要な場合はあらかじめ読込んでおく。
                        if (SearchInfo.TitleOnly == false)
                        {
                            CommonManager.Instance.DB.ReadRecFileAppend();
                        }

                        targetItems.AddRange(CommonManager.Instance.DB.RecFileInfo.Values);
                    }
                    if (SearchInfo.EpgAutoAddInfo == true)
                    {
                        targetItems.AddRange(CommonManager.Instance.DB.EpgAutoAddList.Values);
                    }
                    if (SearchInfo.ManualAutoAddInfo == true)
                    {
                        targetItems.AddRange(CommonManager.Instance.DB.ManualAutoAddList.Values);
                    }

                    string sText = CommonManager.AdjustSearchText(SearchInfo.SearchWord);

                    if (SearchInfo.RegExp == true)
                    {
                        dataList.AddRange(InfoSearchItem.Items(targetItems).Where(data =>
                        {
                            string trgText = data.GetSearchText(SearchInfo.TitleOnly);
                            trgText = CommonManager.ReplaceText(trgText, CommonManager.ReplaceUrlDictionary).ToLower();
                            return Regex.Match(trgText, sText).Success;
                        }));
                    }
                    else
                    {
                        string[] sWords = sText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        dataList.AddRange(InfoSearchItem.Items(targetItems).Where(data =>
                        {
                            //検索ワードで対象のタイトルまたは詳細をAND検索
                            string trgText = data.GetSearchText(SearchInfo.TitleOnly);
                            trgText = CommonManager.AdjustSearchText(trgText).Replace(" ", "");
                            if (sWords.All(word => trgText.Contains(word)) == true)
                            {
                                return true;
                            }

                            //キーワード予約を考慮し、逆に対象のタイトルで検索ワードのAND検索もしておく。
                            string[] trgWords = CommonManager.AdjustSearchText(data.DataTitle).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            return trgWords.Any() == true && trgWords.All(word => sText.Contains(word));
                        }));
                    }

                    ToolTipCheck();
                    return true;
                });

                this.Title = ViewUtil.WindowTitleText(SearchInfo.SearchWord, "予約情報検索");
                UpdateStatus();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        private void UpdateStatus(int mode = 0)
        {
            string s1 = null;
            string s2 = "";
            if (mode == 0) s1 = ViewUtil.ConvertInfoSearchItemStatus(lstCtrl.dataList, "検索数");
            if (Settings.Instance.DisplayStatus == true)
            {
                List<InfoSearchItem> sList = lstCtrl.GetSelectedItemsList();
                s2 = sList.Count == 0 ? "" : ViewUtil.ConvertInfoSearchItemStatus(sList, "　選択中");
            }
            this.statusBar.SetText(s1, s2);
        }
        private void checkBox_ShowToolTip_Checked(object sender, RoutedEventArgs e)
        {
            ToolTipCheck();
            listView_result.Items.Refresh();
        }
        private void ToolTipCheck()
        {
            lstCtrl.dataList.ForEach(item => item.IsToolTipEnabled = checkBox_ShowToolTip.IsChecked == true);

            //ツールチップに番組情報を表示する場合は先に一括で詳細情報を読込んでおく
            if (checkBox_ShowToolTip.IsChecked == true && Settings.Instance.RecInfoToolTipMode == 1)
            {
                var list = lstCtrl.dataList.Select(item => item.Data).OfType<RecFileInfo>();
                CommonManager.Instance.DB.ReadRecFileAppend(list);
            }
        }

        private void mc_ShowDialog(object sender, ExecutedRoutedEventArgs e)
        {
            lstCtrl.SelectSingleItem();//Selection_Changeが走ってRefreshMenuが実行される。
            if (selectedType != typeof(InfoSearchItem))
            {
                EpgCmds.ShowDialog.Execute(e.Parameter, null);
            }
        }
        private void mc_ChgOnOff(object sender, ExecutedRoutedEventArgs e)
        {
            if (listView_result.SelectedItem == null) return;
            //
            List<IRecWorkMainData> dataList = lstCtrl.GetSelectedItemsList().Select(data => data.Data).ToList();

            if (MenuUtil.CautionManyMessage(dataList.Count, "変更の確認") == false)
            { return; }

            MenuUtil.ReserveChangeOnOff(dataList.OfType<ReserveData>().DeepClone(), null, false);
            MenuUtil.RecinfoChgProtect(dataList.OfType<RecFileInfo>().DeepClone(), false);
            MenuUtil.AutoAddChangeKeyEnabled(dataList.OfType<AutoAddData>().DeepClone());

            StatusManager.StatusNotifySet(true, mc.GetCmdMessageFormat("状態切替を実行", dataList.Count));
        }
        private void mc_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (listView_result.SelectedItem == null) return;
            //
            List<IRecWorkMainData> dataList = lstCtrl.GetSelectedItemsList().Select(data => data.Data).ToList();
            dataList.RemoveAll(d => d is RecFileInfo && (d as RecFileInfo).ProtectFlag != 0);

            if (CmdExeUtil.CheckDeleteCancel(e, dataList.Select(data => data.DataTitle).ToList()) == true)
            { return; }

            if (CmdExeRecinfo.mcs_DeleteCheckDelFile(dataList.OfType<RecFileInfo>()) == false)
            { return; }

            if (MenuUtil.CautionManyMessage(dataList.Count, "削除の確認") == false)
            { return; }

            MenuUtil.ReserveDelete(dataList.OfType<ReserveData>().ToList(), false);
            MenuUtil.RecinfoDelete(dataList.OfType<RecFileInfo>().ToList(), false);
            MenuUtil.AutoAddDelete(dataList.OfType<AutoAddData>().ToList(), false);

            StatusManager.StatusNotifySet(true, mc.GetCmdMessageFormat("削除を実行", dataList.Count));
        }
        private void mc_JumpTab(CtxmCode trg_code)
        {
            lstCtrl.SelectSingleItem();
            JumpTabAndHide(trg_code, dic_mc[selectedType].GetJumpTabItem(trg_code));
        }
        private void mc_JumpListView(object sender, ExecutedRoutedEventArgs e)
        {
            InfoSearchItem vItem = lstCtrl.SelectSingleItem();//Selection_Changeが走ってRefreshMenuが実行される。
            JumpTabAndHide(dic_Type_Code[selectedType], vItem == null ? null : vItem.ViewItem);
        }
        private void mc_ReSearch(object sender, ExecutedRoutedEventArgs e)
        {
            if (listView_result.SelectedItem == null) return;
            //
            string word = MenuUtil.TrimEpgKeyword(lstCtrl.SelectSingleItem().EventName, CmdExeUtil.IsKeyGesture(e));
            if (e.Command == EpgCmds.ReSearch)
            {
                TextBox_SearchWord.Text = word;
                mc_Search(sender, e);
            }
            else
            {
                WriteWindowSaveData();
                InfoSearchSettingData data = SearchInfo.DeepClone();
                data.SearchWord = word;
                new InfoSearchWindow(data, noSearchSettingSave).Show();
            }
        }

        protected override void WriteWindowSaveData()
        {
            if (noSearchSettingSave == false) Settings.Instance.InfoSearchData = SearchInfo.DeepClone();
            Settings.Instance.InfoSearchItemTooltip = checkBox_ShowToolTip.IsChecked == true;
            lstCtrl.SaveViewDataToSettings();

            base.WriteWindowSaveData();
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBox_SearchWord.Focus();
            if (startSearch == true)
            {
                Search();
            }
        }

        protected override bool ReloadInfoData()
        {
            Search();
            return true;
        }
    }

    //ジェネリックパラメータTはstatic関係の分割用なので何でもいい
    public class InfoSearchWindowBase : HideableWindow<InfoSearchWindow> { }
}
