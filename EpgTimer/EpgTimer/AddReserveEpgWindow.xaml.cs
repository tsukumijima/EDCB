﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EpgTimer
{
    /// <summary>
    /// AddReserveEpgWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AddReserveEpgWindow : Window
    {
        private EpgEventInfo eventInfo = null;

        public AddReserveEpgWindow()
        {
            InitializeComponent();
        }

        public void SetOpenMode(int mode)
        {
            tabControl.SelectedIndex = mode == 0 && tabItem_reserve.IsEnabled ? 0 : 1;
        }

        public int GetOpenMode()
        {
            return tabControl.SelectedIndex == 0 ? 0 : 1;
        }

        public void SetReservable(bool reservable)
        {
            tabItem_reserve.IsEnabled = reservable;
            button_add_reserve.IsEnabled = reservable;
            SetOpenMode(tabControl.SelectedIndex);
        }

        public void SetEventInfo(EpgEventInfo eventData)
        {
            eventInfo = eventData;
            textBox_info.Text = CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.BasicInfo);
            richTextBox_descInfo.Document = new FlowDocument(CommonManager.ConvertDisplayText(
                CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.BasicText),
                CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.ExtendedText),
                CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.PropertyInfo)));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedItem != null)
            {
                ((TabItem)tabControl.SelectedItem).Focus();
            }
        }

        private void button_add_reserve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (eventInfo.StartTimeFlag == 0)
                {
                    MessageBox.Show("開始時間未定のため予約できません");
                    return;
                }

                ReserveData reserveInfo = new ReserveData();
                if (eventInfo.ShortInfo != null)
                {
                    reserveInfo.Title = eventInfo.ShortInfo.event_name;
                }

                reserveInfo.StartTime = eventInfo.start_time;
                reserveInfo.StartTimeEpg = eventInfo.start_time;

                if (eventInfo.DurationFlag == 0)
                {
                    reserveInfo.DurationSecond = 10 * 60;
                }
                else
                {
                    reserveInfo.DurationSecond = eventInfo.durationSec;
                }

                ulong key = CommonManager.Create64Key(eventInfo.original_network_id, eventInfo.transport_stream_id, eventInfo.service_id);
                if (ChSet5.Instance.ChList.ContainsKey(key) == true)
                {
                    reserveInfo.StationName = ChSet5.Instance.ChList[key].ServiceName;
                }
                reserveInfo.OriginalNetworkID = eventInfo.original_network_id;
                reserveInfo.TransportStreamID = eventInfo.transport_stream_id;
                reserveInfo.ServiceID = eventInfo.service_id;
                reserveInfo.EventID = eventInfo.event_id;
                reserveInfo.RecSetting = recSettingView.GetRecSetting();

                List<ReserveData> list = new List<ReserveData>();
                list.Add(reserveInfo);
                ErrCode err = CommonManager.CreateSrvCtrl().SendAddReserve(list);
                if (err != ErrCode.CMD_SUCCESS)
                {
                    MessageBox.Show(CommonManager.GetErrCodeText(err) ?? "予約登録でエラーが発生しました。終了時間がすでに過ぎている可能性があります。");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            Close();
        }

        private void button_save_program_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.FileName = "a.program.txt";
            dlg.Filter = "txt Files|*.txt|all Files|*.*";
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using (var file = new StreamWriter(dlg.FileName, false, Encoding.UTF8))
                    {
                        file.Write(CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.BasicInfoForProgramText));
                        file.Write(CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.BasicTextForProgramText));
                        file.Write(CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.ExtendedTextForProgramText));
                        file.Write(CommonManager.ConvertProgramText(eventInfo, EventInfoTextMode.PropertyInfo));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.S:
                        // バインディング更新のためフォーカスを移す
                        button_add_reserve.Focus();
                        button_add_reserve.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        e.Handled = true;
                        break;
                }
            }
            else if (Keyboard.Modifiers == ModifierKeys.None)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        Close();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void button_cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
