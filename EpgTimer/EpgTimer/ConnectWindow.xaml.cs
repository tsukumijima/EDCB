using System;
using System.Linq;
using System.Windows;

namespace EpgTimer
{
    /// <summary>
    /// ConnectWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ConnectWindow : Window
    {
        private const string DefPresetStr = "前回接続時";

        public ConnectWindow()
        {
            InitializeComponent();

            try
            {
                var nowSet = new NWPresetItem(DefPresetStr, Settings.Instance.NWServerIP, Settings.Instance.NWServerPort, Settings.Instance.NWWaitPort, Settings.Instance.NWMacAdd);
                cmb_preset.Items.Add(nowSet);
                Settings.Instance.NWPreset.ForEach(item => cmb_preset.Items.Add(item.DeepClone()));
                cmb_preset.SelectedIndex = FindCmbPresetItem(nowSet, true);
                this.KeyDown += ViewUtil.KeyDown_Escape_Close;
                cmb_preset.KeyDown += ViewUtil.KeyDown_Enter(btn_reload);
            }
            catch { }
        }

        private void button_connect_Click(object sender, RoutedEventArgs e)
        {
            SaveLastConnect();
            DialogResult = true;
        }
        private void SaveLastConnect()
        {
            NWPresetItem data = GetSetting();
            Settings.Instance.NWServerIP = data.NWServerIP;
            Settings.Instance.NWServerPort = data.NWServerPort;
            Settings.Instance.NWWaitPort = data.NWWaitPort;
            Settings.Instance.NWMacAdd = data.NWMacAdd;
        }

        private void button_wake_Click(object sender, RoutedEventArgs e)
        {
            byte[] macAddress = ConvertTextMacAddress(textBox_mac.Text);
            if (macAddress == null)
            {
                label_wakeResult.Text = "Error! 書式が間違っているか、\r\n16進アドレスの数値が読み取れません。";
                return;
            }

            int ifCount;
            int ifTotal;
            if (NWConnect.SendMagicPacket(macAddress, out ifCount, out ifTotal))
            {
                label_wakeResult.Text = (ifCount > 0 ? "送信しました" : "送信できませんでした") + "(" + ifCount + "/" + ifTotal + "interfaces)";
            }
            else
            {
                label_wakeResult.Text = "Error! 送信できません";
            }
            SaveLastConnect();
        }
        public static byte[] ConvertTextMacAddress(string txt)
        {
            string[] mac = txt.Split('-');
            if (mac.Length != 6) return null;

            var macAddress = new byte[6];
            for (int i = 0; i < 6; i++)
            {
                if (byte.TryParse(mac[i], System.Globalization.NumberStyles.HexNumber, null, out macAddress[i]) == false)
                {
                    return null;
                }
            }
            return macAddress;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            button_connect.Focus();
        }

        private void SetSetting(NWPresetItem data)
        {
            if (data != null)
            {
                textBox_srvIP.Text = data.NWServerIP;
                textBox_srvPort.Text = data.NWServerPort.ToString();
                checkBox_clientPort.IsChecked = data.NWWaitPort != 0;
                textBox_clientPort.Text = data.NWWaitPort == 0 ? "4520" : data.NWWaitPort.ToString();
                textBox_mac.Text = data.NWMacAdd;
            }
        }
        private NWPresetItem GetSetting()
        {
            var preset = new NWPresetItem();
            preset.NWServerIP = textBox_srvIP.Text;
            preset.NWServerPort = MenuUtil.MyToNumerical(textBox_srvPort, Convert.ToUInt32, Settings.Instance.NWServerPort);
            preset.NWWaitPort = checkBox_clientPort.IsChecked == false ? 0 : MenuUtil.MyToNumerical(textBox_clientPort, Convert.ToUInt32, Settings.Instance.NWWaitPort);
            preset.NWMacAdd = textBox_mac.Text;
            return preset;
        }

        private int FindCmbPresetItem(NWPresetItem preset, bool ignoreName = true)
        {
            return cmb_preset.Items.OfType<NWPresetItem>().ToList().FindLastIndex(item => item.EqualsTo(preset, ignoreName));
        }

        private void btn_reload_Click(object sender, RoutedEventArgs e)
        {
            SetSetting(cmb_preset.SelectedItem as NWPresetItem);
        }
        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            if (IsCmbTextInvalid() == true) return;//デフォルトアイテム

            NWPresetItem newitem = GetSetting();
            newitem.Name = cmb_preset.Text.Trim();

            int pos = cmb_preset.SelectedIndex;
            if (pos >= 0)
            {
                cmb_preset.Items[pos] = newitem;
                cmb_preset.SelectedIndex = pos;
            }
            else
            {
                cmb_preset.Items.Add(newitem);
                cmb_preset.SelectedIndex = cmb_preset.Items.Count - 1;
            }
        }
        private void btn_delete_Click(object sender, RoutedEventArgs e)
        {
            if (IsCmbTextInvalid() == true) return;//デフォルトアイテム

            int pos = cmb_preset.SelectedIndex;
            if (pos >= 0)
            {
                cmb_preset.Items.RemoveAt(pos);
                cmb_preset.SelectedIndex = Math.Min(pos, cmb_preset.Items.Count - 1);
            }
        }
        private bool IsCmbTextInvalid()
        {
            return string.IsNullOrWhiteSpace(cmb_preset.Text) || cmb_preset.Text.Trim() == DefPresetStr;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Settings.Instance.NWPreset = cmb_preset.Items.OfType<NWPresetItem>().Skip(1).ToList();
            Settings.SaveToXmlFile();
        }

    }
}
