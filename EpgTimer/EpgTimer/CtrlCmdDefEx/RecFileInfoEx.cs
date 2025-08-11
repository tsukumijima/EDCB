using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EpgTimer
{
    public partial class RecFileInfo : AutoAddTargetDataStable
    {
        public override string DataTitle { get { return Title; } }
        public override ulong DataID { get { return ID; } }
        public override DateTime PgStartTime { get { return StartTime; } }
        public override uint PgDurationSecond { get { return DurationSecond; } }
        public override ulong Create64PgKey()
        {
            return CommonManager.Create64PgKey(OriginalNetworkID, TransportStreamID, ServiceID, EventID);
        }

        //簡易ステータス
        public RecEndStatusBasic RecStatusBasic
        {
            get
            {
                switch ((RecEndStatus)RecStatus)
                {
                    case RecEndStatus.NORMAL:           //終了・録画終了
                    case RecEndStatus.CHG_TIME:         //開始時間が変更されました
                    case RecEndStatus.NEXT_START_END:   //次の予約開始のためにキャンセルされました
                    case RecEndStatus.NO_RECMODE:       //無効扱いでした(現在未使用)
                        return RecEndStatusBasic.DEFAULT;
                    case RecEndStatus.ERR_END:          //録画中にキャンセルされた可能性があります
                    case RecEndStatus.END_SUBREC:       //録画終了（空き容量不足で別フォルダへの保存が発生）
                    case RecEndStatus.NOT_START_HEAD:   //一部のみ録画が実行された可能性があります
                        return RecEndStatusBasic.WARN;
                    default:                            //前記以外その他、状況不明含む
                        return RecEndStatusBasic.ERR;
                }
            }
        }

        public string[] GetProgramInfoParts()
        {
            // 分離した情報として返す
            var parts = new string[3] { ProgramInfo ?? "", "", "" };
            // 2個目の空行までマッチ
            Match m = Regex.Match(parts[0], @"^[\s\S]*?\r?\n\r?\n[\s\S]*?\r?\n\r?\n");
            if (m.Success)
            {
                parts[2] = parts[0].Substring(m.Length);
                parts[0] = parts[0].Substring(0, m.Length);
                // "詳細情報"のとき空行2行までマッチ
                m = Regex.Match(parts[2], @"^詳細情報\r?\n[\s\S]*?\r?\n\r?\n\r?\n");
                if (m.Success)
                {
                    parts[1] = parts[2].Substring(0, m.Length);
                    parts[2] = parts[2].Substring(m.Length);
                }
            }
            return parts;
        }
        public void ProgramInfoSet()
        {
            if (ProgramInfo == null)//.program.txtがない
            {
                EpgEventInfo pg = GetPgInfo();
                ProgramInfo = pg == null ? "番組情報がありません。" : CommonManager.ConvertProgramText(pg, EventInfoTextMode.AllForProgramText);
            }
        }
        public EpgEventInfo GetPgInfo(bool isSrv = true)
        {
            if (ID == 0) return null;

            //まずは手持ちのデータを探す
            EpgEventInfo pg = MenuUtil.GetPgInfoUidAll(CurrentPgUID());
            if (pg != null || isSrv == false) return pg;

            //過去番組情報を探してみる
            if (PgStartTime >= CommonManager.Instance.DB.EventTimeMinArc)
            {
                var arcList = new List<EpgServiceEventInfo>();
                CommonManager.Instance.DB.LoadEpgArcData(PgStartTime, PgStartTime.AddSeconds(1), ref arcList, Create64Key().IntoList());
                if (arcList.Any()) return arcList[0].eventList.FirstOrDefault();
            }

            //現在番組情報も探してみる ※EPGデータ未読み込み時で、録画直後の場合
            if (CommonManager.Instance.DB.IsEpgLoaded == false)
            {
                var list = new List<EpgEventInfo>();
                try { CommonManager.CreateSrvCtrl().SendGetPgInfoList(Create64PgKey().IntoList(), ref list); } catch { }
                return list.FirstOrDefault();
            }

            return null;
        }

        public override List<EpgAutoAddData> SearchEpgAutoAddList(bool? IsEnabled = null, bool ByFazy = false)
        {
            //EpgTimerSrv側のSearch()をEpgTimerで実装してないので、簡易な推定によるもの
            return MenuUtil.FazySearchEpgAutoAddData(DataTitle, IsEnabled);
        }
        public override List<EpgAutoAddData> GetEpgAutoAddList(bool? IsEnabled = null)
        {
            //それらしいものを選んでおく
            return SearchEpgAutoAddList(IsEnabled)
                .FindAll(data => data.GetReserveList().FirstOrDefault(data2 => data2.Create64Key() == this.Create64Key()) != null);
        }

        //AppendData 関係。ID(元データ)に対して一意の情報なので、データ自体はDB側。
        private RecFileInfoAppend Append1 { get { return CommonManager.Instance.DB.GetRecFileAppend(this, false); } }
        private RecFileInfoAppend Append2 { get { return CommonManager.Instance.DB.GetRecFileAppend(this, true); } }
        public string ProgramInfo       { get { return Append1.ProgramInfo; } set { Append1.ProgramInfo = value; } }
        public string ErrInfo           { get { return Append1.ErrInfo; } }
        public bool HasErrPackets       { get { return this.Drops != 0 || this.Scrambles != 0; } }
        public long DropsCritical       { get { return this.Drops == 0 ? 0 : Append2.DropsCritical; } }
        public long ScramblesCritical   { get { return this.Scrambles == 0 ? 0 : Append2.ScramblesCritical; } }
    }

    public static class RecFileInfoEx
    {
        public static List<RecFileInfo> GetNoProtectedList(this IEnumerable<RecFileInfo> itemlist)
        {
            return itemlist.Where(item => item == null ? false : item.ProtectFlag == 0).ToList();
        }
        public static bool HasNoProtected(this IEnumerable<RecFileInfo> list)
        {
            return list.Any(info => info == null ? false : info.ProtectFlag == 0);
        }
    }

    public class RecFileInfoAppend
    {
        public bool IsValid { get { return ErrInfo != null; } }
        public string ProgramInfo { get; set; }

        private string errInfo = null;
        public string ErrInfo { get { UpdateInfo(); return errInfo; } }
        private long drops = 0;
        private long dropsCritical = 0;
        public long DropsCritical { get { UpdateInfo(); return dropsCritical; } }
        private long scrambles = 0;
        private long scramblesCritical = 0;
        public long ScramblesCritical { get { UpdateInfo(); return scramblesCritical; } }

        private bool needUpdate = false;
        public void SetUpdateNotify() { needUpdate = IsValid; }

        public RecFileInfoAppend(RecFileInfo info, bool isValid = true)
        {
            if (isValid == true)
            {
                if (info.EventID == 0xFFFF || string.IsNullOrEmpty(info._ProgramInfo) == false)
                {
                    ProgramInfo = info._ProgramInfo;
                }
                errInfo = info._ErrInfo;
            }
            drops = info.Drops;
            dropsCritical = drops;
            scrambles = info.Scrambles;
            scramblesCritical = scrambles;
            SetUpdateNotify();
        }

        public void UpdateInfo()
        {
            if (needUpdate == false) return;
            needUpdate = false;

            if (string.IsNullOrEmpty(errInfo) == false)
            {
                try
                {
                    dropsCritical = 0;
                    scramblesCritical = 0;
                    var newInfo = new StringBuilder("");

                    string[] lines = errInfo.Split(new char[] { '\n' });
                    foreach (string ln in lines)
                    {
                        string line = ln.Replace("*", " ");
                        if (line.StartsWith("PID:") == true)
                        {
                            string[] words = line.Split(new char[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            //デフォルト { "EIT", "NIT", "CAT", "SDT", "SDTT", "TOT", "ECM", "EMM" }
                            if (Settings.Instance.RecInfoDropExcept.FirstOrDefault(s => words[8].Contains(s)) == null)
                            {
                                dropsCritical += (long)Convert.ToUInt64(words[5]);
                                scramblesCritical += (long)Convert.ToUInt64(words[7]);
                                line = line.Replace(" " + words[8], "*" + words[8]);
                            }
                        }
                        newInfo.Append(line.TrimEnd('\r') + "\r\n");//単に\n付けるだけでも良いが、一応"\r\n"に確定させる
                        if (ln.Contains("使用BonDriver") == true) break;
                    }
                    newInfo.Append("\r\n");
                    newInfo.AppendFormat("                              * = Critical Drop/Scramble Parameter.\r\n");
                    newInfo.AppendFormat("                              Drop:{0,9}  Scramble:{1,10}  Total\r\n", drops, scrambles);
                    newInfo.AppendFormat("                             *Drop:{0,9} *Scramble:{1,10} *Critical\r\n", dropsCritical, scramblesCritical);
                    errInfo = newInfo.ToString();
                }
                catch { }
            }
        }
    }
}
