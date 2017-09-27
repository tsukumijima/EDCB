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
            SetBorderStyle(Settings.Instance.EpgBorderLeftSize, Settings.Instance.EpgBorderTopSize, new Thickness(3, 0, 3, Settings.Instance.FontSize / 1.7));
        }

        protected override void CreateDrawTextListMain(List<List<Tuple<Brush, GlyphRun>>> textDrawLists)
        {
            var ItemFontNormal = ItemFontCache.ItemFont(Settings.Instance.FontName, false);
            var ItemFontTitle = ItemFontCache.ItemFont(Settings.Instance.FontNameTitle, Settings.Instance.FontBoldTitle);

            double sizeMin = Settings.Instance.FontSizeTitle - 1;
            double sizeTitle = Settings.Instance.FontSizeTitle;
            double sizeNormal = Settings.Instance.FontSize;
            double indentTitle = sizeMin * 1.7;
            double indentNormal = Settings.Instance.EpgTitleIndent ? indentTitle : 0;
            Brush colorTitle = CommonManager.Instance.CustTitle1Color;
            Brush colorNormal = CommonManager.Instance.CustTitle2Color;

            foreach (ProgramViewItem info in Items)
            {
                var textDrawList = new List<Tuple<Brush, GlyphRun>>();
                textDrawLists.Add(textDrawList);

                double useHeight = txtMargin.Top;
                double innerLeft = info.LeftPos + txtMargin.Left;
                double innerWidth = info.Width - txtMargin.Width;
                double innerHeight = info.Height - txtMargin.Height + txtMargin.Top;

                //分
                string min = (info.Data.StartTimeFlag == 0 ? "未定 " : info.Data.start_time.Minute.ToString("d02"));
                RenderText(textDrawList, min, ItemFontTitle, sizeMin, innerWidth, innerHeight, innerLeft, info.TopPos, ref useHeight, colorTitle);
                info.TitleDrawErr = useHeight > info.Height;
                useHeight = txtMargin.Top;//タイトルは同じ行なのでリセット

                //番組情報
                if (info.Data.ShortInfo != null)
                {
                    //タイトル
                    string title = CommonManager.ReplaceText(info.Data.ShortInfo.event_name, ReplaceDictionaryTitle);
                    RenderText(textDrawList, title, ItemFontTitle, sizeTitle, innerWidth - indentTitle, innerHeight, innerLeft + indentTitle, info.TopPos, ref useHeight, colorTitle);
                    info.TitleDrawErr |= useHeight > info.Height;
                    useHeight += sizeTitle / 3;
                    if (useHeight > info.Height) continue;

                    //説明
                    string detail = info.Data.ShortInfo.text_char.TrimEnd('\r', '\n');
                    detail += ExtInfoMode == false || info.Data.ExtInfo == null ? "" : "\r\n\r\n" + info.Data.ExtInfo.text_char.TrimEnd('\r', '\n');
                    detail = CommonManager.ReplaceText(detail, ReplaceDictionaryNormal);
                    RenderText(textDrawList, detail, ItemFontNormal, sizeNormal, innerWidth - indentNormal, innerHeight, innerLeft + indentNormal, info.TopPos, ref useHeight, colorNormal);
                }

                RenderTextHeight = Math.Max(RenderTextHeight, useHeight);
            }
        }
    }
}
