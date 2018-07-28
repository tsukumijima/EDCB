using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace EpgTimer
{
    static class ChSet5
    {
        private static List<ChSet5Item> chListOrderByIndex = null;
        private static Dictionary<UInt64, ChSet5Item> chList = null;
        public static Dictionary<UInt64, ChSet5Item> ChList
        {
            get
            {
                if (chList == null) LoadFile();
                return chList ?? new Dictionary<UInt64, ChSet5Item>();
            }
        }
        public static void Clear() { chList = null; chListOrderByIndex = null; }

        public static IEnumerable<ChSet5Item> ChListSelected
        {
            get { return GetSortedChList(Settings.Instance.ShowEpgCapServiceOnly == false); }
        }
        public static IEnumerable<ChSet5Item> ChListSorted
        {
            get { return GetSortedChList(); }
        }
        private static IEnumerable<ChSet5Item> GetSortedChList(bool ignoreEpgCap = true)
        {
            if (chListOrderByIndex == null && LoadFile() == false) return new List<ChSet5Item>();
            IEnumerable<ChSet5Item> ret = chListOrderByIndex.Where(item => ignoreEpgCap || item.EpgCapFlag);
            if (Settings.Instance.SortServiceList == false) return ret;

            //ネットワーク種別優先かつ限定受信を分離したID順ソート。可能なら地上波はリモコンID優先にする。
            return ret.OrderBy(item => (
                (ulong)(item.IsDttv ? 0 : item.IsBS ? 1 : item.IsCS ? 2 : item.IsSPHD ? 3 : 4) << 60 |
                (ulong)(item.IsDttv ? (item.PartialFlag ? 1 : 0) : item.IsOther ? item.ONID : 0) << 32 |
                (ulong)(item.IsDttv ? (item.RemoconID() + 255) % 256 : item.BSQuickCh()) << 16 |
                (ulong)(item.IsDttv ? 0xFFFF : 0x03FF) & item.SID));
        }
        private static int BSQuickCh(this ChSet5Item item)
        {
            //BSの連動放送のチャンネルをくくる
            //スターチャンネルをまとめて、かつ放送大学ラジオもまとめつつ230番台を順に並べるのは難しい。
            if (item.IsBS == false) return 0;
            int ch = item.SID / 10;
            int offset = ch >= 40 && ch < 60 ? 30 : ch >= 70 && ch < 90 ? 60 : 0;
            return ch - offset;
        }

        public static void SetRemoconID(Dictionary<ulong, EpgServiceAllEventInfo> infoList)
        {
            Settings.Instance.RemoconIDList.Clear();
            foreach (EpgServiceInfo info in infoList.Select(info => info.Value.serviceInfo)
                                                    .Where(info => info.remote_control_key_id != 0))
            {
                Settings.Instance.RemoconIDList[info.TSID] = info.remote_control_key_id;
            }
            Settings.SaveToXmlFile(false);
        }
        public static byte RemoconID(this ChSet5Item item)
        {
            byte ret = 0;
            if (item.IsDttv) Settings.Instance.RemoconIDList.TryGetValue(item.TSID, out ret);
            return ret;
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

        private static Encoding fileEncoding = Encoding.GetEncoding(932);
        public static bool LoadFile()
        {
            try
            {
                using (var sr = new StreamReader(SettingPath.SettingFolderPath + "\\ChSet5.txt", Encoding.GetEncoding(932)))
                {
                    sr.Peek();//CurrentEncodingが更新される
                    fileEncoding = sr.CurrentEncoding;
                    return ChSet5.Load(sr);
                }
            }
            catch { }
            return false;
        }
        public static bool Load(StreamReader reader)
        {
            try
            {
                chList = new Dictionary<UInt64, ChSet5Item>();
                chListOrderByIndex = new List<ChSet5Item>();
                for (string buff = reader.ReadLine(); buff != null; buff = reader.ReadLine())
                {
                    if (buff.StartsWith(";", StringComparison.Ordinal))
                    {
                        //コメント行
                    }
                    else
                    {
                        string[] list = buff.Split('\t');
                        var item = new ChSet5Item();
                        try
                        {
                            item.ServiceName = list[0];
                            item.NetworkName = list[1];
                            item.ONID = Convert.ToUInt16(list[2]);
                            item.TSID = Convert.ToUInt16(list[3]);
                            item.SID = Convert.ToUInt16(list[4]);
                            item.ServiceType = Convert.ToUInt16(list[5]);
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
                using (var writer = new StreamWriter(SettingPath.SettingFolderPath + "\\ChSet5.txt", false, fileEncoding))
                {
                    foreach (ChSet5Item info in chListOrderByIndex)
                    {
                        writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                            info.ServiceName,
                            info.NetworkName,
                            info.ONID,
                            info.TSID,
                            info.SID,
                            info.ServiceType,
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

    public class ChSet5Item
    {
        public ChSet5Item() { }

        public UInt64 Key { get { return CommonManager.Create64Key(ONID, TSID, SID); } }
        public UInt16 ONID { get; set; }
        public UInt16 TSID { get; set; }
        public UInt16 SID { get; set; }
        public UInt16 ServiceType { get; set; }
        public bool PartialFlag { get; set; }
        public String ServiceName { get; set; }
        public String NetworkName { get; set; }
        public bool EpgCapFlag { get; set; }
        public bool SearchFlag { get; set; }

        public bool IsVideo { get { return ChSet5.IsVideo(ServiceType); } }
        public bool IsDttv { get { return ChSet5.IsDttv(ONID); } }
        public bool IsBS { get { return ChSet5.IsBS(ONID); } }
        public bool IsCS { get { return ChSet5.IsCS(ONID); } }
        public bool IsSPHD { get { return ChSet5.IsSPHD(ONID); } }
        public bool IsOther { get { return ChSet5.IsOther(ONID); } }

        public EpgServiceInfo ToInfo()
        {
            return new EpgServiceInfo
            {
                ONID = this.ONID,
                TSID = this.TSID,
                SID = this.SID,
                network_name = this.NetworkName,
                partialReceptionFlag = (byte)(this.PartialFlag ? 1 : 0),
                remote_control_key_id = this.RemoconID(),
                service_name = this.ServiceName,
                service_provider_name = this.NetworkName,
                service_type = (byte)this.ServiceType,
                ts_name = this.NetworkName
            };
        }
        public override string ToString()
        {
            return ServiceName;
        }
    }
}
