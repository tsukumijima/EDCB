using System;
using System.Windows;

namespace EpgTimer
{
    using PresetEditor;

    /// <summary>
    /// AddPresetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AddPresetWindow : Window
    {
        public AddPresetWindow()
        {
            InitializeComponent();
            button_ok.Click += (sender, e) => DialogResult = true;
            button_cancel.Click += (sender, e) => DialogResult = false;
        }

        public void SetMode(PresetEdit chgMode, string title = "プリセット")
        {
            Title = title + new[] { "追加", "変更", "削除" }[(int)chgMode];
            button_ok.Content = new[] { "追加", "変更", "削除" }[(int)chgMode];
            label_chgMsg.Text = new[] { "", "(※設定内容も同時に変更されます)", "(※プリセットを削除します)" }[(int)chgMode];
            textBox_name.SetReadOnlyWithEffect(chgMode == PresetEdit.Delete);
        }

        public void SetName(String name)
        {
            textBox_name.Text = name;
        }

        public String GetName()
        {
            return textBox_name.Text;
        }
    }
}
