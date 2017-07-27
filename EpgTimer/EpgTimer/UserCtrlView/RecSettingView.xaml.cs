using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

        private RadioBtnSelect recEndModeRadioBtns;
        private List<TunerSelectInfo> tunerList = new List<TunerSelectInfo>();
        private static CtrlCmdUtil cmd { get { return CommonManager.Instance.CtrlCmd; } }

        private bool IsManual = false;

        private PresetEditor<RecPresetItem> preEdit = new PresetEditor<RecPresetItem>();
        private ComboBox comboBox_preSet;

        private bool initLoad = false;
        public RecSettingView()
        {
            InitializeComponent();

            try
            {
                if (CommonManager.Instance.NWMode == true)
                {
                    preEdit.button_add.IsEnabled = false;
                    preEdit.button_chg.IsEnabled = false;
                    preEdit.button_del.IsEnabled = false;
                    preEdit.button_add.ToolTip = "EpgTimerNWからは変更出来ません";
                    preEdit.button_chg.ToolTip = preEdit.button_add.ToolTip;
                    preEdit.button_del.ToolTip = preEdit.button_add.ToolTip;
                }

                recSetting = Settings.Instance.RecPresetList[0].Data.Clone();

                comboBox_recMode.ItemsSource = CommonManager.RecModeList;
                comboBox_tuijyu.ItemsSource = CommonManager.YesNoList;
                comboBox_pittari.ItemsSource = CommonManager.YesNoList;
                comboBox_priority.ItemsSource = CommonManager.PriorityList;

                recEndModeRadioBtns = new RadioBtnSelect(radioButton_non, radioButton_standby, radioButton_suspend, radioButton_shutdown);

                tunerList.Add(new TunerSelectInfo("自動", 0));
                foreach (TunerReserveInfo info in CommonManager.Instance.DB.TunerReserveList.Values)
                {
                    if (info.tunerID != 0xFFFFFFFF)
                    {
                        tunerList.Add(new TunerSelectInfo(info.tunerName, info.tunerID));
                    }
                }
                comboBox_tuner.ItemsSource = tunerList;
                comboBox_tuner.SelectedIndex = 0;

                stackPanel_PresetEdit.Children.Clear();
                stackPanel_PresetEdit.Children.Add(preEdit);
                preEdit.Set(this, PresetSelectChanged, PresetEdited, "録画プリセット", SetRecPresetWindow.SettingWithDialog);
                comboBox_preSet = preEdit.comboBox_preSet;

                var bx = new BoxExchangeEdit.BoxExchangeEditor(null, listView_recFolder, true, true, true);
                bx.TargetBox.KeyDown += ViewUtil.KeyDown_Enter(button_recFolderChg);
                bx.targetBoxAllowDoubleClick(bx.TargetBox, (sender, e) => button_recFolderChg.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                button_recFolderDel.Click += new RoutedEventHandler(bx.button_Delete_Click);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public void SetData(object data) { SetDefSetting(data as RecSettingData); }
        public object GetData() { return GetRecSetting(); }
        public IEnumerable<PresetItem> DefPresetList() { return Settings.Instance.RecPresetList.Clone(); }

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
            if (CommonManager.Instance.NWMode == false)
            {
                CommonManager.Instance.CtrlCmd.SendNotifyProfileUpdate();
                ViewUtil.MainWindow.RefreshAllViewsReserveInfo(MainWindow.UpdateViewMode.ReserveInfoNoTuner);
            }

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
                    recSetting = item.Data.Clone();
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
            return (data ?? GetRecSetting()).LookUpPreset(preEdit.Items, IsManual);
        }
        public RecPresetItem SelectedPreset(bool isCheckData = false)
        {
            var select = comboBox_preSet.SelectedItem as RecPresetItem;
            return (isCheckData == false ? select : MatchPresetOrDefault())
                ?? new RecPresetItem((select != null ? select.ToString() : "カスタム") + "*", PresetItem.CustomID);
        }

        public void SetDefSetting(RecSettingData set)
        {
            recSetting = set.Clone();

            //"登録時"を追加する。既存があれば追加前に削除する。検索ダイアログの上下ボタンの移動用のコード。
            comboBox_preSet.Items.Remove(preEdit.FindPreset(RecPresetItem.CustomID));
            comboBox_preSet.Items.Add(new RecPresetItem("登録時", RecPresetItem.CustomID, recSetting.Clone()));

            //該当するものがあれば選択、無ければ"登録時"。
            preEdit.ChangeSelect(MatchPresetOrDefault(recSetting), PresetSelectMode.InitLoad);
        }

        public RecSettingData GetRecSetting()
        {
            if (initLoad == false)
            {
                return recSetting.Clone();
            }

            var setInfo = new RecSettingData();

            setInfo.RecMode = (byte)comboBox_recMode.SelectedIndex;
            setInfo.Priority = (byte)(comboBox_priority.SelectedIndex + 1);
            setInfo.TuijyuuFlag = (byte)comboBox_tuijyu.SelectedIndex;
            setInfo.PittariFlag = (byte)comboBox_pittari.SelectedIndex;

            setInfo.ServiceModeIsDefault = checkBox_serviceMode.IsChecked == true;
            setInfo.ServiceCaption = checkBox_serviceCaption.IsChecked == true;
            setInfo.ServiceData = checkBox_serviceData.IsChecked == true;

            setInfo.BatFilePath = textBox_bat.Text;
            setInfo.RecFolderList.Clear();
            setInfo.PartialRecFolder.Clear();
            foreach (RecFileSetInfoView view in listView_recFolder.Items)
            {
                (view.PartialRec ? setInfo.PartialRecFolder : setInfo.RecFolderList).Add(view.Info);
            }

            setInfo.SetSuspendMode(checkBox_suspendDef.IsChecked == true, recEndModeRadioBtns.Value);
            setInfo.RebootFlag = (byte)(checkBox_reboot.IsChecked == true ? 1 : 0);

            setInfo.UseMargineFlag = (byte)(checkBox_margineDef.IsChecked == true ? 0 : 1);
            setInfo.StartMargine = GetMargin(textBox_margineStart.Text);
            setInfo.EndMargine = GetMargin(textBox_margineEnd.Text);

            setInfo.PartialRecFlag = (byte)(checkBox_partial.IsChecked == true ? 1 : 0);
            setInfo.ContinueRecFlag = (byte)(checkBox_continueRec.IsChecked == true ? 1 : 0);
            setInfo.TunerID = ((TunerSelectInfo)comboBox_tuner.SelectedItem).ID;

            return setInfo;
        }

        private static int GetMargin(string text)
        {
            try
            {
                if (text.Length == 0) return 0;

                int marginSec = 0;
                int marginMinus = 1;
                if (text.IndexOf("-") == 0)
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (initLoad == false)
            {
                UpdateView();
                initLoad = true;
            }
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

                listView_recFolder.Items.Clear();
                foreach (RecFileSetInfo info in recSetting.RecFolderList)
                {
                    listView_recFolder.Items.Add(new RecFileSetInfoView(info.Clone(), false));
                }
                foreach (RecFileSetInfo info in recSetting.PartialRecFolder)
                {
                    listView_recFolder.Items.Add(new RecFileSetInfoView(info.Clone(), true));
                }
                listView_recFolder.FitColumnWidth();

                checkBox_suspendDef.IsChecked = null;//切り替え時のイベント発生のために必要
                checkBox_suspendDef.IsChecked = recSetting.SuspendMode == 0;
                checkBox_margineDef.IsChecked = null;//切り替え時のイベント発生のために必要
                checkBox_margineDef.IsChecked = recSetting.UseMargineFlag == 0;
                checkBox_continueRec.IsChecked = (recSetting.ContinueRecFlag == 1);
                checkBox_partial.IsChecked = (recSetting.PartialRecFlag == 1);
                comboBox_tuner.SelectedItem = comboBox_tuner.Items.OfType<TunerSelectInfo>().FirstOrDefault(info => info.ID == recSetting.TunerID);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            OnUpdatingView = false;
        }

        private void checkBox_margineDef_Checked(object sender, RoutedEventArgs e)
        {
            RecSettingData recSet = recSetting.Clone();
            recSet.UseMargineFlag = (byte)(checkBox_margineDef.IsChecked == true ? 0 : 1);
            if (recSet.UseMargineFlag == 0 && OnUpdatingView == false)
            {
                recSetting.StartMargine = GetMargin(textBox_margineStart.Text);
                recSetting.EndMargine = GetMargin(textBox_margineEnd.Text);
            }
            textBox_margineStart.Text = recSet.StartMarginActual.ToString();
            textBox_margineEnd.Text = recSet.EndMarginActual.ToString();
        }

        private void checkBox_serviceMode_Checked(object sender, RoutedEventArgs e)
        {
            RecSettingData recSet = recSetting.Clone();
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
            RecSettingData recSet = recSetting.Clone();
            recSet.SetSuspendMode(checkBox_suspendDef.IsChecked == true, recEndMode);
            if (recSet.SuspendMode == 0 && OnUpdatingView == false)
            {
                recEndMode = recEndModeRadioBtns.Value;
                recSetting.RebootFlag = (byte)(checkBox_reboot.IsChecked == true ? 1 : 0);
            }
            recEndModeRadioBtns.Value = recSet.RecEndModeActual;
            checkBox_reboot.IsChecked = recSet.RebootFlagActual == 1;
        }

        private void button_bat_Click(object sender, RoutedEventArgs e)
        {
            CommonManager.GetFileNameByDialog(textBox_bat, false, "", ".bat", true);
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
                    setting.GetSetting(ref selectInfo);
                    listView_recFolder.Items.Refresh();
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
            var items = listView_recFolder.SelectedItems.OfType<RecFileSetInfoView>().Select(item => new RecFileSetInfoView(item.Info.Clone(), item.PartialRec));
            listView_recFolder.ScrollIntoViewLast(items);
        }
        private void button_recFolderAdd_Click(object sender, RoutedEventArgs e)
        {
            var setting = new RecFolderWindow { Owner = CommonUtil.GetTopWindow(this) };
            if (setting.ShowDialog() == true)
            {
                var setInfo = new RecFileSetInfoView(new RecFileSetInfo());
                setting.GetSetting(ref setInfo);
                listView_recFolder.ScrollIntoViewLast(setInfo);
                listView_recFolder.FitColumnWidth();
            }
        }
    }

    public class RecFileSetInfoView
    {
        public RecFileSetInfoView(RecFileSetInfo info, bool partialRec = false) { Info = info; PartialRec = partialRec; }
        public RecFileSetInfo Info { get; set; }
        public bool PartialRec { get; set; }
        public string RecFileName { get { return Info.RecFileName; } }
        public string RecFolder { get { return Info.RecFolder; } }
        public string RecNamePlugIn { get { return Info.RecNamePlugIn; } }
        public string WritePlugIn { get { return Info.WritePlugIn; } }
        public string PartialRecYesNo { get { return PartialRec ? "はい" : "いいえ"; } }
    }
}
