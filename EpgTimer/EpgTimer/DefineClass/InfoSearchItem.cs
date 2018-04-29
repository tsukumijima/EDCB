using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;

namespace EpgTimer
{
    public class InfoSearchItem : RecSettingItem, IRecWorkMainData
    {
        public InfoSearchItem() { tIdx = dTypes.Count - 1; }
        public InfoSearchItem(IRecWorkMainData data)
        {
            tIdx = dTypes.IndexOf(data.GetType());
            tIdx = tIdx < 0 ? dTypes.Count - 1 : tIdx;

            this.Data = data;
            ViewItem = Activator.CreateInstance(vTypes[tIdx], data) as DataListItemBase;
        }

        public ulong DataID { get { return KeyID; } }
        public override RecSettingData RecSettingInfo { get { return ViewItem is IRecSetttingData ? (ViewItem as IRecSetttingData).RecSettingInfo : null; } }
        public override bool IsManual { get { return ViewItem is IRecSetttingData ? (ViewItem as IRecSetttingData).IsManual : false; } }

        private int tIdx;
        public IRecWorkMainData Data { get; private set; }
        public DataListItemBase ViewItem { get; private set; }

        public static IEnumerable<InfoSearchItem> Items(IRecWorkMainData d) { return new List<InfoSearchItem> { new InfoSearchItem(d) }; }
        public static IEnumerable<InfoSearchItem> Items(IEnumerable<IRecWorkMainData> list) { return list.Select(d => new InfoSearchItem(d)); }

        class DummyType { public DummyType(IRecWorkMainData data) { } }
        private static List<Type> dTypes = new List<Type> { typeof(ReserveData), typeof(RecFileInfo), typeof(EpgAutoAddData), typeof(ManualAutoAddData), typeof(DummyType) };
        private static List<Type> vTypes = new List<Type> { typeof(ReserveItem), typeof(RecInfoItem), typeof(EpgAutoDataItem), typeof(ManualAutoAddDataItem), typeof(DummyType) };

        private static List<string> viewItemNames = new List<string> { "予約", "録画済み", "キーワード予約", "プログラム予約", "" };
        public static List<string> ViewTypeNameList() { return viewItemNames.Take(viewItemNames.Count - 1).ToList(); }
        public string ViewItemName { get { return viewItemNames[tIdx]; } }

        private static List<ulong> keyIDOffset = new List<ulong> { 0x01UL << 60, 0x02UL << 60, 0x03UL << 60, 0x04UL << 60, 0 };
        public override ulong KeyID { get { return keyIDOffset[tIdx] | ViewItem.KeyID; } }
        public override ulong DisplayID { get { return ViewItem.DisplayID; } }
        public override object DataObj { get { return Data; } }

