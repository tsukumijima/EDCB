using System;
using System.Collections.Generic;
using System.Linq;

namespace EpgTimer
{
    public class CustomEpgTabInfo : IDeepCloneObj
    {
        public CustomEpgTabInfo()
        {
            ViewServiceList = new List<ulong>();
            ViewContentKindList = new List<ushort>();
            ViewContentList = new List<EpgContentData>();
            ViewNotContentFlag = false;
            EpgSettingIndex = 0;
            EpgSettingID = 0;
            RecSetting = null;
            ViewMode = 0;
            NeedTimeOnlyBasic = false;
            NeedTimeOnlyWeek = false;
            StartTimeWeek = 4;
            CombineProgramByReverseSID = true;
            SearchMode = false;
            HighlightContentKind = true;
            SearchKey = new EpgSearchKeyInfo();
            SearchGenreNoSyncView = false;
            FilterEnded = false;
            ID = -1;
            IsVisible = true;
            JumpTarget = true;
        }
        public string TabName { get; set; }
        public override string ToString() { return TabName; }
        public int EpgSettingID { get; set; }
        public int EpgSettingIndex { get; set; }
        public RecSettingData RecSetting { get; set; }
        public int ViewMode { get; set; }
        public bool NeedTimeOnlyBasic { get; set; }
        public bool NeedTimeOnlyWeek { get; set; }
        public int StartTimeWeek { get; set; }
        public bool CombineProgramByReverseSID { get; set; }
        public List<ulong> ViewServiceList { get; set; }
        public List<ushort> ViewContentKindList { get; set; }//旧互換用・未使用
        public List<EpgContentData> ViewContentList { get; set; }
        public bool ViewNotContentFlag { get; set; }
        public bool SearchMode { get; set; }
        public bool HighlightContentKind { get; set; }
        public EpgSearchKeyInfo SearchKey { get; set; }
        public bool SearchGenreNoSyncView { get; set; }
        public bool FilterEnded { get; set; }
        public int ID { get; set; }
        public string Uid { get { return ID.ToString(); } }
        public bool IsVisible { get; set; }
        public bool JumpTarget { get; set; }

        public EpgSearchKeyInfo GetSearchKeyReloadEpg()
        {
            EpgSearchKeyInfo key = SearchKey.DeepClone();
            key.serviceList = ViewServiceList.Select(id => (long)id).ToList();
            if (SearchGenreNoSyncView == false)
            {
                key.contentList = ViewContentList.DeepClone();
                key.notContetFlag = (byte)(ViewNotContentFlag == true ? 1 : 0);
            }
            return key;
        }

        public object DeepCloneObj()
        {
            var other = (CustomEpgTabInfo)MemberwiseClone();
            other.ViewServiceList = ViewServiceList.ToList();
            other.ViewContentList = ViewContentList.DeepClone();
            other.SearchKey = SearchKey.DeepClone();
            return other;
        }
    }
}
