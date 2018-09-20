using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MoeLoader.UI
{
    /// <summary>
    /// CountAjdaustableBoxControl.xaml 的交互逻辑
    /// </summary>
    public partial class CountAdjustableBoxControl : UserControl
    {
        public CountAdjustableBoxControl()
        {
            InitializeComponent();
            
            NumUpButton.Click+= NumUpButtonOnClick;
            NumDownButton.Click += NumDownButtonOnClick;
            CountTextBox.TextChanged += CountTextBoxOnTextChanged;
            CountTextBox.LostFocus += CountTextBoxOnLostFocus;
            CountTextBox.GotKeyboardFocus += CountTextBoxOnGotKeyboardFocus;
            CountTextBox.PreviewKeyDown += CountTextBoxOnPreviewKeyDown;
            
        }

        private void CountTextBoxOnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            
        }

        private void CountTextBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // todo
        }

        private void CountTextBoxOnLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var count = int.Parse(CountTextBox.Text);
                if (count > MaxCount) NumCount = MaxCount;
                else if (count < MinCount) NumCount = MinCount;
                else NumCount = count;
            }
            catch
            {
                //NumCount = NumCount;
                
            }
        }

        private void CountTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void NumDownButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (NumCount <= MinCount) return;
            NumCount = NumCount - 1;
        }

        private void NumUpButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (NumCount >= MaxCount) return;
            NumCount = NumCount + 1;
        }
        
        public int NumCount
        {
            get => (int)GetValue(NumCountProperty);
            set => SetValue(NumCountProperty, value);
        }

        public static readonly DependencyProperty NumCountProperty =
            DependencyProperty.Register(nameof(NumCount), typeof(int), typeof(CountAdjustableBoxControl), new PropertyMetadata(0));



        public int MaxCount
        {
            get => (int)GetValue(MaxCountProperty);
            set => SetValue(MaxCountProperty, value);
        }

        public static readonly DependencyProperty MaxCountProperty =
            DependencyProperty.Register("MaxCount", typeof(int), typeof(CountAdjustableBoxControl), new PropertyMetadata(100));




        public int MinCount
        {
            get => (int)GetValue(MinCountProperty);
            set => SetValue(MinCountProperty, value);
        }

        public static readonly DependencyProperty MinCountProperty =
            DependencyProperty.Register("MinCount", typeof(int), typeof(CountAdjustableBoxControl), new PropertyMetadata(0));
        

    }
}
