using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.IO;
using System.Reflection;

namespace EpgTimer.Setting
{
    using BoxExchangeEdit;

    /// <summary>
    /// SetAppView.xaml の相互作用ロジック
    /// </summary>
    public partial class SetAppView : UserControl
    {
        BoxExchangeEditor bxb;
        BoxExchangeEditor bxt;
        private List<string> buttonItem;
        private List<string> taskItem;

        private List<IEPGStationInfo> stationList;
        private RadioBtnSelect recEndModeRadioBtns;
        private RadioBtnSelect delReserveModeRadioBtns;

        private List<RecPresetItem> recPresetList = new List<RecPresetItem>();
        private List<SearchPresetItem> searchPresetList = new List<SearchPresetItem>();

        public SetAppView()
        {
            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                tabItem1.Foreground = SystemColors.GrayTextBrush;
                grid_AppRecEnd.IsEnabled = false;
                grid_AppRec.IsEnabled = false;
                ViewUtil.ChangeChildren(grid_AppCancelMain, false);
                grid_AppCancelMainInput.IsEnabled = true;
                ViewUtil.ChangeChildren(grid_AppCancelMainInput, false);
                textBox_process.SetReadOnlyWithEffect(true);

                ViewUtil.ChangeChildren(grid_AppReserve1, false);
                ViewUtil.ChangeChildren(grid_AppReserve2, false);
                grid_AppReserveIgnore.IsEnabled = true;
                ViewUtil.ChangeChildren(grid_AppReserveIgnore, false);
                text_RecInfo2RegExp.SetReadOnlyWithEffect(true);
                checkBox_autoDel.IsEnabled = false;
                ViewUtil.ChangeChildren(grid_App2DelMain, false);
                listBox_ext.IsEnabled = true;
                textBox_ext.SetReadOnlyWithEffect(true);
                grid_App2DelChkFolderText.IsEnabled = true;
                listBox_chk_folder.IsEnabled = true;
                textBox_chk_folder.SetReadOnlyWithEffect(true);
                button_chk_open.IsEnabled = true;

                grid_recname.IsEnabled = false;
                checkBox_noChkYen.IsEnabled = false;
                grid_delReserve.IsEnabled = false;

                checkBox_wakeReconnect.IsEnabled = true;
                stackPanel_WoLWait.IsEnabled = true;
                checkBox_suspendClose.IsEnabled = true;
                checkBox_ngAutoEpgLoad.IsEnabled = true;
                checkBox_keepTCPConnect.IsEnabled = true;
                grid_srvResident.IsEnabled = false;
                button_srvSetting.IsEnabled = false;
                label_shortCutSrv.IsEnabled = false;
                button_shortCutSrv.IsEnabled = false;
                checkBox_srvSaveNotifyLog.IsEnabled = false;
                checkBox_srvSaveDebugLog.IsEnabled = false;
                grid_tsExt.IsEnabled = false;
            }
            else
            {
                checkBox_srvResident.Click += (sender, e) => SetIsEnabledBlinkPreRec();
                checkBox_srvShowTray.Click += (sender, e) => SetIsEnabledBlinkPreRec();
            }

            //0 全般
            button_srvSetting.Click += (sender, e) => CommonManager.OpenSrvSetting();

            //1 録画動作
            var bx = new BoxExchangeEditor(null, listBox_process, true);
            listBox_process.SelectionChanged += ViewUtil.ListBox_TextBoxSyncSelectionChanged(listBox_process, textBox_process);
            if (CommonManager.Instance.NWMode == false)
            {
                bx.AllowKeyAction();
                bx.AllowDragDrop();
                button_process_del.Click += new RoutedEventHandler(bx.button_Delete_Click);
                button_process_add.Click += ViewUtil.ListBox_TextCheckAdd(listBox_process, textBox_process);
                textBox_process.KeyDown += ViewUtil.KeyDown_Enter(button_process_add);
            }

            comboBox_process.Items.AddItems(new string[] { "リアルタイム", "高", "通常以上", "通常", "通常以下", "低" });

