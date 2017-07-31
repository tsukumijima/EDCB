using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;

namespace EpgTimer
{
    public enum TaskIconSpec : uint { TaskIconBlue, TaskIconRed, TaskIconOrange, TaskIconGreen, TaskIconGray, TaskIconNone };

    public class TaskTrayClass : IDisposable
    {
        private NotifyIcon notifyIcon = new NotifyIcon();
        private Window targetWindow;

        public string Text {
            get { return notifyIcon.Text; }
            set { notifyIcon.Text = CommonUtil.LimitLenString(value, 63); }
        }
        private TaskIconSpec iconSpec = TaskIconSpec.TaskIconNone;
        public TaskIconSpec Icon
        {
            get { return iconSpec; }
            set
            {
                iconSpec = value;
                Icon icon = GetTaskTrayIcon(value);
                System.Drawing.Size size = SystemInformation.SmallIconSize;
                notifyIcon.Icon = icon == null ? null : new Icon(icon, new System.Drawing.Size((size.Width + 15) / 16 * 16, (size.Height + 15) / 16 * 16));
            }
        }
        private Icon GetTaskTrayIcon(TaskIconSpec status)
        {
            switch (status)
            {
                case TaskIconSpec.TaskIconBlue:     return Properties.Resources.TaskIconBlue;
                case TaskIconSpec.TaskIconRed:      return Properties.Resources.TaskIconRed;
                case TaskIconSpec.TaskIconOrange:   return Properties.Resources.TaskIconOrange;
                case TaskIconSpec.TaskIconGreen:    return Properties.Resources.TaskIconGreen;
                case TaskIconSpec.TaskIconGray:     return Properties.Resources.TaskIconGray;
                default: return null;
            }
        }
        public bool Visible{
            get { return notifyIcon.Visible; }
            set { notifyIcon.Visible = value; }
        }
        public WindowState LastViewState { get; set; }
        public event EventHandler ContextMenuClick = null;

        public TaskTrayClass(Window target)
        {
            Text = "";
            notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            notifyIcon.Click += NotifyIcon_Click;
            notifyIcon.BalloonTipClicked += NotifyIcon_Click;
            // 接続先ウィンドウ
            targetWindow = target;
            LastViewState = targetWindow.WindowState;
            // ウィンドウに接続
            if (targetWindow != null) {
                targetWindow.Closing += new System.ComponentModel.CancelEventHandler(target_Closing);
            }
            notifyIcon.ContextMenuStrip = new ContextMenuStrip();

            // 指定タイムアウトでバルーンチップを強制的に閉じる
            var balloonTimer = new System.Windows.Threading.DispatcherTimer();
            balloonTimer.Tick += (sender, e) =>
            {
                if (notifyIcon.Visible)
                {
                    notifyIcon.Visible = false;
                    notifyIcon.Visible = true;
                }
                balloonTimer.Stop();
            };
            notifyIcon.BalloonTipShown += (sender, e) =>
            {
                if (Settings.Instance.ForceHideBalloonTipSec > 0)
                {
                    balloonTimer.Interval = TimeSpan.FromSeconds(Math.Max(Settings.Instance.ForceHideBalloonTipSec, 1));
                    balloonTimer.Start();
                }
            };
            notifyIcon.BalloonTipClicked += (sender, e) => balloonTimer.Stop();
            notifyIcon.BalloonTipClosed += (sender, e) => balloonTimer.Stop();
        }

        public void SetContextMenu(IEnumerable<Tuple<string,string>> list)
        {
            if (list.Any() != true)
            {
                notifyIcon.ContextMenuStrip = null;
            }
            else
            {
                var menu = new ContextMenuStrip();
                foreach(var item in list)
                {
                    ToolStripMenuItem newcontitem = new ToolStripMenuItem();
                    if (item.Item1.Length > 0)
                    {
                        newcontitem.Tag = item.Item1;
                        newcontitem.Text = item.Item2;
                        newcontitem.Click += new EventHandler(newcontitem_Click);
                        menu.Items.Add(newcontitem);
                    }
                    else
                    {
                        menu.Items.Add(new ToolStripSeparator());
                    }

                }
                notifyIcon.ContextMenuStrip = menu;
            }
        }

