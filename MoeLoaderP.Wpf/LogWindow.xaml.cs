using System.Windows;
using System.Windows.Controls;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf;

/// <summary>
/// LogWindow.xaml 的交互逻辑
/// </summary>
public partial class LogWindow
{
    public Settings Settings { get; set; }
    public LogWindow()
    {
        InitializeComponent();
        CopyButton.Click+= CopyButtonOnClick;
        ClearButton.Click+= ClearButtonOnClick;
        LogListBox.SelectionChanged += LogListBoxOnSelectionChanged;
        Owner = Application.Current.MainWindow;
    }

    private void LogListBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        CopyButton.IsEnabled = LogListBox.SelectedItems.Count > 0;

        CopyButton.Content = $"复制{LogListBox.SelectedItems.Count}项";
    }

    private void ClearButtonOnClick(object sender, RoutedEventArgs e)
    {
        Ex.LogCollection.Clear();
    }

    private void CopyButtonOnClick(object sender, RoutedEventArgs e)
    {
        var col = LogListBox.SelectedItems;
        var strs = "";
        foreach (var str in col)
        {
            strs += str + "\r\n";
        }
        strs.CopyToClipboard();
    }

    public void Init(Settings settings)
    {
        LogListBox.ItemsSource = Ex.LogCollection;
        Settings = settings;
        DataContext = Settings;
        Ex.LogCollection.CollectionChanged += LogCollection_CollectionChanged;
        this.SetWindowFluent(settings);
    }

    private void LogCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        var sv = LogListBox.Template.FindName("HostScrollViewer", LogListBox) as ScrollViewer;
        sv?.ScrollToEnd();
    }

    
}

public class LogWindowHelper
{
    public LogWindow LogWindow { get; set; }

    public void Init(Button button,Settings settings)
    {
        button.Click += delegate
        {
            if (LogWindow == null)
            {
                LogWindow = new LogWindow();
                LogWindow.Init(settings);
                LogWindow.Show();
                LogWindow.Closed += delegate { LogWindow = null; };
            }
            else
            {
                LogWindow.Activate();
            }

        };
    }
}