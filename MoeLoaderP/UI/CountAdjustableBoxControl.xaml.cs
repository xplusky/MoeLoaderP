using System.Windows;

namespace MoeLoader.UI
{
    /// <summary>
    /// 自带调整按钮的数字文本框
    /// </summary>
    public partial class CountAdjustableBoxControl
    {
        public CountAdjustableBoxControl()
        {
            InitializeComponent();
            
            NumUpButton.Click+= NumUpButtonOnClick;
            NumDownButton.Click += NumDownButtonOnClick;
            CountTextBox.LostFocus += CountTextBoxOnLostFocus;
            
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
