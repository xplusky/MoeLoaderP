using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    public partial class MoeExplorerControl
    {
        public List<ImageControl> ImageLoadingPool { get; set; } = new List<ImageControl>();
        public List<ImageControl> ImageWaitForLoadingPool { get; set; } = new List<ImageControl>();
        public List<Task> DownloadsTasks { get; set; }
        public Settings Settings { get; set; }
        public bool IsLoading => ImageLoadingPool.Count != 0;
        public event Action<ImageItem, ImageSource> ImageItemDownloadButtonClicked;
        public event Action<ImageItem, string> ContextMenuTagButtonClicked;
        public ImageControl MouseOnImageControl { get; set; }
        public ObservableCollection<ImageControl> SelectedImageControls { get; set; } = new ObservableCollection<ImageControl>();

        public MoeExplorerControl()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            ImageItemsScrollViewer.MouseRightButtonUp += ImageItemsScrollViewerOnMouseRightButtonDown;
            PagingStackPanel.MouseWheel += PagingStackPanelOnMouseWheel;
            SelectedImageControls.CollectionChanged += SelectedImageControlsOnCollectionChanged;
            ContextSelectAllButton.Click += ContextSelectAllButtonOnClick;
            ContextSelectNoneButton.Click += ContextSelectNoneButtonOnClick;
            ContextSelectReverseButton.Click += ContextSelectReverseButtonOnClick;
            VisualStateManager.GoToState(this, nameof(NoNextPageState), true);
            VisualStateManager.GoToState(this, nameof(NoSelectedItemState), true);
        }

        private void ContextSelectReverseButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (ImageControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.ImageCheckBox.IsChecked = !ctrl.ImageCheckBox.IsChecked;
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void ContextSelectNoneButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (ImageControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.ImageCheckBox.IsChecked = false;
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void ContextSelectAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (ImageControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.ImageCheckBox.IsChecked = true;
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void SelectedImageControlsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            VisualStateManager.GoToState(this, SelectedImageControls.Count == 0 ? nameof(NoSelectedItemState) : nameof(HasSelectedItemState), true);
        }


        private void PagingStackPanelOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if(e.Delta>0) PagingScrollViewer.LineLeft();
            else if (e.Delta <0) PagingScrollViewer.LineRight();
        }

        private void ImageItemsScrollViewerOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MouseOnImageControl == null) return;
            ContextMenuPopup.IsOpen = true;
            ContextMenuPopupGrid.LargenShowSb().Begin();
            TagsWrapPanel.Children.Clear();
            foreach (var tag in MouseOnImageControl.ImageItem.Tags)
            {
                var texblock = new TextBlock
                {
                    FontSize = 9,
                    Margin = new Thickness(2),
                    Foreground = Brushes.White,
                    Text = tag
                };
                var button = new Button
                {
                    Template = (ControlTemplate) FindResource("MoeTagButtonControlTemplate"),
                    Content = texblock,
                    Margin = new Thickness(1)
                };
                button.Click += (o, args) => { ContextMenuTagButtonClicked?.Invoke(MouseOnImageControl.ImageItem, tag); };
                TagsWrapPanel.Children.Add(button);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                for (var i = 0; i < 60; i++)
                {
                   var button = new Button
                    {
                        Width = 32,
                        Height = 32,
                        Margin = new Thickness(3),
                        FontSize = 16,
                        Content = i + 1,
                        Template = (ControlTemplate)FindResource("MoeButtonControlTemplate"),
                    };
                    PagingStackPanel.Children.Add(button);
                }
            }
        }
        
        public void LoadImages(ImageItems items)
        {
            ResetVisual();
            foreach (var item in items)
            {
                var itemctrl = new ImageControl(Settings, item);
                
                itemctrl.DownloadButton.Click += (sender, args) 
                    => ImageItemDownloadButtonClicked?.Invoke(itemctrl.ImageItem, itemctrl.PreviewImage.Source);
                itemctrl.MouseEnter += (sender, args) => MouseOnImageControl = itemctrl;
                itemctrl.ImageCheckBox.Checked += (sender, args) => SelectedImageControls.Add(itemctrl);
                itemctrl.ImageCheckBox.Unchecked += (sender, args) => SelectedImageControls.Remove(itemctrl);
                ImageItemsWrapPanel.Children.Add(itemctrl);
                if (ImageLoadingPool.Count < Settings.MaxOnLoadingImageCount)
                {
                    ImageLoadingPool.Add(itemctrl);
                }
                else
                {
                    ImageWaitForLoadingPool.Add(itemctrl);
                }
            }
        }

        public void ResetVisual()
        {
            ImageItemsWrapPanel.Children.Clear();
            ImageLoadingPool.Clear();
            ImageWaitForLoadingPool.Clear();
            SelectedImageControls.Clear();
            ImageItemsScrollViewer.ScrollToTop();
        }

        public void SearchStartedVisual()
        {
            this.Sb("SearchStartSb").Begin();
            this.Sb("SearchingSb").Begin();
        }

        public void SearchStopedVisual()
        {
            var showsb = this.Sb("ShowSb");
            showsb.Completed += (sender, args) => this.Sb("SearchingSb").Stop();
            showsb.Begin();
        }

        public void RefreshPages(SearchSession session)
        {
            PagingStackPanel.Children.Clear();
            var lastpage = session.LoadedPages.Last();
            LoadImages(lastpage.ImageItems);
            StartDownloadShowImages();
            
            for (var index = 0; index < session.LoadedPages.Count; index++)
            {
                var page = session.LoadedPages[index];
                var hasnextpagestr = page.HasNextPage ? "有" : "无";
                var button = new Button
                {
                    Width = 32,
                    Height = 32,
                    Margin = new Thickness(3),
                    FontSize = 16,
                    Content = index +1,
                    Template = (ControlTemplate) FindResource("MoeButtonControlTemplate"),
                    ToolTip = $"最后真实页码：{page.LastRealPageIndex}\r\n" +
                              $"预加载下一页图片数量：{page.PreLoadNextPageItems.Count}\r\n" +
                              $"是否有下一页：{hasnextpagestr}"
                };
                button.Click += (sender, args) =>
                {
                    foreach (Button btn in PagingStackPanel.Children)
                    {
                        VisualStateManager.GoToState(btn, button.Equals(btn) ? "CurrentPage" : "NotCurrentPage", true);
                    }
                    PagingScrollViewer.ScrollToTop();
                    LoadImages(page.ImageItems);
                    StartDownloadShowImages();
                };
                if (index == session.LoadedPages.Count - 1)
                    button.Loaded += (s, a) => VisualStateManager.GoToState(button, "CurrentPage", true);
                PagingStackPanel.Children.Add(button);
            }

            PagingScrollViewer.ScrollToRightEnd();
            VisualStateManager.GoToState(this, session.LoadedPages.Last().HasNextPage ? nameof(HasNextPageState) : nameof(NoNextPageState), true);
            NewPageButtonNumTextBlock.Text = $"{session.LoadedPages.Count + 1}";
        }
        
        public void StartDownloadShowImages()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < ImageLoadingPool.Count; i++)
            {   
                var item = ImageLoadingPool[i];
                item.ImageLoaded += ItemOnImageLoaded;
                var unused = item.LoadImageAsync();
            }
        }

        private void ItemOnImageLoaded(ImageControl obj)
        {
            ImageLoadingPool.Remove(obj);
            AnyImageLoaded?.Invoke(this);
            if (ImageLoadingPool.Count == 0) { AllImagesLoaded?.Invoke(this);return; }

            if (ImageWaitForLoadingPool.Count > 0)
            {
                var item = ImageWaitForLoadingPool[0];
                ImageWaitForLoadingPool.Remove(item);
                ImageLoadingPool.Add(item);
                item.ImageLoaded += ItemOnImageLoaded;
                var unused = item.LoadImageAsync();
            }
            
        }
        
        public event Action<MoeExplorerControl> AnyImageLoaded; 
        public event Action<MoeExplorerControl> AllImagesLoaded;
    }
}
