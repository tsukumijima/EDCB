using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    using TunerReserveViewCtrl;

    /// <summary>
    /// TunerReserveMainView.xaml の相互作用ロジック
    /// </summary>
    public partial class TunerReserveMainView : DataItemViewBase
    {
        private List<TunerReserveViewItem> reserveList = new List<TunerReserveViewItem>();
        private Point clickPos;

        private CmdExeReserve mc; //予約系コマンド集
        private ContextMenu cmdMenu = new ContextMenuEx() { Tag = -1 };//tunerReserveView.Contextmenu使うとフォーカスおかしくなる‥。

        public TunerReserveMainView()
        {
            InitializeComponent();

            tunerReserveView.ScrollChanged += new ScrollChangedEventHandler(tunerReserveView_ScrollChanged);
            tunerReserveView.LeftDoubleClick += (sender, cursorPos) => EpgCmds.ShowDialog.Execute(null, cmdMenu);
            tunerReserveView.MouseClick += (sender, cursorPos) => clickPos = cursorPos;
            tunerReserveView.RightClick += new TunerReserveView.PanelViewClickHandler(tunerReserveView_RightClick);
            button_now.Click += (sender, e) => tunerReserveView.scrollViewer.ScrollToVerticalOffset(0);

            //ビューコードの登録
            mBinds.View = CtxmCode.TunerReserveView;

            //最初にコマンド集の初期化
            mc = new CmdExeReserve(this);
            mc.SetFuncGetDataList(isAll => isAll == true ? reserveList.GetDataList() : reserveList.GetHitDataList(clickPos));

            //コマンド集からコマンドを登録
            mc.ResetCommandBindings(this, cmdMenu);

            //予約をたどるショートカットの登録。こちらはコマンドで問題無いが、番組表側と揃えておく。
            this.PreviewKeyDown += (sender, e) => ViewUtil.OnKeyMoveNextReserve(sender, e, this);
        }
        public void RefreshMenu()
        {
            mc.EpgInfoOpenMode = Settings.Instance.TunerEpgInfoOpenMode;
            mBinds.ResetInputBindings(this, tunerReserveView.scrollViewer);
            mBinds.ResetInputBindings(tunerReserveTimeView.scrollViewer, tunerReserveNameView.scrollViewer);
            mm.CtxmGenerateContextMenu(cmdMenu, CtxmCode.TunerReserveView, false);
        }
        public void RefreshView()
        {
            tunerReserveView.reserveViewPanel.Background = Settings.BrushCache.TunerBackColor;
            tunerReserveTimeView.Background = Settings.BrushCache.TunerTimeBorderColor;
            tunerReserveNameView.Background = Settings.BrushCache.TunerNameBorderColor;
        }

        /// <summary>表示スクロールイベント呼び出し</summary>
        void tunerReserveView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            tunerReserveView.view_ScrollChanged(tunerReserveView.scrollViewer, tunerReserveTimeView.scrollViewer, tunerReserveNameView.scrollViewer);
        }

        /// <summary>右ボタンクリック</summary>
        protected void sub_erea_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            tunerReserveView_RightClick(sender, new Point(-1, -1));
        }
        void tunerReserveView_RightClick(object sender, Point cursorPos)
        {
            //右クリック表示メニューの作成
            clickPos = cursorPos;
            mc.SupportContextMenuLoading(cmdMenu, null);
        }

        protected override void UpdateStatusData(int mode = 0)
        {
            this.status[1] = ViewUtil.ConvertReserveStatus(reserveList.GetDataList(), "予約数", 3);
        }
        protected override bool ReloadInfoData()
        {
            ReloadReserveViewItem();
            return true;
        }
        /// <summary>
        /// 予約情報の再描画
        /// </summary>
        private void ReloadReserveViewItem()
        {
            try
            {
                tunerReserveView.ClearInfo();
                tunerReserveTimeView.ClearInfo();
                tunerReserveNameView.ClearInfo();
                reserveList.Clear();

                var tunerList = new List<PanelItem<TunerReserveInfo>>();
                var timeSet = new HashSet<DateTime>();

                List<TunerReserveInfo> tunerReserveList = CommonManager.Instance.DB.TunerReserveList.Values
                    .OrderBy(info => info.tunerID).ToList();//多分大丈夫だけど一応ソートしておく
                if (Settings.Instance.TunerDisplayOffReserve == true)
                {
                    var tuner_off = new TunerReserveInfo();
                    tuner_off.tunerID = 0xFFFFFFFF;//IDの表示判定に使っている
                    tuner_off.tunerName = "無効予約";
                    tuner_off.reserveList = CommonManager.Instance.DB.ReserveList.Values
                        .Where(info => info.IsEnabled == false).Select(info => info.ReserveID).ToList();
                    tunerReserveList.Add(tuner_off);
                }

                //チューナ不足と無効予約はアイテムがなければ非表示
                tunerReserveList.RemoveAll(item => item.tunerID == 0xFFFFFFFF && item.reserveList.Count == 0);

                double singleWidth = Settings.Instance.TunerWidth;
                double leftPos = 0;
                var resDic = CommonManager.Instance.DB.ReserveList;
                tunerReserveList.ForEach(info =>
                {
                    var cols = new List<List<ReserveViewItem>>();
                    foreach (ReserveData resInfo in info.reserveList.Where(id => resDic.ContainsKey(id) == true).Select(id => resDic[id]).OrderBy(res => res.Create64Key()))//.ThenBy(res => res.StartTimeActual))
                    {
                        var newItem = new TunerReserveViewItem(resInfo) { Width = singleWidth };
                        reserveList.Add(newItem);

                        //横位置の設定・列を拡げて表示する処置
                        var addCol = cols.FindIndex(col => col.All(item =>
                            MenuUtil.CulcOverlapLength(resInfo.StartTime, resInfo.DurationSecond, item.Data.StartTime, item.Data.DurationSecond) <= 0));
                        if (addCol < 0)
                        {
                            addCol = cols.Count;
                            cols.Add(new List<ReserveViewItem>());
                        }
                        cols[addCol].Add(newItem);
                        newItem.LeftPos = leftPos + addCol * singleWidth;

                        //マージン込みの時間でリストを構築
                        ViewUtil.AddTimeList(timeSet, resInfo.StartTimeActual, resInfo.DurationActual);
                    }
                    double tunerWidth = singleWidth * Math.Max(1, cols.Count);
                    tunerList.Add(new PanelItem<TunerReserveInfo>(info) { Width = tunerWidth });
                    leftPos += tunerWidth;
                });

                //縦位置の設定
                var timeList = new List<DateTime>(timeSet.OrderBy(time => time));

                reserveList.ForEach(item =>
                {
                    ViewUtil.SetItemVerticalPos(timeList, item, item.Data.StartTimeActual, item.Data.DurationActual, Settings.Instance.TunerMinHeight, true);

                    //ごく小さいマージンの表示を抑制する。
                    item.TopPos = Math.Round(item.TopPos);
                    item.Height = Math.Round(item.Height);
                });

                //最低表示行数を適用。また、最低表示高さを確保して、位置も調整する。
                ViewUtil.ModifierMinimumLine(reserveList, Settings.Instance.TunerMinimumLine, Settings.Instance.TunerFontSizeService, Settings.Instance.TunerBorderTopSize);

                //必要時間リストの修正。最低表示行数の適用で下に溢れた分を追加する。
                ViewUtil.AdjustTimeList(reserveList, timeList, Settings.Instance.TunerMinHeight);

                tunerReserveTimeView.SetTime(timeList, false, true);
                tunerReserveNameView.SetTunerInfo(tunerList);
                tunerReserveView.SetReserveList(reserveList,
                    leftPos,
                    timeList.Count * 60 * Settings.Instance.TunerMinHeight);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        protected override void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            base.UserControl_IsVisibleChanged(sender, e);

            if (this.IsVisible == false) return;

            if (BlackoutWindow.HasReserveData == true)
            {
                MoveToItem(BlackoutWindow.SelectedItem.ReserveInfo.ReserveID, BlackoutWindow.NowJumpTable == true ? JumpItemStyle.JumpTo : JumpItemStyle.None);
            }

            BlackoutWindow.Clear();
        }

        public override int MoveToItem(ulong id, JumpItemStyle style = JumpItemStyle.MoveTo, bool dryrun = false)
        {
            int idx = reserveList.FindIndex(item => item.Data.ReserveID == id);
            if (idx != -1 && dryrun == false) tunerReserveView.ScrollToFindItem(reserveList[idx], style);
            if (dryrun == false) ItemIdx = idx;
            return idx;
        }
        public override object MoveNextItem(int direction, ulong id = 0, bool move = true, JumpItemStyle style = JumpItemStyle.MoveTo)
        {
            return ViewUtil.MoveNextReserve(ref itemIdx, tunerReserveView, reserveList, ref clickPos, id, direction, move, style);
        }
        public override object MoveNextReserve(int direction, ulong id = 0, bool move = true, JumpItemStyle style = JumpItemStyle.MoveTo)
        {
            return MoveNextItem(direction, id, move, style);
        }
    }
}
