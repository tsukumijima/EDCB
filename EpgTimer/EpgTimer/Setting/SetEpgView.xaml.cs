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

        private RadioBtnSelect epgPopupRadioBtns;
        private RadioBtnSelect tunerPopupRadioBtns;
        private RadioBtnSelect tunerToolTipRadioBtns;

        public bool IsChangeEpgArcLoadSetting { get; private set; }
        public bool IsChangeRecInfoDropExcept { get; private set; }

        public SetEpgView()
        {
            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                stackPanel_epgArchivePeriod.IsEnabled = false;
            }
            else
            {
                checkBox_CacheKeepConnect.IsEnabled = false;
            }

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
            setComboColor1(CommonUtil.NameOf(() => settings.TitleColor1), comboBox_colorTitle1);
            setComboColor1(CommonUtil.NameOf(() => settings.TitleColor2), comboBox_colorTitle2);
            setComboColors(CommonUtil.NameOf(() => settings.ContentColorList), grid_EpgColors);
            setComboColors(CommonUtil.NameOf(() => settings.EpgResColorList), grid_EpgColorsReserve);
            setComboColors(CommonUtil.NameOf(() => settings.EpgEtcColors), grid_EpgTimeColors);
            setComboColors(CommonUtil.NameOf(() => settings.EpgEtcColors), grid_EpgEtcColors);
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
            SetBindingColorButton(button_colorTitle1, CommonUtil.NameOf(() => settings.TitleCustColor1));
            SetBindingColorButton(button_colorTitle2, CommonUtil.NameOf(() => settings.TitleCustColor2));
            setButtonColors(CommonUtil.NameOf(() => settings.ContentCustColorList), grid_EpgColors);
            setButtonColors(CommonUtil.NameOf(() => settings.EpgResCustColorList), grid_EpgColorsReserve);
            setButtonColors(CommonUtil.NameOf(() => settings.EpgEtcCustColors), grid_EpgTimeColors);
            setButtonColors(CommonUtil.NameOf(() => settings.EpgEtcCustColors), grid_EpgEtcColors);
            setButtonColors(CommonUtil.NameOf(() => settings.TunerServiceCustColors), grid_TunerFontColor);
            setButtonColors(CommonUtil.NameOf(() => settings.TunerServiceCustColors), grid_TunerColors);
            setButtonColors(CommonUtil.NameOf(() => settings.TunerServiceCustColors), grid_TunerEtcColors);

            //録画済み一覧画面
            setButtonColors(CommonUtil.NameOf(() => settings.RecEndCustColors), grid_RecInfoBackColors);
            setComboColors(CommonUtil.NameOf(() => settings.RecEndColors), grid_RecInfoBackColors);

            //予約一覧・共通画面
            SetBindingColorButton(btn_ListDefFontColor, CommonUtil.NameOf(() => settings.ListDefCustColor));
            setButtonColors(CommonUtil.NameOf(() => settings.RecModeFontCustColors), grid_ReserveRecModeColors);
            setButtonColors(CommonUtil.NameOf(() => settings.ResBackCustColors), grid_ReserveBackColors);
            setButtonColors(CommonUtil.NameOf(() => settings.StatCustColors), grid_StatColors);
            setComboColor1(CommonUtil.NameOf(() => settings.ListDefColor), cmb_ListDefFontColor);
            setComboColors(CommonUtil.NameOf(() => settings.RecModeFontColors), grid_ReserveRecModeColors);
            setComboColors(CommonUtil.NameOf(() => settings.ResBackColors), grid_ReserveBackColors);
            setComboColors(CommonUtil.NameOf(() => settings.StatColors), grid_StatColors);

            button_clearSerchKeywords.ToolTip = SearchKeyView.ClearButtonTooltip;
            checkBox_NotNoStyle.ToolTip = string.Format("チェック時、テーマファイル「{0}」があればそれを、無ければ既定のテーマ(Aero)を適用します。", System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location) + ".rd.xaml");
        }

        public void LoadSetting()
        {
            checkBox_FontBoldReplacePattern_Click(null, null);
            checkBox_ReplacePatternEditFontShare_Click(null, null);
            checkbox_EpgChangeBorderWatch_Click(null, null);
            checkbox_TunerChangeBorderWatch_Click(null, null);

            //番組表
            epgPopupRadioBtns = new RadioBtnSelect(panel_epgPopup, settings.EpgPopupMode);

            int epgArcHour = IniFileHandler.GetPrivateProfileInt("SET", "EpgArchivePeriodHour", 0, SettingPath.TimerSrvIniPath);
            double epgArcDay = IniFileHandler.GetPrivateProfileDouble("SET", "EpgArchivePeriodDay", 0, SettingPath.TimerSrvIniPath);
            epgArcDay = (int)(epgArcDay * 24) == epgArcHour ? epgArcDay : epgArcHour / 24d;
            textBox_epgArchivePeriod.Text = Math.Min(Math.Max(epgArcDay, 0), 14).ToString();

            listBox_tab.Items.Clear();
            listBox_tab.Items.AddItems(settings.CustomEpgTabList.Select(info => new CustomEpgTabInfoView(info)));
            listBox_tab.SelectedIndex = 0;

            //チューナー画面
            tunerPopupRadioBtns = new RadioBtnSelect(panel_tunerPopup, settings.TunerPopupMode);
            tunerToolTipRadioBtns = new RadioBtnSelect(panel_tunerTooltip, settings.TunerToolTipMode);

            //録画済み一覧画面
            textBox_RecInfoDropExcept.Text = string.Join(", ", settings.RecInfoDropExcept);

            //予約一覧・共通画面
            textBox_LaterTimeHour.Text = (settings.LaterTimeHour + 24).ToString();
            checkBox_picUpCustom.DataContext = settings.PicUpTitleWork;

            wrapPanel_StartTab.Children.Clear();
            foreach (var item in new Dictionary<CtxmCode, string> {
                        { CtxmCode.ReserveView, "予約一覧" },{ CtxmCode.TunerReserveView, "使用予定チューナー" },
                        { CtxmCode.RecInfoView, "録画済み一覧" },{ CtxmCode.EpgAutoAddView, "キーワード予約登録" },
                        { CtxmCode.ManualAutoAddView, "プログラム予約登録" },{ CtxmCode.EpgView, "番組表" } })
            {
                var rbtn = new RadioButton { Tag = item.Key, Content = item.Value, IsChecked = item.Key == settings.StartTab };
                rbtn.Checked += (sender, e) => settings.StartTab = (CtxmCode)(sender as RadioButton).Tag;
                wrapPanel_StartTab.Children.Add(rbtn);
            }
            wrapPanel_MainViewButtonsDock.Children.Clear();
            foreach (var item in new Dictionary<Dock, string> {
                        { Dock.Bottom, "下" },{ Dock.Top, "上" },{ Dock.Left, "左" },{ Dock.Right, "右" } })
            {
                var rbtn = new RadioButton { Tag = item.Key, Content = item.Value, IsChecked = item.Key == settings.MainViewButtonsDock };
                rbtn.Checked += (sender, e) => settings.MainViewButtonsDock = (Dock)(sender as RadioButton).Tag;
                wrapPanel_MainViewButtonsDock.Children.Add(rbtn);
            }
        }

        public void SaveSetting()
        {
            //番組表
            settings.EpgPopupMode = epgPopupRadioBtns.Value;

            double epgArcDay = MenuUtil.MyToNumerical(textBox_epgArchivePeriod, Convert.ToDouble, 14, 0, 0);
            IniFileHandler.WritePrivateProfileString("SET", "EpgArchivePeriodHour", (int)(epgArcDay * 24), SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString("SET", "EpgArchivePeriodDay", epgArcDay, SettingPath.TimerSrvIniPath);
            IsChangeEpgArcLoadSetting = Settings.Instance.EpgLoadArcInfo != settings.EpgLoadArcInfo;

            settings.CustomEpgTabList = listBox_tab.Items.OfType<CustomEpgTabInfoView>().Select(item => item.Info).ToList();
            settings.SetCustomEpgTabInfoID();

            //チューナー画面
            settings.TunerToolTipMode = tunerToolTipRadioBtns.Value;
            settings.TunerPopupMode = tunerPopupRadioBtns.Value;

            //録画済み一覧画面
            settings.RecInfoDropExcept = textBox_RecInfoDropExcept.Text.Split(',')
                .Where(s => string.IsNullOrWhiteSpace(s) == false).Select(s => s.Trim()).ToList();
            IsChangeRecInfoDropExcept = settings.RecInfoDropExcept.SequenceEqual(Settings.Instance.RecInfoDropExcept) == false;

            //予約一覧・共通画面
            settings.LaterTimeHour = MenuUtil.MyToNumerical(textBox_LaterTimeHour, Convert.ToInt32, 36, 24, 28) - 24;
            if (settings.UseLastSearchKey == false) settings.DefSearchKey = new EpgSearchKeyInfo();
        }

        private void button_tab_add_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new EpgDataViewSettingWindow();
            dlg.Owner = CommonUtil.GetTopWindow(this);
            if (dlg.ShowDialog() == true)
            {
                listBox_tab.ScrollIntoViewLast(new CustomEpgTabInfoView(dlg.GetSetting()));
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
                var dlg = new EpgDataViewSettingWindow(item.Info);
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
                listBox_tab.ScrollIntoViewLast(infos.Select(info => new CustomEpgTabInfoView(info)));
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
            settings.AndKeyList = new List<string>();
            settings.NotKeyList = new List<string>();
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

        private void checkbox_EpgChangeBorderWatch_Click(object sender, RoutedEventArgs e)
        {
            label_EpgReserve.Content = settings.EpgChangeBorderWatch == true ? "通常(録画)" : "通常(EPG)";
            label_EpgReserve2.Content = settings.EpgChangeBorderWatch == true ? "通常(視聴)" : "通常(プログラム)";
        }
        private void checkbox_TunerChangeBorderWatch_Click(object sender, RoutedEventArgs e)
        {
            label_TunerReserve.Content = settings.TunerChangeBorderWatch == true ? "予約枠(録画)" : "予約枠(EPG)";
            label_TunerReserve2.Content = settings.TunerChangeBorderWatch == true ? "予約枠(視聴)" : "予約枠(プログラム)";
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
            
            ContextMenu ctxm = new ContextMenu();
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
        public CustomEpgTabInfoView(CustomEpgTabInfo info1) { Info = info1; }
        private CustomEpgTabInfo info;
        public CustomEpgTabInfo Info 
        {
            get
            {
                info.IsVisible = this.IsSelected;
                return info;
            }
            set
            {
                info = value;
                IsSelected = info.IsVisible;
            } 
        }
        public string TabName { get { return Info.TabName; } }
        public string ViewMode { get { return CommonManager.ConvertViewModeText(Info.ViewMode).Replace("モード", ""); } }
        public string SearchMode { get { return Info.SearchMode == false ? "" : Info.SearchKey.andKey == "" ? "(空白)" : Info.SearchKey.andKey; } }
        public string ContentMode { get { return CommonManager.ConvertJyanruText(Info); } }
    }
}
