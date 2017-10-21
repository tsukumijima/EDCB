﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

namespace EpgTimer
{
    class IniFileHandler
    {
        public static bool ReadOnly { get; set; }

        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        public static extern uint
          GetPrivateProfileString(string lpAppName,
          string lpKeyName, string lpDefault,
          StringBuilder lpReturnedString, uint nSize,
          string lpFileName);

        [DllImport("KERNEL32.DLL",
            EntryPoint = "GetPrivateProfileStringA")]
        public static extern uint
          GetPrivateProfileStringByByteArray(string lpAppName,
          string lpKeyName, string lpDefault,
          byte[] lpReturnedString, uint nSize,
          string lpFileName);
        
        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        public static extern int
          GetPrivateProfileInt(string lpAppName,
          string lpKeyName, int nDefault, string lpFileName);

        [DllImport("KERNEL32.DLL", CharSet = CharSet.Unicode)]
        private static extern uint WritePrivateProfileString(
          string lpAppName,
          string lpKeyName,
          string lpString,
          string lpFileName);

        /// <summary>書き込み値はbool型のみtrue:"1",false:"0"、他はvalue.ToString()。ただし空のstringはnull。</summary>
        public static uint WritePrivateProfileString(string lpAppName, string lpKeyName, object val, string lpFileName)
        {
            if (ReadOnly == true) return 0;
            string lpString = val == null ? null : !(val is bool) ? val.ToString() : (bool)val ? "1" : "0";
            return WritePrivateProfileString(lpAppName, lpKeyName, lpString == "" ? null : lpString, lpFileName);
        }
        /// <summary>デフォルト値の時キーを削除する。書き込み値はbool型のみtrue:"1",false:"0"、他はvalue.ToString()</summary>
        public static uint WritePrivateProfileString<T>(string lpAppName, string lpKeyName, T val, T defval, string lpFileName)
        {
            //引数をTからobjectにすると、valueがdoubleなどのときEqualsで失敗する
            return WritePrivateProfileString(lpAppName, lpKeyName, object.Equals(val, defval) == true ? null : (object)val, lpFileName);
        }

        public static string
          GetPrivateProfileString(string lpAppName,
          string lpKeyName, string lpDefault, string lpFileName)
        {
            StringBuilder buff = null;
            for (uint n = 512; n <= 1024 * 1024; n *= 2)
            {
                //セクション名取得などのNUL文字分割された結果は先頭要素のみ格納される
                buff = new StringBuilder((int)n);
                if (GetPrivateProfileString(lpAppName, lpKeyName, lpDefault, buff, n, lpFileName) < n - 2)
                {
                    break;
                }
            }
            return buff.ToString();
        }

        public static string GetPrivateProfileFolder(string lpAppName, string lpKeyName, string lpFileName)
        {
            var path = IniFileHandler.GetPrivateProfileString(lpAppName ?? "", lpKeyName ?? "", "", lpFileName ?? "");
            return SettingPath.CheckFolder(path);
        }

        /// <summary>"0"をfalse、それ以外をtrueで読み込む。</summary>
        public static bool GetPrivateProfileBool(string lpAppName, string lpKeyName, bool defval, string lpFileName)
        {
            return GetPrivateProfileString(lpAppName, lpKeyName, defval == false ? "0" : "1", lpFileName) != "0";
        }
        public static double GetPrivateProfileDouble(string lpAppName, string lpKeyName, double defval, string lpFileName)
        {
            string s = GetPrivateProfileString(lpAppName, lpKeyName, defval.ToString(), lpFileName);
            double.TryParse(s, out defval);
            return defval;
        }

        /// <summary>INIファイルから指定セクションのキー一覧を取得する。lpAppNameにnullを指定すると全セクションの一覧を取得する。</summary>
        public static string[] GetPrivateProfileKeys(string lpAppName, string lpFileName)
        {
            byte[] buff = null;
            uint resultSize = 0;
            for (uint n = 1024; n <= 1024 * 1024; n *= 2)
            {
                buff = new byte[n];
                resultSize = GetPrivateProfileStringByByteArray(lpAppName, null, null, buff, n, lpFileName);
                if (resultSize < n - 2)
                {
                    break;
                }
            }
            return Encoding.GetEncoding(932).GetString(buff, 0, (int)resultSize).TrimEnd('\0').Split('\0');
        }

        /// <summary>INIファイルから連番のキー/セクションを削除する。lpAppNameにnullを指定すると連番セクションを削除する。</summary>
        public static void DeletePrivateProfileNumberKeys(string lpAppName, string lpFileName, string BaseFront = "", string BaseRear = "", bool deleteBaseName = false)
        {
            if (ReadOnly == true) return;
            string numExp = string.IsNullOrEmpty(BaseFront + BaseRear) == false && deleteBaseName == true ? "*" : "+";
            foreach (string key in IniFileHandler.GetPrivateProfileKeys(lpAppName, lpFileName))
            {
                if (Regex.Match(key, "^" + BaseFront + "\\d" + numExp + BaseRear + "$").Success == true)
                {
                    if (lpAppName != null)
                    {
                        WritePrivateProfileString(lpAppName, key, null, lpFileName);
                    }
                    else
                    {
                        WritePrivateProfileString(key, null, null, lpFileName);
                    }
                }
            }
        }

        public static ErrCode UpdateSrvProfileIniNW(List<string> iniList = null)
        {
            var err = ErrCode.CMD_SUCCESS;
            if (CommonManager.Instance.NWMode == true)
            {
                if (CommonManager.Instance.NW.IsConnected == false) return err;
                err = ReloadSettingFilesNW(iniList);
            }

            ChSet5.Clear();
            Settings.Instance.ReloadOtherOptions();
            return err;
        }

        public static ErrCode ReloadSettingFilesNW(List<string> iniList = null)
        {
            if (iniList == null)
            {
                iniList = new List<string> {
                    "EpgTimerSrv.ini"
                    ,"Common.ini"
                    ,"EpgDataCap_Bon.ini"
                    ,"BonCtrl.ini"
                    ,"ViewApp.ini" 
                    //,"Bitrate.ini" //未使用
                    ,"ChSet5.txt"
                };
            }

            var datalist = new List<FileData>();
            ErrCode err = CommonManager.CreateSrvCtrl().SendFileCopy2(iniList, ref datalist);
            try
            {
                if (err == ErrCode.CMD_SUCCESS)
                {
                    Directory.CreateDirectory(SettingPath.SettingFolderPath);
                    foreach (var data in datalist)
                    {
                        try
                        {
                            string path = Path.Combine(SettingPath.SettingFolderPath, data.Name);
                            if (data.Size == 0)
                            {
                                File.Delete(path);
                                continue;
                            }
                            using (var w = new BinaryWriter(File.Create(path)))
                            {
                                w.Write(data.Data);
                            }
                        }
                        catch { err = ErrCode.CMD_ERR; }
                    }
                }
            }
            catch { err = ErrCode.CMD_ERR; }
            return err;
        }
    }

