using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EpgTimer.EpgView
{
    public class EpgViewData
    {
        //表示形式間で番組表定義と番組リストを共有する
        //EpgTimerNWで検索絞り込みを使用時に多少効果があるくらいだが‥
        public EpgViewData()
        {
            EpgTabInfo = new CustomEpgTabInfo();
            ClearEventList();
        }
        public CustomEpgTabInfo EpgTabInfo { get; set; }
        private bool rloadEpgDataFlg;
        public List<EpgServiceEventInfo> ServiceEventList { get; private set; }
        public void ClearEventList() { ServiceEventList = new List<EpgServiceEventInfo>(); rloadEpgDataFlg = true; }

        public bool ReloadEpgData()
        {
            try
            {
                if (rloadEpgDataFlg == false) return true;
                if (CommonManager.Instance.IsConnected == false) return false;

                Dictionary<UInt64, EpgServiceAllEventInfo> serviceDic;
                if (EpgTabInfo.SearchMode == false)
                {
                    ErrCode err = CommonManager.Instance.DB.ReloadEpgData();
                    if (CommonManager.CmdErrMsgTypical(err, "EPGデータの取得", err == ErrCode.CMD_ERR_BUSY ?
                                                            "EPGデータの読み込みを行える状態ではありません。\r\n(EPGデータ読み込み中など)" :
                                                            "エラーが発生しました。\r\nEPGデータが読み込まれていない可能性があります。") == false) return false;
                    serviceDic = CommonManager.Instance.DB.ServiceEventList;
                }
                else
                {
                    //番組情報の検索
                    var list = new List<EpgEventInfo>();
                    ErrCode err = CommonManager.CreateSrvCtrl().SendSearchPg(CommonUtil.ToList(EpgTabInfo.GetSearchKeyReloadEpg()), ref list);
                    if (CommonManager.CmdErrMsgTypical(err, "EPGデータの取得") == false) return false;

                    //サービス毎のリストに変換
                    serviceDic = list.GroupBy(info => info.Create64Key())
                        .Where(gr => ChSet5.ChList.ContainsKey(gr.Key) == true)
                        .ToDictionary(gr => gr.Key, gr => new EpgServiceAllEventInfo(ChSet5.ChList[gr.Key].ToInfo(), gr.ToList()));
                }

                //並び順はViewServiceListによる。eventListはこの後すぐ作り直すのでとりあえずそのままもらう。
                ServiceEventList = EpgTabInfo.ViewServiceList.Distinct()
                    .Where(id => serviceDic.ContainsKey(id) == true).Select(id => serviceDic[id])
                    .Select(info => new EpgServiceEventInfo { serviceInfo = info.serviceInfo, eventList = info.eventMergeList }).ToList();

                var keyTime = DateTime.UtcNow.AddHours(9).AddDays(-Settings.Instance.EpgNoDisplayOldDays);
                var viewContentMatchingHash = new HashSet<UInt32>(EpgTabInfo.ViewContentList.Select(d => d.MatchingKeyList).SelectMany(x => x));
                foreach (EpgServiceEventInfo item in ServiceEventList)
                {
                    item.eventList = item.eventList.FindAll(eventInfo =>
                        //開始時間未定を除外
                        (eventInfo.StartTimeFlag != 0)

                        //自動登録されたりするので、サービス別番組表では表示させる
                            //&& (eventInfo.IsGroupMainEvent == true)

                        //過去番組表示抑制
                        && (Settings.Instance.EpgNoDisplayOld == false || eventInfo.IsOver(keyTime) == false)

                        //ジャンル絞り込み
                        && (ViewUtil.ContainsContent(eventInfo, viewContentMatchingHash, EpgTabInfo.ViewNotContentFlag) == true)
                    );
                }

                rloadEpgDataFlg = false;
                return true;
            }
            catch (Exception ex) { CommonUtil.DispatcherMsgBoxShow(ex.Message + "\r\n" + ex.StackTrace); }
            return false;
        }
    }

    public class EpgViewState { public int viewMode; }

    public class EpgViewBase : DataItemViewBase
    {
        public static event ViewUpdatedHandler ViewReserveUpdated = null;

        protected CmdExeReserve mc; //予約系コマンド集
        protected bool ReloadReserveInfoFlg = true;
        protected bool RefreshMenuFlg = true;

        protected EpgViewState restoreState = null;
        public virtual EpgViewState GetViewState() { return null; }
        public virtual void SetViewState(EpgViewState data) { restoreState = data; }

        //表示形式間で番組表定義と番組リストを共有する
        //EpgTimerNWで検索絞り込みを使用時に多少効果があるくらいだが‥
        protected EpgViewData viewData = new EpgViewData();
        protected int viewMode = 0;//最初に設定した後は固定するコード。
        public void SetViewData(EpgViewData data) { viewData = data; viewMode = data.EpgTabInfo.ViewMode; }
        protected CustomEpgTabInfo viewInfo { get { return viewData.EpgTabInfo; } }
        protected virtual bool viewCustNeedTimeOnly { get { return viewInfo.NeedTimeOnlyBasic; } }
        protected List<EpgServiceEventInfo> serviceEventList { get { return viewData.ServiceEventList; } }

        protected virtual void InitCommand()
        {
            //ビューコードの登録
            mBinds.View = CtxmCode.EpgView;

            //コマンド集の初期化
            mc = new CmdExeReserve(this);

            //コマンド集にないものを登録
            mc.AddReplaceCommand(EpgCmds.ViewChgSet, (sender, e) => ViewSetting(-1));
            mc.AddReplaceCommand(EpgCmds.ViewChgReSet, (sender, e) => ViewSetting(-2));
            mc.AddReplaceCommand(EpgCmds.ViewChgMode, mc_ViewChgMode);

            //コマンド集を振り替えるもの
            mc.AddReplaceCommand(EpgCmds.JumpTable, mc_JumpTable);
        }

        //表示設定関係
        public event Action<EpgViewBase, int> ViewSettingClick = (sender, info) => { };
        protected void ViewSetting(int param) { ViewSettingClick(this, param); }
        protected void mc_ViewChgMode(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var param = e.Parameter as EpgCmdParam;
                if (param == null || param.ID == viewMode) return;

                //BlackWindowに状態を登録。
                //コマンド集の機能による各ビューの共用メソッド。
                BlackoutWindow.SelectedData = mc.GetJumpTabItem();

                ViewSetting(param.ID);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }
        protected void mc_JumpTable(object sender, ExecutedRoutedEventArgs e)
        {
            var param = e.Parameter as EpgCmdParam;
            if (param == null) return;

            param.ID = 0;//実際は設定するまでもなく、初期値0。
            BlackoutWindow.NowJumpTable = true;
            new BlackoutWindow(ViewUtil.MainWindow).showWindow(ViewUtil.MainWindow.tabItem_epg.Header.ToString());

            mc_ViewChgMode(sender, e);
        }

        public void UpdateMenu(bool refesh = true)
        {
            RefreshMenuFlg |= refesh;
            RefreshMenu();
        }
        protected void RefreshMenu()
        {
            if (RefreshMenuFlg == true && this.IsVisible == true)
            {
                RefreshMenuInfo();
                RefreshMenuFlg = false;
            }
        }
        protected virtual void RefreshMenuInfo()
        {
            mc.EpgInfoOpenMode = Settings.Instance.EpgInfoOpenMode;
        }

        /// 保存関係
        public virtual void SaveViewData() { }

        /// <summary>
        /// 予約情報更新通知
        /// </summary>
        public void UpdateReserveInfo(bool reload = true)
        {
            ReloadReserveInfoFlg |= reload;
            ReloadReserveInfo();
        }
        protected void ReloadReserveInfo()
        {
            if (ReloadReserveInfoFlg == true && this.IsVisible == true)
            {
                ReloadReserveInfoFlg = !ReloadReserveInfoData();
                if (ViewReserveUpdated != null) ViewReserveUpdated(this, true);
                UpdateStatus();
            }
        }
        protected bool ReloadReserveInfoData()
        {
            ReloadReserveViewItem();
            return true;
        }
        protected virtual void ReloadReserveViewItem() { }

        /// <summary>EPGデータ更新</summary>
        protected override bool ReloadInfoData()
        {
            if (viewData.ReloadEpgData() == false) return false;
            ReloadReserveInfoFlg = true;
            ReloadProgramViewItem();
            if (ReloadReserveInfoFlg == true) ReloadReserveInfoFlg = !ReloadReserveInfoData();
            restoreState = null;
            return true;
        }
        protected virtual void ReloadProgramViewItem() { }

        protected override void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible == false) return;

            RefreshMenu();
            ReloadInfo();
            ReloadReserveInfo();//ReloadInfo()が実行された場合は、こちらは素通りになる。

            //「番組表へジャンプ」の場合、またはオプションで指定のある場合に強調表示する。
            var isMarking = (BlackoutWindow.NowJumpTable || Settings.Instance.DisplayNotifyEpgChange) ? JumpItemStyle.JumpTo : JumpItemStyle.None;
            if (BlackoutWindow.HasReserveData == true)
            {
                MoveToReserveItem(BlackoutWindow.SelectedItem.ReserveInfo, isMarking);
            }
            else if (BlackoutWindow.HasProgramData == true)
            {
                MoveToProgramItem(BlackoutWindow.SelectedItem.EventInfo, isMarking);
            }
            BlackoutWindow.Clear();

            RefreshStatus();
        }
    }
}
