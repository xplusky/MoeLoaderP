using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
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

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    /// 图片列表浏览器
    /// </summary>
    public partial class MoeExplorerControl
    {
        public List<ImageControl> ImageLoadingPool { get; set; } = new List<ImageControl>();
        public List<ImageControl> ImageWaitForLoadingPool { get; set; } = new List<ImageControl>();

        public Settings Settings { get; set; }
        public event Action<MoeItem, ImageSource> ImageItemDownloadButtonClicked;
        public event Action<MoeItem, ImageSource> MoeItemPreviewButtonClicked; 
        public ImageControl MouseOnImageControl { get; set; }
        public ObservableCollection<ImageControl> SelectedImageControls { get; set; } = new ObservableCollection<ImageControl>();

        public MoeExplorerControl()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
            ImageItemsScrollViewer.MouseRightButtonUp += ImageItemsScrollViewerOnMouseRightButtonUp;
            ImageItemsWrapPanel.PreviewMouseLeftButtonDown += ImageItemsWrapPanelOnPreviewMouseLeftButtonDown;
            ImageItemsWrapPanel.PreviewMouseMove += ImageItemsWrapPanelOnPreviewMouseMove;
            ImageItemsWrapPanel.PreviewMouseLeftButtonUp += ImageItemsWrapPanelOnPreviewMouseLeftButtonUp;
            SelectedImageControls.CollectionChanged += SelectedImageControlsOnCollectionChanged;
            ContextSelectAllButton.Click += ContextSelectAllButtonOnClick;
            ContextSelectNoneButton.Click += ContextSelectNoneButtonOnClick;
            ContextSelectReverseButton.Click += ContextSelectReverseButtonOnClick;

            this.GoState(nameof(NoNextPageState), nameof(NoSelectedItemState), nameof(HideSearchingMessageState));

            _padTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            _padTimer.Tick += PadTimerOnTick;

            Extend.ShowMessageAction += ShowMessageAction;
        }

        private void ImageItemsScrollViewerOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            SpPanel.Children.Clear();
            ContextMenuImageInfoStackPanel.Children.Clear();
            ContextMenuPopup.IsOpen = true;
        }
        
        private void ShowMessageAction(string arg1, string arg2, Extend.MessagePos arg3)
        {
            switch (arg3)
            {
                case Extend.MessagePos.Searching:
                    SearchingMessageTextBlock.Text = arg1;
                    break;
                case Extend.MessagePos.Page:
                    PageMessageTextBlock.Text = arg1;
                    break;
            }
        }

        #region 框选功能相关代码

        private readonly DispatcherTimer _padTimer;
        private Point _chooseStartPoint;
        private bool _isChoosing;
        private int _lineUpDown;
        private void PadTimerOnTick(object sender, EventArgs e)
        {
            if (_lineUpDown < 0)
            {
                ImageItemsScrollViewer.LineUp();
            }
            else if (_lineUpDown > 0)
            {
                ImageItemsScrollViewer.LineDown();
            }
        }

        private void ImageItemsWrapPanelOnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isChoosing) return;
            _isChoosing = false;
            CalculateChosenItem();
            ChooseBox.Visibility = Visibility.Collapsed;
            _padTimer.Stop();
            _lineUpDown = 0;
        }

        public void CalculateChosenItem()
        {
            // ChooseBox 边界
            var xl = Canvas.GetLeft(ChooseBox);
            var xr = Canvas.GetLeft(ChooseBox) + ChooseBox.Width;
            var yt = Canvas.GetTop(ChooseBox);
            var yb = Canvas.GetTop(ChooseBox) + ChooseBox.Height;
            var ctrlDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftAlt);
            foreach (ImageControl child in ImageItemsWrapPanel.Children)
            {
                var isIn = false;
                var pointLeftTop = child.TranslatePoint(new Point(), ImageItemsWrapPanel);
                var pointRightTop = child.TranslatePoint(new Point(child.Width, 0), ImageItemsWrapPanel);
                var pointRightBottom = child.TranslatePoint(new Point(child.Width, child.Height), ImageItemsWrapPanel);
                var pointLeftBottom = child.TranslatePoint(new Point(0, child.Height), ImageItemsWrapPanel);
                var pointCenter = child.TranslatePoint(new Point(child.Width / 2, child.Height / 2), ImageItemsWrapPanel);
                var plist = new List<Point> { pointLeftTop, pointRightTop, pointRightBottom, pointLeftBottom, pointCenter };

                foreach (var point in plist)
                {
                    if (point.X > xl && point.X < xr && point.Y > yt && point.Y < yb)
                    {
                        isIn = true;
                        break;
                    }
                }
                if (isIn) child.ImageCheckBox.IsChecked = !ctrlDown;
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
            if (ChooseBox.Visibility == Visibility.Collapsed) ChooseBox.Visibility = Visibility.Visible;
            var moveP = e.GetPosition(ChooseCanvasRoot);
            var vx = moveP.X - _chooseStartPoint.X;
            if (vx < 0)
            {
                Canvas.SetLeft(ChooseBox, moveP.X);
                ChooseBox.Width = -vx;
            }
            else
            {
                Canvas.SetLeft(ChooseBox, _chooseStartPoint.X);
                ChooseBox.Width = vx;
            }

            var vy = moveP.Y - _chooseStartPoint.Y;
            if (vy < 0)
            {
                Canvas.SetTop(ChooseBox, moveP.Y);
                ChooseBox.Height = -vy;
            }
            else
            {
                Canvas.SetTop(ChooseBox, _chooseStartPoint.Y);
                ChooseBox.Height = vy;
            }
            _isChoosing = true;

            if (!_padTimer.IsEnabled) _padTimer.Start();
            var pointToViewer = e.GetPosition(ImageItemsScrollViewer);
            if (pointToViewer.Y < 0) _lineUpDown = -1;
            else if (pointToViewer.Y > ImageItemsScrollViewer.ActualHeight) _lineUpDown = 1;
            else _lineUpDown = 0;
        }

        private void ImageItemsWrapPanelOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isChoosing = true;
            _chooseStartPoint = e.GetPosition(ChooseCanvasRoot);
            ChooseBox.Width = 0;
            ChooseBox.Height = 0;
            Canvas.SetLeft(ChooseBox, _chooseStartPoint.X);
            Canvas.SetTop(ChooseBox, _chooseStartPoint.Y);
        }

        #endregion

        #region 右键菜单相关代码

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

        /// <summary>
        /// 生成右键菜单中的小标题TextBlock
        /// </summary>
        public TextBlock GetTitleTextBlock(string text)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(2),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.Black
            };
            return textBlock;
        }

        /// <summary>
        /// 显示右键菜单并生成信息
        /// </summary>
        public void LoadExtFunc(MoeItem moeItem)
        {
            var para = moeItem.Para;
            var site = para.Site;
            SpPanel.Children.Clear();

            // load choose 首次登场图片
            if (site.SupportState.IsSupportSelectPixivRankNew && para.SubMenuIndex == 2)
            {

                var b = GetSpButton("全选首次登场图片");
                b.Click += (o, args) =>
                {
                    foreach (ImageControl img in ImageItemsWrapPanel.Children)
                    {
                        img.ImageCheckBox.IsChecked = img.ImageItem.Tip == "首次登场";
                    }

                    ContextMenuPopup.IsOpen = false;
                };
                SpPanel.Children.Add(b);
            }

            // load search by author id
            if (site.SupportState.IsSupportSearchByAuthorId)
            {
                var b = GetSpButton($"搜索该作者{moeItem.Uploader}的所有作品");
                b.Click += (sender, args) =>
                {
                    SearchByAuthorIdAction?.Invoke(site, moeItem.UploaderId);
                    ContextMenuPopup.IsOpen = false;
                };
                SpPanel.Children.Add(b);
            }
        }

        public Action<MoeSite, string> SearchByAuthorIdAction;

        public void LoadImgInfo(MoeItem item)
        {
            ContextMenuImageInfoStackPanel.Children.Clear();
            if (item.Id > 0) GenImageInfoVisual("ID:", $"{item.Id}");
            if (!item.Uploader.IsEmpty())
            {
                GenImageInfoVisual("Uploader:", item.Uploader);
                if (!item.UploaderId.IsEmpty()) GenImageInfoVisual("UpID:", item.UploaderId);
            }
            if (!item.Title.IsEmpty()) GenImageInfoVisual("Title:", item.Title);
            if (!item.DateString.IsEmpty()) GenImageInfoVisual("Date:", item.DateString);
            if (MouseOnImageControl.ImageItem.Tags.Count > 0)
            {
                GenImageInfoVisual("Tags:", item.Tags.ToArray());
            }
            if(!item.Artist.IsEmpty()) GenImageInfoVisual("Artist:",item.Artist);
            if(!item.Character.IsEmpty()) GenImageInfoVisual("Character:", item.Character);
            if(!item.Copyright.IsEmpty()) GenImageInfoVisual("Copyright:", item.Copyright);
            if(!item.Source.IsEmpty()) GenImageInfoVisual("Source:",item.Source);
        }

        public void GenImageInfoVisual(string title, params string[] buttons)
        {
            var p = new WrapPanel { Margin = new Thickness(2, 4, 2, 2) };
            p.Children.Add(GetTitleTextBlock(title));
            foreach (var button in buttons)
            {
                p.Children.Add(GetTagButton(button));
            }
            ContextMenuImageInfoStackPanel.Children.Add(p);
        }
        
        private Button GetTagButton(string text)
        {
            var textBlock = new TextBlock
            {
                FontSize = 9,
                Margin = new Thickness(2),
                Foreground = Brushes.White,
                Text = text
            };
            var button = new Button
            {
                Template = (ControlTemplate)FindResource("MoeTagButtonControlTemplate"),
                Content = textBlock,
                Margin = new Thickness(1),
                ToolTip = text
            };
            button.Click += (o, args) => text.CopyToClipboard();
            return button;
        }

        private Button GetSpButton(string text)
        {
            var textBlock = new TextBlock
            {
                FontSize = 14,
                Margin = new Thickness(2),
                Foreground = Brushes.Black,
                Text = text
            };

            var grid = new Grid { VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(textBlock);
            var button = new Button
            {
                Template = (ControlTemplate)FindResource("MoeContextMenuButtonControlTemplate"),
                Content = grid,
                Margin = new Thickness(1),
                Height = 32,
            };
            return button;
        }

        #endregion

        private void SelectedImageControlsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.GoState(SelectedImageControls.Count == 0 ? nameof(NoSelectedItemState) : nameof(HasSelectedItemState));
            ImageCountTextBlock.Text = $"已选择{SelectedImageControls.Count}张（组）图片";
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                ContextSelectAllButtonOnClick(null, null);
            }
        }

        public void AddImages(MoeItems imgs)
        {
            foreach (var img in imgs)
            {
                var itemCtrl = new ImageControl(Settings, img);
                itemCtrl.DownloadButton.Click += (sender, args) => { ImageItemDownloadButtonClicked?.Invoke(itemCtrl.ImageItem, itemCtrl.PreviewImage.Source); };
                itemCtrl.PreviewButton.Click += (sender, args) => { MoeItemPreviewButtonClicked?.Invoke(itemCtrl.ImageItem, itemCtrl.PreviewImage.Source); };
                itemCtrl.MouseEnter += (sender, args) => MouseOnImageControl = itemCtrl;
                itemCtrl.ImageCheckBox.Checked += (sender, args) => SelectedImageControls.Add(itemCtrl);
                itemCtrl.ImageCheckBox.Unchecked += (sender, args) => SelectedImageControls.Remove(itemCtrl);
                ImageItemsWrapPanel.Children.Add(itemCtrl);
                itemCtrl.Sb("ShowSb").Begin();
                if (ImageLoadingPool.Count < Settings.MaxOnLoadingImageCount) ImageLoadingPool.Add(itemCtrl);
                else ImageWaitForLoadingPool.Add(itemCtrl);
                itemCtrl.MouseRightButtonUp += ItemCtrlOnMouseRightButtonUp;
            }
        }

        private void ItemCtrlOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenuPopup.IsOpen = true;
            //ContextMenuPopupGrid.EnlargeShowSb().Begin();
            if (sender is ImageControl obj)
            {
                LoadExtFunc(obj.ImageItem);
                LoadImgInfo(obj.ImageItem);
                e.Handled = true;
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

            this.GoState(nameof(ShowSearchingMessageState));
        }

        public void SearchStopVisual()
        {
            var showSb = this.Sb("ShowSb");
            showSb.Completed += (sender, args) => this.Sb("SearchingSb").Stop();
            showSb.Begin();
            this.GoState(nameof(HideSearchingMessageState));
        }

        public void AddPage(SearchSession session)
        {
            var lastPage = session.LoadedVisualPages?.LastOrDefault();
            if (lastPage == null) return;
            AddImages(lastPage.ImageItems);
            StartDownloadShowImages();

            this.GoState(session.LoadedVisualPages.Last().HasNextVisualPage ? nameof(HasNextPageState) : nameof(NoNextPageState));
            NewPageButtonNumTextBlock.Text = $"{session.LoadedVisualPages.Count + 1}";
        }

        public void StartDownloadShowImages()
        {
            for (var i = 0; i < ImageLoadingPool.Count; i++)
            {
                var item = ImageLoadingPool[i];
                item.ImageLoadEnd += ItemOnImageLoaded;
                var unused = item.LoadImageAndDetailTask();
            }
        }

        private void ItemOnImageLoaded(ImageControl obj)
        {
            ImageLoadingPool.Remove(obj);
            if (ImageWaitForLoadingPool.Any())
            {
                var item = ImageWaitForLoadingPool[0];
                ImageWaitForLoadingPool.Remove(item);
                ImageLoadingPool.Add(item);
                item.ImageLoadEnd += ItemOnImageLoaded;
                var unused = item.LoadImageAndDetailTask();
            }
        }

    }
}
