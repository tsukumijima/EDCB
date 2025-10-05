using System;

namespace EpgTimer
{
    public class NotifySrvInfoItem : GridViewSorterItem
    {
        private ulong keyID;
        public override ulong KeyID { get { return keyID; } }

        public NotifySrvInfoItem() { }
        public NotifySrvInfoItem(NotifySrvInfo info, bool delCrlf = true)
        {
            Time = info.time.ToString("yyyy/MM/dd HH:mm:ss.fff");
            TimeView = info.time.ToString("yyyy/MM/dd(ddd) HH:mm:ss.fff");
            var notifyID = (UpdateNotifyItem)info.notifyID;
            Title = notifyID == UpdateNotifyItem.PreRecStart ? "予約録画開始準備" :
                    notifyID == UpdateNotifyItem.RecStart ? "録画開始" :
                    notifyID == UpdateNotifyItem.RecEnd ? "録画終了" :
                    notifyID == UpdateNotifyItem.RecTuijyu ? "追従発生" :
                    notifyID == UpdateNotifyItem.ChgTuijyu ? "番組変更" :
                    notifyID == UpdateNotifyItem.PreEpgCapStart ? "EPG取得" :
                    notifyID == UpdateNotifyItem.EpgCapStart ? "EPG取得" :
                    notifyID == UpdateNotifyItem.EpgCapEnd ? "EPG取得" : info.notifyID.ToString();
            LogText = notifyID == UpdateNotifyItem.EpgCapStart ? "開始" :
                      notifyID == UpdateNotifyItem.EpgCapEnd ? "終了" : info.param4;
            if (delCrlf == true) LogText = LogText.Replace("\r\n↓\r\n", "  →  ");
            if (delCrlf == true) LogText = LogText.Replace("\r\n", "  ");
            keyID = (ulong)this.ToString().GetHashCode();
        }
        public NotifySrvInfoItem(string text)
        {
            string[] s = text.Split(new char[] { '[', ']' }, 3);
            Time = s.Length > 0 ? s[0].TrimEnd(' ') : "";
            DateTime time;
            TimeView = DateTime.TryParse(Time, out time) == false ? Time : time.ToString("yyyy/MM/dd(ddd) HH:mm:ss.fff");
            Title = s.Length > 1 ? s[1] : "";
            LogText = s.Length > 2 ? s[2].TrimStart(' ') : "";
            keyID = (ulong)this.ToString().GetHashCode();
        }
        public string TimeView { get; private set; }
        public string Time { get; private set; }
        public string Title { get; private set; }
        public string LogText { get; private set; }
        public override string ToString() { return Time + " [" + Title + "] " + LogText; }
    }
}
