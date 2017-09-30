using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer.EpgView
{
    class EpgViewPanel : ViewPanel
    {
        public override void SetBorderStyleFromSettings()
        {
            SetBorderStyle(Settings.Instance.EpgBorderLeftSize, Settings.Instance.EpgBorderTopSize, new Thickness(3, 0, 3, Settings.Instance.FontSize * 0.8));
        }

        protected override void CreateDrawTextListMain(List<List<Tuple<Brush, GlyphRun>>> textDrawLists)
        {
            var ItemFontNormal = ItemFontCache.ItemFont(Settings.Instance.FontName, false);
            var ItemFontTitle = ItemFontCache.ItemFont(Settings.Instance.FontNameTitle, Settings.Instance.FontBoldTitle);

            double sizeTitle = Settings.Instance.FontSizeTitle;
            double sizeMin = Math.Max(sizeTitle - 1, Math.Min(sizeTitle, Settings.Instance.FontSize));
            double sizeNormal = Settings.Instance.FontSize;
            double indentTitle = sizeMin * 1.7;
            double indentNormal = Settings.Instance.EpgTitleIndent ? indentTitle : 0;
            Brush colorTitle = CommonManager.Instance.CustTitle1Color;
            Brush colorNormal = CommonManager.Instance.CustTitle2Color;

            foreach (ProgramViewItem info in Items)
            {
                var textDrawList = new List<Tuple<Brush, GlyphRun>>();
                textDrawLists.Add(textDrawList);
                Rect drawRect = TextRenderRect(info);
                info.TitleDrawErr = sizeTitle > drawRect.Height;

                //分
                string min = info.Data.StartTimeFlag == 0 ? "？" : info.Data.start_time.Minute.ToString("d02");
                double useHeight = sizeNormal / 3 + RenderText(textDrawList, min, ItemFontTitle, sizeMin, drawRect, 0, 0, colorTitle);
                
                //番組情報
                if (info.Data.ShortInfo != null)
                {
                    //タイトル
                    string title = CommonManager.ReplaceText(info.Data.ShortInfo.event_name.TrimEnd(), ReplaceDictionaryTitle);
                    useHeight = sizeTitle / 3 + RenderText(textDrawList, title, ItemFontTitle, sizeTitle, drawRect, indentTitle, 0, colorTitle);
                    
                    //説明
                    if (useHeight < drawRect.Height)
                    {
                        string detail = info.Data.ShortInfo.text_char.TrimEnd('\r', '\n');
                        detail += ExtInfoMode == false || info.Data.ExtInfo == null ? "" : "\r\n\r\n" + info.Data.ExtInfo.text_char;
                        detail = CommonManager.ReplaceText(detail.TrimEnd(), ReplaceDictionaryNormal);
                        if (detail != "") useHeight += sizeNormal / 3 + RenderText(textDrawList, detail, ItemFontNormal, sizeNormal, drawRect, indentNormal, useHeight, colorNormal);
                    }
                }

                SaveMaxRenderHeight(useHeight);
            }
        }
    }
}
