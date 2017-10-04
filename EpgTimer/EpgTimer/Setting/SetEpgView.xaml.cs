using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using System.Windows.Shapes;

namespace EpgTimer.Setting
{
    using BoxExchangeEdit;

    /// <summary>
    /// SetEpgView.xaml の相互作用ロジック
    /// </summary>
    public partial class SetEpgView : UserControl
    {
        private PicUpTitle picUpTitle;
        private MenuSettingData ctxmSetInfo;
        private RadioBtnSelect epgPopupRadioBtns;
        private RadioBtnSelect tunerPopupRadioBtns;
        private RadioBtnSelect tunerToolTipRadioBtns;

        public bool IsChangeEpgArcLoadSetting { get; private set; }

        public SetEpgView()
        {
            InitializeComponent();

            if (CommonManager.Instance.NWMode == true)
            {
                stackPanel_epgArchivePeriod.IsEnabled = false;
            }

            try
            {
                textBox_mouse_scroll.Text = Settings.Instance.ScrollSize.ToString();
                textBox_service_width.Text = Settings.Instance.ServiceWidth.ToString();
                textBox_minHeight.Text = Settings.Instance.MinHeight.ToString();
                textBox_dragScroll.Text = Settings.Instance.DragScroll.ToString();
                textBox_minimumHeight.Text = Settings.Instance.MinimumHeight.ToString();
                textBox_borderLeftSize.Text = Settings.Instance.EpgBorderLeftSize.ToString();
                textBox_borderTopSize.Text = Settings.Instance.EpgBorderTopSize.ToString();
                checkBox_epg_popup.IsChecked = Settings.Instance.EpgPopup;
                epgPopupRadioBtns = new RadioBtnSelect(radioButton_epg_popup_onOver, radioButton_epg_popup_onClick, radioButton_epg_popup_resOnly);
                epgPopupRadioBtns.Value = Settings.Instance.EpgPopupMode;
                textBox_epg_popup_Width.Text = Settings.Instance.EpgPopupWidth.ToString();
                checkBox_title_indent.IsChecked = Settings.Instance.EpgTitleIndent;
                checkBox_descToolTip.IsChecked = Settings.Instance.EpgToolTip;
                checkBox_toolTip_noView_only.IsChecked = Settings.Instance.EpgToolTipNoViewOnly;
                textBox_toolTipWait.Text = Settings.Instance.EpgToolTipViewWait.ToString();
                checkBox_epg_ExtInfo_table.IsChecked = Settings.Instance.EpgExtInfoTable;
                checkBox_epg_ExtInfo_popup.IsChecked = Settings.Instance.EpgExtInfoPopup;
                checkBox_epg_ExtInfo_tooltip.IsChecked = Settings.Instance.EpgExtInfoTooltip;
                checkBox_singleOpen.IsChecked = Settings.Instance.EpgInfoSingleClick;
                checkBox_scrollAuto.IsChecked = Settings.Instance.MouseScrollAuto;
                checkBox_gradation.IsChecked = Settings.Instance.EpgGradation;
                checkBox_gradationHeader.IsChecked = Settings.Instance.EpgGradationHeader;
                checkBox_openInfo.IsChecked = (Settings.Instance.EpgInfoOpenMode != 0);
                checkBox_displayNotifyChange.IsChecked = Settings.Instance.DisplayNotifyEpgChange;
                slider_reserveFillOpacity.Value = Math.Min(Math.Max(Settings.Instance.ReserveRectFillOpacity, 0), 100);
                checkBox_reserveFillWithShadow.IsChecked = Settings.Instance.ReserveRectFillWithShadow;

                int epgArcHour = IniFileHandler.GetPrivateProfileInt("SET", "EpgArchivePeriodHour", 0, SettingPath.TimerSrvIniPath);
                double epgArcDay = IniFileHandler.GetPrivateProfileDouble("SET", "EpgArchivePeriodDay", 0, SettingPath.TimerSrvIniPath);
                epgArcDay = (int)(epgArcDay * 24) == epgArcHour ? epgArcDay : epgArcHour / 24d;
                textBox_epgArchivePeriod.Text = Math.Min(Math.Max(epgArcDay, 0), 14).ToString();
                checkBox_epgNoLoadArcInfo.IsChecked = !Settings.Instance.EpgLoadArcInfo;
                checkBox_epgNoDisplayOld.IsChecked = Settings.Instance.EpgNoDisplayOld;
                textBox_epgNoDisplayOldDays.Text = Settings.Instance.EpgNoDisplayOldDays.ToString();
                checkBox_EpgChangeBorderWatch.IsChecked = Settings.Instance.EpgChangeBorderWatch;
                checkbox_EpgChangeBorderWatch_Click(null, null);
                checkBox_EpgChangeBorderOnRec.IsChecked = Settings.Instance.EpgChangeBorderOnRec;

                textBox_replacePattern.Text = Settings.Instance.EpgReplacePattern;
                textBox_replacePatternTitle.Text = Settings.Instance.EpgReplacePatternTitle;
                checkBox_replacePatternDef.IsChecked = Settings.Instance.EpgReplacePatternDef;
                checkBox_replacePatternTitleDef.IsChecked = Settings.Instance.EpgReplacePatternTitleDef;
                checkBox_ApplyReplacePatternTuner.IsChecked = Settings.Instance.ApplyReplacePatternTuner;
                checkBox_ShareEpgReplacePatternTitle.IsChecked = Settings.Instance.ShareEpgReplacePatternTitle;
                checkBox_ShareEpgReplacePatternTitle_Click(null, null);

                textBox_tuner_mouse_scroll.Text = Settings.Instance.TunerScrollSize.ToString();
                textBox_tuner_width.Text = Settings.Instance.TunerWidth.ToString();
                textBox_tuner_minHeight.Text = Settings.Instance.TunerMinHeight.ToString();
                textBox_tunerDdragScroll.Text = Settings.Instance.TunerDragScroll.ToString();
                textBox_tunerMinLineHeight.Text = Settings.Instance.TunerMinimumLine.ToString();
                checkBox_tuner_popup.IsChecked = Settings.Instance.TunerPopup;
                tunerPopupRadioBtns = new RadioBtnSelect(radioButton_tuner_popup_onOver, radioButton_tuner_popup_onClick);
                tunerPopupRadioBtns.Value = Settings.Instance.TunerPopupMode;
                checkBox_tuner_popup_recInfo.IsChecked = Settings.Instance.TunerPopupRecinfo;
                textBox_tuner_popup_Width.Text = Settings.Instance.TunerPopupWidth.ToString();
                checkBox_tuner_title_indent.IsChecked = Settings.Instance.TunerTitleIndent;
                checkBox_tunerDescToolTip.IsChecked = Settings.Instance.TunerToolTip;
                tunerToolTipRadioBtns = new RadioBtnSelect(radioButton_tunerToolTipTunerInfo, radioButton_tunerToolTipEpgInfo);
                tunerToolTipRadioBtns.Value = Settings.Instance.TunerToolTipMode;
                textBox_tunerToolTipWait.Text = Settings.Instance.TunerToolTipViewWait.ToString();
                checkBox_tunerSingleOpen.IsChecked = Settings.Instance.TunerInfoSingleClick;
                checkBox_tunerEpgInfoOpenMode.IsChecked = (Settings.Instance.TunerEpgInfoOpenMode != 0);
                checkBox_TunerChangeBorderWatch.IsChecked = Settings.Instance.TunerChangeBorderWatch;
                checkbox_TunerChangeBorderWatch_Click(null, null);
                checkBox_tuner_scrollAuto.IsChecked = Settings.Instance.TunerMouseScrollAuto;
                textBox_tunerBorderLeftSize.Text = Settings.Instance.TunerBorderLeftSize.ToString();
                textBox_tunerBorderTopSize.Text = Settings.Instance.TunerBorderTopSize.ToString();
                checkBox_tuner_service_nowrap.IsChecked = Settings.Instance.TunerServiceNoWrap;
                checkBox_tunerColorModeUse.IsChecked = Settings.Instance.TunerColorModeUse;
                comboBox_tunerFontColorService.IsEnabled = !Settings.Instance.TunerColorModeUse;
                button_tunerFontCustColorService.IsEnabled = !Settings.Instance.TunerColorModeUse;
                checkBox_tuner_display_offres.IsChecked = Settings.Instance.TunerDisplayOffReserve;

                listBox_tab.KeyDown += ViewUtil.KeyDown_Enter(button_tab_chg);
                SelectableItem.Set_CheckBox_PreviewChanged(listBox_tab);
                var bx = new BoxExchangeEditor(null, this.listBox_tab, true, true, true);
                bx.targetBoxAllowDoubleClick(bx.TargetBox, (sender, e) => button_tab_chg.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
                button_tab_del.Click += new RoutedEventHandler(bx.button_Delete_Click);
                button_tab_del_all.Click += new RoutedEventHandler(bx.button_DeleteAll_Click);
                button_tab_up.Click += new RoutedEventHandler(bx.button_Up_Click);
                button_tab_down.Click += new RoutedEventHandler(bx.button_Down_Click);
                button_tab_top.Click += new RoutedEventHandler(bx.button_Top_Click);
                button_tab_bottom.Click += new RoutedEventHandler(bx.button_Bottom_Click);

                radioButton_1_def.IsChecked = (Settings.Instance.UseCustomEpgView == false);
                radioButton_1_cust.IsChecked = (Settings.Instance.UseCustomEpgView != false);

                listBox_tab.Items.AddItems(Settings.Instance.CustomEpgTabList.Select(info => new CustomEpgTabInfoView(info.Clone())));
                if (listBox_tab.Items.Count > 0) listBox_tab.SelectedIndex = 0;
                checkBox_EpgNameTabEnabled.IsChecked = Settings.Instance.EpgNameTabEnabled;
                checkBox_EpgViewModeTabEnabled.IsChecked = Settings.Instance.EpgViewModeTabEnabled;
                checkBox_EpgTabMoveCheckEnabled.IsChecked = Settings.Instance.EpgTabMoveCheckEnabled;

                XmlLanguage FLanguage = XmlLanguage.GetLanguage("ja-JP");
                List<string> fontList = Fonts.SystemFontFamilies.Select(f => f.FamilyNames.ContainsKey(FLanguage) == true ? f.FamilyNames[FLanguage] : f.Source).OrderBy(s => s).ToList();

                var setCmboFont = new Action<string, ComboBox>((name, cmb) =>
                {
                    cmb.ItemsSource = fontList;
                    cmb.Text = name;
                });
                setCmboFont(Settings.Instance.FontNameTitle, comboBox_fontTitle);
                setCmboFont(Settings.Instance.FontName, comboBox_font);
                setCmboFont(Settings.Instance.TunerFontNameService, comboBox_fontTunerService);
                setCmboFont(Settings.Instance.TunerFontName, comboBox_fontTuner);

                textBox_fontSize.Text = Settings.Instance.FontSize.ToString();
                textBox_fontSizeTitle.Text = Settings.Instance.FontSizeTitle.ToString();
                checkBox_fontBoldTitle.IsChecked = Settings.Instance.FontBoldTitle;
                textBox_fontTunerSize.Text = Settings.Instance.TunerFontSize.ToString();
                textBox_fontTunerSizeService.Text = Settings.Instance.TunerFontSizeService.ToString();
                checkBox_fontTunerBoldService.IsChecked = Settings.Instance.TunerFontBoldService;

                var colorReference = typeof(Brushes).GetProperties().ToDictionary(p => p.Name, p => new ColorComboItem(p.Name, (Brush)p.GetValue(null, null)));
                colorReference.Add("カスタム", new ColorComboItem("カスタム", this.Resources["HatchBrush"] as VisualBrush));

                var setComboColor1 = new Action<string, ComboBox>((name, cmb) =>
                {
                    cmb.ItemsSource = colorReference.Values;
                    cmb.SelectedItem = colorReference.ContainsKey(name) == true ? colorReference[name] : colorReference["カスタム"];
                });
                var setComboColors = new Action<List<string>, Panel>((list, pnl) =>
                {
                    foreach (var cmb in pnl.Children.OfType<ComboBox>())
                    {
                        int idx = int.Parse((string)cmb.Tag);
                        setComboColor1(list[idx], cmb);
                    }
                });

                //番組表のフォント色はSettingsが個別のため個別処理。
                //これをまとめて出来るようにSettingsを変えると以前の設定が消える。
                setComboColor1(Settings.Instance.TitleColor1, comboBox_colorTitle1);
                setComboColor1(Settings.Instance.TitleColor2, comboBox_colorTitle2);
                setComboColors(Settings.Instance.ContentColorList, grid_EpgColors);
                setComboColors(Settings.Instance.EpgResColorList, grid_EpgColorsReserve);
                setComboColors(Settings.Instance.EpgEtcColors, grid_EpgTimeColors);
                setComboColors(Settings.Instance.EpgEtcColors, grid_EpgEtcColors);
                setComboColors(Settings.Instance.TunerServiceColors, grid_TunerFontColor);
                setComboColors(Settings.Instance.TunerServiceColors, grid_TunerColors);
                setComboColors(Settings.Instance.TunerServiceColors, grid_TunerEtcColors);

                var setButtonColor1 = new Action<uint, Button>((clr, btn) => btn.Background = new SolidColorBrush(ColorDef.FromUInt(clr)));
                var setButtonColors = new Action<List<uint>, Panel>((list, pnl) =>
                {
                    foreach (var btn in pnl.Children.OfType<Button>())
                    {
                        int idx = int.Parse((string)btn.Tag);
                        setButtonColor1(list[idx], btn);
                    }
                });
                setButtonColor1(Settings.Instance.TitleCustColor1, button_colorTitle1);
                setButtonColor1(Settings.Instance.TitleCustColor2, button_colorTitle2);
                setButtonColors(Settings.Instance.ContentCustColorList, grid_EpgColors);
                setButtonColors(Settings.Instance.EpgResCustColorList, grid_EpgColorsReserve);
                setButtonColors(Settings.Instance.EpgEtcCustColors, grid_EpgTimeColors);
                setButtonColors(Settings.Instance.EpgEtcCustColors, grid_EpgEtcColors);
                setButtonColors(Settings.Instance.TunerServiceCustColors, grid_TunerFontColor);
                setButtonColors(Settings.Instance.TunerServiceCustColors, grid_TunerColors);
                setButtonColors(Settings.Instance.TunerServiceCustColors, grid_TunerEtcColors);

                //録画済み一覧画面
                textBox_dropErrIgnore.Text = Settings.Instance.RecInfoDropErrIgnore.ToString();
                textBox_dropWrnIgnore.Text = Settings.Instance.RecInfoDropWrnIgnore.ToString();
                textBox_scrambleIgnore.Text = Settings.Instance.RecInfoScrambleIgnore.ToString();
                checkBox_playDClick.IsChecked = Settings.Instance.PlayDClick;
                checkBox_recToolTipEpgInfo.IsChecked = (Settings.Instance.RecInfoToolTipMode != 0);
                checkBox_recinfo_errCritical.IsChecked = Settings.Instance.RecinfoErrCriticalDrops;
                textBox_RecInfoDropExcept.Text = string.Join(", ", Settings.Instance.RecInfoDropExcept);
                button_RecInfoDropExceptDefault.Click += (sender, e) => textBox_RecInfoDropExcept.Text = string.Join(", ", Settings.RecInfoDropExceptDefault);
                checkBox_recNoYear.IsChecked = Settings.Instance.RecInfoNoYear;
                checkBox_recNoSecond.IsChecked = Settings.Instance.RecInfoNoSecond;
                checkBox_recNoDurSecond.IsChecked = Settings.Instance.RecInfoNoDurSecond;
                checkBox_ChacheOn.IsChecked = Settings.Instance.RecInfoExtraDataCache;
                checkBox_CacheOptimize.IsChecked = Settings.Instance.RecInfoExtraDataCacheOptimize;
                checkBox_CacheKeepConnect.IsChecked = Settings.Instance.RecInfoExtraDataCacheKeepConnect;
                if (CommonManager.Instance.NWMode == false)
                {
                    checkBox_CacheKeepConnect.IsEnabled = false;//{Binding}を破棄しているので注意
                }
                setComboColors(Settings.Instance.RecEndColors, grid_RecInfoBackColors);
                setButtonColors(Settings.Instance.RecEndCustColors, grid_RecInfoBackColors);

                //予約一覧・共通画面
                this.ctxmSetInfo = Settings.Instance.MenuSet.Clone();
                checkBox_displayAutoAddMissing.IsChecked = Settings.Instance.DisplayReserveAutoAddMissing;
                checkBox_displayMultiple.IsChecked = Settings.Instance.DisplayReserveMultiple;
                checkBox_resNoYear.IsChecked = Settings.Instance.ResInfoNoYear;
                checkBox_resNoSecond.IsChecked = Settings.Instance.ResInfoNoSecond;
                checkBox_resNoDurSecond.IsChecked = Settings.Instance.ResInfoNoDurSecond;
                checkBox_TrimSortTitle.IsChecked = Settings.Instance.TrimSortTitle;
                picUpTitle = Settings.Instance.PicUpTitleWork.Clone();
                checkBox_picUpCustom.IsChecked = picUpTitle.UseCustom;

                setComboColor1(Settings.Instance.ListDefColor, cmb_ListDefFontColor);
                setComboColors(Settings.Instance.RecModeFontColors, grid_ReserveRecModeColors);
                setComboColors(Settings.Instance.ResBackColors, grid_ReserveBackColors);
                setComboColors(Settings.Instance.StatColors, grid_StatColors);

                setButtonColor1(Settings.Instance.ListDefCustColor, btn_ListDefFontColor);
                setButtonColors(Settings.Instance.RecModeFontCustColors, grid_ReserveRecModeColors);
                setButtonColors(Settings.Instance.ResBackCustColors, grid_ReserveBackColors);
                setButtonColors(Settings.Instance.StatCustColors, grid_StatColors);

                textBox_DisplayJumpTime.Text = Settings.Instance.DisplayNotifyJumpTime.ToString();
                checkBox_LaterTimeUse.IsChecked = Settings.Instance.LaterTimeUse;
                textBox_LaterTimeHour.Text = (Settings.Instance.LaterTimeHour + 24).ToString();
                checkBox_keepReserveWindow.IsChecked = Settings.Instance.KeepReserveWindow;
                checkBox_useLastSearchKey.IsChecked = Settings.Instance.UseLastSearchKey;
                checkBox_saveSearchKeyword.IsChecked = Settings.Instance.SaveSearchKeyword;
                button_clearSerchKeywords.ToolTip = SearchKeyView.ClearButtonTooltip;
                checkBox_displayPresetOnSearch.IsChecked = Settings.Instance.DisplayPresetOnSearch;
                checkBox_toolTips.IsChecked = !Settings.Instance.NoToolTip;
                textBox_ToolTipsWidth.Text = Settings.Instance.ToolTipWidth.ToString();
                checkBox_reserveToolTipEpgInfo.IsChecked = (Settings.Instance.ReserveToolTipMode != 0);
                checkBox_reserveEpgInfoOpenMode.IsChecked = (Settings.Instance.ReserveEpgInfoOpenMode != 0);
                checkBox_searchEpgInfoOpenMode.IsChecked = (Settings.Instance.SearchEpgInfoOpenMode != 0);
                checkBox_NotNoStyle.ToolTip = string.Format("チェック時、テーマファイル「{0}」があればそれを、無ければ既定のテーマ(Aero)を適用します。", System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location) + ".rd.xaml");
                checkBox_NotNoStyle.IsChecked = Settings.Instance.NoStyle == 0;
                checkBox_displayStatus.IsChecked = Settings.Instance.DisplayStatus;
                checkBox_displayStatusNotify.IsChecked = Settings.Instance.DisplayStatusNotify;
                checkBox_IsVisibleReserveView.IsChecked = Settings.Instance.IsVisibleReserveView;
                checkBox_IsVisibleRecInfoView.IsChecked = Settings.Instance.IsVisibleRecInfoView;
                checkBox_IsVisibleAutoAddView.IsChecked = Settings.Instance.IsVisibleAutoAddView;
                checkBox_IsVisibleAutoAddViewMoveOnly.IsChecked = Settings.Instance.IsVisibleAutoAddViewMoveOnly;

                foreach (var item in new Dictionary<object, string> {
                            { CtxmCode.ReserveView, "予約一覧" },{ CtxmCode.TunerReserveView, "使用予定チューナー" },
                            { CtxmCode.RecInfoView, "録画済み一覧" },{ CtxmCode.EpgAutoAddView, "キーワード予約登録" },
                            { CtxmCode.ManualAutoAddView, "プログラム予約登録" },{ CtxmCode.EpgView, "番組表" } })
                {
                    wrapPanel_StartTab.Children.Add(new RadioButton { Tag = item.Key, Content = item.Value });
                }
                var rbtn = wrapPanel_StartTab.Children.OfType<RadioButton>()
                    .FirstOrDefault(item => item.Tag as CtxmCode? == Settings.Instance.StartTab);
                if (rbtn != null) rbtn.IsChecked = true;

                foreach (var item in new Dictionary<object, string> {
                            { Dock.Bottom, "下" },{ Dock.Top, "上" },{ Dock.Left, "左" },{ Dock.Right, "右" } })
                {
                    wrapPanel_MainViewButtonsDock.Children.Add(new RadioButton { Tag = item.Key, Content = item.Value });
                }
                rbtn = wrapPanel_MainViewButtonsDock.Children.OfType<RadioButton>()
                    .FirstOrDefault(item => item.Tag as Dock? == Settings.Instance.MainViewButtonsDock);
                if (rbtn != null) rbtn.IsChecked = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public void SaveSetting()
        {
            try
            {
                Settings.Instance.ScrollSize = MenuUtil.MyToNumerical(textBox_mouse_scroll, Convert.ToDouble, 240);
                Settings.Instance.ServiceWidth = MenuUtil.MyToNumerical(textBox_service_width, Convert.ToDouble, double.MaxValue, 16, 150);//小さいと描画で落ちる
                Settings.Instance.MinHeight = MenuUtil.MyToNumerical(textBox_minHeight, Convert.ToDouble, double.MaxValue, 0.1, 2);
                Settings.Instance.MinimumHeight = MenuUtil.MyToNumerical(textBox_minimumHeight, Convert.ToDouble, double.MaxValue, 0, 0);
                Settings.Instance.EpgBorderLeftSize = Convert.ToDouble(textBox_borderLeftSize.Text);
                Settings.Instance.EpgBorderTopSize = Convert.ToDouble(textBox_borderTopSize.Text);
                Settings.Instance.DragScroll = MenuUtil.MyToNumerical(textBox_dragScroll, Convert.ToDouble, 1.5);
                Settings.Instance.EpgTitleIndent = (checkBox_title_indent.IsChecked == true);
                Settings.Instance.EpgToolTip = (checkBox_descToolTip.IsChecked == true);
                Settings.Instance.EpgToolTipNoViewOnly = (checkBox_toolTip_noView_only.IsChecked == true);
                Settings.Instance.EpgToolTipViewWait = MenuUtil.MyToNumerical(textBox_toolTipWait, Convert.ToInt32, Int32.MaxValue, Int32.MinValue, 1500);
                Settings.Instance.EpgPopup = (checkBox_epg_popup.IsChecked == true);
                Settings.Instance.EpgPopupMode = epgPopupRadioBtns.Value;
                Settings.Instance.EpgPopupWidth = MenuUtil.MyToNumerical(textBox_epg_popup_Width, Convert.ToDouble, double.MaxValue, 0, 1);
                Settings.Instance.EpgExtInfoTable = (checkBox_epg_ExtInfo_table.IsChecked == true);
                Settings.Instance.EpgExtInfoPopup = (checkBox_epg_ExtInfo_popup.IsChecked == true);
                Settings.Instance.EpgExtInfoTooltip = (checkBox_epg_ExtInfo_tooltip.IsChecked == true);
                Settings.Instance.EpgGradation = (checkBox_gradation.IsChecked == true);
                Settings.Instance.EpgGradationHeader = (checkBox_gradationHeader.IsChecked == true);
                Settings.Instance.EpgInfoSingleClick = (checkBox_singleOpen.IsChecked == true);
                Settings.Instance.EpgInfoOpenMode = (checkBox_openInfo.IsChecked == true ? 1 : 0);
                Settings.Instance.MouseScrollAuto = (checkBox_scrollAuto.IsChecked == true);
                Settings.Instance.DisplayNotifyEpgChange = (checkBox_displayNotifyChange.IsChecked == true);
                Settings.Instance.ReserveRectFillOpacity = (int)Math.Round(slider_reserveFillOpacity.Value);
                Settings.Instance.ReserveRectFillWithShadow = checkBox_reserveFillWithShadow.IsChecked == true;

                double epgArcDay = MenuUtil.MyToNumerical(textBox_epgArchivePeriod, Convert.ToDouble, 14, 0, 0);
                IniFileHandler.WritePrivateProfileString("SET", "EpgArchivePeriodHour", ((int)(epgArcDay * 24)).ToString(), SettingPath.TimerSrvIniPath);
                IniFileHandler.WritePrivateProfileString("SET", "EpgArchivePeriodDay", epgArcDay.ToString(), SettingPath.TimerSrvIniPath);
                IsChangeEpgArcLoadSetting = Settings.Instance.EpgLoadArcInfo != (checkBox_epgNoLoadArcInfo.IsChecked == false);
                Settings.Instance.EpgLoadArcInfo = (checkBox_epgNoLoadArcInfo.IsChecked == false);
                Settings.Instance.EpgNoDisplayOld = (checkBox_epgNoDisplayOld.IsChecked == true);
                Settings.Instance.EpgNoDisplayOldDays = MenuUtil.MyToNumerical(textBox_epgNoDisplayOldDays, Convert.ToDouble, double.MaxValue, double.MinValue, 1);
                Settings.Instance.EpgChangeBorderWatch = checkBox_EpgChangeBorderWatch.IsChecked == true;
                Settings.Instance.EpgChangeBorderOnRec = checkBox_EpgChangeBorderOnRec.IsChecked == true;

                Settings.Instance.EpgReplacePattern = textBox_replacePattern.Text;
                Settings.Instance.EpgReplacePatternTitle = textBox_replacePatternTitle.Text;
                Settings.Instance.EpgReplacePatternDef = checkBox_replacePatternDef.IsChecked == true;
                Settings.Instance.EpgReplacePatternTitleDef = checkBox_replacePatternTitleDef.IsChecked == true;
                Settings.Instance.ApplyReplacePatternTuner = checkBox_ApplyReplacePatternTuner.IsChecked == true;
                Settings.Instance.ShareEpgReplacePatternTitle = checkBox_ShareEpgReplacePatternTitle.IsChecked == true;

                Settings.Instance.TunerScrollSize = MenuUtil.MyToNumerical(textBox_tuner_mouse_scroll, Convert.ToDouble, 240);
                Settings.Instance.TunerWidth = MenuUtil.MyToNumerical(textBox_tuner_width, Convert.ToDouble, double.MaxValue, 16, 150);//小さいと描画で落ちる
                Settings.Instance.TunerMinHeight = MenuUtil.MyToNumerical(textBox_tuner_minHeight, Convert.ToDouble, double.MaxValue, 0.1, 2);
                Settings.Instance.TunerMinimumLine = MenuUtil.MyToNumerical(textBox_tunerMinLineHeight, Convert.ToDouble, double.MaxValue, 0, 0);
                Settings.Instance.TunerDragScroll = MenuUtil.MyToNumerical(textBox_tunerDdragScroll, Convert.ToDouble, 1.5);
                Settings.Instance.TunerMouseScrollAuto = (checkBox_tuner_scrollAuto.IsChecked == true);
                Settings.Instance.TunerBorderLeftSize = Convert.ToDouble(textBox_tunerBorderLeftSize.Text);
                Settings.Instance.TunerBorderTopSize = Convert.ToDouble(textBox_tunerBorderTopSize.Text);
                Settings.Instance.TunerServiceNoWrap = (checkBox_tuner_service_nowrap.IsChecked == true);
                Settings.Instance.TunerTitleIndent = (checkBox_tuner_title_indent.IsChecked == true);
                Settings.Instance.TunerToolTip = (checkBox_tunerDescToolTip.IsChecked == true);
                Settings.Instance.TunerToolTipMode = tunerToolTipRadioBtns.Value;
                Settings.Instance.TunerToolTipViewWait = MenuUtil.MyToNumerical(textBox_tunerToolTipWait, Convert.ToInt32, Int32.MaxValue, Int32.MinValue, 1500);
                Settings.Instance.TunerPopup = (checkBox_tuner_popup.IsChecked == true);
                Settings.Instance.TunerPopupMode = tunerPopupRadioBtns.Value;
                Settings.Instance.TunerPopupRecinfo = (checkBox_tuner_popup_recInfo.IsChecked == true);
                Settings.Instance.TunerPopupWidth = MenuUtil.MyToNumerical(textBox_tuner_popup_Width, Convert.ToDouble, double.MaxValue, 0, 1);
                Settings.Instance.TunerInfoSingleClick = (checkBox_tunerSingleOpen.IsChecked == true);
                Settings.Instance.TunerEpgInfoOpenMode = (checkBox_tunerEpgInfoOpenMode.IsChecked == true ? 1 : 0);
                Settings.Instance.TunerChangeBorderWatch = checkBox_TunerChangeBorderWatch.IsChecked == true;
                Settings.Instance.TunerColorModeUse = (checkBox_tunerColorModeUse.IsChecked == true);
                Settings.Instance.TunerDisplayOffReserve = (checkBox_tuner_display_offres.IsChecked == true);

                Settings.Instance.FontName = comboBox_font.Text;
                Settings.Instance.FontSize = MenuUtil.MyToNumerical(textBox_fontSize, Convert.ToDouble, 72, 1, 12);
                Settings.Instance.FontNameTitle = comboBox_fontTitle.Text;
                Settings.Instance.FontSizeTitle = MenuUtil.MyToNumerical(textBox_fontSizeTitle, Convert.ToDouble, 72, 1, 12);
                Settings.Instance.FontBoldTitle = (checkBox_fontBoldTitle.IsChecked == true);

                Settings.Instance.TunerFontName = comboBox_fontTuner.Text;
                Settings.Instance.TunerFontSize = MenuUtil.MyToNumerical(textBox_fontTunerSize, Convert.ToDouble, 72, 1, 12);
                Settings.Instance.TunerFontNameService = comboBox_fontTunerService.Text;
                Settings.Instance.TunerFontSizeService = MenuUtil.MyToNumerical(textBox_fontTunerSizeService, Convert.ToDouble, 72, 1, 12);
                Settings.Instance.TunerFontBoldService = (checkBox_fontTunerBoldService.IsChecked == true);

                Settings.Instance.UseCustomEpgView = (radioButton_1_cust.IsChecked == true);
                Settings.Instance.CustomEpgTabList = listBox_tab.Items.OfType<CustomEpgTabInfoView>().Select(item => item.Info).ToList();
                Settings.SetCustomEpgTabInfoID();
                Settings.Instance.EpgNameTabEnabled = checkBox_EpgNameTabEnabled.IsChecked == true;
                Settings.Instance.EpgViewModeTabEnabled = checkBox_EpgViewModeTabEnabled.IsChecked == true;
                Settings.Instance.EpgTabMoveCheckEnabled = checkBox_EpgTabMoveCheckEnabled.IsChecked == true;

                var getComboColor1 = new Func<ComboBox, string>(cmb => ((ColorComboItem)(cmb.SelectedItem)).Name);
                var getComboColors = new Action<List<string>, Panel>((list, pnl) =>
                {
                    foreach (var cmb in pnl.Children.OfType<ComboBox>())
                    {
                        int idx = int.Parse((string)cmb.Tag);
                        list[idx] = getComboColor1(cmb);
                    }
                });
                Settings.Instance.TitleColor1 = getComboColor1(comboBox_colorTitle1);
                Settings.Instance.TitleColor2 = getComboColor1(comboBox_colorTitle2);
                getComboColors(Settings.Instance.ContentColorList, grid_EpgColors);
                getComboColors(Settings.Instance.EpgResColorList, grid_EpgColorsReserve);
                getComboColors(Settings.Instance.EpgEtcColors, grid_EpgTimeColors);
                getComboColors(Settings.Instance.EpgEtcColors, grid_EpgEtcColors);
                getComboColors(Settings.Instance.TunerServiceColors, grid_TunerFontColor);
                getComboColors(Settings.Instance.TunerServiceColors, grid_TunerColors);
                getComboColors(Settings.Instance.TunerServiceColors, grid_TunerEtcColors);

                var getButtonColor1 = new Func<Button, uint>((btn) => ColorDef.ToUInt((btn.Background as SolidColorBrush).Color));
                var getButtonColors = new Action<List<uint>, Panel>((list, pnl) =>
                {
                    foreach (var btm in pnl.Children.OfType<Button>())
                    {
                        int idx = int.Parse((string)btm.Tag);
                        list[idx] = getButtonColor1(btm);
                    }
                });

                Settings.Instance.TitleCustColor1 = getButtonColor1(button_colorTitle1);
                Settings.Instance.TitleCustColor2 = getButtonColor1(button_colorTitle2);
                getButtonColors(Settings.Instance.ContentCustColorList, grid_EpgColors);
                getButtonColors(Settings.Instance.EpgResCustColorList, grid_EpgColorsReserve);
                getButtonColors(Settings.Instance.EpgEtcCustColors, grid_EpgTimeColors);
                getButtonColors(Settings.Instance.EpgEtcCustColors, grid_EpgEtcColors);
                getButtonColors(Settings.Instance.TunerServiceCustColors, grid_TunerFontColor);
                getButtonColors(Settings.Instance.TunerServiceCustColors, grid_TunerColors);
                getButtonColors(Settings.Instance.TunerServiceCustColors, grid_TunerEtcColors);

                //録画済み一覧画面
                Settings.Instance.PlayDClick = (checkBox_playDClick.IsChecked == true);
                Settings.Instance.RecInfoToolTipMode = (checkBox_recToolTipEpgInfo.IsChecked == true ? 1 : 0);
                Settings.Instance.RecInfoDropErrIgnore = MenuUtil.MyToNumerical(textBox_dropErrIgnore, Convert.ToInt64, Settings.Instance.RecInfoDropErrIgnore);
                Settings.Instance.RecInfoDropWrnIgnore = MenuUtil.MyToNumerical(textBox_dropWrnIgnore, Convert.ToInt64, Settings.Instance.RecInfoDropWrnIgnore);
                Settings.Instance.RecInfoScrambleIgnore = MenuUtil.MyToNumerical(textBox_scrambleIgnore, Convert.ToInt64, Settings.Instance.RecInfoScrambleIgnore);
                Settings.Instance.RecinfoErrCriticalDrops = (checkBox_recinfo_errCritical.IsChecked == true);
                List<string> pids = textBox_RecInfoDropExcept.Text.Split(',')
                    .Where(s => string.IsNullOrWhiteSpace(s) == false).Select(s => s.Trim()).ToList();
                if (pids.SequenceEqual(Settings.Instance.RecInfoDropExcept) == false)
                {
                    Settings.Instance.RecInfoDropExcept = pids;
                    CommonManager.Instance.DB.ResetRecFileErrInfo();
                }
                Settings.Instance.RecInfoNoYear = (checkBox_recNoYear.IsChecked == true);
                Settings.Instance.RecInfoNoSecond = (checkBox_recNoSecond.IsChecked == true);
                Settings.Instance.RecInfoNoDurSecond = (checkBox_recNoDurSecond.IsChecked == true);
                getComboColors(Settings.Instance.RecEndColors, grid_RecInfoBackColors);
                getButtonColors(Settings.Instance.RecEndCustColors, grid_RecInfoBackColors);
                Settings.Instance.RecInfoExtraDataCache = (checkBox_ChacheOn.IsChecked == true);
                Settings.Instance.RecInfoExtraDataCacheOptimize = (checkBox_CacheOptimize.IsChecked == true);
                Settings.Instance.RecInfoExtraDataCacheKeepConnect = (checkBox_CacheKeepConnect.IsChecked == true);

                //予約一覧画面
                Settings.Instance.MenuSet = this.ctxmSetInfo.Clone();
                Settings.Instance.DisplayReserveAutoAddMissing = (checkBox_displayAutoAddMissing.IsChecked != false);
                Settings.Instance.DisplayReserveMultiple = (checkBox_displayMultiple.IsChecked != false);
                Settings.Instance.ResInfoNoYear = (checkBox_resNoYear.IsChecked == true);
                Settings.Instance.ResInfoNoSecond = (checkBox_resNoSecond.IsChecked == true);
                Settings.Instance.ResInfoNoDurSecond = (checkBox_resNoDurSecond.IsChecked == true);
                Settings.Instance.TrimSortTitle = (checkBox_TrimSortTitle.IsChecked == true);
                picUpTitle.UseCustom = checkBox_picUpCustom.IsChecked == true;
                Settings.Instance.PicUpTitleWork = picUpTitle.Clone();

                Settings.Instance.ListDefColor = getComboColor1(cmb_ListDefFontColor);
                getComboColors(Settings.Instance.RecModeFontColors, grid_ReserveRecModeColors);
                getComboColors(Settings.Instance.ResBackColors, grid_ReserveBackColors);
                getComboColors(Settings.Instance.StatColors, grid_StatColors);

                Settings.Instance.ListDefCustColor = getButtonColor1(btn_ListDefFontColor);
                getButtonColors(Settings.Instance.RecModeFontCustColors, grid_ReserveRecModeColors);
                getButtonColors(Settings.Instance.ResBackCustColors, grid_ReserveBackColors);
                getButtonColors(Settings.Instance.StatCustColors, grid_StatColors);

                Settings.Instance.DisplayNotifyJumpTime = MenuUtil.MyToNumerical(textBox_DisplayJumpTime, Convert.ToDouble, Double.MaxValue, 0, 3);
                Settings.Instance.LaterTimeUse = (checkBox_LaterTimeUse.IsChecked == true);
                Settings.Instance.LaterTimeHour = MenuUtil.MyToNumerical(textBox_LaterTimeHour, Convert.ToInt32, 36, 24, 28) - 24;
                Settings.Instance.KeepReserveWindow = (bool)checkBox_keepReserveWindow.IsChecked;
                Settings.Instance.UseLastSearchKey = (bool)checkBox_useLastSearchKey.IsChecked;
                if (Settings.Instance.UseLastSearchKey == false) Settings.Instance.DefSearchKey = new EpgSearchKeyInfo();
                Settings.Instance.SaveSearchKeyword = checkBox_saveSearchKeyword.IsChecked != false;
                Settings.Instance.DisplayPresetOnSearch = (checkBox_displayPresetOnSearch.IsChecked == true);
                Settings.Instance.NoStyle = (checkBox_NotNoStyle.IsChecked == true ? 0 : 1);
                Settings.Instance.NoToolTip = checkBox_toolTips.IsChecked == false;
                Settings.Instance.ToolTipWidth = MenuUtil.MyToNumerical(textBox_ToolTipsWidth, Convert.ToDouble, Double.MaxValue, 16, 400);
                Settings.Instance.ReserveToolTipMode = (checkBox_reserveToolTipEpgInfo.IsChecked == true ? 1 : 0);
                Settings.Instance.ReserveEpgInfoOpenMode = (checkBox_reserveEpgInfoOpenMode.IsChecked == true ? 1 : 0);
                Settings.Instance.SearchEpgInfoOpenMode = (checkBox_searchEpgInfoOpenMode.IsChecked == true ? 1 : 0);
                Settings.Instance.DisplayStatus = (checkBox_displayStatus.IsChecked == true);
                Settings.Instance.DisplayStatusNotify = (checkBox_displayStatusNotify.IsChecked == true);
                Settings.Instance.IsVisibleReserveView = (checkBox_IsVisibleReserveView.IsChecked == true);
                Settings.Instance.IsVisibleRecInfoView = (checkBox_IsVisibleRecInfoView.IsChecked == true);
                Settings.Instance.IsVisibleAutoAddView = (checkBox_IsVisibleAutoAddView.IsChecked == true);
                Settings.Instance.IsVisibleAutoAddViewMoveOnly = (checkBox_IsVisibleAutoAddViewMoveOnly.IsChecked == true);

                CtxmCode? code = wrapPanel_StartTab.Children.OfType<RadioButton>()
                    .Where(btn => btn.IsChecked == true).Select(btn => btn.Tag as CtxmCode?).FirstOrDefault();
                if (code != null) Settings.Instance.StartTab = (CtxmCode)code;

                Dock? dock = wrapPanel_MainViewButtonsDock.Children.OfType<RadioButton>()
                    .Where(btn => btn.IsChecked == true).Select(btn => btn.Tag as Dock?).FirstOrDefault();
                if (dock != null) Settings.Instance.MainViewButtonsDock = (Dock)dock;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
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
                if (listBox_tab.Items.Count != 0)
                {
                    listBox_tab.SelectedIndex = 0;
                }
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
                button_tab_copyAdd(listBox_tab.SelectedItems.OfType<CustomEpgTabInfoView>().Select(item => item.Info).Clone());
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

        private void checkBox_tunerColorModeUse_Click(object sender, RoutedEventArgs e)
        {
            comboBox_tunerFontColorService.IsEnabled = (checkBox_tunerColorModeUse.IsChecked == false);
            button_tunerFontCustColorService.IsEnabled = (checkBox_tunerColorModeUse.IsChecked == false);
        }

        private void button_set_cm_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SetContextMenuWindow(this, ctxmSetInfo);
            if (dlg.ShowDialog() == true)
            {
                this.ctxmSetInfo = dlg.info.Clone();
            }
        }

        private void button_SetPicUpCustom_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SetPicUpCustomWindow(this, picUpTitle);
            if (dlg.ShowDialog() == true)
            {
                picUpTitle = dlg.GetData();
            }
        }

        private void button_clearSerchKeywords_Click(object sender, RoutedEventArgs e)
        {
            Settings.Instance.AndKeyList = new List<string>();
            Settings.Instance.NotKeyList = new List<string>();
        }

        private void checkbox_EpgChangeBorderWatch_Click(object sender, RoutedEventArgs e)
        {
            label_EpgReserve.Content = checkBox_EpgChangeBorderWatch.IsChecked == true ? "通常(録画)" : "通常(EPG)";
            label_EpgReserve2.Content = checkBox_EpgChangeBorderWatch.IsChecked == true ? "通常(視聴)" : "通常(プログラム)";
        }
        private void checkbox_TunerChangeBorderWatch_Click(object sender, RoutedEventArgs e)
        {
            label_TunerReserve.Content = checkBox_TunerChangeBorderWatch.IsChecked == true ? "予約枠(録画)" : "予約枠(EPG)";
            label_TunerReserve2.Content = checkBox_TunerChangeBorderWatch.IsChecked == true ? "予約枠(視聴)" : "予約枠(プログラム)";
        }

        private void checkBox_ShareEpgReplacePatternTitle_Click(object sender, RoutedEventArgs e)
        {
            checkBox_replacePatternDef.IsEnabled = !(bool)checkBox_ShareEpgReplacePatternTitle.IsChecked;
            textBox_replacePattern.SetReadOnlyWithEffect(checkBox_ShareEpgReplacePatternTitle.IsChecked == true);
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
                var bgColor = (SolidColorBrush)btn.Background;
                if (keepA) cmbColor.A = bgColor.Color.A;
                bgColor.Color = cmbColor;
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
