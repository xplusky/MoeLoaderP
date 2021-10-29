using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Wpf.ControlParts
{
    /// <summary>
    ///     搜索控件
    /// </summary>
    public partial class SearchControl : INotifyPropertyChanged
    {
        private MoeSite _currentSelectedSite;

        private bool _lastKeywordGridStateIsDisplay;


        public SearchControl()
        {
            InitializeComponent();
        }

        public Settings Settings { get; set; }

        public MoeSite CurrentSelectedSite
        {
            get => _currentSelectedSite;
            set
            {
                if (value.Equals(_currentSelectedSite)) return;
                _currentSelectedSite = value;
                OnPropertyChanged(nameof(CurrentSelectedSite));
            }
        }


        public AutoHintItems CurrentHintItems { get; set; } = new();

        public MoeSiteConfig CurrentConfig { get; set; }


        private CancellationTokenSource CurrentHintTaskCts { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Init(Settings settings)
        {
            Settings = settings;
            Settings.SiteManager = new SiteManager(Settings);

            Settings.PropertyChanged += SettingsOnPropertyChanged;

            MoeSitesLv1ComboBox.ItemsSource = Settings.SiteManager.Sites;
            DataContext = Settings;


            // account 
            AccountButton.Click += AccountButtonOnClick;

            ShowExlicitOnlyCheckBox.Checked += delegate { FilterExlicitCheckBox.IsChecked = true; };
            FilterExlicitCheckBox.Unchecked += delegate { ShowExlicitOnlyCheckBox.IsChecked = false; };

            KeywordTextBox.TextChanged += KeywordTextBoxOnTextChanged;
            KeywordTextBox.GotFocus += delegate { KeywordPopup.IsOpen = true; };
            KeywordTextBox.LostFocus += delegate { KeywordPopup.IsOpen = false; };
            KeywordTextBox.PreviewKeyDown += KeywordTextBoxOnKeyDown;

            KeywordListBox.ItemsSource = CurrentHintItems;
            KeywordListBox.SelectionChanged += KeywordComboBoxOnSelectionChanged;


            Settings.SiteManager.Sites.CollectionChanged += SitesOnCollectionChanged;

            MoeSitesLv1ComboBox.SelectionChanged += (sender, args) => MoeSitesComboBoxOnSelectionChanged(sender, args, 1); // 一级菜单选择改变
            MoeSitesLv2ComboBox.SelectionChanged += (sender, args) => MoeSitesComboBoxOnSelectionChanged(sender, args, 2); // 二级菜单选择改变
            MoeSitesLv3ComboBox.SelectionChanged += (sender, args) => MoeSitesComboBoxOnSelectionChanged(sender, args, 3); // 三级菜单选择改变
            MoeSitesLv4ComboBox.SelectionChanged += (sender, args) => MoeSitesComboBoxOnSelectionChanged(sender, args, 4); // 四级菜单选择改变


            MoeSitesLv1ComboBox.SelectedIndex = 0;

            AccountButton.MouseRightButtonUp += AccountButtonOnMouseRightButtonUp;
            CustomAddButton.Click += delegate { App.CustomSiteDir.GoDirectory(); };

            FilterCountBox.NumChange += control => { CurrentSelectedSite.SiteSettings.SetSetting("CountPerPage", control.NumCount.ToString()); };
            MirrorSiteComboBox.SelectionChanged += delegate
            {
                var i = MirrorSiteComboBox.SelectedIndex;
                if (i < 0) return;
                CurrentSelectedSite.SiteSettings.SetSetting("MirrorSiteIndex", i.ToString());
            };

            SearchButton.Click += SearchButtonOnClick;
        }

        private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var sm = Settings.SiteManager;
            if (e.PropertyName == nameof(Settings.IsCustomSiteMode))
            {
                sm.Sites.Clear();
                if (Settings.IsCustomSiteMode) sm.SetCustomSitesFormJson(App.CustomSiteDir);
                else sm.SetDefaultSiteList();
            }
        }

        public void SetSearchVisual(bool isSearching)
        {
            if (Application.Current.MainWindow is not MainWindow wm) return;
            if (isSearching)
            {
                if (Settings.CurrentSession == null) wm.Sb("BeginSearchSb").Begin();
                this.GoState(nameof(SearchingState));
                wm.MoeExplorer.SearchStartedVisual();
            }
            else
            {
                wm.MoeExplorer.SearchStopVisual();
                this.GoState(nameof(StopingState));
            }
        }

        public async void SearchButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (Settings.CurrentSession?.IsSearching == true)
            {
                SearchButton.IsEnabled = false;
                await Settings.CurrentSession.StopSearch();
                SearchButton.IsEnabled = true;
                SetSearchVisual(false);
                return;
            }

            await StartSearch();
        }

        public async Task StartSearch()
        {
            if (Application.Current.MainWindow is not MainWindow wm) return;

            SetSearchVisual(true);
            wm.StatusTextBlock.Text = "";
            if (CurrentConfig.IsSupportMultiKeywords)
                if (!KeywordTextBox.Text.IsEmpty())
                    ChangeKeywordToButton();

            var para = GenSearchPara();

            Settings.CurrentSession = new SearchSession(para);
            Settings.CurrentSession.SaveKeywords();
            wm.SiteTextBlock.Text = Settings.CurrentSession.GetCurrentSearchStateText();

            var vp = await Settings.CurrentSession.SearchNextVisualPage();
            if (vp is not null) _ = wm.MoeExplorer.ShowVisualPage(vp);
            SetSearchVisual(false);
        }

        private async void AccountButtonOnClick(object sender, RoutedEventArgs e)
        {
            var wnd = new LoginWindow();
            await wnd.Init(Settings, CurrentSelectedSite);
            try
            {
                wnd.Owner = Application.Current.MainWindow;
                wnd.ShowDialog();
                Refresh();
            }
            catch (Exception ex)
            {
                Ex.Log(ex);
            }
        }

        public void ChangeKeywordToButton()
        {
            if (KeywordTextBox.Text.IsEmpty()) return;
            if (CurrentConfig.IsSupportMultiKeywords == false) return;
            var button = GenMultiWordButton(KeywordTextBox.Text.Trim());
            MultiWordsButtonsStackPanel.Children.Add(button);
            button.Click += delegate { MultiWordsButtonsStackPanel.Children.Remove(button); };
            KeywordTextBox.Text = string.Empty;
        }

        private void KeywordTextBoxOnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) ChangeKeywordToButton();

            if (e.Key == Key.Back)
            {
                if (KeywordTextBox.SelectionStart > 0) return;
                //if (KeywordTextBox.Text != string.Empty) return;
                if (MultiWordsButtonsStackPanel.Children.Count == 0) return;
                MultiWordsButtonsStackPanel.Children.RemoveAt(MultiWordsButtonsStackPanel.Children.Count - 1);
            }

            if (e.Key == Key.Enter)
            {
                SearchButtonOnClick(sender, e);
                KeywordPopup.IsOpen = false;
            }
        }

        public Button GenMultiWordButton(string str)
        {
            var button = new Button();
            button.Template = FindResource("MoeMultiWordButtonTemplate") as ControlTemplate;
            var text = new TextBlock();
            text.Text = str;
            text.Margin = new Thickness(2, 0, 2, 0);
            button.Content = text;
            button.Margin = new Thickness(2, 0, 0, 0);
            return button;
        }


        private void AccountButtonOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentSelectedSite.Logout();
        }

        private void SitesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (MoeSitesLv1ComboBox.SelectedIndex == -1) MoeSitesLv1ComboBox.SelectedIndex = 0;
        }

        public void AdaptConfig(MoeSiteConfig cfg)
        {
            if (cfg == null) return;
            CurrentConfig = cfg;
            // IsSupportAccount
            this.GoState(cfg.IsSupportAccount ? nameof(ShowAccountButtonState) : nameof(HideAccountButtonState));

            // IsSupportDatePicker
            this.GoState(cfg.IsSupportDatePicker ? nameof(ShowDatePickerState) : nameof(HideDatePickerState));

            // IsSupportKeyword
            this.GoState(cfg.IsSupportKeyword ? nameof(SurportKeywordState) : nameof(NotSurportKeywordState));
            if (cfg.IsSupportKeyword)
            {
                if (_lastKeywordGridStateIsDisplay == false)
                {
                    var sb = KeywordGrid.HorizonEnlargeShowSb(164d);
                    sb.Completed += delegate { sb.Stop(); };
                    sb.Begin();
                    _lastKeywordGridStateIsDisplay = true;
                }
            }
            else
            {
                if (_lastKeywordGridStateIsDisplay)
                {
                    KeywordGrid.HorizonLessenShowSb().Begin();
                    _lastKeywordGridStateIsDisplay = false;
                }
            }

            // IsSupportMultiKeywords
            MultiWordsButtonsStackPanel.Children.Clear();

            // IsCustomSite
            this.GoState(cfg.IsCustomSite ? nameof(ShowCustomAddButtonState) : nameof(HideCustomAddButtonState));

            // ImageOrders
            if (cfg.ImageOrders != null)
            {
                OrderByGrid.Visibility = Visibility.Visible;
                OrderByComboBox.ItemsSource = cfg.ImageOrders;
                OrderByComboBox.SelectedIndex = 0;
            }
            else
            {
                OrderByGrid.Visibility = Visibility.Collapsed;
            }

            // IsSupportResolution
            FilterResolutionGroup.Visibility = cfg.IsSupportResolution ? Visibility.Visible : Visibility.Collapsed;

            FilterResolutionCheckBox.IsEnabled = cfg.IsSupportResolution;
            FilterExlicitGroup.IsEnabled = cfg.IsSupportRating;
            FilterStartIdGrid.Visibility = cfg.IsSupportSearchByImageLastId ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ParaBoxVisualUpdate()
        {
            var cs = CurrentSelectedSite;

            DownloadTypeComboBox.ItemsSource = cs.DownloadTypes;
            DownloadTypeComboBox.SelectedIndex = 0;

            FilterStartIdBox.MaxCount = 0;
            FilterStartPageBox.NumCount = 1;
            var numPerPage = cs.SiteSettings.GetSetting("CountPerPage").ToInt();
            FilterCountBox.NumCount = numPerPage > 0 ? numPerPage : 60;
            if (cs.Mirrors != null)
            {
                MirrorSiteGrid.Visibility = Visibility.Visible;
                MirrorSiteComboBox.ItemsSource = cs.Mirrors;
                var mirrorIndex = CurrentSelectedSite.SiteSettings.GetSetting("MirrorSiteIndex").ToInt();
                MirrorSiteComboBox.SelectedIndex = mirrorIndex < 0 || mirrorIndex >= MirrorSiteComboBox.Items.Count ? 0 : mirrorIndex;
            }
            else
            {
                MirrorSiteGrid.Visibility = Visibility.Collapsed;
                MirrorSiteComboBox.ItemsSource = null;
            }
        }

        public void Refresh()
        {
            MoeSitesComboBoxOnSelectionChanged(null, null, 1);
        }

        private void MoeSitesComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e, int level)
        {
            if (level == 1)
            {
                if (MoeSitesLv1ComboBox.SelectedItem is not MoeSite currentSite) return;
                CurrentSelectedSite = currentSite;
                KeywordTextBox.Text = "";
                CurrentHintItems.Clear();
                InitHistoryItems();
                MoeDatePicker.SelectedDate = null;
                ParaBoxVisualUpdate();
                var lv2Cat = CurrentSelectedSite.Lv2Cat;
                if (lv2Cat?.Any() == true)
                {
                    MoeSitesLv2ComboBox.ItemsSource = lv2Cat;
                    if (MoeSitesLv2ComboBox.SelectedIndex == 0)
                        MoeSitesComboBoxOnSelectionChanged(sender, e, 2);
                    else MoeSitesLv2ComboBox.SelectedIndex = 0;

                    MoeSitesLv2ComboBox.SelectedIndex = 0;
                    this.GoState(nameof(ShowSubMenuState));
                }
                else
                {
                    this.GoState(nameof(HideSubMenuState));
                    this.GoState(nameof(HideLv3MenuState));
                    this.GoState(nameof(HideLv4MenuState));
                }
            }

            if (level == 2)
            {
                var lv2Si = MoeSitesLv2ComboBox.SelectedIndex;
                if (lv2Si == -1) return;
                var lv3 = CurrentSelectedSite.Lv2Cat[lv2Si].SubCategories;
                if (lv3?.Any() == true)
                {
                    MoeSitesLv3ComboBox.ItemsSource = lv3;
                    if (MoeSitesLv3ComboBox.SelectedIndex == 0)
                        MoeSitesComboBoxOnSelectionChanged(sender, e, 3);
                    else MoeSitesLv3ComboBox.SelectedIndex = 0;

                    this.GoState(nameof(ShowLv3MenuState));
                }
                else
                {
                    this.GoState(nameof(HideLv3MenuState));
                    this.GoState(nameof(HideLv4MenuState));
                }
            }

            if (level == 3)
            {
                var lv3Si = MoeSitesLv3ComboBox.SelectedIndex;
                if (lv3Si == -1) return;
                var lv4 = (MoeSitesLv2ComboBox.SelectedItem as Category)?.SubCategories[lv3Si].SubCategories;
                if (lv4?.Any() == true)
                {
                    MoeSitesLv4ComboBox.ItemsSource = lv4;
                    if (MoeSitesLv4ComboBox.SelectedIndex == 0)
                        MoeSitesComboBoxOnSelectionChanged(sender, e, 4);
                    else MoeSitesLv4ComboBox.SelectedIndex = 0;

                    this.GoState(nameof(ShowLv4MenuState));
                }
                else
                {
                    this.GoState(nameof(HideLv4MenuState));
                }
            }

            var cfg = CurrentSelectedSite.Config;
            var lvv2 = CurrentSelectedSite.Lv2Cat;
            if (MoeSitesLv2ComboBox.SelectedIndex != -1 && lvv2?.Any() == true)
            {
                var lv2ItemCat = MoeSitesLv2ComboBox.SelectedItem as Category;
                if (lv2ItemCat.OverrideConfig != null) cfg = lv2ItemCat.OverrideConfig;
                var lv3 = lvv2[MoeSitesLv2ComboBox.SelectedIndex].SubCategories;
                if (MoeSitesLv3ComboBox.SelectedIndex != -1 && lv3?.Any() == true)
                {
                    var lv3ItemCat = MoeSitesLv3ComboBox.SelectedItem as Category;
                    if (lv3ItemCat.OverrideConfig != null) cfg = lv3ItemCat.OverrideConfig;
                    var lv4 = lv3[MoeSitesLv3ComboBox.SelectedIndex].SubCategories;
                    if (MoeSitesLv4ComboBox.SelectedIndex != -1 && lv4?.Any() == true)
                    {
                        var lv4ItemCat = MoeSitesLv4ComboBox.SelectedItem as Category;
                        if (lv4ItemCat.OverrideConfig != null) cfg = lv4ItemCat.OverrideConfig;
                    }
                }
            }

            AdaptConfig(cfg);
        }

        private async void KeywordTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchSb = this.Sb("SearchingSpinSb");

            async Task search()
            {
                CurrentHintTaskCts = new CancellationTokenSource();
                try
                {
                    await ShowKeywordComboBoxItemsAsync(KeywordTextBox.Text, CurrentHintTaskCts.Token);
                    this.Sb("SearchingSpinSb").Stop();
                }
                catch (Exception ex)
                {
                    if (ex is TaskCanceledException)
                    {

                    }
                    else
                    {
                        Ex.Log(ex.Message);
                    }

                }
                finally
                {
                    searchSb.Stop();
                    CurrentHintTaskCts = null;
                }
            }


            if (CurrentHintTaskCts == null)
            {
                await search();
            }
            else
            {
                CurrentHintTaskCts.Cancel();
                await search();
            }

        }

        private void KeywordComboBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KeywordListBox.SelectedIndex < 0) return;
            if (KeywordListBox.SelectedItem is not AutoHintItem item || !item?.IsEnable == true) return;
            KeywordTextBox.Text = item.Word;
            KeywordTextBox.Focus();
            KeywordTextBox.SelectionStart = KeywordTextBox.Text.Length;
        }

        /// <summary>
        ///     获取关键字的联想
        /// </summary>
        public async Task ShowKeywordComboBoxItemsAsync(string keyword, CancellationToken token)
        {
            CurrentHintItems.Clear();
            InitHistoryItems();
            if (keyword.IsEmpty()) return;
            await Task.Delay(600, token); // 等待0.6再开始获取，避免每输入一个字都进行网络操作 
            if (token.IsCancellationRequested) throw new TaskCanceledException();
            var list = await CurrentSelectedSite.GetAutoHintItemsAsync(GenSearchPara(), token);
            if (list != null && list.Any())
            {
                CurrentHintItems.Clear();
                foreach (var item in list) CurrentHintItems.Add(item);
                InitHistoryItems();
                Ex.Log($"AutoHint 搜索完成 结果个数{list.Count}");
            }
        }

        private void InitHistoryItems()
        {
            CurrentHintItems.Add(new AutoHintItem { IsEnable = false, Word = "---------历史---------" });
            if (CurrentSelectedSite.SiteSettings.History?.Count > 0)
                foreach (var item in CurrentSelectedSite.SiteSettings.History)
                    CurrentHintItems.Add(item);
        }

        public SearchPara GenSearchPara()
        {
            var keys = new List<string>();
            foreach (Button button in MultiWordsButtonsStackPanel.Children)
            {
                var text = (button.Content as TextBlock)?.Text;
                keys.Add(text);
            }

            var para = new SearchPara
            {
                Site = CurrentSelectedSite,
                CountLimit = FilterCountBox.NumCount,
                PageIndex = FilterStartPageBox.NumCount,
                Keyword = KeywordTextBox.Text,
                MultiKeywords = keys,
                IsShowExplicit = Settings.IsXMode && FilterExlicitCheckBox.IsChecked == true,
                IsShowExplicitOnly = ShowExlicitOnlyCheckBox.IsChecked == true,
                IsFilterResolution = FilterResolutionCheckBox.IsChecked == true,
                MinWidth = FilterMinWidthBox.NumCount,
                MinHeight = FilterMinHeightBox.NumCount,
                Orientation = (ImageOrientation)OrientationComboBox.SelectedIndex,
                IsFilterFileType = FilterFileTypeCheckBox.IsChecked == true,
                FilterFileTypeText = FilterFileTypeTextBox.Text,
                IsFileTypeShowSpecificOnly = FileTypeShowSpecificOnlyComboBox.SelectedIndex == 1,
                DownloadType = DownloadTypeComboBox.SelectionBoxItem as DownloadType, //CurrentSelectedSite.DownloadTypes[DownloadTypeComboBox.SelectedIndex],
                Date = MoeDatePicker.SelectedDate,
                PageIndexCursor = $"{FilterStartIdBox.NumCount}" == "0" ? null : $"{FilterStartIdBox.NumCount}",
                Lv2MenuIndex = MoeSitesLv2ComboBox.SelectedIndex,
                Lv3MenuIndex = MoeSitesLv3ComboBox.SelectedIndex,
                Lv4MenuIndex = MoeSitesLv4ComboBox.SelectedIndex,
                Config = CurrentConfig,
                MirrorSite = MirrorSiteComboBox.SelectionBoxItem as MirrorSiteConfig,
                OrderBy = OrderByComboBox.SelectedItem as ImageOrder
            };
            return para;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}