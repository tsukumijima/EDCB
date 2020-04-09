using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    using BoxExchangeEdit;
    using PresetEditor;

    /// <summary>
    /// RecSettingView.xaml の相互作用ロジック
    /// </summary>
    public partial class RecSettingView : UserControl, IPresetItemView
    {
        public event Action<bool> SelectedPresetChanged = null;
        private void SelectedPreset_Changed(bool SimpleChanged = true)
        {
            if (SelectedPresetChanged != null) SelectedPresetChanged(SimpleChanged);
        }

        private RecSettingData _recSet;
        private int recEndMode;
        private RecSettingData recSetting
        {
            get { return _recSet; }
            set
            {
                //デフォルトオプションに対する内部の初期値を修正する。
                _recSet = value;
                _recSet.StartMargine = _recSet.StartMarginActual;
                _recSet.EndMargine = _recSet.EndMarginActual;
                _recSet.ServiceCaption = _recSet.ServiceCaptionActual;
                _recSet.ServiceData = _recSet.ServiceDataActual;
                recEndMode = _recSet.RecEndModeActual;
                _recSet.RebootFlag = _recSet.RebootFlagActual;
            }
        }

        public bool PresetResCompare { get; set; }

        private RadioBtnSelect recEndModeRadioBtns;
        private bool IsManual = false;

        private PresetEditor<RecPresetItem> preEdit = new PresetEditor<RecPresetItem>();
        private ComboBox comboBox_preSet;

        public RecSettingView()
        {
            PresetResCompare = false;

            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                preEdit.button_add.IsEnabled = false;
                preEdit.button_chg.IsEnabled = false;
                preEdit.button_del.IsEnabled = false;
                preEdit.button_add.ToolTip = "EpgTimerNWからは変更出来ません";
                preEdit.button_chg.ToolTip = preEdit.button_add.ToolTip;
                preEdit.button_del.ToolTip = preEdit.button_add.ToolTip;
            }

            recSetting = Settings.Instance.RecPresetList[0].Data.DeepClone();

            comboBox_recMode.ItemsSource = CommonManager.RecModeList;
            comboBox_tuijyu.ItemsSource = CommonManager.YesNoList;
            comboBox_pittari.ItemsSource = CommonManager.YesNoList;
            comboBox_priority.ItemsSource = CommonManager.PriorityList;

            recEndModeRadioBtns = new RadioBtnSelect(radioButton_non, radioButton_standby, radioButton_suspend, radioButton_shutdown);

            comboBox_tuner.ItemsSource = new List<TunerSelectInfo> { new TunerSelectInfo("自動", 0) }
                .Concat(CommonManager.Instance.DB.TunerReserveList.Values
                .Where(info => info.tunerID != 0xFFFFFFFF)
                .Select(info => new TunerSelectInfo(info.tunerName, info.tunerID)));
            comboBox_tuner.SelectedIndex = 0;

            stackPanel_PresetEdit.Children.Clear();
            stackPanel_PresetEdit.Children.Add(preEdit);
            preEdit.Set(this, PresetSelectChanged, PresetEdited, "録画プリセット", SetRecPresetWindow.SettingWithDialog);
            comboBox_preSet = preEdit.comboBox_preSet;

            var bx = new BoxExchangeEditor(null, listView_recFolder, true, true, true);
            bx.TargetBox.KeyDown += ViewUtil.KeyDown_Enter(button_recFolderChg);
            bx.targetBoxAllowDoubleClick(bx.TargetBox, (sender, e) => button_recFolderChg.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            button_recFolderDel.Click += new RoutedEventHandler(bx.button_Delete_Click);

            button_bat.Click += ViewUtil.OpenFileNameDialog(textBox_bat, false, "", ".bat", true);
        }

        public void SetData(object data) { SetDefSetting(data as RecSettingData); }
        public object GetData() { return GetRecSetting(); }
        public IEnumerable<PresetItem> DefPresetList() { return Settings.Instance.RecPresetList.DeepClone(); }

        public void SetViewMode(bool epgMode)
        {
            IsManual = !epgMode;
            comboBox_tuijyu.IsEnabled = (epgMode == true);
            comboBox_pittari.IsEnabled = (epgMode == true);
        }

        public void SetChangeMode(int chgMode)
        {
            switch (chgMode)
            {
                case 0:
                    ViewUtil.SetSpecificChgAppearance(textBox_margineStart);
                    textBox_margineStart.Focus();
                    textBox_margineStart.SelectAll();
                    break;
                case 1:
                    ViewUtil.SetSpecificChgAppearance(textBox_margineEnd);
                    textBox_margineEnd.Focus();
                    textBox_margineEnd.SelectAll();
                    break;
            }
            stackPanel_PresetEdit.Visibility = chgMode == int.MaxValue ? Visibility.Collapsed : Visibility.Visible;
        }

        private enum PresetSelectMode { Normal, SelectOnly, InitLoad };
        private void PresetEdited(List<RecPresetItem> list, PresetEdit mode)
        {
            Settings.Instance.RecPresetList = list;
            RecPresetItem.SavePresetList(list);
            SettingWindow.UpdatesInfo("録画プリセット変更");
            if (CommonManager.Instance.NWMode == false)
            {
                CommonManager.CreateSrvCtrl().SendNotifyProfileUpdate();
                CommonManager.MainWindow.RefreshAllViewsReserveInfo(MainWindow.UpdateViewMode.ReserveInfoNoTuner);
            }
            SetCustomPresetDisplayName();

            if (mode == PresetEdit.Set)
            {
                preEdit.ChangeSelect(MatchPresetOrDefault() ?? preEdit.Items.Last(), PresetSelectMode.SelectOnly);
            }
            SelectedPreset_Changed(false);
        }
        private void PresetSelectChanged(RecPresetItem item, object msg)
        {
            var code = (msg as PresetSelectMode?) ?? PresetSelectMode.Normal;
            if (code != PresetSelectMode.SelectOnly)
            {
                if (code != PresetSelectMode.InitLoad)
                {
                    recSetting = item.Data.DeepClone();
                }
                UpdateView();
            }
            preEdit.button_chg.IsEnabled = !item.IsCustom && CommonManager.Instance.NWMode == false;
            preEdit.button_del.IsEnabled = preEdit.button_chg.IsEnabled;
            SelectedPreset_Changed(code == PresetSelectMode.Normal);
        }

        //このへんそのうちpreEditに移動か
        public RecPresetItem MatchPresetOrDefault(RecSettingData data = null)
        {
            return (data ?? GetRecSetting()).LookUpPreset(preEdit.Items, IsManual, PresetResCompare);
        }
        public RecPresetItem SelectedPreset(bool isCheckData = false)
        {
            var select = comboBox_preSet.SelectedItem as RecPresetItem;
            return (isCheckData == false ? select : MatchPresetOrDefault())
                ?? new RecPresetItem((select != null ? select.ToString() : "カスタム") + "*", PresetItem.CustomID);
        }

        public void SetDefSetting(RecSettingData set)
        {
            if (set == null) return;
            recSetting = set.DeepClone();

            //"登録時"を追加する。既存があれば追加前に削除する。検索ダイアログの上下ボタンの移動用のコード。
            comboBox_preSet.Items.Remove(preEdit.FindPreset(RecPresetItem.CustomID));
            comboBox_preSet.Items.Add(new RecPresetItem("登録時", RecPresetItem.CustomID, recSetting.DeepClone()));
            SetCustomPresetDisplayName();

            //該当するものがあれば選択、無ければ"登録時"。
            preEdit.ChangeSelect(MatchPresetOrDefault(recSetting), PresetSelectMode.InitLoad);
        }
        public void SetCustomPresetDisplayName()
        {
            if (preEdit.Items.Any() == false) return;
            var cust = preEdit.Items.Last();
            var chkItem = MatchPresetOrDefault(cust.Data);
            cust.DisplayName = (chkItem == null || chkItem == cust) ? "登録時" : string.Format("登録時({0})", chkItem.DisplayName);
        }

        public RecSettingData GetRecSetting()
        {
            var setInfo = new RecSettingData();

            setInfo.RecMode = (byte)comboBox_recMode.SelectedIndex;
            setInfo.Priority = (byte)(comboBox_priority.SelectedIndex + 1);
            setInfo.TuijyuuFlag = (byte)comboBox_tuijyu.SelectedIndex;
            setInfo.PittariFlag = (byte)comboBox_pittari.SelectedIndex;

            setInfo.ServiceModeIsDefault = checkBox_serviceMode.IsChecked == true;
            setInfo.ServiceCaption = checkBox_serviceCaption.IsChecked == true;
            setInfo.ServiceData = checkBox_serviceData.IsChecked == true;

            setInfo.BatFilePath = textBox_bat.Text;
            setInfo.RecTag = textBox_recTag.Text;
            setInfo.RecFolderList.Clear();
            setInfo.PartialRecFolder.Clear();
            foreach (RecFileSetInfoView view in listView_recFolder.Items)
            {
                (view.PartialRec ? setInfo.PartialRecFolder : setInfo.RecFolderList).Add(view.Info);
            }

            setInfo.SetSuspendMode(checkBox_suspendDef.IsChecked == true, recEndModeRadioBtns.Value);
            setInfo.RebootFlag = (byte)(checkBox_reboot.IsChecked == true ? 1 : 0);

            setInfo.IsMarginDefault = checkBox_margineDef.IsChecked == true;
            setInfo.StartMargine = GetMargin(textBox_margineStart.Text);
            setInfo.EndMargine = GetMargin(textBox_margineEnd.Text);

            setInfo.PartialRecFlag = (byte)(checkBox_partial.IsChecked == true ? 1 : 0);
            setInfo.ContinueRecFlag = (byte)(checkBox_continueRec.IsChecked == true ? 1 : 0);
            setInfo.TunerID = (uint)(comboBox_tuner.SelectedValue ?? 0u);

            return setInfo;
        }

        private static int GetMargin(string text)
        {
            try
            {
                if (text.Length == 0) return 0;

                int marginSec = 0;
                int marginMinus = 1;
                if (text.StartsWith("-", StringComparison.Ordinal) == true)
                {
                    marginMinus = -1;
                    text = text.Substring(1);
                }
                string[] startArray = text.Split(':');
                startArray = startArray.Take(Math.Min(startArray.Length, 3)).Reverse().ToArray();
                for (int i = 0; i < startArray.Length; i++)
                {
                    marginSec += Convert.ToInt32(startArray[i]) * (int)Math.Pow(60, i);
                }

                return marginMinus * marginSec;
            }
            catch { }
            return 0;
        }

        private bool OnUpdatingView = false;
        private void UpdateView()
        {
            OnUpdatingView = true;
            try
            {
                comboBox_recMode.SelectedIndex = Math.Min((int)recSetting.RecMode, 5);
                comboBox_priority.SelectedIndex = Math.Min(Math.Max((int)recSetting.Priority, 1), 5) - 1;
                comboBox_tuijyu.SelectedIndex = recSetting.TuijyuuFlag != 0 ? 1 : 0;
                comboBox_pittari.SelectedIndex = recSetting.PittariFlag != 0 ? 1 : 0;
                checkBox_serviceMode.IsChecked = null;//切り替え時のイベント発生のために必要
                checkBox_serviceMode.IsChecked = recSetting.ServiceModeIsDefault;
                textBox_bat.Text = recSetting.BatFilePath;
                textBox_recTag.Text = recSetting.RecTag;

                listView_recFolder.Items.Clear();
                foreach (RecFileSetInfo info in recSetting.RecFolderList)
                {
                    listView_recFolder.Items.Add(new RecFileSetInfoView(info.DeepClone(), false));
                }
                foreach (RecFileSetInfo info in recSetting.PartialRecFolder)
                {
                    listView_recFolder.Items.Add(new RecFileSetInfoView(info.DeepClone(), true));
                }
                listView_recFolder.FitColumnWidth();

                checkBox_suspendDef.IsChecked = null;//切り替え時のイベント発生のために必要
                checkBox_suspendDef.IsChecked = recSetting.RecEndIsDefault;
                checkBox_margineDef.IsChecked = null;//切り替え時のイベント発生のために必要
                checkBox_margineDef.IsChecked = recSetting.IsMarginDefault;
                checkBox_continueRec.IsChecked = (recSetting.ContinueRecFlag == 1);
                checkBox_partial.IsChecked = (recSetting.PartialRecFlag == 1);
                comboBox_tuner.SelectedValue = recSetting.TunerID;
                if (comboBox_tuner.SelectedValue == null)
                {
                    comboBox_tuner.ItemsSource = comboBox_tuner.ItemsSource.Cast<TunerSelectInfo>().Concat(new TunerSelectInfo("不明なチューナー", recSetting.TunerID).IntoList());
                    comboBox_tuner.SelectedValue = recSetting.TunerID;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            OnUpdatingView = false;
        }
        public void RefreshView()
        {
            if (checkBox_margineDef.IsChecked == true)
            {
                textBox_margineStart.Text = Settings.Instance.DefStartMargin.ToString();
                textBox_margineEnd.Text = Settings.Instance.DefEndMargin.ToString();
            }
            if (checkBox_serviceMode.IsChecked == true)
            {
                checkBox_serviceCaption.IsChecked = Settings.Instance.DefServiceCaption;
                checkBox_serviceData.IsChecked = Settings.Instance.DefServiceData;
            }
            if (checkBox_suspendDef.IsChecked == true)
            {
                recEndModeRadioBtns.Value = Settings.Instance.DefRecEndMode;
                checkBox_reboot.IsChecked = Settings.Instance.DefRebootFlg == 1;
            }
        }

        private void checkBox_margineDef_Checked(object sender, RoutedEventArgs e)
        {
            RecSettingData recSet = recSetting.DeepClone();
            recSet.IsMarginDefault = checkBox_margineDef.IsChecked == true;
            if (recSet.IsMarginDefault == true && OnUpdatingView == false)
            {
                recSetting.StartMargine = GetMargin(textBox_margineStart.Text);
                recSetting.EndMargine = GetMargin(textBox_margineEnd.Text);
            }
            textBox_margineStart.Text = recSet.StartMarginActual.ToString();
            textBox_margineEnd.Text = recSet.EndMarginActual.ToString();
        }

        private void checkBox_serviceMode_Checked(object sender, RoutedEventArgs e)
        {
            RecSettingData recSet = recSetting.DeepClone();
            recSet.ServiceModeIsDefault = checkBox_serviceMode.IsChecked == true;
            if (recSet.ServiceModeIsDefault == true && OnUpdatingView == false)
            {
                recSetting.ServiceCaption = checkBox_serviceCaption.IsChecked == true;
                recSetting.ServiceData = checkBox_serviceData.IsChecked == true;
            }
            checkBox_serviceCaption.IsChecked = recSet.ServiceCaptionActual;
            checkBox_serviceData.IsChecked = recSet.ServiceDataActual;
        }

        private void checkBox_suspendDef_Checked(object sender, RoutedEventArgs e)
        {
            RecSettingData recSet = recSetting.DeepClone();
            recSet.SetSuspendMode(checkBox_suspendDef.IsChecked == true, recEndMode);
            if (recSet.RecEndIsDefault == true && OnUpdatingView == false)
            {
                recEndMode = recEndModeRadioBtns.Value;
                recSetting.RebootFlag = (byte)(checkBox_reboot.IsChecked == true ? 1 : 0);
            }
            recEndModeRadioBtns.Value = recSet.RecEndModeActual;
            checkBox_reboot.IsChecked = recSet.RebootFlagActual == 1;
        }

        private void button_recFolderChg_Click(object sender, RoutedEventArgs e)
        {
            if (listView_recFolder.SelectedItem == null)
            {
                listView_recFolder.SelectedIndex = 0;
            }
            var selectInfo = listView_recFolder.SelectedItem as RecFileSetInfoView;
            if (selectInfo != null)
            {
                var setting = new RecFolderWindow { Owner = CommonUtil.GetTopWindow(this) };
                setting.SetDefSetting(selectInfo);
                if (setting.ShowDialog() == true)
                {
                    listView_recFolder.SelectedItem = 
                        listView_recFolder.Items[listView_recFolder.SelectedIndex] = setting.GetSetting();
                    listView_recFolder.FitColumnWidth();
                }
            }
            else
            {
                button_recFolderAdd_Click(null, null);
            }
        }
        private void button_recFolderCopy_Click(object sender, RoutedEventArgs e)
        {
            if (listView_recFolder.SelectedItem == null) return;
            var items = listView_recFolder.SelectedItems.OfType<RecFileSetInfoView>().Select(item => new RecFileSetInfoView(item.Info.DeepClone(), item.PartialRec));
            listView_recFolder.ScrollIntoViewLast(items);
        }
        private void button_recFolderAdd_Click(object sender, RoutedEventArgs e)
        {
            var setting = new RecFolderWindow { Owner = CommonUtil.GetTopWindow(this) };
            if (setting.ShowDialog() == true)
            {
                listView_recFolder.ScrollIntoViewLast(setting.GetSetting());
                listView_recFolder.FitColumnWidth();
            }
        }

        public string GetRecSettingHeaderString(bool SimpleChanged = true)
        {
            string preset_str = "";
            if (Settings.Instance.DisplayPresetOnSearch == true)
            {
                preset_str = string.Format(" - {0}", this.SelectedPreset(!SimpleChanged).ToString());
            }
            return preset_str;
        }

        public ContextMenu CreatePresetSlelectMenu()
        {
            SelectedPreset_Changed(false);//状態の確定
            RecSettingData setting = GetRecSetting();
            bool chk = false;
            return preEdit.CreateSlelectMenu(item => !chk && (chk = setting.EqualsSettingTo(item.Data, IsManual, PresetResCompare)));
        }
        public void OpenPresetSelectMenuOnMouseEvent(object sender, MouseButtonEventArgs e)
        {
            if (Settings.Instance.DisplayPresetOnSearch == false) return;
            CreatePresetSlelectMenu().IsOpen = true;
        }
    }

    public class RecFileSetInfoView
    {
        public RecFileSetInfoView(RecFileSetInfo info, bool partialRec = false) { Info = info; PartialRec = partialRec; }
        public RecFileSetInfo Info { get; set; }
        public bool PartialRec { get; set; }
        public string RecFileName { get { return Info.RecFileName; } }
        public string RecFolder { get { return Info.RecFolder.Equals("!Default", StringComparison.OrdinalIgnoreCase) == true ? "" : Info.RecFolder; } }
        public string RecNamePlugIn { get { return Info.RecNamePlugIn; } }
        public string WritePlugIn { get { return Info.WritePlugIn; } }
        public string PartialRecYesNo { get { return PartialRec ? "はい" : "いいえ"; } }
    }
}
