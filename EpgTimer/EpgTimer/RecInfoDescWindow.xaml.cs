using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EpgTimer
{
    using EpgView;

    /// <summary>
    /// RecInfoDescWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RecInfoDescWindow : RecInfoDescWindowBase
    {
        protected override UInt64 DataID { get { return recInfo.ID; } }
        protected override IEnumerable<KeyValuePair<UInt64, object>> DataRefList { get { return CommonManager.Instance.DB.RecFileInfo.Select(d => new KeyValuePair<UInt64, object>(d.Key, d.Value)); } }
        protected override DataItemViewBase DataView { get { return base.DataView ?? mainWindow.recInfoView; } }

        private RecFileInfo recInfo = new RecFileInfo();
        private CmdExeRecinfo mc;

        public RecInfoDescWindow(RecFileInfo info = null)
        {
            InitializeComponent();

            try
            {
                base.SetParam(false, checkBox_windowPinned, checkBox_dataReplace);

                //最初にコマンド集の初期化
                mc = new CmdExeRecinfo(this);
                mc.SetFuncGetDataList(isAll => recInfo.IntoList());

                //コマンド集に無いもの,変更するもの
                mc.AddReplaceCommand(EpgCmds.Play, (sender, e) => CommonManager.Instance.FilePlay(recInfo.RecFilePath), (sender, e) => e.CanExecute = recInfo.ID != 0);
                mc.AddReplaceCommand(EpgCmds.Cancel, (sender, e) => this.Close());
                mc.AddReplaceCommand(EpgCmds.BackItem, (sender, e) => MoveViewNextItem(-1));
                mc.AddReplaceCommand(EpgCmds.NextItem, (sender, e) => MoveViewNextItem(1));
                mc.AddReplaceCommand(EpgCmds.Search, (sender, e) => MoveViewRecinfoTarget(), (sender, e) => e.CanExecute = DataView is EpgViewBase);
                mc.AddReplaceCommand(EpgCmds.DeleteInDialog, info_del, (sender, e) => e.CanExecute = recInfo.ID != 0 && recInfo.ProtectFlag == 0);
                mc.AddReplaceCommand(EpgCmds.ChgOnOffCheck, (sender, e) => EpgCmds.ProtectChange.Execute(null, this));

                //コマンド集からコマンドを登録
                mc.ResetCommandBindings(this);

                //ボタンの設定
                mBinds.View = CtxmCode.RecInfoView;
                mBinds.SetCommandToButton(button_play, EpgCmds.Play);
                mBinds.SetCommandToButton(button_cancel, EpgCmds.Cancel);
                mBinds.SetCommandToButton(button_up, EpgCmds.BackItem);
                mBinds.SetCommandToButton(button_down, EpgCmds.NextItem);
                mBinds.SetCommandToButton(button_chk, EpgCmds.Search);
                mBinds.SetCommandToButton(button_del, EpgCmds.DeleteInDialog);
                mBinds.AddInputCommand(EpgCmds.ProtectChange);//ショートカット登録
                RefreshMenu();

                button_del.ToolTipOpening += (sender, e) => button_del.ToolTip = (button_del.ToolTip as string +
                        (IniFileHandler.GetPrivateProfileBool("SET", "RecInfoDelFile", false, SettingPath.CommonIniPath) ?
                        "\r\n録画ファイルが存在する場合は一緒に削除されます。" : "")).Trim();

                grid_protect.ToolTipOpening += (sender, e) => grid_protect.ToolTip =
                        ("" + MenuBinds.GetInputGestureTextView(EpgCmds.ProtectChange, mBinds.View) + "\r\nプロテクト設定/解除").Trim();

                //ステータスバーの設定
                this.statusBar.Status.Visibility = Visibility.Collapsed;
                StatusManager.RegisterStatusbar(this.statusBar, this);

                ChangeData(info);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        protected override bool ReloadInfoData()
        {
            if (recInfo.ID == 0) return false;

            RecFileInfo info;
            CommonManager.Instance.DB.RecFileInfo.TryGetValue(recInfo.ID, out info);
            if (info == null) recInfo.ID = 0;
            ChangeData(info ?? recInfo);
            return true;
        }

        public override void ChangeData(object data)
        {
            var info = data as RecFileInfo ?? new RecFileInfo();//nullデータを受け付ける
            DataContext = new RecInfoItem(info);

            //Appendデータが無くなる場合を考慮し、テキストはrecInfoと連動させない
            if (recInfo != data)
            {
                recInfo = info;
                this.Title = ViewUtil.WindowTitleText(recInfo.Title, "録画情報");
                if (recInfo.ID != 0 && recInfo.ProgramInfo == null)//.program.txtがない
                {
                    recInfo.ProgramInfo = CommonManager.ConvertProgramText(recInfo.GetPgInfo(), EventInfoTextMode.All);
                }
                textBox_pgInfo.Text = recInfo.ProgramInfo;
                textBox_errLog.Text = recInfo.ErrInfo;
            }
            UpdateViewSelection(0);
        }

        private void info_del(object sender, ExecutedRoutedEventArgs e)
        {
            EpgCmds.Delete.Execute(e.Parameter, this);
            if (mc.IsCommandExecuted == true) MoveViewNextItem(1);
        }

        protected override void UpdateViewSelection(int mode = 0)
        {
            //番組表では「前へ」「次へ」の移動の時だけ追従させる。mode=2はアクティブ時の自動追尾
            var style = JumpItemStyle.MoveTo | (mode < 2 ? JumpItemStyle.PanelNoScroll : JumpItemStyle.None);
            if (DataView is RecInfoView)
            {
                if (mode != 0) DataView.MoveToItem(DataID, style);
            }
            else if (DataView is EpgMainViewBase)
            {
                if (mode != 2) ((EpgMainViewBase)DataView).MoveToRecInfoItem(recInfo, style);
            }
            else if (DataView is EpgListMainView)
            {
                if (mode != 0 && mode != 2) DataView.MoveToRecInfoItem(recInfo, style);
            }
            else if (DataView is SearchWindow.AutoAddWinListView)
            {
                if (mode != 0) DataView.MoveToRecInfoItem(recInfo, style);
            }
        }
        private void MoveViewRecinfoTarget()
        {
            //一覧以外では「前へ」「次へ」の移動の時に追従させる
            if (DataView is EpgViewBase)
            {
                //BeginInvokeはフォーカス対応
                MenuUtil.CheckJumpTab(new ReserveItem(recInfo.ToReserveData()), true);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    DataView.MoveToRecInfoItem(recInfo);
                }), DispatcherPriority.Loaded);
            }
            else
            {
                UpdateViewSelection(3);
            }
        }
        protected override void MoveViewNextItem(int direction, bool toRefData = false)
        {
            object NewData = null;
            if (DataView is EpgViewBase || DataView is SearchWindow.AutoAddWinListView)
            {
                NewData = DataView.MoveNextRecinfo(direction, recInfo.CurrentPgUID(), true, JumpItemStyle.MoveTo);
                if (NewData is RecFileInfo)
                {
                    ChangeData(NewData);
                    return;
                }
                toRefData = true;
            }
            base.MoveViewNextItem(direction, toRefData);
        }
    }
    public class RecInfoDescWindowBase : ReserveWindowBase<RecInfoDescWindow> { }
}
