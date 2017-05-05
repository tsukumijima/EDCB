using System;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : AttendantWindow
    {
        public enum SettingMode { Default, EpgSetting }
        public SettingMode Mode { get; private set; }

        public SettingWindow(SettingMode mode = SettingMode.Default)
        {
            InitializeComponent();

            base.SetParam(false, new CheckBox());
            this.Pinned = true;

            button_cancel.Click += (sender, e) => this.Close();
            SetMode(mode);
        }

        //このメソッドとxamlのWindowStartupLocation="CenterOwner"を削除すると、ウィンドウの位置・サイズ保存されるようになるが、とりあえずこのまま。
        protected override void WriteWindowSaveData() { Settings.Instance.WndSettings.Remove(this); }

        public void SetMode(SettingMode mode)
        {
            Mode = mode;
            switch (mode)
            {
                case SettingMode.EpgSetting:
                    tabItem_epgView.IsSelected = true;
                    setEpgView.tabEpg.IsSelected = true;
                    setEpgView.tabEpgTab.IsSelected = true;
                    break;
            }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                setBasicView.SaveSetting();
                setAppView.SaveSetting();
                setEpgView.SaveSetting();
                setOtherAppView.SaveSetting();

                if (CommonManager.Instance.NWMode == false)
                {
                    ChSet5.SaveFile();
                    Settings.Instance.ReloadOtherOptions();//NWでは別途iniの更新通知後に実行される。
                }
                /*if (setEpgView.IsClearColorSetting == true)
                {
                    Settings.Instance.ResetColorSetting();
                    Settings.Instance.SetColorSetting();
                }*/
                CommonManager.Instance.ReloadCustContentColorList();
                ItemFontCache.Clear();

                this.Close();
                ViewUtil.MainWindow.SaveData();
                ViewUtil.MainWindow.RefreshSetting(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                MessageBox.Show("不正な入力値によるエラーのため、一部設定のみ更新されました。");
                this.Close();
            }
        }
    }
}
