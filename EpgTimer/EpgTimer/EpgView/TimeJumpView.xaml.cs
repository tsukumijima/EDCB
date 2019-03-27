using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer.EpgView
{
    public partial class TimeJumpView : UserControl, IEpgSettingAccess, IEpgViewDataSet
    {
        public event Action<EpgViewPeriod> JumpDateClick = period => { };
        public event Action DateChanged = () => { };
        public void SetDate(EpgViewPeriod period = null, DateTime? limit = null)
        {
            if (period != null)
            {
                picker_start.SelectedDate = period.Start;
                text_days.Text = period.Days.ToString();
            }
            if (limit != null)
            {
                label_Limit.Text = limit != DateTime.MaxValue ? ((DateTime)limit).ToString(modeText + "可能期間 yyyy\\/MM\\/dd(ddd) 以降") : "*過去番組データなし";
            }
        }
        public EpgViewPeriod GetDate() { return GetDate(false); }
        private EpgViewPeriod GetDate(bool setDef)
        {
            var start = picker_start.SelectedDate ?? prdef.InitStart;
            double days = MenuUtil.MyToNumerical(text_days, Convert.ToDouble, 366, 0, 7, setDef);
            return new EpgViewPeriod(start, days);
        }

        public TimeJumpView()
        {
            InitializeComponent();

            button_jumpDate.Click += (sender, e) => JumpDateClick(GetDate(true));
            picker_start.SelectedDateChanged += (sender, e) => DateChanged();
            text_days.TextChanged += (sender, e) => DateChanged();

            //デフォルト値
            prdef = new EpgViewPeriodDef(Settings.Instance.EpgSettingList[0]);
            SetDate(prdef.DefPeriod, prdef.InitStart);
        }
        private EpgViewPeriodDef prdef;
        public int EpgSettingIndex { get; private set; }
        public void SetViewData(EpgViewData data)
        {
            EpgSettingIndex = data.EpgSettingIndex;
            RefreshPeriod();
            SetDate(prdef.DefPeriod, prdef.InitStart);
        }
        public void RefreshPeriod()
        {
            prdef = new EpgViewPeriodDef(this.EpgStyle());
        }

        protected void TextBoxOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || Keyboard.Modifiers != ModifierKeys.None || e.IsRepeat) return;
            //
            switch (e.Key)
            {
                case Key.Enter:
                    button_jumpDate.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    e.Handled = true;
                    break;
            }
        }
        public void DateTooltip(object sender, ToolTipEventArgs e)
        {
            (sender as FrameworkElement).ToolTip = GetDate().ConvertText(DateTime.MaxValue);
        }

        private string modeText = "表示";
        public void SetSearchMode()
        {
            button_jumpDate.Visibility = Visibility.Collapsed;
            text_calendar.Text = "検索期間";
            modeText = "検索";
            SetDate(prdef.DefPeriod, prdef.InitStart);
        }
    }
}
