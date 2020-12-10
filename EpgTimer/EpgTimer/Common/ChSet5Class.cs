using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EpgTimer
{
    static class ChSet5
    {
        private static List<EpgServiceInfo> chListOrderByIndex = null;
        private static Dictionary<UInt64, EpgServiceInfo> chList = null;
        public static Dictionary<UInt64, EpgServiceInfo> ChList
        {
            get
            {
                if (chList == null) LoadFile();
                return chList ?? new Dictionary<UInt64, EpgServiceInfo>();
            }
        }
        public static void Clear() { chList = null; chListOrderByIndex = null; bsmin = null; }

        public static EpgServiceInfo ChItem(UInt64 key, bool noNullReturn = false, bool TryIgnoreTSID = false)
        {
            return ChItemMask(key, noNullReturn, (UInt64)(TryIgnoreTSID == false ? 0 : 0x0000FFFF0000UL));
        }
        public static EpgServiceInfo ChItemMask(UInt64 key, bool noNullReturn, UInt64 orMask)
        {
            EpgServiceInfo item = null;
            if ((chListOrderByIndex != null || LoadFile() != false) &&
                    ChList.TryGetValue(key, out item) == false && orMask != 0)
            {
                item = chListOrderByIndex.FirstOrDefault(ch => (ch.Key | orMask) == (key | orMask));
            }
            return item ?? (noNullReturn ? new EpgServiceInfo { Key = key } : null);
        }

        public static IEnumerable<EpgServiceInfo> ChListSelected
        {
            get { return GetSortedChList(Settings.Instance.ShowEpgCapServiceOnly == false); }
        }
        public static IEnumerable<EpgServiceInfo> ChListSorted
        {
            get { return GetSortedChList(); }
        }
        private static IEnumerable<EpgServiceInfo> GetSortedChList(bool ignoreEpgCap = true)
        {
            if (chListOrderByIndex == null && LoadFile() == false) return new List<EpgServiceInfo>();
            return GetSortedChList(chListOrderByIndex, ignoreEpgCap);
        }
        public static IEnumerable<EpgServiceInfo> GetSortedChList(IEnumerable<EpgServiceInfo> list, bool ignoreEpgCap = true, bool forceSort = false)
        {
            if (list == null) return new List<EpgServiceInfo>();
            list = list.Where(item => ignoreEpgCap || item.EpgCapFlag);
            if (Settings.Instance.SortServiceList == false && forceSort == false) return list;

            //ネットワーク種別優先かつ限定受信を分離したID順ソート。可能なら地上波はリモコンID優先にする。
            return list.OrderBy(item => (
                (ulong)(item.IsDttv ? 0 : item.IsBS ? 1 : item.IsCS ? 2 : item.IsSPHD ? 3 : 4) << 60 |
                (ulong)(item.IsDttv ? (item.PartialFlag ? 1 : 0) : item.IsOther ? item.ONID : 0) << 32 |
                (ulong)(item.IsDttv ? (item.RemoconID() + 255) % 256 : item.BSQuickCh()) << 16 |
                (ulong)(item.IsDttv ? 0xFFFF : 0x03FF) & item.SID));
        }
        private static Dictionary<ushort, ushort> bsmin = null;
        private static int BSQuickCh(this EpgServiceInfo item)
        {
            //BSの連動放送のチャンネルをくくる
            if (item.IsBS == false) return 0;
            if (bsmin == null)
            {
                bsmin = (chListOrderByIndex ?? new List<EpgServiceInfo>()).GroupBy(d => d.TSID, d => d.SID)
                    .ToDictionary(d => d.Key, d => d.Min());
            }
            ushort ret = 0;
            bsmin.TryGetValue(item.TSID, out ret);
            return ret;
        }

        public static void SetRemoconID(IEnumerable<EpgServiceInfo> infoList, bool addOnly = false)
        {
            if (addOnly == false) Settings.Instance.RemoconIDList.Clear();
            foreach (EpgServiceInfo info in infoList.Where(info => info.remote_control_key_id != 0))
            {
                //登録済みを更新しない(過去データで上書きしない)
                if (Settings.Instance.RemoconIDList.ContainsKey(info.TSID) == false)
                {
                    Settings.Instance.RemoconIDList.Add(info.TSID, info.remote_control_key_id);
                }
            }
            if (addOnly == false) Settings.SaveToXmlFile(false);
        }
        public static byte RemoconID(this EpgServiceInfo item)
        {
            byte ret = 0;
            if (item.IsDttv) Settings.Instance.RemoconIDList.TryGetValue(item.TSID, out ret);
            return ret;
        }
        public static int ChNumber(this EpgServiceInfo item)
        {
            return item.IsDttv ? item.RemoconID() : item.SID & 0x3FF;
        }
        public static int ChNumber(ulong key)
        {
            return new EpgServiceInfo { Key = key }.ChNumber();
        }

        public static bool IsVideo(UInt16 ServiceType)
        {
            return ServiceType == 0x01 || ServiceType == 0xA5 || ServiceType == 0xAD;
        }
        public static bool IsDttv(UInt16 ONID)
        {
            return 0x7880 <= ONID && ONID <= 0x7FE8;
        }
        public static bool IsBS(UInt16 ONID)
        {
            return ONID == 0x0004;
        }
        public static bool IsCS(UInt16 ONID)
        {
            return IsCS1(ONID) || IsCS2(ONID);
        }
        public static bool IsCS1(UInt16 ONID)
        {
            return ONID == 0x0006;
        }
        public static bool IsCS2(UInt16 ONID)
        {
            return ONID == 0x0007;
        }
        public static bool IsSP(UInt16 ONID)//iEPG用
        {
            return IsSPHD(ONID) || ONID == 0x0001 || ONID == 0x0003;
        }
        public static bool IsSPHD(UInt16 ONID)
        {
            return ONID == 0x000A;
        }
        public static bool IsOther(UInt16 ONID)
        {
            return IsDttv(ONID) == false && IsBS(ONID) == false && IsCS(ONID) == false && IsSPHD(ONID) == false;
        }

        public static bool LoadFile()
        {
            try
            {
                using (var fs = new FileStream(SettingPath.SettingFolderPath + "\\ChSet5.txt", FileMode.Open, FileAccess.Read))
                {
                    return LoadWithStreamReader(fs);
                }
            }
            catch { }
            return false;
        }
        private static Encoding enc = Encoding.UTF8;
        public static bool LoadWithStreamReader(Stream stream)
        {
            try
            {
                enc = Encoding.GetEncoding(932);
            }
            catch
            {
                enc = Encoding.UTF8;
            }

            try
            {
                chList = new Dictionary<UInt64, EpgServiceInfo>();
                chListOrderByIndex = new List<EpgServiceInfo>();
                using (var reader = new System.IO.StreamReader(stream, enc))
                {
                    for (string buff = reader.ReadLine(); buff != null; buff = reader.ReadLine())
                    {
                        if (buff.StartsWith(";", StringComparison.Ordinal))
                        {
                            //コメント行
                            continue;
                        }
                        string[] list = buff.Split('\t');
                        var item = new EpgServiceInfo();
                        try
                        {
                            item.service_name = list[0];
                            item.network_name = list[1];
                            item.ONID = Convert.ToUInt16(list[2]);
                            item.TSID = Convert.ToUInt16(list[3]);
                            item.SID = Convert.ToUInt16(list[4]);
                            item.service_type = Convert.ToByte(list[5]);
                            item.PartialFlag = Convert.ToInt32(list[6]) != 0;
                            item.EpgCapFlag = Convert.ToInt32(list[7]) != 0;
                            item.SearchFlag = Convert.ToInt32(list[8]) != 0;
                        }
                        catch
                        {
                            //不正
                            continue;
                        }
                        if (chList.ContainsKey(item.Key) == false)
                        {
                            chList[item.Key] = item;
                            chListOrderByIndex.Add(item);
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
        public static bool SaveFile()
        {
            try
            {
                if (chListOrderByIndex == null) return false;
                //
                using (var writer = new StreamWriter(SettingPath.SettingFolderPath + "\\ChSet5.txt", false, enc))
                {
                    foreach (EpgServiceInfo info in chListOrderByIndex)
                    {
                        writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                            info.service_name,
                            info.network_name,
                            info.ONID,
                            info.TSID,
                            info.SID,
                            info.service_type,
                            info.PartialFlag == true ? 1 : 0,
                            info.EpgCapFlag == true ? 1 : 0,
                            info.SearchFlag == true ? 1 : 0);
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }

    public partial class EpgServiceInfo
    {
        public bool PartialFlag { get { return partialReceptionFlag == 1; } set { partialReceptionFlag = (byte)(value ? 1 : 0); } }
        public bool EpgCapFlag = false;
        public bool SearchFlag = false;

        public bool IsVideo { get { return ChSet5.IsVideo(service_type); } }
        public bool IsDttv { get { return ChSet5.IsDttv(ONID); } }
        public bool IsBS { get { return ChSet5.IsBS(ONID); } }
        public bool IsCS { get { return ChSet5.IsCS(ONID); } }
        public bool IsSP { get { return ChSet5.IsSP(ONID); } }
        public bool IsSPHD { get { return ChSet5.IsSPHD(ONID); } }
        public bool IsOther { get { return ChSet5.IsOther(ONID); } }
        public override string ToString() { return service_name; }

        public UInt64 Key
        {
            get { return HasSPKey ? SPKey : CommonManager.Create64Key(ONID, TSID, SID); }
            set { if (IsSPKey(value)) SPKey = value; else { ONID = (UInt16)(value >> 32); TSID = (UInt16)(value >> 16); SID = (UInt16)value; } }
        }

        public static EpgServiceInfo FromKey(UInt64 key)
        {
            if (IsSPKey(key)) return CreateSPInfo(key);

            EpgServiceInfo info = ChSet5.ChItem(key, true, true);
            if (info.Key != key)
            {
                //TSID移動前のチャンネルだった場合
                info.TSID = (ushort)(key >> 16);
            }
            else if (string.IsNullOrEmpty(info.service_name))
            {
                //ChSet5で全く見つからず、キーだけが入って戻ってきた場合
                info.network_name = CommonManager.ConvertNetworkNameText(info.ONID);
                //info.partialReceptionFlag = 0;不明
                info.remote_control_key_id = info.RemoconID();
                info.service_name = "[不明]";
                info.service_provider_name = info.network_name;
                //info.service_type = 0x01;不明
                info.ts_name = info.network_name;
            }
            return info;
        }

        public enum SpecialViewServices : ulong
        {
            ViewServiceDttv = 0x1000000000000,
            ViewServiceBS,
            ViewServiceCS,
            ViewServiceCS3,
            ViewServiceOther,
        }
        public static IEnumerable<UInt64> SPKeyList { get { return Enum.GetValues(typeof(EpgServiceInfo.SpecialViewServices)).Cast<ulong>(); } }
        public UInt64 SPKey = 0;
        public static bool IsSPKey(UInt64 key) { return key >= (UInt64)SpecialViewServices.ViewServiceDttv; }
        public bool HasSPKey { get { return IsSPKey(SPKey); } }

        public static EpgServiceInfo CreateSPInfo(UInt64 key)
        {
            var networks = new UInt16[] { 0x7880, 0x0004, 0x0006, 0x000A, 0x0000 };
            int idx = Math.Min((int)(key & 0xFFFFUL), networks.Length - 1);
            var ret = new EpgServiceInfo();
            ret.Key = key;
            ret.ONID = networks[idx];
            ret.network_name = CommonManager.ConvertNetworkNameText(ret.ONID, true);
            ret.service_name = string.Format("[{0}:全サービス]", ret.network_name);
            return ret;
        }
    }
}
