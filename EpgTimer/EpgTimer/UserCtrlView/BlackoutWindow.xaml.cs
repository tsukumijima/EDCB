using System;
using System.Windows;
using System.Windows.Threading;

namespace EpgTimer {
    /// <summary>
    /// Interaction logic for BlackoutWindow.xaml
    /// </summary>
    public partial class BlackoutWindow : Window {

        /// <summary>
        /// 番組表への受け渡し
        /// </summary>
        public static object SelectedData = null;
        public static bool HasData { get { return SelectedData != null; } }
        public static bool HasItemData { get { return SelectedItem != null; } }

        public static SearchItem SelectedItem { get { return SelectedData as SearchItem; } }
        public static AutoAddTargetData ItemData { get { return HasItemData ? (AutoAddTargetData)SelectedItem.ReserveInfo ?? SelectedItem.EventInfo : null; } }
        public static bool HasReserveData { get { return SelectedItem != null && SelectedItem.ReserveInfo != null; } }
        public static bool HasProgramData { get { return SelectedItem != null && SelectedItem.EventInfo != null; } }

        //番組表へジャンプ中
        public static bool NowJumpTable = false;

        public static void Clear()
        {
            SelectedData = null;
            NowJumpTable = false;
        }

        public BlackoutWindow(Window owner0) {
            InitializeComponent();
            //
            this.Owner = owner0;
            //
            this.WindowState = this.Owner.WindowState;
            this.Width = this.Owner.Width;
            this.Height = this.Owner.Height;
            //this.Topmost = true;
        }

        public void showWindow(string message0) {
            this.messageLabel.Content = message0 + "...";
            this.Show();
            //
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (sender, e) => { timer.Stop(); this.Close(); };
            timer.Start();
        }

    }
}
