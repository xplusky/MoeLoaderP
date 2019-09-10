using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MoeLoader.Core;
using MoeLoader.Core.Sites;

namespace MoeLoader.UI
{
    /// <summary>
    /// 搜索控件
    /// </summary>
    public partial class SearchControl
    {
        public SiteManager SiteManager { get; set; }
        public MoeSite CurrentSelectedSite { get; set; }
        public Settings Settings { get; set; }
        public AutoHintItems HintItems { get; set; } = new AutoHintItems();

        public TextBox KeywordTextBox => (TextBox) KeywordComboBox?.Template.FindName(nameof(KeywordTextBox), KeywordComboBox);
        
        public SearchControl()
        {
            InitializeComponent();
            MoeSitesComboBox.SelectionChanged += MoeSitesComboBoxOnSelectionChanged;
            KeywordComboBox.ItemsSource = HintItems;
            SearchParaCheckBox.Checked += SearchParaCheckBoxOnChecked;
            ShowExlicitOnlyCheckBox.Checked += (sender, args) => FilterExlicitCheckBox.IsChecked = true;
            FilterExlicitCheckBox.Unchecked += (sender, args) => ShowExlicitOnlyCheckBox.IsChecked = false;
            KeywordComboBox.SelectionChanged += KeywordComboBoxOnSelectionChanged;
            MoeSitesSubComboBox.SelectionChanged += MoeSitesSubComboBoxOnSelectionChanged;
            MoeSitesLv3ComboBox.SelectionChanged += MoeSitesLv3ComboBoxOnSelectionChanged;
            Loaded += (sender, args) =>
            {
                HintItems.Clear();
                AddHistoryItems();
                KeywordTextBox.TextChanged += KeywordTextBoxOnTextChanged;
                KeywordTextBox.GotFocus += KeywordTextBoxOnGotFocus;
            };
        }

        private async void KeywordTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (CurrentTaskCts == null) this.Sb("SearchingSpinSb").Begin();
            CurrentTaskCts?.Cancel();
            CurrentTaskCts = new CancellationTokenSource();

            await ShowKeywordComboBoxItemsAsync(KeywordTextBox.Text, CurrentTaskCts.Token);
        }

        private void KeywordTextBoxOnGotFocus(object sender, RoutedEventArgs e)
        {
            
            KeywordComboBox.IsDropDownOpen = KeywordTextBox.IsFocused;
        }

