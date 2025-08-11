using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;

namespace EpgTimer
{
    public class SelectableItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool selected = false;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public bool IsSelected
        {
            get { return this.selected; }
            set
            {
                this.selected = value;
                NotifyPropertyChanged("IsSelected");
                NotifyPropertyChanged("IsSelectedViewCmd");
            }
        }
        public virtual bool IsSelectedViewCmd
        {
            get { return this.IsSelected; }
            set
            {
                CMD_CheckBox_PreviewChanged.Execute(this, null);
            }
        }

        public static RoutedCommand CMD_CheckBox_PreviewChanged = new RoutedCommand();
        public static void Set_CheckBox_PreviewChanged(ListBox box, Action hdlr = null)
        {
            Action SelectedItemsChange = () =>
            {
                foreach (var item in box.SelectedItems.OfType<SelectableItem>())
                {
                    item.IsSelected = !item.IsSelected;
                }
            };

            //キー操作(スペースキー)
            box.PreviewKeyDown += new KeyEventHandler((sender, e) =>
            {
                if (e.Handled == false && Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Space && e.IsRepeat == false)
                {
                    e.Handled = true;
                    (hdlr ?? SelectedItemsChange)();
                }
            });
            //マウス操作
            box.CommandBindings.Add(new CommandBinding(SelectableItem.CMD_CheckBox_PreviewChanged, (sender, e) =>
            {
                if (box.SelectedItems.Contains(e.Parameter) == false)
                {
                    box.SelectedItem = e.Parameter;
                }
                (hdlr ?? SelectedItemsChange)();
            }));
        }
    }

    public class SelectableItemNWMode : SelectableItem
    {
        public bool IsEnabled { get { return CommonManager.Instance.NWMode == false; } }
    }
}
