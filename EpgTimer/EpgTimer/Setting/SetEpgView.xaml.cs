using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;

namespace EpgTimer.Setting
{
    using BoxExchangeEdit;

    /// <summary>
    /// SetEpgView.xaml の相互作用ロジック
    /// </summary>
    public partial class SetEpgView : UserControl
    {
        private Settings settings { get { return (Settings)DataContext; } }

        public bool IsChangeRecInfoDropExcept { get; private set; }

        //デザイン管理用
        private HashSet<int> idxHash = new HashSet<int>();

        public SetEpgView()
        {
            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                stackPanel_epgArchivePeriod.IsEnabled = false;
                stackPanel_DropLogThresh.IsEnabled = false;
            }

            textBox_des_name.KeyDown += ViewUtil.KeyDown_Enter(btn_des_name);
            listBox_tab.KeyDown += ViewUtil.KeyDown_Enter(button_tab_chg);
            SelectableItem.Set_CheckBox_PreviewChanged(listBox_tab);
            var bx = new BoxExchangeEditor(null, this.listBox_tab, true, true, true);
            bx.targetBoxAllowDoubleClick(bx.TargetBox, (sender, e) => button_tab_chg.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            button_tab_del.Click += bx.button_Delete_Click;
            button_tab_del_all.Click += bx.button_DeleteAll_Click;
            button_tab_up.Click += bx.button_Up_Click;
            button_tab_down.Click += bx.button_Down_Click;
            button_tab_top.Click += bx.button_Top_Click;
            button_tab_bottom.Click += bx.button_Bottom_Click;
            button_RecInfoDropExceptDefault.Click += (sender, e) => textBox_RecInfoDropExcept.Text = string.Join(", ", Settings.RecInfoDropExceptDefault);

            var FLanguage = XmlLanguage.GetLanguage("ja-JP");
            comboBox_fontTitle.ItemsSource = Fonts.SystemFontFamilies.Select(f => f.FamilyNames.ContainsKey(FLanguage) == true ? f.FamilyNames[FLanguage] : f.Source).OrderBy(s => s).ToList();

            RadioButtonTagConverter.SetBindingButtons(CommonUtil.NameOf(() => settings.EpgSettingList[0].EpgPopupMode), panel_epgPopup);
            cmb_design.SelectedValuePath = CommonUtil.NameOf(() => settings.EpgSettingList[0].ID);
            cmb_design.DisplayMemberPath = CommonUtil.NameOf(() => settings.EpgSettingList[0].Name);

            RadioButtonTagConverter.SetBindingButtons(CommonUtil.NameOf(() => settings.EpgSettingList[0].EpgChangeBorderMode), panel_EpgBorderColor);
            RadioButtonTagConverter.SetBindingButtons(CommonUtil.NameOf(() => settings.TunerChangeBorderMode), panel_TunerBorderColor);
            
            //カラー関係はまとめてバインドする
            var colorReference = typeof(Brushes).GetProperties().Select(p => new ColorComboItem(p.Name, (Brush)p.GetValue(null, null))).ToList();
            colorReference.Add(new ColorComboItem("カスタム", this.Resources["HatchBrush"] as VisualBrush));
            var setComboColor1 = new Action<string, ComboBox>((path, cmb) =>
            {
                cmb.ItemsSource = colorReference;
                SetBindingColorCombo(cmb, path);
            });
            var setComboColors = new Action<string, Panel>((path, pnl) =>
            {
                foreach (var cmb in pnl.Children.OfType<ComboBox>())
                {
                    setComboColor1(path + "[" + (string)cmb.Tag + "]", cmb);
                }
            });
            setComboColor1(CommonUtil.NameOf(() => EpgStyle.TitleColor1), comboBox_colorTitle1);
            setComboColor1(CommonUtil.NameOf(() => EpgStyle.TitleColor2), comboBox_colorTitle2);
            setComboColors(CommonUtil.NameOf(() => EpgStyle.ContentColorList), grid_EpgColors);
            setComboColors(CommonUtil.NameOf(() => EpgStyle.EpgResColorList), grid_EpgColorsReserve);
            setComboColors(CommonUtil.NameOf(() => EpgStyle.EpgEtcColors), grid_EpgTimeColors);
            setComboColors(CommonUtil.NameOf(() => EpgStyle.EpgEtcColors), grid_EpgEtcColors);
            setComboColors(CommonUtil.NameOf(() => settings.TunerServiceColors), grid_TunerFontColor);
            setComboColors(CommonUtil.NameOf(() => settings.TunerServiceColors), grid_TunerColors);
            setComboColors(CommonUtil.NameOf(() => settings.TunerServiceColors), grid_TunerEtcColors);
            
            var setButtonColors = new Action<string, Panel>((path, pnl) =>
            {
                foreach (var btn in pnl.Children.OfType<Button>())
                {
                    SetBindingColorButton(btn, path + "[" + (string)btn.Tag + "]");
                }
            });
            SetBindingColorButton(button_colorTitle1, CommonUtil.NameOf(() => EpgStyle.TitleCustColor1));
            SetBindingColorButton(button_colorTitle2, CommonUtil.NameOf(() => EpgStyle.TitleCustColor2));
            setButtonColors(CommonUtil.NameOf(() => EpgStyle.ContentCustColorList), grid_EpgColors);
            setButtonColors(CommonUtil.NameOf(() => EpgStyle.EpgResCustColorList), grid_EpgColorsReserve);
            setButtonColors(CommonUtil.NameOf(() => EpgStyle.EpgEtcCustColors), grid_EpgTimeColors);
            setButtonColors(CommonUtil.NameOf(() => EpgStyle.EpgEtcCustColors), grid_EpgEtcColors);
            setButtonColors(CommonUtil.NameOf(() => settings.TunerServiceCustColors), grid_TunerFontColor);
            setButtonColors(CommonUtil.NameOf(() => settings.TunerServiceCustColors), grid_TunerColors);
            setButtonColors(CommonUtil.NameOf(() => settings.TunerServiceCustColors), grid_TunerEtcColors);

            //録画済み一覧画面
            setButtonColors(CommonUtil.NameOf(() => settings.RecEndCustColors), grid_RecInfoBackColors);
            setComboColors(CommonUtil.NameOf(() => settings.RecEndColors), grid_RecInfoBackColors);

            //予約一覧・共通画面
            SetBindingColorButton(btn_ListDefFontColor, CommonUtil.NameOf(() => settings.ListDefCustColor));
            SetBindingColorButton(btn_ListRuledLineColor, CommonUtil.NameOf(() => settings.ListRuledLineCustColor));
            setButtonColors(CommonUtil.NameOf(() => settings.RecModeFontCustColors), grid_ReserveRecModeColors);
            setButtonColors(CommonUtil.NameOf(() => settings.ResBackCustColors), grid_ReserveBackColors);
            setButtonColors(CommonUtil.NameOf(() => settings.StatCustColors), grid_StatColors);
            setComboColor1(CommonUtil.NameOf(() => settings.ListDefColor), cmb_ListDefFontColor);
            setComboColor1(CommonUtil.NameOf(() => settings.ListRuledLineColor), cmb_ListRuledLineColor);
            setComboColors(CommonUtil.NameOf(() => settings.RecModeFontColors), grid_ReserveRecModeColors);
            setComboColors(CommonUtil.NameOf(() => settings.ResBackColors), grid_ReserveBackColors);
            setComboColors(CommonUtil.NameOf(() => settings.StatColors), grid_StatColors);

            button_clearSerchKeywords.ToolTip = SearchKeyView.ClearButtonTooltip;
            checkBox_NotNoStyle.ToolTip = string.Format("チェック時、テーマファイル「{0}」があればそれを、無ければ既定のテーマ(Aero)を適用します。", SettingPath.ModuleName + ".rd.xaml");
            checkBox_ApplyContextMenuStyle.ToolTip = string.Format("チェック時、テーマファイル「{0}」があればそれを、無ければ既定のテーマ(Aero)を適用します。", SettingPath.ModuleName + ".rdcm.xaml");

            comboBox_startTab.ItemsSource = new Dictionary<CtxmCode, string> {
                        { CtxmCode.ReserveView, "予約一覧" },{ CtxmCode.TunerReserveView, "使用予定チューナー" },
                        { CtxmCode.RecInfoView, "録画済み一覧" },{ CtxmCode.EpgAutoAddView, "キーワード自動予約登録" },
                        { CtxmCode.ManualAutoAddView, "プログラム自動予約登録" },{ CtxmCode.EpgView, "番組表" }};

            comboBox_mainViewButtonsDock.ItemsSource = new Dictionary<Dock, string> {
                        { Dock.Bottom, "下" },{ Dock.Top, "上" },{ Dock.Left, "左" },{ Dock.Right, "右" }};
        }

