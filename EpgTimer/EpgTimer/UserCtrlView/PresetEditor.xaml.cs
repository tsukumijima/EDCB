using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EpgTimer
{
    using BoxExchangeEdit;
    using PresetEditor;

    namespace PresetEditor
    {
        public enum PresetEdit { Add = 0, Change = 1, Delete = 2, Set };
    }

    /// <summary>
    /// SearchKey.xaml の相互作用ロジック
    /// </summary>
    public partial class PresetEditorBase : UserControl { public PresetEditorBase() { InitializeComponent(); } }
    public class PresetEditor<S> : PresetEditorBase where S : PresetItem, new()
    {
        private IPresetItemView dView;
        private event Action<S, object> SelectionChanged = (item, msg) => { };
        private event Action<PresetEdit> PresetEdited = (mode) => { };
        private Func<List<S>> PresetSetting = () => { return null; };

        public PresetEditor()
        {
            comboBox_preSet.SelectionChanged += comboBox_preSet_SelectionChanged;
            comboBox_preSet.KeyDown += ViewUtil.KeyDown_Enter(button_reload);
            button_add.Click += (sender, e) => button_Click(PresetEdit.Add, sender, e);
            button_chg.Click += (sender, e) => button_Click(PresetEdit.Change, sender, e);
            button_del.Click += (sender, e) => button_Click(PresetEdit.Delete, sender, e);
            button_reload.Click += (sender, e) => comboBox_preSet_SelectionChanged(null, null);
            button_set.Click += button_set_Click;
        }

        public void Set(IPresetItemView view, Action<S, object> selectionChanged, Action<List<S>, PresetEdit> presetEdited,
            string title = "プリセット", Func<Visual, IEnumerable<S>, List<S>> setList = null)
        {
            SelectionChanged = selectionChanged;
            PresetEdited = (mode) => presetEdited(PresetList, mode);
            PresetSetting = () => setList == null ? null : setList(this, PresetList);
            dView = view;
            comboBox_preSet.Items.AddItems(dView.DefPresetList());
            comboBox_preSet.SelectedIndex = 0;
            txt_title.Text = title ?? "プリセット";
            if (PresetSetting == null) button_set.Visibility = Visibility.Collapsed;
        }

        public IEnumerable<S> Items { get { return comboBox_preSet.Items.OfType<S>(); } }
        public List<S> PresetList { get { return Items.Where(item => item.IsCustom == false).DeepClone().FixUp(); } }

        public S FindPreset(int presetID)
        {
            return Items.FirstOrDefault(item => item.ID == presetID);
        }

        private bool changeSelect_noEvent = false;
        private object changeSelect_msg = null;
        public void ChangeSelect(int index, object msg = null, bool noEvent = false)
        {
            ChangeSelect(comboBox_preSet.Items[index], msg, noEvent);
        }
        public void ChangeSelect(object item, object msg = null, bool noEvent = false)
        {
            comboBox_preSet.SelectedItem = null;
            if (item == null) return;
            changeSelect_noEvent = noEvent;
            changeSelect_msg = msg;
            comboBox_preSet.SelectedItem = item;
        }

        private void comboBox_preSet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var item = comboBox_preSet.SelectedItem as S;
                if (item == null) return;

                button_chg.IsEnabled = !item.IsCustom;
                button_del.IsEnabled = button_chg.IsEnabled;

                if (changeSelect_noEvent == false) SelectionChanged(item, changeSelect_msg);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            changeSelect_noEvent = false;
            changeSelect_msg = null;
        }

        private void button_Click(PresetEdit mode, object sender, RoutedEventArgs e)
        {
            try
            {
                var item = comboBox_preSet.SelectedItem as S;
                if (mode != PresetEdit.Add && item == null) return;

                var setting = new AddPresetWindow { Owner = CommonUtil.GetTopWindow(this) };
                setting.SetMode(mode, this.txt_title.Text);
                if (mode != PresetEdit.Add) setting.PresetName = item.DisplayName;
                if (setting.ShowDialog() == true)
                {
                    int index = comboBox_preSet.SelectedIndex;
                    switch (mode)
                    {
                        case PresetEdit.Add:
                            index = Items.Count(it => it.IsCustom == false);
                            var newInfo = new S { DisplayName = setting.PresetName, ID = 0, Data = dView.GetData() };
                            comboBox_preSet.Items.Insert(index, newInfo);
                            break;
                        case PresetEdit.Change:
                            item.DisplayName = setting.PresetName;
                            item.Data = dView.GetData();
                            break;
                        case PresetEdit.Delete:
                            index = Math.Max(0, Math.Min(index, comboBox_preSet.Items.Count - 2));
                            comboBox_preSet.Items.Remove(item);
                            break;
                    }
                    comboBox_preSet.Items.Refresh();
                    ChangeSelect(index, null, true);
                    PresetEdited(mode);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void button_set_Click(object sender, RoutedEventArgs e)
        {
            var setList = PresetSetting();
            if (setList == null) return;

            var keepList = Items.Where(item => item.IsCustom == true).ToList();
            comboBox_preSet.Items.Clear();
            comboBox_preSet.Items.AddItems(setList.Concat(keepList));
            PresetEdited(PresetEdit.Set);
        }

        public ContextMenu CreateSlelectMenu(Func<S, bool> isMenuChecked = null, Func<S, bool> isMenuEnabled = null, RoutedEventHandler clicked = null)
        {
            var ctxm = new ContextMenuEx();
            int i = 0;
            foreach (var item in Items)
            {
                var menu = new MenuItem() { Header = item.DeepClone(), Tag = i++ };
                menu.Click += clicked ?? SelectMenuClicked;
                if (isMenuChecked != null) menu.IsChecked = isMenuChecked(menu.Header as S);
                if (isMenuEnabled != null) menu.IsEnabled = isMenuEnabled(menu.Header as S);
                ctxm.Items.Add(menu);
            }
            return ctxm;
        }
        private void SelectMenuClicked(object sender, RoutedEventArgs e)
        {
            comboBox_preSet.SelectedIndex = -1;
            comboBox_preSet.SelectedIndex = Math.Min((int)(sender as MenuItem).Tag, comboBox_preSet.Items.Count - 1);
        }
    }
}