using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;

namespace EpgTimer
{
    public class TaskTrayClass : IDisposable
    {
        // NotifyIconの生成は初回のVisibleまで遅延
        private NotifyIcon notifyIcon;
        private string _text = "";
        private Uri _iconUri;
        private List<KeyValuePair<string, EventHandler>> _contextMenuList;

        public event EventHandler Click;

        public string Text
        {
            get { return _text; }
            set
            {
                _text = value;
                if (notifyIcon != null)
                {
                    notifyIcon.Text = CommonUtil.LimitLenString(_text, 63); ;
                }
            }
        }
        public Uri IconUri
        {
            get { return _iconUri; }
            set
            {
                _iconUri = value;
                if (notifyIcon != null)
                {
                    if (IconUri != null)
                    {
                        using (var stream = System.Windows.Application.GetResourceStream(IconUri).Stream)
                        {
                            System.Drawing.Size size = SystemInformation.SmallIconSize;
                            notifyIcon.Icon = new Icon(stream, (size.Width + 15) / 16 * 16, (size.Height + 15) / 16 * 16);
                        }
                    }
                    else
                    {
                        notifyIcon.Icon = null;
                    }
                }
            }
        }
        public List<KeyValuePair<string, EventHandler>> ContextMenuList
        {
            get { return _contextMenuList; }
            set
            {
                _contextMenuList = value;
                if (notifyIcon != null)
                {
                    if (ContextMenuList != null && ContextMenuList.Count > 0)
                    {
                        var menu = new ContextMenuStrip();
                        foreach (var item in ContextMenuList)
                        {
                            if (item.Key != null)
                            {
                                var newcontitem = new ToolStripMenuItem();
                                newcontitem.Text = item.Key;
                                newcontitem.Click += item.Value;
                                menu.Items.Add(newcontitem);
                            }
                            else
                            {
                                menu.Items.Add(new ToolStripSeparator());
                            }
                        }
                        notifyIcon.ContextMenuStrip = menu;
                    }
                    else
                    {
                        notifyIcon.ContextMenuStrip = null;
                    }
                }
            }
        }
        public int ForceHideBalloonTipSec { get; set; }

        public bool Visible
        {
            get { return notifyIcon != null && notifyIcon.Visible; }
            set
            {
                if (notifyIcon != null)
                {
                    notifyIcon.Visible = value;
                }
                else if (value)
                {
                    notifyIcon = new NotifyIcon();
                    notifyIcon.Click += (sender, e) =>
                    {
                        var mouseEvent = e as MouseEventArgs;
                        if (mouseEvent != null && mouseEvent.Button == MouseButtons.Left)
                        {
                            // 左クリック
                            if (Click != null)
                            {
                                Click(sender, e);
                            }
                        }
                    };

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
                        if (ForceHideBalloonTipSec > 0)
                        {
                            balloonTimer.Interval = TimeSpan.FromSeconds(ForceHideBalloonTipSec);
                            balloonTimer.Start();
                        }
                    };
                    notifyIcon.BalloonTipClicked += (sender, e) => balloonTimer.Stop();
                    notifyIcon.BalloonTipClosed += (sender, e) => balloonTimer.Stop();

                    // プロパティ反映のため
                    Text = Text;
                    IconUri = IconUri;
                    ContextMenuList = ContextMenuList;
                    notifyIcon.Visible = true;
                }
            }
        }
        /// <summary> timeOutMSecは設定しても効かない環境がある </summary>
        public void ShowBalloonTip(string title, string tips, int timeOutMSec = 10 * 1000)
        {
            if (notifyIcon != null)
            {
                title = string.IsNullOrEmpty(title) == true ? " " : title;
                tips = string.IsNullOrEmpty(tips) == true ? " " : tips;
                notifyIcon.ShowBalloonTip(timeOutMSec, title, tips, ToolTipIcon.Info);
            }
        }
        public void Dispose()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Dispose();
                notifyIcon = null;
            }
        }
    }

    public static class TrayManager
    {
        private static TaskTrayClass taskTray = new TaskTrayClass();
        public static TaskTrayClass Tray { get { return taskTray; } }

        private static uint srvState = uint.MaxValue;
        public static bool IsSrvLost { get { return srvState == uint.MaxValue; } }
        public static void SrvLosted(bool updateTray = true) { UpdateInfo(uint.MaxValue, updateTray); }
        public static void UpdateInfo(uint? srvStatus = null, bool updateTray = true)
        {
            if (srvStatus != null) srvState = (uint)srvStatus;
            if (Settings.Instance.ShowTray == false || updateTray == false) return;

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

            if      (IsSrvLost == true)  taskTray.IconUri = new Uri("pack://application:,,,/Resources/TaskIconGray.ico");
            else if (srvState == 1)      taskTray.IconUri = new Uri("pack://application:,,,/Resources/TaskIconRed.ico");
            else if (isOnPreRec == true) taskTray.IconUri = new Uri("pack://application:,,,/Resources/TaskIconOrange.ico");
            else if (srvState == 2)      taskTray.IconUri = new Uri("pack://application:,,,/Resources/TaskIconGreen.ico");
            else                         taskTray.IconUri = new Uri("pack://application:,,,/Resources/EpgTimer_Bon_Vista_blue_rev2.ico");
        }
    }
}