        private List<TabItem> EpgSettingTabs { get { return new List<TabItem> { tabEpgBasic, tabEpgBasic2, tabEpgColor, tabEpgColor2 }; } }
        public void LoadSetting()
        {
            //番組表
            panel_fontReplaceEditFont.DataContext = settings;

            settings.EpgSettingList.ForEach(s => idxHash.Add(s.ID));
            SetDesignCombo(Math.Max(0, cmb_design.SelectedIndex));

            int epgArcHour = IniFileHandler.GetPrivateProfileInt("SET", "EpgArchivePeriodHour", 0, SettingPath.TimerSrvIniPath);
            textBox_epgArchivePeriod.Text = Math.Min(Math.Max(epgArcHour / 24, 0), 20000).ToString();

            listBox_tab.Items.Clear();
            listBox_tab.Items.AddItems(settings.CustomEpgTabList.Select(info => new CustomEpgTabInfoView(info, () => settings)));
            listBox_tab.SelectedIndex = 0;

            //録画済み一覧画面
            textBox_DropSaveThresh.Text = IniFileHandler.GetPrivateProfileInt("SET", "DropSaveThresh", 0, SettingPath.EdcbIniPath).ToString();
            textBox_ScrambleSaveThresh.Text = IniFileHandler.GetPrivateProfileInt("SET", "ScrambleSaveThresh", -1, SettingPath.EdcbIniPath).ToString();
            textBox_RecInfoDropExcept.Text = string.Join(", ", settings.RecInfoDropExcept);

            //予約一覧・共通画面
            textBox_LaterTimeHour.Text = (settings.LaterTimeHour + 24).ToString();
            checkBox_picUpCustom.DataContext = settings.PicUpTitleWork;

            checkBox_FontBoldReplacePattern_Click(null, null);
            checkBox_ReplacePatternEditFontShare_Click(null, null);
        }

