using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpgTimer
{
    public class RecSettingItem : DataListItemBase, IRecSetttingData
    {
        public virtual RecSettingData RecSettingInfo { get { return null; } set { } }
        public virtual void Reset() { preset = null; }
        public virtual bool IsManual { get { return false; } }

        public virtual string MarginStart
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return CustomTimeFormat(RecSettingInfo.StartMarginActual * -1);
            }
        }
        public virtual double MarginStartValue
        {
            get
            {
                if (RecSettingInfo == null) return double.MinValue;
                //
                return CustomMarginValue(RecSettingInfo.StartMarginActual * -1);
            }
        }
        public virtual string MarginEnd
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return CustomTimeFormat(RecSettingInfo.EndMarginActual);
            }
        }
        public virtual double MarginEndValue
        {
            get
            {
                if (RecSettingInfo == null) return double.MinValue;
                //
                return CustomMarginValue(RecSettingInfo.EndMarginActual);
            }
        }
        protected string CustomTimeFormat(int span)
        {
            string hours;
            string minutes;
            string seconds = (span % 60).ToString("00;00");
            if (Math.Abs(span) < 3600)
            {
                hours = "";
                minutes = (span / 60).ToString("0;0") + ":";
            }
            else
            {
                hours = (span / 3600).ToString("0;0") + ":";
                minutes = ((span % 3600) / 60).ToString("00;00") + ":";
            }
            return span.ToString("+;-") + hours + minutes + seconds + (RecSettingInfo.IsMarginDefault == true ? "*" : " ");
        }
        protected double CustomMarginValue(int span)
        {
            return span + (RecSettingInfo.IsMarginDefault == true ? 0 : 0.1);
        }

        protected string preset = null;
        public virtual string Preset
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                if (preset == null) preset = RecSettingInfo.LookUpPreset(IsManual).DisplayName;
                return preset;
            }
        }
        public virtual string RecMode
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return CommonManager.ConvertRecModeText(RecSettingInfo.RecMode);
            }
        }
        public virtual string Priority
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return RecSettingInfo.Priority.ToString();
            }
        }
        public virtual string Tuijyu
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return CommonManager.ConvertYesNoText(RecSettingInfo.TuijyuuFlag);
            }
        }
        public virtual string Pittari
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return CommonManager.ConvertYesNoText(RecSettingInfo.PittariFlag);
            }
        }
        public virtual string TunerID
        {
            get
            {
                if (RecSettingInfo == null) return "";
                //
                return CommonManager.ConvertTunerText(RecSettingInfo.TunerID);
            }
        }
        public virtual string BatFilePath
        {
            get { return RecSettingInfo.BatFilePath; }
        }

        public virtual List<string> RecFolder
        {
            get
            {
                if (RecSettingInfo == null) return new List<string>();
                //
                return RecSettingInfo.RecFolderViewList;
            }
        }

        public string ConvertRecSettingText()
        {
            if (RecSettingInfo == null) return "";
            //
            string view = "録画モード : " + RecMode + "\r\n";
            view += "優先度 : " + Priority + "\r\n";
            view += "追従 : " + Tuijyu + "\r\n";
            view += "ぴったり(?): " + Pittari + "\r\n";
            view += "指定サービス対象データ : 字幕含" + (RecSettingInfo.ServiceCaptionActual ? "める" : "めない")
                                            + " データカルーセル含" + (RecSettingInfo.ServiceDataActual ? "める" : "めない")
                                            + (RecSettingInfo.ServiceModeIsDefault ? " (デフォルト)" : "") + "\r\n";
            view += "録画実行bat : " + (RecSettingInfo.BatFilePath == "" ? "なし" : RecSettingInfo.BatFilePath) + "\r\n";
            {
                List<RecFileSetInfo> recFolderList = RecSettingInfo.RecFolderList;
                view += "録画フォルダ : " + (recFolderList.Count == 0 ? "(デフォルト)" : "") + "\r\n";
                if (recFolderList.Count == 0)
                {
                    string plugInFile = IniFileHandler.GetPrivateProfileString("SET", "RecNamePlugInFile", "RecName_Macro.dll", SettingPath.TimerSrvIniPath);
                    foreach (string info in Settings.Instance.DefRecFolders)
                    {
                        view += info + " (WritePlugIn:Write_Default.dll ファイル名PlugIn:" + plugInFile + ")\r\n";
                    }
                }
                else
                {
                    foreach (RecFileSetInfo info in RecSettingInfo.RecFolderList)
                    {
                        view += info.RecFolder + " (WritePlugIn:" + info.WritePlugIn + " ファイル名PlugIn:" + info.RecNamePlugIn + ")\r\n";
                    }
                }
            }
            view += "録画マージン : 開始 " + RecSettingInfo.StartMarginActual.ToString() +
                                  " 終了 " + RecSettingInfo.EndMarginActual.ToString()
                     + (RecSettingInfo.IsMarginDefault == true ? " (デフォルト)" : "") + "\r\n";

            view += "録画後動作 : "
                + new string[] { "何もしない", "スタンバイ", "休止", "シャットダウン" }[RecSettingInfo.RecEndModeActual]
                + (RecSettingInfo.RebootFlagActual == 1 ? " 復帰後再起動する" : "")
                + (RecSettingInfo.SuspendMode == 0 ? " (デフォルト)" : "") + "\r\n";

            if (RecSettingInfo.PartialRecFlag == 0)
            {
                view += "部分受信 : 同時出力なし\r\n";
            }
            else
            {
                view += "部分受信 : 同時出力あり\r\n";
                view += "部分受信　録画フォルダ : \r\n";
                foreach (RecFileSetInfo info in RecSettingInfo.PartialRecFolder)
                {
                    view += info.RecFolder + " (WritePlugIn:" + info.WritePlugIn + " ファイル名PlugIn:" + info.RecNamePlugIn + ")\r\n";
                }
            }
            view += "連続録画動作 : " + (RecSettingInfo.ContinueRecFlag == 0 ? "分割" : "同一ファイル出力") + "\r\n";
            view += "使用チューナー強制指定 : " + TunerID;

            return view;
        }
    }
}
