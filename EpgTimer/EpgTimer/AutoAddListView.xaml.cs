using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// EpgAutoAddView.xaml の相互作用ロジック
    /// </summary>
    public partial class AutoAddListView : DataItemViewBase
    {
        public AutoAddListView()
        {
            InitializeComponent();
        }
    }
    public class AutoAddViewBaseT<T, S> : AutoAddListView
        where T : AutoAddData, new()
        where S : AutoAddDataItemT<T>
    {
        protected CtxmCode viewCode;
        protected string ColumnSavePath;
        protected List<GridViewColumn> ColumnList;

        protected ListViewController<S> lstCtrl;
        protected CmdExeAutoAdd<T> mc;

        protected virtual void PostProcSaveOrder(Dictionary<ulong, ulong> changeIDTable) { }

        //ドラッグ移動ビュー用の設定
        class lvDragData : ListBoxDragMoverView.LVDMHelper
        {
            private AutoAddViewBaseT<T, S> View;
            public lvDragData(AutoAddViewBaseT<T, S> view) { View = view; }
            public override ulong GetID(object data) { return (data as S).Data.DataID; }
            public override bool SaveChange(Dictionary<ulong, ulong> changeIDTable)
            {
                var newList = View.lstCtrl.dataList.AutoAddInfoList().DeepClone();
                newList.ForEach(item => item.DataID = changeIDTable[item.DataID]);

                bool ret = MenuUtil.AutoAddChange(newList, false, false, false, true);
                StatusManager.StatusNotifySet(ret, "並べ替えを保存");
                if (ret == true)
                {
                    //dataListと検索ダイアログへのIDの反映。dataListは既にコピーだが、SaveChange成功後に行う
                    View.lstCtrl.dataList.ForEach(item => item.Data.DataID = changeIDTable[item.Data.DataID]);
                    View.PostProcSaveOrder(changeIDTable);
                }
                return ret;
            }
            public override bool RestoreOrder()
            {
                bool ret = View.ReloadInfoData();
                StatusManager.StatusNotifySet(ret, "並べ替えを復元");
                return ret;
            }
            public override void StatusChanged()
            {
                //とりあえず今はこれで
                var tab = View.Parent as TabItem;
                tab.Header = (tab.Header as string).TrimEnd('*') + (View.dragMover.NotSaved == true ? "*" : "");
                tab.ToolTip = View.dragMover.NotSaved ? "並び替え状態未保存\r\n　Ctrl+S:保存\r\n　Ctrl+Z:復元" : null;
            }
            public override void ItemMoved() { View.lstCtrl.gvSorter.ResetSortParams(); }
        }

        public AutoAddViewBaseT()
        {
            //リストビューデータ差し込み
            ColumnList = Resources[this.GetType().Name] as GridViewColumnList;
            ColumnList.AddRange(Resources["CommonColumns"] as GridViewColumnList);
            ColumnList.AddRange(Resources["RecSettingViewColumns"] as GridViewColumnList);

            InitAutoAddView();
        }

        public virtual void InitAutoAddView()
        {
            try
            {
                //リストビュー関連の設定
                lstCtrl = new ListViewController<S>(this);
                lstCtrl.SetSavePath(ColumnSavePath);
                lstCtrl.SetViewSetting(listView_key, gridView_key, true, false
                    , ColumnList, (sender, e) => dragMover.NotSaved |= lstCtrl.GridViewHeaderClickSort(e));
                ColumnList = null;
                lstCtrl.SetSelectedItemDoubleClick(EpgCmds.ShowDialog);

                //ステータス変更の設定
                lstCtrl.SetSelectionChangedEventHandler((sender, e) => this.UpdateStatus(1));

                //ドラッグ移動関係
                this.dragMover.SetData(this, listView_key, new lvDragData(this));

                //最初にコマンド集の初期化
                //mc = (R)Activator.CreateInstance(typeof(R), this);
                mc.SetFuncGetDataList(isAll => (isAll == true ? lstCtrl.dataList : lstCtrl.GetSelectedItemsList()).AutoAddInfoList());
                mc.SetFuncSelectSingleData((noChange) =>
                {
                    var item = lstCtrl.SelectSingleItem(noChange);
                    return item == null ? null : item.Data as T;
                });
                mc.SetFuncReleaseSelectedData(() => listView_key.UnselectAll());

                //コマンド集に無いもの
                mc.AddReplaceCommand(EpgCmds.ChgOnOffCheck, (sender, e) => lstCtrl.ChgOnOffFromCheckbox(e.Parameter, EpgCmds.ChgOnOffKeyEnabled));

                //コマンドをコマンド集から登録
                mc.ResetCommandBindings(this, listView_key.ContextMenu);

                //コンテキストメニューを開く時の設定
                listView_key.ContextMenu.Opened += new RoutedEventHandler(mc.SupportContextMenuLoading);

                //ボタンの設定
                mBinds.View = viewCode;
                mBinds.SetCommandToButton(button_ShowAddDialog, EpgCmds.ShowAddDialog);
                mBinds.SetCommandToButton(button_Delete, EpgCmds.Delete);
                mBinds.SetCommandToButton(button_Delete2, EpgCmds.Delete2);
                mBinds.SetCommandToButton(button_ToAutoadd, EpgCmds.ToAutoadd);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        public void RefreshMenu()
        {
            mc.EpgInfoOpenMode = Settings.Instance.SearchEpgInfoOpenMode;
            mBinds.ResetInputBindings(this, listView_key);
            mm.CtxmGenerateContextMenu(listView_key.ContextMenu, viewCode, true);
        }
        public void TabContextMenuOpen(object sender, MouseButtonEventArgs e)
        {
            var ctxm = new ContextMenuEx { IsOpen = true };
            e.Handled = true;

            var menu = new MenuItem { Header = "自動登録画面の画面設定(_O)..." };
            menu.Click += (s2, e2) => CommonManager.MainWindow.OpenSettingDialog(SettingWindow.SettingMode.ReserveSetting);
            ctxm.Items.Add(menu);

            //非表示の時は設定画面のみ
            if (this.IsVisible == false) return;

            ctxm.Items.Add(new Separator());
            menu = new MenuItem { Header = "並びを保存する(_S)", IsEnabled = dragMover.NotSaved, InputGestureText = MenuBinds.GetInputGestureText(EpgCmds.SaveOrder) };
            menu.Click += (s2, e2) => EpgCmds.SaveOrder.Execute(null, dragMover);
            ctxm.Items.Add(menu);
            menu = new MenuItem { Header = "並びを元に戻す(_Z)", IsEnabled = dragMover.NotSaved, InputGestureText = MenuBinds.GetInputGestureText(EpgCmds.RestoreOrder) };
            menu.Click += (s2, e2) => EpgCmds.RestoreOrder.Execute(null, dragMover);
            ctxm.Items.Add(menu);
        }

        public void SaveViewData()
        {
            lstCtrl.SaveViewDataToSettings();
        }
        protected override bool ReloadInfoData()
        {
            EpgCmds.DragCancel.Execute(null, dragMover);

            return lstCtrl.ReloadInfoData(dataList =>
            {
                dataList.AddRange(AutoAddData.GetDBManagerList(typeof(T)).Select(info => (S)Activator.CreateInstance(typeof(S), info.DeepCloneObj())));
                dragMover.NotSaved = false;
                return true;
            });
        }

        protected override ListBox DataListBox { get { return listView_key; } }

        protected override void UpdateStatusData(int mode = 0)
        {
            if (mode == 0) this.status[1] = ViewUtil.ConvertAutoAddStatus(lstCtrl.dataList, "自動予約登録数");
            List<S> sList = lstCtrl.GetSelectedItemsList();
            this.status[2] = sList.Count == 0 ? "" : ViewUtil.ConvertAutoAddStatus(sList, "　選択中");
        }
    }

    public class EpgAutoAddView : AutoAddViewBaseT<EpgAutoAddData, EpgAutoDataItem>
    {
        public override void InitAutoAddView()
        {
            //固有設定
            mc = new CmdExeEpgAutoAdd(this);//ジェネリックでも処理できるが‥
            viewCode = CtxmCode.EpgAutoAddView;
            ColumnSavePath = CommonUtil.NameOf(() => Settings.Instance.AutoAddEpgColumn);
            button_ToAutoadd.Content = "Andキーワードで検索";

            //初期化の続き
            base.InitAutoAddView();
        }
        protected override void PostProcSaveOrder(Dictionary<ulong, ulong> changeIDTable)
        {
            SearchWindow.UpdatesAutoAddViewOrderChanged(changeIDTable);
        }
    }

    public class ManualAutoAddView : AutoAddViewBaseT<ManualAutoAddData, ManualAutoAddDataItem>
    {
        public override void InitAutoAddView()
        {
            //固有設定
            mc = new CmdExeManualAutoAdd(this);
            viewCode = CtxmCode.ManualAutoAddView;
            ColumnSavePath = CommonUtil.NameOf(() => Settings.Instance.AutoAddManualColumn);

            //録画設定の表示項目を調整
            ColumnList.Remove(ColumnList.Find(data => (data.Header as GridViewColumnHeader).Uid == "Tuijyu"));
            ColumnList.Remove(ColumnList.Find(data => (data.Header as GridViewColumnHeader).Uid == "Pittari"));
            ColumnList.RenameHeader("SearchCount", "曜日数");

            //初期化の続き
            base.InitAutoAddView();
        }
        protected override void PostProcSaveOrder(Dictionary<ulong, ulong> changeIDTable)
        {
            AddManualAutoAddWindow.UpdatesAutoAddViewOrderChanged(changeIDTable);
        }
    }

}
