using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    public partial class DownloaderPanelControl
    {
        public Settings Settings { get; set; }
        public DownloadItems DownloadItems { get; set; } = new DownloadItems();
        public DownloadItems DownloadingItemsPool { get; set; } = new DownloadItems();
        public DownloadItems WaitForDownloadItemsPool { get; set; } = new DownloadItems();
        public bool IsDownloading => WaitForDownloadItemsPool.Count > 0;

        public DownloaderPanelControl()
        {
            InitializeComponent();
            DownloadItemsListBox.ItemsSource = DownloadItems;
            DownloadItemsListBox.MouseRightButtonUp += DownloadItemsListBoxOnMouseRightButtonUp;
            OpenFolderButton.Click += OpenFolderButtonOnClick;
            DeleteAllButton.Click += DeleteAllButtonOnClick;
            DeleteButton.Click += DeleteButtonOnClick;
            StopButton.Click += StopButtonOnClick;
            StartButton.Click += StartButtonOnClick;
            SelectAllButton.Click += SelectAllButtonOnClick;
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                DownloadItemsListBox.SelectAll();
            }
        }

        private void SelectAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            DownloadItemsListBox.SelectAll();
        }

        private void StartButtonOnClick(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < DownloadItemsListBox.SelectedItems.Count; i++)
            {
                var item = DownloadItemsListBox.SelectedItems[i];
                var index = DownloadItemsListBox.Items.IndexOf(item);
                if (index == -1) continue;
                if (DownloadItems[index].DownloadStatus != DownloadStatusEnum.Downloading &&
                    DownloadItems[index].DownloadStatus != DownloadStatusEnum.WaitForDownload &&
                    DownloadItems[index].DownloadStatus != DownloadStatusEnum.Success)
                {
                    AddDownload(DownloadItems[index]);
                    DownloadItems[index].DownloadStatus = DownloadStatusEnum.WaitForDownload;
                }
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void StopButtonOnClick(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < DownloadItemsListBox.SelectedItems.Count; i++)
            {
                var item = DownloadItemsListBox.SelectedItems[i];
                var index = DownloadItemsListBox.Items.IndexOf(item);
                if (index == -1) continue;
                if (DownloadItems[index].DownloadStatus != DownloadStatusEnum.Success)
                {
                    DownloadItems[index].CurrentDownloadTaskCts?.Cancel();
                    DownloadItems[index].DownloadStatus = DownloadStatusEnum.Cancel;
                }
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void DeleteButtonOnClick(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < DownloadItemsListBox.SelectedItems.Count; i++)
            {
                var item = DownloadItemsListBox.SelectedItems[i];
                var index = DownloadItemsListBox.Items.IndexOf(item);
                if (index == -1) continue;
                DownloadItems[index].CurrentDownloadTaskCts?.Cancel();
                DownloadItems[index].DownloadStatus = DownloadStatusEnum.Cancel;
                DownloadItems.RemoveAt(index);
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void DeleteAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            for (var i = 0; i < DownloadItems.Count; i++)
            {
                var item = DownloadItems[i];
                if (item.DownloadStatus == DownloadStatusEnum.Success)
                {
                    DownloadItems.Remove(item);
                    i--;
                }
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void OpenFolderButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (DownloadItemsListBox.SelectedIndex >= 0)
            {
                var path = Path.GetDirectoryName(DownloadItems[DownloadItemsListBox.SelectedIndex].LocalFileFullPath);
                path?.Go();
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void DownloadItemsListBoxOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenuPopup.IsOpen = true;
            ContextMenuPopupGrid.LargenShowSb().Begin();
        }

        public void Init(Settings settings)
        {
            Settings = settings;
        }

        public void AddDownload(DownloadItem downitem)
        {
            downitem.DownloadStatusChanged += di => DownloadStatusChanged();
            DownloadItems.Add(downitem);
            //DownloadItemsListBox.Items.Refresh();
            if (DownloadingItemsPool.Count < Settings.MaxOnDownloadingImageCount)
            {
                DownloadingItemsPool.Add(downitem);
            }
            else
            {
                WaitForDownloadItemsPool.Add(downitem);
            }
            DownloadStatusChanged();
            
        }

        public void AddDownload(ImageItem item,ImageSource img)
        {
            var downitem = new DownloadItem
            {
                Settings = Settings,
                ImageSource = img,
                ImageItem = item,
                OriginFileName = Path.GetFileName(item.DownloadUrlInfo.Url)
            };
            if (item.ChilldrenItems.Count > 0)
            {
                for (var i = 0; i < item.ChilldrenItems.Count; i++)
                {
                    var subitem = item.ChilldrenItems[i];
                    var downsubitem = new DownloadItem
                    {
                        Settings = Settings,
                        ImageItem = subitem,
                        OriginFileName = Path.GetFileName(subitem.DownloadUrlInfo.Url),
                        SubIndex = i + 1
                    };
                    downitem.SubItems.Add(downsubitem);
                }
            }
            downitem.GenFileNameWithouExt();
            AddDownload(downitem);
            var sv = (ScrollViewer)DownloadItemsListBox.Template.FindName("DownloadListScrollViewer", DownloadItemsListBox);
            sv.ScrollToEnd();
        }
        
        public void StopAll()
        {

        }

        public void DownloadStatusChanged()
        {
            for (var i = 0; i < DownloadingItemsPool.Count; i++)
            {
                var item = DownloadingItemsPool[i];

                switch (item.DownloadStatus)
                {
                    case DownloadStatusEnum.WaitForDownload:
                    {
                        var _ = item.DownloadFileAsync();
                            break;
                    }
                    case DownloadStatusEnum.Downloading:break;
                    default:
                    {
                        DownloadingItemsPool.RemoveAt(i);
                        i--;
                        if (WaitForDownloadItemsPool.Count > 0)
                        {
                            var first = WaitForDownloadItemsPool.First();
                            DownloadingItemsPool.Add(first);
                            WaitForDownloadItemsPool.RemoveAt(0);
                        }break;
                    }
                }
            }
        }
    }
}