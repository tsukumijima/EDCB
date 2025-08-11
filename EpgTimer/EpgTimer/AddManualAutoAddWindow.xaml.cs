using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// AddManualAutoAddWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AddManualAutoAddWindow : AddManualAutoAddWindowBase
    {
        protected override DataItemViewBase DataView { get { return mainWindow.autoAddView.manualAutoAddView; } }
        protected override string AutoAddString { get { return "プログラム予約登録"; } }

        private List<CheckBox> chbxList;

        static AddManualAutoAddWindow()
        {
            //追加時の選択用
            mainWindow.autoAddView.manualAutoAddView.ViewUpdated += AddManualAutoAddWindow.UpdatesViewSelection;
        }
        public AddManualAutoAddWindow(ManualAutoAddData data = null, AutoAddMode mode = AutoAddMode.NewAdd)
            : base(data, mode)
        {
            InitializeComponent();

            try
            {
                base.SetParam(false, checkBox_windowPinned, checkBox_dataReplace);

                //コマンドの登録
                this.CommandBindings.Add(new CommandBinding(EpgCmds.Cancel, (sender, e) => this.Close()));
                this.CommandBindings.Add(new CommandBinding(EpgCmds.AddInDialog, autoadd_add));
                this.CommandBindings.Add(new CommandBinding(EpgCmds.ChangeInDialog, autoadd_chg, (sender, e) => e.CanExecute = winMode == AutoAddMode.Change));
                this.CommandBindings.Add(new CommandBinding(EpgCmds.DeleteInDialog, autoadd_del1, (sender, e) => e.CanExecute = winMode == AutoAddMode.Change));
                this.CommandBindings.Add(new CommandBinding(EpgCmds.Delete2InDialog, autoadd_del2, (sender, e) => e.CanExecute = winMode == AutoAddMode.Change));
                this.CommandBindings.Add(new CommandBinding(EpgCmds.BackItem, (sender, e) => MoveViewNextItem(-1)));
                this.CommandBindings.Add(new CommandBinding(EpgCmds.NextItem, (sender, e) => MoveViewNextItem(1)));

                //ボタンの設定
                mBinds.SetCommandToButton(button_cancel, EpgCmds.Cancel);
                mBinds.SetCommandToButton(button_chg, EpgCmds.ChangeInDialog);
                mBinds.SetCommandToButton(button_add, EpgCmds.AddInDialog);
                mBinds.SetCommandToButton(button_del, EpgCmds.DeleteInDialog);
                mBinds.SetCommandToButton(button_del2, EpgCmds.Delete2InDialog);
                mBinds.SetCommandToButton(button_up, EpgCmds.BackItem);
                mBinds.SetCommandToButton(button_down, EpgCmds.NextItem);
                RefreshMenu();

                //ステータスバーの登録
                this.statusBar.Status.Visibility = Visibility.Collapsed;
                StatusManager.RegisterStatusbar(this.statusBar, this);

                //その他設定
                chbxList = CommonManager.DayOfWeekArray.Select(wd => 
                    new CheckBox { Content = wd, Margin = new Thickness(0, 0, 6, 0) }).ToList();
                chbxList.ForEach(chbx => stackPanel_week.Children.Add(chbx));

                comboBox_startHH.ItemsSource = CommonManager.CustomHourList;
                comboBox_startHH.SelectedIndex = 0;
                comboBox_startMM.ItemsSource = Enumerable.Range(0, 60);
                comboBox_startMM.SelectedIndex = 0;
                comboBox_startSS.ItemsSource = Enumerable.Range(0, 60);
                comboBox_startSS.SelectedIndex = 0;
                comboBox_endHH.ItemsSource = CommonManager.CustomHourList;
                comboBox_endHH.SelectedIndex = 0;
                comboBox_endMM.ItemsSource = Enumerable.Range(0, 60);
                comboBox_endMM.SelectedIndex = 0;
                comboBox_endSS.ItemsSource = Enumerable.Range(0, 60);
                comboBox_endSS.SelectedIndex = 0;
                ViewUtil.Set_ComboBox_LostFocus_SelectItemUInt(panel_times);

                comboBox_service.ItemsSource = ChSet5.ChListSelected;
                comboBox_service.SelectedIndex = 0;

                recSettingView.SetViewMode(false);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        public override void SetWindowTitle()
        {
            this.Title = ViewUtil.WindowTitleText(textBox_title.Text, "プログラム自動予約登録");
        }

        protected override bool ReloadInfoData()
        {
            recSettingView.RefreshView();
            return true;
        }

        public override AutoAddData GetData()
        {
            try
            {
                var data = new ManualAutoAddData();
                data.dataID = (uint)dataID;

                uint startTime = ((uint)comboBox_startHH.SelectedIndex * 60 * 60) + ((uint)comboBox_startMM.SelectedIndex * 60) + (uint)comboBox_startSS.SelectedIndex;
                uint endTime = ((uint)comboBox_endHH.SelectedIndex * 60 * 60) + ((uint)comboBox_endMM.SelectedIndex * 60) + (uint)comboBox_endSS.SelectedIndex;
                while (endTime < startTime) endTime += 24 * 60 * 60;
                uint duration = endTime - startTime;
                if (duration >= 24 * 60 * 60)
                {
                    //深夜時間帯の処理の関係で、不可条件が新たに発生しているため、その対応。
                    MessageBox.Show("24時間以上の録画時間は設定出来ません。", "録画時間長の確認", MessageBoxButton.OK);
                    return null;
                }

                data.startTime = startTime;
                data.durationSecond = duration;

                //曜日の処理、0～6bit目:日～土
                data.dayOfWeekFlag = 0;
                int val = 0;
                chbxList.ForEach(chbx => data.dayOfWeekFlag |= (byte)((chbx.IsChecked == true ? 0x01 : 0x00) << val++));

                //開始時刻を0～24時に調整する。
                data.RegulateData();

                data.IsEnabled = checkBox_keyDisabled.IsChecked != true;

                data.title = textBox_title.Text;

                var chItem = comboBox_service.SelectedItem as EpgServiceInfo;
                data.stationName = chItem.service_name;
                data.originalNetworkID = chItem.ONID;
                data.transportStreamID = chItem.TSID;
                data.serviceID = chItem.SID;
                data.recSetting = recSettingView.GetRecSetting();

                return data;
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            return null;
        }
        protected override bool SetData(ManualAutoAddData data)
        {
            if (data == null) return false;

            data = data.DeepClone();
            dataID = data.dataID;

            //深夜時間帯の処理
            if (Settings.Instance.LaterTimeUse == true && DateTime28.IsLateHour(data.PgStartTime.Hour) == true)
            {
                data.ShiftRecDay(-1);
            }

            //曜日の処理、0～6bit目:日～土
            int val = 0;
            chbxList.ForEach(chbx => chbx.IsChecked = (data.dayOfWeekFlag & (0x01 << val++)) != 0);

            checkBox_keyDisabled.IsChecked = data.IsEnabled == false;

            comboBox_startHH.SelectedIndex = (int)(data.startTime / (60 * 60));
            comboBox_startMM.SelectedIndex = (int)((data.startTime % (60 * 60)) / 60);
            comboBox_startSS.SelectedIndex = (int)(data.startTime % 60);

            //深夜時間帯の処理も含む
            uint endTime = data.startTime + data.durationSecond;
            if (endTime >= comboBox_endHH.Items.Count * 60 * 60 || endTime >= 24 * 60 * 60
                && DateTime28.JudgeLateHour(data.PgStartTime.AddSeconds(data.durationSecond), data.PgStartTime) == false)
            {
                //正規のデータであれば、必ず0～23時台かつstartTimeより小さくなる。
                endTime -= 24 * 60 * 60;
            }
            comboBox_endHH.SelectedIndex = (int)(endTime / (60 * 60));
            comboBox_endMM.SelectedIndex = (int)((endTime % (60 * 60)) / 60);
            comboBox_endSS.SelectedIndex = (int)(endTime % 60);

            textBox_title.Text = data.title;

            comboBox_service.SelectedItem = ChSet5.ChItem(data.Create64Key());
            if (comboBox_service.SelectedItem == null) comboBox_service.SelectedIndex = 0;

            recSettingView.SetDefSetting(data.recSetting);

            return true;
        }
    }
    public class AddManualAutoAddWindowBase : AutoAddWindow<AddManualAutoAddWindow, ManualAutoAddData>
    {
        public AddManualAutoAddWindowBase() { }//デザイナ用
        public AddManualAutoAddWindowBase(ManualAutoAddData data = null, AutoAddMode mode = AutoAddMode.Find) : base(data, mode) { }
    }
}
