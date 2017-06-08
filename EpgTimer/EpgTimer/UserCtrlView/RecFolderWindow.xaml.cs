using System;
using System.Linq;
using System.Windows;

namespace EpgTimer
{
    /// <summary>
    /// RecFolderWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RecFolderWindow : Window
    {
        public RecFolderWindow()
        {
            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                button_write.IsEnabled = false;
                button_recName.IsEnabled = false;
            }

            button_ok.Click += (sender, e) => DialogResult = true;
            button_cancel.Click += (sender, e) => DialogResult = false;

            if (CommonManager.Instance.IsConnected == true)
            {
                CommonManager.Instance.DB.ReloadPlugInFile();
            }
            comboBox_writePlugIn.ItemsSource = CommonManager.Instance.DB.WritePlugInList;
            comboBox_writePlugIn.SelectedItem = "Write_Default.dll";

            comboBox_recNamePlugIn.ItemsSource = new[] { "なし" }.Concat(CommonManager.Instance.DB.RecNamePlugInList);
            comboBox_recNamePlugIn.SelectedIndex = 0;
        }

        public void SetDefSetting(RecFileSetInfoView info)
        {
            button_ok.Content = "変更";
            chkbox_partial.IsChecked = info.PartialRec;
            textBox_recFolder.Text = String.Compare(info.RecFolder, "!Default", true) == 0 ? "" : SettingPath.CheckFolder(info.RecFolder);
            comboBox_writePlugIn.SelectedItem = comboBox_writePlugIn.Items.OfType<string>().FirstOrDefault(s => String.Compare(s, info.WritePlugIn, true) == 0);
            string pluginName = info.RecNamePlugIn.Substring(0, (info.RecNamePlugIn + '?').IndexOf('?'));
            var plugin = comboBox_recNamePlugIn.Items.OfType<string>().FirstOrDefault(s => String.Compare(s, pluginName, true) == 0);
            if (plugin != null)
            {
                comboBox_recNamePlugIn.SelectedItem = plugin;
                textBox_recNameOption.Text = info.RecNamePlugIn.Length <= pluginName.Length + 1 ? "" : info.RecNamePlugIn.Substring(pluginName.Length + 1);
            }
        }
        public void GetSetting(ref RecFileSetInfoView info)
        {
            var recFolder = SettingPath.CheckFolder(textBox_recFolder.Text);
            info.Info.RecFolder = recFolder == "" ? "!Default" : recFolder;
            info.Info.WritePlugIn = comboBox_writePlugIn.SelectedItem as string ?? "";
            info.Info.RecNamePlugIn = comboBox_recNamePlugIn.SelectedIndex <= 0 ? "" : comboBox_recNamePlugIn.SelectedItem as string ?? "";
            if (info.RecNamePlugIn != "" && textBox_recNameOption.Text.Trim() != "")
            {
                info.Info.RecNamePlugIn += '?' + textBox_recNameOption.Text.Trim();
            }
            info.PartialRec = chkbox_partial.IsChecked == true;
        }

        private void button_folder_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFolderNameByDialog(textBox_recFolder, "録画フォルダの選択", true);
        }

        private void button_write_Click(object sender, RoutedEventArgs e)
        {
            ViewUtil.WritePlugInSet(comboBox_writePlugIn.SelectedItem as string, this);
        }

        private void button_recName_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_recNamePlugIn.SelectedIndex <= 0) return;
            ViewUtil.RecNamePlugInSet(comboBox_recNamePlugIn.SelectedItem as string, this);
        }
    }
}
