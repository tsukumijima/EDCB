﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace EpgTimer
{
    public class CmdExeBase
    {
        public struct cmdOption
        {
            public ExecutedRoutedEventHandler Exe;
            public CanExecuteRoutedEventHandler CanExe;
            public cmdExeType ExeType;
            public bool IsNeedClone;//データをコピーする。変更系コマンドで通信エラーなどあった場合に問題が起きないようにする。
            public bool IsNeedItem;//コマンド実行に対象が必要。ビュー切り替えなどはこれがfalse。
            public bool IsReleaseItem;//コマンド実行前に選択アイテムを解除する。

            public cmdOption(ExecutedRoutedEventHandler exe
                , CanExecuteRoutedEventHandler canExe = null
                , cmdExeType exeType = cmdExeType.NoSetItem
                , bool needClone = false
                , bool needItem = true
                , bool releaseItem = false
                )
            {
                Exe = exe;
                CanExe = canExe;
                ExeType = exeType;
                IsNeedClone = needClone;
                IsNeedItem = needItem;
                IsReleaseItem = releaseItem;
            }
        }
        public enum cmdExeType
        {
            MultiItem,//複数選択対象
            SingleItem,//単一アイテム対象
            NoSetItem,//アイテム不要か自力で収集、IsCommandCancelは使う
            AllItem,//全アイテム対象
            Direct//完全に独立して実行、IsCommandCancelも使わない
        }

        protected static MainWindow mainWindow { get { return CommonManager.MainWindow; } }
        protected static MenuManager mm { get { return CommonManager.Instance.MM; } }

        public virtual void SetFuncGetDataList(Func<bool, IEnumerable<object>> f) { }
        public virtual void SetFuncSelectSingleData(Func<bool, object> f) { }
        public virtual void SetFuncReleaseSelectedData(Action f) { }

        public virtual void AddReplaceCommand(ICommand icmd, ExecutedRoutedEventHandler exe, CanExecuteRoutedEventHandler canExe = null) { }
        public virtual void ResetCommandBindings(params UIElement[] cTrgs) { }
        public virtual object GetJumpTabItem(CtxmCode trg_code = CtxmCode.EpgView) { return null; }
        public virtual Int32 EpgInfoOpenMode { get; set; }
        public virtual void SupportContextMenuLoading(object sender, RoutedEventArgs e) { }
    }
    public class CmdExe<T> : CmdExeBase
        where T : class, IRecWorkMainData, new()
    {
        protected UIElement Owner;

        protected Dictionary<ICommand, cmdOption> cmdList = new Dictionary<ICommand, cmdOption>();
        protected static Dictionary<ICommand, string> cmdMessage = new Dictionary<ICommand, string>();

        protected Func<bool, IEnumerable<object>> _getDataList = null;
        protected Func<bool, object> _selectSingleData = null;
        protected Action _releaseSelectedData = null;

        //型チェックが効きにくいが‥
        public override void SetFuncGetDataList(Func<bool, IEnumerable<object>> f) { _getDataList = f; }
        public override void SetFuncSelectSingleData(Func<bool, object> f) { _selectSingleData = f; }
        public override void SetFuncReleaseSelectedData(Action f) { _releaseSelectedData = f; }

        protected virtual int ItemCount { get { return dataList.Count; } }
        protected List<T> dataList = new List<T>();
        public bool IsCommandExecuted { get; set; }

        static CmdExe()
        {
            SetCmdMessage();
        }

        public CmdExe(UIElement owner)
        {
            this.Owner = owner;
            cmdList.Add(EpgCmds.AddReserve, new cmdOption(mc_Add, null, cmdExeType.MultiItem));
            cmdList.Add(EpgCmds.AddOnPreset, new cmdOption(mc_AddOnPreset, null, cmdExeType.MultiItem));
            cmdList.Add(EpgCmds.ChgOnOff, new cmdOption(mc_ChangeOnOff, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgOnPreset, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgResMode, new cmdOption(mc_ChgResMode, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgBulkRecSet, new cmdOption(mc_ChgBulkRecSet, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgGenre, new cmdOption(mc_ChgGenre, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgRecmode, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgPriority, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgRelay, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgPittari, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgTuner, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgMarginStart, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgMarginEnd, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgMarginValue, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgRecEndMode, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgRecEndReboot, new cmdOption(mc_ChangeRecSetting, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgKeyEnabled, new cmdOption(mc_ChangeKeyEnabled, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ChgOnOffKeyEnabled, new cmdOption(mc_ChangeOnOffKeyEnabled, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.CopyItem, new cmdOption(mc_CopyItem, null, cmdExeType.Direct));//別途処理
            cmdList.Add(EpgCmds.Delete, new cmdOption(mc_Delete, null, cmdExeType.MultiItem));
            cmdList.Add(EpgCmds.Delete2, new cmdOption(mc_Delete2, null, cmdExeType.MultiItem));
            cmdList.Add(EpgCmds.DeleteAll, new cmdOption(mc_Delete, null, cmdExeType.AllItem));
            cmdList.Add(EpgCmds.AdjustReserve, new cmdOption(mc_AdjustReserve, null, cmdExeType.MultiItem));
            cmdList.Add(EpgCmds.RestoreItem, new cmdOption(mc_RestoreItem, null, cmdExeType.Direct, needItem: false));
            cmdList.Add(EpgCmds.RestoreClear, new cmdOption(mc_RestoreClear, null, cmdExeType.Direct, needItem: false));
            cmdList.Add(EpgCmds.ShowDialog, new cmdOption(mc_ShowDialog, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.ShowAddDialog, new cmdOption(mc_ShowAddDialog, null, cmdExeType.NoSetItem, false, false, true));
            cmdList.Add(EpgCmds.ShowAutoAddDialog, new cmdOption(mc_ShowAutoAddDialog, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.ShowReserveDialog, new cmdOption(mc_ShowReserveDialog, null, cmdExeType.MultiItem));
            cmdList.Add(EpgCmds.JumpReserve, new cmdOption(mc_JumpReserve, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.JumpRecInfo, new cmdOption(mc_JumpRecInfo, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.JumpTuner, new cmdOption(mc_JumpTuner, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.JumpTable, new cmdOption(mc_JumpTable, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.JumpListView, new cmdOption(null, null, cmdExeType.SingleItem));//個別に指定
            cmdList.Add(EpgCmds.ToAutoadd, new cmdOption(mc_ToAutoadd, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.ReSearch, new cmdOption(null, null, cmdExeType.SingleItem));//個別に指定
            cmdList.Add(EpgCmds.ReSearch2, new cmdOption(null, null, cmdExeType.SingleItem));//個別に指定
            cmdList.Add(EpgCmds.Play, new cmdOption(mc_Play, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.OpenFolder, new cmdOption(mc_OpenFolder, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.CopyTitle, new cmdOption(mc_CopyTitle, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.CopyContent, new cmdOption(mc_CopyContent, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.InfoSearchTitle, new cmdOption(mc_InfoSearchTitle, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.SearchTitle, new cmdOption(mc_SearchTitle, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.InfoSearchRecTag, new cmdOption(mc_InfoSearchRecTag, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.SearchRecTag, new cmdOption(mc_SearchRecTag, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.CopyRecTag, new cmdOption(mc_CopyRecTag, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.SetRecTag, new cmdOption(mc_SetRecTag, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.CopyNotKey, new cmdOption(mc_CopyNotKey, null, cmdExeType.SingleItem));
            cmdList.Add(EpgCmds.SetNotKey, new cmdOption(mc_SetNotKey, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ProtectChange, new cmdOption(mc_ProtectChange, null, cmdExeType.MultiItem, true));
            cmdList.Add(EpgCmds.ViewChgSet, new cmdOption(null, null, cmdExeType.Direct, needItem: false));//個別に指定
            cmdList.Add(EpgCmds.ViewChgReSet, new cmdOption(null, null, cmdExeType.SingleItem, needItem: false));//個別に指定
            cmdList.Add(EpgCmds.ViewChgMode, new cmdOption(null, null, cmdExeType.SingleItem, needItem: false));//個別に指定
            cmdList.Add(EpgCmds.MenuSetting, new cmdOption(mc_MenuSetting, null, cmdExeType.Direct, needItem: false));

            cmdList.Add(EpgCmdsEx.AddMenu, new cmdOption(null));//メニュー用
            cmdList.Add(EpgCmdsEx.ChgMenu, new cmdOption(null));//メニュー用
            cmdList.Add(EpgCmdsEx.ShowAutoAddDialogMenu, new cmdOption(null));//メニュー用
            cmdList.Add(EpgCmdsEx.ShowReserveDialogMenu, new cmdOption(null));//メニュー用
            cmdList.Add(EpgCmdsEx.RestoreMenu, new cmdOption(null, needItem: false));//メニュー用
            cmdList.Add(EpgCmdsEx.OpenFolderMenu, new cmdOption(null));//メニュー用
            cmdList.Add(EpgCmdsEx.ViewMenu, new cmdOption(null, needItem: false));//メニュー用
        }
        protected virtual void SetData(bool IsAllData = false)
        {
            var listSrc = _getDataList == null ? null : _getDataList(IsAllData);
            dataList = listSrc == null ? new List<T>() : listSrc.OfType<T>().ToList();
            OrderAdjust(dataList);
        }
        protected void OrderAdjust<S>(List<S> list) where S : class
        {
            if (list.Count >= 2)
            {
                var single = SelectSingleData(true) as S;
                if (list.Contains(single))
                {
                    list.Remove(single);
                    list.Insert(0, single);
                }
            }
        }
        protected virtual void ClearData()
        {
            dataList.Clear();
        }
        protected virtual object SelectSingleData(bool noChange = false)
        {
            return _selectSingleData == null ? null : _selectSingleData(noChange);
        }
        protected virtual void ReleaseSelectedData()
        {
            if (_releaseSelectedData != null) _releaseSelectedData();
        }
        protected virtual void CopyDataList()
        {
            if (typeof(T).GetInterface(typeof(IDeepCloneObj).Name) != null)
            {
                dataList = dataList.Select(data => (T)(data as IDeepCloneObj).DeepCloneObj()).ToList();
            }
        }
        protected cmdOption GetCmdParam(ICommand icmd)
        {
            cmdOption cmdPrm;
            cmdList.TryGetValue(icmd, out cmdPrm);//無ければnullメンバのparamが返る。
            return cmdPrm;
        }
        public override void AddReplaceCommand(ICommand icmd, ExecutedRoutedEventHandler exe, CanExecuteRoutedEventHandler canExe = null)
        {
            if (icmd == null) return;

            cmdOption cmdPrm = GetCmdParam(icmd);
            cmdPrm.Exe = exe;
            cmdPrm.CanExe = canExe;
            cmdPrm.ExeType = cmdExeType.Direct;

            if (cmdList.ContainsKey(icmd) == true)
            {
                cmdList[icmd] = cmdPrm;
            }
            else
            {
                cmdList.Add(icmd, cmdPrm);
            }
        }
        /// <summary>持っているコマンドを登録する。</summary>
        public override void ResetCommandBindings(params UIElement[] cTrgs)
        {
            try
            {
                foreach (var item in cmdList)
                {
                    //Exeがあるものを処理する。
                    ExecutedRoutedEventHandler exeh = GetExecute(item.Key);
                    if (exeh != null)
                    {
                        foreach (var cTrg in cTrgs)
                        {
                            //古いものは削除
                            var delList = cTrg.CommandBindings.OfType<CommandBinding>().Where(bind => bind.Command == item.Key).ToList();
                            delList.ForEach(delItem => cTrg.CommandBindings.Remove(delItem));
                            cTrg.CommandBindings.Add(new CommandBinding(item.Key, exeh, GetCanExecute(item.Key)));
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        protected ExecutedRoutedEventHandler GetExecute(ICommand icmd)
        {
            cmdOption cmdPrm = GetCmdParam(icmd);
            return cmdPrm.ExeType == cmdExeType.Direct ? cmdPrm.Exe : GetExecute(cmdPrm);
        }
        protected ExecutedRoutedEventHandler GetExecute(cmdOption cmdPrm)
        {
            if (cmdPrm.Exe == null) return null;
            return new ExecutedRoutedEventHandler((sender, e) =>
            {
                try
                {
                    IsCommandExecuted = false;

                    if (cmdPrm.ExeType == cmdExeType.SingleItem) SelectSingleData();
                    if (cmdPrm.ExeType != cmdExeType.NoSetItem) SetData(cmdPrm.ExeType == cmdExeType.AllItem);
                    if (cmdPrm.IsNeedClone == true) CopyDataList();
                    if (cmdPrm.IsReleaseItem == true) ReleaseSelectedData();

                    if (cmdPrm.Exe != null && (cmdPrm.IsNeedItem == false || this.ItemCount != 0))
                    {
                        cmdPrm.Exe(sender, e);
                        if (Settings.Instance.DisplayStatus == true && Settings.Instance.DisplayStatusNotify == true &&
                            e != null && e.Command != null)
                        {
                            StatusManager.StatusNotifySet(IsCommandExecuted, GetCmdMessage(e.Command));
                        }
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
                ClearData();
            });
        }
        public CanExecuteRoutedEventHandler GetCanExecute(ICommand icmd)
        {
            return GetCmdParam(icmd).CanExe;
        }

        protected virtual void mc_Add(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_AddOnPreset(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChangeOnOff(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChangeRecSetting(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChgResMode(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChgBulkRecSet(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChgGenre(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChangeKeyEnabled(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ChangeOnOffKeyEnabled(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_CopyItem(object sender, ExecutedRoutedEventArgs e)
        {
            GetExecute(Settings.Instance.MenuSet.ShowCopyDialog ?
                        new cmdOption(mcs_CopyItemDialog, null, cmdExeType.SingleItem, true) :
                        new cmdOption(mcs_CopyItem, null, cmdExeType.MultiItem))(sender, e);
        }
        protected virtual void mcs_CopyItem(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mcs_CopyItemDialog(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_Delete(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual bool mcs_DeleteCheck(ExecutedRoutedEventArgs e)
        {
            if (dataList.Count == 0) return false;

            if (e.Command == EpgCmds.DeleteAll)
            {
                if (CmdExeUtil.CheckAllDeleteCancel(e, dataList.Count) == true)
                { return false; ; }
            }
            else
            {
                if (CmdExeUtil.CheckDeleteCancel(e, dataList) == true)
                { return false; ; }
            }
            return true;
        }
        protected virtual void mc_Delete2(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_AdjustReserve(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_RestoreItem(object sender, ExecutedRoutedEventArgs e)
        {
            int count = 0;
            int id = CmdExeUtil.ReadIdData(e);
            try//Historysは操作以外で削除されないが、直接実行なので念のため
            {
                count = CmdHistorys.Historys[id].Items.Count;
                IsCommandExecuted = true == MenuUtil.RecWorkMainDataAdd(CmdHistorys.Historys[id].Items);
                if (IsCommandExecuted == true) CmdHistorys.Historys.RemoveAt(id);
            }
            catch { }
            StatusManager.StatusNotifySet(IsCommandExecuted, GetCmdMessageFormat("アイテムの復元", count));
        }
        protected virtual void mc_RestoreClear(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show("履歴をクリアします。\r\nよろしいですか?", "アイテムの復元"
                    , MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                CmdHistorys.Clear();
                IsCommandExecuted = true;
            }
            StatusManager.StatusNotifySet(IsCommandExecuted, "アイテムの復元履歴をクリア");
        }
        protected virtual void mc_ShowDialog(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ShowAddDialog(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_JumpReserve(object sender, ExecutedRoutedEventArgs e)
        {
            mcs_JumpTab(CtxmCode.ReserveView);
        }
        protected virtual void mc_JumpRecInfo(object sender, ExecutedRoutedEventArgs e)
        {
            mcs_JumpTab(CtxmCode.RecInfoView);
        }
        protected virtual void mc_JumpTuner(object sender, ExecutedRoutedEventArgs e)
        {
            mcs_JumpTab(CtxmCode.TunerReserveView);
        }
        protected virtual void mc_JumpTable(object sender, ExecutedRoutedEventArgs e)
        {
            mcs_JumpTab(CtxmCode.EpgView);
        }
        protected virtual void mcs_JumpTab(CtxmCode trg_code)
        {
            MenuUtil.JumpTab(mcs_GetJumpTabItem(trg_code), trg_code);
            IsCommandExecuted = true;
        }
        protected virtual object mcs_GetJumpTabItem(CtxmCode trg_code)
        {
            if (trg_code == CtxmCode.RecInfoView)
            {
                RecFileInfo data = mcs_GetRecInfoItem();
                return data == null ? null : new RecInfoItem(data);
            }

            SearchItem item = mcs_GetSearchItem();
            if (item == null) return null;

            bool reserveOnly = trg_code == CtxmCode.ReserveView || trg_code == CtxmCode.TunerReserveView;
            bool onReserveOnly = trg_code == CtxmCode.TunerReserveView && Settings.Instance.TunerDisplayOffReserve == false;

            if (reserveOnly && item.IsReserved == false) return null;
            if (onReserveOnly && item.ReserveInfo.IsEnabled == false) return null;

            return item;
        }
        public override object GetJumpTabItem(CtxmCode trg_code = CtxmCode.EpgView)
        {
            object retv = null;
            var cmdPrm = new cmdOption((s, e) => retv = mcs_GetJumpTabItem(trg_code), null, cmdExeType.SingleItem);
            GetExecute(cmdPrm)(null, null);
            return retv;
        }
        protected virtual SearchItem mcs_GetSearchItem()
        {
            ReserveData data = mcs_GetNextReserve();
            return data == null ? null : new ReserveItem(data);
        }
        protected virtual ReserveData mcs_GetNextReserve() { return null; }
        protected virtual RecFileInfo mcs_GetRecInfoItem() { return null; }
        protected virtual void mc_ShowAutoAddDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenChangeAutoAddDialog(CmdExeUtil.ReadObjData(e) as Type, (uint)CmdExeUtil.ReadIdData(e));
        }
        protected virtual void mc_ShowReserveDialog(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenChangeReserveDialog((uint)CmdExeUtil.ReadIdData(e), EpgInfoOpenMode);
        }
        protected virtual void mc_ToAutoadd(object sender, ExecutedRoutedEventArgs e)
        {
            MenuUtil.SendAutoAdd(dataList[0] as IBasicPgInfo, CmdExeUtil.IsKeyGesture(e));
            IsCommandExecuted = true;
        }
        protected virtual void mc_Play(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_OpenFolder(object sender, ExecutedRoutedEventArgs e)
        {
            var path = CmdExeUtil.ReadObjData(e) as string;
            if (string.IsNullOrEmpty(path) && dataList[0] is IRecSetttingData)//ショートカットから
            {
                RecSettingData recSet = (dataList[0] as IRecSetttingData).RecSettingInfo;
                RecFileSetInfo f1 = recSet.RecFolderList.Concat(recSet.PartialRecFolder).FirstOrDefault();
                path = (f1 == null || f1.RecFolder == "!Default") ? Settings.Instance.DefRecFolders[0] : f1.RecFolder;
            }
            CommonManager.OpenRecFolder(path);
            IsCommandExecuted = true;
        }
        protected virtual void mc_CopyTitle(object sender, ExecutedRoutedEventArgs e)
        {
            MenuUtil.CopyTitle2Clipboard(dataList[0].DataTitle, CmdExeUtil.IsKeyGesture(e));
            IsCommandExecuted = true;
        }
        protected virtual void mc_CopyContent(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_InfoSearchTitle(object sender, ExecutedRoutedEventArgs e)
        {
            IsCommandExecuted = true == MenuUtil.OpenInfoSearchDialog(dataList[0].DataTitle, CmdExeUtil.IsKeyGesture(e));
        }
        protected virtual void mc_SearchTitle(object sender, ExecutedRoutedEventArgs e)
        {
            MenuUtil.SearchTextWeb(dataList[0].DataTitle, CmdExeUtil.IsKeyGesture(e));
            IsCommandExecuted = true;
        }
        protected virtual void mc_InfoSearchRecTag(object sender, ExecutedRoutedEventArgs e)
        {
            string tag = mcs_getRecTag();
            if (tag != null) IsCommandExecuted = true == MenuUtil.OpenInfoSearchDialog(tag);
        }
        protected virtual void mc_SearchRecTag(object sender, ExecutedRoutedEventArgs e)
        {
            string tag = mcs_getRecTag();
            if (tag != null) MenuUtil.SearchTextWeb(tag);
        }
        protected virtual void mc_CopyRecTag(object sender, ExecutedRoutedEventArgs e)
        {
            string tag = mcs_getRecTag();
            if (tag != null) Clipboard.SetDataObject(mcs_getRecTag());
        }
        protected virtual string mcs_getRecTag()
        {
            var data = SelectSingleData(true) as IRecSetttingData;
            IsCommandExecuted = data != null && data.RecSettingInfo != null;
            return IsCommandExecuted == true ? data.RecSettingInfo.RecTag : null;
        }
        protected virtual void mc_SetRecTag(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_CopyNotKey(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_SetNotKey(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_ProtectChange(object sender, ExecutedRoutedEventArgs e) { }
        protected virtual void mc_MenuSetting(object sender, ExecutedRoutedEventArgs e)
        {
            var dlg = new SetContextMenuWindow(Owner, Settings.Instance.MenuSet);
            if (dlg.ShowDialog() == true)
            {
                Settings.Instance.MenuSet = dlg.info.DeepClone();
                Settings.SaveToXmlFile();//メニュー設定の保存
                SettingWindow.UpdatesInfo("右クリックメニューの変更");
                mainWindow.RefreshMenu();
            }
        }
        protected bool mcc_chgRecSetting(ExecutedRoutedEventArgs e, bool PresetResCompare = false)
        {
            List<RecSettingData> infoList = dataList.OfType<IRecSetttingData>().Where(data => data.RecSettingInfo != null).RecSettingList();
            if (infoList.Count == 0) return false;

            if (e.Command == EpgCmds.ChgOnPreset)
            {
                var val = Settings.Instance.RecPreset(CmdExeUtil.ReadIdData(e, 0, 0xFE)).Data;
                MenuUtil.ChangeRecSet(dataList.OfType<IRecSetttingData>(), val);
            }
            else if (e.Command == EpgCmds.ChgRecmode)
            {
                var val = (byte)CmdExeUtil.ReadIdData(e, 0, 5);
                infoList.ForEach(info => info.RecMode = val);
            }
            else if (e.Command == EpgCmds.ChgPriority)
            {
                var val = (byte)CmdExeUtil.ReadIdData(e, 1, 5);
                infoList.ForEach(info => info.Priority = val);
            }
            else if (e.Command == EpgCmds.ChgRelay)
            {
                var val = (byte)CmdExeUtil.ReadIdData(e, 0, 1);
                infoList.ForEach(info => info.TuijyuuFlag = val);
            }
            else if (e.Command == EpgCmds.ChgPittari)
            {
                var val = (byte)CmdExeUtil.ReadIdData(e, 0, 1);
                infoList.ForEach(info => info.PittariFlag = val);
            }
            else if (e.Command == EpgCmds.ChgTuner)
            {
                var val = (uint)CmdExeUtil.ReadIdData(e, 0, int.MaxValue - 1);
                infoList.ForEach(info => info.TunerID = val);
            }
            else if (e.Command == EpgCmds.ChgMarginStart)
            {
                int? offset = CmdExeUtil.ReadIdData(e);
                MenuUtil.ChangeMargin(infoList, offset == 0, offset, null, true);
            }
            else if (e.Command == EpgCmds.ChgMarginEnd)
            {
                int? offset = CmdExeUtil.ReadIdData(e);
                MenuUtil.ChangeMargin(infoList, offset == 0, null, offset, true);
            }
            else if (e.Command == EpgCmds.ChgMarginValue)
            {
                return MenuUtil.ChangeMarginValue(infoList, CmdExeUtil.ReadIdData(e, 0, 2) == 1, this.Owner, PresetResCompare);
            }
            else if (e.Command == EpgCmds.ChgRecEndMode)
            {
                var val = CmdExeUtil.ReadIdData(e);
                infoList.ForEach(info =>
                {
                    info.RebootFlag = val < 0 ? Settings.Instance.DefRebootFlg : info.RebootFlagActual;//先にやる
                    info.SetSuspendMode(val < 0, val);
                });
            }
            else if (e.Command == EpgCmds.ChgRecEndReboot)
            {
                var val = (byte)CmdExeUtil.ReadIdData(e, 0, 1);
                infoList.ForEach(info =>
                {
                    info.RebootFlag = val;
                    info.SetSuspendMode(false, info.RecEndModeActual);
                });
            }
            return true;
        }
        public override void SupportContextMenuLoading(object sender, RoutedEventArgs e)
        {
            try
            {
                IsCommandExecuted = false;
                SetData();

                var ctxm = sender as ContextMenu;

                ctxm.IsOpen = ctxm.Items.Count != 0;
                if (ctxm.IsOpen == false)
                {
                    return;
                }

                if (ctxm.PlacementTarget is ListBox && e != null)
                {
                    //リストビューの場合は、アイテムの無いところではデータ選択してないものと見なす。
                    if ((ctxm.PlacementTarget as ListBox).GetPlacementItem() == null)
                    {
                        ClearData();
                    }
                }

                foreach (var menu in ctxm.Items.OfType<MenuItem>())
                {
                    //有効無効制御。ボタンをあまりグレーアウトしたくないのでCanExecuteを使わずここで実施する
                    menu.IsEnabled = this.ItemCount != 0 || GetCmdParam(menu.Tag as ICommand).IsNeedItem == false;

                    //共通の処理
                    menu.ToolTip = null;
                    mcs_ctxmLoading_edit_tooltip(menu);

                    //録画タグ
                    if (menu.Tag == EpgCmds.InfoSearchRecTag || menu.Tag == EpgCmds.SearchRecTag || menu.Tag == EpgCmds.CopyRecTag)
                    {
                        menu.IsEnabled = mcs_getRecTag() != null;
                    }
                    else if (menu.Tag == EpgCmdsEx.RestoreMenu)
                    {
                        mm.CtxmGenerateRestoreMenuItems(menu);
                    }
                    //コピー
                    if (menu.Tag == EpgCmds.CopyItem)
                    {
                        menu.Header = "コピーを追加" + (Settings.Instance.MenuSet.ShowCopyDialog ? "..." : "");
                    }

                    //コマンド集に応じた処理
                    mcs_ctxmLoading_switch(ctxm, menu);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            ClearData();
        }
        protected virtual void mcs_ctxmLoading_switch(ContextMenu ctxm, MenuItem menu) { }
        protected virtual void mcs_ctxmLoading_edit_tooltip(MenuItem menu)
        {
            Func<bool, string, string, string, string> _ToggleModeTooltip = (mode, Caption, OnText, OffText) =>
            {
                string ModeText = (mode == true ? OnText : OffText);
                string ToggleText = (mode == false ? OnText : OffText);
                return Caption + ModeText + " (Shift+クリックで一時的に'" + ToggleText + "')";
            };
            if (menu.Tag == EpgCmds.ToAutoadd || menu.Tag == EpgCmds.ReSearch || menu.Tag == EpgCmds.ReSearch2)
            {
                menu.ToolTip = _ToggleModeTooltip(Settings.Instance.MenuSet.Keyword_Trim, "記号除去モード : ", "オン", "オフ");
            }
            else if (menu.Tag == EpgCmds.CopyTitle)
            {
                menu.ToolTip = _ToggleModeTooltip(Settings.Instance.MenuSet.CopyTitle_Trim, "記号除去モード : ", "オン", "オフ");
            }
            else if (menu.Tag == EpgCmds.CopyContent)
            {
                menu.ToolTip = _ToggleModeTooltip(Settings.Instance.MenuSet.CopyContentBasic, "取得モード : ", "基本情報のみ", "詳細情報");
            }
            else if (menu.Tag == EpgCmds.InfoSearchTitle)
            {
                menu.ToolTip = _ToggleModeTooltip(Settings.Instance.MenuSet.InfoSearchTitle_Trim, "記号除去モード : ", "オン", "オフ");
            }
            else if (menu.Tag == EpgCmds.SearchTitle)
            {
                menu.ToolTip = _ToggleModeTooltip(Settings.Instance.MenuSet.SearchTitle_Trim, "記号除去モード : ", "オン", "オフ");
            }
        }
        protected virtual void mcs_ctxmLoading_jumpTabRes(MenuItem menu, string offres_tooltip = null)
        {
            //メニュー実行時に選択されるアイテムが予約でないとき、または予約が無いときは無効
            ReserveData resinfo = mcs_GetNextReserve();
            menu.IsEnabled = (resinfo != null);
            if (resinfo == null) return;

            if (resinfo.IsEnabled == false)
            {
                menu.ToolTip = offres_tooltip;

                if (menu.Tag == EpgCmds.JumpTuner && Settings.Instance.TunerDisplayOffReserve == false)
                {
                    //無効予約を回避
                    menu.IsEnabled = false;
                    menu.ToolTip = "無効予約は使用予定チューナー画面に表示されない設定になっています。";
                }
            }
        }
        protected virtual void mcs_ctxmLoading_jumpTabEpg(MenuItem menu)
        {
            //ジャンプ先がない場合無効にする
            SearchItem item = mcs_GetSearchItem();
            menu.IsEnabled = item != null;
            //時間がかかったりするとイヤなのでメニュー構築を優先する
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (item != null && MenuUtil.CheckJumpTab(item) == false)
                {
                    menu.ToolTip = ((menu.ToolTip as string) + "\r\n番組表にアイテムが見つかりません。").Trim();
                }
            }), DispatcherPriority.Loaded);
        }
        protected void mcs_SetSingleMenuEnabled(MenuItem menu, bool isEnabled)
        {
            if (menu.IsEnabled == false) return;
            foreach (var subMenu in menu.Items.OfType<MenuItem>())
            {
                subMenu.IsEnabled = isEnabled || GetCmdParam(subMenu.Tag as ICommand).ExeType != cmdExeType.SingleItem;
            }
        }
        protected void mcs_chgMenuOpening(MenuItem menu, bool PresetResCompare = false)
        {
            if (menu.IsEnabled == false) return;

            var listr = dataList.OfType<IRecSetttingData>().Where(data => data.RecSettingInfo != null).ToList();
            List<RecSettingData> recSettings = listr.RecSettingList();

            Action<MenuItem, int> SetCheckmarkSubMenus = (subMenu, value) =>
            {
                foreach (var item in subMenu.Items.OfType<MenuItem>())
                {
                    item.IsChecked = ((item.CommandParameter as EpgCmdParam).ID == value);
                }
            };

            //選択アイテムが全て同じ設定の場合だけチェックを表示する
            foreach (var subMenu in menu.Items.OfType<MenuItem>())
            {
                subMenu.Visibility = Visibility.Visible;
                if (subMenu.Tag == EpgCmds.ShowDialog)
                {
                    subMenu.Header = "ダイアログ表示(_X)...";
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgKeyEnabledMenu)
                {
                    if (typeof(T).IsSubclassOf(typeof(AutoAddData)) == false)
                    {
                        subMenu.Visibility = Visibility.Collapsed;
                        continue;
                    }
                    var list = dataList.OfType<AutoAddData>().ToList();
                    bool? value = list.All(info => info.IsEnabled == list[0].IsEnabled) ? (bool?)list[0].IsEnabled : null;
                    subMenu.Header = string.Format("自動登録有効(_A) : {0}", value == null ? "*" : (value == true ? "有効" : "無効"));
                    SetCheckmarkSubMenus(subMenu, value == true ? 0 : value == false ? 1 : int.MinValue);
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgOnPresetMenu)
                {
                    mm.CtxmGenerateChgOnPresetItems(subMenu);

                    RecPresetItem pre_0 = listr[0].RecSettingInfo.LookUpPreset(listr[0].IsManual, false, PresetResCompare);
                    RecPresetItem value = listr.All(data => data.RecSettingInfo.LookUpPreset(data.IsManual, false, PresetResCompare).ID == pre_0.ID) ? pre_0 : null;
                    subMenu.Header = string.Format("プリセット(_P) : {0}", value == null ? "*" : value.DisplayName);
                    SetCheckmarkSubMenus(subMenu, value == null ? int.MinValue : value.ID);
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgResModeMenu)
                {
                    //メニュークリアもあるので先に実行
                    mm.CtxmGenerateChgResModeAutoAddItems(subMenu, dataList.Count == 1 ? dataList[0] as ReserveData : null);

                    if (typeof(T) != typeof(ReserveData))
                    {
                        subMenu.Visibility = Visibility.Collapsed;
                        continue;
                    }
                    var list = dataList.OfType<ReserveData>().ToList();
                    ReserveMode? resMode_0 = list[0].ReserveMode;
                    ReserveMode? value = list.All(info => info.ReserveMode == resMode_0) ? resMode_0 : null;
                    subMenu.Header = string.Format("予約モード(_M) : {0}", value == null ? "*" : CommonManager.ConvertResModeText(value));
                    SetCheckmarkSubMenus(subMenu, value == ReserveMode.EPG ? 0 : value == ReserveMode.Program ? 1 : int.MinValue);

                    if (list[0].IsAutoAdded == false) continue;

                    foreach (var item in subMenu.Items.OfType<MenuItem>())
                    {
                        Type type = (item.CommandParameter as EpgCmdParam).Data as Type;
                        int id = (item.CommandParameter as EpgCmdParam).ID;
                        AutoAddData autoAdd = AutoAddData.AutoAddList(type, (uint)id);
                        if (autoAdd != null)
                        {
                            item.IsChecked = autoAdd.GetReserveList().Any(info => info.ReserveID == list[0].ReserveID);
                        }
                    }
                }
                else if (subMenu.Tag == EpgCmds.ChgBulkRecSet)
                {
                    if (recSettings.Count < 2) subMenu.Visibility = Visibility.Collapsed;
                    subMenu.Header = "まとめて録画設定を変更(_O)...";
                }
                else if (subMenu.Tag == EpgCmds.ChgGenre)
                {
                    if (recSettings.Count < 2 || typeof(T) != typeof(EpgAutoAddData)) subMenu.Visibility = Visibility.Collapsed;
                    subMenu.Header = "まとめてジャンル絞り込みを変更(_J)...";
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgRecmodeMenu)
                {
                    byte value = recSettings.All(info => info.RecMode == recSettings[0].RecMode) ? recSettings[0].RecMode : byte.MaxValue;
                    subMenu.Header = string.Format("録画モード(_R) : {0}", value == byte.MaxValue ? "*" : CommonManager.ConvertRecModeText(value));
                    SetCheckmarkSubMenus(subMenu, value);
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgPriorityMenu)
                {
                    byte value = recSettings.All(info => info.Priority == recSettings[0].Priority) ? recSettings[0].Priority : byte.MaxValue;
                    subMenu.Header = string.Format("優先度(_Y) : {0}", value == byte.MaxValue ? "*" : value.ToString());
                    SetCheckmarkSubMenus(subMenu, value);
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgRelayMenu || subMenu.Tag == EpgCmdsEx.ChgPittariMenu)
                {
                    if (typeof(T) != typeof(ReserveData) && typeof(T) != typeof(EpgAutoAddData))
                    {
                        subMenu.Visibility = Visibility.Collapsed;
                        continue;
                    }

                    byte value;
                    string format;
                    if (subMenu.Tag == EpgCmdsEx.ChgRelayMenu)
                    {
                        value = recSettings.All(info => info.TuijyuuFlag == recSettings[0].TuijyuuFlag) ? recSettings[0].TuijyuuFlag : byte.MaxValue;
                        format = "イベントリレー追従(_Z) : {0}";
                    }
                    else
                    {
                        value = recSettings.All(info => info.PittariFlag == recSettings[0].PittariFlag) ? recSettings[0].PittariFlag : byte.MaxValue;
                        format = "ぴったり(?)録画(_F) : {0}";
                    }
                    subMenu.Header = string.Format(format, value == byte.MaxValue ? "*" : CommonManager.ConvertYesNoText(value));
                    SetCheckmarkSubMenus(subMenu, value);
                    subMenu.IsEnabled = listr.Any(info => info.IsManual == false);
                    subMenu.ToolTip = (subMenu.IsEnabled != true ? "プログラム予約は対象外" : null);
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgTunerMenu)
                {
                    uint tunerID = recSettings.All(info => info.TunerID == recSettings[0].TunerID) ? recSettings[0].TunerID : uint.MaxValue;
                    mm.CtxmGenerateTunerMenuItems(subMenu);
                    subMenu.Header = string.Format("チューナー(_T) : {0}", tunerID == uint.MaxValue ? "*" : CommonManager.ConvertTunerText(tunerID));
                    SetCheckmarkSubMenus(subMenu, (int)tunerID);
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgMarginStartMenu)
                {
                    int value = recSettings.All(info => info.StartMarginActual == recSettings[0].StartMarginActual) ? recSettings[0].StartMarginActual : int.MaxValue;
                    bool def = recSettings.All(info => info.IsMarginDefault == true);
                    subMenu.Header = string.Format("開始マージン(_S) : {0} 秒{1}", value == int.MaxValue ? "*" : value.ToString(), def ? " (デフォルト)" : "");
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgMarginEndMenu)
                {
                    int value = recSettings.All(info => info.EndMarginActual == recSettings[0].EndMarginActual) ? recSettings[0].EndMarginActual : int.MaxValue;
                    bool def = recSettings.All(info => info.IsMarginDefault == true);
                    subMenu.Header = string.Format("終了マージン(_E) : {0} 秒{1}", value == int.MaxValue ? "*" : value.ToString(), def ? " (デフォルト)" : "");
                }
                else if (subMenu.Tag == EpgCmdsEx.ChgRecEndMenu)
                {
                    int mode = recSettings.All(info => info.RecEndModeActual == recSettings[0].RecEndModeActual) ? recSettings[0].RecEndModeActual : int.MaxValue;
                    byte reboot = recSettings.All(info => info.RebootFlag == recSettings[0].RebootFlag) ? recSettings[0].RebootFlag : byte.MaxValue;
                    bool def = recSettings.All(info => info.RecEndIsDefault == true);
                    subMenu.Header = string.Format("録画後動作(_W) : {0} / {1}{2}", mode == int.MaxValue ? "*" : CommonManager.ConvertRecEndModeText(mode),
                        reboot == byte.MaxValue ? "*" : "再起動" + CommonManager.ConvertYesNoText(reboot), def ? " (デフォルト)" : "");
                    SetCheckmarkSubMenus(subMenu, mode);
                    foreach (var item in subMenu.Items.OfType<MenuItem>().Where(item => item.Command == EpgCmds.ChgRecEndReboot))
                    {
                        item.IsChecked = ((item.CommandParameter as EpgCmdParam).ID == reboot);
                    }
                }
            }
        }

        protected virtual string GetCmdMessage(ICommand icmd)
        {
            string cmdMsg = null;
            cmdMessage.TryGetValue(icmd, out cmdMsg);
            return GetCmdMessageFormat(cmdMsg, this.ItemCount);
        }
        public string GetCmdMessageFormat(string cmdMsg, int Count)
        {
            if (string.IsNullOrEmpty(cmdMsg) == true) return null;
            return string.Format("{0}(処理数:{1})", cmdMsg, Count);
        }
        protected static void SetCmdMessage()
        {
            cmdMessage.Add(EpgCmds.AddReserve, "予約を追加");
            cmdMessage.Add(EpgCmds.AddOnPreset, "指定プリセットで予約を追加");
            cmdMessage.Add(EpgCmds.ChgOnOff, "簡易予約/有効・無効切替を実行");
            cmdMessage.Add(EpgCmds.ChgOnPreset, "録画プリセットを変更");
            cmdMessage.Add(EpgCmds.ChgResMode, "予約モードを変更");
            cmdMessage.Add(EpgCmds.ChgBulkRecSet, "録画設定を変更");
            cmdMessage.Add(EpgCmds.ChgGenre, "ジャンル絞り込みを変更");
            cmdMessage.Add(EpgCmds.ChgRecmode, "録画モードを変更");
            cmdMessage.Add(EpgCmds.ChgPriority, "優先度を変更");
            cmdMessage.Add(EpgCmds.ChgRelay, "イベントリレー追従設定を変更");
            cmdMessage.Add(EpgCmds.ChgPittari, "ぴったり録画設定を変更");
            cmdMessage.Add(EpgCmds.ChgTuner, "チューナー指定を変更");
            cmdMessage.Add(EpgCmds.ChgMarginStart, "録画マージンを変更");
            cmdMessage.Add(EpgCmds.ChgMarginEnd, "録画マージンを変更");
            cmdMessage.Add(EpgCmds.ChgMarginValue, "録画マージンを変更");
            cmdMessage.Add(EpgCmds.ChgRecEndMode, "録画後動作を変更");
            cmdMessage.Add(EpgCmds.ChgRecEndReboot, "録画後動作を変更");
            cmdMessage.Add(EpgCmds.ChgKeyEnabled, "有効/無効を変更");
            cmdMessage.Add(EpgCmds.ChgOnOffKeyEnabled, "有効/無効切替を実行");
            cmdMessage.Add(EpgCmds.CopyItem, "コピーを追加");
            cmdMessage.Add(EpgCmds.Delete, "削除を実行");
            cmdMessage.Add(EpgCmds.Delete2, "予約ごと削除を実行");
            cmdMessage.Add(EpgCmds.DeleteAll, "全て削除を実行");
            cmdMessage.Add(EpgCmds.AdjustReserve, "自動予約登録に予約を合わせる");
            cmdMessage.Add(EpgCmds.ProtectChange, "プロテクト切替を実行");
            cmdMessage.Add(EpgCmds.CopyTitle, "番組名/ANDキーをコピー");
            cmdMessage.Add(EpgCmds.CopyContent, "番組情報をコピー");
            cmdMessage.Add(EpgCmds.CopyRecTag, "録画タグをコピー");
            cmdMessage.Add(EpgCmds.SetRecTag, "録画タグを変更");
            cmdMessage.Add(EpgCmds.CopyNotKey, "Notキーをコピー");
            cmdMessage.Add(EpgCmds.SetNotKey, "Notキーを変更");
        }
    }

    public class CmdExeUtil
    {
        public static bool CheckAllDeleteCancel(ExecutedRoutedEventArgs e, int Count)
        {
            if (Count == 0) return true;//今は無くても同じ
            if (IsMessageBeforeCommand(e) == false) return false;

            return (MessageBox.Show(string.Format(
                "全て削除しますか?\r\n" + "[削除項目数: {0}]", Count)
                , "[全削除]の確認", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK);
        }
        public static bool CheckDeleteCancel(ExecutedRoutedEventArgs e, IEnumerable<IRecWorkMainData> dataList)
        {
            if (dataList.Any() == false) return true;//今は無くても同じ
            if (IsMessageBeforeCommand(e) == false) return false;

            List<string> titleList = dataList.Select(info => info.DataTitle).ToList();
            return (MessageBox.Show(
                string.Format("削除しますか?\r\n\r\n" + "[削除項目数: {0}]\r\n\r\n", titleList.Count) + FormatTitleListForDialog(titleList)
                , "削除の確認", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK);
        }
        public static bool CheckAllProcCancel(ExecutedRoutedEventArgs e, IEnumerable<AutoAddData> dataList, bool IsDelete)
        {
            if (dataList.Any() == false) return true;//今は無くても同じ
            if (IsMessageBeforeCommand(e) == false) return false;

            List<string> titleList = dataList.Select(info => info.DataTitle).ToList();
            var s = IsDelete == true
                ? new string[] { "予約ごと削除して", "削除", "削除される予約数", "予約ごと削除" }
                : new string[] { "予約の録画設定を自動登録の録画設定に合わせても", "処理", "対象予約数", "予約の録画設定変更" };

            var text = string.Format("{0}よろしいですか?\r\n"
                                        + "(個別予約も処理の対象となります。)\r\n\r\n"
                                        + "[{1}項目数: {2}]\r\n"
                                        + "[{3}: {4}]\r\n\r\n", s[0], s[1], titleList.Count, s[2], dataList.GetReserveList().Count)
                + FormatTitleListForDialog(titleList);

            return (MessageBox.Show(text, "[" + s[3] + "]の確認", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK);
        }
        public static bool CheckSetFromClipBoardCancel(ExecutedRoutedEventArgs e, IEnumerable<IRecWorkMainData> dataList, string caption)
        {
            if (dataList.Any() == false) return true;
            if (IsMessageBeforeCommand(e) == false) return false;

            List<string> titleList = dataList.Select(info => info.DataTitle).ToList();
            var text = string.Format("{0}を変更してよろしいですか?\r\n\r\n"
                + "[変更項目数: {1}]\r\n[貼り付けテキスト: \"{2}\"]\r\n\r\n", caption, titleList.Count, Clipboard.GetText())
                + FormatTitleListForDialog(titleList);

            return (MessageBox.Show(text, "[" + caption + "変更]の確認", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) ;
        }
        public static string FormatTitleListForDialog(ICollection<string> list)
        {
            int DisplayNum = MenuSettingData.CautionDisplayItemNum;
            var text = new StringBuilder();
            foreach (var info in list.Take(DisplayNum)) { text.AppendFormat(" ・ {0}\r\n", info); }
            if (list.Count > DisplayNum) text.AppendFormat("\r\n　　ほか {0} 項目", list.Count - DisplayNum);
            return text.ToString();
        }
        public static bool IsMessageBeforeCommand(ExecutedRoutedEventArgs e)
        {
            if (HasCommandParameter(e) == false) return false;
            //コマンド側のオプションに変更可能なようにまとめておく
            bool NoMessage = true;
            if (e.Command == EpgCmds.DeleteAll) NoMessage = Settings.Instance.MenuSet.NoMessageDeleteAll;
            else if (e.Command == EpgCmds.Delete || e.Command == EpgCmds.DeleteInDialog) NoMessage = Settings.Instance.MenuSet.NoMessageDelete;
            else if (e.Command == EpgCmds.Delete2 || e.Command == EpgCmds.Delete2InDialog) NoMessage = Settings.Instance.MenuSet.NoMessageDelete2;
            else if (e.Command == EpgCmds.SetRecTag) NoMessage = Settings.Instance.MenuSet.NoMessageRecTag;
            else if (e.Command == EpgCmds.SetNotKey) NoMessage = Settings.Instance.MenuSet.NoMessageNotKEY;
            else if (e.Command == EpgCmds.AdjustReserve) NoMessage = Settings.Instance.MenuSet.NoMessageAdjustRes;

            return NoMessage == false || IsDisplayKgMessage(e);
        }
        public static bool IsDisplayKgMessage(ExecutedRoutedEventArgs e)
        {
            return Settings.Instance.MenuSet.NoMessageKeyGesture == false && IsKeyGesture(e);
        }
        public static bool IsKeyGesture(ExecutedRoutedEventArgs e)
        {
            if (HasCommandParameter(e) == false) return false;
            return (e.Parameter as EpgCmdParam).SourceType == typeof(KeyGesture);
        }
        public static int ReadIdData(ExecutedRoutedEventArgs e, int min = int.MinValue, int max = int.MaxValue)
        {
            if (HasCommandParameter(e) == false) return min;
            return Math.Max(Math.Min((e.Parameter as EpgCmdParam).ID, max), min);
        }
        public static object ReadObjData(ExecutedRoutedEventArgs e)
        {
            if (HasCommandParameter(e) == false) return null;
            return (e.Parameter as EpgCmdParam).Data;
        }
        public static bool HasCommandParameter(ExecutedRoutedEventArgs e)
        {
            return (e != null && e.Parameter is EpgCmdParam);
        }

    }
}