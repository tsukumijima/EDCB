using System;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace EpgTimer
{
    using EpgView;

    /// <summary>
    /// AddReserveEpgWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AddReserveEpgWindow : AddReserveEpgWindowBase
    {
        private EpgEventInfo eventInfo = null;
        private readonly bool KeepWin = Settings.Instance.KeepReserveWindow;//固定する
        private bool AddEnabled { get { return eventInfo != null && eventInfo.IsReservable == true; } }
        private bool chgEnabled = false;
        private string tabStr = "予約";

        static AddReserveEpgWindow()
        {
            //重複予約の変更時の選択継続用(無視するなら不要)
            SearchWindow.ViewReserveUpdated += AddReserveEpgWindow.UpdatesViewSelection;
            EpgViewBase.ViewReserveUpdated += AddReserveEpgWindow.UpdatesViewSelection;
        }
        public AddReserveEpgWindow(EpgEventInfo info = null, int epgInfoOpenMode = 0, RecSettingData setInfo = null)
        {
            InitializeComponent();

            base.SetParam(false, checkBox_windowPinned, checkBox_dataReplace);
            recSettingView.PresetResCompare = true;

            //コマンドの登録
            this.CommandBindings.Add(new CommandBinding(EpgCmds.Cancel, (sender, e) => this.Close()));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.AddInDialog, reserve_add, (sender, e) => e.CanExecute = AddEnabled));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.ChangeInDialog, reserve_chg, (sender, e) => e.CanExecute = KeepWin == true && AddEnabled && chgEnabled));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.DeleteInDialog, reserve_del, (sender, e) => e.CanExecute = KeepWin == true && AddEnabled && chgEnabled));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.BackItem, (sender, e) => MoveViewNextItem(-1), (sender, e) => e.CanExecute = KeepWin == true && (DataView is EpgViewBase || DataViewSearch != null || DataRefList.Any())));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.NextItem, (sender, e) => MoveViewNextItem(1), (sender, e) => e.CanExecute = KeepWin == true && (DataView is EpgViewBase || DataViewSearch != null || DataRefList.Any())));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.Search, (sender, e) => MoveViewEpgTarget(), (sender, e) => e.CanExecute = KeepWin == true && DataView is EpgViewBase));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.SaveTextInDialog, (sender, e) => CommonManager.Save_ProgramText(eventInfo), (sender, e) => e.CanExecute = eventInfo != null));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.ShowInDialog, (sender, e) => MenuUtil.OpenRecInfoDialog(eventInfo.GetRecinfoFromPgUID()), (sender, e) => e.CanExecute = button_open_recinfo.Visibility == Visibility.Visible));

            //ボタンの設定
            mBinds.SetCommandToButton(button_cancel, EpgCmds.Cancel);
            mBinds.SetCommandToButton(button_add_reserve, EpgCmds.AddInDialog);
            mBinds.SetCommandToButton(button_chg_reserve, EpgCmds.ChangeInDialog);
            mBinds.SetCommandToButton(button_del_reserve, EpgCmds.DeleteInDialog);
            mBinds.SetCommandToButton(button_up, EpgCmds.BackItem);
            mBinds.SetCommandToButton(button_down, EpgCmds.NextItem);
            mBinds.SetCommandToButton(button_chk, EpgCmds.Search);
            mBinds.SetCommandToButton(button_save_program, EpgCmds.SaveTextInDialog);
            mBinds.SetCommandToButton(button_open_recinfo, EpgCmds.ShowInDialog);
            RefreshMenu();

            //録画設定タブ関係の設定
            recSettingView.SelectedPresetChanged += SetReserveTabHeader;
            reserveTabHeader.MouseRightButtonUp += recSettingView.OpenPresetSelectMenuOnMouseEvent;
            recSettingView.SetDefSetting(setInfo);

            tabControl.SelectedIndex = epgInfoOpenMode == 1 ? 0 : 1;
            if (KeepWin == true)
            {
                button_cancel.Content = "閉じる";
                //ステータスバーの設定
                this.statusBar.Status.Visibility = Visibility.Collapsed;
                StatusManager.RegisterStatusbar(this.statusBar, this);
            }
            else
            {
                button_chg_reserve.Visibility = Visibility.Collapsed;
                button_del_reserve.Visibility = Visibility.Collapsed;
                button_up.Visibility = Visibility.Collapsed;
                button_down.Visibility = Visibility.Collapsed;
                button_chk.Visibility = Visibility.Collapsed;
            }
            ChangeData(info);
        }

        private bool InfoCheckFlg = false;
        public override void UpdateInfo(bool reload = true)
        {
            InfoCheckFlg = true;
            base.UpdateInfo(reload);
        }
        protected override void ReloadInfo()
        {
            if (InfoCheckFlg == true && this.IsVisible == true && (this.WindowState != WindowState.Minimized || this.IsActive == true))
            {
                //eventInfo更新は必要なときだけ
                if (eventInfo != null)
                {
                    if (ReloadInfoFlg == true)
                    {
                        SetData(MenuUtil.GetPgInfoUidAll(eventInfo.CurrentPgUID()));
                    }
                    else
                    {
                        //予約情報変更の反映のみ実施
                        richTextBox_descInfo.Document = CommonManager.ConvertDisplayText(eventInfo);
                    }
                }
                recSettingView.RefreshView();
                CheckData(false);
                ReloadInfoFlg = false;
                InfoCheckFlg = false;
            }
        }
        public override void ChangeData(object data)
        {
            SetData(data);
            //他のダイアログと異なり、data==nullでも処理を打ち切らない。
            CheckData(true);
        }
        private void SetData(object data)
        {
            var info = data as EpgEventInfo;
            if (data is SearchItem) info = ((SearchItem)data).EventInfo;

            if (info == null) return;

            eventInfo = info;
            Title = ViewUtil.WindowTitleText(eventInfo.DataTitle, "予約登録");
            textBox_info.Text = CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.BasicInfo);
            richTextBox_descInfo.Document = CommonManager.ConvertDisplayText(eventInfo);
            tabStr = eventInfo.IsOver() == true ? "放映終了" : "予約";

            UpdateViewSelection(0);
        }
        private void CheckData(bool recSetChange = true)
        {
            List<ReserveData> list = eventInfo.GetReserveListFromPgUID() ?? new List<ReserveData>();
            chgEnabled = list.Count != 0;
            label_Msg.Visibility = list.Count <= 1 ? Visibility.Hidden : Visibility.Visible;
            button_add_reserve.Content = list.Count == 0 ? "追加" : "重複追加";
            button_open_recinfo.Visibility = eventInfo.GetRecinfoFromPgUID() != null ? Visibility.Visible : Visibility.Collapsed;

            if (chgEnabled == true && recSetChange == true)
            {
                recSettingView.SetDefSetting(list[0].RecSetting);
            }

            SetReserveTabHeader(recSetChange);
        }

        public void SetReserveTabHeader(bool SimpleChanged = true)
        {
            reserveTabHeader.Text = tabStr + recSettingView.GetRecSettingHeaderString(SimpleChanged);
        }

        //proc 0:追加、1:変更、2:削除
        static string[] cmdMsg = new string[] { "追加", "変更", "削除" };
        private void reserve_add(object sender, ExecutedRoutedEventArgs e) { reserve_proc(e, 0); }
        private void reserve_chg(object sender, ExecutedRoutedEventArgs e) { reserve_proc(e, 1); }
        private void reserve_del(object sender, ExecutedRoutedEventArgs e) { reserve_proc(e, 2); }
        private void reserve_proc(ExecutedRoutedEventArgs e, int proc)
        {
            if (CmdExeUtil.IsMessageBeforeCommand(e) == true)
            {
                if (MessageBox.Show("予約を" + cmdMsg[proc] + "します。\r\nよろしいですか？", cmdMsg[proc] + "の確認", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                { return; }
            }

            bool ret = false;

            if (proc == 0)
            {
                ret = MenuUtil.ReserveAdd(eventInfo.IntoList(), recSettingView.GetRecSetting());
            }
            else
            {
                List<ReserveData> list = eventInfo.GetReserveListFromPgUID();
                if (proc == 1)
                {
                    RecSettingData recSet = recSettingView.GetRecSetting();
                    list.ForEach(data => data.RecSetting = recSet);
                    ret = MenuUtil.ReserveChange(list);
                }
                else
                {
                    ret = MenuUtil.ReserveDelete(list);
                }
            }

            StatusManager.StatusNotifySet(ret, "録画予約を" + cmdMsg[proc]);

            if (ret == false) return;
            if (KeepWin == false) this.Close();
        }

        protected override ulong DataID { get { return eventInfo == null ? 0 : eventInfo.CurrentPgUID(); } }
        protected override IEnumerable<KeyValuePair<ulong, object>> DataRefList
        {
            get
            {
                return CommonManager.Instance.DB.ServiceEventList.Values.SelectMany(list => list.eventMergeList)
                    .Select(d => new KeyValuePair<ulong, object>(d.CurrentPgUID(), d));
            }
        }
        protected override void UpdateViewSelection(int mode = 0)
        {
            //番組表では「前へ」「次へ」の移動の時だけ追従させる。mode=2はアクティブ時の自動追尾
            var style = JumpItemStyle.MoveTo | (mode < 2 ? JumpItemStyle.PanelNoScroll : JumpItemStyle.None);
            if (DataView is EpgMainViewBase)
            {
                if (mode != 2) DataView.MoveToItem(DataID, style);
            }
            else if (DataView is EpgListMainView)//mode=0で実行させると重複予約アイテムの選択が解除される。
            {
                if (mode != 0 && mode != 2) DataView.MoveToItem(DataID, style);
            }
            else if (DataView is SearchWindow.AutoAddWinListView)
            {
                if (mode != 0) DataView.MoveToItem(DataID, style);//リスト番組表と同様
            }
            else if (mainWindow.reserveView.IsVisible == true)
            {
                if (mode == 2) mainWindow.reserveView.MoveToItem(0, style);//予約一覧での選択解除
            }
        }
        private void MoveViewEpgTarget()
        {
            if (DataView is EpgViewBase)
            {
                //BeginInvokeはフォーカス対応
                MenuUtil.CheckJumpTab(new SearchItem(eventInfo), true);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DataView.MoveToItem(DataID);
                }), DispatcherPriority.Loaded);
            }
            else
            {
                UpdateViewSelection(3);
            }
        }
    }
    public class AddReserveEpgWindowBase : ReserveWindowBase<AddReserveEpgWindow> { }
}
