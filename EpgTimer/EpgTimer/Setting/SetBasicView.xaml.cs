using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace EpgTimer.Setting
{
    using BoxExchangeEdit;

    /// <summary>
    /// SetBasicView.xaml の相互作用ロジック
    /// </summary>
    public partial class SetBasicView : UserControl
    {
        private ObservableCollection<EpgCaptime> timeList = new ObservableCollection<EpgCaptime>();

        public bool IsChangeSettingPath { get; private set; }

        public SetBasicView()
        {
            InitializeComponent();

            IsChangeSettingPath = false;

            if (CommonManager.Instance.NWMode == true)
            {
                ViewUtil.ChangeChildren(grid_folder, false);
                checkbox_OpenFolderWithFileDialog.IsEnabled = true;
                label1.IsEnabled = true;
                textBox_setPath.IsEnabled = true;
                button_setPath.IsEnabled = true;
                textBox_exe.SetReadOnlyWithEffect(true);
                button_exe.IsEnabled = true;
                textBox_cmdBon.SetReadOnlyWithEffect(true);
                listBox_recFolder.IsEnabled = true;
                textBox_recFolder.SetReadOnlyWithEffect(true);
                button_rec_open.IsEnabled = true;
                textBox_recInfoFolder.SetReadOnlyWithEffect(true);
                button_recInfoFolder.IsEnabled = true;
                listBox_bon.IsEnabled = true;

                ViewUtil.ChangeChildren(grid_epg, false);
                listView_service.IsEnabled = true;
                listView_time.IsEnabled = true;
                grid_ServiceOptions.IsEnabled = true;
                ViewUtil.ChangeChildren(grid_ServiceOptions, false);
                checkBox_showEpgCapServiceOnly.IsEnabled = true;

                tab_NW.Foreground = SystemColors.GrayTextBrush;
                checkBox_tcpServer.IsEnabled = false;
                ViewUtil.ChangeChildren(grid_tcpCtrl, false);
                textBox_tcpAcl.SetReadOnlyWithEffect(true);

                checkBox_httpServer.IsEnabled = false;
                ViewUtil.ChangeChildren(grid_httpCtrl, false);
                textBox_httpAcl.SetReadOnlyWithEffect(true);
                ViewUtil.ChangeChildren(grid_httpfolder, false);
                textBox_docrootPath.SetReadOnlyWithEffect(true);
                button_docrootPath.IsEnabled = true;
                checkBox_httpLog.IsEnabled = false;
                checkBox_dlnaServer.IsEnabled = false;
            }

            listBox_Button_Set();

            try
            {
                checkbox_OpenFolderWithFileDialog.IsChecked = Settings.Instance.OpenFolderWithFileDialog;
                checkbox_OpenFolderWithFileDialog.Click += (sender, e) =>
                    Settings.Instance.OpenFolderWithFileDialog = checkbox_OpenFolderWithFileDialog.IsChecked == true;

                textBox_setPath.Text = SettingPath.SettingFolderPath;
                textBox_exe.Text = SettingPath.EdcbExePath;

                string viewAppIniPath = SettingPath.ModulePath.TrimEnd('\\') + "\\ViewApp.ini";
                textBox_cmdBon.Text = IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Bon", "-d", viewAppIniPath);
                textBox_cmdMin.Text = IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Min", "-min", viewAppIniPath);
                textBox_cmdViewOff.Text = IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "ViewOff", "-noview", viewAppIniPath);

                Settings.Instance.DefRecFolders.ForEach(folder => listBox_recFolder.Items.Add(folder));
                textBox_recInfoFolder.Text = IniFileHandler.GetPrivateProfileFolder("SET", "RecInfoFolder", SettingPath.CommonIniPath);

                var tunerInfo = new List<KeyValuePair<Int32, TunerInfo>>();
                foreach (string fileName in CommonManager.GetBonFileList())
                {
                    var item = new TunerInfo(fileName);
                    item.TunerNum = IniFileHandler.GetPrivateProfileInt(item.BonDriver, "Count", 0, SettingPath.TimerSrvIniPath).ToString();
                    bool isEpgCap = (IniFileHandler.GetPrivateProfileInt(item.BonDriver, "GetEpg", 1, SettingPath.TimerSrvIniPath) != 0);
                    item.EPGNum = IniFileHandler.GetPrivateProfileInt(item.BonDriver, "EPGCount", 0, SettingPath.TimerSrvIniPath).ToString();
                    item.EPGNum = (isEpgCap == true && item.EPGNumInt == 0) ? "すべて" : item.EPGNum;
                    int priority = IniFileHandler.GetPrivateProfileInt(item.BonDriver, "Priority", 0xFFFF, SettingPath.TimerSrvIniPath);
                    tunerInfo.Add(new KeyValuePair<int, TunerInfo>(priority, item));
                }
                foreach (var item in tunerInfo.ToLookup(info => info.Key, info => info.Value).OrderBy(item => item.Key))
                {
                    listBox_bon.Items.AddItems(item);
                }
                if (listBox_bon.Items.Count > 0)
                {
                    listBox_bon.SelectedIndex = 0;
                }

                combo_bon_num.ItemsSource = Enumerable.Range(0, 100);
                combo_bon_epgnum.Items.Add("すべて");
                combo_bon_epgnum.Items.AddItems(Enumerable.Range(0, 100));

                comboBox_wday.ItemsSource = new string[] { "毎日" }.Concat(CommonManager.DayOfWeekArray);
                comboBox_wday.SelectedIndex = 0;
                comboBox_HH.ItemsSource = Enumerable.Range(0, 24);
                comboBox_HH.SelectedIndex = 0;
                comboBox_MM.ItemsSource = Enumerable.Range(0, 60);
                comboBox_MM.SelectedIndex = 0;

                listView_service.ItemsSource = ChSet5.ChListSorted.Select(info => new ServiceViewItem(info) { IsSelected = info.EpgCapFlag }).ToList();

                checkBox_bs.IsChecked = IniFileHandler.GetPrivateProfileInt("SET", "BSBasicOnly", 1, SettingPath.CommonIniPath) == 1;
                checkBox_cs1.IsChecked = IniFileHandler.GetPrivateProfileInt("SET", "CS1BasicOnly", 1, SettingPath.CommonIniPath) == 1;
                checkBox_cs2.IsChecked = IniFileHandler.GetPrivateProfileInt("SET", "CS2BasicOnly", 1, SettingPath.CommonIniPath) == 1;
                checkBox_cs3.IsChecked = IniFileHandler.GetPrivateProfileInt("SET", "CS3BasicOnly", 0, SettingPath.CommonIniPath) == 1;
                textBox_EpgCapTimeOut.Text = IniFileHandler.GetPrivateProfileInt("EPGCAP", "EpgCapTimeOut", 10, SettingPath.BonCtrlIniPath).ToString();
                checkBox_EpgCapSaveTimeOut.IsChecked = IniFileHandler.GetPrivateProfileBool("EPGCAP", "EpgCapSaveTimeOut", false, SettingPath.BonCtrlIniPath);
                checkBox_timeSync.IsChecked = IniFileHandler.GetPrivateProfileInt("SET", "TimeSync", 0, SettingPath.TimerSrvIniPath) == 1;
                checkBox_showEpgCapServiceOnly.IsChecked = Settings.Instance.ShowEpgCapServiceOnly;

                int capCount = IniFileHandler.GetPrivateProfileInt("EPG_CAP", "Count", int.MaxValue, SettingPath.TimerSrvIniPath);
                if (capCount == int.MaxValue)
                {
                    var item = new EpgCaptime();
                    item.IsSelected = true;
                    item.Time = "23:00";
                    item.BSBasicOnly = checkBox_bs.IsChecked == true;
                    item.CS1BasicOnly = checkBox_cs1.IsChecked == true;
                    item.CS2BasicOnly = checkBox_cs2.IsChecked == true;
                    item.CS3BasicOnly = checkBox_cs3.IsChecked == true;
                    timeList.Add(item);
                }
                else
                {
                    for (int i = 0; i < capCount; i++)
                    {
                        var item = new EpgCaptime();
                        item.Time = IniFileHandler.GetPrivateProfileString("EPG_CAP", i.ToString(), "", SettingPath.TimerSrvIniPath);
                        item.IsSelected = IniFileHandler.GetPrivateProfileInt("EPG_CAP", i.ToString() + "Select", 0, SettingPath.TimerSrvIniPath) == 1;

                        // 取得種別(bit0(LSB)=BS,bit1=CS1,bit2=CS2,bit3=CS3)。負値のときは共通設定に従う
                        int flags = IniFileHandler.GetPrivateProfileInt("EPG_CAP", i.ToString() + "BasicOnlyFlags", -1, SettingPath.TimerSrvIniPath);
                        item.BSBasicOnly = flags >= 0 ? (flags & 1) != 0 : checkBox_bs.IsChecked == true;
                        item.CS1BasicOnly = flags >= 0 ? (flags & 2) != 0 : checkBox_cs1.IsChecked == true;
                        item.CS2BasicOnly = flags >= 0 ? (flags & 4) != 0 : checkBox_cs2.IsChecked == true;
                        item.CS3BasicOnly = flags >= 0 ? (flags & 8) != 0 : checkBox_cs3.IsChecked == true;
                        timeList.Add(item);
                    }
                }
                listView_time.ItemsSource = timeList;

                textBox_ngCapMin.Text = IniFileHandler.GetPrivateProfileInt("SET", "NGEpgCapTime", 20, SettingPath.TimerSrvIniPath).ToString();
                textBox_ngTunerMin.Text = IniFileHandler.GetPrivateProfileInt("SET", "NGEpgCapTunerTime", 20, SettingPath.TimerSrvIniPath).ToString();

                // ネットワーク
                checkBox_tcpServer.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "EnableTCPSrv", false, SettingPath.TimerSrvIniPath);
                textBox_tcpPort.Text = IniFileHandler.GetPrivateProfileInt("SET", "TCPPort", 4510, SettingPath.TimerSrvIniPath).ToString();
                textBox_tcpAcl.Text = IniFileHandler.GetPrivateProfileString("SET", "TCPAccessControlList", "+127.0.0.1,+192.168.0.0/16", SettingPath.TimerSrvIniPath);
                textBox_tcpResTo.Text = IniFileHandler.GetPrivateProfileInt("SET", "TCPResponseTimeoutSec", 120, SettingPath.TimerSrvIniPath).ToString();

                int enableHttpSrv = IniFileHandler.GetPrivateProfileInt("SET", "EnableHttpSrv", 0, SettingPath.TimerSrvIniPath);
                checkBox_httpServer.IsChecked = enableHttpSrv != 0;
                checkBox_httpLog.IsChecked = enableHttpSrv == 2;

                textBox_httpPort.Text = IniFileHandler.GetPrivateProfileInt("SET", "HttpPort", 5510, SettingPath.TimerSrvIniPath).ToString();
                textBox_httpAcl.Text = IniFileHandler.GetPrivateProfileString("SET", "HttpAccessControlList", "+127.0.0.1", SettingPath.TimerSrvIniPath);
                textBox_httpTimeout.Text = IniFileHandler.GetPrivateProfileInt("SET", "HttpRequestTimeoutSec", 120, SettingPath.TimerSrvIniPath).ToString();
                textBox_httpThreads.Text = IniFileHandler.GetPrivateProfileInt("SET", "HttpNumThreads", 5, SettingPath.TimerSrvIniPath).ToString();
                textBox_docrootPath.Text = IniFileHandler.GetPrivateProfileString("SET", "HttpPublicFolder", System.IO.Path.Combine(SettingPath.ModulePath, "HttpPublic"), SettingPath.TimerSrvIniPath);
                checkBox_dlnaServer.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "EnableDMS", false, SettingPath.TimerSrvIniPath);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public void SaveSetting()
        {
            try
            {
                string setPath = SettingPath.CheckFolder(textBox_setPath.Text);
                setPath = setPath == "" ? SettingPath.DefSettingFolderPath : setPath;
                System.IO.Directory.CreateDirectory(setPath);

                IsChangeSettingPath = string.Compare(setPath, SettingPath.SettingFolderPath, true) != 0;
                SettingPath.SettingFolderPath = setPath;

                IniFileHandler.WritePrivateProfileString("SET", "RecExePath",
                    string.Compare(textBox_exe.Text, SettingPath.ModulePath.TrimEnd('\\') + "\\EpgDataCap_Bon.exe", true) == 0 ? null : textBox_exe.Text, SettingPath.CommonIniPath);

                string viewAppIniPath = SettingPath.ModulePath.TrimEnd('\\') + "\\ViewApp.ini";
                if (IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Bon", "-d", viewAppIniPath) != textBox_cmdBon.Text)
                {
                    IniFileHandler.WritePrivateProfileString("APP_CMD_OPT", "Bon", textBox_cmdBon.Text, viewAppIniPath);
                }
                if (IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Min", "-min", viewAppIniPath) != textBox_cmdMin.Text)
                {
                    IniFileHandler.WritePrivateProfileString("APP_CMD_OPT", "Min", textBox_cmdMin.Text, viewAppIniPath);
                }
                if (IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "ViewOff", "-noview", viewAppIniPath) != textBox_cmdViewOff.Text)
                {
                    IniFileHandler.WritePrivateProfileString("APP_CMD_OPT", "ViewOff", textBox_cmdViewOff.Text, viewAppIniPath);
                }

                List<String> recFolderList = ViewUtil.GetFolderList(listBox_recFolder);
                int recFolderCount = recFolderList.Count == 1 &&
                    string.Compare(recFolderList[0], SettingPath.SettingFolderPath, true) == 0 ? 0 : recFolderList.Count;
                IniFileHandler.WritePrivateProfileString("SET", "RecFolderNum", recFolderCount.ToString(), SettingPath.CommonIniPath);
                IniFileHandler.DeletePrivateProfileNumberKeys("SET", SettingPath.CommonIniPath, "RecFolderPath");
                for (int i = 0; i < recFolderCount; i++)
                {
                    IniFileHandler.WritePrivateProfileString("SET", "RecFolderPath" + i.ToString(), recFolderList[i], SettingPath.CommonIniPath);
                }

                var recInfoFolder = SettingPath.CheckFolder(textBox_recInfoFolder.Text);
                IniFileHandler.WritePrivateProfileString("SET", "RecInfoFolder", recInfoFolder, "", SettingPath.CommonIniPath);

                for (int i = 0; i < listBox_bon.Items.Count; i++)
                {
                    var info = listBox_bon.Items[i] as TunerInfo;
                    IniFileHandler.WritePrivateProfileString(info.BonDriver, "Count", info.TunerNumInt.ToString(), SettingPath.TimerSrvIniPath);
                    IniFileHandler.WritePrivateProfileString(info.BonDriver, "GetEpg", info.EPGNum == "0" ? "0" : "1", SettingPath.TimerSrvIniPath);
                    IniFileHandler.WritePrivateProfileString(info.BonDriver, "EPGCount", info.EPGNumInt >= info.TunerNumInt ? "0" : info.EPGNumInt.ToString(), SettingPath.TimerSrvIniPath);
                    IniFileHandler.WritePrivateProfileString(info.BonDriver, "Priority", i.ToString(), SettingPath.TimerSrvIniPath);
                }

                IniFileHandler.WritePrivateProfileString("SET", "BSBasicOnly", checkBox_bs.IsChecked == true ? "1" : "0", SettingPath.CommonIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "CS1BasicOnly", checkBox_cs1.IsChecked == true ? "1" : "0", SettingPath.CommonIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "CS2BasicOnly", checkBox_cs2.IsChecked == true ? "1" : "0", SettingPath.CommonIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "CS3BasicOnly", checkBox_cs3.IsChecked == true ? "1" : "0", SettingPath.CommonIniPath);
                IniFileHandler.WritePrivateProfileString("EPGCAP", "EpgCapTimeOut", textBox_EpgCapTimeOut.Text, SettingPath.BonCtrlIniPath);
                IniFileHandler.WritePrivateProfileString("EPGCAP", "EpgCapSaveTimeOut", checkBox_EpgCapSaveTimeOut.IsChecked == true ? "1" : "0", SettingPath.BonCtrlIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "TimeSync", checkBox_timeSync.IsChecked, SettingPath.TimerSrvIniPath);
                Settings.Instance.ShowEpgCapServiceOnly = (bool)checkBox_showEpgCapServiceOnly.IsChecked;

                foreach (ServiceViewItem info in listView_service.ItemsSource)
                {
                    if (ChSet5.ChList.ContainsKey(info.Key) == true)//変更中に更新される場合があるため
                    {
                        ChSet5.ChList[info.Key].EpgCapFlag = info.IsSelected;
                    }
                }

                IniFileHandler.WritePrivateProfileString("EPG_CAP", "Count", timeList.Count.ToString(), SettingPath.TimerSrvIniPath);
                IniFileHandler.DeletePrivateProfileNumberKeys("EPG_CAP", SettingPath.TimerSrvIniPath);
                IniFileHandler.DeletePrivateProfileNumberKeys("EPG_CAP", SettingPath.TimerSrvIniPath, "", "Select");
                IniFileHandler.DeletePrivateProfileNumberKeys("EPG_CAP", SettingPath.TimerSrvIniPath, "", "BasicOnlyFlags");
                for (int i = 0; i < timeList.Count; i++)
                {
                    var item = timeList[i] as EpgCaptime;
                    IniFileHandler.WritePrivateProfileString("EPG_CAP", i.ToString(), item.Time, SettingPath.TimerSrvIniPath);
                    IniFileHandler.WritePrivateProfileString("EPG_CAP", i.ToString() + "Select", item.IsSelected == true ? "1" : "0", SettingPath.TimerSrvIniPath);
                    int flags = (item.BSBasicOnly ? 1 : 0) | (item.CS1BasicOnly ? 2 : 0) | (item.CS2BasicOnly ? 4 : 0) | (item.CS3BasicOnly ? 8 : 0);
                    IniFileHandler.WritePrivateProfileString("EPG_CAP", i.ToString() + "BasicOnlyFlags", flags.ToString(), SettingPath.TimerSrvIniPath);
                }

                IniFileHandler.WritePrivateProfileString("SET", "NGEpgCapTime", textBox_ngCapMin.Text, SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "NGEpgCapTunerTime", textBox_ngTunerMin.Text, SettingPath.TimerSrvIniPath);

                // ネットワーク
                IniFileHandler.WritePrivateProfileString("SET", "EnableTCPSrv", checkBox_tcpServer.IsChecked, false, SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "TCPPort", textBox_tcpPort.Text, "4510", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "TCPAccessControlList", textBox_tcpAcl.Text, "+127.0.0.1,+192.168.0.0/16", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "TCPResponseTimeoutSec", textBox_tcpResTo.Text, "120", SettingPath.TimerSrvIniPath);

                var enableHttpSrv = checkBox_httpServer.IsChecked != true ? null : checkBox_httpLog.IsChecked != true ? "1" : "2";
                IniFileHandler.WritePrivateProfileString("SET", "EnableHttpSrv", enableHttpSrv, SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "HttpPort", textBox_httpPort.Text, "5510", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "HttpAccessControlList", textBox_httpAcl.Text, "+127.0.0.1", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "HttpRequestTimeoutSec", textBox_httpTimeout.Text, "120", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "HttpNumThreads", textBox_httpThreads.Text, "5", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "HttpPublicFolder", textBox_docrootPath.Text, System.IO.Path.Combine(SettingPath.ModulePath, "HttpPublic"), SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "EnableDMS", checkBox_dlnaServer.IsChecked, false, SettingPath.TimerSrvIniPath);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        private void button_setPath_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFolderNameByDialog(textBox_setPath, "設定関係保存フォルダの選択");
        }
        private void button_exe_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFileNameByDialog(textBox_exe, false, "", ".exe", true);
        }
        private void button_recInfoFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFolderNameByDialog(textBox_recInfoFolder, "録画情報保存フォルダの選択", true);
        }

        private void listBox_Button_Set()
        {
            //エスケープキャンセルだけは常に有効にする。
            var bxr = new BoxExchangeEditor(null, this.listBox_recFolder, true);
            var bxb = new BoxExchangeEditor(null, this.listBox_bon, true);
            var bxt = new BoxExchangeEditor(null, this.listView_time, true);
            new BoxExchangeEditor(null, this.listView_service, true);

            bxr.TargetBox.SelectionChanged += ViewUtil.ListBox_TextBoxSyncSelectionChanged(bxr.TargetBox, textBox_recFolder);
            bxr.TargetBox.KeyDown += ViewUtil.KeyDown_Enter(button_rec_open);
            bxr.targetBoxAllowDoubleClick(bxr.TargetBox, (sender, e) => button_rec_open.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));

            if (CommonManager.Instance.NWMode == false)
            {
                //録画設定関係
                bxr.AllowDragDrop();
                bxr.AllowKeyAction();
                button_rec_up.Click += new RoutedEventHandler(bxr.button_Up_Click);
                button_rec_down.Click += new RoutedEventHandler(bxr.button_Down_Click);
                button_rec_del.Click += new RoutedEventHandler(bxr.button_Delete_Click);
                button_rec_add.Click += (sender, e) => textBox_recFolder.Text = SettingPath.CheckFolder(textBox_recFolder.Text);
                button_rec_add.Click += ViewUtil.ListBox_TextCheckAdd(listBox_recFolder, textBox_recFolder);
                textBox_recFolder.KeyDown += ViewUtil.KeyDown_Enter(button_rec_add);

                //チューナ関係関係
                bxb.AllowDragDrop();
                button_bon_up.Click += new RoutedEventHandler(bxb.button_Up_Click);
                button_bon_down.Click += new RoutedEventHandler(bxb.button_Down_Click);

                //EPG取得関係
                bxt.TargetItemsSource = timeList;
                bxt.AllowDragDrop();
                bxt.AllowKeyAction();
                button_upTime.Click += new RoutedEventHandler(bxt.button_Up_Click);
                button_downTime.Click += new RoutedEventHandler(bxt.button_Down_Click);
                button_delTime.Click += new RoutedEventHandler(bxt.button_Delete_Click);
                SelectableItem.Set_CheckBox_PreviewChanged(listView_time);

                SelectableItem.Set_CheckBox_PreviewChanged(listView_service);
            }
        }

        private void button_rec_open_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFolderNameByDialog(textBox_recFolder, "録画フォルダの選択", true);
        }

        private void button_allChk_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem info in listView_service.ItemsSource) info.IsSelected = true;
        }
        private void button_videoChk_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem info in listView_service.ItemsSource) info.IsSelected = info.ServiceInfo.IsVideo == true;
        }
        private void button_allClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem info in listView_service.ItemsSource) info.IsSelected = false;
        }

        private void button_addTime_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboBox_HH.SelectedItem != null && comboBox_MM.SelectedItem != null)
                {
                    int hh = comboBox_HH.SelectedIndex;
                    int mm = comboBox_MM.SelectedIndex;
                    String time = hh.ToString("D2") + ":" + mm.ToString("D2");
                    int wday = comboBox_wday.SelectedIndex;
                    if (1 <= wday && wday <= 7)
                    {
                        // 曜日指定接尾辞(w1=Mon,...,w7=Sun)
                        time += "w" + ((wday + 5) % 7 + 1);
                    }

                    if (timeList.Any(info => String.Compare(info.Time, time, true) == 0) == true)
                    { return; }

                    var item = new EpgCaptime();
                    item.IsSelected = true;
                    item.Time = time;
                    item.BSBasicOnly = checkBox_bs.IsChecked == true;
                    item.CS1BasicOnly = checkBox_cs1.IsChecked == true;
                    item.CS2BasicOnly = checkBox_cs2.IsChecked == true;
                    item.CS3BasicOnly = checkBox_cs3.IsChecked == true;
                    timeList.Add(item);
                    listView_time.ScrollIntoViewLast();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        private void button_docrootPath_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFolderNameByDialog(textBox_docrootPath, "WebUI公開フォルダの選択");
        }
    }

    //BonDriver一覧の表示・設定用クラス
    public class TunerInfo
    {
        public TunerInfo(string bon) { BonDriver = bon; }
        public String BonDriver { get; set; }
        public String TunerNum { get; set; }
        public String EPGNum { get; set; }
        public UInt32 TunerNumInt { get { return ToUInt(TunerNum); } }
        public UInt32 EPGNumInt { get { return ToUInt(EPGNum); } }
        public override string ToString() { return BonDriver; }
        private UInt32 ToUInt(string s)
        {
            UInt32 val = 0;
            UInt32.TryParse(s, out val);
            return val;
        }
    }

    //Epg取得情報の表示・設定用クラス
    public class EpgCaptime : SelectableItemNWMode
    {
        public string Time { get; set; }
        public bool BSBasicOnly { get; set; }
        public bool CS1BasicOnly { get; set; }
        public bool CS2BasicOnly { get; set; }
        public bool CS3BasicOnly { get; set; }
        public string ViewTime { get { return Time.Substring(0, 5); } }//曜日情報は削除
        public string ViewBasicOnly { get { return (BSBasicOnly ? "基" : "詳") + "," + (CS1BasicOnly ? "基" : "詳") + "," + (CS2BasicOnly ? "基" : "詳") + "," + (CS3BasicOnly ? "基" : "詳"); } }
        public string WeekDay
        {
            get
            {
                int i = Time.IndexOf('w');
                if (i < 0) return "";
                //
                uint wday;
                uint.TryParse(Time.Substring(i + 1), out wday);
                return "日月火水木金土"[(int)(wday % 7)].ToString();
            }
        }
    }
}
