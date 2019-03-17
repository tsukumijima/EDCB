using System;
using System.Windows;
using System.Windows.Controls;

namespace EpgTimer.EpgView
{
    public partial class TimeMoveView : UserControl
    {
        public event Action<bool> OpenToggleClick;
        public event Action<int> MoveButtonClick = (mode) => { };
        public event Action<Button, ToolTipEventArgs, int> MoveButtonToolTipOpen = (button, e, mode) => { };

        public void SetButtonEnabled(bool? prev, bool? next, bool? isOpen = null)
        {
            if (prev != null) button_Prev.IsEnabled = (bool)prev;
            if (next != null) button_Next.IsEnabled = (bool)next;
            if (isOpen != null) button_Panel.Content = isOpen == true ? "↑" : "↓";
        }

        public TimeMoveView()
        {
            InitializeComponent();
            OpenToggleClick += isOpen => SetButtonEnabled(null, null, isOpen);
            button_Panel.Click += (sender, e) => OpenToggleClick(button_Panel.Content as string == "↓");
            button_Prev.Click += (sender, e) => MoveButtonClick(-1);
            button_Next.Click += (sender, e) => MoveButtonClick(1);
            button_Prev.ToolTipOpening += (sender, e) => MoveButtonToolTipOpen(button_Prev, e, -1);
            button_Next.ToolTipOpening += (sender, e) => MoveButtonToolTipOpen(button_Next, e, 1);
        }
    }
}
