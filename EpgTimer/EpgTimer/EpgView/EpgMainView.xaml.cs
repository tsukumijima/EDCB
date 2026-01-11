using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    using EpgView;

    /// <summary>
    /// EpgMainView.xaml の相互作用ロジック
    /// </summary>
    public partial class EpgMainView : EpgMainViewBase
    {
        public EpgMainView()
        {
            InitializeComponent();
            SetControls(epgProgramView, timeView, serviceView.scrollViewer);
            SetControlsPeriod(timeJumpView, timeMoveView, button_now);

            base.InitCommand();

            //時間関係の設定の続き
            dateView.TimeButtonClick += (time, isDayMove) => MoveTime(time + TimeSpan.FromHours(isDayMove ? GetScrollTime().Hour : 0));
            nowViewTimer.Tick += (sender, e) => dateView.SetTodayMark();
        }
        public override void SetViewData(EpgViewData data)
        {
            base.SetViewData(data);
            serviceView.SetViewData(viewData);
        }

        List<EpgServiceInfo> primeServiceList = new List<EpgServiceInfo>();

        //強制イベント用。ScrollChangedEventArgsがCreate出来ない(RaiseEvent出来ない)のでOverride対応
        protected override void epgProgramView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            base.epgProgramView_ScrollChanged(sender, e);
            dateView.SetScrollTime(GetScrollTime());
        }

        protected override DateTime LimitedStart(IBasicPgInfo info)
        {
            return CommonUtil.Max(info.PgStartTime, ViewPeriod.Start);
        }
        protected override uint LimitedDuration(IBasicPgInfo info)
        {
            return (uint)(info.PgDurationSecond - (LimitedStart(info) - info.PgStartTime).TotalSeconds);
        }

        /// <summary>サービスロゴの再描画</summary>
        protected override void ReloadServiceLogo()
        {
            serviceView.RefreshLogo();
        }

        /// <summary>予約情報の再描画</summary>
        protected override void ReloadReserveViewItem()
        {
            try
            {
                reserveList.Clear();
                recinfoList.Clear();
                timeView.ClearMarker();

                var serviceReserveList = CombinedReserveList().ToLookup(data => data.Create64Key());
                int mergePos = 0;
                int mergeNum = 0;
                int servicePos = -1;
                for (int i = 0; i < serviceEventList.Count; i++)
                {
                    //TSIDが同じでSIDが逆順に登録されているときは併合する
                    if (--mergePos < i - mergeNum)
                    {
                        EpgServiceInfo curr = serviceEventList[i].serviceInfo;
                        for (mergePos = i; mergePos + 1 < serviceEventList.Count; mergePos++)
                        {
                            EpgServiceInfo next = serviceEventList[mergePos + 1].serviceInfo;
                            if (!viewInfo.CombineProgramByReverseSID || next.ONID != curr.ONID || next.TSID != curr.TSID || next.SID >= curr.SID)
                            {
                                break;
                            }
                            curr = next;
                        }
                        mergeNum = mergePos + 1 - i;
                        servicePos++;
                    }
                    var key = serviceEventList[mergePos].serviceInfo.Key;
                    if (serviceReserveList.Contains(key) == true)
                    {
                        foreach (var info in serviceReserveList[key])
                        {
                            ProgramViewItem refPgItem = null;
                            ReserveViewItem resItem = AddReserveViewItem(info, ref refPgItem, true);
                            if (resItem != null)
                            {
                                //横位置の設定
                                if (refPgItem != null && refPgItem.Data.Create64Key() != key)
                                {
                                    refPgItem = null;
                                }
                                resItem.Width = refPgItem != null ? refPgItem.Width : this.EpgStyle().ServiceWidth / mergeNum;
                                resItem.LeftPos = this.EpgStyle().ServiceWidth * (servicePos + (double)((mergeNum + i - mergePos - 1) / 2) / mergeNum);
                            }
                        }
                    }
                }

                epgProgramView.SetReserveList(dataItemList);

                if (this.EpgStyle().ReserveRectShowMarker)
                {
                    var setList = dataItemList.Where(item => item.Data.IsEnabled).OrderBy(item => item.Data.StartTimeActual).ToList();
                    var lists = new List<IEnumerable<ReserveViewItem>>
                    {
                        setList.Where(info => info.Data is ReserveDataEnd),
                        setList.Where(info => !(info.Data is ReserveDataEnd) && info.Data.OverlapMode != 1 && info.Data.OverlapMode != 2),
                        setList.Where(info => info.Data.OverlapMode == 1),
                        setList.Where(info => info.Data.OverlapMode == 2)
                    };
                    for (int i = 0; i < lists.Count; i++)
                    {
                        if(lists[i].Any())
                        {
                            Brush brush = i != 1 ? lists[i].First().BorderBrush : this.EpgBrushCache().ResColorList[0];
                            var timeRanges = lists[i].Select(info => new KeyValuePair<DateTime, TimeSpan>(info.Data.StartTimeActual, TimeSpan.FromSeconds(info.Data.DurationActual)));
                            timeView.AddMarker(timeRanges, brush);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        /// <summary>番組情報の再描画</summary>
        protected override void ReloadProgramViewItem()
        {
            try
            {
                dateView.ClearInfo();
                timeView.ClearInfo();
                serviceView.ClearInfo();
                primeServiceList.Clear();
                epgProgramView.ClearInfo();
                timeList.Clear();
                programList.Clear();
                ReDrawNowLine();

                if (serviceEventList.Count == 0) return;

                //必要番組の抽出と時間チェック
                //番組表でまとめて描画する矩形の幅と番組集合のリスト
                var programGroupList = new List<PanelItem<List<ProgramViewItem>>>();
                int groupSpan = 1;
                int mergePos = 0;
                int mergeNum = 0;
                int servicePos = -1;
                for (int i = 0; i < serviceEventList.Count; i++)
                {
                    //TSIDが同じでSIDが逆順に登録されているときは併合する
                    int spanCheckNum = 1;
                    if (--mergePos < i - mergeNum)
                    {
                        EpgServiceInfo curr = serviceEventList[i].serviceInfo;
                        for (mergePos = i; mergePos + 1 < serviceEventList.Count; mergePos++)
                        {
                            EpgServiceInfo next = serviceEventList[mergePos + 1].serviceInfo;
                            if (!viewInfo.CombineProgramByReverseSID || next.ONID != curr.ONID || next.TSID != curr.TSID || next.SID >= curr.SID)
                            {
                                break;
                            }
                            curr = next;
                        }
                        mergeNum = mergePos + 1 - i;
                        servicePos++;
                        //正順のときは貫きチェックするサービス数を調べる
                        for (; mergeNum == 1 && i + spanCheckNum < serviceEventList.Count; spanCheckNum++)
                        {
                            EpgServiceInfo next = serviceEventList[i + spanCheckNum].serviceInfo;
                            if (next.ONID != curr.ONID || next.TSID != curr.TSID)
                            {
                                break;
                            }
                            else if (viewInfo.CombineProgramByReverseSID && next.SID < curr.SID)
                            {
                                spanCheckNum--;
                                break;
                            }
                            curr = next;
                        }
                        if (--groupSpan <= 0)
                        {
                            groupSpan = spanCheckNum;
                            programGroupList.Add(new PanelItem<List<ProgramViewItem>>(new List<ProgramViewItem>()) { Width = this.EpgStyle().ServiceWidth * groupSpan });
                        }
                        primeServiceList.Add(serviceEventList[mergePos].serviceInfo);
                    }

                    foreach (EpgEventInfo eventInfo in serviceEventList[mergePos].eventList)
                    {
                        //イベントグループのチェック
                        int widthSpan = 1;
                        if (eventInfo.EventGroupInfo != null)
                        {
                            //サービス2やサービス3の結合されるべきもの
                            if (eventInfo.IsGroupMainEvent == false) continue;

                            //横にどれだけ貫くかチェック
                            int count = 1;
                            while (mergeNum == 1 ? count < spanCheckNum : count < mergeNum - (mergeNum + i - mergePos - 1) / 2)
                            {
                                EpgServiceInfo nextInfo = serviceEventList[mergeNum == 1 ? i + count : mergePos - count].serviceInfo;
                                bool findNext = false;
                                foreach (EpgEventData data in eventInfo.EventGroupInfo.eventDataList)
                                {
                                    if (nextInfo.Key == data.Create64Key())
                                    {
                                        widthSpan++;
                                        findNext = true;
                                    }
                                }
                                if (findNext == false)
                                {
                                    break;
                                }
                                count++;
                            }
                        }

                        //continueが途中にあるので登録はこの位置
                        var viewItem = new ProgramViewItem(eventInfo) { EpgSettingIndex = viewInfo.EpgSettingIndex, Filtered = viewData.EventFilteredHash.Contains(eventInfo.CurrentPgUID()) };
                        viewItem.DrawHours = eventInfo.start_time != LimitedStart(eventInfo);
                        programList[eventInfo.CurrentPgUID()] = viewItem;
                        programGroupList.Last().Data.Add(viewItem);

                        //横位置の設定
                        viewItem.Width = this.EpgStyle().ServiceWidth * widthSpan / mergeNum;
                        viewItem.LeftPos = this.EpgStyle().ServiceWidth * (servicePos + (double)((mergeNum + i - mergePos - 1) / 2) / mergeNum);
                    }
                }

                //縦位置の設定
                if (viewCustNeedTimeOnly == false && programList.Count != 0)
                {
                    ViewUtil.AddTimeList(timeList, programList.Values.Min(item => LimitedStart(item.Data)), 0);
                }
                SetProgramViewItemVertical();

                epgProgramView.SetProgramList(programGroupList, timeList.Count * 60 * this.EpgStyle().MinHeight);
                timeView.SetTime(timeList, false);
                dateView.SetTime(timeList, ViewPeriod);
                serviceView.SetService(primeServiceList);

                ReDrawNowLine();
                MoveNowTime();
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        protected override object GetJumpTabService()
        {
            int idx = (int)Math.Floor(clickPos.X / viewData.EpgStyle().ServiceWidth);
            if (idx < 0 || idx >= primeServiceList.Count) return null;
            EpgServiceInfo info = primeServiceList[idx];
            return new SearchItem { ReserveInfo = new ReserveData { OriginalNetworkID = info.ONID, TransportStreamID = info.TSID, ServiceID = info.SID } };
        }
    }
}
