using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace EpgTimer
{
    public static class ItemFontCache
    {
        public static void Clear() { cacheFont.Clear(); cacheType.Clear(); }

        private static Dictionary<string, ItemFont> cacheFont = new Dictionary<string, ItemFont>();
        public static ItemFont ItemFont(string familyName, bool isBold)
        {
            string key = string.Format("{0}::{1}", familyName, isBold);
            if (cacheFont.ContainsKey(key) == false)
            {
                cacheFont.Add(key, new ItemFont(familyName, isBold));
            }
            return cacheFont[key];
        }

        private static Dictionary<string, GlyphTypeSet> cacheType = new Dictionary<string, GlyphTypeSet>();
        public static GlyphTypeSet GetGlyphType(FontFamily fontfamily, bool isBold)
        {
            string key = string.Format("{0}::{1}", fontfamily.Source, isBold);
            if (cacheType.ContainsKey(key) == false)
            {
                cacheType.Add(key, new GlyphTypeSet(fontfamily, isBold));
            }
            return cacheType[key];
        }
        public class GlyphTypeSet
        {
            public readonly GlyphTypeface Type ;
            public readonly Dictionary<int, ushort> hitDic = new Dictionary<int, ushort>();
            public readonly int[] indexCache;
            public readonly float[] widthCache;
            public GlyphTypeSet(FontFamily fontfamily, bool isBold)
            {
                new Typeface(fontfamily, FontStyles.Normal, isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out Type);
                indexCache = Type == null ? null : Enumerable.Repeat<int>(-1, ushort.MaxValue + 1).ToArray();
                widthCache = Type == null ? null : new float[ushort.MaxValue + 1];
            }
            public ushort GlyphIndex(int key)
            {
                ushort glyphIndex;
                if (key < indexCache.Length)
                {
                    if (indexCache[key] >= 0)
                    {
                        return (ushort)indexCache[key];
                    }
                    Type.CharacterToGlyphMap.TryGetValue(key, out glyphIndex);
                    indexCache[key] = glyphIndex;
                }
                else
                {
                    if (hitDic.TryGetValue(key, out glyphIndex) == true)
                    {
                        return glyphIndex;
                    }
                    Type.CharacterToGlyphMap.TryGetValue(key, out glyphIndex);
                    hitDic.Add(key, glyphIndex);
                }

                double glyphWidth;
                Type.AdvanceWidths.TryGetValue(glyphIndex, out glyphWidth);
                widthCache[glyphIndex] = (float)glyphWidth;
                return glyphIndex;
            }
        }
    }
    public class ItemFont
    {
        public readonly GlyphTypeface[] GlyphType;
        private readonly ItemFontCache.GlyphTypeSet[] cacheSet;
        private readonly short[] fontCache = Enumerable.Repeat<short>(-1, ushort.MaxValue + 1).ToArray();
        private readonly Dictionary<int, Tuple<int, ushort>> fontDic = new Dictionary<int, Tuple<int, ushort>>();
        public ItemFont(string familyName, bool isBold)
        {
            cacheSet = familyName.Split(new[] { ',' })
                .Select(s => ItemFontCache.GetGlyphType(new FontFamily(s.Trim()), isBold))
                .Where(t => t.Type != null).Distinct().ToArray();
            if (cacheSet.Length == 0)
            {
                cacheSet = new[] { ItemFontCache.GetGlyphType(SystemFonts.MessageFontFamily, isBold) };
            }
            GlyphType = cacheSet.Select(t => t.Type).ToArray();
        }
        public double GlyphWidth(string line, ref int n, out ushort glyphIndex, out int fontIndex)
        {
            int key = line[n];
            fontIndex = fontCache[key];
            if (fontIndex >= 0)
            {
                glyphIndex = (ushort)cacheSet[fontIndex].indexCache[key];
                return cacheSet[fontIndex].widthCache[glyphIndex];
            }

            glyphIndex = 0;
            if (char.IsSurrogatePair(line, n))
            {
                key = char.ConvertToUtf32(line, n++);
                Tuple<int, ushort> keyData;
                if (fontDic.TryGetValue(key, out keyData) == true)
                {
                    fontIndex = keyData.Item1;
                    glyphIndex = keyData.Item2;
                    return cacheSet[fontIndex].widthCache[glyphIndex];
                }
            }
            else if (char.IsSurrogate((char)key)) key = 0;//可能性は低いが一応不完全なサロゲートペアの混入対策

            for (fontIndex = 0; fontIndex < cacheSet.Length; fontIndex++)
            {
                glyphIndex = cacheSet[fontIndex].GlyphIndex(key);
                if (glyphIndex != 0) break;
            }
            if (fontIndex == cacheSet.Length) fontIndex = 0;

            if (key < cacheSet[fontIndex].indexCache.Length)
            {
                fontCache[key] = (byte)fontIndex;
            }
            else
            {
                fontDic[key] = new Tuple<int, ushort>(fontIndex, glyphIndex);
            }
            return cacheSet[fontIndex].widthCache[glyphIndex];
        }
    }
}