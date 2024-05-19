using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace EpgTimer
{
    static class ChSet5
    {
        public static event Action LogoChanged;

        private static DispatcherTimer loadLogoTimer;
        private static List<EpgServiceInfo> chListOrderByIndex = null;
        private static Dictionary<ulong, EpgServiceInfo> chList = null;
        public static Dictionary<ulong, EpgServiceInfo> ChList
        {
            get
            {
                if (chList == null) LoadFile();
                return chList ?? new Dictionary<ulong, EpgServiceInfo>();
            }
        }
        public static void Clear() { chList = null; chListOrderByIndex = null; bsmin = null; ClearLogo(); }

        public static EpgServiceInfo ChItem(ulong key, bool noNullReturn = false, bool TryIgnoreTSID = false)
        {
            return ChItemMask(key, noNullReturn, TryIgnoreTSID == false ? 0 : 0x0000FFFF0000UL);
        }
        public static EpgServiceInfo ChItemMask(ulong key, bool noNullReturn, ulong orMask)
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
            return bsmin.GetValue(item.TSID);
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
            return item.IsDttv ? Settings.Instance.RemoconIDList.GetValue(item.TSID) : (byte)0;
        }
        public static int ChNumber(this EpgServiceInfo item)
        {
            return item.IsDttv ? item.RemoconID() : item.SID & 0x3FF;
        }
        public static int ChNumber(ulong key)
        {
            return new EpgServiceInfo { Key = key }.ChNumber();
        }

        public static bool IsVideo(ushort ServiceType)
        {
            return ServiceType == 0x01 || ServiceType == 0xA5 || ServiceType == 0xAD;
        }
        public static bool IsDttv(ushort ONID)
        {
            return 0x7880 <= ONID && ONID <= 0x7FE8;
        }
        public static bool IsBS(ushort ONID)
        {
            return ONID == 0x0004;
        }
        public static bool IsCS(ushort ONID)
        {
            return IsCS1(ONID) || IsCS2(ONID);
        }
        public static bool IsCS1(ushort ONID)
        {
            return ONID == 0x0006;
        }
        public static bool IsCS2(ushort ONID)
        {
            return ONID == 0x0007;
        }
        public static bool IsSP(ushort ONID)//iEPG用
        {
            return IsSPHD(ONID) || ONID == 0x0001 || ONID == 0x0003;
        }
        public static bool IsSPHD(ushort ONID)
        {
            return ONID == 0x000A;
        }
        public static bool IsOther(ushort ONID)
        {
            return IsDttv(ONID) == false && IsBS(ONID) == false && IsCS(ONID) == false && IsSPHD(ONID) == false;
        }

        public static bool LoadFile()
        {
            bool ret = false;
            try
            {
                using (var fs = new FileStream(SettingPath.SettingFolderPath + "\\ChSet5.txt", FileMode.Open, FileAccess.Read))
                {
                    ret = LoadWithStreamReader(fs);
                }
                ReLoadLogo();
            }
            catch { }
            return ret;
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
                chList = new Dictionary<ulong, EpgServiceInfo>();
                chListOrderByIndex = new List<EpgServiceInfo>();
                using (var reader = new StreamReader(stream, enc))
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
                            //リモコンIDのフィールドは必ずしも存在しない
                            item.remote_control_key_id = list.Length < 10 ? (byte)0 : Convert.ToByte(list[9]);
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
                        writer.WriteLine(string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}",
                            info.service_name,
                            info.network_name,
                            info.ONID,
                            info.TSID,
                            info.SID,
                            info.service_type,
                            info.PartialFlag == true ? 1 : 0,
                            info.EpgCapFlag == true ? 1 : 0,
                            info.SearchFlag == true ? 1 : 0)
                            + (info.remote_control_key_id == 0 ? "" : "\t" + info.remote_control_key_id.ToString()));
                    }
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        //ロゴ関係
        public static Dictionary<ulong, BitmapSource> LogoList;

        public static void ReLoadLogo()
        {
            if (Settings.Instance.ShowLogo && LogoList == null)
            {
                if (loadLogoTimer == null)
                {
                    loadLogoTimer = new DispatcherTimer();
                    loadLogoTimer.Tick += (sender, e) =>
                    {
                        //2回目以降は間を開ける。
                        loadLogoTimer.Interval = TimeSpan.FromMilliseconds(500);
                        if (LoadLogo())
                        {
                            loadLogoTimer.Stop();
                        }
                    };
                }
                loadLogoTimer.Interval = TimeSpan.Zero;
                loadLogoTimer.Start();
            }
        }
        private static bool logoLoadCompleted = true;
        private static byte[] logoIniBinary;
        private static byte[] logoIndexBinary;
        private static bool logoIndexLoaded;
        private static Dictionary<uint, uint> chLogoIDs;
        private static Dictionary<uint, string> logoNames;
        private static int logoNamesLoadedCount;

        private static void ClearLogo()
        {
            LogoList = null;
            logoLoadCompleted = true;
            logoIniBinary = null;
            logoIndexBinary = null;
            logoIndexLoaded = false;
            chLogoIDs = null;
            logoNames = null;
            logoNamesLoadedCount = 0;
    }
    /// <summary>
    /// ロゴを取得してChSet5に格納する。完了すればtrueが返る
    /// </summary>
    public static bool LoadLogo()
        {
            if (logoLoadCompleted)
            {
                //取得開始
                if (logoIndexLoaded == false)
                {
                    logoIniBinary = null;
                    logoIndexBinary = null;
                    logoIndexLoaded = true;
                    try
                    {
                        var dataList = new List<FileData>();
                        if (Settings.Instance.ShowLogo &&
                            CommonManager.CreateSrvCtrl().SendFileCopy2(new List<string> { "LogoData.ini", "LogoData\\*.*" }, ref dataList) == ErrCode.CMD_SUCCESS)
                        {
                            logoIniBinary = dataList.Count < 1 ? null : dataList[0].Data;
                            logoIndexBinary = dataList.Count < 2 ? null : dataList[1].Data;
                        }
                    }
                    catch { }
                }
                logoLoadCompleted = false;
                LogoList = new Dictionary<ulong, BitmapSource>();
            }

            bool changed = false;
            if (logoIndexLoaded)
            {
                //インデックス情報が更新された
                logoIndexLoaded = false;
                /*foreach (EpgServiceInfo ch in ChList.Values)
                {
                    changed = changed || ch.Logo != null;
                    ch.Logo = null;
                }*/

                string logoIni = null;
                string logoIndex = null;
                if (logoIniBinary != null && logoIndexBinary != null)
                {
                    try
                    {
                        //サーバーの環境によりUTF-8かBOMつきUTF-16LE
                        logoIni = (logoIniBinary.Length > 2 && logoIniBinary[0] == 0xFF && logoIniBinary[1] == 0xFE ?
                                       Encoding.Unicode.GetString(logoIniBinary) :
                                       Encoding.UTF8.GetString(logoIniBinary)).TrimStart('\uFEFF');
                        logoIndex = (logoIndexBinary.Length > 2 && logoIndexBinary[0] == 0xFF && logoIndexBinary[1] == 0xFE ?
                                         Encoding.Unicode.GetString(logoIndexBinary) :
                                         Encoding.UTF8.GetString(logoIndexBinary)).TrimStart('\uFEFF');
                    }
                    catch { }
                }
                if (logoIni == null || logoIndex == null)
                {
                    //インデックス情報を取得できていない
                    if (LogoChanged != null && changed)
                    {
                        LogoChanged();
                    }
                    logoLoadCompleted = true;
                    return logoLoadCompleted;
                }

                //ChSet5のサービスとロゴ識別との対照表を作る
                string[] lines = logoIni.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Array.Sort(lines, StringComparer.OrdinalIgnoreCase); 
                chLogoIDs = new Dictionary<uint, uint>();
                foreach (EpgServiceInfo ch in ChList.Values)
                {
                    uint chID = (uint)ch.ONID << 16 | ch.SID;
                    string startKey = chID.ToString("X8") + "=";
                    int index = Array.BinarySearch(lines, startKey, StringComparer.OrdinalIgnoreCase);
                    index = index < 0 ? ~index : index;
                    if (index < lines.Length && lines[index].StartsWith(startKey, StringComparison.OrdinalIgnoreCase))
                    {
                        int logoID;
                        if (int.TryParse(lines[index].Substring(9), out logoID) && 0 <= logoID && logoID <= 0x1FF)
                        {
                            chLogoIDs[chID] = (uint)ch.ONID << 16 | (uint)logoID;
                        }
                    }
                }

                //インデックス情報からファイル名を抽出してソート
                lines = logoIndex.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string s = lines[i];
                    lines[i] = s.Count(c => c == ' ') < 3 ? "" : s.Substring(s.IndexOf(' ', s.IndexOf(' ', s.IndexOf(' ') + 1) + 1) + 1);
                }
                Array.Sort(lines, StringComparer.OrdinalIgnoreCase);

                //ロゴ識別とロゴファイル名との対照表を作る
                logoNames = new Dictionary<uint, string>();
                var logoTypes = new int[] { 5, 2, 4, 1, 3, 0 };
                foreach (uint onidLogoID in chLogoIDs.Values.Distinct())
                {
                    string startKey = (onidLogoID >> 16).ToString("X4") + "_" + (onidLogoID & 0x1FF).ToString("X3") + "_";
                    int index = Array.BinarySearch(lines, startKey, StringComparer.OrdinalIgnoreCase);
                    index = index < 0 ? ~index : index;
                    for (int logoTypeIndex = 0; logoTypeIndex < logoTypes.Length; logoTypeIndex++)
                    {
                        string endKey = "_0" + logoTypes[logoTypeIndex] + ".png";
                        for (int i = index; i < lines.Length && lines[i].StartsWith(startKey, StringComparison.OrdinalIgnoreCase); i++)
                        {
                            if (lines[i].EndsWith(endKey, StringComparison.OrdinalIgnoreCase))
                            {
                                logoNames[onidLogoID] = "LogoData\\" + lines[i];
                                logoTypeIndex = logoTypes.Length - 1;
                                break;
                            }
                        }
                    }
                }
                logoNamesLoadedCount = 0;
            }

            //サーバーの負荷を考慮して少しずつ取得する
            var copyNameList = logoNames.Values.Skip(logoNamesLoadedCount).Take(20).ToList();
            if (copyNameList.Count > 0)
            {
                logoNamesLoadedCount += copyNameList.Count;
                var bitmapList = new List<BitmapSource>();
                try
                {
                    var dataList = new List<FileData>();
                    if (CommonManager.CreateSrvCtrl().SendFileCopy2(copyNameList, ref dataList) == ErrCode.CMD_SUCCESS)
                    {
                        foreach (FileData data in dataList)
                        {
                            var decoder = new PngBitmapDecoder(new MemoryStream(data.Data),
                                                               BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                            bitmapList.Add(decoder.Frames[0]);
                            bitmapList.Last().Freeze();
                        }
                    }
                }
                catch { }

                foreach (EpgServiceInfo ch in ChList.Values)
                {
                    uint onidLogoID;
                    string name;
                    if (chLogoIDs.TryGetValue((uint)ch.ONID << 16 | ch.SID, out onidLogoID) &&
                        logoNames.TryGetValue(onidLogoID, out name))
                    {
                        int i = copyNameList.IndexOf(name);
                        if (0 <= i && i < bitmapList.Count())
                        {
                            LogoList[ch.Key] = bitmapList[i];
                            changed = true;
                        }
                    }
                }
            }

            if (LogoChanged != null && changed)
            {
                LogoChanged();
            }

            logoLoadCompleted = logoNamesLoadedCount == logoNames.Count;
            return logoLoadCompleted;
        }
    }

    public partial class EpgServiceInfo
    {
        public bool PartialFlag { get { return partialReceptionFlag == 1; } set { partialReceptionFlag = (byte)(value ? 1 : 0); } }
        public bool EpgCapFlag = false;
        public bool SearchFlag = false;
        public BitmapSource Logo { get { return ChSet5.LogoList.GetValue(Key); } }
        public bool IsVideo { get { return ChSet5.IsVideo(service_type); } }
        public bool IsDttv { get { return ChSet5.IsDttv(ONID); } }
        public bool IsBS { get { return ChSet5.IsBS(ONID); } }
        public bool IsCS { get { return ChSet5.IsCS(ONID); } }
        public bool IsSP { get { return ChSet5.IsSP(ONID); } }
        public bool IsSPHD { get { return ChSet5.IsSPHD(ONID); } }
        public bool IsOther { get { return ChSet5.IsOther(ONID); } }
        public override string ToString() { return service_name; }

        public ulong Key
        {
            get { return HasSPKey ? SPKey : CommonManager.Create64Key(ONID, TSID, SID); }
            set { if (IsSPKey(value)) SPKey = value; else { ONID = (ushort)(value >> 32); TSID = (ushort)(value >> 16); SID = (ushort)value; } }
        }

        public static EpgServiceInfo FromKey(ulong key)
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
        public static IEnumerable<ulong> SPKeyList { get { return Enum.GetValues(typeof(EpgServiceInfo.SpecialViewServices)).Cast<ulong>(); } }
        public ulong SPKey = 0;
        public static bool IsSPKey(ulong key) { return key >= (ulong)SpecialViewServices.ViewServiceDttv; }
        public bool HasSPKey { get { return IsSPKey(SPKey); } }

        public static EpgServiceInfo CreateSPInfo(ulong key)
        {
            var networks = new ushort[] { 0x7880, 0x0004, 0x0006, 0x000A, 0x0000 };
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
