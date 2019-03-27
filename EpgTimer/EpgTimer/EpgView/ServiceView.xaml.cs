using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer.EpgView
{
    /// <summary>
    /// ServiceView.xaml の相互作用ロジック
    /// </summary>
    public partial class ServiceView : UserControl, IEpgSettingAccess, IEpgViewDataSet
    {
        public ServiceView()
        {
            InitializeComponent();
        }

        public void ClearInfo()
        {
            stackPanel_service.Children.Clear();
        }

        public int EpgSettingIndex { get; private set; }
        public void SetViewData(EpgViewData data)
        {
            EpgSettingIndex = data.EpgSettingIndex;
            Background = this.EpgBrushCache().ServiceBorderColor;
        }

        public void SetService(List<EpgServiceInfo> serviceList)
        {
            stackPanel_service.Children.Clear();
            foreach (EpgServiceInfo info in serviceList)
            {
                var service1 = new StackPanel();
                service1.Width = this.EpgStyle().ServiceWidth - 1;
                service1.VerticalAlignment = VerticalAlignment.Center;
                service1.MouseLeftButtonDown += (sender, e) =>
                {
                    if (e.ClickCount != 2) return;
                    //
                    var serviceInfo = ((FrameworkElement)sender).DataContext as EpgServiceInfo;
                    CommonManager.Instance.TVTestCtrl.SetLiveCh(serviceInfo.ONID, serviceInfo.TSID, serviceInfo.SID);
                };
                service1.DataContext = info;

                var text = ViewUtil.GetPanelTextBlock(CommonManager.ReplaceUrl(info.service_name));
                text.Margin = new Thickness(1, 0, 1, 0);
                text.Foreground = this.EpgBrushCache().ServiceFontColor;
                service1.Children.Add(text);

                int chnum = ChSet5.ChNumber(info.Key);
                text = ViewUtil.GetPanelTextBlock((info.IsDttv ? (chnum != 0 ? "地デジ " : "ServiceID:") : CommonManager.ReplaceUrl(info.network_name) + " ") + (chnum != 0 ? chnum : info.SID).ToString());
                text.Margin = new Thickness(1, 0, 1, 2);
                text.Foreground = this.EpgBrushCache().ServiceFontColor;
                service1.Children.Add(text);

                service1.ToolTip = this.EpgStyle().EpgServiceNameTooltip != true ? null : ViewUtil.ServiceHeaderToToolTip(service1);

                var grid1 = new Grid();
                grid1.Background = this.EpgBrushCache().ServiceBackColor;
                grid1.Margin = new Thickness(0, 1, 1, 1);
                grid1.Children.Add(service1);
                stackPanel_service.Children.Add(grid1);
            }
        }
    }
}
