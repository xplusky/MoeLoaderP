using System;
using System.Windows;

namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
/// 自带调整按钮的数字文本框
/// </summary>
public partial class VariableCountControl
{
    public VariableCountControl()
    {
        InitializeComponent();
            
        NumUpButton.Click+= NumUpButtonOnClick;
        NumDownButton.Click += NumDownButtonOnClick;
        CountTextBox.LostFocus += CountTextBoxOnLostFocus;
            
    }

    public event Action<VariableCountControl> NumChange;
        
    private void CountTextBoxOnLostFocus(object sender, RoutedEventArgs e)
    {
        var b = int.TryParse(CountTextBox.Text, out var count);
        if(b == false) return;
        if (count > MaxCount) NumCount = MaxCount;
        else if (count < MinCount) NumCount = MinCount;
        else NumCount = count;
    }

    private void NumDownButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (NumCount <= MinCount) return;
        NumCount -= 1;
    }

    private void NumUpButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (NumCount >= MaxCount) return;
        NumCount += 1;
    }
        
    public int NumCount
    {
        get => (int)GetValue(NumCountProperty);
        set
        {
            SetValue(NumCountProperty, value);
            NumChange?.Invoke(this);
        }
    }

    public static readonly DependencyProperty NumCountProperty = DependencyProperty.Register(nameof(NumCount), typeof(int), typeof(VariableCountControl), new PropertyMetadata(0));


    public int MaxCount
    {
        get => (int)GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register(nameof(MaxCount), typeof(int), typeof(VariableCountControl), new PropertyMetadata(int.MaxValue));

        
    public int MinCount
    {
        get => (int)GetValue(MinCountProperty);
        set => SetValue(MinCountProperty, value);
    }

    public static readonly DependencyProperty MinCountProperty = DependencyProperty.Register(nameof(MinCount), typeof(int), typeof(VariableCountControl), new PropertyMetadata(0));


}