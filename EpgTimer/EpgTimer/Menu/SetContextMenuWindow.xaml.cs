using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace EpgTimer
{
    using BoxExchangeEdit;
    using ComboItem = KeyValuePair<CtxmCode, string>;

    /// <summary>
    /// SetContextMenuWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SetContextMenuWindow : Window
    {
        private static MenuManager mm { get { return CommonManager.Instance.MM; } }

        private static ComboItem[] MenuCodeToTitle = new ComboItem[]{
            new ComboItem(CtxmCode.ReserveView, "予約一覧"),
            new ComboItem(CtxmCode.TunerReserveView, "使用予定チューナー"),
            new ComboItem(CtxmCode.RecInfoView, "録画済み一覧"),
            new ComboItem(CtxmCode.EpgAutoAddView, "キーワード自動予約登録"),
            new ComboItem(CtxmCode.ManualAutoAddView, "プログラム自動予約登録"),
            new ComboItem(CtxmCode.EpgView, "番組表(共通)"),
            new ComboItem(CtxmCode.SearchWindow, "検索/キーワード予約ダイアログ"),
            new ComboItem(CtxmCode.InfoSearchWindow, "予約情報検索ダイアログ"),
            new ComboItem(CtxmCode.EditChgMenu, "[編集]サブメニュー")
        };

        private static List<List<ICommand>> SettingTable = new List<List<ICommand>>{
            new List<ICommand>{EpgCmds.ChgOnOff},
            new List<ICommand>{EpgCmds.CopyItem},
            new List<ICommand>{EpgCmds.Delete},
            new List<ICommand>{EpgCmds.Delete2},
            new List<ICommand>{EpgCmds.AdjustReserve},
            new List<ICommand>{EpgCmdsEx.RestoreMenu},
            new List<ICommand>{EpgCmds.ProtectChange},
            new List<ICommand>{EpgCmds.ShowAddDialog},

            new List<ICommand>{EpgCmds.JumpReserve, EpgCmds.JumpRecInfo},
            new List<ICommand>{EpgCmds.JumpTuner},
            new List<ICommand>{EpgCmds.JumpTable},
            new List<ICommand>{EpgCmds.JumpListView},
            new List<ICommand>{EpgCmdsEx.ShowAutoAddDialogMenu},
            new List<ICommand>{},//オプション用のダミー行
            new List<ICommand>{},//オプション用のダミー行
            new List<ICommand>{EpgCmdsEx.ShowReserveDialogMenu},
            new List<ICommand>{EpgCmds.ToAutoadd},
            new List<ICommand>{EpgCmds.ReSearch},
            new List<ICommand>{},//オプション用のダミー行
            new List<ICommand>{EpgCmds.ReSearch2},
            new List<ICommand>{EpgCmds.Play},
            new List<ICommand>{EpgCmds.OpenFolder, EpgCmdsEx.OpenFolderMenu},

            new List<ICommand>{EpgCmds.CopyTitle},
            new List<ICommand>{EpgCmds.CopyContent},
            new List<ICommand>{EpgCmds.InfoSearchTitle},
            new List<ICommand>{EpgCmds.SearchTitle},
            new List<ICommand>{EpgCmds.InfoSearchRecTag},
            new List<ICommand>{EpgCmds.SearchRecTag},
            new List<ICommand>{EpgCmds.CopyRecTag},
            new List<ICommand>{EpgCmds.SetRecTag},
            new List<ICommand>{EpgCmds.CopyNotKey},
            new List<ICommand>{EpgCmds.SetNotKey},
            new List<ICommand>{EpgCmds.CopyNote},
            new List<ICommand>{EpgCmds.SetNote},

            new List<ICommand>{EpgCmds.MenuSetting}
        };

        public MenuSettingData info;
        List<CtxmSetting> defaultMenu;
        List<CtxmSetting> editMenu;

        List<CheckBox> stackItems_menu;
        List<CheckBox> stackItems_ges1;
        List<CheckBox> stackItems_ges2;

        public SetContextMenuWindow(Visual owner = null, MenuSettingData data = null)
        {
            InitializeComponent();
            try
	        {
                this.Owner = CommonUtil.GetTopWindow(owner);
                if (data != null) info = data.DeepClone();

                //共通設定画面用の設定
                Action<StackPanel, StackPanel> CopyStackItem = (src, trg) =>
                {
                    foreach (var item in src.Children.OfType<Control>().Where(i => i is Label != true))
                    {
                        var newItem = (Control)Activator.CreateInstance(item.GetType());
                        newItem.Style = item.Style;
                        if (item is CheckBox) newItem.Visibility = Visibility.Hidden;
                        trg.Children.Add(newItem);
                    }
                };
                CopyStackItem(stackPanel_menu, stackPanel_gesture);
                CopyStackItem(stackPanel_menu, stackPanel_gesture2);
                
                stackItems_menu = stackPanel_menu.Children.OfType<CheckBox>().ToList();
                stackItems_ges1 = stackPanel_gesture.Children.OfType<CheckBox>().ToList();
                stackItems_ges2 = stackPanel_gesture2.Children.OfType<CheckBox>().ToList();

                //個別設定画面用の設定
                var bx = new BoxExchangeEditor(this.listBox_Default, this.listBox_Setting, true, true, true, true);
                bx.AllowDuplication(StringItem.Items(EpgCmdsEx.SeparatorString), StringItem.Cloner, StringItem.Comparator);
                button_reset.Click += new RoutedEventHandler(bx.button_Reset_Click);
                button_add.Click += new RoutedEventHandler(bx.button_Add_Click);
                button_ins.Click += new RoutedEventHandler(bx.button_Insert_Click);
                button_del.Click += new RoutedEventHandler(bx.button_Delete_Click);
                button_delAll.Click += new RoutedEventHandler(bx.button_DeleteAll_Click);
                button_top.Click += new RoutedEventHandler(bx.button_Top_Click);
                button_up.Click += new RoutedEventHandler(bx.button_Up_Click);
                button_down.Click += new RoutedEventHandler(bx.button_Down_Click);
                button_bottom.Click += new RoutedEventHandler(bx.button_Bottom_Click);

                //その他画面用の設定
                foreach (var item in MenuCodeToTitle.Where(i => i.Key != CtxmCode.EditChgMenu))
                {
                    var chkbox = new CheckBox();
                    chkbox.Tag = item.Key;
                    chkbox.Content = item.Value;
                    chkbox.Checked += new RoutedEventHandler(checkBox_IsManualMenuCode_Checked);
                    chkbox.Unchecked += new RoutedEventHandler(checkBox_IsManualMenuCode_Checked);
                    wrapPanel_IsManualMenu.Children.Add(chkbox);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetData();
        }

        private void button_Initialize_Click(object sender, RoutedEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                mm.SetDefaultGestures(info);
            }
            else
            {
                info = mm.GetDefaultMenuSettingData();
            }
            SetData();
        }

        private void SetData()
        {
            try
            {
                this.DataContext = info;

                ManualMenuCheckboxWorking = true;
                foreach (var chkbox in IsManualChkBox)
                {
                    chkbox.IsChecked = info.IsManualAssign.Contains((CtxmCode)chkbox.Tag);
                }
                ManualMenuCheckboxWorking = false;
                checkBox_IsManualMenuCode_Checked(null, null);

                defaultMenu = mm.GetDefaultCtxmSettingForEditor();
                editMenu = info.ManualMenuItems.DeepClone();
                editMenu.ForEach(m => m.Items = m.Items.FindAll(i => defaultMenu.FindData(m.ctxmCode).Items.Contains(i)));

                for (int i = 0; i < SettingTable.Count; i++)
                {
                    if (SettingTable[i].Count == 0) continue;

                    MenuSettingData.CmdSaveData src = info.EasyMenuItems.Find(item => 
                        item.GetCommand() == SettingTable[i][0]);

                    stackItems_menu[i].IsChecked = src.IsMenuEnabled;
                    stackItems_ges1[i].IsChecked = src.IsGestureEnabled;
                    stackItems_ges2[i].IsChecked = !src.IsGesNeedMenu;

                    stackItems_ges1[i].Content = MenuBinds.GetInputGestureText(src.GetGestuers()) ?? "";
                    stackItems_ges2[i].Content = "使用する";

                    stackItems_ges1[i].Visibility = string.IsNullOrEmpty(stackItems_ges1[i].Content as string) ? Visibility.Hidden : Visibility.Visible;
                    stackItems_ges2[i].Visibility = stackItems_ges1[i].Visibility;
                }

                this.listBox_Default.ItemsSource = null;
                this.listBox_Setting.ItemsSource = null;//初期化ボタンでSetData()使うとき用のリセット。
                this.comboBoxViewSelect.ItemsSource = MenuCodeToTitle;
                this.comboBoxViewSelect.SelectedIndex = -1; //初期化ボタンでSetData()使うとき用のリセット。
                this.comboBoxViewSelect.SelectedIndex = this.comboBoxViewSelect.Items.Count - 1; //これでSelectionChanged発生する
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void button_OK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (comboBoxViewSelect.SelectedValue != null)
                {
                    var code = (CtxmCode)this.comboBoxViewSelect.SelectedValue;
                    editMenu.FindData(code).Items = this.listBox_Setting.Items.Cast<StringItem>().ValueList();
                }

                for (int i = 0; i < SettingTable.Count; i++)
                {
                    SettingTable[i].ForEach(icmd =>
                    {
                        MenuSettingData.CmdSaveData trg = info.EasyMenuItems.Find(item => item.GetCommand() == icmd);
                        trg.IsMenuEnabled = stackItems_menu[i].IsChecked == true;
                        trg.IsGestureEnabled = stackItems_ges1[i].IsChecked == true;
                        trg.IsGesNeedMenu = stackItems_ges2[i].IsChecked == false;
                    });
                }

                info.IsManualAssign = IsManualChkBox.Where(c => c.IsChecked == true).Select(c => (CtxmCode)c.Tag).ToList();
                info.ManualMenuItems = editMenu.DeepClone();

                DialogResult = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void comboBoxViewSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (comboBoxViewSelect.SelectedIndex == -1) return;

                if (this.listBox_Default.ItemsSource != null)
                {
                    CtxmCode oldcode = ((ComboItem)e.RemovedItems[0]).Key;
                    editMenu.FindData(oldcode).Items = this.listBox_Setting.Items.Cast<StringItem>().ValueList();
                }
                CtxmCode newcode = ((ComboItem)e.AddedItems[0]).Key;
                this.listBox_Default.ItemsSource = StringItem.Items(defaultMenu.FindData(newcode).Items);
                this.listBox_Setting.Items.Clear();
                this.listBox_Setting.Items.AddItems(StringItem.Items(editMenu.FindData(newcode).Items));

                switch (newcode)
                {
                    case CtxmCode.EpgView:
                        textblocExp.Text = "・「番組表(標準モード)へジャンプ」は標準モードでは非表示になります。";
                        break;
                    case CtxmCode.EditChgMenu:
                        textblocExp.Text = "・[編集]サブメニューは「表示項目は個別設定を使用する」の設定に関わらず、常にこの個別設定が反映されます。\r\n\r\n"
                        + "・「自動登録有効」「予約モード変更」「まとめてジャンル絞り込みを変更」は該当画面のみ表示されます。\r\n\r\n"
                        + "・「イベントリレー追従」「ぴったり（？）録画」は、プログラム自動予約登録画面では表示されません。";
                            break;
                    default:
                        textblocExp.Text = "";
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void button_allDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                editMenu = mm.GetDefaultMenuSettingData().ManualMenuItems;
                if (comboBoxViewSelect.SelectedValue != null)
                {
                    var code = (CtxmCode)this.comboBoxViewSelect.SelectedValue;
                    this.listBox_Setting.Items.Clear();
                    this.listBox_Setting.Items.AddItems(StringItem.Items(editMenu.FindData(code).Items));
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void button_separator_Click(object sender, RoutedEventArgs e)
        {
            listBox_Setting.ScrollIntoViewLast(new StringItem(EpgCmdsEx.SeparatorString));
        }

        private bool ManualMenuCheckboxWorking = false;
        private IEnumerable<CheckBox> IsManualChkBox { get { return wrapPanel_IsManualMenu.Children.OfType<CheckBox>(); } }
        private void checkBox_IsManualMenu_Checked(object sender, RoutedEventArgs e)
        {
            if (ManualMenuCheckboxWorking == true) return;
            ManualMenuCheckboxWorking = true;

            foreach (var chkbox in IsManualChkBox)
            {
                chkbox.IsChecked = checkBox_IsManualMenu.IsChecked;
            }

            ManualMenuCheckboxWorking = false;
        }
        private void checkBox_IsManualMenuCode_Checked(object sender, RoutedEventArgs e)
        {
            if (ManualMenuCheckboxWorking == true) return;
            ManualMenuCheckboxWorking = true;

            int count = IsManualChkBox.Where(c => c.IsChecked == true).Count();
            checkBox_IsManualMenu.IsChecked = count == 0 ? false : count == IsManualChkBox.Count() ? (bool?)true : null;

            ManualMenuCheckboxWorking = false;
        }
    }
}
