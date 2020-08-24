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
        private Settings settings { get { return (Settings)DataContext; } }
        public bool IsChangeSettingPath { get; private set; }

        public SetBasicView()
        {
            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                ViewUtil.SetIsEnabledChildren(grid_folder, false);
                checkbox_OpenFolderWithFileDialog.IsEnabled = true;
                label1.IsEnabled = true;
                textBox_setPath.IsEnabled = true;
                button_setPath.IsEnabled = true;
                textBox_exe.SetReadOnlyWithEffect(true);
                button_exe.IsEnabled = true;
                textBox_cmdBon.SetReadOnlyWithEffect(true);
                label_recFolder.ToolTip = "未設定の場合は(EpgTimerSrv側の)「設定関係保存フォルダ」がデフォルトになります";
                listBox_recFolder.IsEnabled = true;
                textBox_recFolder.SetReadOnlyWithEffect(true);
                button_rec_open.IsEnabled = true;
                textBox_recInfoFolder.SetReadOnlyWithEffect(true);
                button_recInfoFolder.IsEnabled = true;
                listBox_bon.IsEnabled = true;

                ViewUtil.SetIsEnabledChildren(grid_epg, false);
                ServiceListHeader.IsEnabled = true;
                listView_service.IsEnabled = true;
                listView_time.IsEnabled = true;
                ViewUtil.SetIsEnabledChildren(grid_ServiceOptions, false);
                checkBox_showEpgCapServiceOnly.IsEnabled = true;
                checkBox_SortServiceList.IsEnabled = true;

                tab_NW.Foreground = SystemColors.GrayTextBrush;
                ViewUtil.SetIsEnabledChildren(grid_tcpServer, false);
                ViewUtil.SetIsEnabledChildren(grid_tcpCtrl, false);
                textBox_tcpAcl.SetReadOnlyWithEffect(true);

                checkBox_httpServer.IsEnabled = false;
                ViewUtil.SetIsEnabledChildren(grid_httpCtrl, false);
                textBox_httpAcl.SetReadOnlyWithEffect(true);
                ViewUtil.SetIsEnabledChildren(grid_httpfolder, false);
                textBox_docrootPath.SetReadOnlyWithEffect(true);
                button_docrootPath.IsEnabled = true;
                checkBox_httpLog.IsEnabled = false;
                checkBox_dlnaServer.IsEnabled = false;
            }

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
                button_rec_up.Click += bxr.button_Up_Click;
                button_rec_down.Click += bxr.button_Down_Click;
                button_rec_del.Click += bxr.button_Delete_Click;
                button_rec_add.Click += (sender, e) => textBox_recFolder.Text = SettingPath.CheckFolder(textBox_recFolder.Text);
                button_rec_add.Click += ViewUtil.ListBox_TextCheckAdd(listBox_recFolder, textBox_recFolder);
                textBox_recFolder.KeyDown += ViewUtil.KeyDown_Enter(button_rec_add);

                //チューナ関係関係
                bxb.AllowDragDrop();
                button_bon_up.Click += bxb.button_Up_Click;
                button_bon_down.Click += bxb.button_Down_Click;

                //EPG取得関係
                bxt.AllowDragDrop();
                bxt.AllowKeyAction();
                button_upTime.Click += bxt.button_Up_Click;
                button_downTime.Click += bxt.button_Down_Click;
                button_delTime.Click += bxt.button_Delete_Click;
                SelectableItem.Set_CheckBox_PreviewChanged(listView_time);
                SelectableItem.Set_CheckBox_PreviewChanged(listView_service);
            }

            //これは即時反映。DataContextとSettings.Instanceを両方書き換える。
            checkbox_OpenFolderWithFileDialog.Click += (sender, e) =>
                Settings.Instance.OpenFolderWithFileDialog = checkbox_OpenFolderWithFileDialog.IsChecked == true;

            button_setPath.Click += ViewUtil.OpenFolderNameDialog(textBox_setPath, "設定関係保存フォルダの選択");
            button_exe.Click += ViewUtil.OpenFileNameDialog(textBox_exe, false, "", ".exe", true);
            button_recInfoFolder.Click += ViewUtil.OpenFolderNameDialog(textBox_recInfoFolder, "録画情報保存フォルダの選択", true);
            button_rec_open.Click += ViewUtil.OpenFolderNameDialog(textBox_recFolder, "録画フォルダの選択", true);
            button_docrootPath.Click += ViewUtil.OpenFolderNameDialog(textBox_docrootPath, "WebUI公開フォルダの選択");

            combo_bon_num.ItemsSource = Enumerable.Range(0, 100);
            combo_bon_epgnum.Items.Add("すべて");
            combo_bon_epgnum.Items.AddItems(Enumerable.Range(0, 100));

            comboBox_wday.ItemsSource = new[] { "毎日" }.Concat(CommonManager.DayOfWeekArray);
            comboBox_wday.SelectedIndex = 0;
            comboBox_HH.ItemsSource = Enumerable.Range(0, 24);
            comboBox_HH.SelectedIndex = 0;
            comboBox_MM.ItemsSource = Enumerable.Range(0, 60);
            comboBox_MM.SelectedIndex = 0;
        }

        public void LoadSetting()
        {
            textBox_setPath.Text = SettingPath.SettingFolderPath;
            textBox_exe.Text = SettingPath.EdcbExePath;
            textBox_cmdBon.Text = IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Bon", "-d", SettingPath.ViewAppIniPath);
            textBox_cmdMin.Text = IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Min", "-min", SettingPath.ViewAppIniPath);
            textBox_cmdViewOff.Text = IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "ViewOff", "-noview", SettingPath.ViewAppIniPath);

            listBox_recFolder.Items.Clear();
            listBox_recFolder.Items.AddItems(settings.DefRecFolders);
            textBox_recInfoFolder.Text = IniFileHandler.GetPrivateProfileFolder("SET", "RecInfoFolder", SettingPath.CommonIniPath);

            listBox_bon.Items.Clear();
            listBox_bon.Items.AddItems(CommonManager.GetBonFileList().Select(fileName =>
            {
                var item = new TunerInfo(fileName);
                item.TunerNum = IniFileHandler.GetPrivateProfileInt(item.BonDriver, "Count", 0, SettingPath.TimerSrvIniPath).ToString();
                bool isEpgCap = IniFileHandler.GetPrivateProfileBool(item.BonDriver, "GetEpg", true, SettingPath.TimerSrvIniPath);
                int epgNum = IniFileHandler.GetPrivateProfileInt(item.BonDriver, "EPGCount", 0, SettingPath.TimerSrvIniPath);
                item.EPGNum = (isEpgCap == true && epgNum == 0) ? "すべて" : epgNum.ToString();
                item.Priority = IniFileHandler.GetPrivateProfileInt(item.BonDriver, "Priority", 0xFFFF, SettingPath.TimerSrvIniPath);
                return item;
            }).OrderBy(item => item.Priority));
            listBox_bon.SelectedIndex = 0;

            listView_service.ItemsSource = ChSet5.ChListSorted.Select(info => new ServiceViewItem(info) { IsSelected = info.EpgCapFlag });

            checkBox_bs.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "BSBasicOnly", true, SettingPath.CommonIniPath);
            checkBox_cs1.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "CS1BasicOnly", true, SettingPath.CommonIniPath);
            checkBox_cs2.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "CS2BasicOnly", true, SettingPath.CommonIniPath);
            checkBox_sp.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "CS3BasicOnly", false, SettingPath.CommonIniPath);
            textBox_EpgCapTimeOut.Text = IniFileHandler.GetPrivateProfileInt("EPGCAP", "EpgCapTimeOut", 10, SettingPath.BonCtrlIniPath).ToString();
            checkBox_EpgCapSaveTimeOut.IsChecked = IniFileHandler.GetPrivateProfileBool("EPGCAP", "EpgCapSaveTimeOut", false, SettingPath.BonCtrlIniPath);
            checkBox_timeSync.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "TimeSync", false, SettingPath.TimerSrvIniPath);

            listView_time.Items.Clear();
            int capCount = IniFileHandler.GetPrivateProfileInt("EPG_CAP", "Count", int.MaxValue, SettingPath.TimerSrvIniPath);
            if (capCount == int.MaxValue)
            {
                var item = new EpgCaptime();
                item.IsSelected = true;
                item.Time = "23:00";
                item.BSBasicOnly = (bool)checkBox_bs.IsChecked;
                item.CS1BasicOnly = (bool)checkBox_cs1.IsChecked;
                item.CS2BasicOnly = (bool)checkBox_cs2.IsChecked;
                item.SPBasicOnly = (bool)checkBox_sp.IsChecked;
                listView_time.Items.Add(item);
            }
            else
            {
                for (int i = 0; i < capCount; i++)
                {
                    var item = new EpgCaptime();
                    item.Time = IniFileHandler.GetPrivateProfileString("EPG_CAP", i.ToString(), "", SettingPath.TimerSrvIniPath);
                    item.IsSelected = IniFileHandler.GetPrivateProfileBool("EPG_CAP", i.ToString() + "Select", false, SettingPath.TimerSrvIniPath);

                    // 取得種別(bit0(LSB)=BS,bit1=CS1,bit2=CS2,bit3=スカパー)。負値のときは共通設定に従う
                    int flags = IniFileHandler.GetPrivateProfileInt("EPG_CAP", i.ToString() + "BasicOnlyFlags", -1, SettingPath.TimerSrvIniPath);
                    item.BSBasicOnly = flags >= 0 ? (flags & 1) != 0 : (bool)checkBox_bs.IsChecked;
                    item.CS1BasicOnly = flags >= 0 ? (flags & 2) != 0 : (bool)checkBox_cs1.IsChecked;
                    item.CS2BasicOnly = flags >= 0 ? (flags & 4) != 0 : (bool)checkBox_cs2.IsChecked;
                    item.SPBasicOnly = flags >= 0 ? (flags & 8) != 0 : (bool)checkBox_sp.IsChecked;
                    listView_time.Items.Add(item);
                }
            }

            textBox_ngCapMin.Text = IniFileHandler.GetPrivateProfileInt("SET", "NGEpgCapTime", 20, SettingPath.TimerSrvIniPath).ToString();
            textBox_ngTunerMin.Text = IniFileHandler.GetPrivateProfileInt("SET", "NGEpgCapTunerTime", 20, SettingPath.TimerSrvIniPath).ToString();

            // ネットワーク
            checkBox_tcpServer.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "EnableTCPSrv", false, SettingPath.TimerSrvIniPath);
            checkBox_tcpIPv6.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "TCPIPv6", false, SettingPath.TimerSrvIniPath);
            textBox_tcpPort.Text = IniFileHandler.GetPrivateProfileInt("SET", "TCPPort", 4510, SettingPath.TimerSrvIniPath).ToString();
            textBox_tcpAcl.Text = IniFileHandler.GetPrivateProfileString("SET", "TCPAccessControlList", "+127.0.0.1,+192.168.0.0/16", SettingPath.TimerSrvIniPath);
            textBox_tcpResTo.Text = IniFileHandler.GetPrivateProfileInt("SET", "TCPResponseTimeoutSec", 120, SettingPath.TimerSrvIniPath).ToString();

            int enableHttpSrv = IniFileHandler.GetPrivateProfileInt("SET", "EnableHttpSrv", 0, SettingPath.TimerSrvIniPath);
            checkBox_httpServer.IsChecked = enableHttpSrv != 0;
            checkBox_httpLog.IsChecked = enableHttpSrv == 2;

            textBox_httpPort.Text = IniFileHandler.GetPrivateProfileString("SET", "HttpPort", "5510", SettingPath.TimerSrvIniPath);
            textBox_httpAcl.Text = IniFileHandler.GetPrivateProfileString("SET", "HttpAccessControlList", "+127.0.0.1,+::1,+::ffff:127.0.0.1", SettingPath.TimerSrvIniPath);
            textBox_httpTimeout.Text = IniFileHandler.GetPrivateProfileInt("SET", "HttpRequestTimeoutSec", 120, SettingPath.TimerSrvIniPath).ToString();
            textBox_httpThreads.Text = IniFileHandler.GetPrivateProfileInt("SET", "HttpNumThreads", 5, SettingPath.TimerSrvIniPath).ToString();
            textBox_docrootPath.Text = IniFileHandler.GetPrivateProfileString("SET", "HttpPublicFolder", SettingPath.DefHttpPublicPath, SettingPath.TimerSrvIniPath);
            checkBox_dlnaServer.IsChecked = IniFileHandler.GetPrivateProfileBool("SET", "EnableDMS", false, SettingPath.TimerSrvIniPath);
        }

        public void SaveSetting()
        {
            string org_setPath = SettingPath.SettingFolderPath;
            SettingPath.SettingFolderPath = textBox_setPath.Text;
            System.IO.Directory.CreateDirectory(SettingPath.SettingFolderPath);
            IsChangeSettingPath = org_setPath.Equals(SettingPath.SettingFolderPath, StringComparison.OrdinalIgnoreCase) == false;

            SettingPath.EdcbExePath = textBox_exe.Text;

            //同じ値の時は書き込まない
            if (IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Bon", "-d", SettingPath.ViewAppIniPath) != textBox_cmdBon.Text)
            {
                IniFileHandler.WritePrivateProfileString("APP_CMD_OPT", "Bon", textBox_cmdBon.Text, SettingPath.ViewAppIniPath);
            }
            if (IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "Min", "-min", SettingPath.ViewAppIniPath) != textBox_cmdMin.Text)
            {
                IniFileHandler.WritePrivateProfileString("APP_CMD_OPT", "Min", textBox_cmdMin.Text, SettingPath.ViewAppIniPath);
            }
            if (IniFileHandler.GetPrivateProfileString("APP_CMD_OPT", "ViewOff", "-noview", SettingPath.ViewAppIniPath) != textBox_cmdViewOff.Text)
            {
                IniFileHandler.WritePrivateProfileString("APP_CMD_OPT", "ViewOff", textBox_cmdViewOff.Text, SettingPath.ViewAppIniPath);
            }

            settings.DefRecFolders = ViewUtil.GetFolderList(listBox_recFolder);
            IniFileHandler.WritePrivateProfileString("SET", "RecInfoFolder", SettingPath.CheckFolder(textBox_recInfoFolder.Text), "", SettingPath.CommonIniPath);

            for (int i = 0; i < listBox_bon.Items.Count; i++)
            {
                var info = listBox_bon.Items[i] as TunerInfo;
                IniFileHandler.WritePrivateProfileString(info.BonDriver, "Count", info.TunerNumInt, SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString(info.BonDriver, "GetEpg", info.EPGNum != "0", SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString(info.BonDriver, "EPGCount", info.EPGNumInt >= info.TunerNumInt ? 0 : info.EPGNumInt, SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString(info.BonDriver, "Priority", i, SettingPath.TimerSrvIniPath);
            }

            IniFileHandler.WritePrivateProfileString("SET", "BSBasicOnly", checkBox_bs.IsChecked, SettingPath.CommonIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "CS1BasicOnly", checkBox_cs1.IsChecked, SettingPath.CommonIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "CS2BasicOnly", checkBox_cs2.IsChecked, SettingPath.CommonIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "CS3BasicOnly", checkBox_sp.IsChecked, SettingPath.CommonIniPath);
            IniFileHandler.WritePrivateProfileString("EPGCAP", "EpgCapTimeOut", textBox_EpgCapTimeOut.Text, SettingPath.BonCtrlIniPath);
            IniFileHandler.WritePrivateProfileString("EPGCAP", "EpgCapSaveTimeOut", checkBox_EpgCapSaveTimeOut.IsChecked, SettingPath.BonCtrlIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "TimeSync", checkBox_timeSync.IsChecked, SettingPath.TimerSrvIniPath);

            foreach (ServiceViewItem info in listView_service.Items)
            {
                //変更中に更新される場合があるため
                ChSet5.ChItem(info.Key, true).EpgCapFlag = info.IsSelected;
            }

            IniFileHandler.WritePrivateProfileString("EPG_CAP", "Count", listView_time.Items.Count, SettingPath.TimerSrvIniPath);
            IniFileHandler.DeletePrivateProfileNumberKeys("EPG_CAP", SettingPath.TimerSrvIniPath);
            IniFileHandler.DeletePrivateProfileNumberKeys("EPG_CAP", SettingPath.TimerSrvIniPath, "", "Select");
            IniFileHandler.DeletePrivateProfileNumberKeys("EPG_CAP", SettingPath.TimerSrvIniPath, "", "BasicOnlyFlags");
            for (int i = 0; i < listView_time.Items.Count; i++)
            {
                var item = listView_time.Items[i] as EpgCaptime;
                IniFileHandler.WritePrivateProfileString("EPG_CAP", i.ToString(), item.Time, SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("EPG_CAP", i.ToString() + "Select", item.IsSelected, SettingPath.TimerSrvIniPath);
                int flags = (item.BSBasicOnly ? 1 : 0) | (item.CS1BasicOnly ? 2 : 0) | (item.CS2BasicOnly ? 4 : 0) | (item.SPBasicOnly ? 8 : 0);
                IniFileHandler.WritePrivateProfileString("EPG_CAP", i.ToString() + "BasicOnlyFlags", flags, SettingPath.TimerSrvIniPath);
            }

            IniFileHandler.WritePrivateProfileString("SET", "NGEpgCapTime", textBox_ngCapMin.Text, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "NGEpgCapTunerTime", textBox_ngTunerMin.Text, SettingPath.TimerSrvIniPath);

            // ネットワーク
            IniFileHandler.WritePrivateProfileString("SET", "EnableTCPSrv", checkBox_tcpServer.IsChecked, false, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "TCPIPv6", checkBox_tcpIPv6.IsChecked, false, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "TCPPort", textBox_tcpPort.Text, "4510", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "TCPAccessControlList", textBox_tcpAcl.Text, "+127.0.0.1,+192.168.0.0/16", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "TCPResponseTimeoutSec", textBox_tcpResTo.Text, "120", SettingPath.TimerSrvIniPath);

            var enableHttpSrv = checkBox_httpServer.IsChecked != true ? null : checkBox_httpLog.IsChecked != true ? "1" : "2";
            IniFileHandler.WritePrivateProfileString("SET", "EnableHttpSrv", enableHttpSrv, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "HttpPort", textBox_httpPort.Text, "5510", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "HttpAccessControlList", textBox_httpAcl.Text, "+127.0.0.1", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "HttpRequestTimeoutSec", textBox_httpTimeout.Text, "120", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "HttpNumThreads", textBox_httpThreads.Text, "5", SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "HttpPublicFolder", textBox_docrootPath.Text, SettingPath.DefHttpPublicPath, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "EnableDMS", checkBox_dlnaServer.IsChecked, false, SettingPath.TimerSrvIniPath);
        }

        private void button_allChk_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem info in listView_service.Items) info.IsSelected = true;
        }
        private void button_videoChk_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem info in listView_service.Items) info.IsSelected = info.ServiceInfo.IsVideo;
        }
        private void button_allClear_Click(object sender, RoutedEventArgs e)
        {
            foreach (ServiceViewItem info in listView_service.Items) info.IsSelected = false;
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

                    if (listView_time.Items.Cast<EpgCaptime>().Any(info => info.Time.Equals(time, StringComparison.OrdinalIgnoreCase) == true) == true)
                    { return; }

                    var item = new EpgCaptime();
                    item.IsSelected = true;
                    item.Time = time;
                    item.BSBasicOnly = (bool)checkBox_bs.IsChecked;
                    item.CS1BasicOnly = (bool)checkBox_cs1.IsChecked;
                    item.CS2BasicOnly = (bool)checkBox_cs2.IsChecked;
                    item.SPBasicOnly = (bool)checkBox_sp.IsChecked;
                    listView_time.ScrollIntoViewLast(item);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
    }

    //BonDriver一覧の表示・設定用クラス
    public class TunerInfo
    {
        public TunerInfo(string bon) { BonDriver = bon; }
        public String BonDriver { get; set; }
        public String TunerNum { get; set; }
        public UInt32 TunerNumInt { get { return ToUInt(TunerNum); } }
        public String EPGNum { get; set; }
        public UInt32 EPGNumInt { get { return ToUInt(EPGNum); } }
        public int Priority { get; set; }
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
        public bool SPBasicOnly { get; set; }
        public string ViewTime { get { return Time.Substring(0, 5); } }//曜日情報は削除
        public string ViewBasicOnly { get { return (BSBasicOnly ? "基" : "詳") + "," + (CS1BasicOnly ? "基" : "詳") + "," + (CS2BasicOnly ? "基" : "詳") + "," + (SPBasicOnly ? "基" : "詳"); } }
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