            //2 予約管理情報
            var bxe = new BoxExchangeEditor(null, listBox_ext, true);
            var bxc = new BoxExchangeEditor(null, listBox_chk_folder, true);
            listBox_ext.SelectionChanged += ViewUtil.ListBox_TextBoxSyncSelectionChanged(listBox_ext, textBox_ext);
            bxc.TargetBox.SelectionChanged += ViewUtil.ListBox_TextBoxSyncSelectionChanged(bxc.TargetBox, textBox_chk_folder);
            bxc.TargetBox.KeyDown += ViewUtil.KeyDown_Enter(button_chk_open);
            bxc.targetBoxAllowDoubleClick(bxc.TargetBox, (sender, e) => button_chk_open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            if (CommonManager.Instance.NWMode == false)
            {
                bxe.AllowKeyAction();
                bxe.AllowDragDrop();
                button_ext_del.Click += new RoutedEventHandler(bxe.button_Delete_Click);
                button_ext_add.Click += ViewUtil.ListBox_TextCheckAdd(listBox_ext, textBox_ext);
                bxc.AllowKeyAction();
                bxc.AllowDragDrop();
                button_chk_del.Click += new RoutedEventHandler(bxc.button_Delete_Click);
                button_chk_add.Click += (sender, e) => textBox_chk_folder.Text = SettingPath.CheckFolder(textBox_chk_folder.Text);
                button_chk_add.Click += ViewUtil.ListBox_TextCheckAdd(listBox_chk_folder, textBox_chk_folder);

                textBox_ext.KeyDown += ViewUtil.KeyDown_Enter(button_ext_add);
                textBox_chk_folder.KeyDown += ViewUtil.KeyDown_Enter(button_chk_add);

                checkBox_autoDel_Click(null, null);
            }

            //3 ボタン表示 ボタン表示画面の上下ボタンのみ他と同じものを使用する。
            bxb = new BoxExchangeEditor(this.listBox_itemBtn, this.listBox_viewBtn, true);
            bxt = new BoxExchangeEditor(this.listBox_itemTask, this.listBox_viewTask, true);
            textblockTimer.Text = CommonManager.Instance.NWMode == true ?
                "EpgTimerNW側の設定です。" :
                "録画終了時にスタンバイ、休止する場合は必ず表示されます(ただし、サービス未使用時はこの設定は使用されず15秒固定)。";

            //上部表示ボタン関係
            bxb.AllowDuplication(StringItem.Items(Settings.ViewButtonSpacer), StringItem.Cloner, StringItem.Comparator);
            button_btnUp.Click += new RoutedEventHandler(bxb.button_Up_Click);
            button_btnDown.Click += new RoutedEventHandler(bxb.button_Down_Click);
            button_btnAdd.Click += new RoutedEventHandler((sender, e) => button_Add(bxb, buttonItem));
            button_btnIns.Click += new RoutedEventHandler((sender, e) => button_Add(bxb, buttonItem, true));
            button_btnDel.Click += new RoutedEventHandler((sender, e) => button_Dell(bxb, bxt, buttonItem));
            bxb.sourceBoxAllowKeyAction(listBox_itemBtn, (sender, e) => button_btnAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxb.targetBoxAllowKeyAction(listBox_viewBtn, (sender, e) => button_btnDel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxb.sourceBoxAllowDoubleClick(listBox_itemBtn, (sender, e) => button_btnAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxb.targetBoxAllowDoubleClick(listBox_viewBtn, (sender, e) => button_btnDel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxb.sourceBoxAllowDragDrop(listBox_itemBtn, (sender, e) => button_btnDel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxb.targetBoxAllowDragDrop(listBox_viewBtn, (sender, e) => drag_drop(sender, e, button_btnAdd, button_btnIns));

            //タスクアイコン関係
            bxt.AllowDuplication(StringItem.Items(Settings.TaskMenuSeparator), StringItem.Cloner, StringItem.Comparator);
            button_taskUp.Click += new RoutedEventHandler(bxt.button_Up_Click);
            button_taskDown.Click += new RoutedEventHandler(bxt.button_Down_Click);
            button_taskAdd.Click += new RoutedEventHandler((sender, e) => button_Add(bxt, taskItem));
            button_taskIns.Click += new RoutedEventHandler((sender, e) => button_Add(bxt, taskItem, true));
            button_taskDel.Click += new RoutedEventHandler((sender, e) => button_Dell(bxt, bxb, taskItem));
            bxt.sourceBoxAllowKeyAction(listBox_itemTask, (sender, e) => button_taskAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxt.targetBoxAllowKeyAction(listBox_viewTask, (sender, e) => button_taskDel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxt.sourceBoxAllowDoubleClick(listBox_itemTask, (sender, e) => button_taskAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxt.targetBoxAllowDoubleClick(listBox_viewTask, (sender, e) => button_taskDel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxt.sourceBoxAllowDragDrop(listBox_itemTask, (sender, e) => button_taskDel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            bxt.targetBoxAllowDragDrop(listBox_viewTask, (sender, e) => drag_drop(sender, e, button_taskAdd, button_taskIns));

            //5 iEpg キャンセルアクションだけは付けておく
            new BoxExchangeEditor(null, this.listBox_service, true);
            var bxi = new BoxExchangeEditor(null, this.listBox_iEPG, true);
            bxi.targetBoxAllowKeyAction(this.listBox_iEPG, new KeyEventHandler((sender, e) => button_del.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
            bxi.TargetBox.SelectionChanged += ViewUtil.ListBox_TextBoxSyncSelectionChanged(bxi.TargetBox, textBox_station);
            textBox_station.KeyDown += ViewUtil.KeyDown_Enter(button_add);
        }

        public void LoadSetting()
        {
            //0 全般
            checkBox_closeMin.IsChecked = Settings.Instance.CloseMin;
            checkBox_minWake.IsChecked = Settings.Instance.WakeMin;
            checkBox_showTray.IsChecked = Settings.Instance.ShowTray;
            checkBox_minHide.IsChecked = Settings.Instance.MinHide;

            recPresetList = Settings.Instance.RecPresetList.Clone();
            searchPresetList = Settings.Instance.SearchPresetList.Clone();

            checkBox_noBallonTips.IsChecked = Settings.Instance.NoBallonTips;
            textBox_ForceHideBalloonTipSec.Text = Settings.Instance.ForceHideBalloonTipSec.ToString();

            int residentMode = IniFileHandler.GetPrivateProfileInt("SET", "ResidentMode", 0, SettingPath.TimerSrvIniPath);
            checkBox_srvResident.IsChecked = residentMode >= 1;
            checkBox_srvShowTray.IsChecked = residentMode >= 2;
            SetIsEnabledBlinkPreRec();
            checkBox_blinkPreRec.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "BlinkPreRec", false, SettingPath.TimerSrvIniPath);
            checkBox_srvNoBalloonTip.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "NoBalloonTip", false, SettingPath.TimerSrvIniPath);

            string StartUpPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            button_shortCut.Tag = Path.Combine(StartUpPath, SettingPath.ModuleName + ".lnk");
            button_shortCut.Content = File.Exists(button_shortCut.Tag as string) ? "削除" : "作成";
            button_shortCutSrv.Tag = Path.Combine(StartUpPath, "EpgTimerSrv.lnk");
            button_shortCutSrv.Content = File.Exists(button_shortCutSrv.Tag as string) ? "削除" : "作成";

            checkBox_srvSaveNotifyLog.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "SaveNotifyLog", false, SettingPath.TimerSrvIniPath);
            checkBox_AutoSaveNotifyLog.IsChecked = Settings.Instance.AutoSaveNotifyLog == 1;
            checkBox_srvSaveDebugLog.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "SaveDebugLog", false, SettingPath.TimerSrvIniPath);
            textBox_tsExt.Text = SettingPath.CheckTSExtension(IniFileHandler.GetPrivateProfileString("SET", "TSExt", ".ts", SettingPath.TimerSrvIniPath));

            checkBox_cautionManyChange.IsChecked = Settings.Instance.CautionManyChange;
            textBox_cautionManyChange.Text = Settings.Instance.CautionManyNum.ToString();
            textBox_upDateTaskText.IsChecked = Settings.Instance.UpdateTaskText;
            checkBox_exitAfterProcessingArgs.IsChecked = Settings.Instance.ExitAfterProcessingArgs;
            checkBox_wakeReconnect.IsChecked = Settings.Instance.WakeReconnectNW;
            checkBox_WoLWaitRecconect.IsChecked = Settings.Instance.WoLWaitRecconect;
            textBox_WoLWaitSecond.Text = Settings.Instance.WoLWaitSecond.ToString();
            checkBox_suspendClose.IsChecked = Settings.Instance.SuspendCloseNW;
            checkBox_ngAutoEpgLoad.IsChecked = Settings.Instance.NgAutoEpgLoadNW;
            checkBox_keepTCPConnect.IsChecked = Settings.Instance.ChkSrvRegistTCP;
            textBox_chkTimerInterval.Text = Settings.Instance.ChkSrvRegistInterval.ToString();
            checkBox_forceNWMode.IsChecked = Settings.Instance.ForceNWMode;

            //1 録画動作
            recEndModeRadioBtns = new RadioBtnSelect(panel_recEndMode);
            recEndModeRadioBtns.Value = IniFileHandler.GetPrivateProfileInt("SET", "RecEndMode", 2, SettingPath.TimerSrvIniPath);
            checkBox_reboot.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "Reboot", false, SettingPath.TimerSrvIniPath);
            textBox_pcWakeTime.Text = IniFileHandler.GetPrivateProfileInt("SET", "WakeTime", 5, SettingPath.TimerSrvIniPath).ToString();

            listBox_process.Items.Clear();
            int ngCount = IniFileHandler.GetPrivateProfileInt("NO_SUSPEND", "Count", int.MaxValue, SettingPath.TimerSrvIniPath);
            if (ngCount == int.MaxValue)
            {
                listBox_process.Items.Add("EpgDataCap_Bon.exe");
            }
            else
            {
                for (int i = 0; i < ngCount; i++)
                {
                    listBox_process.Items.Add(IniFileHandler.GetPrivateProfileString("NO_SUSPEND", i.ToString(), "", SettingPath.TimerSrvIniPath));
                }
            }
            textBox_ng_min.Text = IniFileHandler.GetPrivateProfileString("NO_SUSPEND", "NoStandbyTime", "10", SettingPath.TimerSrvIniPath);
            checkBox_ng_usePC.IsChecked = IniFileHandler.GetPrivateProfileBool("NO_SUSPEND", "NoUsePC", false, SettingPath.TimerSrvIniPath);
            textBox_ng_usePC_min.Text = IniFileHandler.GetPrivateProfileString("NO_SUSPEND", "NoUsePCTime", "3", SettingPath.TimerSrvIniPath);
            checkBox_ng_fileStreaming.IsChecked = IniFileHandler.GetPrivateProfileBool("NO_SUSPEND", "NoFileStreaming", false, SettingPath.TimerSrvIniPath);
            checkBox_ng_shareFile.IsChecked = IniFileHandler.GetPrivateProfileBool("NO_SUSPEND", "NoShareFile", false, SettingPath.TimerSrvIniPath);

            textBox_megine_start.Text = IniFileHandler.GetPrivateProfileInt("SET", "StartMargin", 5, SettingPath.TimerSrvIniPath).ToString();
            textBox_margine_end.Text = IniFileHandler.GetPrivateProfileInt("SET", "EndMargin", 2, SettingPath.TimerSrvIniPath).ToString();
            textBox_appWakeTime.Text = Settings.Instance.RecAppWakeTime.ToString();
            checkBox_appMin.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecMinWake", true, SettingPath.TimerSrvIniPath);
            checkBox_appView.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecView", true, SettingPath.TimerSrvIniPath);
            checkBox_appDrop.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "DropLog", true, SettingPath.TimerSrvIniPath);
            checkBox_addPgInfo.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "PgInfoLog", true, SettingPath.TimerSrvIniPath);
            checkBox_appNW.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecNW", false, SettingPath.TimerSrvIniPath);
            checkBox_appKeepDisk.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "KeepDisk", true, SettingPath.TimerSrvIniPath);
            checkBox_appOverWrite.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecOverWrite", false, SettingPath.TimerSrvIniPath);
            checkBox_wakeUpHdd.IsChecked = Settings.Instance.WakeUpHdd;
            textBox_noWakeUpHddMin.Text = Settings.Instance.NoWakeUpHddMin.ToString();
            checkBox_onlyWakeUpHddOverlap.IsChecked = Settings.Instance.WakeUpHddOverlapNum >= 1;
            comboBox_process.SelectedIndex = IniFileHandler.GetPrivateProfileInt("SET", "ProcessPriority", 3, SettingPath.TimerSrvIniPath);

            //2 予約管理情報
            checkBox_back_priority.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "BackPriority", true, SettingPath.TimerSrvIniPath);
            checkBox_fixedTunerPriority.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "FixedTunerPriority", true, SettingPath.TimerSrvIniPath);
            text_RecInfo2RegExp.Text = IniFileHandler.GetPrivateProfileString("SET", "RecInfo2RegExp", "", SettingPath.TimerSrvIniPath);
            checkBox_recInfoFolderOnly.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecInfoFolderOnly", true, SettingPath.TimerSrvIniPath);
            checkBox_autoDelRecInfo.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "AutoDelRecInfo", false, SettingPath.TimerSrvIniPath);
            textBox_autoDelRecInfo.Text = IniFileHandler.GetPrivateProfileInt("SET", "AutoDelRecInfoNum", 100, SettingPath.TimerSrvIniPath).ToString();
            checkBox_recInfoDelFile.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecInfoDelFile", false, SettingPath.CommonIniPath);
            checkBox_applyExtTo.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "ApplyExtToRecInfoDel", false, SettingPath.TimerSrvIniPath);
            checkBox_autoDel.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "AutoDel", false, SettingPath.TimerSrvIniPath);

