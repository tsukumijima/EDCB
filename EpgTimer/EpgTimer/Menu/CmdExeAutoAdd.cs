using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    //キーワード予約、プログラム自動登録の共通メソッド
    public class CmdExeAutoAdd<T> : CmdExe<T> where T : AutoAddData, new()
    {
        public CmdExeAutoAdd(UIElement owner) : base(owner) { }
        protected override void mc_ChangeKeyEnabled(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = MenuUtil.AutoAddChangeKeyEnabled(dataList, CmdExeUtil.ReadIdData(e, 0, 1) == 0);
        }
        protected override void mc_ChangeOnOffKeyEnabled(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = MenuUtil.AutoAddChangeKeyEnabled(dataList);
        }
        protected override void mc_ChangeRecSetting(object sender, ExecutedRoutedEventArgs e)
        {
            if (mcc_chgRecSetting(e) == false) return;
            IsCommandExecuted = MenuUtil.AutoAddChange(dataList);
        }
        protected override void mc_ChgBulkRecSet(object sender, ExecutedRoutedEventArgs e)
        {
            if (MenuUtil.ChangeBulkSet(dataList, this.Owner, typeof(T) == typeof(ManualAutoAddData)) == false) return;
            IsCommandExecuted = MenuUtil.AutoAddChange(dataList);
        }
        protected override void mcs_CopyItem(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = MenuUtil.AutoAddAdd(dataList);
        }
        protected override void mcs_CopyItemDialog(object sender, ExecutedRoutedEventArgs e)
        {
            dataList[0].DataID = 0;
            mc_ShowDialog(sender, e);
        }
        protected override void mc_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (mcs_DeleteCheck(e) == false) return;
            IsCommandExecuted = MenuUtil.AutoAddDelete(dataList);
        }
        protected override void mc_Delete2(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.CheckAllProcCancel(e, dataList, true) == true) return;
            IsCommandExecuted = MenuUtil.AutoAddDelete(dataList, true, true);
        }
        protected override void mc_AdjustReserve(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.CheckAllProcCancel(e, dataList, false) == true) return;
            IsCommandExecuted = MenuUtil.AutoAddChangeSyncReserve(dataList);
        }
        protected override ReserveData mcs_GetNextReserve()
        {
            if (dataList.Count == 0) return null;

            ReserveData resinfo = dataList[0].GetNextReserve();
            return resinfo != null ? resinfo : dataList[0].GetReserveList().GetNextReserve(true);
        }
        protected override void mc_SetRecTag(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.CheckSetFromClipBoardCancel(e, dataList, "録画タグ") == true) return;
            IsCommandExecuted = MenuUtil.AutoAddChangeRecTag(dataList, Clipboard.GetText());
        }
        protected override void mcs_ctxmLoading_switch(ContextMenu ctxm, MenuItem menu)
        {
            if (menu.Tag == EpgCmdsEx.ChgMenu)
            {
                mcs_chgMenuOpening(menu);
            }
            else if (menu.Tag == EpgCmds.JumpReserve || menu.Tag == EpgCmds.JumpTuner || menu.Tag == EpgCmds.JumpTable)
            {
                mcs_ctxmLoading_jumpTabRes(menu, "次の無効予約へジャンプ");
                if (menu.Tag == EpgCmds.JumpTable && menu.IsEnabled == true)
                {
                    mcs_ctxmLoading_jumpTabEpg(menu);
                }
            }
            else if (menu.Tag == EpgCmdsEx.ShowReserveDialogMenu)
            {
                menu.IsEnabled = mm.CtxmGenerateShowReserveDialogMenuItems(menu, dataList);
            }
            else if (menu.Tag == EpgCmdsEx.OpenFolderMenu)
            {
                mm.CtxmGenerateOpenFolderItems(menu, dataList.Count == 0 ? null : dataList[0].RecSettingInfo);
            }
        }
    }

    //プログラム自動登録の固有メソッド
    public class CmdExeManualAutoAdd : CmdExeAutoAdd<ManualAutoAddData>
    {
        public CmdExeManualAutoAdd(UIElement owner) : base(owner) { }
        protected override void mc_ShowDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenChangeManualAutoAddDialog(dataList[0]);
        }
        protected override void mc_ShowAddDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenAddManualAutoAddDialog();
        }
    }

    //キーワード予約の固有メソッド
    public class CmdExeEpgAutoAdd : CmdExeAutoAdd<EpgAutoAddData>
    {
        public CmdExeEpgAutoAdd(UIElement owner) : base(owner) { }
        protected override void mc_ShowDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenChangeEpgAutoAddDialog(dataList[0]);
        }
        protected override void mc_ShowAddDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenAddEpgAutoAddDialog();
        }
        protected override void mc_ToAutoadd(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenAddEpgAutoAddDialog(dataList[0].DataTitle);
        }
        protected override void mc_ChgGenre(object sender, ExecutedRoutedEventArgs e)
        {
            if (MenuUtil.ChgGenre(dataList.RecSearchKeyList(), this.Owner) == false) return;
            IsCommandExecuted = MenuUtil.AutoAddChange(dataList);
        }
        protected override void mc_CopyNotKey(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetDataObject(dataList[0].searchInfo.notKey);
            IsCommandExecuted = true;
        }
        protected override void mc_SetNotKey(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.CheckSetFromClipBoardCancel(e, dataList, "Notキーワード") == true) return;
            IsCommandExecuted = MenuUtil.EpgAutoAddChangeNotKey(dataList);
        }
        protected override void mc_CopyNote(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetDataObject(dataList[0].searchInfo.note);
            IsCommandExecuted = true;
        }
        protected override void mc_SetNote(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.CheckSetFromClipBoardCancel(e, dataList, "メモ欄") == true) return;
            IsCommandExecuted = MenuUtil.EpgAutoAddChangeNote(dataList);
        }
        protected override void mcs_ctxmLoading_edit_tooltip(MenuItem menu)
        {
            base.mcs_ctxmLoading_edit_tooltip(menu);
            if (menu.Tag == EpgCmds.ToAutoadd) menu.ToolTip = null;
        }
    }
}