        public void SaveSetting()
        {
            //番組表
            int epgArcDay = (int)MenuUtil.MyToNumerical(textBox_epgArchivePeriod, Convert.ToDouble, 20000, 0, 0);
            IniFileHandler.WritePrivateProfileString("SET", "EpgArchivePeriodHour", epgArcDay * 24, SettingPath.TimerSrvIniPath);

            settings.CustomEpgTabList = listBox_tab.Items.OfType<CustomEpgTabInfoView>().Select(item => item.Info).ToList();
            settings.SetCustomEpgTabInfoID();

            //録画済み一覧画面
            IniFileHandler.WritePrivateProfileString("SET", "DropSaveThresh", textBox_DropSaveThresh.Text, SettingPath.EdcbIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "ScrambleSaveThresh", textBox_ScrambleSaveThresh.Text, SettingPath.EdcbIniPath);
            settings.RecInfoDropExcept = textBox_RecInfoDropExcept.Text.Split(',')
                .Where(s => string.IsNullOrWhiteSpace(s) == false).Select(s => s.Trim()).ToList();
            IsChangeRecInfoDropExcept = settings.RecInfoDropExcept.SequenceEqual(Settings.Instance.RecInfoDropExcept) == false;

            //予約一覧・共通画面
            settings.LaterTimeHour = MenuUtil.MyToNumerical(textBox_LaterTimeHour, Convert.ToInt32, 36, 24, 28) - 24;
            if (settings.UseLastSearchKey == false) settings.DefSearchKey = new EpgSearchKeyInfo();
        }

