using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MoeLoader.UI
{
    public class KeywordComboBox : ComboBox
    {
        public TextBox EditTextBox => (TextBox)Template.FindName("EditableTextBox", this);

        public string KeywordText
        {
            get => EditTextBox.Text;
            set => EditTextBox.Text = value;
        }


        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(new MouseWheelEventArgs(e.MouseDevice,0,0));
        }

        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
        {
            base.OnPreviewMouseWheel(new MouseWheelEventArgs(e.MouseDevice, 0, 0));
        }

    }
}
