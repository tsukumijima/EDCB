using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace EpgTimer
{
    public class RecInfoItem : DataListItemBase
    {
        public RecInfoItem() { }
        public RecInfoItem(RecFileInfo item) { RecInfo = item; }

        public RecFileInfo RecInfo { get; private set; }
        public override ulong KeyID { get { return RecInfo == null ? 0 : RecInfo.ID; } }
        public override object DataObj { get { return RecInfo; } }

        public bool IsProtect
        {
            set { EpgCmds.ChgOnOffCheck.Execute(this, null); }
            get { return RecInfo.ProtectFlag != 0; }
        }
        public string EventName
        {
            get { return RecInfo.Title; }
        }
        public string EventNameValue
        {
            get { return Settings.Instance.TrimSortTitle == true ? MenuUtil.TrimKeyword(EventName) : EventName; }
        }
        public string ServiceName
        {
            get { return RecInfo.ServiceName; }
        }
        public string ProgramDuration
        {
            get { return CommonManager.ConvertDurationText(RecInfo.DurationSecond, Settings.Instance.RecInfoNoDurSecond); }
        }
        public UInt32 ProgramDurationValue
        {
            get { return RecInfo.DurationSecond; }
        }
        public string StartTime
        {
            get { return CommonManager.ConvertTimeText(RecInfo.StartTime, RecInfo.DurationSecond, Settings.Instance.RecInfoNoYear, Settings.Instance.RecInfoNoSecond); }
        }
        public long StartTimeValue
        {
            get { return RecInfo.StartTime.Ticks; }
        }
        public long Drops
        {
            get { return RecInfo.Drops; }
        }
        public long DropsSerious
        {
            get { return RecInfo.DropsCritical; }
        }
        public long Scrambles
        {
            get { return RecInfo.Scrambles; }
        }
        public long ScramblesSerious
        {
            get { return RecInfo.ScramblesCritical; }
        }
        public string Result
        {
            get { return RecInfo.Comment; }
        }
        public string NetworkName
        {
            get { return CommonManager.ConvertNetworkNameText(RecInfo.OriginalNetworkID); }
        }
        public string RecFilePath
        {
            get { return RecInfo.RecFilePath; }
        }
        public override Brush BackColor
        {
            get
            {
                //番組表へジャンプ時の強調表示
                if (NowJumpingTable != 0) return base.BackColor;

                //通常表示
                long drops = Settings.Instance.RecinfoErrCriticalDrops == false ? RecInfo.Drops : RecInfo.DropsCritical;
                long scrambles = Settings.Instance.RecinfoErrCriticalDrops == false ? RecInfo.Scrambles : RecInfo.ScramblesCritical;

                if (Settings.Instance.RecInfoDropErrIgnore >= 0 && drops > Settings.Instance.RecInfoDropErrIgnore
                    || RecInfo.RecStatusBasic == RecEndStatusBasic.ERR)
                {
                    return CommonManager.Instance.RecEndBackColor[1];
                }
                if (Settings.Instance.RecInfoDropWrnIgnore >= 0 && drops > Settings.Instance.RecInfoDropWrnIgnore
                    || Settings.Instance.RecInfoScrambleIgnore >= 0 && scrambles > Settings.Instance.RecInfoScrambleIgnore
                    || RecInfo.RecStatusBasic == RecEndStatusBasic.WARN)
                {
                    return CommonManager.Instance.RecEndBackColor[2];
                }
                return CommonManager.Instance.RecEndBackColor[0];
            }
        }
        public override string ConvertInfoText(object param = null)
        {
            var mode = param is Int32 ? (Int32)param : Settings.Instance.RecInfoToolTipMode;
            if (mode == 1) return RecInfo.ProgramInfo;

            string view = CommonManager.ConvertTimeText(RecInfo.StartTime, RecInfo.DurationSecond, false, false, false) + "\r\n";
            view += ServiceName + "(" + NetworkName + ")" + "\r\n";
            view += EventName + "\r\n\r\n";

            view += "録画結果 : " + Result + "\r\n";
            view += "録画ファイルパス : " + RecFilePath + "\r\n";
            view += ConvertDropText() + "\r\n";
            view += ConvertScrambleText()+ "\r\n\r\n";

            view += CommonManager.Convert64PGKeyString(RecInfo.Create64PgKey());

            return view;
        }
        public string DropInfoText
        {
            get { return ConvertDropText("D:") + " " + ConvertScrambleText("S:"); }
        }
        private string ConvertDropText(string title = "Drops : ")
        {
            if (Settings.Instance.RecinfoErrCriticalDrops == true)
            {
                return "*" + title + RecInfo.DropsCritical.ToString();
            }
            else
            {
                return title + RecInfo.Drops.ToString();
            }
        }
        private string ConvertScrambleText(string title = "Scrambles : ")
        {
            if (Settings.Instance.RecinfoErrCriticalDrops == true)
            {
                return "*" + title + RecInfo.ScramblesCritical.ToString();
            }
            else
            {
                return title + RecInfo.Scrambles.ToString();
            }
        }
    }

    public static class RecInfoItemEx
    {
        public static List<RecFileInfo> RecInfoList(this ICollection<RecInfoItem> itemlist)
        {
            return itemlist.Where(item => item != null).Select(item => item.RecInfo).ToList();
        }
    }
}
