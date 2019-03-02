using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EpgTimer.EpgView
{
    public class EpgMainViewBase : EpgViewBase
    {
        protected class StateMainBase : StateBase
        {
            public DateTime? time;
            public StateMainBase() { }
            public StateMainBase(EpgMainViewBase view) : base(view) { time = view.GetScrollTime(); }
        }
        public override EpgViewState GetViewState() { return new StateMainBase(this); }
        protected StateMainBase RestoreState { get { return restoreState as StateMainBase ?? new StateMainBase(); } }

        protected Dictionary<UInt64, ProgramViewItem> programList = new Dictionary<UInt64, ProgramViewItem>();
        protected List<ReserveViewItem> reserveList = new List<ReserveViewItem>();
        protected List<ReserveViewItem> recinfoList = new List<ReserveViewItem>();
        protected IEnumerable<ReserveViewItem> dataItemList { get { return recinfoList.Concat(reserveList); } }
        protected List<DateTime> timeList = new List<DateTime>();
        protected DispatcherTimer nowViewTimer;
        protected Point clickPos;

        private ProgramView programView = null;
        private TimeView timeView = null;
        private ScrollViewer horizontalViewScroll = null;

        protected ContextMenu cmdMenu = new ContextMenu();

        protected override void InitCommand()
        {
            base.InitCommand();

            //コマンド集の初期化の続き
            mc.SetFuncGetDataList(isAll => isAll == true ? dataItemList.GetDataList() : dataItemList.GetHitDataList(clickPos));
            mc.SetFuncGetEpgEventList(() => 
            {
                ProgramViewItem hitItem = programView.GetProgramViewData(clickPos);
                return hitItem != null && hitItem.Data != null ? CommonUtil.ToList(hitItem.Data) : new List<EpgEventInfo>();
            });

            //コマンド集からコマンドを登録
            mc.ResetCommandBindings(this, cmdMenu);
        }
        protected override void RefreshMenuInfo()
        {
            base.RefreshMenuInfo();
            mBinds.ResetInputBindings(this);
            mm.CtxmGenerateContextMenu(cmdMenu, CtxmCode.EpgView, false);
        }

        public void SetControls(ProgramView pv, TimeView tv, ScrollViewer hv, Button button_now)
        {
            programView = pv;
            timeView = tv;
            horizontalViewScroll = hv;

            programView.ScrollChanged += epgProgramView_ScrollChanged;
            programView.LeftDoubleClick += (sender, cursorPos) => EpgCmds.ShowDialog.Execute(null, cmdMenu);
            programView.MouseClick += (sender, cursorPos) => clickPos = cursorPos;
            programView.RightClick += epgProgramView_RightClick;

            nowViewTimer = new DispatcherTimer(DispatcherPriority.Normal);
            nowViewTimer.Tick += (sender, e) => ReDrawNowLine();
            this.Unloaded += (sender, e) => nowViewTimer.Stop();//アンロード時にReDrawNowLine()しないパスがある。

            button_now.Click += (sender, e) => MoveNowTime();
        }

        protected override void UpdateStatusData(int mode = 0)
        {
            this.status[1] = string.Format("番組数:{0}", programList.Count)
                + ViewUtil.ConvertReserveStatus(reserveList.GetDataList(), "　予約");
        }

        protected virtual DateTime GetViewTime(DateTime time)
        {
            return time;
        }

        /// <summary>番組の縦表示位置設定</summary>
        protected virtual void SetProgramViewItemVertical()
        {
            //時間リストを構築
            if (viewCustNeedTimeOnly == true)
            {
                var timeSet = new HashSet<DateTime>();
                foreach (ProgramViewItem item in programList.Values)
                {
                    ViewUtil.AddTimeList(timeSet, GetViewTime(item.Data.start_time), item.Data.PgDurationSecond);
                }
                timeList.AddRange(timeSet.OrderBy(time => time));
            }

            //縦位置を設定
            foreach (ProgramViewItem item in programList.Values)
            {
                ViewUtil.SetItemVerticalPos(timeList, item, GetViewTime(item.Data.start_time), item.Data.durationSec, Settings.Instance.MinHeight, viewCustNeedTimeOnly);
            }

            //最低表示行数を適用。また、最低表示高さを確保して、位置も調整する。
            ViewUtil.ModifierMinimumLine(programList.Values, Settings.Instance.MinimumHeight, Settings.Instance.FontSizeTitle, Settings.Instance.EpgBorderTopSize);

            //必要時間リストの修正。番組長の関係や、最低表示行数の適用で下に溢れた分を追加する。
            ViewUtil.AdjustTimeList(programList.Values, timeList, Settings.Instance.MinHeight);
        }

        protected virtual ReserveViewItem AddReserveViewItem(ReserveData resInfo, ref ProgramViewItem refPgItem, bool SearchEvent = false)
        {
            //マージン適用前
            DateTime startTime = GetViewTime(resInfo.StartTime);
            DateTime chkStartTime = startTime.Date.AddHours(startTime.Hour);

            //離れた時間のプログラム予約など、番組表が無いので表示不可
            int index = timeList.BinarySearch(chkStartTime);
            if (index < 0) return null;

            //EPG予約の場合は番組の外側に予約枠が飛び出さないようなマージンを作成。
            double StartMargin = resInfo.IsEpgReserve == false ? resInfo.StartMarginResActual : Math.Min(0, resInfo.StartMarginResActual);
            double EndMargin = resInfo.IsEpgReserve == false ? resInfo.EndMarginResActual : Math.Min(0, resInfo.EndMarginResActual);

            //duationがマイナスになる場合は後で処理される
            startTime = startTime.AddSeconds(-StartMargin);
            double duration = resInfo.DurationSecond + StartMargin + EndMargin;

            var resItem = new ReserveViewItem(resInfo);
            (resInfo is ReserveDataEnd ? recinfoList : reserveList).Add(resItem);

            //予約情報から番組情報を特定し、枠表示位置を再設定する
            refPgItem = null;
            programList.TryGetValue(resInfo.CurrentPgUID(), out refPgItem);
            if (refPgItem == null && SearchEvent == true)
            {
                EpgEventInfo epgInfo = resInfo.ReserveEventInfo();
                if (epgInfo != null)
                {
                    EpgEventInfo epgRefInfo = epgInfo.GetGroupMainEvent(viewData.EventUIDList);
                    if (epgRefInfo != null)
                    {
                        programList.TryGetValue(epgRefInfo.CurrentPgUID(), out refPgItem);
                    }
                }
            }

            if (resInfo.IsEpgReserve == true && refPgItem != null && resInfo.DurationSecond != 0)
            {
                resItem.Height = Math.Max(refPgItem.Height * duration / resInfo.DurationSecond, ViewUtil.PanelMinimumHeight);
                resItem.TopPos = refPgItem.TopPos + Math.Min(refPgItem.Height - resItem.Height, refPgItem.Height * (-StartMargin) / resInfo.DurationSecond);
            }
            else
            {
                resItem.Height = Math.Max(duration * Settings.Instance.MinHeight / 60, ViewUtil.PanelMinimumHeight);
                resItem.TopPos = Settings.Instance.MinHeight * (index * 60 + (startTime - chkStartTime).TotalMinutes);
            }
            return resItem;
        }

        protected IEnumerable<ReserveData> CombinedReserveList()
        {
            return CommonManager.Instance.DB.RecFileInfo.Values
                    .Where(item => programList.ContainsKey(item.CurrentPgUID()) || item.EventID == 0xFFFF)
                    .Select(item => new ReserveDataEnd
                    {
                        ReserveID = item.ID,
                        StartTime = item.StartTime,
                        DurationSecond = item.DurationSecond,
                        OriginalNetworkID = item.OriginalNetworkID,
                        TransportStreamID = item.TransportStreamID,
                        ServiceID = item.ServiceID,
                        EventID = item.EventID,
                        //Title = item.Title,
                        //StationName = item.ServiceName,
                        //Comment = item.Comment,
                        //RecFileNameList = CommonUtil.ToList(item.RecFilePath),
                        //RecSetting.RecFolderList =,
                    }).Concat(CommonManager.Instance.DB.ReserveList.Values);
        }

        /// <summary>表示スクロールイベント呼び出し</summary>
        protected void epgProgramView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            programView.view_ScrollChanged(programView.scrollViewer, timeView.scrollViewer, horizontalViewScroll);
        }

        /// <summary>右ボタンクリック</summary>
        protected void button_erea_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            epgProgramView_RightClick(sender, new Point(-1, -1));
        }
        protected void epgProgramView_RightClick(object sender, Point cursorPos)
        {
            try
            {
                //右クリック表示メニューの作成
                clickPos = cursorPos;
                cmdMenu.Tag = viewMode;     //Viewの情報を与えておく
                mc.SupportContextMenuLoading(cmdMenu, null);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        public override int MoveToItem(UInt64 id, JumpItemStyle style = JumpItemStyle.MoveTo, bool dryrun = false)
        {
            ProgramViewItem target_item;
            programList.TryGetValue(id, out target_item);
            if (dryrun == false) programView.ScrollToFindItem(target_item, style);
            return target_item == null ? -1 : 0;
        }

        public override object MoveNextItem(int direction, UInt64 id = 0, bool move = true, JumpItemStyle style = JumpItemStyle.MoveTo)
        {
            if (programList.Count == 0) return null;

            var list = programList.Values.OrderBy(item => (int)(item.LeftPos / Settings.Instance.ServiceWidth) * 1e6 + item.TopPos + item.Width / Settings.Instance.ServiceWidth / 100).ToList();
            int idx = list.FindIndex(item => item.Data.CurrentPgUID() == id);
            idx = ViewUtil.GetNextIdx(ItemIdx, idx, list.Count, direction);
            if (move == true) programView.ScrollToFindItem(list[idx], style);
            if (move == true) ItemIdx = idx;
            return list[idx] == null ? null : list[idx].Data;
        }

        public override int MoveToReserveItem(ReserveData target, JumpItemStyle style = JumpItemStyle.MoveTo, bool dryrun = false)
        {
            if (target == null) return -1;
            int idx = reserveList.FindIndex(item => item.Data.ReserveID == target.ReserveID);
            if (idx != -1 && dryrun == false) programView.ScrollToFindItem(reserveList[idx], style);
            if (dryrun == false) ItemIdx = idx;
            return idx;
        }
        public override int MoveToProgramItem(EpgEventInfo target, JumpItemStyle style = JumpItemStyle.MoveTo, bool dryrun = false)
        {
            target = target == null ? null : target.GetGroupMainEvent(viewData.EventUIDList);
            return MoveToItem(target == null ? 0 : target.CurrentPgUID(), style, dryrun);
        }
        public virtual int MoveToRecInfoItem(RecFileInfo target, JumpItemStyle style = JumpItemStyle.MoveTo, bool dryrun = false)
        {
            if (target == null) return -1;
            int idx = recinfoList.FindIndex(item => item.Data.ReserveID == target.ID);
            if (idx != -1 && dryrun == false) programView.ScrollToFindItem(recinfoList[idx], style);
            if (dryrun == false) ItemIdx = idx;
            return idx;
        }

        protected int resIdx = -1;
        public override object MoveNextReserve(int direction, UInt64 id = 0, bool move = true, JumpItemStyle style = JumpItemStyle.MoveTo)
        {
            return ViewUtil.MoveNextReserve(ref resIdx, programView, reserveList, ref clickPos, id, direction, move, style);
        }

        protected int recIdx = -1;
        public override object MoveNextRecinfo(int direction, UInt64 id = 0, bool move = true, JumpItemStyle style = JumpItemStyle.MoveTo)
        {
            return MenuUtil.GetRecFileInfo(ViewUtil.MoveNextReserve(ref recIdx, programView, recinfoList, ref clickPos, id, direction, move, style) as ReserveDataEnd);
        }

        /// <summary>表示位置を現在の時刻にスクロールする</summary>
        protected void MoveNowTime()
        {
            MoveTime(RestoreState.time ?? GetViewTime(DateTime.UtcNow.AddHours(9)), RestoreState.time == null ? -120 : 0);
        }
        protected void MoveTime(DateTime time, int offset = 0)
        {
            int idx = timeList.BinarySearch(time.AddSeconds(1));
            double pos = ((idx < 0 ? ~idx : idx) - 1) * 60 * Settings.Instance.MinHeight + offset;
            programView.scrollViewer.ScrollToVerticalOffset(Math.Max(0, pos));
        }
        protected DateTime GetScrollTime()
        {
            if (timeList.Any() == false) return DateTime.MinValue;
            var idx = (int)(programView.scrollViewer.VerticalOffset / 60 / Settings.Instance.MinHeight);
            return timeList[Math.Max(0, Math.Min(idx, timeList.Count - 1))];
        }
        /// <summary>現在ライン表示</summary>
        protected virtual void ReDrawNowLine()
        {
            nowViewTimer.Stop();
            programView.nowLine.Visibility = Visibility.Hidden;
            if (this.IsVisible == false || timeList.Any() == false) return;

            DateTime nowTime = GetViewTime(DateTime.UtcNow.AddHours(9));
            int idx = timeList.BinarySearch(nowTime.Date.AddHours(nowTime.Hour));
            double posY = (idx < 0 ? ~idx * 60 : (idx * 60 + nowTime.Minute)) * Settings.Instance.MinHeight;

            programView.nowLine.X1 = 0;
            programView.nowLine.Y1 = posY;
            programView.nowLine.X2 = programView.epgViewPanel.Width;
            programView.nowLine.Y2 = posY;
            programView.nowLine.Visibility = Visibility.Visible;

            nowViewTimer.Interval = TimeSpan.FromSeconds(60 - nowTime.Second);
            nowViewTimer.Start();
        }

        protected override void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ReDrawNowLine();
            base.UserControl_IsVisibleChanged(sender, e);
        }
    }
}