        public string Status
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).Status;
                else if (ViewItem is RecInfoItem)           return ((RecInfoItem)ViewItem).IsProtect == true ? "プロテクト" : "";
                else if (Data is AutoAddData)               return (((AutoAddData)Data).IsEnabled == false ? "無" : "");
                else                                        return "";
            }
        }
        public Brush StatusColor
        {
            get
            {
                if  (ViewItem is ReserveItem)               return ((ReserveItem)ViewItem).StatusColor;
                else                                        return CommonManager.Instance.ResStatusColor[0];
            }
        }
        public string EventName
        {
            get { return Data.DataTitle; }
        }
        public string EventNameValue
        {
            get { return Settings.Instance.TrimSortTitle == true ? MenuUtil.TrimKeyword(EventName) : EventName; }
        }
        public string DataTitle { get { return EventName; } }
        public string StartTime
        {
            get
            {
                if  (ViewItem is AutoAddDataItem)           return ((AutoAddDataItem)ViewItem).NextReserve;
                else if (Data is IBasicPgInfo)              return ReserveItem.GetTimeStringReserveStyle(((IBasicPgInfo)Data).PgStartTime, ((IBasicPgInfo)Data).PgDurationSecond);
                else                                        return "";
            }
        }
        public long StartTimeValue
        {
            get
            {
                if  (ViewItem is AutoAddDataItem)           return ((AutoAddDataItem)ViewItem).NextReserveValue;
                else if (Data is IBasicPgInfo)              return ((IBasicPgInfo)Data).PgStartTime.Ticks;
                else                                        return long.MaxValue;
            }
        }
        public string ProgramDuration
        {
            get
            {
                if      (Data is EpgAutoAddData)            return new ReserveItem(((EpgAutoAddData)Data).GetNextReserve()).ProgramDuration;
                else if (Data is IBasicPgInfo)              return ReserveItem.GetDurationStringReserveStyle(((IBasicPgInfo)Data).PgDurationSecond);
                else                                        return "";
            }
        }
        public UInt32 ProgramDurationValue
        {
            get
            {
                if      (Data is EpgAutoAddData)            return new ReserveItem (((EpgAutoAddData)Data).GetNextReserve()).ProgramDurationValue;
                else if (Data is IBasicPgInfo)              return ((IBasicPgInfo)Data).PgDurationSecond;
                else                                        return UInt32.MaxValue;
            }
        }
        public string NetworkName
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).NetworkName;
                else if (ViewItem is RecInfoItem)           return ((RecInfoItem)ViewItem).NetworkName;
                else if (ViewItem is AutoAddDataItem)       return ((AutoAddDataItem)ViewItem).NetworkName;
                else                                        return "";
            }
        }
        public string ServiceName
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).ServiceName;
                else if (ViewItem is RecInfoItem)           return ((RecInfoItem)ViewItem).ServiceName;
                else if (ViewItem is AutoAddDataItem)       return ((AutoAddDataItem)ViewItem).ServiceName;
                else                                        return "";
            }
        }
        public string JyanruKey
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).JyanruKey;
                else if (ViewItem is EpgAutoDataItem)       return ((EpgAutoDataItem)ViewItem).JyanruKey;
                else                                        return "";
            }
        }
        public string Attrib
        {
            get
            {
                if (ViewItem is ReserveItem)                return ((ReserveItem)ViewItem).Attrib;
                else                                        return "";
            }
        }
        public bool IsEnabled
        {
            set
            {
                EpgCmds.ChgOnOffCheck.Execute(this, null);
            }
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).IsEnabled;
                else if (ViewItem is RecInfoItem)           return ((RecInfoItem)ViewItem).IsProtect;
                else if (ViewItem is AutoAddDataItem)       return ((AutoAddDataItem)ViewItem).KeyEnabled;
                else                                        return false;
            }
        }
        public string Comment
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).Comment;
                else if (ViewItem is RecInfoItem)           return ((RecInfoItem)ViewItem).Result;
                else if (ViewItem is AutoAddDataItem)       return string.Format("検索/予約:{0}/{1}", ((AutoAddDataItem)ViewItem).SearchCount, ((AutoAddDataItem)ViewItem).ReserveCount);
                else return "";
            }
        }
        public string ProgramContent
        {
            get
            {
                string ret = "";
                if      (ViewItem is ReserveItem)           ret = ((ReserveItem)ViewItem).ProgramContent;
                else if (ViewItem is RecInfoItem)           ret = ((RecInfoItem)ViewItem).DropInfoText;
                else if (ViewItem is EpgAutoDataItem)       ret = (string.IsNullOrEmpty(((EpgAutoDataItem)ViewItem).NotKey) == true ? "" : "Notキー:" + ((EpgAutoDataItem)ViewItem).NotKey)
                                                                + (string.IsNullOrEmpty(((EpgAutoDataItem)ViewItem).NextReserveName) == true ? "" : " 予約:" + ((EpgAutoDataItem)ViewItem).NextReserveName);
                else if (ViewItem is ManualAutoAddDataItem) ret = ((ManualAutoAddDataItem)ViewItem).StartTimeShort + " " + ((ManualAutoAddDataItem)ViewItem).DayOfWeek;
                ret = ret.Replace("\r\n", " ");//先に長さを確定
                return ret.Substring(0, Math.Min(50, ret.Length));
            }
        }
        public List<string> RecFileName
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).RecFileName;
                else if (ViewItem is RecInfoItem)           return new List<string> { Path.GetFileName(((RecInfoItem)ViewItem).RecFilePath) };
                else if (Data is EpgAutoAddData)            return new ReserveItem(((EpgAutoAddData)Data).GetNextReserve()).RecFileName;
                else                                        return new List<string>();
            }
        }
        public string ReserveTuner
        {
            get
            {
                if      (ViewItem is ReserveItem)           return ((ReserveItem)ViewItem).ReserveTuner;
                else if (Data is EpgAutoAddData)            return new ReserveItem(((EpgAutoAddData)Data).GetNextReserve()).ReserveTuner;
                else                                        return "";
            }
        }
        public override List<string> RecFolder
        {
            get
            {
                if      (ViewItem is RecInfoItem)           return new List<string> { Path.GetDirectoryName(((RecInfoItem)ViewItem).RecFilePath) };
                else                                        return base.RecFolder;
            }
        }

        public string GetSearchText(bool TitleOnly)
        {
            if (TitleOnly == false)
            {
                if (ViewItem is ReserveItem || ViewItem is RecInfoItem)
                {
                    return ViewItem.ConvertInfoText(0) + ViewItem.ConvertInfoText(1);
                }
                else if (ViewItem is AutoAddDataItem)
                {
                    return ViewItem.ConvertInfoText();
                }
            }
            return DataTitle;
        }

        public bool IsToolTipEnabled = false;
        public override TextBlock ToolTipView
        {
            get
            {
                if (IsToolTipEnabled == false) return null; 
                //
                return ViewItem.ToolTipViewAlways;
            }
        }
        public override Brush ForeColor
        {
            get { return ViewItem.ForeColor; }
        }
        public override Brush BackColor
        {
            get { return ViewItem.BackColor; }
        }
        public override Brush BorderBrush
        {
            get { return ViewItem.BorderBrush; }
        }
    }
}