        private void MoeSitesLv3ComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentSelectedSite.Lv3ListIndex = MoeSitesLv3ComboBox.SelectedIndex;
        }
        
        private void MoeSitesSubComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentSelectedSite.SubListIndex = MoeSitesSubComboBox.SelectedIndex;
            if (MoeSitesSubComboBox.SelectedIndex < 0)
            {
                MoeSitesLv3ComboBox.Visibility = Visibility.Collapsed;
                CurrentSelectedSite.Lv3ListIndex = -1;
                return;
            }
            var menu = CurrentSelectedSite.SubMenu[MoeSitesSubComboBox.SelectedIndex];
            if (menu.SubMenu.Count > 0)
            {
                MoeSitesLv3ComboBox.Visibility = Visibility.Visible;
                MoeSitesLv3ComboBox.ItemsSource = menu.SubMenu;
                MoeSitesLv3ComboBox.SelectedIndex = 0;
            }
            else
            {
                MoeSitesLv3ComboBox.Visibility = Visibility.Collapsed;
            }
            VisualStateManager.GoToState(this, menu.NoNeedKeyword ? nameof(NotSurportKeywordState) : nameof(SurportKeywordState), true);
        }

        private void KeywordComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KeywordComboBox.SelectedIndex < 0) return;
            var item = HintItems[KeywordComboBox.SelectedIndex];
            if (!item.IsEnable) return;
            KeywordTextBox.Text = item.Word;
            KeywordTextBox.Focus();
        }

        public void Init(SiteManager manager,Settings settings)
        {
            SiteManager = manager;
            MoeSitesComboBox.ItemsSource = SiteManager.Sites;
            Settings = settings;
            DataContext = Settings;
        }

        private void SearchParaCheckBoxOnChecked(object sender, RoutedEventArgs e)
        {
            SearchParaPopupGrid.LargenShowSb().Begin();
        }

        
        private void MoeSitesComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(KeywordTextBox!=null) KeywordTextBox.Text = "";
            var index = MoeSitesComboBox.SelectedIndex;
            CurrentSelectedSite = index >= 0 ? SiteManager.Sites[index] : null;
            if(CurrentSelectedSite == null)return;
            MoeSitesSubComboBox.ItemsSource = CurrentSelectedSite.SubMenu;
            // change visual
            if (CurrentSelectedSite.SubMenu.Count > 0)
            {
                MoeSitesSubComboBox.SelectedIndex = 0;
                MoeSitesSubComboBox.Visibility = Visibility.Visible;
            }
            else
            {
                MoeSitesSubComboBox.Visibility = Visibility.Collapsed;
            }
            VisualStateManager.GoToState(this, CurrentSelectedSite.SurpportState.IsSupportKeyword ? nameof(SurportKeywordState) : nameof(NotSurportKeywordState), true);
            FilterResolutionCheckBox.IsEnabled = CurrentSelectedSite.SurpportState.IsSupportResolution;
            FilterExlicitGroup.IsEnabled = CurrentSelectedSite.SurpportState.IsSupportRating;
            KeywordComboBox.Text = "";
            DownloadTypeComboBox.ItemsSource = CurrentSelectedSite.DownloadTypes;
            DownloadTypeComboBox.SelectedIndex = 0;
        }

        private CancellationTokenSource CurrentTaskCts { get; set; }

        public async Task ShowKeywordComboBoxItemsAsync( string keyword, CancellationToken token)
        {
            try
            {
                HintItems.Clear();
                AddHistoryItems();
                await Task.Delay(600, token); // 等待0.6再开始获取，避免每输入一个字都进行网络操作
                if (string.IsNullOrWhiteSpace(keyword)) throw new Exception("keyword is empty");
                // 开始搜索
                var list = await CurrentSelectedSite.GetAutoHintItemsAsync(GetSearchPara(), token);
                if (list.Count > 0)
                {
                    HintItems.Clear();
                    foreach (var item in list)
                    {
                        HintItems.Add(item);
                    }
                    AddHistoryItems();
                }
                App.Log($"AutoPredict 搜索完成 结果个数{list.Count}");
                
            }
            catch (TaskCanceledException) // 任务取消
            {
                return;
            }
            catch (Exception e)
            {
                App.Log(e.Message);
            }

            this.Sb("SearchingSpinSb").Stop();
            CurrentTaskCts = null;
        }
        
        private void AddHistoryItems()
        {
            HintItems.Add(new AutoHintItem { Word = "---------历史记录---------", IsEnable = false });
            if (Settings?.HistoryKeywords?.Count > 0)
            {
                foreach (var kitem in Settings.HistoryKeywords)
                {
                    HintItems.Add(kitem);
                }
            }
        }

        public SearchPara GetSearchPara()
        {
            var para = new SearchPara
            {
                Site = CurrentSelectedSite,
                Count = FilterCountBox.NumCount,
                PageIndex = FilterStartPageBox.NumCount,
                Keyword = KeywordTextBox.Text,
                IsShowExplicit = FilterExlicitCheckBox.IsChecked == true,
                IsShowExplicitOnly = ShowExlicitOnlyCheckBox.IsChecked == true,
                IsFilterResolution = FilterResolutionCheckBox.IsChecked == true,
                MinWidth = FilterMinWidthBox.NumCount,
                MinHeight = FilterMinHeightBox.NumCount,
                Orientation = (ImageOrientation) OrientationComboBox.SelectedIndex,
                IsFilterFileType = FilterFileTypeCheckBox.IsChecked == true,
                FilterFileTpyeText = FilterFileTypeTextBox.Text,
                IsFileTypeShowSpecificOnly = FileTypeShowSpecificOnlyComboBox.SelectedIndex == 1,
                DownloadType = CurrentSelectedSite.DownloadTypes[DownloadTypeComboBox.SelectedIndex],
            };
            if (!Settings.IsXMode) para.IsShowExplicit = false;
            return para;
        }

    }
}