            listBox_ext.Items.Clear();
            int count;
            count = IniFileHandler.GetPrivateProfileInt("DEL_EXT", "Count", int.MaxValue, SettingPath.TimerSrvIniPath);
            if (count == int.MaxValue)
            {
                button_ext_def_Click(null, null);
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    listBox_ext.Items.Add(IniFileHandler.GetPrivateProfileString("DEL_EXT", i.ToString(), "", SettingPath.TimerSrvIniPath));
                }
            }
            listBox_chk_folder.Items.Clear();
            count = IniFileHandler.GetPrivateProfileInt("DEL_CHK", "Count", 0, SettingPath.TimerSrvIniPath);
            for (int i = 0; i < count; i++)
            {
                listBox_chk_folder.Items.Add(IniFileHandler.GetPrivateProfileFolder("DEL_CHK", i.ToString(), SettingPath.TimerSrvIniPath));
            }

            checkBox_recname.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "RecNamePlugIn", false, SettingPath.TimerSrvIniPath);
            if (CommonManager.Instance.IsConnected == true)
            {
                CommonManager.Instance.DB.ReloadPlugInFile();
            }
            comboBox_recname.ItemsSource = CommonManager.Instance.DB.RecNamePlugInList;
            comboBox_recname.SelectedItem = IniFileHandler.GetPrivateProfileString("SET", "RecNamePlugInFile", "RecName_Macro.dll", SettingPath.TimerSrvIniPath);

            checkBox_noChkYen.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "NoChkYen", false, SettingPath.TimerSrvIniPath);
            delReserveModeRadioBtns = new RadioBtnSelect(grid_delReserve);
            delReserveModeRadioBtns.Value = IniFileHandler.GetPrivateProfileInt("SET", "DelReserveMode", 2, SettingPath.TimerSrvIniPath);

            checkBox_cautionOnRecChange.IsChecked = Settings.Instance.CautionOnRecChange;
            textBox_cautionOnRecMarginMin.Text = Settings.Instance.CautionOnRecMarginMin.ToString();
            checkBox_SyncResAutoAddChange.IsChecked = Settings.Instance.SyncResAutoAddChange;
            checkBox_SyncResAutoAddChgNewRes.IsChecked = Settings.Instance.SyncResAutoAddChgNewRes;
            checkBox_SyncResAutoAddDelete.IsChecked = Settings.Instance.SyncResAutoAddDelete;

            //3 ボタン表示
            buttonItem = Settings.GetViewButtonAllIDs();
            listBox_viewBtn.Items.Clear();
            listBox_viewBtn.Items.AddItems(StringItem.Items(Settings.Instance.ViewButtonList.Where(item => buttonItem.Contains(item) == true)));
            reLoadButtonItem(bxb, buttonItem);

            taskItem = Settings.GetTaskMenuAllIDs();
            listBox_viewTask.Items.Clear();
            listBox_viewTask.Items.AddItems(StringItem.Items(Settings.Instance.TaskMenuList.Where(item => taskItem.Contains(item) == true)));
            reLoadButtonItem(bxt, taskItem);

            checkBox_showAsTab.IsChecked = Settings.Instance.ViewButtonShowAsTab;
            checkBox_suspendChk.IsChecked = Settings.Instance.SuspendChk == 1;
            textBox_suspendChkTime.Text = Settings.Instance.SuspendChkTime.ToString();

            //4 カスタムボタン
            textBox_name1.Text = Settings.Instance.Cust1BtnName;
            textBox_exe1.Text = Settings.Instance.Cust1BtnCmd;
            textBox_opt1.Text = Settings.Instance.Cust1BtnCmdOpt;

            textBox_name2.Text = Settings.Instance.Cust2BtnName;
            textBox_exe2.Text = Settings.Instance.Cust2BtnCmd;
            textBox_opt2.Text = Settings.Instance.Cust2BtnCmdOpt;

            textBox_name3.Text = Settings.Instance.Cust3BtnName;
            textBox_exe3.Text = Settings.Instance.Cust3BtnCmd;
            textBox_opt3.Text = Settings.Instance.Cust3BtnCmdOpt;

            //5 iEpg
            listBox_service.ItemsSource = ChSet5.ChListSelected.Select(info => new ServiceViewItem(info));
            stationList = Settings.Instance.IEpgStationList.ToList();
        }

        public void SaveSetting()
        {
            //0 全般
            Settings.Instance.CloseMin = (bool)checkBox_closeMin.IsChecked;
            Settings.Instance.WakeMin = (bool)checkBox_minWake.IsChecked;
            Settings.Instance.ShowTray = (bool)checkBox_showTray.IsChecked;
            Settings.Instance.MinHide = (bool)checkBox_minHide.IsChecked;

            Settings.Instance.RecPresetList = recPresetList.Clone();
            Settings.Instance.SearchPresetList = searchPresetList.Clone();

            Settings.Instance.NoBallonTips = (bool)checkBox_noBallonTips.IsChecked;
            Settings.Instance.ForceHideBalloonTipSec = MenuUtil.MyToNumerical(textBox_ForceHideBalloonTipSec, Convert.ToInt32, 255, 0, Settings.Instance.ForceHideBalloonTipSec);

            IniFileHandler.WritePrivateProfileString("SET", "ResidentMode",
                checkBox_srvResident.IsChecked == false ? 0 : checkBox_srvShowTray.IsChecked == false ? 1 : 2, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "BlinkPreRec", checkBox_blinkPreRec.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "NoBalloonTip", checkBox_srvNoBalloonTip.IsChecked, SettingPath.TimerSrvIniPath);

            IniFileHandler.WritePrivateProfileString("SET", "SaveNotifyLog", checkBox_srvSaveNotifyLog.IsChecked, SettingPath.TimerSrvIniPath);
            Settings.Instance.AutoSaveNotifyLog = (short)(checkBox_AutoSaveNotifyLog.IsChecked == true ? 1 : 0);
            IniFileHandler.WritePrivateProfileString("SET", "SaveDebugLog", checkBox_srvSaveDebugLog.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "TSExt", SettingPath.CheckTSExtension(textBox_tsExt.Text), SettingPath.TimerSrvIniPath);

            Settings.Instance.CautionManyChange = (bool)checkBox_cautionManyChange.IsChecked;
            Settings.Instance.CautionManyNum = MenuUtil.MyToNumerical(textBox_cautionManyChange, Convert.ToInt32, Settings.Instance.CautionManyNum);
            Settings.Instance.ExitAfterProcessingArgs = (bool)checkBox_exitAfterProcessingArgs.IsChecked;
            Settings.Instance.WakeReconnectNW = (bool)checkBox_wakeReconnect.IsChecked;
            Settings.Instance.WoLWaitRecconect = (bool)checkBox_WoLWaitRecconect.IsChecked;
            Settings.Instance.WoLWaitSecond = MenuUtil.MyToNumerical(textBox_WoLWaitSecond, Convert.ToDouble, 3600, 1, Settings.Instance.WoLWaitSecond);
            Settings.Instance.SuspendCloseNW = (bool)checkBox_suspendClose.IsChecked;
            Settings.Instance.NgAutoEpgLoadNW = (bool)checkBox_ngAutoEpgLoad.IsChecked;
            Settings.Instance.ChkSrvRegistTCP = (bool)checkBox_keepTCPConnect.IsChecked;
            Settings.Instance.ChkSrvRegistInterval = MenuUtil.MyToNumerical(textBox_chkTimerInterval, Convert.ToDouble, 1440 * 7, 1, Settings.Instance.ChkSrvRegistInterval);
            Settings.Instance.UpdateTaskText = (bool)textBox_upDateTaskText.IsChecked;
            Settings.Instance.ForceNWMode = (bool)checkBox_forceNWMode.IsChecked;
            
            //1 録画動作
            IniFileHandler.WritePrivateProfileString("SET", "RecEndMode", recEndModeRadioBtns.Value, -1, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "Reboot", checkBox_reboot.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "WakeTime", textBox_pcWakeTime.Text, SettingPath.TimerSrvIniPath);

            List<String> ngProcessList = listBox_process.Items.OfType<string>().ToList();
            IniFileHandler.WritePrivateProfileString("NO_SUSPEND", "Count", ngProcessList.Count, SettingPath.TimerSrvIniPath);
            IniFileHandler.DeletePrivateProfileNumberKeys("NO_SUSPEND", SettingPath.TimerSrvIniPath);
            for (int i = 0; i < ngProcessList.Count; i++)
            {
                IniFileHandler.WritePrivateProfileString("NO_SUSPEND", i.ToString(), ngProcessList[i], SettingPath.TimerSrvIniPath);
            }

            IniFileHandler.WritePrivateProfileString("NO_SUSPEND", "NoStandbyTime", textBox_ng_min.Text, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("NO_SUSPEND", "NoUsePC", checkBox_ng_usePC.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("NO_SUSPEND", "NoUsePCTime", textBox_ng_usePC_min.Text, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("NO_SUSPEND", "NoFileStreaming", checkBox_ng_fileStreaming.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("NO_SUSPEND", "NoShareFile", checkBox_ng_shareFile.IsChecked, SettingPath.TimerSrvIniPath);

            IniFileHandler.WritePrivateProfileString("SET", "StartMargin", textBox_megine_start.Text, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "EndMargin", textBox_margine_end.Text, SettingPath.TimerSrvIniPath);
            Settings.Instance.RecAppWakeTime = MenuUtil.MyToNumerical(textBox_appWakeTime, Convert.ToInt32, Int32.MaxValue, 0, Settings.Instance.RecAppWakeTime);
            IniFileHandler.WritePrivateProfileString("SET", "RecMinWake", checkBox_appMin.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecView", checkBox_appView.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "DropLog", checkBox_appDrop.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "PgInfoLog", checkBox_addPgInfo.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecNW", checkBox_appNW.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "KeepDisk", checkBox_appKeepDisk.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecOverWrite", checkBox_appOverWrite.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "ProcessPriority", comboBox_process.SelectedIndex, SettingPath.TimerSrvIniPath);
            Settings.Instance.WakeUpHdd = (bool)checkBox_wakeUpHdd.IsChecked;
            if (Settings.Instance.WakeUpHdd == false) CommonManager.WakeUpHDDLogClear();
            Settings.Instance.NoWakeUpHddMin = MenuUtil.MyToNumerical(textBox_noWakeUpHddMin, Convert.ToInt32, Int32.MaxValue, 0, Settings.Instance.NoWakeUpHddMin);
            Settings.Instance.WakeUpHddOverlapNum = checkBox_onlyWakeUpHddOverlap.IsChecked == true ? 1 : 0;

            //2 予約管理情報
            IniFileHandler.WritePrivateProfileString("SET", "BackPriority", checkBox_back_priority.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "FixedTunerPriority", checkBox_fixedTunerPriority.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecInfoFolderOnly", checkBox_recInfoFolderOnly.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecInfo2RegExp", text_RecInfo2RegExp.Text, "", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "AutoDelRecInfo", checkBox_autoDelRecInfo.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "AutoDelRecInfoNum", textBox_autoDelRecInfo.Text, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecInfoDelFile", checkBox_recInfoDelFile.IsChecked, false, SettingPath.CommonIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "ApplyExtToRecInfoDel", checkBox_applyExtTo.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "AutoDel", checkBox_autoDel.IsChecked, SettingPath.TimerSrvIniPath);

            List<String> extList = listBox_ext.Items.OfType<string>().ToList();
            List<String> delChkFolderList = ViewUtil.GetFolderList(listBox_chk_folder);
            IniFileHandler.WritePrivateProfileString("DEL_EXT", "Count", extList.Count, SettingPath.TimerSrvIniPath);
            IniFileHandler.DeletePrivateProfileNumberKeys("DEL_EXT", SettingPath.TimerSrvIniPath);
            for (int i = 0; i < extList.Count; i++)
            {
                IniFileHandler.WritePrivateProfileString("DEL_EXT", i.ToString(), extList[i], SettingPath.TimerSrvIniPath);
            }
            IniFileHandler.WritePrivateProfileString("DEL_CHK", "Count", delChkFolderList.Count, SettingPath.TimerSrvIniPath);
            IniFileHandler.DeletePrivateProfileNumberKeys("DEL_CHK", SettingPath.TimerSrvIniPath);
            for (int i = 0; i < delChkFolderList.Count; i++)
            {
                IniFileHandler.WritePrivateProfileString("DEL_CHK", i.ToString(), delChkFolderList[i], SettingPath.TimerSrvIniPath);
            }

            IniFileHandler.WritePrivateProfileString("SET", "RecNamePlugIn", checkBox_recname.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "RecNamePlugInFile", comboBox_recname.SelectedItem as string ?? "", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "NoChkYen", checkBox_noChkYen.IsChecked, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "DelReserveMode", delReserveModeRadioBtns.Value, -1, SettingPath.TimerSrvIniPath);

            Settings.Instance.CautionOnRecChange = (bool)checkBox_cautionOnRecChange.IsChecked;
            Settings.Instance.CautionOnRecMarginMin = MenuUtil.MyToNumerical(textBox_cautionOnRecMarginMin, Convert.ToInt32, Settings.Instance.CautionOnRecMarginMin);
            Settings.Instance.SyncResAutoAddChange = (bool)checkBox_SyncResAutoAddChange.IsChecked;
            Settings.Instance.SyncResAutoAddDelete = (bool)checkBox_SyncResAutoAddDelete.IsChecked;
            Settings.Instance.SyncResAutoAddChgNewRes = (bool)checkBox_SyncResAutoAddChgNewRes.IsChecked;

            //3 ボタン表示
            Settings.Instance.ViewButtonList = listBox_viewBtn.Items.OfType<StringItem>().ValueList();
            Settings.Instance.TaskMenuList = listBox_viewTask.Items.OfType<StringItem>().ValueList();

            Settings.Instance.ViewButtonShowAsTab = (bool)checkBox_showAsTab.IsChecked;
            Settings.Instance.SuspendChk = (uint)(checkBox_suspendChk.IsChecked == true ? 1 : 0);
            Settings.Instance.SuspendChkTime = MenuUtil.MyToNumerical(textBox_suspendChkTime, Convert.ToUInt32, Settings.Instance.SuspendChkTime);

            //4 カスタムボタン
            Settings.Instance.Cust1BtnName = textBox_name1.Text;
            Settings.Instance.Cust1BtnCmd = textBox_exe1.Text;
            Settings.Instance.Cust1BtnCmdOpt = textBox_opt1.Text;

            Settings.Instance.Cust2BtnName = textBox_name2.Text;
            Settings.Instance.Cust2BtnCmd = textBox_exe2.Text;
            Settings.Instance.Cust2BtnCmdOpt = textBox_opt2.Text;

            Settings.Instance.Cust3BtnName = textBox_name3.Text;
            Settings.Instance.Cust3BtnCmd = textBox_exe3.Text;
            Settings.Instance.Cust3BtnCmdOpt = textBox_opt3.Text;

            //5 iEpg
            Settings.Instance.IEpgStationList = stationList.ToList();
        }

        private void button_process_open_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFileNameByDialog(textBox_process, true, "", ".exe");
        }

        private void button_ext_def_Click(object sender, RoutedEventArgs e)
        {
            ViewUtil.ListBox_TextCheckAdd(listBox_ext, ".ts.err");
            ViewUtil.ListBox_TextCheckAdd(listBox_ext, ".ts.program.txt");
        }
        private void button_chk_open_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFolderNameByDialog(textBox_chk_folder, "自動削除対象フォルダの選択", true);
        }

        private void button_recname_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.ShowPlugInSetting(comboBox_recname.SelectedItem as string, "RecName", this);
        }

        private void drag_drop(object sender, DragEventArgs e, Button add, Button ins)
        {
            var handler = (BoxExchangeEditor.GetDragHitItem(sender, e) == null ? add : ins);
            handler.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        private void button_Add(BoxExchangeEditor bx, List<string> src, bool isInsert = false)
        {
            int pos = bx.SourceBox.SelectedIndex - bx.SourceBox.SelectedItems.Count;
            bx.bxAddItems(bx.SourceBox, bx.TargetBox, isInsert);
            reLoadButtonItem(bx, src);
            if (bx.SourceBox.Items.Count != 0)
            {
                pos = Math.Max(0, Math.Min(pos, bx.SourceBox.Items.Count - 1));
                bx.SourceBox.SelectedIndex = pos;//順序がヘンだが、ENTERの場合はこの後に+1処理が入る模様
            }
        }
        private void button_Dell(BoxExchangeEditor bx, BoxExchangeEditor bx_other, List<string> src)
        {
            if (bx.TargetBox.SelectedItem == null) return;
            //
            var item1 = bx.TargetBox.SelectedItems.OfType<StringItem>().FirstOrDefault(item => item.Value == "設定");
            var item2 = bx_other.TargetBox.Items.OfType<StringItem>().FirstOrDefault(item => item.Value == "設定");
            if (item1 != null && item2 == null)
            {
                MessageBox.Show("設定は上部表示ボタンか右クリック表示項目のどちらかに必要です");
                return;
            }

            bx.bxDeleteItems(bx.TargetBox);
            reLoadButtonItem(bx, src);
        }
        private void button_btnIni_Click(object sender, RoutedEventArgs e)
        {
            listBox_viewBtn.Items.Clear();
            listBox_viewBtn.Items.AddItems(StringItem.Items(Settings.GetViewButtonDefIDs(CommonManager.Instance.NWMode)));
            reLoadButtonItem(bxb, buttonItem);
        }
        private void button_taskIni_Click(object sender, RoutedEventArgs e)
        {
            listBox_viewTask.Items.Clear();
            listBox_viewTask.Items.AddItems(StringItem.Items(Settings.GetTaskMenuDefIDs(CommonManager.Instance.NWMode)));
            reLoadButtonItem(bxt, taskItem);
        }
        private void reLoadButtonItem(BoxExchangeEditor bx, List<string> src)
        {
            var viewlist = bx.TargetBox.Items.OfType<StringItem>().Values();
            var diflist = src.Except(viewlist).ToList();
            diflist.Insert(0, (bx.DuplicationSpecific.First() as StringItem).Value);

            bx.SourceBox.ItemsSource = StringItem.Items(diflist.Distinct());
        }

        private void button_recDef_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SetRecPresetWindow(this, recPresetList);
            if (dlg.ShowDialog() == true)
            {
                recPresetList = dlg.GetPresetList();
            }
        }

        private void button_searchDef_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SetSearchPresetWindow(this, searchPresetList);
            if (dlg.ShowDialog() == true)
            {
                searchPresetList = dlg.GetPresetList();
            }
        }

        private void button_exe_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFileNameByDialog((sender as Button).DataContext as TextBox, false, "", ".exe");
        }

        private void ReLoadStation()
        {
            listBox_iEPG.Items.Clear();
            if (listBox_service.SelectedItem == null) return;
            //
            var key = (listBox_service.SelectedItem as ServiceViewItem).Key;
            listBox_iEPG.Items.AddItems(stationList.Where(item => item.Key == key));
        }

        //メモ:「更新」を追加するなら、IEPGStationInfoのコピーが必要になる
        private void button_add_iepg_Click(object sender, RoutedEventArgs e)
        {
            if (listBox_service.SelectedItem == null) return;
            //
            if (stationList.Any(info => info.StationName == textBox_station.Text) == true)
            {
                MessageBox.Show("すでに追加されています");
                return;
            }
            var key = (listBox_service.SelectedItem as ServiceViewItem).Key;
            stationList.Add(new IEPGStationInfo { StationName = textBox_station.Text, Key = key });
            ReLoadStation();
            listBox_iEPG.ScrollIntoViewLast();
        }

        private void button_del_iepg_Click(object sender, RoutedEventArgs e)
        {
            if (listBox_service.SelectedItem == null) return;
            //
            listBox_iEPG.SelectedItemsList().ForEach(item => stationList.Remove(item as IEPGStationInfo));
            ReLoadStation();
        }

        private void listBox_service_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReLoadStation();
        }

        private void button_shortCut_Click(object sender, RoutedEventArgs e)
        {
            button_shortCutClick(button_shortCut, Assembly.GetEntryAssembly().Location);
        }

        private void button_shortCutSrv_Click(object sender, RoutedEventArgs e)
        {
            button_shortCutClick(button_shortCutSrv, Path.Combine(SettingPath.ModulePath, "EpgTimerSrv.exe"));
        }

        private void button_shortCutClick(Button btn, string scLinkPath)
        {
            try
            {
                string shortcutPath = btn.Tag as string;
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                    btn.Content = "作成";
                }
                else
                {
                    CreateShortCut(shortcutPath, scLinkPath, "");
                    btn.Content = "削除";
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        /// <summary>
        /// ショートカットの作成
        /// </summary>
        /// <remarks>WSHを使用して、ショートカット(lnkファイル)を作成します。(遅延バインディング)</remarks>
        /// <param name="path">出力先のファイル名(*.lnk)</param>
        /// <param name="targetPath">対象のアセンブリ(*.exe)</param>
        /// <param name="description">説明</param>
        private void CreateShortCut(String path, String targetPath, String description)
        {
            //using System.Reflection;

            // WSHオブジェクトを作成し、CreateShortcutメソッドを実行する
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            object shell = Activator.CreateInstance(shellType);
            object shortCut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });

            Type shortcutType = shell.GetType();
            // TargetPathプロパティをセットする
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortCut, new object[] { targetPath });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortCut, new object[] { Path.GetDirectoryName(targetPath) });
            // Descriptionプロパティをセットする
            shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortCut, new object[] { description });
            // Saveメソッドを実行する
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortCut, null);
        }

        private void checkBox_autoDel_Click(object sender, RoutedEventArgs e)
        {
            bool chkEnabled = (bool)checkBox_autoDel.IsChecked;
            bool extEnabled = chkEnabled || (bool)checkBox_recInfoDelFile.IsChecked && (bool)checkBox_applyExtTo.IsChecked;
            textBox_ext.SetReadOnlyWithEffect(!extEnabled);
            button_ext_def.IsEnabled = extEnabled;
            button_ext_del.IsEnabled = extEnabled;
            button_ext_add.IsEnabled = extEnabled;
            textBox_chk_folder.SetReadOnlyWithEffect(!chkEnabled);
            button_chk_del.IsEnabled = chkEnabled;
            button_chk_add.IsEnabled = chkEnabled;
        }
        private void SetIsEnabledBlinkPreRec()
        {
            checkBox_blinkPreRec.IsEnabled = (bool)checkBox_srvResident.IsEnabled && (bool)checkBox_srvResident.IsChecked && (bool)checkBox_srvShowTray.IsChecked;
        }
    }
}
