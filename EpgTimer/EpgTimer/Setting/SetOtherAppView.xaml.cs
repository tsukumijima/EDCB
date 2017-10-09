using System;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer.Setting
{
    /// <summary>
    /// SetOtherAppView.xaml の相互作用ロジック
    /// </summary>
    public partial class SetOtherAppView : UserControl
    {
        public SetOtherAppView()
        {
            InitializeComponent();

            //エスケープキャンセルだけは常に有効にする。
            var bx = new BoxExchangeEdit.BoxExchangeEditor(null, this.listBox_bon, true);
            if (CommonManager.Instance.NWMode == false)
            {
                bx.AllowDragDrop();
                bx.AllowKeyAction();
                button_up.Click += new RoutedEventHandler(bx.button_Up_Click);
                button_down.Click += new RoutedEventHandler(bx.button_Down_Click);
                button_del.Click += new RoutedEventHandler(bx.button_Delete_Click);
            }
            else
            {
                label3.IsEnabled = false;
                button_up.IsEnabled = false;
                button_down.IsEnabled = false;
                button_del.IsEnabled = false;
                button_add.IsEnabled = false;
                checkBox_playOnNwWithExe.IsEnabled = true;
            }
        }

        public void LoadSetting()
        {
            textBox_exe.Text = Settings.Instance.TvTestExe;
            textBox_cmd.Text = Settings.Instance.TvTestCmd;
            checkBox_nwTvMode.IsChecked = Settings.Instance.NwTvMode;
            checkBox_nwUDP.IsChecked = Settings.Instance.NwTvModeUDP;
            checkBox_nwTCP.IsChecked = Settings.Instance.NwTvModeTCP;
            textBox_TvTestOpenWait.Text = Settings.Instance.TvTestOpenWait.ToString();
            textBox_TvTestChgBonWait.Text = Settings.Instance.TvTestChgBonWait.ToString();

            textBox_playExe.Text = Settings.Instance.FilePlayExe;
            textBox_playCmd.Text = Settings.Instance.FilePlayCmd;
            checkBox_playOnAirWithExe.IsChecked = Settings.Instance.FilePlayOnAirWithExe;
            checkBox_playOnNwWithExe.IsChecked = Settings.Instance.FilePlayOnNwWithExe;

            comboBox_bon.ItemsSource = CommonManager.GetBonFileList();
            comboBox_bon.SelectedIndex = 0;

            listBox_bon.Items.Clear();
            int num = IniFileHandler.GetPrivateProfileInt("TVTEST", "Num", 0, SettingPath.TimerSrvIniPath);
            for (uint i = 0; i < num; i++)
            {
                string item = IniFileHandler.GetPrivateProfileString("TVTEST", i.ToString(), "", SettingPath.TimerSrvIniPath);
                if (item.Length > 0) listBox_bon.Items.Add(item);
            }
        }

        public void SaveSetting()
        {
            Settings.Instance.TvTestExe = textBox_exe.Text;
            Settings.Instance.TvTestCmd = textBox_cmd.Text;
            Settings.Instance.NwTvMode = (bool)checkBox_nwTvMode.IsChecked;
            Settings.Instance.NwTvModeUDP = (bool)checkBox_nwUDP.IsChecked;
            Settings.Instance.NwTvModeTCP = (bool)checkBox_nwTCP.IsChecked;
            Settings.Instance.TvTestOpenWait = MenuUtil.MyToNumerical(textBox_TvTestOpenWait, Convert.ToInt32, 120000, 0, Settings.Instance.TvTestOpenWait);
            Settings.Instance.TvTestChgBonWait = MenuUtil.MyToNumerical(textBox_TvTestChgBonWait, Convert.ToInt32, 120000, 0, Settings.Instance.TvTestChgBonWait);

            IniFileHandler.WritePrivateProfileString("TVTEST", "Num", listBox_bon.Items.Count, SettingPath.TimerSrvIniPath);
            IniFileHandler.DeletePrivateProfileNumberKeys("TVTEST", SettingPath.TimerSrvIniPath);
            for (int i = 0; i < listBox_bon.Items.Count; i++)
            {
                IniFileHandler.WritePrivateProfileString("TVTEST", i.ToString(), listBox_bon.Items[i], SettingPath.TimerSrvIniPath);
            }

            Settings.Instance.FilePlayExe = textBox_playExe.Text;
            Settings.Instance.FilePlayCmd = textBox_playCmd.Text;
            Settings.Instance.FilePlayOnAirWithExe = (bool)checkBox_playOnAirWithExe.IsChecked;
            Settings.Instance.FilePlayOnNwWithExe = (bool)checkBox_playOnNwWithExe.IsChecked;
        }

        private void button_exe_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFileNameByDialog(textBox_exe, false, "", ".exe");
        }

        private void button_add_Click(object sender, RoutedEventArgs e)
        {
            ViewUtil.ListBox_TextCheckAdd(listBox_bon, comboBox_bon.Text);
        }

        private void button_playExe_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFileNameByDialog(textBox_playExe, false, "", ".exe");
        }
    }
}
