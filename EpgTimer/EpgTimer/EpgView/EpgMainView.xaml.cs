using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>予約情報の再描画</summary>
        protected override void ReloadReserveViewItem()
        {
            try
            {
                reserveList.Clear();
                recinfoList.Clear();

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
                            if (next.ONID != curr.ONID || next.TSID != curr.TSID || next.SID >= curr.SID)
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
                                resItem.Width = refPgItem != null ? refPgItem.Width : Settings.Instance.ServiceWidth / mergeNum;
                                resItem.LeftPos = Settings.Instance.ServiceWidth * (servicePos + (double)((mergeNum + i - mergePos - 1) / 2) / mergeNum);
                            }
                        }
                    }
                }

                epgProgramView.SetReserveList(dataItemList);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        /// <summary>番組情報の再描画</summary>
        protected override void ReloadProgramViewItem()
        {
            try
            {
                dateView.ClearInfo();
                timeView.ClearInfo();
                serviceView.ClearInfo();
                epgProgramView.ClearInfo();
                timeList.Clear();
                programList.Clear();
                ReDrawNowLine();

                if (serviceEventList.Count == 0) return;

                //必要番組の抽出と時間チェック
                var primeServiceList = new List<EpgServiceInfo>();
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
                            if (next.ONID != curr.ONID || next.TSID != curr.TSID || next.SID >= curr.SID)
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
                            else if (next.SID < curr.SID)
                            {
                                spanCheckNum--;
                                break;
                            }
                            curr = next;
                        }
                        if (--groupSpan <= 0)
                        {
                            groupSpan = spanCheckNum;
                            programGroupList.Add(new PanelItem<List<ProgramViewItem>>(new List<ProgramViewItem>()) { Width = Settings.Instance.ServiceWidth * groupSpan });
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
                        var viewItem = new ProgramViewItem(eventInfo);
                        viewItem.DrawHours = eventInfo.start_time != LimitedStart(eventInfo);
                        programList[eventInfo.CurrentPgUID()] = viewItem;
                        programGroupList.Last().Data.Add(viewItem);

                        //横位置の設定
                        viewItem.Width = Settings.Instance.ServiceWidth * widthSpan / mergeNum;
                        viewItem.LeftPos = Settings.Instance.ServiceWidth * (servicePos + (double)((mergeNum + i - mergePos - 1) / 2) / mergeNum);
                    }
                }

                //縦位置の設定
                if (viewCustNeedTimeOnly == false && programList.Count != 0)
                {
                    ViewUtil.AddTimeList(timeList, programList.Values.Min(item => LimitedStart(item.Data)), 0);
                }
                SetProgramViewItemVertical();

                epgProgramView.SetProgramList(programGroupList, timeList.Count * 60 * Settings.Instance.MinHeight);
                timeView.SetTime(timeList, false);
                dateView.SetTime(timeList, ViewPeriod);
                serviceView.SetService(primeServiceList);

                ReDrawNowLine();
                MoveNowTime();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
    }
}
