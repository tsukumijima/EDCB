using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpgTimer
{
    public partial class RecFileInfo : AutoAddTargetData
    {
        public override string DataTitle { get { return Title; } }
        public override ulong DataID { get { return ID; } }
        public override DateTime PgStartTime { get { return StartTime; } }
        public override uint PgDurationSecond { get { return DurationSecond; } }
        public override UInt64 Create64PgKey()
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
                    case RecEndStatus.NORMAL:           //正常終了
                        return RecEndStatusBasic.DEFAULT;
                    case RecEndStatus.OPEN_ERR:         //チューナーのオープンができなかった
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.ERR_END:          //録画中にエラーが発生した
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.NEXT_START_END:   //次の予約開始のため終了
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.START_ERR:        //開始時間が過ぎていた
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.CHG_TIME:         //開始時間が変更された
                        return RecEndStatusBasic.DEFAULT;
                    case RecEndStatus.NO_TUNER:         //チューナーが足りなかった
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.NO_RECMODE:       //無効扱いだった
                        return RecEndStatusBasic.DEFAULT;
                    case RecEndStatus.NOT_FIND_PF:      //p/fに番組情報確認できなかった
                        return RecEndStatusBasic.WARN;
                    case RecEndStatus.NOT_FIND_6H:      //6時間番組情報確認できなかった
                        return RecEndStatusBasic.WARN;
                    case RecEndStatus.END_SUBREC:       //サブフォルダへの録画が発生した
                        return RecEndStatusBasic.WARN;
                    case RecEndStatus.ERR_RECSTART:     //録画開始に失敗した
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.NOT_START_HEAD:   //一部のみ録画された
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.ERR_CH_CHG:       //チャンネル切り替えに失敗した
                        return RecEndStatusBasic.ERR;
                    case RecEndStatus.ERR_END2:         //録画中にエラーが発生した(Writeでexception)
                        return RecEndStatusBasic.ERR;
                    default:                            //状況不明
                        return RecEndStatusBasic.ERR;
                }
            }
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

        public EpgEventInfo SearchEventInfo()
        {
            //それらしいものを選んでみる
            return MenuUtil.SearchEventInfoLikeThat(this, true);
        }

        //AppendData 関係。ID(元データ)に対して一意の情報なので、データ自体はDB側。
        private RecFileInfoAppend Append1 { get { return CommonManager.Instance.DB.GetRecFileAppend(this, false); } }
        private RecFileInfoAppend Append2 { get { return CommonManager.Instance.DB.GetRecFileAppend(this, true); } }
        public string ProgramInfo       { get { return Append1.ProgramInfo; } }
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
        //public static bool HasProtected(this IEnumerable<RecInfoItem> list)
        //{
        //    return list.Any(info => info == null ? false : info.RecInfo.ProtectFlag == true);
        //}
        public static bool HasNoProtected(this IEnumerable<RecFileInfo> list)
        {
            return list.Any(info => info == null ? false : info.ProtectFlag == 0);
        }

        public static List<RecFileInfo> Clone(this IEnumerable<RecFileInfo> src) { return CopyObj.Clone(src, CopyData); }
        public static RecFileInfo Clone(this RecFileInfo src) { return CopyObj.Clone(src, CopyData); }
        public static void CopyTo(this RecFileInfo src, RecFileInfo dest) { CopyObj.CopyTo(src, dest, CopyData); }
        private static void CopyData(RecFileInfo src, RecFileInfo dest)
        {
            dest.Comment = src.Comment;
            dest.Drops = src.Drops;
            dest.DurationSecond = src.DurationSecond;
            dest._ErrInfo = src._ErrInfo;
            dest.EventID = src.EventID;
            dest.ID = src.ID;
            dest.OriginalNetworkID = src.OriginalNetworkID;
            dest._ProgramInfo = src._ProgramInfo;
            dest.ProtectFlag = src.ProtectFlag;
            dest.RecFilePath = src.RecFilePath;
            dest.RecStatus = src.RecStatus;
            dest.Scrambles = src.Scrambles;
            dest.ServiceID = src.ServiceID;
            dest.ServiceName = src.ServiceName;
            dest.StartTime = src.StartTime;
            dest.StartTimeEpg = src.StartTimeEpg;
            dest.Title = src.Title;
            dest.TransportStreamID = src.TransportStreamID;
        }
    }

    public class RecFileInfoAppend
    {
        public bool IsValid { get { return ProgramInfo != null; } }
        public string ProgramInfo { get; protected set; }

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
                ProgramInfo = info._ProgramInfo;
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
                                dropsCritical += (Int64)Convert.ToUInt64(words[5]);
                                scramblesCritical += (Int64)Convert.ToUInt64(words[7]);
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
