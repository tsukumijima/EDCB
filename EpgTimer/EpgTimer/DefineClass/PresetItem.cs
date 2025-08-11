using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace EpgTimer
{
    public class PresetItem : IDeepCloneObj
    {
        public PresetItem() { }
        public PresetItem(string name, int id, object data = null) { DisplayName = name; ID = id; Data = data; }
        public string DisplayName { get; set; }
        public int ID { get; set; }
        public virtual object Data { get; set; }
        public virtual object DeepCloneObj() { return null; }
        public override string ToString() { return string.IsNullOrWhiteSpace(DisplayName) ? "(プリセット)" : DisplayName; }
        public virtual object Reset() { DisplayName = "デフォルト"; ID = 0; Data = null; return this; }

        public const int CustomID = -1;
        public const int LastID = -2;
        public virtual bool IsCustom { get { return ID < 0; } }
    }

    static class PresetItemEx
    {
        static public void FixID<S>(this IEnumerable<S> list) where S : PresetItem
        {
            int i = 0;
            foreach (var item in list.Where(p => p.IsCustom == false)) item.ID = i++;
        }
        static public List<S> FixUp<S>(this List<S> list) where S : PresetItem, new()
        {
            list.FixID();
            if (list.Count == 0) list.Add((S)(new S().Reset()));
            return list;
        }
    }

    public class PresetItemT<T> : PresetItem where T : class, IDeepCloneObj, new()
    {
        public PresetItemT() { }
        public PresetItemT(string name, int id, T data = null) : base(name, id, data) { }
        [XmlIgnore]
        public new virtual T Data { get { return base.Data as T; } set { base.Data = value; } }
        public override object Reset() { base.Reset(); Data = new T(); return this; }
        public override object DeepCloneObj()
        {
            var other = (PresetItem)MemberwiseClone();
            if (Data != null) other.Data = Data.DeepCloneObj();//nullのときはnull。
            return other;
        }
    }

    public class SearchPresetItem : PresetItemT<EpgSearchKeyInfo>
    {
        public SearchPresetItem() { }
        public SearchPresetItem(string name, int id, EpgSearchKeyInfo data = null) : base(name, id, data) { }
    }

    public class RecPresetItem : PresetItemT<RecSettingData>
    {
        public RecPresetItem() { }
        public RecPresetItem(string name, int id, RecSettingData data = null) : base(name, id, data) { }

        protected void LoadPresetData()
        {
            string IDS = ID == 0 ? "" : ID.ToString();
            string defName = "REC_DEF" + IDS;

            Data = new RecSettingData();
            Data.RecMode = (byte)IniFileHandler.GetPrivateProfileInt(defName, "RecMode", 1, SettingPath.TimerSrvIniPath);
            Data.IsEnable = Data.RecMode / 5 % 2 == 0;
            if (!Data.IsEnable) Data.RecMode = (byte)(IniFileHandler.GetPrivateProfileInt(defName, "NoRecMode", 1, SettingPath.TimerSrvIniPath) % 5);
            Data.Priority = (byte)IniFileHandler.GetPrivateProfileInt(defName, "Priority", 2, SettingPath.TimerSrvIniPath);
            Data.TuijyuuFlag = (byte)IniFileHandler.GetPrivateProfileInt(defName, "TuijyuuFlag", 1, SettingPath.TimerSrvIniPath);
            Data.ServiceMode = (byte)IniFileHandler.GetPrivateProfileInt(defName, "ServiceMode", 16, SettingPath.TimerSrvIniPath);
            Data.PittariFlag = (byte)IniFileHandler.GetPrivateProfileInt(defName, "PittariFlag", 0, SettingPath.TimerSrvIniPath);
            Data.UnPackBatFilePath(IniFileHandler.GetPrivateProfileString(defName, "BatFilePath", "", SettingPath.TimerSrvIniPath));

            var GetRecFileSetInfo = new Action<string, List<RecFileSetInfo>>((appName, folderList) =>
            {
                int count = IniFileHandler.GetPrivateProfileInt(appName, "Count", 0, SettingPath.TimerSrvIniPath);
                for (int i = 0; i < count; i++)
                {
                    var folderInfo = new RecFileSetInfo();
                    folderInfo.RecFolder = IniFileHandler.GetPrivateProfileFolder(appName, i.ToString(), SettingPath.TimerSrvIniPath);
                    folderInfo.WritePlugIn = IniFileHandler.GetPrivateProfileString(appName, "WritePlugIn" + i.ToString(), "Write_Default.dll", SettingPath.TimerSrvIniPath);
                    folderInfo.RecNamePlugIn = IniFileHandler.GetPrivateProfileString(appName, "RecNamePlugIn" + i.ToString(), "", SettingPath.TimerSrvIniPath);
                    folderList.Add(folderInfo);
                }
            });
            GetRecFileSetInfo("REC_DEF_FOLDER" + IDS, Data.RecFolderList);
            GetRecFileSetInfo("REC_DEF_FOLDER_1SEG" + IDS, Data.PartialRecFolder);

            Data.SuspendMode = (byte)IniFileHandler.GetPrivateProfileInt(defName, "SuspendMode", 0, SettingPath.TimerSrvIniPath);
            Data.RebootFlag = (byte)IniFileHandler.GetPrivateProfileInt(defName, "RebootFlag", 0, SettingPath.TimerSrvIniPath);
            Data.UseMargineFlag = (byte)IniFileHandler.GetPrivateProfileInt(defName, "UseMargineFlag", 0, SettingPath.TimerSrvIniPath);
            Data.StartMargine = IniFileHandler.GetPrivateProfileInt(defName, "StartMargine", 5, SettingPath.TimerSrvIniPath);
            Data.EndMargine = IniFileHandler.GetPrivateProfileInt(defName, "EndMargine", 2, SettingPath.TimerSrvIniPath);
            Data.ContinueRecFlag = (byte)IniFileHandler.GetPrivateProfileInt(defName, "ContinueRec", 0, SettingPath.TimerSrvIniPath);
            Data.PartialRecFlag = (byte)IniFileHandler.GetPrivateProfileInt(defName, "PartialRec", 0, SettingPath.TimerSrvIniPath);
            Data.TunerID = (uint)IniFileHandler.GetPrivateProfileInt(defName, "TunerID", 0, SettingPath.TimerSrvIniPath);
        }
        protected void SavePresetData()
        {
            if (Data == null) return;

            string IDS = ID == 0 ? "" : ID.ToString();
            string defName = "REC_DEF" + IDS;

            IniFileHandler.WritePrivateProfileString(defName, "SetName", DisplayName, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "RecMode", Data.IsEnable ? Data.RecMode : 5, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "NoRecMode", Data.RecMode, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "Priority", Data.Priority, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "TuijyuuFlag", Data.TuijyuuFlag, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "ServiceMode", Data.ServiceMode, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "PittariFlag", Data.PittariFlag, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "BatFilePath", Data.PackBatFilePath(), SettingPath.TimerSrvIniPath);

            var WriteRecFileSetInfo = new Action<string, List<RecFileSetInfo>>((appName, folderList) =>
            {
                IniFileHandler.WritePrivateProfileString(appName, "Count", folderList.Count.ToString(), SettingPath.TimerSrvIniPath);
                for (int j = 0; j < folderList.Count; j++)
                {
                    IniFileHandler.WritePrivateProfileString(appName, j.ToString(), folderList[j].RecFolder, SettingPath.TimerSrvIniPath);
                    IniFileHandler.WritePrivateProfileString(appName, "WritePlugIn" + j.ToString(), folderList[j].WritePlugIn, SettingPath.TimerSrvIniPath);
                    IniFileHandler.WritePrivateProfileString(appName, "RecNamePlugIn" + j.ToString(), folderList[j].RecNamePlugIn, SettingPath.TimerSrvIniPath);
                }
            });
            WriteRecFileSetInfo("REC_DEF_FOLDER" + IDS, Data.RecFolderList);
            WriteRecFileSetInfo("REC_DEF_FOLDER_1SEG" + IDS, Data.PartialRecFolder);

            IniFileHandler.WritePrivateProfileString(defName, "SuspendMode", Data.SuspendMode, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "RebootFlag", Data.RebootFlag, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "UseMargineFlag", Data.UseMargineFlag, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "StartMargine", Data.StartMargine, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "EndMargine", Data.EndMargine, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "ContinueRec", Data.ContinueRecFlag, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "PartialRec", Data.PartialRecFlag, SettingPath.TimerSrvIniPath);
            IniFileHandler.WritePrivateProfileString(defName, "TunerID", Data.TunerID, SettingPath.TimerSrvIniPath);
        }

        static public void SavePresetList(List<RecPresetItem> list)
        {
            if (list == null) return;

            //古いエントリを削除
            IniFileHandler.DeletePrivateProfileNumberKeys(null, SettingPath.TimerSrvIniPath, "REC_DEF", "", true);
            IniFileHandler.DeletePrivateProfileNumberKeys(null, SettingPath.TimerSrvIniPath, "REC_DEF_FOLDER", "", true);
            IniFileHandler.DeletePrivateProfileNumberKeys(null, SettingPath.TimerSrvIniPath, "REC_DEF_FOLDER_1SEG", "", true);

            //保存
            string saveID = "";
            list.FixUp().ForEach(item =>
            {
                item.SavePresetData();
                if (item.ID != 0) saveID += item.ID + ",";
            });
            IniFileHandler.WritePrivateProfileString("SET", "PresetID", saveID, SettingPath.TimerSrvIniPath);
        }
        static public List<RecPresetItem> LoadPresetList()
        {
            var list = new List<RecPresetItem> { new RecPresetItem(IniFileHandler.GetPrivateProfileString("REC_DEF", "SetName", "デフォルト", SettingPath.TimerSrvIniPath), 0) };
            foreach (string s in IniFileHandler.GetPrivateProfileString("SET", "PresetID", "", SettingPath.TimerSrvIniPath).Split(','))
            {
                int id;
                int.TryParse(s, out id);
                if (list.Exists(p => p.ID == id) == false)
                {
                    string name = IniFileHandler.GetPrivateProfileString("REC_DEF" + id, "SetName", "", SettingPath.TimerSrvIniPath);
                    list.Add(new RecPresetItem(name, id));
                }
            }
            list.ForEach(item => item.LoadPresetData());//ID=0の読込も忘れない

            return list.FixUp();
        }
    }
}