    class SettingPath
    {
        //Path.Combine()はドライブ区切り':'の扱いがアヤシイのでここでは使用しないことにする。
        //他のEpgTimerコード中で使っているところはModelePathに対してなので大丈夫。(ModelePathは'C:'のようなパスを返さない)
        public static string CheckFolder(string path)
        {
            path = (path ?? "").Trim(' ');
            if (path != "\\") path = path.TrimEnd('\\');
            return path + (path.EndsWith(Path.VolumeSeparatorChar.ToString()) == true ? "\\" : "");
        }
        public static string CheckTSExtension(string ext)
        {
            //5文字以下の英数字拡張子に限る
            if (ext.Length < 2 || ext.Length > 6 || ext[0] != '.'
                || ext.Substring(1).Any(c => !('0' <= c && c <= '9' || 'A' <= c && c <= 'Z' || 'a' <= c && c <= 'z')))
            { return ".ts"; }
            return ext;
        }
        private static string IniPath
        {
            get { return (CommonManager.Instance.NWMode == false ? ModulePath : SettingFolderPath); }
        }
        public static string BonCtrlIniPath
        {
            get { return IniPath.TrimEnd('\\') + "\\BonCtrl.ini"; }
        }
        public static string CommonIniPath
        {
            get { return IniPath.TrimEnd('\\') + "\\Common.ini"; }
        }
        public static string TimerSrvIniPath
        {
            get { return IniPath.TrimEnd('\\') + "\\EpgTimerSrv.ini"; }
        }
        public static string ViewAppIniPath
        {
            get { return IniPath.TrimEnd('\\') + "\\ViewApp.ini"; }
        }
        public static string DefHttpPublicPath
        {
            get { return GetModulePath(true).TrimEnd('\\') + "\\HttpPublic"; }
        }
        public static string DefEdcbExePath
        {
            get { return GetModulePath(true).TrimEnd('\\') + "\\EpgDataCap_Bon.exe"; }
        }
        public static string EdcbExePath
        {
            get { return IniFileHandler.GetPrivateProfileString("SET", "RecExePath", DefEdcbExePath, CommonIniPath); }
            set { IniFileHandler.WritePrivateProfileString("SET", "RecExePath", value == "" || string.Compare(value, DefEdcbExePath, true) == 0 ? null : value, SettingPath.CommonIniPath); }
        }
        public static string EdcbIniPath
        {
            get
            {
                if (CommonManager.Instance.NWMode == false)
                {
                    return EdcbExePath.TrimEnd("exe".ToArray()) + "ini";
                }
                else
                {
                    return IniPath.TrimEnd('\\') + "\\" + Path.GetFileNameWithoutExtension(EdcbExePath) + ".ini";
                }
            }
        }
        private static string GetDefSettingFolderPath(bool isSrv = false)
        {
            return GetModulePath(isSrv).TrimEnd('\\') + "\\Setting" + (isSrv == true || CommonManager.Instance.NWMode == false ? "" : "\\EpgTimerNW");
        }
        public static string GetSettingFolderPath(bool isSrv = false)
        {
            string path;
            if (isSrv == true || CommonManager.Instance.NWMode == false)
            {
                path = IniFileHandler.GetPrivateProfileFolder("SET", "DataSavePath", CommonIniPath);
            }
            else
            {
                path = CheckFolder(Settings.Instance.SettingFolderPathNW);
            }
            path = string.IsNullOrEmpty(path) == true ? GetDefSettingFolderPath(isSrv) : path;
            return (Path.IsPathRooted(path) ? "" : GetModulePath(isSrv).TrimEnd('\\') + "\\") + path;
        }
        public static string DefSettingFolderPath
        {
            get { return GetDefSettingFolderPath(); }
        }
        public static string SettingFolderPath
        {
            get { return GetSettingFolderPath(); }
            set
            {
                string path = CheckFolder(value);
                bool isDefaultPath = path == "" || string.Compare(path.TrimEnd('\\'), SettingPath.DefSettingFolderPath.TrimEnd('\\'), true) == 0;
                if (CommonManager.Instance.NWMode == false)
                {
                    IniFileHandler.WritePrivateProfileString("SET", "DataSavePath", isDefaultPath == true ? null : path, SettingPath.CommonIniPath);
                }
                else
                {
                    Settings.Instance.SettingFolderPathNW = isDefaultPath == true ? "" : path;
                }
            }
        }
        private static string GetModulePath(bool isSrv = false)
        {
            if (isSrv == true && CommonManager.Instance.NWMode == true)
            {
                return IniFileHandler.GetPrivateProfileString("SET", "ModulePath", "(サーバ側アプリフォルダ)", CommonIniPath);
            }
            else
            {
                return Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            }
        }
        public static string ModulePath
        {
            get { return GetModulePath(); }
        }
        public static string ModuleName
        {
            get { return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location); }
        }
    }

    public class Settings
    {
        private int verSaved = 0;
        public int SettingFileVer { get { return 20170717; } set { verSaved = value; } }

        public bool UseCustomEpgView { get; set; }
        public List<CustomEpgTabInfo> CustomEpgTabList { get; set; }
        public double MinHeight { get; set; }
        public double MinimumHeight { get; set; }
        public double ServiceWidth { get; set; }
        public double ScrollSize { get; set; }
        public string FontName { get; set; }
        public double FontSize { get; set; }
        public string FontNameTitle { get; set; }
        public double FontSizeTitle { get; set; }
        public bool FontBoldTitle { get; set; }
        public bool NoToolTip { get; set; }
        public double ToolTipWidth { get; set; }
        public bool NoBallonTips { get; set; }
        public int ForceHideBalloonTipSec { get; set; }
        public bool PlayDClick { get; set; }
        public bool ShowEpgCapServiceOnly { get; set; }
        public bool SortServiceList { get; set; }
        public bool ExitAfterProcessingArgs { get; set; }
        public bool RecinfoErrCriticalDrops { get; set; }
        public double DragScroll { get; set; }
        public List<string> ContentColorList { get; set; }
        public List<UInt32> ContentCustColorList { get; set; }
        public List<string> EpgResColorList { get; set; }
        public List<UInt32> EpgResCustColorList { get; set; }
        public List<string> EpgEtcColors { get; set; }
        public List<UInt32> EpgEtcCustColors { get; set; }
        public int ReserveRectFillOpacity { get; set; }
        public bool ReserveRectFillWithShadow { get; set; }
        public Int32 ReserveToolTipMode { get; set; }
        public Int32 ReserveEpgInfoOpenMode { get; set; }
        public string TitleColor1 { get; set; }
        public string TitleColor2 { get; set; }
        public UInt32 TitleCustColor1 { get; set; }
        public UInt32 TitleCustColor2 { get; set; }
        public string TunerFontNameService { get; set; }
        public double TunerFontSizeService { get; set; }
        public bool TunerFontBoldService { get; set; }
        public string TunerFontName { get; set; }
        public double TunerFontSize { get; set; }
        public List<string> TunerServiceColors { get; set; }
        public List<UInt32> TunerServiceCustColors { get; set; }
        public double TunerMinHeight { get; set; }
        public double TunerMinimumLine { get; set; }
        public double TunerDragScroll { get; set; }
        public double TunerScrollSize { get; set; }
        public bool TunerMouseScrollAuto { get; set; }
        public double TunerWidth { get; set; }
        public bool TunerServiceNoWrap { get; set; }
        public double TunerBorderLeftSize { get; set; }
        public double TunerBorderTopSize { get; set; }
        public bool TunerTitleIndent { get; set; }
        public bool TunerToolTip { get; set; }
        public int TunerToolTipViewWait { get; set; }
        public bool TunerPopup { get; set; }
        public int TunerPopupMode { get; set; }
        public bool TunerPopupRecinfo { get; set; }
        public double TunerPopupWidth { get; set; }
        public bool TunerChangeBorderWatch { get; set; }
        public bool TunerInfoSingleClick { get; set; }
        public bool TunerColorModeUse { get; set; }
        public bool TunerDisplayOffReserve { get; set; }
        public Int32 TunerToolTipMode { get; set; }
        public Int32 TunerEpgInfoOpenMode { get; set; }
        public double EpgBorderLeftSize { get; set; }
        public double EpgBorderTopSize { get; set; }
        public bool EpgTitleIndent { get; set; }
        public string EpgReplacePattern { get; set; }
        public string EpgReplacePatternTitle { get; set; }
        public bool EpgReplacePatternDef { get; set; }
        public bool EpgReplacePatternTitleDef { get; set; }
        public bool ApplyReplacePatternTuner { get; set; }
        public bool ShareEpgReplacePatternTitle { get; set; }
        public string FontReplacePatternEdit { get; set; }
        public double FontSizeReplacePattern { get; set; }
        public bool FontBoldReplacePattern { get; set; }
        public bool ReplacePatternEditFontShare { get; set; }
        public bool EpgToolTip { get; set; }
        public bool EpgToolTipNoViewOnly { get; set; }
        public int EpgToolTipViewWait { get; set; }
        public bool EpgPopup { get; set; }
        public int EpgPopupMode { get; set; }
        public double EpgPopupWidth { get; set; }
        public bool EpgExtInfoTable { get; set; }
        public bool EpgExtInfoPopup { get; set; }
        public bool EpgExtInfoTooltip { get; set; }
        public bool EpgGradation { get; set; }
        public bool EpgGradationHeader { get; set; }
        public bool EpgLoadArcInfo { get; set; }
        public bool EpgNoDisplayOld { get; set; }
        public double EpgNoDisplayOldDays { get; set; }
        public bool EpgChangeBorderWatch { get; set; }
        public bool EpgChangeBorderOnRec { get; set; }
        public bool EpgNameTabEnabled { get; set; }
        public bool EpgViewModeTabEnabled { get; set; }
        public bool EpgTabMoveCheckEnabled { get; set; }
        public string ResColumnHead { get; set; }
        public ListSortDirection ResSortDirection { get; set; }
        public WindowSettingData WndSettings { get; set; }
        public double SearchWndTabsHeight { get; set; }
        public bool CloseMin { get; set; }
        public bool WakeMin { get; set; }
        public bool ViewButtonShowAsTab { get; set; }
        public List<string> ViewButtonList { get; set; }
        public List<string> TaskMenuList { get; set; }
        public string Cust1BtnName { get; set; }
        public string Cust1BtnCmd { get; set; }
        public string Cust1BtnCmdOpt { get; set; }
        public string Cust2BtnName { get; set; }
        public string Cust2BtnCmd { get; set; }
        public string Cust2BtnCmdOpt { get; set; }
        public string Cust3BtnName { get; set; }
        public string Cust3BtnCmd { get; set; }
        public string Cust3BtnCmdOpt { get; set; }
        public List<string> AndKeyList { get; set; }
        public List<string> NotKeyList { get; set; }
        public EpgSearchKeyInfo DefSearchKey { get; set; }
        public bool UseLastSearchKey { get; set; }
        public List<SearchPresetItem> SearchPresetList { get; set; }
        public bool SetWithoutSearchKeyWord { get; set; }
        public Int32 RecInfoToolTipMode { get; set; }
        public string RecInfoColumnHead { get; set; }
        public ListSortDirection RecInfoSortDirection { get; set; }
        public long RecInfoDropErrIgnore { get; set; }
        public long RecInfoDropWrnIgnore { get; set; }
        public long RecInfoScrambleIgnore { get; set; }
        public List<string> RecInfoDropExcept { get; set; }
        public static readonly List<string> RecInfoDropExceptDefault = new List<string> { "EIT", "NIT", "CAT", "SDT/BAT", "SDTT", "TDT/TOT", "ECM", "EMM" };
        public bool RecInfoNoYear { get; set; }
        public bool RecInfoNoSecond { get; set; }
        public bool RecInfoNoDurSecond { get; set; }
        public bool ResInfoNoYear { get; set; }
        public bool ResInfoNoSecond { get; set; }
        public bool ResInfoNoDurSecond { get; set; }
        public string TvTestExe { get; set; }
        public string TvTestCmd { get; set; }
        public bool NwTvMode { get; set; }
        public bool NwTvModeUDP { get; set; }
        public bool NwTvModeTCP { get; set; }
        public string FilePlayExe { get; set; }
        public string FilePlayCmd { get; set; }
        public bool FilePlayOnAirWithExe { get; set; }
        public bool FilePlayOnNwWithExe { get; set; }
        public bool OpenFolderWithFileDialog { get; set; }
        public List<IEPGStationInfo> IEpgStationList { get; set; }
        public MenuSettingData MenuSet { get; set; }
        public string NWServerIP { get; set; }
        public UInt32 NWServerPort { get; set; }
        public UInt32 NWWaitPort { get; set; }
        public string NWMacAdd { get; set; }
        public List<NWPresetItem> NWPreset { get; set; }
        public bool WakeReconnectNW { get; set; }
        public bool WoLWaitRecconect { get; set; }
        public double WoLWaitSecond { get; set; }
        public bool SuspendCloseNW { get; set; }
        public bool NgAutoEpgLoadNW { get; set; }
        public bool ChkSrvRegistTCP { get; set; }
        public double ChkSrvRegistInterval { get; set; }
        public Int32 TvTestOpenWait { get; set; }
        public Int32 TvTestChgBonWait { get; set; }
        public List<string> RecEndColors { get; set; }
        public List<uint> RecEndCustColors { get; set; }
        public string ListDefColor { get; set; }
        public UInt32 ListDefCustColor { get; set; }
        public List<string> RecModeFontColors { get; set; }
        public List<uint> RecModeFontCustColors { get; set; }
        public List<string> ResBackColors { get; set; }
        public List<uint> ResBackCustColors { get; set; }
        public List<string> StatColors { get; set; }
        public List<uint> StatCustColors { get; set; }
        public bool EpgInfoSingleClick { get; set; }
        public Int32 EpgInfoOpenMode { get; set; }
        public UInt32 ExecBat { get; set; }
        public UInt32 SuspendChk { get; set; }
        public UInt32 SuspendChkTime { get; set; }
        public List<ListColumnInfo> ReserveListColumn { get; set; }
        public List<ListColumnInfo> RecInfoListColumn { get; set; }
        public List<ListColumnInfo> AutoAddEpgColumn { get; set; }
        public List<ListColumnInfo> AutoAddManualColumn { get; set; }
        public List<ListColumnInfo> EpgListColumn { get; set; }
        public string EpgListColumnHead { get; set; }
        public ListSortDirection EpgListSortDirection { get; set; }
        public List<ListColumnInfo> SearchWndColumn { get; set; }
        public string SearchColumnHead { get; set; }
        public ListSortDirection SearchSortDirection { get; set; }
        public Int32 SearchEpgInfoOpenMode { get; set; }
        public bool SaveSearchKeyword { get; set; }
        public List<ListColumnInfo> InfoSearchWndColumn { get; set; }
        public string InfoSearchColumnHead { get; set; }
        public ListSortDirection InfoSearchSortDirection { get; set; }
        public string InfoSearchLastWord { get; set; }
        public bool InfoSearchTitleOnly { get; set; }
        public bool InfoSearchReserveInfo { get; set; }
        public bool InfoSearchRecInfo { get; set; }
        public bool InfoSearchEpgAutoAddInfo { get; set; }
        public bool InfoSearchManualAutoAddInfo { get; set; }
        public bool InfoSearchItemTooltip { get; set; }
        public short AutoSaveNotifyLog { get; set; }
        public int NotifyLogMax { get; set; }
        public bool NotifyLogEpgTimer { get; set; }
        public bool ShowTray { get; set; }
        public bool MinHide { get; set; }
        public bool MouseScrollAuto { get; set; }
        public int NoStyle { get; set; }
        public int NoSendClose { get; set; }
        public bool CautionManyChange { get; set; }
        public int CautionManyNum { get; set; }
        public bool CautionOnRecChange { get; set; }
        public int CautionOnRecMarginMin { get; set; }
        public bool SyncResAutoAddChange { get; set; }
        public bool SyncResAutoAddChgNewRes { get; set; }
        public bool SyncResAutoAddDelete { get; set; }
        public bool DisplayNotifyEpgChange { get; set; }
        public double DisplayNotifyJumpTime { get; set; }
        public bool DisplayReserveAutoAddMissing { get; set; }
        public bool DisplayReserveMultiple { get; set; }
        public bool TryEpgSetting { get; set; }
        public bool LaterTimeUse { get; set; }
        public int LaterTimeHour { get; set; }
        public bool DisplayPresetOnSearch { get; set; }
        public bool ForceNWMode { get; set; }
        public string SettingFolderPathNW { get; set; }
        public bool RecInfoExtraDataCache { get; set; }
        public bool RecInfoExtraDataCacheOptimize { get; set; }
        public bool RecInfoExtraDataCacheKeepConnect { get; set; }
        public bool UpdateTaskText { get; set; }
        public bool DisplayStatus { get; set; }
        public bool DisplayStatusNotify { get; set; }
        public bool IsVisibleReserveView { get; set; }
        public bool IsVisibleRecInfoView { get; set; }
        public bool IsVisibleAutoAddView { get; set; }
        public bool IsVisibleAutoAddViewMoveOnly { get; set; }
        public Dock MainViewButtonsDock { get; set; }
        public CtxmCode StartTab { get; set; }
        public bool TrimSortTitle { get; set; }
        public bool KeepReserveWindow { get; set; }
        public PicUpTitle PicUpTitleWork { get; set; }
        [XmlIgnore]
        public bool WakeUpHdd 
        {
            get { return IniFileHandler.GetPrivateProfileInt("SET", "WakeUpHdd", 0, SettingPath.TimerSrvIniPath) == 1; }
            set { IniFileHandler.WritePrivateProfileString("SET", "WakeUpHdd", value, SettingPath.TimerSrvIniPath); }
        }
        [XmlIgnore]
        public Int32 NoWakeUpHddMin
        {
            get { return IniFileHandler.GetPrivateProfileInt("SET", "NoWakeUpHddMin", 30, SettingPath.TimerSrvIniPath); }
            set { IniFileHandler.WritePrivateProfileString("SET", "NoWakeUpHddMin", value, SettingPath.TimerSrvIniPath); }
        }
        [XmlIgnore]
        public Int32 WakeUpHddOverlapNum
        {
            get { return IniFileHandler.GetPrivateProfileInt("SET", "WakeUpHddOverlapNum", 0, SettingPath.TimerSrvIniPath); }
            set { IniFileHandler.WritePrivateProfileString("SET", "WakeUpHddOverlapNum", value, SettingPath.TimerSrvIniPath); }
        }
        [XmlIgnore]
        public Int32 RecAppWakeTime
        {
            get { return IniFileHandler.GetPrivateProfileInt("SET", "RecAppWakeTime", 2, SettingPath.TimerSrvIniPath); }
            set { IniFileHandler.WritePrivateProfileString("SET", "RecAppWakeTime", value, SettingPath.TimerSrvIniPath); }
        }
        //デフォルトマージン
        [XmlIgnore]
        public int DefStartMargin { get; set; }
        [XmlIgnore]
        public int DefEndMargin { get; set; }

        private List<RecPresetItem> recPresetList = null;
        [XmlIgnore]
        public List<RecPresetItem> RecPresetList
        {
            get
            {
                if (recPresetList == null) recPresetList = RecPresetItem.LoadPresetList();
                return recPresetList;
            }
            set
            {
                recPresetList = value;
                RecPresetItem.SavePresetList(recPresetList);
            }
        }

        private List<string> defRecFolders = null;
        [XmlIgnore]
        public List<string> DefRecFolders
        {
            get
            {
                if (defRecFolders == null)
                {
                    defRecFolders = new List<string>();
                    int num = IniFileHandler.GetPrivateProfileInt("SET", "RecFolderNum", 0, SettingPath.CommonIniPath);
                    if (num <= 0) defRecFolders.Add("");

                    for (int i = 0; i < num; i++)
                    {
                        defRecFolders.Add(IniFileHandler.GetPrivateProfileFolder("SET", "RecFolderPath" + i.ToString(), SettingPath.CommonIniPath));
                    }

                    if (defRecFolders[0] == "") defRecFolders[0] = SettingPath.SettingFolderPath;
                    defRecFolders = defRecFolders.Except(new[] { "" }).ToList();
                }
                return defRecFolders;
            }
            set
            {
                defRecFolders = value;
                if (defRecFolders == null) return;

                int recFolderCount = defRecFolders.Count == 1 && string.Compare(defRecFolders[0], SettingPath.SettingFolderPath, true) == 0 ? 0 : defRecFolders.Count;
                IniFileHandler.WritePrivateProfileString("SET", "RecFolderNum", recFolderCount, SettingPath.CommonIniPath);
                IniFileHandler.DeletePrivateProfileNumberKeys("SET", SettingPath.CommonIniPath, "RecFolderPath");
                
                for (int i = 0; i < recFolderCount; i++)
                {
                    IniFileHandler.WritePrivateProfileString("SET", "RecFolderPath" + i.ToString(), defRecFolders[i], SettingPath.CommonIniPath);
                }
            }
        }

        public void ReloadOtherOptions()
        {
            DefStartMargin = IniFileHandler.GetPrivateProfileInt("SET", "StartMargin", 5, SettingPath.TimerSrvIniPath);
            DefEndMargin = IniFileHandler.GetPrivateProfileInt("SET", "EndMargin", 2, SettingPath.TimerSrvIniPath);
            RecPresetList = null;
            DefRecFolders = null;
        }

        public Settings()
        {
            ResetColorSetting();
            
            UseCustomEpgView = false;
            CustomEpgTabList = new List<CustomEpgTabInfo>();
            MinHeight = 2;
            MinimumHeight = 0;
            ServiceWidth = 150;
            ScrollSize = 240;
            FontName = System.Drawing.SystemFonts.DefaultFont.Name;
            FontSize = 12;
            FontNameTitle = System.Drawing.SystemFonts.DefaultFont.Name;
            FontSizeTitle = 12;
            FontBoldTitle = true;
            NoToolTip = false;
            ToolTipWidth = 400;
            NoBallonTips = false;
            ForceHideBalloonTipSec = 0;
            PlayDClick = false;
            ShowEpgCapServiceOnly = false;
            SortServiceList = true;
            ExitAfterProcessingArgs = false;
            RecinfoErrCriticalDrops = false;
            DragScroll = 1.5;
            ReserveToolTipMode = 0;
            ReserveEpgInfoOpenMode = 0;
            TunerFontNameService = System.Drawing.SystemFonts.DefaultFont.Name;
            TunerFontSizeService = 12;
            TunerFontBoldService = true;
            TunerFontName = System.Drawing.SystemFonts.DefaultFont.Name;
            TunerFontSize = 12;
            TunerMinHeight = 2;
            TunerMinimumLine = 0;
            TunerDragScroll = 1.5;
            TunerScrollSize = 240;
            TunerMouseScrollAuto = false;
            TunerWidth = 150;
            TunerServiceNoWrap = true;
            TunerBorderLeftSize = 1;
            TunerBorderTopSize = 1;
            TunerTitleIndent = true;
            TunerToolTip = false;
            TunerToolTipViewWait = 1500;
            TunerPopup = false;
            TunerPopupMode = 0;
            TunerPopupRecinfo = false;
            TunerInfoSingleClick = false;
            TunerPopupWidth = 1;
            TunerChangeBorderWatch = false;
            TunerColorModeUse = false;
            TunerDisplayOffReserve = false;
            TunerToolTipMode = 0;
            TunerEpgInfoOpenMode = 0;
            EpgBorderLeftSize = 1;
            EpgBorderTopSize = 0.5;
            EpgTitleIndent = true;
            EpgReplacePattern = "";
            EpgReplacePatternTitle = "";
            EpgReplacePatternDef = false;
            EpgReplacePatternTitleDef = false;
            ApplyReplacePatternTuner = false;
            ShareEpgReplacePatternTitle = false;
            FontReplacePatternEdit = "";
            FontSizeReplacePattern = 12;
            FontBoldReplacePattern = false;
            ReplacePatternEditFontShare = false;
            EpgToolTip = false;
            EpgToolTipNoViewOnly = true;
            EpgToolTipViewWait = 1500;
            EpgPopup = true;
            EpgPopupMode = 0;
            EpgPopupWidth = 1;
            EpgExtInfoTable = false;
            EpgExtInfoPopup = false;
            EpgExtInfoTooltip = true;
            EpgGradation = true;
            EpgGradationHeader = true;
            EpgLoadArcInfo = false;
            EpgNoDisplayOld = false;
            EpgNoDisplayOldDays = 1;
            EpgChangeBorderWatch = false;
            EpgChangeBorderOnRec = false;
            EpgNameTabEnabled = true;
            EpgViewModeTabEnabled = false;
            EpgTabMoveCheckEnabled = true;
            ResColumnHead = "";
            ResSortDirection = ListSortDirection.Ascending;
            WndSettings = new WindowSettingData();
            SearchWndTabsHeight = 0;
            CloseMin = false;
            WakeMin = false;
            ViewButtonShowAsTab = false;
            ViewButtonList = new List<string>();
            TaskMenuList = new List<string>();
            Cust1BtnName = "";
            Cust1BtnCmd = "";
            Cust1BtnCmdOpt = "";
            Cust2BtnName = "";
            Cust2BtnCmd = "";
            Cust2BtnCmdOpt = "";
            Cust3BtnName = "";
            Cust3BtnCmd = "";
            Cust3BtnCmdOpt = "";
            AndKeyList = new List<string>();
            NotKeyList = new List<string>();
            DefSearchKey = new EpgSearchKeyInfo();
            UseLastSearchKey = false;
            SearchPresetList = new List<SearchPresetItem>();
            SetWithoutSearchKeyWord = false;
            RecInfoToolTipMode = 0;
            RecInfoColumnHead = "";
            RecInfoSortDirection = ListSortDirection.Ascending;
            RecInfoDropErrIgnore = 0;
            RecInfoDropWrnIgnore = 0;
            RecInfoScrambleIgnore = 0;
            RecInfoDropExcept = new List<string>();
            RecInfoNoYear = false;
            RecInfoNoSecond = false;
            RecInfoNoDurSecond = false;
            ResInfoNoYear = false;
            ResInfoNoSecond = false;
            ResInfoNoDurSecond = false;
            TvTestExe = "";
            TvTestCmd = "";
            NwTvMode = false;
            NwTvModeUDP = false;
            NwTvModeTCP = false;
            FilePlayExe = "";
            FilePlayCmd = "\"$FilePath$\"";
            FilePlayOnAirWithExe = false;
            FilePlayOnNwWithExe = false;
            OpenFolderWithFileDialog = false;
            IEpgStationList = new List<IEPGStationInfo>();
            MenuSet = new MenuSettingData();
            NWServerIP = "";
            NWServerPort = 4510;
            NWWaitPort = 0;
            NWMacAdd = "";
            NWPreset = new List<NWPresetItem>();
            WakeReconnectNW = false;
            WoLWaitRecconect = false;
            WoLWaitSecond= 30;
            SuspendCloseNW = false;
            NgAutoEpgLoadNW = false;
            ChkSrvRegistTCP = false;
            ChkSrvRegistInterval = 5;
            TvTestOpenWait = 2000;
            TvTestChgBonWait = 2000;
            EpgInfoSingleClick = false;
            EpgInfoOpenMode = 0;
            ExecBat = 0;
            SuspendChk = 0;
            SuspendChkTime = 15;
            ReserveListColumn = new List<ListColumnInfo>();
            RecInfoListColumn = new List<ListColumnInfo>();
            AutoAddEpgColumn = new List<ListColumnInfo>();
            AutoAddManualColumn = new List<ListColumnInfo>();
            EpgListColumn = new List<ListColumnInfo>();
            EpgListColumnHead = "";
            EpgListSortDirection = ListSortDirection.Ascending;
            SearchWndColumn = new List<ListColumnInfo>();
            SearchColumnHead = "";
            SearchSortDirection = ListSortDirection.Ascending;
            SearchEpgInfoOpenMode = 0;
            SaveSearchKeyword = true;
            InfoSearchWndColumn = new List<ListColumnInfo>();
            InfoSearchColumnHead = "";
            InfoSearchSortDirection = ListSortDirection.Ascending;
            InfoSearchLastWord = "";
            InfoSearchTitleOnly = true;
            InfoSearchReserveInfo = true;
            InfoSearchRecInfo = true;
            InfoSearchEpgAutoAddInfo = true;
            InfoSearchManualAutoAddInfo = true;
            InfoSearchItemTooltip = true;
            AutoSaveNotifyLog = 0;
            NotifyLogMax = 100;
            NotifyLogEpgTimer = false;
            ShowTray = true;
            MinHide = true;
            MouseScrollAuto = false;
            NoStyle = 0;
            NoSendClose = 0;
            CautionManyChange = true;
            CautionManyNum = 10;
            CautionOnRecChange = true;
            CautionOnRecMarginMin = 5;
            SyncResAutoAddChange = false;
            SyncResAutoAddChgNewRes = false;
            SyncResAutoAddDelete = false;
            DisplayNotifyEpgChange = false;
            DisplayNotifyJumpTime = 3;
            DisplayReserveAutoAddMissing = false;
            DisplayReserveMultiple = true;
            TryEpgSetting = true;
            LaterTimeUse = false;
            LaterTimeHour = 28 - 24;
            DisplayPresetOnSearch = false;
            ForceNWMode = false;
            SettingFolderPathNW = "";
            RecInfoExtraDataCache = true;
            RecInfoExtraDataCacheOptimize = true;
            RecInfoExtraDataCacheKeepConnect = false;
            UpdateTaskText = false;
            DisplayStatus = false;
            DisplayStatusNotify = false;
            IsVisibleReserveView = true;
            IsVisibleRecInfoView = true;
            IsVisibleAutoAddView = true;
            IsVisibleAutoAddViewMoveOnly = false;
            MainViewButtonsDock = Dock.Right;
            StartTab = CtxmCode.ReserveView;
            TrimSortTitle = false;
            KeepReserveWindow = false;
            PicUpTitleWork = new PicUpTitle();
        }

        public Settings DeepCloneStaticSettings()
        {
            //[XmlIgnore]絡みは何らかの形でフォローが入る
            var xs = new XmlSerializer(typeof(Settings));
            var ms = new MemoryStream();
            xs.Serialize(ms, this);
            ms.Seek(0, SeekOrigin.Begin);
            var other = (Settings)xs.Deserialize(ms);
            other.DefStartMargin = DefStartMargin;
            other.DefEndMargin = DefEndMargin;
            other.recPresetList = recPresetList.DeepClone();
            other.defRecFolders = defRecFolders == null ? null : defRecFolders.ToList();
            return other;
        }
        public void ShallowCopyDynamicSettingsTo(Settings dest)
        {
            //設定画面と関係なくその場で動的に更新されるプロパティ
            dest.ResColumnHead = ResColumnHead;
            dest.ResSortDirection = ResSortDirection;
            dest.WndSettings = WndSettings;
            dest.SearchWndTabsHeight = SearchWndTabsHeight;
            dest.RecInfoColumnHead = RecInfoColumnHead;
            dest.RecInfoSortDirection = RecInfoSortDirection;
            dest.OpenFolderWithFileDialog = OpenFolderWithFileDialog;
            dest.NWServerIP = NWServerIP;
            dest.NWServerPort = NWServerPort;
            dest.NWWaitPort = NWWaitPort;
            dest.NWMacAdd = NWMacAdd;
            dest.NWPreset = NWPreset;
            dest.ReserveListColumn = ReserveListColumn;
            dest.RecInfoListColumn = RecInfoListColumn;
            dest.AutoAddEpgColumn = AutoAddEpgColumn;
            dest.AutoAddManualColumn = AutoAddManualColumn;
            dest.EpgListColumn = EpgListColumn;
            dest.EpgListColumnHead = EpgListColumnHead;
            dest.EpgListSortDirection = EpgListSortDirection;
            dest.SearchWndColumn = SearchWndColumn;
            dest.SearchColumnHead = SearchColumnHead;
            dest.SearchSortDirection = SearchSortDirection;
            dest.InfoSearchWndColumn = InfoSearchWndColumn;
            dest.InfoSearchColumnHead = InfoSearchColumnHead;
            dest.InfoSearchSortDirection = InfoSearchSortDirection;
            dest.InfoSearchLastWord = InfoSearchLastWord;
            dest.InfoSearchTitleOnly = InfoSearchTitleOnly;
            dest.InfoSearchReserveInfo = InfoSearchReserveInfo;
            dest.InfoSearchRecInfo = InfoSearchRecInfo;
            dest.InfoSearchEpgAutoAddInfo = InfoSearchEpgAutoAddInfo;
            dest.InfoSearchManualAutoAddInfo = InfoSearchManualAutoAddInfo;
            dest.InfoSearchItemTooltip = InfoSearchItemTooltip;
            dest.NotifyLogMax = NotifyLogMax;
            dest.NotifyLogEpgTimer = NotifyLogEpgTimer;
            dest.TryEpgSetting = TryEpgSetting;
        }

        private static Settings _instance;
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                    //色設定関係
                    _instance.SetColorSetting();
                }
                return _instance;
            }
            set { _instance = value; }
        }

        private static bool noSave = false;
        /// <summary>設定ファイルロード関数</summary>
        public static void LoadFromXmlFile(bool nwMode = false)
        {
            string path = GetSettingPath();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    //読み込んで逆シリアル化する
                    Instance = (Settings)(new XmlSerializer(typeof(Settings)).Deserialize(fs));
                }
            }
            catch (Exception ex)
            {
                if (ex.GetBaseException().GetType() != typeof(FileNotFoundException))
                {
                    noSave = true;
                    MessageBox.Show("設定ファイルの読込に失敗しました。\r\n" +
                                    "現在の設定ファイルを削除するか、バックアップ(" + path + ".back)から復元してください。" +
                                    "EpgTimerを再起動するまで設定ファイルは上書き保存されません。", "設定ファイルエラー", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace);
                }
            }

            try
            {
                nwMode |= Instance.ForceNWMode;

                // タイミング合わせにくいので、メニュー系のデータチェックは
                // MenuManager側のワークデータ作成時に実行する。

                Instance.SetCustomEpgTabInfoID();
                Instance.SearchPresetList.FixUp();

                //互換用コード。検索プリセット対応。DefSearchKeyの吸収があるので旧CS仮対応コードより前。
                if (Instance.verSaved < 20170717)
                {
                    Instance.SearchPresetList[0].Data = Instance.DefSearchKey;
                }

                //互換用コード。旧CS仮対応コード(+0x70)も変換する。
                if (Instance.verSaved < 20170512)
                {
                    foreach (var info in Instance.CustomEpgTabList)
                    {
                        info.ViewContentList.AddRange(info.ViewContentKindList.Select(id_old => new EpgContentData((UInt32)(id_old << 16))));
                        EpgContentData.FixNibble(info.ViewContentList);
                        EpgContentData.FixNibble(info.SearchKey.contentList);
                        info.ViewContentKindList = null;
                    }
                    EpgContentData.FixNibble(Instance.SearchPresetList[0].Data.contentList);
                }

                //色設定関係
                Instance.SetColorSetting();

                if (Instance.ViewButtonList.Count == 0)
                {
                    Instance.ViewButtonList.AddRange(GetViewButtonDefIDs(nwMode));
                }
                if (Instance.TaskMenuList.Count == 0)
                {
                    Instance.TaskMenuList.AddRange(GetTaskMenuDefIDs(nwMode));
                }
                if (Instance.ReserveListColumn.Count == 0)
                {
                    var obj = new ReserveItem();
                    Instance.ReserveListColumn.AddRange(GetDefaultColumn(typeof(ReserveView)));
                    Instance.ResColumnHead = CommonUtil.NameOf(() => obj.StartTime);
                    Instance.ResSortDirection = ListSortDirection.Ascending;
                }
                if (Instance.RecInfoListColumn.Count == 0)
                {
                    var obj = new RecInfoItem();
                    Instance.RecInfoListColumn.AddRange(GetDefaultColumn(typeof(RecInfoView)));
                    Instance.RecInfoColumnHead = CommonUtil.NameOf(() => obj.StartTime);
                    Instance.RecInfoSortDirection = ListSortDirection.Descending;
                }
                if (Instance.AutoAddEpgColumn.Count == 0)
                {
                    Instance.AutoAddEpgColumn.AddRange(GetDefaultColumn(typeof(EpgAutoAddView)));
                }
                if (Instance.AutoAddManualColumn.Count == 0)
                {
                    Instance.AutoAddManualColumn.AddRange(GetDefaultColumn(typeof(ManualAutoAddView)));
                }
                if (Instance.EpgListColumn.Count == 0)
                {
                    var obj = new SearchItem();
                    Instance.EpgListColumn.AddRange(GetDefaultColumn(typeof(EpgListMainView)));
                    Instance.EpgListColumnHead = CommonUtil.NameOf(() => obj.StartTime);
                    Instance.EpgListSortDirection = ListSortDirection.Ascending;
                }
                if (Instance.SearchWndColumn.Count == 0)
                {
                    var obj = new SearchItem();
                    Instance.SearchWndColumn.AddRange(GetDefaultColumn(typeof(SearchWindow)));
                    Instance.SearchColumnHead = CommonUtil.NameOf(() => obj.StartTime);
                    Instance.SearchSortDirection = ListSortDirection.Ascending;
                }
                if (Instance.InfoSearchWndColumn.Count == 0)
                {
                    var obj = new InfoSearchItem();
                    Instance.InfoSearchWndColumn.AddRange(GetDefaultColumn(typeof(InfoSearchWindow)));
                    Instance.InfoSearchColumnHead = CommonUtil.NameOf(() => obj.StartTime);
                    Instance.InfoSearchSortDirection = ListSortDirection.Ascending;
                }
                if (Instance.RecInfoDropExcept.Count == 0)
                {
                    Instance.RecInfoDropExcept = RecInfoDropExceptDefault.ToList();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        /// <summary>設定ファイルセーブ関数</summary>
        public static void SaveToXmlFile()
        {
            try
            {
                if (noSave == true) return;

                string path = GetSettingPath();
                if (File.Exists(path) == true)
                {
                    File.Copy(path, path + ".back", true);
                }

                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    //シリアル化して書き込む
                    new XmlSerializer(typeof(Settings)).Serialize(fs, Instance);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        private static string GetSettingPath()
        {
            return Assembly.GetEntryAssembly().Location + ".xml";
        }

        public void SetSettings(string propertyName, object value)
        {
            if (propertyName == null) return;
            var info = typeof(Settings).GetProperty(propertyName);
            if (info != null) info.SetValue(this, value, null);
        }

        public object GetSettings(string propertyName)
        {
            if (propertyName == null) return null;
            var info = typeof(Settings).GetProperty(propertyName);
            return (info == null ? null : info.GetValue(this, null));
        }

        public RecPresetItem RecPreset(Int32 presetID)
        {
            return Instance.RecPresetList[Math.Max(0, Math.Min(presetID, Instance.RecPresetList.Count - 1))];
        }

        public void SetCustomEpgTabInfoID()
        {
            for (int i = 0; i < CustomEpgTabList.Count; i++)
            {
                CustomEpgTabList[i].ID = i;
            }
        }

        private static List<string> viewButtonIDs = new List<string>();
        public const string ViewButtonSpacer = "（空白）";
        public const string TaskMenuSeparator = "（セパレータ）";
        public static void ResisterViewButtonIDs(IEnumerable<string> list)
        {
            viewButtonIDs = list == null ? new List<string>() : list.ToList();
        } 
        public static List<string> GetViewButtonAllIDs()
        {
            return new List<string> { ViewButtonSpacer }.Concat(viewButtonIDs).ToList();
        }
        public static List<string> GetTaskMenuAllIDs()
        {
            return new List<string> { TaskMenuSeparator }.Concat(viewButtonIDs).ToList();
        }
        public static List<string> GetViewButtonDefIDs(bool nwMode)
        {
            if (nwMode == false)
            {
                return new List<string>
                {
                    "設定",
                    ViewButtonSpacer,
                    "検索",
                    ViewButtonSpacer,
                    "スタンバイ",
                    "休止",
                    ViewButtonSpacer,
                    "EPG取得",
                    ViewButtonSpacer,
                    "EPG再読み込み",
                    ViewButtonSpacer,
                    "終了",
                };
            }
            else
            {
                return new List<string>
                {
                    "設定",
                    ViewButtonSpacer,
                    "再接続",
                    ViewButtonSpacer,
                    "検索",
                    ViewButtonSpacer,
                    "EPG取得",
                    ViewButtonSpacer,
                    "EPG再読み込み",
                    ViewButtonSpacer,
                    "終了",
                };
            }
        }
        public static List<string> GetTaskMenuDefIDs(bool nwMode)
        {
            if (nwMode == false)
            {
                return new List<string>
                {
                    "設定",
                    TaskMenuSeparator,
                    "スタンバイ",
                    "休止",
                    TaskMenuSeparator,
                    "終了",
                };
            }
            else
            {
                return new List<string>
                {
                    "設定",
                    TaskMenuSeparator,
                    "再接続",
                    TaskMenuSeparator,
                    "終了",
                };
            }
        }

        public static List<ListColumnInfo> GetDefaultColumn(Type t)
        {
            if (t == typeof(ReserveView))
            {
                var obj = new ReserveItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.StartTime) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.NetworkName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ServiceName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.RecMode) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Priority) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Tuijyu) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Comment) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.RecFileName) },
                };
            }
            else if (t == typeof(RecInfoView))
            {
                var obj = new RecInfoItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.IsProtect) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.StartTime) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.NetworkName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ServiceName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Drops) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Scrambles) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Result) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.RecFilePath) },
                };
            }
            else if (t == typeof(EpgAutoAddView))
            {
                var obj = new EpgAutoDataItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.NotKey) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.RegExp) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.RecMode) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Priority) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Tuijyu) },
                };
            }
            else if (t == typeof(ManualAutoAddView))
            {
                var obj = new ManualAutoAddDataItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.DayOfWeek) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.StartTime) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ServiceName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.RecMode) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Priority) },
                };
            }
            else if (t == typeof(EpgListMainView))
            {
                var obj = new SearchItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Status) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.StartTime) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.NetworkName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ServiceName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                };
            }
            else if (t == typeof(SearchWindow))
            {
                var obj = new SearchItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Status) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.StartTime) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ProgramDuration) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.NetworkName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ServiceName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ProgramContent) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.JyanruKey) },
                };
            }
            else if (t == typeof(InfoSearchWindow))
            {
                var obj = new InfoSearchItem();
                return new List<ListColumnInfo>
                {
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ViewItemName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.Status) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.StartTime) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ProgramDuration) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.NetworkName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ServiceName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.EventName) },
                    new ListColumnInfo { Tag = CommonUtil.NameOf(() => obj.ProgramContent) },
                };
            }
            return null;
        }

        private void ResetColorSetting()
        {
            ContentColorList = new List<string>();
            ContentCustColorList = new List<uint>();
            EpgResColorList = new List<string>();
            EpgResCustColorList = new List<uint>();
            EpgEtcColors = new List<string>();
            EpgEtcCustColors = new List<uint>();
            ReserveRectFillOpacity = 0;
            ReserveRectFillWithShadow = true;
            TitleColor1 = "Black";
            TitleColor2 = "Black";
            TitleCustColor1 = 0xFFFFFFFF;
            TitleCustColor2 = 0xFFFFFFFF;
            TunerServiceColors = new List<string>();
            TunerServiceCustColors = new List<uint>();
            RecEndColors = new List<string>();
            RecEndCustColors = new List<uint>();
            ListDefColor = "カスタム";
            ListDefCustColor = 0xFF042271;
            RecModeFontColors = new List<string>();
            RecModeFontCustColors = new List<uint>();
            ResBackColors = new List<string>();
            ResBackCustColors = new List<uint>();
            StatColors = new List<string>();
            StatCustColors = new List<uint>();
        }
        //色リストの初期化用
        private static void _FillList<T>(List<T> list, T val, int num)
        {
            if (list.Count < num) list.AddRange(Enumerable.Repeat(val, num - list.Count));
        }
        private static void _FillList<T>(List<T> list, IEnumerable<T> val)
        {
            if (list.Count < val.Count()) list.AddRange(val.Skip(list.Count));
        }
        private void SetColorSetting()
        {
            int num;
            //番組表の背景色
            num = 17;//番組表17色。過去に16色時代があった。
            if (ContentColorList.Count < num)
            {
                var defColors = new string[]{
                        "LightYellow"
                        ,"Lavender"
                        ,"LavenderBlush"
                        ,"MistyRose"
                        ,"Honeydew"
                        ,"LightCyan"
                        ,"PapayaWhip"
                        ,"Pink"
                        ,"LightYellow"
                        ,"PapayaWhip"
                        ,"AliceBlue"
                        ,"AliceBlue"
                        ,"White"
                        ,"White"
                        ,"White"
                        ,"WhiteSmoke"
                        ,"White"
                    };
                _FillList(ContentColorList, defColors);
            }
            _FillList(ContentCustColorList, 0xFFFFFFFF, num);

            //番組表の予約枠色
            num = 9;
            if (EpgResColorList.Count < num)
            {
                var defColors = new string[]{
                        "Lime"              //通常
                        ,"Lime"             //通常(プログラム予約)
                        ,"Black"            //無効
                        ,"Red"              //チューナ不足
                        ,"Yellow"           //一部実行
                        ,"Blue"             //自動予約登録不明
                        ,"Purple"           //重複EPG予約
                        ,"Lime"             //録画中
                        ,"Lime"             //視聴中
                    };
                _FillList(EpgResColorList, defColors);
            }
            _FillList(EpgResCustColorList, 0xFFFFFFFF, num);

            //番組表の時間軸のデフォルトの背景色、その他色
            num = 12;
            if (EpgEtcColors.Count < num)
            {
                var defColors = new string[]{
                        "MediumPurple"      //00-05時
                        ,"LightSeaGreen"    //06-11時
                        ,"LightSalmon"      //12-17時
                        ,"CornflowerBlue"   //18-23時
                        ,"LightSlateGray"   //サービス背景色
                        ,"DarkGray"         //番組表背景色
                        ,"DarkGray"         //番組枠色
                        ,"White"            //サービス文字
                        ,"Silver"           //サービス枠色
                        ,"White"            //時間文字色
                        ,"White"            //時間枠色
                        ,"LightSlateGray"   //一週間モード日付枠色
                    };
                _FillList(EpgEtcColors, defColors);
            }
            _FillList(EpgEtcCustColors, 0xFFFFFFFF, num);

            //チューナー画面各色、保存名はServiceColorsだが他も含む。
            num = 2 + 5 + 12;//固定色2+優先度色5+追加の画面色
            if (TunerServiceColors.Count < num)
            {
                var defColors = Enumerable.Repeat("Black", 7)
                    .Concat(new string[]{
                        "DarkGray"          //チューナ画面背景色
                        ,"LightGray"        //予約枠色
                        ,"Black"            //時間文字色
                        ,"AliceBlue"        //時間背景色
                        ,"LightSlateGray"   //時間枠色
                        ,"Black"            //チューナー名文字色
                        ,"AliceBlue"        //チューナー名背景色
                        ,"LightSlateGray"   //チューナー名枠色
                        ,"LightGray"        //予約枠色(プログラム予約)
                        ,"Black"            //予約枠色(無効)
                        ,"OrangeRed"        //予約枠色(録画中)
                        ,"OrangeRed"        //予約枠色(視聴中)
                    });
                _FillList(TunerServiceColors, defColors);
            }
            _FillList(TunerServiceCustColors, 0xFFFFFFFF, num);

            //録画済み一覧背景色
            num = 3;
            if (RecEndColors.Count < num)
            {
                var defColors = new string[]{
                        "White"         //デフォルト
                        ,"LightPink"    //エラー
                        ,"LightYellow"  //ワーニング
                    };
                _FillList(RecEndColors, defColors);
            }
            _FillList(RecEndCustColors, 0xFFFFFFFF, num);

            //録画モード別予約文字色
            num = 6;
            _FillList(RecModeFontColors, ListDefColor, num);//そのときのデフォルト色をあてる
            _FillList(RecModeFontCustColors, ListDefCustColor, num);

            //状態別予約背景色
            num = 6;
            if (ResBackColors.Count < num)
            {
                var defColors = new string[]{
                        "White"         //通常
                        ,"LightGray"    //無効
                        ,"LightPink"    //チューナー不足
                        ,"LightYellow"  //一部実行
                        ,"LightBlue"    //自動予約登録不明
                        ,"Thistle"      //重複EPG予約
                    };
                _FillList(ResBackColors, defColors);
            }
            _FillList(ResBackCustColors, 0xFFFFFFFF, num);

            //予約状態列文字色
            num = 4;
            if (StatColors.Count < num)
            {
                var defColors = new List<string>{
                        "Blue"      //予約中
                        ,"OrangeRed"//録画中
                        ,"LimeGreen"//放送中
                        ,"OrangeRed"//視聴中
                    };
                _FillList(StatColors, defColors);
            }
            _FillList(StatCustColors, 0xFFFFFFFF, num);
        }
    }
}
