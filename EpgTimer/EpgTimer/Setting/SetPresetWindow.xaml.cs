using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer
{
    using BoxExchangeEdit;

    public interface IPresetItemView
    {
        void SetChangeMode(int chgMode);
        void SetData(object data);
        object GetData();
        IEnumerable<PresetItem> DefPresetList();
    }

    /// <summary>
    /// SetDefRecSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SetPresetWindowBase : Window { }
    public class SetPresetWindow<S, V, W> : SetPresetWindowBase
        where S : PresetItem, new()
        where V : UIElement, IPresetItemView, new()
        where W : SetPresetWindow<S, V, W>
    {
        public V DataView = new V();

        public SetPresetWindow(Visual owner = null, IEnumerable<S> presetlist = null)
        {
            InitializeComponent();

            this.Owner = CommonUtil.GetTopWindow(owner);

            this.Loaded += (sender, e) => { if (presetlist != null)SetPresetList(presetlist); };
            button_add.Click += (sender, e) => listBox_preset.ScrollIntoViewLast(new S { DisplayName = textBox_preset.Text, ID = 0, Data = DataView.GetData() });
            button_iniLoad.Click += (sender, e) => SetPresetList(DataView.DefPresetList().OfType<S>());
            listBox_preset.SelectionChanged += listBox_preset_SelectionChanged;
            textBox_preset.TextChanged += textBox_preset_TextChanged;

            button_cancel.Click += (sender, e) => DialogResult = false;
            button_ok.Click += (sender, e) =>
            {
                listBox_preset.SelectedIndex = -1;//listBox_preset_SelectionChanged()を走らせる
                DialogResult = true;
            };

            //リストボックスの設定
            var bx = new BoxExchangeEditor(null, listBox_preset, true, true, true);
            button_up.Click += bx.button_Up_Click;
            button_down.Click += bx.button_Down_Click;
            button_top.Click += bx.button_Top_Click;
            button_bottom.Click += bx.button_Bottom_Click;
            button_del.Click += bx.button_Delete_Click;

            //ビューを登録,簡易編集用のバーを非表示に
            grid_Data.Children.Add(DataView);
            SetSettingMode(null, int.MaxValue);
        }

        public virtual void SetSettingMode(string title, int chgMode = -1)
        {
            if (title != null) this.Title = title;
            this.Height = chgMode == int.MaxValue ? 650 : 530;
            grid_PresetEdit.Visibility = chgMode == int.MaxValue ? Visibility.Visible : Visibility.Collapsed;
            button_iniLoad.Visibility = grid_PresetEdit.Visibility;
            DataView.SetChangeMode(chgMode);
        }

        public virtual void SetPresetList(IEnumerable<S> srclist)
        {
            listBox_preset.Items.Clear();
            listBox_preset.Items.AddItems(srclist.DeepClone());
            listBox_preset.SelectedIndex = 0;
        }
        public virtual List<S> GetPresetList()
        {
            return listBox_preset.Items.OfType<S>().DeepClone().FixUp();
        }

        PresetItem currentItem = null;
        protected virtual void listBox_preset_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currentItem != null)
            {
                currentItem.Data = DataView.GetData();//現在の選択データ更新
            }

            currentItem = listBox_preset.SelectedItem as PresetItem;//新しい選択データ取得
            if (currentItem == null) return;

            DataView.SetData(currentItem.Data);

            noTextChange = true;//リストボックス内をキーボードで操作しているときのフォーカス対策
            textBox_preset.Text = currentItem.DisplayName;
            noTextChange = false;
        }

        bool noTextChange = false;
        protected virtual void textBox_preset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (currentItem == null || noTextChange == true) return;
            currentItem.DisplayName = textBox_preset.Text;
            listBox_preset.Items.Refresh();
        }

        public static List<S> SettingWithDialog(Visual owner, IEnumerable<S> presetList = null)
        {
            var dlg = (W)Activator.CreateInstance(typeof(W), owner, presetList ?? new List<S>());
            return dlg.ShowDialog() == true ? dlg.GetPresetList() : null;
        }
    }

    public class SetSearchPresetWindow : SetPresetWindow<SearchPresetItem, SearchKeyView, SetSearchPresetWindow>
    {
        public SetSearchPresetWindow(Visual owner = null, IEnumerable<SearchPresetItem> presetlist = null) : base(owner, presetlist) { }
    }

    public class SetRecPresetWindow : SetPresetWindow<RecPresetItem, RecSettingView, SetRecPresetWindow>
    {
        public SetRecPresetWindow(Visual owner = null, IEnumerable<RecPresetItem> presetlist = null) : base(owner, presetlist) { }
        public override void SetSettingMode(string title, int chgMode = -1)
        {
            button_ok.IsEnabled = !(chgMode == int.MaxValue && CommonManager.Instance.NWMode == true);
            button_ok.ToolTip = button_ok.IsEnabled == false ? "EpgTimerNWからは変更出来ません" : null;
            base.SetSettingMode(title ?? "録画プリセット設定", chgMode);
        }
    }
}
