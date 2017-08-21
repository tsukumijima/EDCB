using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// RecInfoDescWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RecInfoDescWindow : RecInfoDescWindowBase
    {
        protected override UInt64 DataID { get { return recInfo == null ? 0 : recInfo.ID; } }
        protected override IEnumerable<KeyValuePair<UInt64, object>> DataRefList { get { return CommonManager.Instance.DB.RecFileInfo.Select(d => new KeyValuePair<UInt64, object>(d.Key, d.Value)); } }
        protected override DataItemViewBase DataView { get { return mainWindow.recInfoView; } }

        private RecFileInfo recInfo = null;
        private CmdExeRecinfo mc;

        public RecInfoDescWindow(RecFileInfo info = null)
        {
            InitializeComponent();

            try
            {
                base.SetParam(false, checkBox_windowPinned, checkBox_dataReplace);

                //最初にコマンド集の初期化
                mc = new CmdExeRecinfo(this);
                mc.SetFuncGetDataList(isAll => CommonUtil.ToList(recInfo));

                //コマンド集に無いもの,変更するもの
                mc.AddReplaceCommand(EpgCmds.Play, (sender, e) => CommonManager.Instance.FilePlay(recInfo.RecFilePath), (sender, e) => e.CanExecute = recInfo != null);
                mc.AddReplaceCommand(EpgCmds.Cancel, (sender, e) => this.Close());
                mc.AddReplaceCommand(EpgCmds.BackItem, (sender, e) => MoveViewNextItem(-1));
                mc.AddReplaceCommand(EpgCmds.NextItem, (sender, e) => MoveViewNextItem(1));
                mc.AddReplaceCommand(EpgCmds.DeleteInDialog, info_del, (sender, e) => e.CanExecute = recInfo != null && recInfo.ProtectFlag == 0);
                mc.AddReplaceCommand(EpgCmds.ChgOnOffCheck, (sender, e) => EpgCmds.ProtectChange.Execute(null, this));

                //コマンド集からコマンドを登録
                mc.ResetCommandBindings(this);

                //ボタンの設定
                mBinds.View = CtxmCode.RecInfoView;
                mBinds.SetCommandToButton(button_play, EpgCmds.Play);
                mBinds.SetCommandToButton(button_cancel, EpgCmds.Cancel);
                mBinds.SetCommandToButton(button_up, EpgCmds.BackItem);
                mBinds.SetCommandToButton(button_down, EpgCmds.NextItem);
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
            if (recInfo == null) return false;

            CommonManager.Instance.DB.ReloadRecFileInfo();
            CommonManager.Instance.DB.RecFileInfo.TryGetValue(recInfo.ID, out recInfo);
            ChangeData(recInfo);
            return true;
        }

        public override void ChangeData(object data)
        {
            var info = data as RecFileInfo;
            chk_key.DataContext = new RecInfoItem(info);
            label_Msg.Visibility = info != null ? Visibility.Hidden : Visibility.Visible;
            if (info == null) return;

            recInfo = info;
            this.Title = ViewUtil.WindowTitleText(recInfo.Title, "録画情報");
            textBox_pgInfo.Text = recInfo.ProgramInfo;
            textBox_errLog.Text = recInfo.ErrInfo;
        }

        private void info_del(object sender, ExecutedRoutedEventArgs e)
        {
            EpgCmds.Delete.Execute(e.Parameter, this);
            if (mc.IsCommandExecuted == true) MoveViewNextItem(1);
        }
    }
    public class RecInfoDescWindowBase : AttendantDataWindow<RecInfoDescWindow> { }
}
