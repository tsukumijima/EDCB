using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    public class CmdExeRecinfo : CmdExe<RecFileInfo>
    {
        public CmdExeRecinfo(UIElement owner) : base(owner) { }
        protected override void mc_ShowDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenRecInfoDialog(dataList[0]);
        }
        protected override void mc_ProtectChange(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = MenuUtil.RecinfoChgProtect(dataList);
        }
        protected override void mc_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            dataList = dataList.GetNoProtectedList();
            if (mcs_DeleteCheck(e) == false || mcs_DeleteCheckDelFile(dataList) == false) return;
            IsCommandExecuted = MenuUtil.RecinfoDelete(dataList);
        }
        public static bool mcs_DeleteCheckDelFile(IEnumerable<RecFileInfo> list)
        {
            if (Settings.Instance.ConfirmDelRecInfoFileDelete && list.Any(info => info.RecFilePath.Length > 0)
                && IniFileHandler.GetPrivateProfileInt("SET", "RecInfoDelFile", 0, SettingPath.CommonIniPath) == 1)
            {
                return (MessageBox.Show("録画ファイルが存在する場合は一緒に削除されます。\r\nよろしいですか?",
                    "ファイル削除", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK);
            }
            return true;
        }
        protected override void mc_Play(object sender, ExecutedRoutedEventArgs e)
        {
            CommonManager.Instance.FilePlay(dataList[0].RecFilePath);
            IsCommandExecuted = true;
        }
        protected override void mc_CopyContent(object sender, ExecutedRoutedEventArgs e)
        {
            MenuUtil.CopyContent2Clipboard(dataList[0], CmdExeUtil.IsKeyGesture(e));
            IsCommandExecuted = true;
        }
        protected override SearchItem mcs_GetSearchItem()
        {
            if (dataList.Count == 0) return null;

            EpgEventInfo info = null;
            if (dataList[0].StartTime + TimeSpan.FromSeconds(dataList[0].DurationSecond)
                    >= DateTime.UtcNow.AddHours(9) - TimeSpan.FromDays((Settings.Instance.EpgNoDisplayOld == true ? Settings.Instance.EpgNoDisplayOldDays : 15)))
            {
                info = new EpgEventInfo();
                info.original_network_id = dataList[0].OriginalNetworkID;
                info.transport_stream_id = dataList[0].TransportStreamID;
                info.service_id = dataList[0].ServiceID;
                info.event_id = dataList[0].EventID;
                info.start_time = dataList[0].StartTime;
                info.StartTimeFlag = 1;
            }
            return info == null ? null : new SearchItem(info);
        }
        protected override void mcs_ctxmLoading_switch(ContextMenu ctxm, MenuItem menu)
        {
            if (menu.Tag == EpgCmds.Delete || menu.Tag == EpgCmds.DeleteAll)
            {
                menu.IsEnabled = dataList.HasNoProtected();
            }
            else if (menu.Tag == EpgCmds.JumpTable)
            {
                mcs_ctxmLoading_jumpTabEpg(menu);
            }
            else if (menu.Tag == EpgCmdsEx.ShowAutoAddDialogMenu)
            {
                menu.IsEnabled = mm.CtxmGenerateChgAutoAdd(menu, dataList.Count != 0 ? dataList[0] : null);
            }
            else if (menu.Tag == EpgCmds.OpenFolder)
            {
                string path = (dataList.Count == 0 ? null : dataList[0].RecFilePath);
                (menu.CommandParameter as EpgCmdParam).Data = path;
                menu.ToolTip = path;
            }
        }
    }
}
