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
            public readonly GlyphTypeface Type = null;
            public readonly ushort[] indexCache = null;
            public readonly float[] widthCache = null;
            public GlyphTypeSet(FontFamily fontfamily, bool isBold)
            {
                new Typeface(fontfamily, FontStyles.Normal, isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal).TryGetGlyphTypeface(out Type);
                indexCache = Type == null ? null : new ushort[ushort.MaxValue + 1];
                widthCache = Type == null ? null : new float[ushort.MaxValue + 1];
            }
        }
    }
    public class ItemFont
    {
        public readonly GlyphTypeface[] GlyphType = null;
        private readonly ItemFontCache.GlyphTypeSet[] cacheSet = null;
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
            for (fontIndex = 0; fontIndex < cacheSet.Length; fontIndex++)
            {
                glyphIndex = cacheSet[fontIndex].indexCache[key];
                if (glyphIndex != 0) return cacheSet[fontIndex].widthCache[glyphIndex];
            }

            if (char.IsSurrogatePair(line, n))
            {
                key = char.ConvertToUtf32(line, n++);
            }
            else if (char.IsSurrogate((char)key))//不完全なサロゲートペアの混入対策
            {
                key = 0;
            }

            glyphIndex = 0;
            for (fontIndex = 0; fontIndex < cacheSet.Length; fontIndex++)
            {
                if (GlyphType[fontIndex].CharacterToGlyphMap.TryGetValue(key, out glyphIndex) == true) break;
            }
            if (fontIndex == cacheSet.Length) fontIndex = 0;

            double glyphWidth;
            GlyphType[fontIndex].AdvanceWidths.TryGetValue(glyphIndex, out glyphWidth);
            if (key < cacheSet[fontIndex].indexCache.Length)
            {
                cacheSet[fontIndex].indexCache[key] = glyphIndex;
                cacheSet[fontIndex].widthCache[glyphIndex] = (float)glyphWidth;
            }

            return (float)glyphWidth;
        }
    }
}