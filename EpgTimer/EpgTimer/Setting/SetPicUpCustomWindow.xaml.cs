using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer
{
    public partial class SetPicUpCustomWindow : Window
    {
        //起動中はテストデータを保存しておく
        private static string testString = "";

        //テスト入力のサンプル
        private const string testStringSample = "[二][字]NHKニュース7"
                            + "\r\n" + "[5.1][SS][字]NHK歌謡コンサート「人生の旅路に　この歌を」"
                            + "\r\n" + "午後のロードショー「私がウォシャウスキー」魅惑のヒロイン特集[字][S][二]"
                            + "\r\n" + "【Ｍｏｔｈｅｒ’ｓ　Ｄａｙ　Ｓｐｅｃｉａｌ】【映】バンガー・シスターズ";

        public SetPicUpCustomWindow(Visual owner = null, PicUpTitle set = null)
        {
            InitializeComponent();

            this.Owner = CommonUtil.GetTopWindow(owner);
            SetData(set);

            //テスト入力はキャンセル時も保存
            textbox_TestInput.Text = testString;
            this.Closing += (sender, e) => testString = textbox_TestInput.Text;
            button_ok.Click += (sender, e) => DialogResult = true;
            button_cancel.Click += (sender, e) => DialogResult = false;

            button_ReplaceClear.Click += (sender, e) => textBox_replaceSet.Clear();
            button_ReplaceCopy.Click += (sender, e) => AddSetData(textBox_replaceSet, PicUpTitle.ReplaceSetDefault);
            button_TitleClear.Click += (sender, e) => textBox_titleSet.Clear();
            button_TitleCopy.Click += (sender, e) => AddSetData(textBox_titleSet, PicUpTitle.TitleSetDefault);

            button_TestSample.Click += (sender, e) => textbox_TestInput.Text = testStringSample;
            button_TestClearInput.Click += (sender, e) => textbox_TestInput.Clear();
            button_TestClearResult.Click += (sender, e) => textbox_TestResult.Clear();
        }

        //今は関係無いが、状況によっては set.UseCustom の面倒もちゃんとみないといけない
        public void SetData(PicUpTitle set)
        {
            set = set ?? new PicUpTitle();
            textBox_replaceSet.Text = string.Join("\r\n", set.ReplaceSet);
            textBox_titleSet.Text = string.Join("\r\n", set.TitleSet);
        }
        public PicUpTitle GetData()
        {
            var ret=  new PicUpTitle();
            ret.ReplaceSet.AddRange(textBox_replaceSet.Text.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None));
            ret.TitleSet.AddRange(textBox_titleSet.Text.Trim().Split(new string[] { "\r\n" }, StringSplitOptions.None));
            return ret;
        }

        private void AddSetData(TextBox box, IEnumerable<string> data)
        {
            string txt = box.Text.TrimEnd();
            box.Text = (txt == "" ? "" : txt + "\r\n\r\n") + string.Join("\r\n", data);
            box.ScrollToEnd();
        }

        private void button_TestRun_Click(object sender, RoutedEventArgs e)
        {
            PicUpTitle set = GetData();
            set.UseCustom = true;
            string[] list = textbox_TestInput.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            try
            {
                textbox_TestResult.TextWrapping = TextWrapping.NoWrap;
                textbox_TestResult.Text = string.Join("\r\n", list.Select(s => set.PicUp(s)));
            }
            catch (Exception ex)
            {
                textbox_TestResult.TextWrapping = TextWrapping.Wrap;
                textbox_TestResult.Text = ex.Message;
            }
        }
    }
}
