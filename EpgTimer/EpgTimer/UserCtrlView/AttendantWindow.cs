using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    //Pinの相手はメインウィンドウ限定
    public class AttendantWindow : Window
    {
        protected static MainWindow mainWindow { get { return CommonManager.MainWindow; } }
        protected bool XMLSaveOnClose = false;

        protected virtual void WriteWindowSaveData()
        {
            SavePinned();
            Settings.Instance.WndSettings.GetSizeFromWindow(this);
        }
        protected virtual void SavePinned()
        {
            Settings.Instance.WndSettings[this].Pinned = this.Pinned == true;
        }

        protected override void OnInitialized(EventArgs e)
        {
            try
            {
                base.OnInitialized(e);

                Settings.Instance.WndSettings.SetSizeToWindow(this);

                //ウィンドウ位置調整
                var wnds = Application.Current.Windows.OfType<AttendantWindow>().Where(w => w.GetType() == this.GetType()).ToList();
                if (wnds.Count > 1)
                {
                    this.Left = wnds[wnds.Count - 2].Left + 50;
                    this.Top = wnds[wnds.Count - 2].Top + 25;
                }

                //はみ出ないよう修正
                ViewUtil.AdjustWindowPosition(this);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        //前面表示関係
        protected void SetParam(bool xmlSaveOnClosed = false, CheckBox pinned_chk = null)
        {
            XMLSaveOnClose = xmlSaveOnClosed;
            chkPinned = pinned_chk;
            if (chkPinned != null)
            {
                chkPinned.Checked += (sender, e) => { SavePinned(); UpdatePinned(); };
                chkPinned.Unchecked += (sender, e) => { SavePinned(); UpdatePinned(); };
                this.Pinned = Settings.Instance.WndSettings[this].Pinned;
            }
        }

        private CheckBox chkPinned = null;
        public virtual bool? Pinned
        {
            get { return chkPinned == null ? (bool?)null : chkPinned.IsChecked == true; }
            set { if (chkPinned != null) chkPinned.IsChecked = value == true; }
        }

        public static void UpdatesPinned()
        {
            foreach (var win in Application.Current.Windows.OfType<AttendantWindow>())
            {
                win.UpdatePinned();
            }
        }
        public virtual void UpdatePinned()
        {
            if (Pinned == null) return;
            Window newOwner = Pinned == true ? mainWindow : null;
            if (Pinned == true && mainWindow.IsVisible == false || this.Owner == newOwner) return;
            this.Owner = newOwner;
        }

        //Closedイベントなど
        public static void MinimizeWindows()
        {
            foreach (var win in Application.Current.Windows.OfType<AttendantWindow>())
            {
                win.WindowState = WindowState.Minimized;
            }
        }
        private static bool AllClosing = false;
        public static void CloseWindows()
        {
            AllClosing = true;
            foreach (var win in Application.Current.Windows.OfType<AttendantWindow>())
            {
                try
                {
                    win.Close();
                }
                catch { }
            }
            AllClosing = false;
        }
        //フォーカス関係はOnClosed()でないと意図通り動かないが、RestoreBounds()はOnClosingでないと取れない。
        protected override void OnClosing(CancelEventArgs e)
        {
            WriteWindowSaveData();
            base.OnClosing(e);
        }
        protected override void OnClosed(EventArgs e)
        {
            //フォーカスがおかしくなるときがあるので、とりあえずの対応
            if (Application.Current.Windows.OfType<AttendantWindow>().Any() != true)
            {
                if (mainWindow.IsLoaded) mainWindow.Activate();
            }
            if (AllClosing == false)
            {
                if (XMLSaveOnClose == true)
                {
                    Settings.SaveToXmlFile();//ウィンドウ位置などの保存
                }
                if (mainWindow.IsActive == true)
                {
                    mainWindow.ListFoucsOnVisibleChanged();
                }
            }

            base.OnClosed(e);
        }

        //ショートカット更新など
        protected static MenuManager mm { get { return CommonManager.Instance.MM; } }
        protected MenuBinds mBinds = new MenuBinds();
        public static void RefreshMenus()
        {
            foreach (var win in Application.Current.Windows.OfType<AttendantWindow>())
            {
                win.RefreshMenu();
            }
        }
        public virtual void RefreshMenu()
        {
            //設定されてなければ何もしない
            mBinds.ResetInputBindings(this, this);
        }
    }

    public class AttendantDataWindow<T> : AttendantWindow
    {
        protected override void WriteWindowSaveData()
        {
            SaveDataReplace();
            base.WriteWindowSaveData();
        }
        protected virtual void SaveDataReplace()
        {
            Settings.Instance.WndSettings[this].DataChange = this.EnableDataChange;
        }

        protected void SetParam(bool xmlSaveOnClosed = false, CheckBox pinned_chk = null, CheckBox dataChange_chk = null)
        {
            chkDataChange = dataChange_chk;
            if (chkDataChange != null)
            {
                if (chkDataChange.ToolTip == null)
                {
                    chkDataChange.ToolTip = "このウィンドウが見えているとき、新しく開いたアイテムをこのウィンドウに表示します";
                }
                chkDataChange.Checked += (sender, e) => { SaveDataReplace(); OrderAdjust(); };
                chkDataChange.Unchecked += (sender, e) => SaveDataReplace();
                this.EnableDataChange = Settings.Instance.WndSettings[this].DataChange;
            }
            base.SetParam(xmlSaveOnClosed, pinned_chk);
        }

        //データ選択関係
        protected virtual ulong DataID { get { return 0; } }
        protected virtual IEnumerable<KeyValuePair<ulong, object>> DataRefList { get { return null; } }
        private int selIdx = -1;
        protected virtual DataItemViewBase DataView { get { return null; } }
        public static bool UpdatesViewSelection(DataViewBase view, bool Reloaded)
        {
            if (Reloaded == false) return false;
            foreach (var win in Application.Current.Windows.OfType<AttendantDataWindow<T>>())
            {
                if (win.IsActive == true)
                {
                    win.UpdateViewSelection(1);
                    return true;
                }
            }
            return false;
        }
        protected virtual void UpdateViewSelection(int mode = 0)
        {
            if (DataView != null && DataView.IsVisible == true) DataView.MoveToItem(DataID);
        }
        protected virtual void MoveViewNextItem(int direction, bool toRefData = false)
        {
            object NewData = toRefData || DataView == null || DataView.IsVisible == false ? null : DataView.MoveNextItem(direction, DataID, true, JumpItemStyle.None);
            if (NewData == null)
            {
                IEnumerable<KeyValuePair<ulong, object>> idList = DataRefList;
                if (idList == null || idList.Any() == false) return;
                var list = idList.ToList();
                NewData = list[selIdx = ViewUtil.GetNextIdx(selIdx, list.FindIndex(d => d.Key == DataID), list.Count, direction)].Value;
            }
            ChangeData(NewData);
        }

        //データ入れ替え関係
        private CheckBox chkDataChange = null;
        protected virtual bool EnableDataChange
        {
            get { return chkDataChange == null ? false : chkDataChange.IsChecked == true; }
            set { if (chkDataChange != null) chkDataChange.IsChecked = value; }
        }
        protected int LastUsed = 0;
        protected virtual void OrderAdjust()
        {
            if (EnableDataChange == true)
            {
                foreach (var win in Application.Current.Windows.OfType<AttendantDataWindow<T>>())
                {
                    win.LastUsed++;
                }
                this.LastUsed = int.MinValue;
            }
        }
        public static Window ChangeDataLastUsedWindow(object data)
        {
            var win = Application.Current.Windows.OfType<AttendantDataWindow<T>>()
                .Where(w => w.EnableDataChange == true && w.IsVisible == true && w.WindowState != WindowState.Minimized)
                .OrderBy(w => w.LastUsed).FirstOrDefault() as AttendantDataWindow<T>;

            if (win != null) win.ChangeData(data);
            return win;
        }
        public virtual void ChangeData(object data) { }

        //リロード関係
        protected bool ReloadInfoFlg = false;
        protected virtual bool ReloadInfoData() { return true; }
        public static void UpdatesInfo(bool reload = true)
        {
            foreach (var win in Application.Current.Windows.OfType<AttendantDataWindow<T>>())
            {
                win.UpdateInfo(reload);
            }
        }
        public virtual void UpdateInfo(bool reload = true)
        {
            ReloadInfoFlg |= reload;
            ReloadInfo();
        }
        protected virtual void ReloadInfo()
        {
            if (ReloadInfoFlg == true && this.IsVisible == true && (this.WindowState != WindowState.Minimized || this.IsActive == true))
            {
                ReloadInfoFlg = !ReloadInfoData();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            ReloadInfo();
            OrderAdjust();
            UpdateViewSelection(2);
            base.OnActivated(e);
        }
    }

    public class HideableWindow<T> : AttendantDataWindow<T>
    {
        /// <summary>番組表などへジャンプした際に最小化したWindow</summary>
        protected static string buttonID = "[]";
        protected static HideableWindow<T> hideWindow = null;
        public static bool HasHideWindow { get { return hideWindow != null; } }
        protected static void SetHideWindow(HideableWindow<T> win)
        {
            // 情報を保持は最新のもの1つだけ
            hideWindow = win;
            mainWindow.EmphasizeButton(HasHideWindow, buttonID);
        }
        public static void RestoreHideWindow()
        {
            // 最小化したWindowを復帰
            if (HasHideWindow == true)
            {
                hideWindow.Show();
                hideWindow.WindowState = WindowState.Normal;
            }
        }
        protected virtual void JumpTabAndHide(CtxmCode code, object item)
        {
            if (item == null) return;
            //
            mainWindow.RestoreMinimizedWindow();
            mainWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                SetHideWindow(this);
                MinimizeWindows();
                MenuUtil.JumpTab(item, code);
            }));
        }
        protected override void OnClosed(EventArgs e)
        {
            if (hideWindow == this) SetHideWindow(null);
            base.OnClosed(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState != WindowState.Minimized)
            {
                if (hideWindow == this) SetHideWindow(null);
            }
            base.OnStateChanged(e);
        }
    }
}
