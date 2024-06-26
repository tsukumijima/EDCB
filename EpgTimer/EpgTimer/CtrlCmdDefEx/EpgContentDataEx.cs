﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace EpgTimer
{
    public partial class EpgContentData
    {
        public EpgContentData() { }
        public EpgContentData(UInt32 key)
        {
            this.content_nibble_level_1 = (byte)(key >> 24);
            this.content_nibble_level_2 = (byte)(key >> 16);
            this.user_nibble_1 = (byte)(key >> 8);
            this.user_nibble_2 = (byte)key;
        }
        public Byte Nibble1 { get { return IsUserNibble ? user_nibble_1 : content_nibble_level_1; } }
        public Byte Nibble2 { get { return IsUserNibble ? user_nibble_2 : content_nibble_level_2; } }
        public UInt32 Key { get { return (UInt32)(content_nibble_level_1 << 24 | content_nibble_level_2 << 16 | (IsUserNibble ? (user_nibble_1 << 8 | user_nibble_2) : 0)); } }
        public UInt32 CategoryKey { get { return Key | (UInt32)(IsUserNibble ? 0x000000FF : 0x00FF0000); } }
        public List<UInt32> MatchingKeyList { get { return IsCategory && content_nibble_level_1 < 0x10 ? Enumerable.Range(0, 16).Select(i => (UInt32)((i << (IsUserNibble ? 0 : 16)) + (Key & (IsUserNibble ? 0xFFFFFF00 : 0xFF000000)))).ToList() : new List<UInt32> { Key }; } }
        public bool IsCategory { get { return Nibble2 == 0xFF; } }
        public bool IsUserNibble { get { return content_nibble_level_1 == 0x0E; } }
        public bool IsAttributeInfo { get { return IsUserNibble && content_nibble_level_2 == 0x00; } }

        /// <summary>互換コード。旧CS仮対応コード(+0x70)の処置用。</summary>
        static public void FixNibble(IEnumerable<EpgContentData> list) { foreach (var data in list) FixNibble(data); }
        static public void FixNibble(EpgContentData data)
        {
            if ((data.content_nibble_level_1 & 0xF0) == 0x70)
            {
                data.user_nibble_1 = (byte)(data.content_nibble_level_1 & 0x0F);
                data.user_nibble_2 = data.content_nibble_level_2;
                data.content_nibble_level_1 = 0x0E;
                data.content_nibble_level_2 = 0x01;
            }
        }
    }

    public class ContentKindInfo
    {
        public ContentKindInfo(UInt32 key = 0, String contentName = "", String subName = "")
        {
            this.Data = new EpgContentData(key);
            this.ContentName = contentName;
            this.SubName = subName;
        }
        public String ContentName { get; set; }
        public String SubName { get; set; }
        public EpgContentData Data { get; private set; }
        public String ListBoxView
        {
            get { return ContentName + (Data.IsCategory ? "" : " - " + SubName); }
        }
        public override string ToString()
        {
            return Data.IsCategory ? ContentName ?? "" : "  " + SubName;
        }
    }
}
