﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
    
using CtrlCmdCLI;
using CtrlCmdCLI.Def;

namespace EpgTimer
{
    public class CmdExeEpgAutoAdd : CmdExe<EpgAutoAddData>
    {
        public CmdExeEpgAutoAdd(Control owner)
            : base(owner)
        {
            _copyItemData = CtrlCmdDefEx.CopyTo;
        }
        protected override void mc_ShowDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == mutil.OpenChangeEpgAutoAddDialog(dataList[0], this.Owner);
        }
        protected override void mc_ShowAddDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == mutil.OpenAddEpgAutoAddDialog(Owner);
        }
        protected override void mc_ChangeKeyEnabled(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = mutil.EpgAutoAddChangeKeyEnabled(dataList, (byte)CmdExeUtil.ReadIdData(e, 0, 1));
        }
        protected override void mc_ChangeOnOffKeyEnabled(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = mutil.EpgAutoAddChangeOnOffKeyEnabled(dataList);
        }
        protected override void mc_ChangeRecSetting(object sender, ExecutedRoutedEventArgs e)
        {
            if (mcc_chgRecSetting(dataList.RecSettingList(), e, this.Owner) == false) return;
            IsCommandExecuted = mutil.EpgAutoAddChange(dataList);
        }
        protected override void mc_ChgBulkRecSet(object sender, ExecutedRoutedEventArgs e)
        {
            if (mutil.ChangeBulkSet(dataList.RecSettingList(), this.Owner) == false) return;
            IsCommandExecuted = mutil.EpgAutoAddChange(dataList);
        }
        protected override void mc_ChgGenre(object sender, ExecutedRoutedEventArgs e)
        {
            if (mutil.ChgGenre(dataList.RecSearchKeyList(), this.Owner) == false) return;
            IsCommandExecuted = mutil.EpgAutoAddChange(dataList);
        }
        protected override void mc_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == EpgCmds.DeleteAll)
            {
                if (CmdExeUtil.CheckAllDeleteCancel(e, dataList.Count) == true)
                { return; }
            }
            else
            {
                if (CmdExeUtil.CheckKeyboardDeleteCancel(e, 
                    dataList.Select(data => data.searchInfo.andKey).ToList()) == true) return;
            }
            IsCommandExecuted = mutil.EpgAutoAddDelete(dataList);
        }
        protected override void mc_Delete2(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = mcs_Delete2_3(sender, e, true);
        }
        protected override void mc_Delete3(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = mcs_Delete2_3(sender, e, false);
        }
        protected bool mcs_Delete2_3(object sender, ExecutedRoutedEventArgs e, bool deleteAutoAddItem)
        {
            if (CmdExeUtil.IsMessageBeforeCommand(e) == true)
            {
                string msg1 = deleteAutoAddItem == true ? "予約ごと削除" : "予約のみ削除";
                string msg2 = deleteAutoAddItem == true ? "削除項目数" : "処理項目数";

                string text = string.Format(msg1 + "してよろしいですか?\r\n"
                    + "(無効の「自動予約登録項目」による予約も削除されます。)\r\n\r\n"
                    + "[" + msg2 + ": {0}]\r\n[削除される予約数: {1}]\r\n\r\n", dataList.Count, dataList.Sum(data => data.ReserveCount()))
                    + CmdExeUtil.FormatTitleListForDialog(dataList.Select(info => info.searchInfo.andKey).ToList());

                if (MessageBox.Show(text, "[" + msg1 + "]の確認", MessageBoxButton.OKCancel,
                                    MessageBoxImage.Exclamation, MessageBoxResult.OK) != MessageBoxResult.OK)
                { return false; }
            }

            if (deleteAutoAddItem == true)
            {
                //EpgTimerSrvでの自動予約登録の実行タイミングに左右されず確実に予約を削除するため、先に削除
                if (mutil.EpgAutoAddDelete(dataList) == false) return false;
            }

            //配下の予約の削除、再収集する
            return mutil.ReserveDelete(dataList.GetReserveList());
        }
        protected override void mc_AdjustReserve(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.IsMessageBeforeCommand(e) == true)
            {
                string text = string.Format("予約の録画設定を自動登録の録画設定に合わせてもよろしいですか?\r\n\r\n"
                    + "[処理項目数: {0}]\r\n[対象予約数: {1}]\r\n\r\n", dataList.Count, dataList.Sum(data => data.ReserveCount()))
                    + CmdExeUtil.FormatTitleListForDialog(dataList.Select(info => info.searchInfo.andKey).ToList());

                if (MessageBox.Show(text, "[予約の録画設定変更]の確認", MessageBoxButton.OKCancel,
                                    MessageBoxImage.Exclamation, MessageBoxResult.OK) != MessageBoxResult.OK)
                { return; }
            }
            IsCommandExecuted = mutil.ReserveAdjustChange(dataList);
        }
        protected override void mc_CopyTitle(object sender, ExecutedRoutedEventArgs e)
        {
            mutil.CopyTitle2Clipboard(dataList[0].searchInfo.andKey, CmdExeUtil.IsKeyGesture(e));
            IsCommandExecuted = true;
        }
        protected override void mc_SearchTitle(object sender, ExecutedRoutedEventArgs e)
        {
            mutil.SearchText(dataList[0].searchInfo.andKey, CmdExeUtil.IsKeyGesture(e));
            IsCommandExecuted = true;
        }
        protected override void mc_CopyNotKey(object sender, ExecutedRoutedEventArgs e)
        {
            Clipboard.SetDataObject(dataList[0].searchInfo.notKey);
            IsCommandExecuted = true;
        }
        protected override void mc_SetNotKey(object sender, ExecutedRoutedEventArgs e)
        {
            if (CmdExeUtil.IsMessageBeforeCommand(e) == true)
            {
                int DisplayNum = Settings.Instance.KeyDeleteDisplayItemNum;
                var text = new StringBuilder(string.Format("Notキーを変更してよろしいですか?\r\n\r\n"
                    + "[変更項目数: {0}]\r\n[貼り付けテキスト: \"{1}\"]\r\n\r\n", dataList.Count, Clipboard.GetText()));
                foreach (var info in dataList.Take(DisplayNum)) { text.AppendFormat(" ・ {0}\r\n", info.searchInfo.andKey); }
                if (dataList.Count > DisplayNum) text.AppendFormat("\r\n　　ほか {0} 項目", dataList.Count - DisplayNum);

                if (MessageBox.Show(text.ToString(), "[Notキーワード変更]の確認", MessageBoxButton.OKCancel,
                                    MessageBoxImage.Exclamation, MessageBoxResult.OK) != MessageBoxResult.OK)
                { return; }
            }

            IsCommandExecuted = mutil.EpgAutoAddChangeNotKey(dataList);
        }
        protected override void mcs_ctxmLoading_switch(ContextMenu ctxm, MenuItem menu)
        {
            if (menu.Tag == EpgCmdsEx.ChgMenu)
            {
                mcs_chgMenuOpening(menu, dataList.RecSettingList(), false);
                mcs_chgMenuOpening2(menu, dataList.RecSearchKeyList());
            }
            else if (menu.Tag == EpgCmdsEx.OpenFolderMenu)
            {
                mm.CtxmGenerateOpenFolderItems(menu, this.itemCount == 0 ? null : dataList[0].recSetting);
            }
        }
        protected void mcs_chgMenuOpening2(MenuItem menu, List<EpgSearchKeyInfo> keys)
        {
            if (menu.IsEnabled == false) return;

            foreach (var subMenu in menu.Items.OfType<MenuItem>())
            {
                if (subMenu.Tag == EpgCmdsEx.ChgKeyEnabledMenu)
                {
                    byte value = keys.All(info => info.keyDisabledFlag == keys[0].keyDisabledFlag) ? keys[0].keyDisabledFlag : byte.MaxValue;
                    subMenu.Header = string.Format("自動登録有効 : {0}", value == byte.MaxValue ? "*" : (value == 0 ? "有効" : "無効"));
                    foreach (var item in subMenu.Items.OfType<MenuItem>())
                    {
                        //選択アイテムが全て同じ設定の場合だけチェックを表示する
                        item.IsChecked = ((item.CommandParameter as EpgCmdParam).ID == value);
                    }
                    break;
                }
            }
        }
    }
}