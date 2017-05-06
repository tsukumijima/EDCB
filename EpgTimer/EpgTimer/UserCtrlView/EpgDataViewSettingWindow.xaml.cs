using System.Windows;

namespace EpgTimer
{
    /// <summary>EpgDataViewSettingWindow.xaml の相互作用ロジック</summary>
    public partial class EpgDataViewSettingWindow : Window
    {
        public EpgDataViewSettingWindow(CustomEpgTabInfo setInfo = null)
        {
            InitializeComponent();
            button_ok.Click += new RoutedEventHandler((sender, e) => DialogResult = true);
            button_cancel.Click += new RoutedEventHandler((sender, e) => DialogResult = false);
            checkBox_tryEpgSetting.Visibility = Visibility.Hidden;
            SetDefSetting(setInfo ?? new CustomEpgTabInfo());
        }
        /// <summary>デフォルト表示の設定値</summary>
        public void SetDefSetting(CustomEpgTabInfo setInfo)
        {
            epgDataViewSetting.SetSetting(setInfo);
        }
        /// <summary>設定値の取得</summary>
        public CustomEpgTabInfo GetSetting()
        {
            return epgDataViewSetting.GetSetting();
        }
        public void SetTryMode(bool tryOnly = false)
        {
            checkBox_tryEpgSetting.Visibility = Visibility.Visible;
            checkBox_tryEpgSetting.IsChecked = tryOnly == true || Settings.Instance.TryEpgSetting == true;
            if (tryOnly == true)
            {
                checkBox_tryEpgSetting.IsEnabled = false;
                checkBox_tryEpgSetting.ToolTip = "デフォルト表示では一時的な変更のみ可能で設定は保存されません。";
            }
            checkBox_tryEpgSetting_Click(null, null);
        }
        private void checkBox_tryEpgSetting_Click(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                //これはダイアログの設定なので即座に反映
                Settings.Instance.TryEpgSetting = (checkBox_tryEpgSetting.IsChecked == true);
            }
            epgDataViewSetting.checkBox_isVisible.IsEnabled = Settings.Instance.TryEpgSetting != true;
            epgDataViewSetting.textBox_tabName.IsEnabled = Settings.Instance.TryEpgSetting != true;
        }
    }
}
