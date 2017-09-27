using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer.TunerReserveViewCtrl
{
    class TunerReservePanel : ViewPanel
    {
        public override void SetBorderStyleFromSettings()
        {
            SetBorderStyle(Settings.Instance.TunerBorderLeftSize, Settings.Instance.TunerBorderTopSize, new Thickness(2, 0, 4, Settings.Instance.TunerFontSize / 1.7));
        }

        protected override void CreateDrawTextListMain(List<List<Tuple<Brush, GlyphRun>>> textDrawLists)
        {
            var ItemFontNormal = ItemFontCache.ItemFont(Settings.Instance.TunerFontName, false);
            var ItemFontTitle = ItemFontCache.ItemFont(Settings.Instance.TunerFontNameService, Settings.Instance.TunerFontBoldService);

            double sizeMin = Settings.Instance.TunerFontSize;
            double sizeTitle = Settings.Instance.TunerFontSizeService;
            double sizeNormal = Settings.Instance.TunerFontSize;
            double indentTitle = ExtInfoMode == true ? 0 : sizeMin * 1.7;
            double indentNormal = Settings.Instance.TunerTitleIndent ? indentTitle : 0;
            Brush colorNormal = CommonManager.Instance.CustTunerTextColor;

            bool noWrap = PopUpMode == true ? false : Settings.Instance.TunerServiceNoWrap;

            //録画中のものを後で描画する
            Items = Items.Cast<TunerReserveViewItem>().OrderBy(info => info.Data.IsOnRec()).ToList();

            foreach (TunerReserveViewItem info in Items)
            {
                var resItem = new ReserveItem(info.Data);
                var textDrawList = new List<Tuple<Brush, GlyphRun>>();
                textDrawLists.Add(textDrawList);

                double useHeight = txtMargin.Top;
                double innerLeft = info.LeftPos + txtMargin.Left;
                double innerWidth = info.Width - txtMargin.Width;
                double innnerHeight = info.Height - txtMargin.Height + txtMargin.Top;

                //追加情報の表示
                if (ExtInfoMode == true)
                {
                    string text = info.Status;
                    if (text != "")
                    {
                        RenderText(textDrawList, text, ItemFontNormal, sizeNormal, innerWidth, innnerHeight, innerLeft, info.TopPos, ref useHeight, resItem.StatusColor);
                        useHeight += sizeNormal / 5;
                    }

                    text = resItem.StartTimeShort;
                    text += "\r\n" + "優先度 : " + resItem.Priority;
                    text += "\r\n" + "録画モード : " + resItem.RecMode;
                    RenderText(textDrawList, text, ItemFontNormal, sizeNormal, innerWidth, innnerHeight, innerLeft, info.TopPos, ref useHeight, info.ServiceColor);
                    useHeight += sizeNormal / 2;
                }
                else
                {
                    //分のみ
                    RenderText(textDrawList, info.Data.StartTime.Minute.ToString("d02"), ItemFontNormal, sizeMin, innerWidth, innnerHeight, innerLeft, info.TopPos, ref useHeight, info.ServiceColor);
                    useHeight = txtMargin.Top;//タイトルは同じ行なのでリセット
                }

                //サービス名
                string serviceName = info.Data.StationName + "(" + CommonManager.ConvertNetworkNameText(info.Data.OriginalNetworkID) + ")";
                serviceName = CommonManager.ReplaceText(serviceName, ReplaceDictionaryTitle);
                RenderText(textDrawList, serviceName, ItemFontTitle, sizeTitle, innerWidth - indentTitle, innnerHeight, innerLeft + indentTitle, info.TopPos, ref useHeight, info.ServiceColor, noWrap);
                useHeight += sizeTitle / 3;
                if (useHeight > info.Height) continue;

                //番組名
                string title = CommonManager.ReplaceText(info.Data.Title, ReplaceDictionaryTitle);
                RenderText(textDrawList, title, ItemFontNormal, sizeNormal, innerWidth - indentNormal, innnerHeight, innerLeft + indentNormal, info.TopPos, ref useHeight, colorNormal);

                RenderTextHeight = Math.Max(RenderTextHeight, useHeight);
            }
        }
    }
}
