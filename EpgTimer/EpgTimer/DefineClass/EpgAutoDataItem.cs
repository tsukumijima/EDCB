using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace EpgTimer
{
    //キーワード予約とプログラム自動登録の共通項目
    public class AutoAddDataItem : RecSettingItem
    {
        public AutoAddData Data { get; protected set; }

        public AutoAddDataItem() {}
        public AutoAddDataItem(AutoAddData data) { Data = data; }

        public override ulong KeyID { get { return Data.DataID; } }
        public override object DataObj { get { return Data; } }
        public override RecSettingData RecSettingInfo { get { return Data.RecSettingInfo; } }

        public string EventName
        {
            get { return Data.DataTitle; }
        }
        public uint SearchCount
        {
            get { return Data.SearchCount; }
        }
        public uint ReserveCount
        {
            get { return Data.ReserveCount; }
        }
        //"ReserveCount"のうち、有効な予約アイテム数
        public uint OnCount
        {
            get { return Data.OnCount; }
        }
        //"ReserveCount"のうち、無効な予約アイテム数
        public uint OffCount
        {
            get { return Data.OffCount; }
        }
        public string NextReserveName
        {
            get { return new ReserveItem(Data.GetNextReserve()).EventName; }
        }
        public string NextReserveNameValue
        {
            get { return new ReserveItem(Data.GetNextReserve()).EventNameValue; }
        }
        public string NextReserve
        {
            get { return new ReserveItem(Data.GetNextReserve()).StartTime; }
        }
        public long NextReserveValue
        {
            get
            {
                if (Data.GetNextReserve() == null) return long.MaxValue;
                //
                return new ReserveItem(Data.GetNextReserve()).StartTimeValue;
            }
        }
        public string NextReserveDuration
        {
            get { return new ReserveItem(Data.GetNextReserve()).Duration; }
        }
        public long NextReserveDurationValue
        {
            get
            {
                if (Data.GetNextReserve() == null) return long.MaxValue;
                //
                return new ReserveItem(Data.GetNextReserve()).DurationValue;
            }
        }
        public virtual string NetworkName { get { return ""; } }
        public virtual string ServiceName { get { return ""; } }
        public virtual bool KeyEnabled
        {
            set { EpgCmds.ChgOnOffCheck.Execute(this, null); }
            get { return Data.IsEnabled; }
        }
        public new string RecMode
        {
            get { return RecEnabled + "/" + base.RecMode; }
        }
        public override string ConvertInfoText(object param = null) { return ""; }
        public override Brush ForeColor
        {
            get
            {
                //番組表へジャンプ時の強調表示
                if (NowJumpingTable != 0 || Data.IsEnabled == true) return base.ForeColor;
                //
                //無効の場合
                return Settings.BrushCache.RecModeForeColor[5];
            }
        }
        public override Brush BackColor
        {
            get { return NowJumpingTable != 0 ? base.BackColor : BackColorBrush(); }
        }
        public override Brush BackColor2
        {
            get { return BackColorBrush(true); }
        }
        private Brush BackColorBrush(bool defTransParent = false)
        {
            int idx = Data.IsEnabled == false ? 1 : defTransParent ? -1 : 0;
            return idx < 0 ? null : Settings.BrushCache.ResBackColor[idx];
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

        public string NotKey
        {
            get { return EpgAutoAddInfo.searchInfo.notKey; }
        }
        public string RegExp
        {
            get { return EpgAutoAddInfo.searchInfo.regExpFlag == 1 ? "○" : "×"; }
        }
        public string Aimai
        {
            get { return EpgAutoAddInfo.searchInfo.aimaiFlag == 1 ? "○" : "×"; }
        }
        public string TitleOnly
        {
            get { return EpgAutoAddInfo.searchInfo.titleOnlyFlag == 1 ? "○" : "×"; }
        }
        public string CaseSensitive
        {
            get { return EpgAutoAddInfo.searchInfo.caseFlag == 1 ? "はい" : "いいえ"; }
        }
        public string DateKey
        {
            get
            {
                switch (EpgAutoAddInfo.searchInfo.dateList.Count)
                {
                    case 0: return "なし";
                    case 1: return CommonManager.ConvertTimeText(EpgAutoAddInfo.searchInfo.dateList[0]);
                    default: return "複数指定";
                }
            }
        }
        public string AddCount
        {
            get { return EpgAutoAddInfo.addCount.ToString(); }
        }
        public string JyanruKey
        {
            get { return CommonManager.ConvertJyanruText(EpgAutoAddInfo.searchInfo); }
        }
        /// <summary>
        /// 地デジ、BS、CS
        /// </summary>
        public override string NetworkName
        {
            get
            {
                return EpgAutoAddInfo.searchInfo.serviceList.Count == 0 ? "なし": 
                    string.Join(",", EpgAutoAddInfo.searchInfo.serviceList
                        .Select(service1 => CommonManager.ConvertNetworkNameText((ushort)(service1 >> 32), true))
                        .Distinct());
            }
        }
        /// <summary>
        /// NHK総合１・東京、NHKBS1
        /// </summary>
        public override string ServiceName
        {
            get { return _ServiceName(2); }
        }
        private string _ServiceName(int count = -1, bool withNetwork = false)
        {
            string view = "";
            int countAll = EpgAutoAddInfo.searchInfo.serviceList.Count;
            foreach (ulong service1 in EpgAutoAddInfo.searchInfo.serviceList.Take(count == -1 ? countAll : count))
            {
                if (view != "") { view += ", "; }
                EpgServiceInfo EpgServiceInfo1;
                if (ChSet5.ChList.TryGetValue(service1, out EpgServiceInfo1) == true)
                {
                    view += EpgServiceInfo1.service_name + (withNetwork == true ? "(" + CommonManager.ConvertNetworkNameText(EpgServiceInfo1.ONID) + ")" : "");
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
        public override string ConvertInfoText(object param = null)
        {
            string view = "【検索条件】\r\n";
            view += "Andキーワード : " + EventName + "\r\n";
            view += "Notキーワード : " + NotKey + "\r\n";
            view += "正規表現モード : " + RegExp + "\r\n";
            view += "あいまい検索モード : " + Aimai + "\r\n";
            view += "番組名のみ検索対象 : " + TitleOnly + "\r\n";
            view += "大小文字区別 : " + CaseSensitive + "\r\n";
            view += "自動登録 : " + CommonManager.ConvertIsEnableText(KeyEnabled) + "\r\n";
            view += "ジャンル絞り込み : " + JyanruKey + "\r\n";
            view += "時間絞り込み : " + DateKey + "\r\n";
            view += "検索対象サービス : " + _ServiceName(10, true) + "\r\n\r\n";

            view += "【録画設定】\r\n" + ConvertRecSettingText() + "\r\n\r\n";

            view += "キーワード予約ID : " + string.Format("{0} (0x{0:X})", DisplayID);
            return view;
        }
        public override Brush BorderBrush
        {
            get { return Settings.Instance.ListRuledLineContent ? BorderBrushLeft : base.BorderBrush; }
        }
        public override Brush BorderBrushLeft
        {
            get
            {
                if (EpgAutoAddInfo.searchInfo.contentList.Count == 0 || EpgAutoAddInfo.searchInfo.notContetFlag != 0)
                {
                    return Brushes.Gainsboro;
                }
                return ViewUtil.EpgDataContentBrush(EpgAutoAddInfo.searchInfo.contentList);
            }
        }
    }
}
