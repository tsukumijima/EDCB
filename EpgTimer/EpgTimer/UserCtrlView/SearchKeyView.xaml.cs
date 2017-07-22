﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    using BoxExchangeEdit;

    /// <summary>
    /// SearchKey.xaml の相互作用ロジック
    /// </summary>
    public partial class SearchKeyView : UserControl, IPresetItemView
    {
        public const string ClearButtonTooltip = "即時にクリアされ、設定画面をキャンセルしても戻りません";

        private List<ServiceViewItem> serviceList;
        private List<CheckBox> chbxWeekList;

        private PresetEditor<SearchPresetItem> preEdit = new PresetEditor<SearchPresetItem>();
        private ComboBox comboBox_preSet;

        public SearchKeyView()
        {
            InitializeComponent();

            Settings.Instance.AndKeyList.ForEach(s => comboBox_andKey.Items.Add(s));
            Settings.Instance.NotKeyList.ForEach(s => comboBox_notKey.Items.Add(s));
            Button_clearAndKey.Click += (sender, e) => ClearSerchLog(comboBox_andKey, Settings.Instance.AndKeyList);
            Button_clearNotKey.Click += (sender, e) => ClearSerchLog(comboBox_notKey, Settings.Instance.NotKeyList);

            comboBox_content.ItemsSource = CommonManager.ContentKindList;
            comboBox_content.SelectedIndex = 0;
            comboBox_content.KeyUp += ViewUtil.KeyDown_Enter(button_content_add);

            comboBox_time_sw.ItemsSource = CommonManager.DayOfWeekArray;
            comboBox_time_sw.SelectedIndex = 0;
            comboBox_time_sh.ItemsSource = Enumerable.Range(0, 24);
            comboBox_time_sh.SelectedIndex = 0;
            comboBox_time_sm.ItemsSource = Enumerable.Range(0, 60);
            comboBox_time_sm.SelectedIndex = 0;
            comboBox_time_ew.ItemsSource = CommonManager.DayOfWeekArray;
            comboBox_time_ew.SelectedIndex = 6;
            comboBox_time_eh.ItemsSource = Enumerable.Range(0, 24);
            comboBox_time_eh.SelectedIndex = 23;
            comboBox_time_em.ItemsSource = Enumerable.Range(0, 60);
            comboBox_time_em.SelectedIndex = 59;
            comboBox_week_sh.ItemsSource = CommonManager.CustomHourList;
            comboBox_week_sh.SelectedIndex = 0;
            comboBox_week_sm.ItemsSource = Enumerable.Range(0, 60);
            comboBox_week_sm.SelectedIndex = 0;
            comboBox_week_eh.ItemsSource = CommonManager.CustomHourList;
            comboBox_week_eh.SelectedIndex = 23;
            comboBox_week_em.ItemsSource = Enumerable.Range(0, 60);
            comboBox_week_em.SelectedIndex = 59;
            radioButton_time.IsChecked = true;

            new BoxExchangeEdit.BoxExchangeEditor(null, listView_service, true);
            SelectableItem.Set_CheckBox_PreviewChanged(listView_service);
            serviceList = ChSet5.ChListSelected.Select(info => new ServiceViewItem(info)).ToList();
            listView_service.ItemsSource = serviceList;
            listView_service.FitColumnWidth();//他は勝手にフィットするのに‥なぜこれだけ？

            var bxc = new BoxExchangeEdit.BoxExchangeEditor(null, listBox_content, true, true);
            button_content_clear.Click += (sender, e) => { bxc.button_DeleteAll_Click(sender, e); CheckListBox(listBox_content); };
            button_content_del.Click += (sender, e) => { bxc.button_Delete_Click(sender, e); CheckListBox(listBox_content); };
            button_content_add.Click += button_content_add_Click;
            bxc.targetBoxAllowKeyAction(listBox_content, (sender, e) => button_content_del.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            listBox_content.Tag = new { ListBoxView = "(全ジャンル)" };
            CheckListBox(listBox_content);

            var bxd = new BoxExchangeEdit.BoxExchangeEditor(null, listBox_date, true, true);
            button_date_clear.Click += (sender, e) => { bxd.button_DeleteAll_Click(sender, e); CheckListBox(listBox_date); };
            button_date_del.Click += (sender, e) => { bxd.button_Delete_Click(sender, e); CheckListBox(listBox_date); };
            button_date_add.Click += button_date_add_Click;
            bxd.targetBoxAllowKeyAction(listBox_date, (sender, e) => button_date_del.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            listBox_date.Tag = "(全期間)";
            CheckListBox(listBox_date);

            chbxWeekList = CommonManager.DayOfWeekArray.Select(wd =>
                new CheckBox { Content = wd, Margin = new Thickness(0, 0, 5, 0) }).ToList();
            chbxWeekList.ForEach(chbx => stack_data_week.Children.Add(chbx));

            grid_PresetEdit.Children.Clear();
            grid_PresetEdit.Children.Add(preEdit);
            comboBox_preSet = preEdit.comboBox_preSet;
            preEdit.Set(this,
                (item, msg) => UpdateView(item),
                (list, mode) =>
                {
                    Settings.Instance.SearchPresetList = list;
                    if (comboBox_preSet.SelectedItem == null)
                    {
                        preEdit.ChangeSelect(preEdit.FindPreset(PresetItem.CustomID), null, true);
                    }
                },
                "検索プリセット", SetSearchPresetWindow.SettingWithDialog);
            if (Settings.Instance.UseLastSearchKey == true)
            {
                comboBox_preSet.Items.Add(new SearchPresetItem("前回検索条件", SearchPresetItem.LastID, null));
            }
            checkBox_setWithoutSearchKeyWord.IsChecked = Settings.Instance.SetWithoutSearchKeyWord;
        }

        public void SetData(object data) { SetSearchKey(data as EpgSearchKeyInfo); }
        public object GetData() { return GetSearchKey(); }
        public IEnumerable<PresetItem> DefPresetList() { return Settings.Instance.SearchPresetList.Clone(); }

        protected virtual void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = e.OriginalSource as TextBox;
            if (tb != null)
            {
                ((ComboBox)sender).Text = tb.Text;
            }
        }

        public void AddSearchLog()
        {
            if (Settings.Instance.SaveSearchKeyword == false) return;

            AddSerchLog(comboBox_andKey, Settings.Instance.AndKeyList);
            AddSerchLog(comboBox_notKey, Settings.Instance.NotKeyList);
        }
        private void AddSerchLog(ComboBox box, List<string> log)
        {
            try
            {
                string searchWord = box.Text;
                if (string.IsNullOrEmpty(searchWord) == true) return;

                box.Items.Remove(searchWord);
                box.Items.Insert(0, searchWord);
                box.Text = searchWord;

                SaveSearchLogSettings(box, log);
            }
            catch { }
        }
        private void comboBox_andKey_KeyUp(object sender, KeyEventArgs e)
        {
            comboBox_KeyUp(sender, e, Settings.Instance.AndKeyList);
        }
        private void comboBox_notKey_KeyUp(object sender, KeyEventArgs e)
        {
            comboBox_KeyUp(sender, e, Settings.Instance.NotKeyList);
        }
        private void comboBox_KeyUp(object sender, KeyEventArgs e, List<string> log)
        {
            var box = sender as ComboBox;
            if (e.Key == Key.Delete && box.IsDropDownOpen)
            {
                int i = box.SelectedIndex;
                if (i >= 0)
                {
                    box.Items.RemoveAt(i);
                    box.SelectedIndex = Math.Min(i, box.Items.Count - 1);
                    SaveSearchLogSettings(box, log);
                }
            }
        }
        private void ClearSerchLog(ComboBox box, List<string> log)
        {
            string searchWord = box.Text;
            box.Items.Clear();
            box.Text = searchWord;
            SaveSearchLogSettings(box, log);
        }
        private void SaveSearchLogSettings(ComboBox box, List<string> log)
        {
            log.Clear();
            log.AddRange(box.Items.OfType<string>().Take(30));
        }

        public EpgSearchKeyInfo GetSearchKey()
        {
            var key = new EpgSearchKeyInfo();
            try
            {
                key.andKey = comboBox_andKey.Text;
                key.notKey = comboBox_notKey.Text;
                key.regExpFlag = checkBox_regExp.IsChecked == true ? 1 : 0;
                key.aimaiFlag = (byte)(checkBox_aimai.IsChecked == true ? 1 : 0);
                key.titleOnlyFlag = checkBox_titleOnly.IsChecked == true ? 1 : 0;
                key.caseFlag = (byte)(checkBox_case.IsChecked == true ? 1 : 0);
                key.keyDisabledFlag = (byte)(checkBox_keyDisabled.IsChecked == true ? 1 : 0);
                key.contentList = listBox_content.Items.OfType<ContentKindInfo>().Select(info => info.Data).Clone();
                key.notContetFlag = (byte)(checkBox_notContent.IsChecked == true ? 1 : 0);
                key.serviceList = serviceList.Where(info => info.IsSelected == true).Select(info => (Int64)info.Key).ToList();
                key.dateList = listBox_date.Items.OfType<DateItem>().Select(info => info.DateInfo).ToList();
                key.notDateFlag = (byte)(checkBox_notDate.IsChecked == true ? 1 : 0);
                key.freeCAFlag = (byte)Math.Min(Math.Max(comboBox_free.SelectedIndex, 0), 2);
                key.chkRecEnd = (byte)(checkBox_chkRecEnd.IsChecked == true ? 1 : 0);
                key.chkRecDay = (ushort)MenuUtil.MyToNumerical(textBox_chkRecDay, Convert.ToUInt32, 9999u, 0u, 0u);
                key.chkRecNoService = (byte)(radioButton_chkRecNoService2.IsChecked == true ? 1 : 0);
                key.chkDurationMin = (ushort)MenuUtil.MyToNumerical(textBox_chkDurationMin, Convert.ToUInt32, 9999u, 0u, 0u);
                key.chkDurationMax = (ushort)MenuUtil.MyToNumerical(textBox_chkDurationMax, Convert.ToUInt32, 9999u, 0u, 0u);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            return key;
        }
        public void SetSearchKey(EpgSearchKeyInfo key)
        {
            //"登録時"を追加する。既存があれば追加前に削除する。検索ダイアログの上下ボタンの移動用のコード。
            comboBox_preSet.Items.Remove(preEdit.FindPreset(SearchPresetItem.CustomID));
            comboBox_preSet.Items.Add(new SearchPresetItem("登録時", SearchPresetItem.CustomID, key.Clone()));
            loadingSetting = true;
            comboBox_preSet.SelectedIndex = comboBox_preSet.Items.Count - 1;
        }
        private bool loadingSetting = true;
        public void UpdateView(SearchPresetItem pItem)
        {
            try
            {
                EpgSearchKeyInfo key = pItem.Data ?? Settings.Instance.DefSearchKey;
                if (loadingSetting == true || checkBox_setWithoutSearchKeyWord.IsChecked == false)
                {
                    comboBox_andKey.Text = key.andKey;
                    comboBox_notKey.Text = key.notKey;
                }
                checkBox_regExp.IsChecked = key.regExpFlag == 1;
                checkBox_aimai.IsChecked = key.aimaiFlag == 1;
                checkBox_titleOnly.IsChecked = key.titleOnlyFlag == 1;
                checkBox_case.IsChecked = key.caseFlag == 1;
                checkBox_keyDisabled.IsChecked = key.keyDisabledFlag == 1;

                listBox_content.Items.Clear();
                key.contentList.ForEach(item => listBox_content.Items.Add(CommonManager.ContentKindInfoForDisplay(item)));
                CheckListBox(listBox_content);
                checkBox_notContent.IsChecked = key.notContetFlag == 1;

                listBox_date.Items.Clear();
                key.dateList.ForEach(info => listBox_date.Items.Add(new DateItem(info)));
                CheckListBox(listBox_date);
                checkBox_notDate.IsChecked = key.notDateFlag == 1;

                var serviceKeyHash = new HashSet<long>(key.serviceList);
                serviceList.ForEach(info => info.IsSelected = serviceKeyHash.Contains((long)info.Key));
                listView_service.ScrollIntoView(serviceList.Find(i => i.IsSelected == true));

                button_dttv_on.IsEnabled = serviceList.Any(item => item.ServiceInfo.IsDttv == true);
                button_bs_on.IsEnabled = serviceList.Any(item => item.ServiceInfo.IsBS == true);
                button_cs_on.IsEnabled = serviceList.Any(item => item.ServiceInfo.IsCS == true);
                button_1seg_on.IsEnabled = serviceList.Any(item => item.ServiceInfo.PartialFlag == true);

                comboBox_free.SelectedIndex = key.freeCAFlag % 3;
                checkBox_chkRecEnd.IsChecked = key.chkRecEnd == 1;
                textBox_chkRecDay.Text = key.chkRecDay.ToString();
                radioButton_chkRecNoService1.IsChecked = key.chkRecNoService == 0;
                radioButton_chkRecNoService2.IsChecked = key.chkRecNoService != 0;
                textBox_chkDurationMin.Text = key.chkDurationMin.ToString();
                textBox_chkDurationMax.Text = key.chkDurationMax.ToString();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
            loadingSetting = false;
        }

        public void SetChangeMode(int chgMode)
        {
            switch (chgMode)
            {
                case 0:
                    ViewUtil.SetSpecificChgAppearance(listBox_content);
                    listBox_content.Focus();
                    if (listBox_content.Items.Count != 0) listBox_content.SelectedIndex = 0;
                    break;
            }
            stackPanel_PresetEdit.Visibility = chgMode == int.MaxValue ? Visibility.Collapsed : Visibility.Visible;
            Button_clearAndKey.ToolTip = chgMode == int.MaxValue ? SearchKeyView.ClearButtonTooltip : null;
            Button_clearNotKey.ToolTip = Button_clearAndKey.ToolTip;
        }

        private void button_content_add_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_content.SelectedItem != null)
            {
                var key = (comboBox_content.SelectedItem as ContentKindInfo).Data.Key;
                if (listBox_content.Items.OfType<ContentKindInfo>().Any(item => item.Data.Key == key) == true)
                { return; }

                listBox_content.ScrollIntoViewLast(comboBox_content.SelectedItem);
                CheckListBox(listBox_content);
            }
        }
        private void CheckListBox(ListBox box)
        {
            box.Items.Remove(box.Tag);
            box.IsEnabled = box.Items.Count != 0;
            if (box.IsEnabled == false)
            {
                box.Items.Add(box.Tag);
            }
        }

        private void button_all_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info => info.IsSelected = true);
        }
        private void button_all_off_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info => info.IsSelected = false);
        }
        private void button_video_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info => info.IsSelected = info.ServiceInfo.IsVideo);
        }
        private void button_bs_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info =>
            {
                if (info.ServiceInfo.IsBS == true && info.ServiceInfo.IsVideo == true)
                {
                    info.IsSelected = true;
                }
            });
        }
        private void button_cs_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info =>
            {
                if (info.ServiceInfo.IsCS == true && info.ServiceInfo.IsVideo == true)
                {
                    info.IsSelected = true;
                }
            });
        }
        private void button_dttv_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info =>
            {
                if (info.ServiceInfo.IsDttv == true && info.ServiceInfo.IsVideo == true)
                {
                    info.IsSelected = true;
                }
            });
        }
        private void button_1seg_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info =>
            {
                if (info.ServiceInfo.IsDttv == true && info.ServiceInfo.PartialFlag == true)
                {
                    info.IsSelected = true;
                }
            });
        }
        private void button_other_on_Click(object sender, RoutedEventArgs e)
        {
            serviceList.ForEach(info =>
            {
                if (info.ServiceInfo.IsOther == true)
                {
                    info.IsSelected = true;
                }
            });
        }

        private void button_date_add_Click(object sender, RoutedEventArgs e)
        {
            var addItems = new List<object>();
            if (radioButton_time.IsChecked == true)
            {
                var info = new EpgSearchDateInfo();
                info.startDayOfWeek = (byte)Math.Min(Math.Max(comboBox_time_sw.SelectedIndex, 0), 6);
                info.startHour = (ushort)Math.Min(Math.Max(comboBox_time_sh.SelectedIndex, 0), 23);
                info.startMin = (ushort)Math.Min(Math.Max(comboBox_time_sm.SelectedIndex, 0), 59);
                info.endDayOfWeek = (byte)Math.Min(Math.Max(comboBox_time_ew.SelectedIndex, 0), 6);
                info.endHour = (ushort)Math.Min(Math.Max(comboBox_time_eh.SelectedIndex, 0), 23);
                info.endMin = (ushort)Math.Min(Math.Max(comboBox_time_em.SelectedIndex, 0), 59);
                addItems.Add(new DateItem(info));
            }
            else
            {
                int startHour = Math.Min(Math.Max(comboBox_week_sh.SelectedIndex, 0), comboBox_week_sh.Items.Count - 1);
                int startMin = Math.Min(Math.Max(comboBox_week_sm.SelectedIndex, 0), 59);
                int endHour = Math.Min(Math.Max(comboBox_week_eh.SelectedIndex, 0), comboBox_week_eh.Items.Count - 1);
                int endMin = Math.Min(Math.Max(comboBox_week_em.SelectedIndex, 0), 59);

                for (byte day = 0; day < 7; day++)
                {
                    if (chbxWeekList[day].IsChecked != true) continue;
                    //
                    var info = new EpgSearchDateInfo();
                    info.startDayOfWeek = day;
                    info.startHour = (ushort)startHour;
                    info.startMin = (ushort)startMin;
                    info.endDayOfWeek = info.startDayOfWeek;
                    info.endHour = (ushort)endHour;
                    info.endMin = (ushort)endMin;
                    info.RegulateData();
                    addItems.Add(new DateItem(info));
                }
            }
            listBox_date.Items.AddItems(addItems);
            listBox_date.UnselectAll();
            listBox_date.SelectedItemsAdd(addItems);
            listBox_date.ScrollIntoViewIndex(int.MaxValue);
            CheckListBox(listBox_date);
        }

        private void comboBox_content_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_content.IsDropDownOpen)
            {
                button_content_add_Click(null, null);
            }
        }

        private void checkBox_setWithoutSearchKeyWord_Click(object sender, RoutedEventArgs e)
        {
            //これはダイアログの設定なので即座に反映
            Settings.Instance.SetWithoutSearchKeyWord = (checkBox_setWithoutSearchKeyWord.IsChecked == true);
        }
    }

    public class DateItem
    {
        public DateItem(EpgSearchDateInfo info) { DateInfo = info; }
        public EpgSearchDateInfo DateInfo { get; private set; }
        public override string ToString() { return CommonManager.ConvertTimeText(DateInfo); }
    }
}