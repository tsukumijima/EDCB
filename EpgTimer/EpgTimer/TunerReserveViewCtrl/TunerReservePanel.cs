using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace EpgTimer.TunerReserveViewCtrl
{
    class TunerReservePanel : ViewPanel
    {
        public override void SetBorderStyleFromSettings()
        {
            SetBorderStyle(Settings.Instance.TunerBorderLeftSize, Settings.Instance.TunerBorderTopSize, new Thickness(2, 0, 2, Settings.Instance.TunerFontSize * 0.8));
        }
        protected override Pen BorderPen(PanelItem info)
        {
            if (((ReserveViewItem)info).Data.IsWatchMode == false) return null;
            var pen = new Pen(info.BackColor, borderMax) { DashStyle = Settings.BrushCache.TunerDashStyle };
            pen.Freeze();
            return pen;
        }

        protected override void CreateDrawTextListMain(List<List<Tuple<Brush, GlyphRun>>> textDrawLists)
        {
            var ItemFontNormal = ItemFontCache.ItemFont(Settings.Instance.TunerFontName, false);
            var ItemFontTitle = ItemFontCache.ItemFont(Settings.Instance.TunerFontNameService, Settings.Instance.TunerFontBoldService);
            EpgSetting set = Settings.Instance.EpgSettingList.FirstOrDefault(s => s.ApplyReplacePatternTuner);
            var DictionaryTitle = set != null ? CommonManager.GetReplaceDictionaryTitle(set) : null;

            bool recSettingInfo = PopUpMode == true ? Settings.Instance.TunerPopupRecinfo : false;
            bool noWrap = PopUpMode == true ? false : Settings.Instance.TunerServiceNoWrap;

            double sizeTitle = Settings.Instance.TunerFontSizeService;
            double sizeMin = Math.Max(sizeTitle - 1, Math.Min(sizeTitle, Settings.Instance.TunerFontSize));
            double sizeNormal = Settings.Instance.TunerFontSize;
            double indentTitle = recSettingInfo ? 0 : sizeMin * CulcRenderWidth("0", ItemFontNormal) * (2 + sizeTitle / sizeMin);
            double indentNormal = Settings.Instance.TunerTitleIndent ? indentTitle : 0;
            Brush colorNormal = Settings.BrushCache.CustTunerTextColor;

            //録画中のものを後で描画する
            Items = Items.Cast<TunerReserveViewItem>().OrderBy(info => info.Data.IsOnRec()).ToList();

            foreach (TunerReserveViewItem info in Items)
            {
                var textDrawList = new List<Tuple<Brush, GlyphRun>>();
                textDrawLists.Add(textDrawList);
                Rect drawRect = TextRenderRect(info);
                double useHeight = 0;

                //追加情報の表示
                if (recSettingInfo == true)
                {
                    var resItem = new ReserveItem(info.Data);
                    string text = info.Status;
                    if (text != "") useHeight = sizeNormal / 5 + RenderText(textDrawList, text, ItemFontNormal, sizeNormal, drawRect, 0, 0, resItem.StatusColor);

                    text = resItem.StartTimeShort;
                    text += "\r\n" + "優先度 : " + resItem.Priority;
                    text += "\r\n" + "録画モード : " + resItem.RecMode;
                    useHeight += sizeNormal / 2 + RenderText(textDrawList, text, ItemFontNormal, sizeNormal, drawRect, 0, useHeight, info.ServiceColor);
                }
                else
                {
                    //分のみ
                    RenderText(textDrawList, info.Data.StartTime.ToString("mm"), ItemFontNormal, sizeMin, drawRect, 0, 0, info.ServiceColor);
                }

                //サービス名
                string serviceName = info.Data.StationName + "(" + CommonManager.ConvertNetworkNameText(info.Data.OriginalNetworkID) + ")";
                serviceName = CommonManager.ReplaceText(serviceName, DictionaryTitle);
                useHeight += sizeTitle / 3 + RenderText(textDrawList, serviceName, ItemFontTitle, sizeTitle, drawRect, indentTitle, useHeight, info.ServiceColor, noWrap);

                //番組名
                if (useHeight < drawRect.Height)
                {
                    string title = CommonManager.ReplaceText(info.Data.Title.TrimEnd(), DictionaryTitle);
                    if (title != "") useHeight += sizeNormal / 3 + RenderText(textDrawList, title, ItemFontNormal, sizeNormal, drawRect, indentNormal, useHeight, colorNormal);
                }

                SaveMaxRenderHeight(useHeight);
            }
        }
    }
}
