using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace EpgTimer
{
    public interface IRecSetttingData
    {
        RecSettingData RecSettingInfo { get; set; }
        bool IsManual { get; }
    }
    public static class RecSettingDataEx
    {
        public static List<RecSettingData> RecSettingList(this IEnumerable<IRecSetttingData> list)
        {
            return list.Where(item => item != null).Select(item => item.RecSettingInfo).ToList();
        }
    }

    public partial class RecSettingData
    {
        public RecPresetItem LookUpPreset(bool IsManual = false, bool CopyData = false, bool ResCompare = false)
        {
            RecPresetItem preset = LookUpPreset(Settings.Instance.RecPresetList, IsManual, ResCompare);
            return preset == null ? new RecPresetItem("登録時", RecPresetItem.CustomID, CopyData == true ? this.DeepClone() : null) : preset;
        }
        public RecPresetItem LookUpPreset(IEnumerable<RecPresetItem> refdata, bool IsManual = false, bool ResCompare = false)
        {
            return refdata.FirstOrDefault(p1 => EqualsSettingTo(p1.Data, IsManual, ResCompare));
        }
        public bool EqualsSettingTo(RecSettingData other, bool IsManual = false, bool ResCompare = false)
        {
            return other != null
                && BatFilePath == other.BatFilePath
                //&& RecTag == other.RecTag RecTagは見ないことにする
                && ContinueRecFlag == other.ContinueRecFlag
                && (EndMargine == other.EndMargine || IsMarginDefault)//マージンデフォルト時
                && PartialRecFlag == other.PartialRecFlag
                && PartialRecFolder.EqualsTo(other.PartialRecFolder)
                && (PittariFlag == other.PittariFlag || IsManual == true)//プログラム予約時
                && Priority == other.Priority
                && (RebootFlag == other.RebootFlag || RecEndIsDefault)//動作後設定デフォルト時
                && RecFolderList.EqualsTo(other.RecFolderList)
                && (RecMode == other.RecMode || ResCompare && (RecMode == 5 || other.RecMode == 5))
                && (ServiceMode == other.ServiceMode || ((ServiceMode | other.ServiceMode) & 0x0F) == 0)//字幕等データ設定デフォルト時
                && (StartMargine == other.StartMargine || IsMarginDefault)//マージンデフォルト時
                && SuspendMode == other.SuspendMode//動作後設定
                && (TuijyuuFlag == other.TuijyuuFlag || IsManual == true)//プログラム予約時
                && TunerID == other.TunerID
                && IsMarginDefault == other.IsMarginDefault;
        }

        [XmlIgnore]
        public List<string> RecFolderViewList
        {
            get
            {
                List<string> defs = Settings.Instance.DefRecFolders;
                string def1 = defs.Count == 0 ? "!Default" : defs[0];
                Func<string, string> AdjustName = (f => f == "!Default" ? def1 : f);

                return RecFolderList.Select(info => AdjustName(info.RecFolder)).Concat(
                    PartialRecFolder.Select(info => "(ワンセグ) " + AdjustName(info.RecFolder))).ToList();
            }
        }

        [XmlIgnore]
        public bool IsMarginDefault { get { return UseMargineFlag == 0; } set { UseMargineFlag = (byte)(value == true ? 0 : 1); } }

        //真のマージン値
        public int StartMarginActual
        {
            get { return IsMarginDefault ? Settings.Instance.DefStartMargin : StartMargine; }
        }
        public int EndMarginActual
        {
            get { return IsMarginDefault ? Settings.Instance.DefEndMargin : EndMargine; }
        }

        public void SetMargin(bool isDefault, int? start = null, int? end = null, bool isOffset = false)
        {
            if (isDefault == false)
            {
                //デフォルトマージンだった場合は初期化する
                StartMargine = StartMarginActual;
                EndMargine = EndMarginActual;

                if (start != null) StartMargine = (int)start + (isOffset == true ? StartMargine : 0);
                if (end != null) EndMargine = (int)end + (isOffset == true ? EndMargine : 0);
            }
            IsMarginDefault = isDefault;
        }

        //指定サービス対象モードの補助
        [XmlIgnore]
        public bool ServiceModeIsDefault
        {
            get { return (ServiceMode & 0x0Fu) == 0; }
            set { ServiceMode = (ServiceMode & ~0x0Fu) | (value == true ? 0x00u : 0x01u); }
        }
        [XmlIgnore]
        public bool ServiceCaption
        {
            get { return (ServiceMode & 0x10u) != 0; }
            set { ServiceMode = (ServiceMode & ~0x10u) | (value == true ? 0x10u : 0x00u); }
        }
        [XmlIgnore]
        public bool ServiceData
        {
            get { return (ServiceMode & 0x20u) != 0; }
            set { ServiceMode = (ServiceMode & ~0x20u) | (value == true ? 0x20u : 0x00u); }
        }
        public bool ServiceCaptionActual
        {
            get { return ServiceModeIsDefault ? Settings.Instance.DefServiceCaption : ServiceCaption; }
        }
        public bool ServiceDataActual
        {
            get { return ServiceModeIsDefault ? Settings.Instance.DefServiceData : ServiceData; }
        }

        //録画後動作モードの補助。ToRecEndMode()はRecEndMode自体の範囲修正にも使用している。
        private static int ToRecEndMode(int val) { return (1 <= val && val <= 3) ? val : 0; }
        private static byte ToSuspendMode(int val) { return (byte)((1 <= val && val <= 3) ? val : 4); }
        public void SetSuspendMode(bool isDefault, int recEndMode = 0)
        {
            SuspendMode = isDefault ? (byte)0 : ToSuspendMode(recEndMode);
        }
        public bool RecEndIsDefault { get { return SuspendMode == 0; } }
        public int RecEndModeActual
        {
            get { return ToRecEndMode(RecEndIsDefault ? Settings.Instance.DefRecEndMode : SuspendMode); }
        }
        public byte RebootFlagActual
        {
            get { return RecEndIsDefault ? Settings.Instance.DefRebootFlg : RebootFlag; }
        }
    }
}
