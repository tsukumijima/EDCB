using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpgTimer
{
    public class PicUpTitle : IDeepCloneObj
    {
        public bool UseCustom { get; set; }
        public List<string> ReplaceSet { get; private set; }
        public List<string> TitleSet { get; private set; }

        public object DeepCloneObj()
        {
            var other = (PicUpTitle)MemberwiseClone();
            other.ReplaceSet = ReplaceSet.ToList();
            other.TitleSet = TitleSet.ToList();
            return other;
        }

        public PicUpTitle()
        {
            UseCustom = false;
            ReplaceSet = new List<string>();
            TitleSet = new List<string>();
        }

        private const string marks = "(\\[[^\\]]+\\]|【[^】]+】|［[^］]+］)+";//.+?にすると末尾からのときミスヒットする
        public static readonly string[] ReplaceSetDefault = new[]
        {
            "<.+?>|＜.+?＞|[□■◇◆▽▼].*", //補足や番組説明
            "[\\[［【\\(（](二|無料?|字幕?版?|吹替?版?|[^\\]］】\\)）]*リマスター.*?)[\\]］】\\)）]", //特定の記号。'リマスター'の前は'.*?'不可
            "((HD|ＨＤ|ハイビジョン|デジタル)リマスター(HD|ＨＤ)?版?)|(リマスター(HD|ＨＤ|版)版?)", //'リマスター'カッコ無し版、'リマスター'だけのものは消さない
            "^" + marks, //その他先頭の記号
            "^[#＃♯第][\\d０-９]+[話回部]?", //先頭にあるドラマ等の話数。記号類を消してから実行。
            "([#＃♯第][\\d０-９]|[\\(（][\\d０-９]+[\\)）]\\s*[「『]).*", //ドラマ等の話数から後ろ全て。前行と組み合わせでないと全部消えるので注意。
            marks + "$", //その他末尾の記号、話数の手前に入ることもあるので最後に。でも消えすぎるときもある。
        };
        public static readonly string[] TitleSetDefault = new[]
        {
            "^[「『](?<Title>.+?)[」』]",
            "(シネマ?ズ?|シアター|プレミアム|ロード(ショー|SHOW!|ＳＨＯＷ！)|午後ロード|映画天国)\\s*[「『](?<Title>.+?)[」』]",
            "(シネマ|キネマ).*『(?<Title>.+?)』", //汎用ではなく『』のみヒットさせる
        };

        public string PicUp(string val)
        {
            if (string.IsNullOrEmpty(val)) return val;

            // 記号など除去
            foreach (string exp in UseCustom == false ? ReplaceSetDefault : ReplaceSet.Where(s => string.IsNullOrWhiteSpace(s) == false))
            {
                val = Regex.Replace(val.Trim(), exp, string.Empty);
            }
            val = val.Trim();

            // 映画のタイトル抽出
            foreach (string exp in UseCustom == false ? TitleSetDefault : TitleSet.Where(s => string.IsNullOrWhiteSpace(s) == false))
            {
                Match m = Regex.Match(val, exp);
                if (m.Success == true)
                {
                    val = m.Groups["Title"].Value.Trim();
                    break;
                }
            }
            return val;
        }
    }
}
