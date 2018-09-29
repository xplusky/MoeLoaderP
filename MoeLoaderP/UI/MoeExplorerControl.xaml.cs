using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    /// <summary>
    /// 图片列表浏览器
    /// </summary>
    public partial class MoeExplorerControl
    {
        public List<ImageControl> ImageLoadingPool { get; set; } = new List<ImageControl>();
        public List<ImageControl> ImageWaitForLoadingPool { get; set; } = new List<ImageControl>();
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
            ImageItemsWrapPanel.PreviewMouseLeftButtonDown += ImageItemsWrapPanelOnPreviewMouseLeftButtonDown;
            ImageItemsWrapPanel.PreviewMouseMove += ImageItemsWrapPanelOnPreviewMouseMove;
            ImageItemsWrapPanel.PreviewMouseLeftButtonUp += ImageItemsWrapPanelOnPreviewMouseLeftButtonUp;
            PagingStackPanel.MouseWheel += PagingStackPanelOnMouseWheel;
            SelectedImageControls.CollectionChanged += SelectedImageControlsOnCollectionChanged;
            ContextSelectAllButton.Click += ContextSelectAllButtonOnClick;
            ContextSelectNoneButton.Click += ContextSelectNoneButtonOnClick;
            ContextSelectReverseButton.Click += ContextSelectReverseButtonOnClick;

            VisualStateManager.GoToState(this, nameof(NoNextPageState), true);
            VisualStateManager.GoToState(this, nameof(NoSelectedItemState), true);

            _padTimer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(20)};
            _padTimer.Tick += PadTimerOnTick;
        }

        private void PadTimerOnTick(object sender, EventArgs e)
        {
            if (_lineupdown < 0)
            {
                ImageItemsScrollViewer.LineUp();
            }
            else if(_lineupdown > 0)
            {
                ImageItemsScrollViewer.LineDown();
            }
        }

        private void ImageItemsWrapPanelOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if(!_isChoosing)return;
            _isChoosing = false;
            CalculateChoosedItem();
            ChooseBox.Visibility = Visibility.Collapsed;
            _padTimer.Stop();
            _lineupdown = 0;
        }

        public void CalculateChoosedItem()
        {
            // ChooseBox 边界
            var xl = Canvas.GetLeft(ChooseBox);
            var xr = Canvas.GetLeft(ChooseBox) + ChooseBox.Width;
            var yt = Canvas.GetTop(ChooseBox);
            var yb = Canvas.GetTop(ChooseBox) + ChooseBox.Height;
            var ctrlDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt);
            foreach (ImageControl child in ImageItemsWrapPanel.Children)
            {
                var isin = false;
                var pointLeftTop = child.TranslatePoint(new Point(), ImageItemsWrapPanel);
                var pointRightTop = child.TranslatePoint(new Point(child.Width,0), ImageItemsWrapPanel);
                var pointRightBottom = child.TranslatePoint(new Point(child.Width, child.Height), ImageItemsWrapPanel);
                var pointLeftBtttom = child.TranslatePoint(new Point(0, child.Height), ImageItemsWrapPanel);
                var pointCenter = child.TranslatePoint(new Point(child.Width/2, child.Height/2), ImageItemsWrapPanel);
                var plist = new List<Point>{ pointLeftTop , pointRightTop , pointRightBottom , pointLeftBtttom , pointCenter };
                
                foreach (var point in plist)
                {
                    if (point.X > xl && point.X < xr && point.Y > yt && point.Y < yb)
                    {
                        isin = true;
                        break;
                    }
                }
                if (isin) child.ImageCheckBox.IsChecked = !ctrlDown;
            }
        }

        private void ImageItemsWrapPanelOnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (_isChoosing) ImageItemsWrapPanelOnPreviewMouseLeftButtonUp(sender, null);
                return;
            }
            if (!_isChoosing) return;
            if(ChooseBox.Visibility == Visibility.Collapsed) ChooseBox.Visibility = Visibility.Visible;
            var movep = e.GetPosition(ChooseCanvasRoot);
            var vx = movep.X - _chooseStartPoint.X;
            if (vx < 0)
            {
                Canvas.SetLeft(ChooseBox, movep.X);
                ChooseBox.Width = -vx;
            }
            else
            {
                Canvas.SetLeft(ChooseBox, _chooseStartPoint.X);
                ChooseBox.Width = vx;
            }

            var vy = movep.Y - _chooseStartPoint.Y;
            if (vy < 0)
            {
                Canvas.SetTop(ChooseBox, movep.Y);
                ChooseBox.Height = -vy;
            }
            else
            {
                Canvas.SetTop(ChooseBox, _chooseStartPoint.Y);
                ChooseBox.Height = vy;
            }
            _isChoosing = true;

            if(!_padTimer.IsEnabled) _padTimer.Start();
            var pointToViewer = e.GetPosition(ImageItemsScrollViewer);
            if (pointToViewer.Y < 0) _lineupdown = -1;
            else if(pointToViewer.Y > ImageItemsScrollViewer.ActualHeight) _lineupdown = 1;
            else _lineupdown = 0;
        }

        private readonly DispatcherTimer _padTimer;
        private Point _chooseStartPoint;
        private bool _isChoosing;
        private int _lineupdown;

        private void ImageItemsWrapPanelOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isChoosing = true;
            _chooseStartPoint = e.GetPosition(ChooseCanvasRoot);
            ChooseBox.Width = 0;
            ChooseBox.Height = 0;
            Canvas.SetLeft(ChooseBox, _chooseStartPoint.X);
            Canvas.SetTop(ChooseBox, _chooseStartPoint.Y);
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

        public Button GetTagButton(string text)
        {
            var texblock = new TextBlock
            {
                FontSize = 9,
                Margin = new Thickness(2),
                Foreground = Brushes.White,
                Text = text
            };
            var button = new Button
            {
                Template = (ControlTemplate)FindResource("MoeTagButtonControlTemplate"),
                Content = texblock,
                Margin = new Thickness(1),
            };
            return button;
        }

        public TextBlock GetTitieTextBlock(string text)
        {
            var textblock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(2),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            };
            return textblock;
        }

        private void ImageItemsScrollViewerOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MouseOnImageControl == null) return;
            ContextMenuPopup.IsOpen = true;
            ContextMenuPopupGrid.LargenShowSb().Begin();

            // load tag info
            var tags = MouseOnImageControl.ImageItem.Tags;
            if (tags.Count > 0)
            {
                TagsWrapPanel.Visibility = Visibility.Visible;
                TagsWrapPanel.Children.Clear();
                
                TagsWrapPanel.Children.Add(GetTitieTextBlock("Tags:"));
                foreach (var tag in MouseOnImageControl.ImageItem.Tags)
                {
                    var button = GetTagButton(tag);
                    button.Click += (o, args) => { ContextMenuTagButtonClicked?.Invoke(MouseOnImageControl.ImageItem, tag); };
                    TagsWrapPanel.Children.Add(button);
                }
            }
            else
            {
                TagsWrapPanel.Visibility = Visibility.Collapsed;
            }

            // load title info
            var title = MouseOnImageControl.ImageItem.Title;
            if (!string.IsNullOrWhiteSpace(title))
            {
                TitleWrapPanel.Visibility = Visibility.Visible;
                TitleWrapPanel.Children.Clear(); 
                TitleWrapPanel.Children.Add(GetTitieTextBlock("Title:"));
                var btn = GetTagButton(title);
                btn.Click += (o, args) => Clipboard.SetText(title);
                TitleWrapPanel.Children.Add(btn);
            }
            else
            {
                TitleWrapPanel.Visibility = Visibility.Collapsed;
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

        public void RefreshPaging(SearchSession session)
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
                item.ImageLoadEnd += ItemOnImageLoaded;
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
                item.ImageLoadEnd += ItemOnImageLoaded;
                var unused = item.LoadImageAsync();
            }
            
        }
        
        public event Action<MoeExplorerControl> AnyImageLoaded; 
        public event Action<MoeExplorerControl> AllImagesLoaded;
    }
}
