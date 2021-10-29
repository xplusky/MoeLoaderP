using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using MoeLoaderP.Core;

namespace MoeLoaderP.Wpf.ControlParts
{
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

        public void Init(SearchedVisualPage page, double size)
        {
            VisualPage = page;
            VisualPage.LoadStartEvent += VisualPageOnLoadStartEvent;
            VisualPage.LoadEndEvent += VisualPageOnLoadEndEvent;
            PageNumTextBlock.Text = page.PageDisplayIndex.ToString();
            NextIconTextBlock.Visibility = Visibility.Collapsed;
            Width = size;
            Height = size;
            VisualPage.PropertyChanged += VisualPageOnPropertyChanged;
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
}
