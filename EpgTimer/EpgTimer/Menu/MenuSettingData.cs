using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Reflection;

namespace EpgTimer
{
    //設定画面用
    public class MenuSettingData : IDeepCloneObj
    {
        public class CmdSaveData : IDeepCloneObj
        {
            public string Name { get; set; }
            public string TypeName { get; set; }
            public bool IsMenuEnabled { get; set; }
            public bool IsGestureEnabled { get; set; }
            public bool IsGesNeedMenu { get; set; }
            public List<ShortCutData> ShortCuts { get; set; }

            public CmdSaveData() { ShortCuts = new List<ShortCutData>(); }
            public object DeepCloneObj()
            {
                var other = (CmdSaveData)MemberwiseClone();
                other.ShortCuts = ShortCuts.ToList();
                return other;
            }

            public CmdSaveData(ICommand cmd, bool isMenuE, bool isGestureE, bool isGesNeedMenu, InputGestureCollection igc)
            {
                SetCommand(cmd);
                SetGestuers(igc);
                IsMenuEnabled = isMenuE;
                IsGestureEnabled = isGestureE;
                IsGesNeedMenu = isGesNeedMenu;
            }
            public void SetGestuers(InputGestureCollection igc)
            {
                ShortCuts = new List<ShortCutData>();
                foreach (var kg in igc.OfType<KeyGesture>()) { ShortCuts.Add(new ShortCutData(kg)); }
            }
            public InputGestureCollection GetGestuers()
            {
                var igc = new InputGestureCollection();
                ShortCuts.ForEach(item =>
                {
                    KeyGesture kg = item.GetKeyGesture();
                    if (kg != null) igc.Add(kg);
                });
                return igc;
            }
            public void SetCommand(ICommand cmd)
            {
                if (cmd is RoutedUICommand)
                {
                    Name = (cmd as RoutedUICommand).Name;
                    TypeName = (cmd as RoutedUICommand).OwnerType.FullName;
                }
                else
                {
                    Name = null;
                    TypeName = null;
                }
            }
            private RoutedUICommand command = null;
            public RoutedUICommand GetCommand()
            {
                try
                {
                    if (command == null || command.Name != Name || command.OwnerType.FullName != TypeName)
                    {
                        command = null;
                        Type t = Type.GetType(TypeName);
                        foreach (PropertyInfo info in t.GetProperties())
                        {
                            var cmd = info.GetValue(null, null) as RoutedUICommand;
                            if (cmd.Name == Name)
                            {
                                command = cmd;
                                break;
                            }
                        }
                    }
                    return command;
                }
                catch { }
                return null;
            }
        }
        public struct ShortCutData
        {
            public Key skey;
            public ModifierKeys mKey;
            public ShortCutData(KeyGesture kg) { skey = kg.Key; mKey = kg.Modifiers; }
            public KeyGesture GetKeyGesture() 
            {
                try
                {
                    //エラーの場合があり得る
                    return new KeyGesture(skey, mKey); 
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
                return null;
            }
        }

        public const int CautionDisplayItemNum = 10;
    
        public List<CtxmCode> IsManualAssign { get; set; }
        public bool NoMessageKeyGesture { get; set; }
        public bool NoMessageDelete { get; set; }
        public bool NoMessageDeleteAll { get; set; }
        public bool NoMessageDelete2 { get; set; }
        public bool NoMessageAdjustRes { get; set; }
        public bool RestoreNoUse { get; set; }
        public bool SetJunreToAutoAdd { get; set; }
        public bool SetJunreContentToAutoAdd { get; set; }        
        public bool CancelAutoAddOff { get; set; }
        public bool ShowCopyDialog { get; set; }
        public bool AutoAddFazySearch { get; set; }
        public bool AutoAddSearchToolTip { get; set; }
        public bool AutoAddSearchSkipSubMenu { get; set; }
        public bool ReserveSearchToolTip { get; set; }
        public bool OpenParentFolder { get; set; }
        public bool Keyword_Trim { get; set; }
        public bool CopyTitle_Trim { get; set; }
        public bool CopyContentBasic { get; set; }
        public bool InfoSearchTitle_Trim { get; set; }
        public bool SearchTitle_Trim { get; set; }
        public string SearchURI { get; set; }
        public bool NoMessageRecTag { get; set; }
        public bool NoMessageNotKEY { get; set; }
        public bool NoMessageNote { get; set; }
        public List<CmdSaveData> EasyMenuItems { get; set; }
        public List<CtxmSetting> ManualMenuItems { get; set; }

        public MenuSettingData() 
        {
            IsManualAssign = new List<CtxmCode>();
            NoMessageKeyGesture = false;
            NoMessageDelete = true;
            NoMessageDeleteAll = false;
            NoMessageDelete2 = false;
            NoMessageAdjustRes = false;
            RestoreNoUse = false;
            SetJunreToAutoAdd = true;
            SetJunreContentToAutoAdd = false;
            CancelAutoAddOff = false;
            ShowCopyDialog = false;
            AutoAddFazySearch = false;
            AutoAddSearchToolTip = false;
            AutoAddSearchSkipSubMenu = false;
            ReserveSearchToolTip = false;
            OpenParentFolder = false;
            Keyword_Trim = true;
            CopyTitle_Trim = false;
            CopyContentBasic = false;
            InfoSearchTitle_Trim = true;
            SearchTitle_Trim = true;
            SearchURI = "https://www.google.co.jp/search?hl=ja&q=";
            NoMessageRecTag = false;
            NoMessageNotKEY = false;
            NoMessageNote = false;
            EasyMenuItems = new List<CmdSaveData>();
            ManualMenuItems = new List<CtxmSetting>();
        }
        public object DeepCloneObj()
        {
            var other = (MenuSettingData)MemberwiseClone();
            other.IsManualAssign = IsManualAssign.ToList();
            other.EasyMenuItems = EasyMenuItems.DeepClone();
            other.ManualMenuItems = ManualMenuItems.DeepClone();
            return other;
        }
    }

    public class CtxmSetting : IDeepCloneObj
    {
        public CtxmCode ctxmCode { set; get; }
        public List<string> Items { set; get; }

        public CtxmSetting() { Items = new List<string>(); }
        //デフォルト内部データ → デフォルトセーブデータ用
        public CtxmSetting(CtxmData data)
        {
            ctxmCode = data.ctxmCode;
            Items = data.Items.Select(item => item.Header).ToList();
        }
        public object DeepCloneObj()
        {
            var other = (CtxmSetting)MemberwiseClone();
            other.Items = Items.ToList();
            return other;
        }
    }

    public static class CtxmSettingEx
    {
        public static CtxmSetting FindData(this List<CtxmSetting> list, CtxmCode code)
        { return list.Find(data => data.ctxmCode == code); }
    }
}
