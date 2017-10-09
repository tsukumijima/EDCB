using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    using Setting;

    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : AttendantWindow
    {
        public static void UpdatesInfo()
        {
            foreach (var win in Application.Current.Windows.OfType<SettingWindow>())
            {
                win.SetReload(true);
            }
        }
        private void SetReload(bool reload)
        {
            button_Reload.Content = "再読込" + (reload == false ? "" : "*");
            button_Reload.ToolTip = reload == false ? null : "他の操作により設定が変更されています";
        }

        public enum SettingMode { Default, EpgSetting }
        public SettingMode Mode { get; private set; }

        public SettingWindow(SettingMode mode = SettingMode.Default, object param = null)
        {
            InitializeComponent();

            //設定ウィンドウについては最低サイズを決めておく。
            if (Height < 580) Height = 580;
            if (Width < 780) Width = 780;

            base.SetParam(false, new CheckBox());
            this.Pinned = true;

            button_Reload.Click += (sender, e) => LoadSetting();
            button_Apply.Click += (sender, e) => Apply();
            button_OK.Click += (sender, e) => { this.Close(); Apply(); };
            button_cancel.Click += (sender, e) => this.Close();

            LoadSetting();
            SetMode(mode, param);
        }

        public void LoadSetting()
        {
            try
            {
                setBasicView.LoadSetting();
                setAppView.LoadSetting();
                setEpgView.LoadSetting();
                setOtherAppView.LoadSetting();
                SetReload(false);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public void SetMode(SettingMode mode, object param)
        {
            Mode = mode;
            switch (mode)
            {
                case SettingMode.EpgSetting:
                    tabItem_epgView.IsSelected = true;
                    setEpgView.tabEpg.IsSelected = true;
                    setEpgView.tabEpgTab.IsSelected = true;
                    setEpgView.listBox_tab.SelectedItem = setEpgView.listBox_tab.Items.OfType<CustomEpgTabInfoView>().FirstOrDefault(item => item.Info.Uid == param as string);
                    break;
            }
        }

        private void Apply()
        {
            try
            {
                setBasicView.SaveSetting();
                setAppView.SaveSetting();
                setEpgView.SaveSetting();
                setOtherAppView.SaveSetting();

                SettingWindow.UpdatesInfo();//基本的に一つしか使わないが一応通知
                SetReload(false);

                if (CommonManager.Instance.NWMode == false)
                {
                    ChSet5.SaveFile();
                    Settings.ReloadOtherOptions();//NWでは別途iniの更新通知後に実行される。
                }
                CommonManager.Instance.ReloadCustContentColorList();
                CommonManager.ReloadReplaceDictionary();
                ItemFontCache.Clear();

                ViewUtil.MainWindow.SaveData();
                ViewUtil.MainWindow.RefreshSetting(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("不正な入力値によるエラーのため、一部設定のみ更新されました。");
            }
        }
    }
}
