using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
/// PagingButtonControl.xaml 的交互逻辑
/// </summary>
public partial class PagingButtonControl
{
    public PagingButtonControl()
    {
        InitializeComponent();
    }
        
    public SearchedVisualPage VisualPage { get; set; }

    public void Init(SearchedVisualPage page, double size, int? startPageNum = null)
    {
        VisualPage = page;
        VisualPage.LoadStartEvent += VisualPageOnLoadStartEvent;
        VisualPage.LoadEndEvent += VisualPageOnLoadEndEvent;
        VisualPage.GetEndEvent += VisualPageOnGetEndEvent;
        PageNumTextBlock.Text = startPageNum == null ? page.VisualIndex.ToString() : startPageNum.ToString();

        MultiPageNumTextGrid.Visibility = Visibility.Collapsed;
        NextIconTextBlock.Visibility = Visibility.Collapsed;
        Width = size;
        Height = size;
        VisualPage.PropertyChanged += VisualPageOnPropertyChanged;
        PageButton.Click += PageButtonOnClick;
    }

    private void PageButtonOnClick(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftAlt))
        {
            ShowOriginString();
        }
    }

    public void ShowOriginString()
    {
        MessageWindow.ShowDialog(VisualPage);
    }
    private void VisualPageOnGetEndEvent(SearchedVisualPage page)
    {
        if (page.RealPages.Count > 1)
        {
            PageNumTextBlock.Visibility = Visibility.Collapsed;
            MultiPageNumTextGrid.Visibility = Visibility.Visible;
            PageStartNumTextBlock.Text = (page.RealPages[0].CurrentPageNum?? page.RealPages[0].CurrentPageNumFromOne).ToString();
            PageEndNumTextBlock.Text = (page.RealPages[^1].CurrentPageNum?? page.RealPages[^1].CurrentPageNumFromOne).ToString();
        }
    }

    private void VisualPageOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualPage.IsCurrentPage))
        {
            if (VisualPage.IsCurrentPage)
            {
                PageNumTextBlock.Foreground = Brushes.DarkBlue;
                PageButton.GoState("CurrentPage");
            }
            else
            {
                PageNumTextBlock.Foreground = Brushes.Black;
                PageButton.GoState("NotCurrentPage");
            }
        }
    }

    public void SetNextPageButton()
    {
        PageNumTextBlock.Visibility = Visibility.Collapsed;
        MultiPageNumTextGrid.Visibility = Visibility.Collapsed;
    }

    private void VisualPageOnLoadEndEvent(SearchedVisualPage obj)
    {
            
        StopLoading();
    }

    private void VisualPageOnLoadStartEvent(SearchedVisualPage obj)
    {
            
        StartLoading();
    }

    public void StartLoading()
    {
        Ex.Log("StartLoading()");
        //IsEnabled = false;
        this.Sb("LoadingSpinSb").Begin();
        this.Sb("StartLoadingSb").Begin();

    }

    public void StopLoading()
    {
        Ex.Log("StopLoading()");
        this.Sb("LoadingSpinSb").Stop();
        this.Sb("StartLoadingSb").Stop();
        //IsEnabled = true;
    }
}