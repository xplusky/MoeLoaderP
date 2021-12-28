using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf
{
    /// <summary>
    /// LogWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LogWindow : Window
    {
        public Settings Settings { get; set; }
        public LogWindow()
        {
            InitializeComponent();
            CopyButton.Click+= CopyButtonOnClick;
            ClearButton.Click+= ClearButtonOnClick;
            LogListBox.SelectionChanged += LogListBoxOnSelectionChanged;
        }

        private void LogListBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LogListBox.SelectedItems.Count > 0)
            {
                CopyButton.IsEnabled = true;
            }
            else
            {
                CopyButton.IsEnabled = false;
            }

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
        }

        private void LogCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var sv = LogListBox.Template.FindName("HostScrollViewer", LogListBox) as ScrollViewer;
            sv?.ScrollToEnd();
        }


        public void Log(string text)
        {
            var tb = new TextBlock
            {
                Text = text
            };
            LogListBox.Items.Add(tb);
            LogListBox.ScrollIntoView(tb);
            if (LogListBox.Items.Count > 500) LogListBox.Items.RemoveAt(0);
        }
    }
}