        /// <summary> timeOutMilliSecは設定しても効かない環境がある </summary>
        public void ShowBalloonTip(String title, String tips, Int32 timeOutMilliSec = 10 * 1000)
        {
            try
            {
                if (Settings.Instance.NoBallonTips == false)
                {
                    title = string.IsNullOrEmpty(title) == true ? " " : title;
                    tips = string.IsNullOrEmpty(tips) == true ? " " : tips;
                    notifyIcon.ShowBalloonTip(timeOutMilliSec, title, tips, ToolTipIcon.Info);
                }
            }
            catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message + "\r\n" + ex.StackTrace); }
        }

        void  newcontitem_Click(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(ToolStripMenuItem))
            {
                if (ContextMenuClick != null)
                {
                    ContextMenuClick((sender as ToolStripMenuItem).Tag, e);
                }
            }
        }

        public void Dispose()
        {
            // ウィンドウから切断
            if (targetWindow != null)
            {
                targetWindow.Closing -= new System.ComponentModel.CancelEventHandler(target_Closing);
                targetWindow = null;
            }
        }

        private void target_Closing(object sender, CancelEventArgs e)
        {
            if (e.Cancel == false)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (e.GetType() == typeof(MouseEventArgs))
            {
                MouseEventArgs mouseEvent = e as MouseEventArgs;
                if (mouseEvent.Button == MouseButtons.Left)
                {
                    //左クリック
                    if (targetWindow != null)
                    {
                        try
                        {
                            targetWindow.Show();
                            targetWindow.WindowState = LastViewState;
                            targetWindow.Activate();
                        }
                        catch { }
                    }
                }
            }
        }   
    }

    public class TaskTrayState
    {
        private TaskTrayClass taskTray;
        private uint srvState = uint.MaxValue;
        public TaskTrayState(TaskTrayClass tasktray) { taskTray = tasktray ?? new TaskTrayClass(null); }

        public bool IsSrvLost { get { return srvState == uint.MaxValue; } }
        public void SrvLosted(bool updateTray = true) { UpdateInfo(uint.MaxValue, updateTray); }
        public void UpdateInfo(uint? srvStatus = null, bool updateTray = true)
        {
            if (srvStatus != null) srvState = (uint)srvStatus;
            if (updateTray == false) return;

            if (Settings.Instance.ShowTray == false)
            {
                taskTray.Text = "";
                return;
            }

            var sortList = CommonManager.Instance.DB.ReserveList.Values
                .Where(info => info.IsEnabled == true && info.IsOver() == false)
                .OrderBy(info => info.StartTimeActual).ToList();

            bool isOnPreRec = false;
            string infoText = IsSrvLost == true ? "[未接続]\r\n(?)" : "";
            infoText += srvState == 2 ? "EPG取得中\r\n" : "";

            if (sortList.Count == 0)
            {
                infoText += "次の予約なし";
            }
            else
            {
                int infoCount = 1;
                if (sortList[0].IsOnRec() == true)
                {
                    infoText += "録画中:";
                    infoCount = sortList.Count(info => info.IsOnRec());
                }
                else
                {
                    var PreRecTime = DateTime.UtcNow.AddHours(9).AddMinutes(Settings.Instance.RecAppWakeTime);
                    isOnPreRec = sortList[0].OnTime(PreRecTime) >= 0;
                    if (isOnPreRec == true) //録画準備中
                    {
                        infoText += "録画準備中:";
                        infoCount = sortList.Count(info => info.OnTime(PreRecTime) >= 0);//あまり意味無い
                    }
                    else if (Settings.Instance.UpdateTaskText == true && sortList[0].OnTime(PreRecTime.AddMinutes(30)) >= 0) //30分以内に録画準備に入るもの
                    {
                        infoText += "まもなく録画:";
                        infoCount = sortList.Count(info => info.OnTime(PreRecTime.AddMinutes(30)) >= 0);
                    }
                    else
                    {
                        infoText += "次の予約:";
                    }
                }

                infoText += sortList[0].StationName + " " + new ReserveItem(sortList[0]).StartTimeShort + " " + sortList[0].Title;
                string endText = (infoCount <= 1 ? "" : "\r\n他" + (infoCount - 1).ToString());
                infoText = CommonUtil.LimitLenString(infoText, 63 - endText.Length) + endText;
            }

            taskTray.Text = infoText;

            if (IsSrvLost == true)          taskTray.Icon = TaskIconSpec.TaskIconGray;
            else if (srvState == 1)         taskTray.Icon = TaskIconSpec.TaskIconRed;
            else if (isOnPreRec == true)    taskTray.Icon = TaskIconSpec.TaskIconOrange;
            else if (srvState == 2)         taskTray.Icon = TaskIconSpec.TaskIconGreen;
            else                            taskTray.Icon = TaskIconSpec.TaskIconBlue;
        }
    }
}