        EpgSetting EpgStyle { get { return (EpgSetting)tabEpgBasic.DataContext; } }
        private void cmb_des_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmb_design.SelectedIndex < 0) return;
            EpgSettingTabs.ForEach(tab => tab.DataContext = null);
            EpgSettingTabs.ForEach(tab => tab.DataContext = settings.EpgSettingList[cmb_design.SelectedIndex]);
        }
        private void SetDesignCombo(int new_idx)
        {
            cmb_design.ItemsSource = null;
            cmb_design.ItemsSource = settings.EpgSettingList;
            cmb_design.SelectedIndex = -1;
            cmb_design.SelectedIndex = new_idx;
            listBox_tab.FitColumnWidth();
        }
        private void btn_des_add_Click(object sender, RoutedEventArgs e)
        {
            var set = (cmb_design.SelectedItem as EpgSetting);
            AddEpgSettingItem(set != null ? set.DeepClone() : EpgSetting.DefSetting, textBox_des_name.Text);
            textBox_des_name.Clear();
            SetDesignCombo(settings.EpgSettingList.Count - 1);
        }
        private void btn_des_name_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_design.SelectedIndex < 0) return;

            ((EpgSetting)cmb_design.SelectedItem).Name = textBox_des_name.Text;
            textBox_des_name.Clear();
            SetDesignCombo(cmb_design.SelectedIndex);
        }
        private void btn_des_reset_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_design.SelectedIndex < 0) return;

            ((EpgSetting)cmb_design.SelectedItem).Reset();
            SetDesignCombo(cmb_design.SelectedIndex);
        }
        private void btn_des_delete_Click(object sender, RoutedEventArgs e)
        {
            if (cmb_design.SelectedIndex < 0) return;

            settings.EpgSettingList.RemoveAt(cmb_design.SelectedIndex);
            if (settings.EpgSettingList.Count == 0)
            {
                AddEpgSettingItem(EpgSetting.DefSetting);
            }
            settings.CustomEpgTabList.ForEach(tab =>
            {
                tab.EpgSettingIndex = settings.EpgSettingList.FindIndex(set => tab.EpgSettingID == set.ID);
                if (tab.EpgSettingIndex < 0)
                {
                    tab.EpgSettingIndex = 0;
                    tab.EpgSettingID = settings.EpgSettingList[0].ID;
                }
            });
            SetDesignCombo(Math.Min(cmb_design.SelectedIndex, settings.EpgSettingList.Count - 1));
        }
        private void AddEpgSettingItem(EpgSetting set, string name = "")
        {
            set.Name = name;
            set.ID = Enumerable.Range(0, idxHash.Count + 1).FirstOrDefault(idx => idxHash.Contains(idx) == false);
            idxHash.Add(set.ID);

            settings.EpgSettingList.Add(set);
        }

        private void button_tab_add_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new EpgDataViewSettingWindow(null, settings.EpgSettingList);
            dlg.Owner = CommonUtil.GetTopWindow(this);
            if (dlg.ShowDialog() == true)
            {
                listBox_tab.ScrollIntoViewLast(new CustomEpgTabInfoView(dlg.GetSetting(), () => settings));
                listBox_tab.FitColumnWidth();
            }
        }
        private void button_tab_chg_Click(object sender, RoutedEventArgs e)
        {
            if (listBox_tab.SelectedItem == null)
            {
                listBox_tab.SelectedIndex = 0;
            }
            var item = listBox_tab.SelectedItem as CustomEpgTabInfoView;
            if (item != null)
            {
                listBox_tab.UnselectAll();
                listBox_tab.SelectedItem = item;
                var dlg = new EpgDataViewSettingWindow(item.Info, settings.EpgSettingList);
                dlg.Owner = CommonUtil.GetTopWindow(this);
                if (dlg.ShowDialog() == true)
                {
                    item.Info = dlg.GetSetting();
                    listBox_tab.FitColumnWidth();
                }
            }
            else
            {
                button_tab_add_Click(null, null);
            }
        }

        private void button_tab_clone_Click(object sender, RoutedEventArgs e)
        {
            if (listBox_tab.SelectedItem != null)
            {
                button_tab_copyAdd(listBox_tab.SelectedItems.OfType<CustomEpgTabInfoView>().Select(item => item.Info).DeepClone());
            }
        }
        private void button_tab_defaultCopy_Click(object sender, RoutedEventArgs e)
        {
            button_tab_copyAdd(CommonManager.CreateDefaultTabInfo());
        }
        private void button_tab_copyAdd(List<CustomEpgTabInfo> infos)
        {
            if (infos.Count != 0)
            {
                infos.ForEach(info => info.ID = -1);
                listBox_tab.ScrollIntoViewLast(infos.Select(info => new CustomEpgTabInfoView(info, () => settings)));
            }
        }

        private void button_tab_Select_Click(object sender, RoutedEventArgs e)
        {
            button_tab_Change_Visible(listBox_tab.SelectedItems, true);
        }
        private void button_tab_Select_All_Click(object sender, RoutedEventArgs e)
        {
            button_tab_Change_Visible(listBox_tab.Items, true);
        }
        private void button_tab_None_Click(object sender, RoutedEventArgs e)
        {
            button_tab_Change_Visible(listBox_tab.SelectedItems, false);
        }
        private void button_tab_NoneAll_Click(object sender, RoutedEventArgs e)
        {
            button_tab_Change_Visible(listBox_tab.Items, false);
        }
        private void button_tab_Change_Visible(ICollection items, bool isVisible)
        {
            foreach (var item in items.OfType<CustomEpgTabInfoView>())
            {
                item.IsSelected = isVisible;
            }
        }

        private void button_Color_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ColorSetWindow(((SolidColorBrush)((Button)sender).Background).Color, this);
            if (dlg.ShowDialog() == true)
            {
                ((Button)sender).Background = new SolidColorBrush(dlg.GetColor());
            }
        }

        private void button_set_cm_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SetContextMenuWindow(this, settings.MenuSet);
            if (dlg.ShowDialog() == true)
            {
                settings.MenuSet = dlg.info;
            }
        }

        private void button_SetPicUpCustom_Click(object sender, RoutedEventArgs e)
        {
            bool backCustom = settings.PicUpTitleWork.UseCustom;
            var dlg = new SetPicUpCustomWindow(this, settings.PicUpTitleWork);
            if (dlg.ShowDialog() == true)
            {
                settings.PicUpTitleWork = dlg.GetData();
                settings.PicUpTitleWork.UseCustom = backCustom;
                checkBox_picUpCustom.DataContext = settings.PicUpTitleWork;
            }
        }

        private void button_clearSerchKeywords_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.AndKeyList.Clear();
            Settings.Instance.NotKeyList.Clear();
        }

        private void checkBox_ReplacePatternEditFontShare_Click(object sender, RoutedEventArgs e)
        {
            bool isChange = settings.ReplacePatternEditFontShare;
            string path = isChange ? "Text" : CommonUtil.NameOf(() => settings.FontReplacePatternEdit);
            comboBox_FontReplacePatternEdit.SetBinding(ComboBox.TextProperty, path);
            comboBox_FontReplacePatternEdit.SetReadOnlyWithEffect(isChange);
            comboBox_FontReplacePatternEdit.DataContext = isChange ? comboBox_fontTitle : this.DataContext;
        }
        private void checkBox_FontBoldReplacePattern_Click(object sender, RoutedEventArgs e)
        {
            var fw = settings.FontBoldReplacePattern == true ? FontWeights.Bold : FontWeights.Normal;
            textBox_ReplacePatternTitle.FontWeight = fw;
            textBox_ReplacePattern.FontWeight = fw;
        }

        private void button_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var btn = sender as Button;
            var menuCustom1 = new MenuItem { Header = "カスタム色をコンボボックスの選択色で有効化" };
            var menuCustom2 = new MenuItem { Header = "カスタム色をコンボボックスの選択色で有効化(透過度保持)" };
            var menuReset = new MenuItem { Header = "カスタム色をリセット" };
            var menuSelect = new MenuItem { Header = "コンボボックスから現在のカスタム色に近い色を選択" };

            var pnl = btn.Parent as Panel;
            var cmb = pnl == null ? null : pnl.Children.OfType<ComboBox>().FirstOrDefault(item => item.Tag as string == btn.Tag as string);
            if (cmb == null) return; //無いはずだけど保険

            menuCustom1.IsEnabled = cmb.SelectedItem != null && cmb.SelectedIndex != cmb.Items.Count - 1;
            menuCustom2.IsEnabled = menuCustom1.IsEnabled;
            menuReset.Click += (sender2, e2) => btn.Background = new SolidColorBrush(Colors.White);

            var SetColor = new Action<bool>(keepA =>
            {
                var cmbColor = (Color)cmb.SelectedValue;
                if (keepA) cmbColor.A = ((SolidColorBrush)btn.Background).Color.A;
                btn.Background = new SolidColorBrush(cmbColor);
                cmb.SelectedIndex = cmb.Items.Count - 1;
            });
            menuCustom1.Click += (sender2, e2) => SetColor(false);
            menuCustom2.Click += (sender2, e2) => SetColor(true);
            menuSelect.Click += (sender2, e2) => ColorSetWindow.SelectNearColor(cmb, ((SolidColorBrush)btn.Background).Color);
            
            ContextMenu ctxm = new ContextMenuEx();
            ctxm.Items.Add(menuCustom1);
            ctxm.Items.Add(menuCustom2);
            ctxm.Items.Add(menuReset);
            ctxm.Items.Add(menuSelect);
            ctxm.IsOpen = true;
        }

        private static ColorButtonConverter colorBtnCnv = new ColorButtonConverter();
        public static BindingExpressionBase SetBindingColorButton(Button btn, string path)
        {
            var binding = new Binding(path) { Converter = colorBtnCnv, Mode = BindingMode.TwoWay };
            return btn.SetBinding(Button.BackgroundProperty, binding);
        }
        private static ColorComboConverter colorCmbCnv = new ColorComboConverter();
        public static BindingExpressionBase SetBindingColorCombo(ComboBox cmb, string path)
        {
            var binding = new Binding(path) { Converter = colorCmbCnv, ConverterParameter = cmb };
            return cmb.SetBinding(ComboBox.SelectedItemProperty, binding);
        }
        public class ColorButtonConverter : IValueConverter
        {
            public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
            {
                return new SolidColorBrush(ColorDef.FromUInt((uint)v));
            }
            public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            {
                return ColorDef.ToUInt((v as SolidColorBrush).Color);
            }
        }
        public class ColorComboConverter : IValueConverter
        {
            public virtual object Convert(object v, Type t, object p, System.Globalization.CultureInfo c)
            {
                var items = (p as ComboBox).Items.OfType<ColorComboItem>();
                var val = v as string;
                ColorComboItem selected = items.FirstOrDefault(item => item.Name == val);
                return selected ?? items.Last();
            }
            public virtual object ConvertBack(object v, Type t, object p, System.Globalization.CultureInfo c)
            {
                return (v as ColorComboItem).Name;
            }
        }
    }

    public class ColorComboItem
    {
        public ColorComboItem(string name, Brush value) { Name = name; Value = value; }
        public string Name { get; set; }
        public Brush Value { get; set; }
        public string ToolTipText 
        {
            get 
            {
                var solid = Value as SolidColorBrush;
                return Name + (solid == null ? "" : string.Format(":#{0:X8}", ColorDef.ToUInt(solid.Color)));
            }
        }
        public override string ToString() { return Name; }
    }

    public class CustomEpgTabInfoView : SelectableItem
    {
        public CustomEpgTabInfoView(CustomEpgTabInfo info, Func<Settings> settings) { Info = info; Settings = settings; }
        private Func<Settings> Settings;
        private CustomEpgTabInfo _info;
        public CustomEpgTabInfo Info
        {
            get
            {
                _info.IsVisible = this.IsSelected;
                return _info;
            }
            set
            {
                _info = value;
                IsSelected = _info.IsVisible;
            }
        }
        public string TabName { get { return Info.TabName; } }
        public string ViewMode { get { return CommonManager.ConvertViewModeText(Info.ViewMode).Replace("モード", ""); } }
        public string Design { get { return Settings().EpgSettingList[_info.EpgSettingIndex].Name; } }
        public string SearchMode { get { return Info.SearchMode == false ? "" : Info.SearchKey.andKey == "" ? "(空白)" : Info.SearchKey.andKey; } }
        public string ContentMode { get { return CommonManager.ConvertJyanruText(Info); } }
    }
}
