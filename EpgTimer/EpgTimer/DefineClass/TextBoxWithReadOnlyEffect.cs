using System.Windows;
using System.Windows.Controls;

namespace EpgTimer
{
    public class TextBoxWithReadOnlyEffect : TextBox
    {
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == TextBox.IsReadOnlyProperty)
            {
                ViewUtil.SetDisabledEffect(this, IsReadOnly);
            }
        }
    }
}
