using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    using EpgView;

    /// <summary>
    /// EpgDataViewItem.xaml の相互作用ロジック
    /// </summary>
    public partial class EpgDataViewItem : UserControl
    {
        public event ViewSettingClickHandler ViewSettingClick = null; 
        public EpgViewBase viewCtrl { get; private set; }

        public EpgDataViewItem()
        {
            InitializeComponent();
        }

        public void SaveViewData()
        {
            if (viewCtrl != null) viewCtrl.SaveViewData();
        }

        public void RefreshMenu()
        {
            if (viewCtrl != null) viewCtrl.RefreshMenu();
        }

        public void UpdateReserveInfo(bool reload = true)
        {
            if (viewCtrl != null) viewCtrl.UpdateReserveInfo(reload);
        }

        public void UpdateInfo(bool reload = true)
        {
            if (viewCtrl != null) viewCtrl.UpdateInfo(reload);
        }

        public object GetViewState()
        {
            return viewCtrl == null ? null : viewCtrl.GetViewState();
        }
        public void SetViewState(object data)
        {
            if (viewCtrl != null) viewCtrl.SetViewState(data);
        }

        /// <summary>現在のEPGデータ表示モードの設定を取得する</summary>
        public CustomEpgTabInfo GetViewMode()
        {
            return viewCtrl == null ? null : viewCtrl.GetViewMode();
        }

        /// <summary>EPGデータの表示モードを設定する</summary>
        /// <param name="setInfo">[IN]表示モードの設定値</param>
        public void SetViewMode(CustomEpgTabInfo setInfo, object state = null)
        {
            //表示モード一緒で、絞り込み内容変化のみ。
            if (viewCtrl != null)
            {
                CustomEpgTabInfo viewInfo = viewCtrl.GetViewMode();
                if (viewInfo != null && viewInfo.ViewMode == setInfo.ViewMode)
                {
                    viewCtrl.SetViewMode(setInfo);
                    viewCtrl.UpdateInfo();
                    return;
                }
            }

            //切り替える場合
            SaveViewData();
            switch (setInfo.ViewMode)
            {
                case 1://1週間表示
                    viewCtrl = new EpgWeekMainView();
                    break;
                case 2://リスト表示
                    viewCtrl = new EpgListMainView();
                    break;
                default://標準ラテ欄表示
                    viewCtrl = new EpgMainView();
                    break;
            }

            viewCtrl.ViewSettingClick += new ViewSettingClickHandler(item_ViewSettingClick);
            viewCtrl.SetViewMode(setInfo);
            if (state != null) SetViewState(state);
            grid_main.Children.Clear();
            grid_main.Children.Add(viewCtrl);
        }

        private void item_ViewSettingClick(object sender, CustomEpgTabInfo info)
        {
            if (ViewSettingClick != null) ViewSettingClick(this, info);
        }
    }
}
