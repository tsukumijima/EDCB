using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer.EpgView
{
    /// <summary>
    /// ServiceView.xaml の相互作用ロジック
    /// </summary>
    public partial class ServiceView : UserControl
    {
        public ServiceView()
        {
            InitializeComponent();
            this.Background = CommonManager.Instance.EpgServiceBorderColor;
        }

        public void ClearInfo()
        {
            stackPanel_service.Children.Clear();
        }

        public void SetService(List<EpgServiceInfo> serviceList)
        {
            stackPanel_service.Children.Clear();
            foreach (EpgServiceInfo info in serviceList)
            {
                var service1 = new StackPanel();
                service1.Width = Settings.Instance.ServiceWidth - 1;
                service1.Margin = new Thickness(0, 1, 1, 1);
                service1.Background = CommonManager.Instance.EpgServiceBackColor;
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
                text.Foreground = CommonManager.Instance.EpgServiceFontColor;
                service1.Children.Add(text);

                int chnum = ChSet5.ChNumber(info.Key);
                text = ViewUtil.GetPanelTextBlock((info.IsDttv ? (chnum != 0 ? "地デジ " : "ServiceID:") : CommonManager.ReplaceUrl(info.network_name) + " ") + (chnum != 0 ? chnum : info.SID).ToString());
                text.Margin = new Thickness(1, 0, 1, 2);
                text.Foreground = CommonManager.Instance.EpgServiceFontColor;
                service1.Children.Add(text);

                service1.ToolTip = Settings.Instance.EpgServiceNameTooltip != true ? null : ViewUtil.ServiceHeaderToToolTip(service1);
                stackPanel_service.Children.Add(service1);
            }
        }
    }
}
