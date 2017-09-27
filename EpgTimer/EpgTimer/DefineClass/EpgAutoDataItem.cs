using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows;

namespace EpgTimer
{
    //キーワード予約とプログラム自動登録の共通項目
    public class AutoAddDataItem : RecSettingItem
    {
        public AutoAddData Data { get; protected set; }

        public AutoAddDataItem() {}
        public AutoAddDataItem(AutoAddData data) { Data = data; }

        public override ulong KeyID { get { return Data == null ? 0 : Data.DataID; } }
        public override object DataObj { get { return Data; } }
        public override RecSettingData RecSettingInfo { get { return Data != null ? Data.RecSettingInfo : null; } }

        public String EventName
        {
            get
            {
                if (Data == null) return "";
                //
                return Data.DataTitle;
            }
        }
        public String SearchCount
        {
            get
            {
                if (Data == null) return "";
                //
                return Data.SearchCount.ToString();
            }
        }
        public String ReserveCount
        {
            get
            {
                if (Data == null) return "";
                //
                return Data.ReserveCount.ToString();
            }
        }
        //"ReserveCount"のうち、有効な予約アイテム数
        public String OnCount
        {
            get
            {
                if (Data == null) return "";
                //
                return Data.OnCount.ToString();
            }
        }
        //"ReserveCount"のうち、無効な予約アイテム数
        public String OffCount
        {
            get
            {
                if (Data == null) return "";
                //
                return Data.OffCount.ToString();
            }
        }
        public String NextReserveName
        {
            get
            {
                if (Data == null) return "";
                //
                return new ReserveItem(Data.GetNextReserve()).EventName;
            }
        }
        public String NextReserveNameValue
        {
            get
            {
                if (Data == null) return "";
                //
                return new ReserveItem(Data.GetNextReserve()).EventNameValue;
            }
        }
        public String NextReserve
        {
            get
            {
                if (Data == null) return "";
                //
                return new ReserveItem(Data.GetNextReserve()).StartTime;
            }
        }
        public long NextReserveValue
        {
            get
            {
                if (Data == null) return long.MinValue;
                if (Data.GetNextReserve() == null) return long.MaxValue;
                //
                return new ReserveItem(Data.GetNextReserve()).StartTimeValue;
            }
        }
        public virtual String NetworkName { get { return ""; } }
        public virtual String ServiceName { get { return ""; } }
        public virtual bool KeyEnabled
        {
            set
            {
                EpgCmds.ChgOnOffCheck.Execute(this, null);
            }
            get
            {
                if (Data == null) return false;
                //
                return Data.IsEnabled;
            }
        }
        public override String ConvertInfoText(object param = null) { return ""; }
        public override Brush ForeColor
        {
            get
            {
                //番組表へジャンプ時の強調表示
                if (NowJumpingTable != 0 || Data == null || Data.IsEnabled == true) return base.ForeColor;
                //
                //無効の場合
                return CommonManager.Instance.RecModeForeColor[5];
            }
        }
        public override Brush BackColor
        {
            get
            {
                //番組表へジャンプ時の強調表示
                if (NowJumpingTable != 0 || Data == null || Data.IsEnabled == true) return base.BackColor;
                //
                //無効の場合
                return CommonManager.Instance.ResBackColor[1];
            }
        }
    }

    //T型との関連付け
    public class AutoAddDataItemT<T> : AutoAddDataItem where T : AutoAddData
    {
        public AutoAddDataItemT() { }
        public AutoAddDataItemT(T item) : base(item) { }
    }

    public static class AutoAddDataItemEx
    {
        public static AutoAddDataItem CreateIncetance(AutoAddData data)
        {
            if (data is EpgAutoAddData)
            {
                return new EpgAutoDataItem(data as EpgAutoAddData);
            }
            else if (data is ManualAutoAddData)
            {
                return new ManualAutoAddDataItem(data as ManualAutoAddData);
            }
            else
            {
                return new AutoAddDataItem(data);
            }
        }

        public static List<T> AutoAddInfoList<T>(this IEnumerable<AutoAddDataItemT<T>> itemlist) where T : AutoAddData
        {
            return itemlist.Where(item => item != null).Select(item => (T)item.Data).ToList();
        }
    }

    public class EpgAutoDataItem : AutoAddDataItemT<EpgAutoAddData>
    {
        public EpgAutoDataItem() { }
        public EpgAutoDataItem(EpgAutoAddData item) : base(item) { }

        public EpgAutoAddData EpgAutoAddInfo { get { return (EpgAutoAddData)Data; } set { Data = value; } }

