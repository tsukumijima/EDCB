﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EpgTimer
{
    public class ReserveViewItem : ViewPanelItem<ReserveData>
    {
        public ReserveViewItem(ReserveData info) : base(info) { }
        public ReserveData ReserveInfo
        {
            get { return _Data; }
            set { _Data = value; }
        }
        public SolidColorBrush ForeColorPriTuner
        {
            get
            {
                if (ReserveInfo == null) return Brushes.Black;

                return CommonManager.Instance.CustTunerServiceColorPri[ReserveInfo.RecSetting.Priority - 1];
            }
        }
        public SolidColorBrush BackColorTuner
        {
            get
            {
                return vutil.ReserveErrBrush(ReserveInfo);
            }
        }
        public String StatusTuner
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsOnRec() == true)
                    {
                        if (ReserveInfo.RecSetting.RecMode == 5 || ReserveInfo.OverlapMode == 2)
                        {
                            return "放送中";
                        }
                        if (ReserveInfo.OverlapMode == 1)
                        {
                            return "一部のみ録画中";
                        }
                        return "録画中";
                    }
                }
                return "";
            }
        }
        public SolidColorBrush BorderBrushTuner
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.IsOnRec() == true)
                    {
                        return CommonManager.Instance.StatRecForeColor;
                    }
                    if (ReserveInfo.RecSetting.RecMode == 5)
                    {
                        return Brushes.Black;
                    }
                }
                return Brushes.DarkGray;
            }
        }
        public Brush BorderBrush
        {
            get
            {
                if (ReserveInfo != null)
                {
                    if (ReserveInfo.RecSetting.RecMode == 5)
                    {
                        return CommonManager.Instance.CustContentColorList[0x12];
                    }
                    if (ReserveInfo.OverlapMode == 2)
                    {
                        return CommonManager.Instance.CustContentColorList[0x13];
                    }
                    if (ReserveInfo.OverlapMode == 1)
                    {
                        return CommonManager.Instance.CustContentColorList[0x14];
                    }
                    if (ReserveInfo.IsAutoAddMissing() == true)
                    {
                        return CommonManager.Instance.CustContentColorList[0x15];
                    }
                }
                return CommonManager.Instance.CustContentColorList[0x11];
            }
        }
    }

    public static class ReserveViewItemEx
    {
        public static List<ReserveData> GetHitDataList(this List<ReserveViewItem> list, Point cursorPos)
        {
            return ReserveViewItem.GetHitDataList(list, cursorPos);
        }
        public static List<ReserveData> GetDataList(this ICollection<ReserveViewItem> list)
        {
            return ReserveViewItem.GetDataList(list);
        }
    }
}
