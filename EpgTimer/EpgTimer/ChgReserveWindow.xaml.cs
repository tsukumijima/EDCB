using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EpgTimer
{
    using EpgView;

    /// <summary>
    /// ChgReserveWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ChgReserveWindow : ChgReserveWindowBase
    {
        private ReserveData reserveInfo = null;
        private bool KeepWin = Settings.Instance.KeepReserveWindow;//固定する

        protected enum AddMode { Add, Re_Add, Change }
        private AddMode addMode = AddMode.Add;      //予約モード、再予約モード、変更モード
        private int selectedTab = 0;                //EPGViewで番組表を表示するかどうか
        private bool resModeProgram = true;         //プログラム予約かEPG予約か
        private bool initOpen = true;

        private EpgEventInfo eventInfoNow = null;
        private ReserveData resInfoDisplay = null;

        static ChgReserveWindow()
        {
            mainWindow.reserveView.ViewUpdated += ChgReserveWindow.UpdatesViewSelection;
            mainWindow.tunerReserveView.ViewUpdated += ChgReserveWindow.UpdatesViewSelection;
            SearchWindow.ViewReserveUpdated += ChgReserveWindow.UpdatesViewSelection;
            EpgViewBase.ViewReserveUpdated += ChgReserveWindow.UpdatesViewSelection;
        }
        public ChgReserveWindow(ReserveData info = null, int epgInfoOpenMode = 0)
        {
            InitializeComponent();

            base.SetParam(false, checkBox_windowPinned, checkBox_dataReplace);

            //コマンドの登録
            this.CommandBindings.Add(new CommandBinding(EpgCmds.Cancel, (sender, e) => this.Close()));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.AddInDialog, reserve_add));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.ChangeInDialog, reserve_chg, (sender, e) => e.CanExecute = addMode == AddMode.Change));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.DeleteInDialog, reserve_del, (sender, e) => e.CanExecute = addMode == AddMode.Change));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.BackItem, (sender, e) => MoveViewNextItem(-1), (sender, e) => e.CanExecute = KeepWin == true));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.NextItem, (sender, e) => MoveViewNextItem(1), (sender, e) => e.CanExecute = KeepWin == true));
            this.CommandBindings.Add(new CommandBinding(EpgCmds.Search, (sender, e) => MoveViewReserveTarget(), (sender, e) => e.CanExecute = KeepWin == true && DataView is EpgViewBase || DataView is TunerReserveMainView));

            //ボタンの設定
            mBinds.SetCommandToButton(button_cancel, EpgCmds.Cancel);
            mBinds.SetCommandToButton(button_add_reserve, EpgCmds.AddInDialog);
            mBinds.SetCommandToButton(button_chg_reserve, EpgCmds.ChangeInDialog);
            mBinds.SetCommandToButton(button_del_reserve, EpgCmds.DeleteInDialog);
            mBinds.SetCommandToButton(button_up, EpgCmds.BackItem);
            mBinds.SetCommandToButton(button_down, EpgCmds.NextItem);
            mBinds.SetCommandToButton(button_chk, EpgCmds.Search);
            RefreshMenu();

            //録画設定タブ関係の設定
            recSettingView.SelectedPresetChanged += SetReserveTabHeader;
            reserveTabHeader.MouseRightButtonUp += recSettingView.OpenPresetSelectMenuOnMouseEvent;

            //その他設定
            //深夜時間関係は、comboBoxの表示だけ変更する手もあるが、
            //オプション変更タイミングなどいろいろ面倒なので、実際の値で処理することにする。
            comboBox_service.ItemsSource = ChSet5.ChListSelected;
            comboBox_sh.ItemsSource = CommonManager.CustomHourList;
            comboBox_eh.ItemsSource = CommonManager.CustomHourList;
            comboBox_sm.ItemsSource = Enumerable.Range(0, 60);
            comboBox_em.ItemsSource = Enumerable.Range(0, 60);
            comboBox_ss.ItemsSource = Enumerable.Range(0, 60);
            comboBox_es.ItemsSource = Enumerable.Range(0, 60);
            ViewUtil.Set_ComboBox_LostFocus_SelectItemUInt(stack_start);
            ViewUtil.Set_ComboBox_LostFocus_SelectItemUInt(stack_end);

            if (info == null)
            {
                info = new ReserveData();
                var sTime = DateTime.UtcNow.AddHours(9).AddMinutes(5);
                info.StartTime = sTime.AddSeconds(-sTime.Second);
                info.StartTimeEpg = info.StartTime;
                info.DurationSecond = 1800;
                info.EventID = 0xFFFF;
                info.RecSetting = Settings.Instance.RecPresetList[0].Data.DeepClone();
                reserveInfo = info;
            }
            selectedTab = epgInfoOpenMode == 1 ? 0 : 1;
            if (KeepWin == true)
            {
                button_cancel.Content = "閉じる";
                //ステータスバーの設定
                this.statusBar.Status.Visibility = Visibility.Collapsed;
                StatusManager.RegisterStatusbar(this.statusBar, this);
            }
            else
            {
                button_up.Visibility = Visibility.Collapsed;
                button_down.Visibility = Visibility.Collapsed;
                button_chk.Visibility = Visibility.Collapsed;
            }
            ChangeData(info);
            initOpen = false;
            CheckMultiReserve();
        }

        public void SetReserveTabHeader(bool SimpleChanged = true)
        {
            reserveTabHeader.Text = "予約" + recSettingView.GetRecSettingHeaderString(SimpleChanged);
        }

        private void SetAddMode(AddMode mode)
        {
            addMode = mode;
            switch (mode)
            {
                case AddMode.Add:
                    if (KeepWin == false)
                    {
                        button_chg_reserve.Visibility = Visibility.Collapsed;
                        button_del_reserve.Visibility = Visibility.Collapsed;
                    }
                    break;
                case AddMode.Re_Add:
                    reserveInfo.ReserveID = 0;
                    checkBox_releaseAutoAdd.IsChecked = false;
                    text_Status.ItemsSource = null;
                    label_errStar.Content = null;
                    //変更及び削除ボタンはCanExeの判定でグレーアウトする。
                    break;
            }
        }
        private void SetResModeProgram(bool mode)
        {
            resModeProgram = mode;

            radioButton_Epg.IsChecked = !resModeProgram;
            radioButton_Program.IsChecked = resModeProgram;

            textBox_title.SetReadOnlyWithEffect(!resModeProgram);
            comboBox_service.IsEnabled = resModeProgram;
            stack_start.IsEnabled = resModeProgram;
            stack_end.IsEnabled = resModeProgram;
            recSettingView.SetViewMode(!resModeProgram);

            CheckMultiReserve();
        }
        private void CheckMultiReserve()
        {
            if (initOpen == true) return;

            bool setMode = false;
            if (resModeProgram == false)
            {
                var resinfo = new ReserveData();
                this.GetReserveTimeInfo(ref resinfo);
                setMode = CommonManager.Instance.DB.ReserveList.Values.Any(rs => rs.IsEpgReserve && rs.IsSamePg(resinfo));
            }
            button_add_reserve.Content = setMode == true ? "重複追加" : "追加";
        }

        protected override bool ReloadInfoData()
        {
            CheckMultiReserve();
            UpdateErrStatus();
            return true;
        }

        public override void ChangeData(object data)
        {
            var info = data as ReserveData;
            if (info == null) return;

            if (reserveInfo != info)
            {
                addMode = AddMode.Change;
                reserveInfo = info.DeepClone();
            }
            recSettingView.SetViewMode(!reserveInfo.IsManual);
            recSettingView.SetDefSetting(reserveInfo.RecSetting);
            checkBox_releaseAutoAdd.IsChecked = false;
            checkBox_releaseAutoAdd.IsEnabled = reserveInfo.IsAutoAdded;

            SetAddMode(addMode);
            SetResModeProgram(reserveInfo.IsManual);
            SetReserveTimeInfo(reserveInfo);

            //番組詳細タブを初期化
            richTextBox_descInfo.Document = CommonManager.ConvertDisplayText(null, null);
            eventInfoNow = null;
            resInfoDisplay = null;

            tabControl.SelectedIndex = -1;
            tabControl.SelectedIndex = selectedTab;

            //エラー状況の表示など
            CheckMultiReserve();
            UpdateErrStatus();
            UpdateViewSelection(0);
            SetReserveTabHeader(false);
        }
        private void UpdateErrStatus()
        {
            text_Status.ItemsSource = null;

            if (addMode != AddMode.Add)
            {
                ReserveData res; //一応重複チューナなどの確認のため、データベースを読みに行く
                CommonManager.Instance.DB.ReserveList.TryGetValue(reserveInfo.ReserveID, out res);
                var resItem = new ReserveItem(res ?? reserveInfo);
                text_Status.ItemsSource = new string[] { resItem.CommentBase }.Concat(resItem.ErrComment.Select(s => "＊" + s));
                text_Status.SelectedIndex = 0;
                label_errStar.Content = text_Status.Items.Count > 1 ? string.Format("＊×{0}", text_Status.Items.Count - 1) : null;
            }
        }

        private void SetReserveTimeInfo(ReserveData resInfo)
        {
            if (resInfo == null) return;

            try
            {
                Title = ViewUtil.WindowTitleText(resInfo.Title, addMode == AddMode.Add ? "予約登録" : "予約変更");

                //テキストの選択位置を戻す
                textBox_title.Text = null;
                Dispatcher.BeginInvoke(new Action(() => textBox_title.Text = resInfo.Title), DispatcherPriority.Render);

                comboBox_service.SelectedIndex = 0;
                ChSet5Item ch;
                if (ChSet5.ChList.TryGetValue(resInfo.Create64Key(), out ch) == true)
                {
                    comboBox_service.SelectedItem = ch;
                }

                DateTime startTime = resInfo.StartTime;
                DateTime endTime = resInfo.StartTime.AddSeconds(resInfo.DurationSecond);

                //深夜時間帯の処理
                bool use28 = Settings.Instance.LaterTimeUse == true && (endTime - startTime).TotalDays < 1;
                bool late_start = use28 && startTime.Hour + 24 < comboBox_sh.Items.Count && DateTime28.IsLateHour(startTime.Hour);
                bool late_end = use28 && endTime.Hour + 24 < comboBox_eh.Items.Count && DateTime28.JudgeLateHour(endTime, startTime);

                datePicker_start.SelectedDate = startTime.Date.AddDays(late_start == true ? -1 : 0);
                comboBox_sh.SelectedIndex = startTime.Hour + (late_start == true ? 24 : 0);
                comboBox_sm.SelectedIndex = startTime.Minute;
                comboBox_ss.SelectedIndex = startTime.Second;

                datePicker_end.SelectedDate = endTime.Date.AddDays(late_end == true ? -1 : 0);
                comboBox_eh.SelectedIndex = endTime.Hour + (late_end == true ? 24 : 0);
                comboBox_em.SelectedIndex = endTime.Minute;
                comboBox_es.SelectedIndex = endTime.Second;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        private int GetReserveTimeInfo(ref ReserveData resInfo)
        {
            if (resInfo == null) return -1;

            try
            {
                resInfo.Title = textBox_title.Text;
                var ch = comboBox_service.SelectedItem as ChSet5Item;

                resInfo.StationName = ch.ServiceName;
                resInfo.OriginalNetworkID = ch.ONID;
                resInfo.TransportStreamID = ch.TSID;
                resInfo.ServiceID = ch.SID;

                resInfo.StartTime = datePicker_start.SelectedDate.Value.Date
                    .AddHours(comboBox_sh.SelectedIndex)
                    .AddMinutes(comboBox_sm.SelectedIndex)
                    .AddSeconds(comboBox_ss.SelectedIndex);

                DateTime endTime = datePicker_end.SelectedDate.Value.Date
                    .AddHours(comboBox_eh.SelectedIndex)
                    .AddMinutes(comboBox_em.SelectedIndex)
                    .AddSeconds(comboBox_es.SelectedIndex);

                resInfo.DurationSecond = (uint)Math.Max(0, (endTime - resInfo.StartTime).TotalSeconds);
                return resInfo.StartTime > endTime ? -2 : 0;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return -1;
        }

        //proc 0:追加、1:変更、2:削除
        static string[] cmdMsg = new string[] { "追加", "変更", "削除" };
        protected virtual bool CheckReserveChange(ExecutedRoutedEventArgs e, int proc)
        {
            if (CmdExeUtil.IsDisplayKgMessage(e) == true)
            {
                if (MessageBox.Show("予約を" + cmdMsg[proc] + "します。\r\nよろしいですか？", cmdMsg[proc] + "の確認", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                { return false; }
            }
            if (proc != 0)
            {
                if (CheckExistReserveItem() == false)
                { return false; }
            }
            return true;
        }
        private bool CheckExistReserveItem()
        {
            bool retval = CommonManager.Instance.DB.ReserveList.ContainsKey(this.reserveInfo.ReserveID);
            if (retval == false)
            {
                MessageBox.Show("項目がありません。\r\n" + "既に削除されています。", "データエラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                SetAddMode(AddMode.Re_Add);
                CheckMultiReserve();
            }
            return retval;
        }

        private void reserve_add(object sender, ExecutedRoutedEventArgs e) { reserve_add_chg(e, 0); }
        private void reserve_chg(object sender, ExecutedRoutedEventArgs e) { reserve_add_chg(e, 1); }
        private void reserve_add_chg(ExecutedRoutedEventArgs e, int proc)
        {
            try
            {
                if (CheckReserveChange(e, proc) == false) return;

                var resInfo = reserveInfo.DeepClone();

                if (resModeProgram == true)
                {
                    if (GetReserveTimeInfo(ref resInfo) == -2)
                    {
                        MessageBox.Show("終了日時が開始日時より前です");
                        return;
                    }

                    //サービスや時間が変わったら、個別予約扱いにする。タイトルのみ変更は見ない。
                    if (resInfo.EventID != 0xFFFF || reserveInfo.IsSamePg(resInfo) == false)
                    {
                        resInfo.EventID = 0xFFFF;
                        resInfo.ReleaseAutoAdd();
                    }
                }
                else
                {
                    //EPG予約に変える場合、またはEPG予約で別の番組に変わる場合
                    if (eventInfoNow != null && (reserveInfo.IsManual == true || reserveInfo.IsSamePg(eventInfoNow) == false))
                    {
                        //基本的にAddReserveEpgWindowと同じ処理内容
                        if (MenuUtil.CheckReservable(CommonUtil.ToList(eventInfoNow)) == null) return;
                        eventInfoNow.ConvertToReserveData(ref resInfo);
                        resInfo.ReleaseAutoAdd();
                    }
                }
                if (checkBox_releaseAutoAdd.IsChecked == true)
                {
                    resInfo.ReleaseAutoAdd();
                }

                resInfo.RecSetting = recSettingView.GetRecSetting();

                bool ret = false;
                HashSet<uint> oldset = null;
                if (proc == 0)
                {
                    oldset = new HashSet<uint>(CommonManager.Instance.DB.ReserveList.Keys);
                    ret = MenuUtil.ReserveAdd(CommonUtil.ToList(resInfo));
                    StatusManager.StatusNotifySet(ret, "録画予約を追加");
                }
                else
                {
                    ret = MenuUtil.ReserveChange(CommonUtil.ToList(resInfo));
                    StatusManager.StatusNotifySet(ret, "録画予約を変更");
                }
                if (ret == false) return;

                if (KeepWin == false)
                {
                    this.Close();
                    return;
                }

                if (proc == 0)
                {
                    var list = new List<ReserveData>();
                    CommonManager.CreateSrvCtrl().SendEnumReserve(ref list);
                    var newlist = list.Where(rs => oldset.Contains(rs.ReserveID) == false).ToList();
                    if (newlist.Count == 1)
                    {
                        ChangeData(newlist[0]);
                    }
                }
                SetReserveTabHeader(false);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        private void reserve_del(object sender, ExecutedRoutedEventArgs e)
        {
            if (CheckReserveChange(e, 2) == false) return;

            bool ret = MenuUtil.ReserveDelete(CommonUtil.ToList(reserveInfo));
            StatusManager.StatusNotifySet(ret, "録画予約を削除");
            if (ret == false) return;

            if (KeepWin == false)
            {
                this.Close();
                return;
            }

            SetAddMode(AddMode.Re_Add);
            SetReserveTabHeader(false);
        }

        //一応大丈夫だが、クリックのたびに実行されないようにしておく。
        private void radioButton_Epg_Click(object sender, RoutedEventArgs e)
        {
            if (resModeProgram == true && radioButton_Epg.IsChecked == true)
            {
                ReserveModeChanged(false);
            }
        }
        private void radioButton_Program_Click(object sender, RoutedEventArgs e)
        {
            if (resModeProgram == false && radioButton_Program.IsChecked == true)
            {
                ReserveModeChanged(true);
            }
        }
        private void ReserveModeChanged(bool programMode)
        {
            SetResModeProgram(programMode);

            eventInfoNow = null;
            if (programMode == false)
            {
                var resInfo = new ReserveData();
                GetReserveTimeInfo(ref resInfo);

                //EPGデータが読込まれていない場合も考慮し、先に判定する。
                if (reserveInfo.IsEpgReserve == true && reserveInfo.IsSamePg(resInfo) == true)
                {
                    //EPG予約で、元の状態に戻る場合
                    textBox_title.Text = reserveInfo.Title;
                }
                else
                {
                    eventInfoNow = resInfo.SearchEventInfoLikeThat();
                    if (eventInfoNow == null)
                    {
                        MessageBox.Show("変更可能な番組がありません。\r\n" +
                                        "EPGの期間外か、EPGデータが読み込まれていません。");
                        SetResModeProgram(true);
                    }
                    else
                    {
                        SetReserveTimeInfo(CtrlCmdDefEx.ConvertEpgToReserveData(eventInfoNow));
                    }
                }
            }
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ComboBoxのSelectionChangedにも反応するので。(WPFの仕様)
            if (sender != e.OriginalSource) return;

            selectedTab = tabControl.SelectedIndex != -1 ? tabControl.SelectedIndex : selectedTab;
            if (tabItem_program.IsSelected)
            {
                var resInfo = new ReserveData();
                GetReserveTimeInfo(ref resInfo);

                //描画軽減。人の操作では気にするほどのことはないが、保険。
                if (resInfo.IsSamePg(resInfoDisplay) == true) return;
                resInfoDisplay = resInfo;

                EpgEventInfo eventInfo = null;
                //EPGを自動で読み込んでない時でも、元がEPG予約ならその番組情報は表示させられるようにする
                if (reserveInfo.IsEpgReserve == true && reserveInfo.IsSamePg(resInfo) == true)
                {
                    eventInfo = eventInfoNow ?? reserveInfo.ReserveEventInfo();
                }
                else
                {
                    eventInfo = eventInfoNow ?? resInfo.SearchEventInfoLikeThat();
                }

                richTextBox_descInfo.Document = CommonManager.ConvertDisplayText(eventInfo);
            }
        }

        protected override DataItemViewBase DataView { get { return mainWindow.tunerReserveView.IsVisible == true ? mainWindow.tunerReserveView : base.DataView; } }
        protected override UInt64 DataID { get { return reserveInfo == null ? 0 : reserveInfo.ReserveID; } }
        protected override IEnumerable<KeyValuePair<UInt64, object>> DataRefList { get { return CommonManager.Instance.DB.ReserveList.OrderBy(d => d.Value.StartTimeActual).Select(d => new KeyValuePair<UInt64, object>(d.Key, d.Value)); } }

        protected override void UpdateViewSelection(int mode = 0)
        {
            //番組表では「前へ」「次へ」の移動の時だけ追従させる。mode=2はアクティブ時の自動追尾
            var style = JumpItemStyle.MoveTo | (mode < 2 ? JumpItemStyle.PanelNoScroll : JumpItemStyle.None);
            if (DataView is ReserveView)
            {
                if (mode != 0) DataView.MoveToItem(DataID, style);
            }
            else if (DataView is TunerReserveMainView)
            {
                if (mode != 2) DataView.MoveToItem(DataID, style);
            }
            else if (DataView is EpgMainViewBase)
            {
                if (mode != 2) DataView.MoveToReserveItem(reserveInfo, style);
            }
            else if (DataView is EpgListMainView)
            {
                if (mode != 0 && mode != 2) DataView.MoveToReserveItem(reserveInfo, style);
            }
            else if (DataView is SearchWindow.AutoAddWinListView)
            {
                if (mode != 0) DataView.MoveToReserveItem(reserveInfo, style);
            }
        }
        private void MoveViewReserveTarget()
        {
            //予約一覧以外では「前へ」「次へ」の移動の時に追従させる
            if (DataView is EpgViewBase)
            {
                //BeginInvokeはフォーカス対応
                mainWindow.epgView.SearchJumpTargetProgram(reserveInfo == null ? 0 : reserveInfo.Create64Key());
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DataView.MoveToReserveItem(reserveInfo);
                }), DispatcherPriority.Loaded);
            }
            else
            {
                UpdateViewSelection(3);
            }
        }
        protected override void MoveViewNextItem(int direction)
        {
            object NewData = null;
            if (DataView is EpgViewBase || DataView is SearchWindow.AutoAddWinListView)
            {
                NewData = DataView.MoveNextReserve(direction, DataID, true, JumpItemStyle.None);
                if (NewData != null)
                {
                    ChangeData(NewData);
                    return;
                }
            }
            base.MoveViewNextItem(direction);
        }
    }
    public class ChgReserveWindowBase : ReserveWindowBase<ChgReserveWindow> { }
    public class ReserveWindowBase<T> : AttendantDataWindow<T>
    {
        public ReserveWindowBase()
        {
            var win = Application.Current.Windows.OfType<SearchWindow>().FirstOrDefault(w => w.IsActive == true);
            if (win != null) SearchWinHash = win.GetHashCode();
        }
        protected override DataItemViewBase DataView
        {
            get
            {
                DataItemViewBase view = mainWindow.epgView.ActiveView;
                return view != null && view.IsVisible == true ? view : mainWindow.reserveView.IsVisible == true ? mainWindow.reserveView : DataViewSearch;
            }
        }
        protected int SearchWinHash = 0;
        protected DataItemViewBase DataViewSearch
        {
            get
            {
                if (SearchWinHash == 0) return null;
                var win = Application.Current.Windows.OfType<SearchWindow>().FirstOrDefault(w => w.GetHashCode() == SearchWinHash);
                if (win == null)
                {
                    SearchWinHash = 0;
                    return null;
                }
                return win.IsVisible == true ? win.DataListView : null;
            }

        }
    }
}
