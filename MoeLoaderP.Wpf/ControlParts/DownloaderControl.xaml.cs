using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts
{
    public partial class DownloaderControl
    {
        public Settings Settings { get; set; }
        public MoeDownloader Downloader { get; set; }
        public DispatcherTimer Timer { get; set; } = new DispatcherTimer();
        public DownloaderControl()
        {
            InitializeComponent();
        }

        public void Init(Settings settings)
        {
            Settings = settings;
            Downloader = new MoeDownloader(Settings);

            DownloadItemsListBox.ItemsSource = Downloader.DownloadItems;
            DownloadItemsListBox.MouseRightButtonUp += DownloadItemsListBoxOnMouseRightButtonUp;
            OpenFolderButton.Click += OpenFolderButtonOnClick;
            DeleteAllButton.Click += DeleteAllButtonOnClick;
            DeleteButton.Click += DeleteButtonOnClick;
            StopButton.Click += StopButtonOnClick;
            SelectAllButton.Click += SelectAllButtonOnClick;
            RetryButton.Click += RetryButtonOnClick;
            KeyDown += OnKeyDown;

            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += TimerOnTick;
            Timer.Start();
        }

        private void TimerOnTick(object sender, EventArgs e)
        {
            Downloader.TimerOnTick(sender, e);
        }
        
        public DownloadItems CastSelectToDwDownloadItems()
        {
            var selectItems = DownloadItemsListBox.SelectedItems;
            var lb = DownloadItemsListBox;

            var di = new DownloadItems();
            foreach (var selectItem in selectItems)
            {
                var i = lb.Items.IndexOf(selectItem);
                di.Add(Downloader.DownloadItems[i]);
            }
            return di;
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                DownloadItemsListBox.SelectAll();
            }
        }
        private void DownloadItemsListBoxOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenuPopup.IsOpen = true;
            ContextMenuPopupGrid.EnlargeShowSb().Begin();
        }

        #region 右键菜单

        private void RetryButtonOnClick(object sender, RoutedEventArgs e)
        {
            Downloader.Retry(CastSelectToDwDownloadItems());
            ContextMenuPopup.IsOpen = false;
        }


        private void SelectAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            DownloadItemsListBox.SelectAll();
            ContextMenuPopup.IsOpen = false;
        }

        private void StopButtonOnClick(object sender, RoutedEventArgs e)
        {
            Downloader.Stop(CastSelectToDwDownloadItems());

            ContextMenuPopup.IsOpen = false;
        }

        private void DeleteButtonOnClick(object sender, RoutedEventArgs e)
        {
            Downloader.Delete(CastSelectToDwDownloadItems());
            ContextMenuPopup.IsOpen = false;
        }

        private void DeleteAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            Downloader.DeleteAllSuccess();
            ContextMenuPopup.IsOpen = false;
        }

        private void OpenFolderButtonOnClick(object sender, RoutedEventArgs e)
        {
            var item = CastSelectToDwDownloadItems().FirstOrDefault();
            if (item?.ChildrenItems.Count > 0)
            {
                item.ChildrenItems[0].LocalFileFullPath.GoFile();
            }
            else
            {
                item?.LocalFileFullPath.GoFile();
            }
            ContextMenuPopup.IsOpen = false;
        }

        #endregion

    }
}