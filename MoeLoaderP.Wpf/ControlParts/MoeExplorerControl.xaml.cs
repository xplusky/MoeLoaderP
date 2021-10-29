using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
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
        public List<MoeItemControl> ImageLoadingPool { get; set; } = new();
        public List<MoeItemControl> ImageWaitForLoadingPool { get; set; } = new();

        public PreviewWindow PreviewWindowInstance { get; set; }
        public Settings Settings { get; set; }

        public event Action<MoeItem, ImageSource> ImageItemDownloadButtonClicked;
        public event Action<MoeItem, ImageSource> MoeItemPreviewButtonClicked; 

        public MoeItemControl MouseOnImageControl { get; set; }

        public ObservableCollection<MoeItemControl> SelectedImageControls { get; set; } = new();
        
        public MoeExplorerControl()
        {
            InitializeComponent();
            
        }

        public void Init(Settings settings)
        {
            Settings = settings;
            KeyDown += OnKeyDown;
            ImageItemsScrollViewer.MouseRightButtonUp += ImageItemsScrollViewerOnMouseRightButtonUp;
            ImageItemsWrapPanel.PreviewMouseLeftButtonDown += ImageItemsWrapPanelOnPreviewMouseLeftButtonDown;
            ImageItemsWrapPanel.PreviewMouseMove += ImageItemsWrapPanelOnPreviewMouseMove;
            ImageItemsWrapPanel.PreviewMouseLeftButtonUp += ImageItemsWrapPanelOnPreviewMouseLeftButtonUp;
            SelectedImageControls.CollectionChanged += SelectedImageControlsOnCollectionChanged;
            ContextSelectAllButton.Click += ContextSelectAllButtonOnClick;
            ContextSelectNoneButton.Click += ContextSelectNoneButtonOnClick;
            ContextSelectReverseButton.Click += ContextSelectReverseButtonOnClick;
            NextButtonControl.PageButton.Click += NextPageButtonOnClick;
            MoeItemPreviewButtonClicked += OnMoeItemPreviewButtonClicked;
            DownloadSelectedImagesButton.Click += DownloadSelectedImagesButtonOnClick;
            ImageItemDownloadButtonClicked += OnImageItemDownloadButtonClicked;
            OutputSelectedImagesUrlsButton.Click += OutputSelectedImagesUrlsButtonOnClick;
            SearchByAuthorIdAction += SearchByAuthorId;

            this.GoState(nameof(NoNextPageState), nameof(NoSelectedItemState), nameof(HideSearchingMessageState));

            _padTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
            _padTimer.Tick += PadTimerOnTick;

            Ex.ShowMessageAction += ShowMessageAction;

            NextButtonControl.SetNextPageButton();
            NextButtonControl.Visibility = Visibility.Collapsed;

            Settings.PropertyChanged += SettingsOnPropertyChanged;

        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.CurrentSession))
            {
                InitSession(Settings.CurrentSession);
                ResetVisualPageDisplay();
            }
        }

        public void InitSession(SearchSession session)
        {
            PagingStackPanel.Children.Clear();
            Settings.CurrentSession = session;
            session.VisualPages.AddEvent += VisualPagesOnAddEvent;
        }

        private void VisualPagesOnAddEvent(SearchedVisualPage page)
        {
            var button = new PagingButtonControl();
            button.Init(page, 48);
            button.PageButton.Click += (sender, args) => PageButtonOnClick(sender, args, button);
            PagingStackPanel.Children.Add(button);
        }

        public int CurrentDisplayIndex { get; set; }

        private void PageButtonOnClick(object sender, RoutedEventArgs e, PagingButtonControl ctrl)
        {
            foreach (PagingButtonControl c in PagingStackPanel.Children)
            {
                c.VisualPage.IsCurrentPage = false;
            }
            if(ctrl.VisualPage.PageDisplayIndex == CurrentDisplayIndex)return;
            if (PagingStackPanel.Children.IndexOf(ctrl) == PagingStackPanel.Children.Count - 1)
            {
                if (ctrl.VisualPage.IsSearchComplete == false && ctrl.VisualPage.Count < Settings.CurrentSession.FirstSearchPara.CountLimit)
                {
                    NextPageButtonOnClick(null,null);
                    return;
                }
            }
            _ = ShowVisualPage(ctrl.VisualPage);
        }


        #region 页码相关


        #endregion

        private void OutputSelectedImagesUrlsButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is not MainWindow mw) return;
            foreach (var ctrl in SelectedImageControls)
            {
                var url = ctrl.MoeItem.DownloadUrlInfo;
                if (ctrl.MoeItem.ChildrenItems.Count > 0)
                {
                    foreach (var item in ctrl.MoeItem.ChildrenItems)
                    {
                        mw.CollectTextBox.Text += item.DownloadUrlInfo?.Url + Environment.NewLine;
                    }
                }
                else mw.CollectTextBox.Text += url?.Url + Environment.NewLine;
            }

            foreach (MoeItemControl ct in ImageItemsWrapPanel.Children)
            {
                ct.ImageCheckBox.IsChecked = false;
            }

            Ex.ShowMessage("已添加至收集箱");
        }

        private void OnImageItemDownloadButtonClicked(MoeItem item, ImageSource imgSource)
        {
            if(Application.Current.MainWindow is not MainWindow mw) return;
            mw.MoeDownloaderControl.Downloader.AddDownload(item, imgSource);
            var lb = mw.MoeDownloaderControl.DownloadItemsListBox;
            var ctrl = lb.Items[^1];
            lb.ScrollIntoView(ctrl);
            if (mw.DownloaderMenuCheckBox.IsChecked != false) return;
            mw.DownloaderMenuCheckBox.IsChecked = true;
        }

        private void SearchByAuthorId(MoeSite arg1, string arg2)
        {
            if (Application.Current.MainWindow is not MainWindow mw ) return;
            
            mw.SearchControl.CurrentSelectedSite = arg1;
            mw.SearchControl.MoeSitesLv2ComboBox.SelectedIndex = 1;
            mw.SearchControl.KeywordTextBox.Text = arg2;
            mw.SearchControl.SearchButtonOnClick(null, null);
        }

        private void DownloadSelectedImagesButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is not MainWindow mw) return;
            var count = 0;
            foreach (var ctrl in SelectedImageControls)
            {
                var img = ctrl.MoeItem;
                if (img.DownloadUrlInfo?.Url == null) continue;
                mw.MoeDownloaderControl.Downloader.AddDownload(img, ctrl.PreviewImage.Source);
                count++;
            }
            if (mw.DownloaderMenuCheckBox.IsChecked == false && count > 0) mw.DownloaderMenuCheckBox.IsChecked = true;
            
            foreach (MoeItemControl ct in ImageItemsWrapPanel.Children)
            {
                ct.ImageCheckBox.IsChecked = false;
            }

            var lb = mw.MoeDownloaderControl.DownloadItemsListBox;
            if (lb.Items.Count != 0)
            {
                lb.ScrollIntoView(lb.Items[^1]);
            }
        }

        private void OnMoeItemPreviewButtonClicked(MoeItem item, ImageSource imgSource)
        {
            PreviewWindow.Show(PreviewWindowInstance, Application.Current.MainWindow, item, imgSource);
        }

        private async void NextPageButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (PagingButtonControl control in PagingStackPanel.Children)
            {
                control.VisualPage.IsCurrentPage = false;
            }
            var vp = await Settings.CurrentSession.SearchNextVisualPage();
            _ = ShowVisualPage(vp);
        }

        

        private void ImageItemsScrollViewerOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            SpPanel.Children.Clear();
            ContextMenuImageInfoStackPanel.Children.Clear();
            if (ImageItemsWrapPanel.Children.Count != 0) ContextMenuPopup.IsOpen = true;
        }
        
        private void ShowMessageAction(string arg1, string arg2, Ex.MessagePos arg3, bool highlight)
        {
            switch (arg3)
            {
                case Ex.MessagePos.Searching:
                    SearchingMessageTextBlock.Text = arg1;
                    break;
                case Ex.MessagePos.Page:
                    PageMessageTextBlock.Text = arg1;
                    break;
            }
        }


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

        
        private void ItemCtrlOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenuPopup.IsOpen = true;
            //ContextMenuPopupGrid.EnlargeShowSb().Begin();
            if (sender is MoeItemControl obj)
            {
                LoadExtFunc(obj.MoeItem);
                LoadImgInfo(obj.MoeItem);
                e.Handled = true;
            }
        }


        public void ResetVisualPageDisplay()
        {
            foreach (MoeItemControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.Dispose();
            }
            ImageItemsWrapPanel.Children.Clear();
            ImageLoadingPool.Clear();
            ImageWaitForLoadingPool.Clear();
            SelectedImageControls.Clear();
            ImageItemsScrollViewer.ScrollToTop();
            GC.Collect();
        }



        public void SearchStartedVisual()
        {
            this.Sb("SearchStartSb").Begin();
            this.Sb("SearchingSb").Begin();

            this.GoState(nameof(ShowSearchingMessageState));
        }

        public async void SearchStopVisual()
        {
           
            this.Sb("ShowSb").Begin();
            await Task.Delay(TimeSpan.FromSeconds(2));
            this.Sb("SearchingSb").Stop();
            this.GoState(nameof(HideSearchingMessageState));
        }
        
        

        public async Task ShowVisualPage(SearchedVisualPage page)
        {
            NextButtonControl.Visibility = Visibility.Visible;
            CurrentDisplayIndex = page.PageDisplayIndex;
            if (PagingStackPanel.Children[page.PageDisplayIndex - 1] is PagingButtonControl button)
            {
                button.VisualPage.IsCurrentPage = true;
            }
            page.LoadStart();
            ResetVisualPageDisplay();
            var startLoad = false;
            foreach (var img in page)
            {
                await ImageItemsWrapPanel.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    var ctrl = new MoeItemControl(Settings, img);
                    ctrl.DownloadButton.Click += delegate { ImageItemDownloadButtonClicked?.Invoke(ctrl.MoeItem, ctrl.PreviewImage.Source); };
                    ctrl.PreviewButton.Click += delegate { MoeItemPreviewButtonClicked?.Invoke(ctrl.MoeItem, ctrl.PreviewImage.Source); };
                    ctrl.MouseEnter += delegate { MouseOnImageControl = ctrl; };
                    ctrl.ImageCheckBox.Checked += delegate { SelectedImageControls.Add(ctrl); };
                    ctrl.ImageCheckBox.Unchecked += delegate { SelectedImageControls.Remove(ctrl); };
                    ctrl.MouseRightButtonUp += ItemCtrlOnMouseRightButtonUp;
                    ImageItemsWrapPanel.Children.Add(ctrl);
                    if (ImageLoadingPool.Count < Settings.MaxOnLoadingImageCount) ImageLoadingPool.Add(ctrl);
                    else ImageWaitForLoadingPool.Add(ctrl);
                    if (startLoad == false && ImageWaitForLoadingPool.Count > 0)
                    {
                        StartDownloadShowImages();
                        startLoad = true;
                    }

                }));
            }
            
            page.LoadEnd();
        }
        
        public void StartDownloadShowImages()
        {
            for (var i = 0; i < ImageLoadingPool.Count; i++)
            {
                var item = ImageLoadingPool[i];
                item.ImageLoadEnd += ItemOnImageLoaded;
                _ = item.TryLoad();
            }
        }

        private void ItemOnImageLoaded(MoeItemControl obj)
        {
            ImageLoadingPool.Remove(obj);
            if (ImageWaitForLoadingPool.Any())
            {
                var item = ImageWaitForLoadingPool[0];
                ImageWaitForLoadingPool.Remove(item);
                ImageLoadingPool.Add(item);
                item.ImageLoadEnd += ItemOnImageLoaded;
                _ = item.TryLoad();
            }
        }


        #region 框选功能相关代码

        private DispatcherTimer _padTimer;
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
            foreach (MoeItemControl child in ImageItemsWrapPanel.Children)
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
            foreach (MoeItemControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.ImageCheckBox.IsChecked = !ctrl.ImageCheckBox.IsChecked;
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void ContextSelectNoneButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (MoeItemControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.ImageCheckBox.IsChecked = false;
            }
            ContextMenuPopup.IsOpen = false;
        }

        private void ContextSelectAllButtonOnClick(object sender, RoutedEventArgs e)
        {
            foreach (MoeItemControl ctrl in ImageItemsWrapPanel.Children)
            {
                ctrl.ImageCheckBox.IsChecked = true;
            }
            ContextMenuPopup.IsOpen = false;
        }

        /// <summary>
        /// 生成右键菜单中的小标题TextBlock
        /// </summary>
        public static TextBlock GetTitleTextBlock(string text)
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

            var items = SelectedImageControls.Where(ctrl => ctrl.RefreshButton.Visibility == Visibility.Visible);
            if (items?.Count() > 0)
            {
                var b = GetSpButton("刷新未加载的缩略图");
                b.Click += (sender, args) =>
                {
                    ContextMenuPopup.IsOpen = false;
                    foreach (var item in items)
                    {
                        _ = item.TryLoad();
                    }
                };
                SpPanel.Children.Add(b);
            }

            // pixiv load choose 首次登场图片
            if (site.ShortName == "pixiv" && para.Lv2MenuIndex == 2)
            {

                var b = GetSpButton("全选首次登场图片");

                b.Click += delegate
                {
                    ContextMenuPopup.IsOpen = false;
                    foreach (MoeItemControl img in ImageItemsWrapPanel.Children)
                    {
                        img.ImageCheckBox.IsChecked = img.MoeItem.Tip == "首次登场";
                    }
                };
                SpPanel.Children.Add(b);
            }

            // load search by author id
            if (site.ShortName == "pixiv")
            {
                var b = GetSpButton($"搜索该作者{moeItem.Uploader}的所有作品");
                b.Click += delegate
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
            if (MouseOnImageControl.MoeItem.Tags.Count > 0)
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
            button.Click += delegate { text.CopyToClipboard(); };
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

    }
    
}