        public String NotKey
        {
            get
            {
                if (EpgAutoAddInfo == null) return "";
                //
                return EpgAutoAddInfo.searchInfo.notKey;
            }
        }
        public String RegExp
        {
            get
            {
                if (EpgAutoAddInfo == null) return "";
                //
                return EpgAutoAddInfo.searchInfo.regExpFlag == 1 ? "○" : "×";
            }
        }
        public String TitleOnly
        {
            get
            {
                if (EpgAutoAddInfo == null) return "";
                //
                return EpgAutoAddInfo.searchInfo.titleOnlyFlag == 1 ? "○" : "×";
            }
        }
        public String DateKey
        {
            get
            {
                if (EpgAutoAddInfo == null) return "";
                //
                switch (EpgAutoAddInfo.searchInfo.dateList.Count)
                {
                    case 0: return "なし";
                    case 1: return CommonManager.ConvertTimeText(EpgAutoAddInfo.searchInfo.dateList[0]);
                    default: return "複数指定";
                }
            }
        }
        public String AddCount
        {
            get
            {
                if (EpgAutoAddInfo == null) return "";
                //
                return EpgAutoAddInfo.addCount.ToString();
            }
        }
        public String JyanruKey
        {
            get
            {
                if (EpgAutoAddInfo == null || EpgAutoAddInfo.searchInfo == null) return "";
                //
                return CommonManager.ConvertJyanruText(EpgAutoAddInfo.searchInfo);
            }
        }
        /// <summary>
        /// 地デジ、BS、CS
        /// </summary>
        public override String NetworkName
        {
            get
            {
                if (EpgAutoAddInfo == null) return "";
                //
                return this.EpgAutoAddInfo.searchInfo.serviceList.Count == 0 ? "なし": 
                    string.Join(",", this.EpgAutoAddInfo.searchInfo.serviceList
                        .Select(service1 => CommonManager.ConvertNetworkNameText((ushort)(service1 >> 32), true))
                        .Distinct());
            }
        }
        /// <summary>
        /// NHK総合１・東京、NHKBS1
        /// </summary>
        public override String ServiceName
        {
            get { return _ServiceName(2); }
        }
        private String _ServiceName(int count = -1, bool withNetwork = false)
        {
            if (EpgAutoAddInfo == null) return "";
            //
            String view = "";
            int countAll = EpgAutoAddInfo.searchInfo.serviceList.Count;
            foreach (ulong service1 in EpgAutoAddInfo.searchInfo.serviceList.Take(count == -1 ? countAll : count))
            {
                if (view != "") { view += ", "; }
                ChSet5Item chSet5Item1;
                if (ChSet5.ChList.TryGetValue(service1, out chSet5Item1) == true)
                {
                    view += chSet5Item1.ServiceName + (withNetwork == true ? "(" + CommonManager.ConvertNetworkNameText(chSet5Item1.ONID) + ")" : "");
                }
                else
                {
                    view += "?" + (withNetwork == true ? "(?)" : "");
                }
            }
            if (count != -1 && count < countAll)
            {
                view += (view == "" ? "" : ", ") + "他" + (countAll - count);
            }
            if (view == "")
            {
                view = "なし";
            }
            return view;
        }
        public override String ConvertInfoText(object param = null)
        {
            if (EpgAutoAddInfo == null) return "";
            //
            String view = "";
            view += "Andキーワード : " + EventName + "\r\n";
            view += "Notキーワード : " + NotKey + "\r\n";
            view += "正規表現モード : " + RegExp + "\r\n";
            view += "番組名のみ検索対象 : " + TitleOnly + "\r\n";
            view += "自動登録 : " + (KeyEnabled == true ? "有効" : "無効") + "\r\n";
            view += "ジャンル絞り込み : " + JyanruKey + "\r\n";
            view += "時間絞り込み : " + DateKey + "\r\n";
            view += "検索対象サービス : " + _ServiceName(10, true);

            view += "\r\n\r\n" + ConvertRecSettingText();
            return view;
        }
        public override Brush BorderBrush
        {
            get
            {
                if (EpgAutoAddInfo == null) return null;
                //
                if (EpgAutoAddInfo.searchInfo.contentList.Count == 0 || EpgAutoAddInfo.searchInfo.notContetFlag != 0)
                {
                    return Brushes.Gainsboro;
                }
                return ViewUtil.EpgDataContentBrush(EpgAutoAddInfo.searchInfo.contentList);
            }
        }

    }

}
