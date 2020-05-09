using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    /// <summary>
    /// AutoAddView.xaml の相互作用ロジック
    /// </summary>
    public partial class AutoAddView : UserControl
    {
        public AutoAddView()
        {
            InitializeComponent();
        }

        public void RefreshMenu()
        {
            epgAutoAddView.RefreshMenu();
            manualAutoAddView.RefreshMenu();
        }

        public void TabContextMenuOpen(object sender, MouseButtonEventArgs e)
        {
            var tab = tabControl.GetPlacementItem() as TabItem;
            if (tab == tabItem_epgAutoAdd) epgAutoAddView.TabContextMenuOpen(sender, e);
            if (tab == tabItem_manualAutoAdd) manualAutoAddView.TabContextMenuOpen(sender, e);
        }

        public void SaveViewData()
        {
            epgAutoAddView.SaveViewData();
            manualAutoAddView.SaveViewData();
        }

        public void UpdateInfo(bool reload = true)
        {
            epgAutoAddView.UpdateInfo(reload);
            manualAutoAddView.UpdateInfo(reload);
        }

    }
}
