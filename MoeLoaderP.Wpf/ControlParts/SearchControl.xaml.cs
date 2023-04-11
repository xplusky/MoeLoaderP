using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
///     搜索控件
/// </summary>
public partial class SearchControl
{
        
    public Settings Settings { get; set; }
        
    public MoeSiteConfig CurrentConfig { get; set; }
    public AutoHintItems CurrentHintItems { get; set; } = new();

    private bool _lastKeywordGridStateIsDisplay;
        
    public SearchControl()
    {
        InitializeComponent();
    }

    public ComboBox GetSitesMenuLvX(int level)
    {
        return level switch
        {
            1 => MoeSitesLv1ComboBox,
            2 => MoeSitesLv2ComboBox,
            3 => MoeSitesLv3ComboBox,
            4 => MoeSitesLv4ComboBox,
            _ => null
        };
    }
        
    public void Init(Settings settings)
    {
        Settings = settings;
        DataContext = Settings;
        
        Application.Current.Deactivated += delegate
        {
            if (SearchHintParaPopup.IsOpen) SearchHintParaPopup.IsOpen = false;
        };

        Application.Current.Activated += async delegate
        {
            await Task.Yield();
            if (KeywordTextBox.IsKeyboardFocused) SearchHintParaPopup.IsOpen = true;
        };

        Settings.SiteManager.Sites.CollectionChanged += SitesOnCollectionChanged;
        MoeSitesLv1ComboBox.ItemsSource = Settings.SiteManager.Sites;

        CustomAddButton.Click += delegate { App.CustomSiteDir.GoDirectory(); };
        AccountButton.Click += AccountButtonOnClick;
        AccountButton.MouseRightButtonUp += delegate
        {
            Settings.SiteManager.CurrentSelectedSite.Logout();
            AccountCheckedIconTextBlock.Visibility = Visibility.Collapsed;
        };

        KeywordTextBox.TextChanged += KeywordTextBoxOnTextChanged;
        KeywordTextBox.GotFocus += delegate
        {
            SearchHintParaPopup.IsOpen = true;
        };
        KeywordTextBox.LostFocus += async delegate
        {
            await Task.Yield();
            if (IsVisualFocusOn(SearchHintParaControl)) return;
            SearchHintParaPopup.IsOpen = false;
        };
        SearchHintParaControl.LostFocus += async delegate
        {
            await Task.Yield();
            if(KeywordTextBox.IsFocused || IsVisualFocusOn(SearchHintParaControl)) return;
            SearchHintParaPopup.IsOpen = false;
            if (SearchParaCheckBox.IsChecked == true)
            {
                SearchParaCheckBox.IsChecked = false;
            }
        };
        KeywordTextBox.PreviewKeyDown += KeywordTextBoxOnKeyDown;
        SearchParaCheckBox.Checked += delegate
        {
            SearchHintParaPopup.IsOpen = true;
            SearchHintParaControl.Focus();
        };
        SearchParaCheckBox.Unchecked += delegate
        {
            SearchHintParaPopup.IsOpen = false;
        };
        

        InitParaControl();
        PopupHelper.SetPopupPlacementTarget(SearchHintParaPopup, KeywordGridRoot);

        for (var i = 1; i < 5; i++)
        {
            var ii = i;
            GetSitesMenuLvX(i).SelectionChanged += delegate { MoeSitesComboBoxOnSelectionChanged(ii); };
        }
        SearchButton.Click += SearchButtonOnClick;
        MoeSitesLv1ComboBox.SelectedIndex = 0;
        SpinTextBlock.Visibility = Visibility.Collapsed;


    }
    

    public void InitParaControl()
    {
        var pbox = SearchHintParaControl;
        pbox.ShowNsfwOnlyCheckBox.Checked += delegate { pbox.FilterNsfwCheckBox.IsChecked = true; };
        pbox.FilterNsfwCheckBox.Unchecked += delegate { pbox.ShowNsfwOnlyCheckBox.IsChecked = false; };
        pbox.FilterCountBox.NumChange += control => { Settings.SiteManager.CurrentSelectedSite.SiteSettings.SetSetting("CountPerPage", control.NumCount.ToString()); };
        pbox.MirrorSiteComboBox.SelectionChanged += delegate
        {
            var i = pbox.MirrorSiteComboBox.SelectedIndex;
            if (i < 0) return;
            Settings.SiteManager.CurrentSelectedSite.SiteSettings.SetSetting("MirrorSiteIndex", i.ToString());
        };
        pbox.KeywordListBox.ItemsSource = CurrentHintItems;
        pbox.KeywordListBox.SelectionChanged += delegate
        {
            if (pbox.KeywordListBox.SelectedIndex < 0) return;
            if (pbox.KeywordListBox.SelectedItem is not AutoHintItem { IsEnable: true } item) return;
            KeywordTextBox.Text = item.Word; 
            //KeywordTextBox.Focus();
            KeywordTextBox.SelectionStart = KeywordTextBox.Text.Length;
        };

        // proxy
        CurrentSiteProxySets.Add(new IndiSiteProxySetting{Name = "全局设置（默认）", ProxyMode = Settings.ProxyModeEnum.Default});
        CurrentSiteProxySets.Add(new IndiSiteProxySetting { Name = "不使用代理", ProxyMode = Settings.ProxyModeEnum.None });
        CurrentSiteProxySets.Add(new IndiSiteProxySetting { Name = "自定义代理", ProxyMode = Settings.ProxyModeEnum.Custom });
        CurrentSiteProxySets.Add(new IndiSiteProxySetting { Name = "系统代理", ProxyMode = Settings.ProxyModeEnum.Ie });

        pbox.ProxyComboBox.ItemsSource = CurrentSiteProxySets;
        pbox.ProxyComboBox.SelectionChanged += delegate
        {
            var set = Settings.SiteManager.CurrentSelectedSite.SiteSettings;
            if (pbox.ProxyComboBox.SelectedItem is IndiSiteProxySetting p && p.ProxyMode != set.SiteProxy)
            {
                set.SiteProxy = p.ProxyMode;
            }
            Settings.SiteManager.CurrentSelectedSite.OnPropertyChanged(nameof(MoeSite.IsUseProxy));
        };
    }

    

    public IndiSiteProxySettings CurrentSiteProxySets { get; set; } = new IndiSiteProxySettings();
    

    public void ClearCurrentHintItems()
    {
        CurrentHintItems.Clear();
    }

    public bool IsVisualFocusOn(Visual myVisual)
    {
        var e = myVisual as UIElement;
        if (e?.IsFocused == true || e?.IsKeyboardFocused == true) return true;
        var cb = myVisual as ComboBox;
        if (cb?.IsDropDownOpen == true) return true;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(myVisual); i++)
        {
            var childVisual = (Visual)VisualTreeHelper.GetChild(myVisual, i);
            var b = IsVisualFocusOn(childVisual);
            if (b) return true;
        }

        return false;
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
        SearchHintParaPopup.IsOpen = false;
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
        wm.MoeExplorer.DownloadTypeComboBox.ItemsSource = Settings.CurrentSession.ResultDownloadTypes;
        wm.MoeExplorer.DownloadTypeComboBox.SelectedIndex = 0;
        var vp = await Settings.CurrentSession.SearchNextVisualPage();
        if (vp is not null) _ = wm.MoeExplorer.ShowVisualPage(vp);
        SetSearchVisual(false);
            
    }

    private async void AccountButtonOnClick(object sender, RoutedEventArgs e)
    {
        var wnd = new LoginWindow();
        await wnd.Init(Settings, Settings.SiteManager.CurrentSelectedSite);
        try
        {
            wnd.Owner = Application.Current.MainWindow;
            wnd.ShowDialog();
            MoeSitesComboBoxOnSelectionChanged(1);
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
        switch (e.Key)
        {
            case Key.Space:
                ChangeKeywordToButton();
                break;
            case Key.Back when KeywordTextBox.SelectionStart > 0:
            case Key.Back when MultiWordsButtonsStackPanel.Children.Count == 0:
                return;
            case Key.Back:
                MultiWordsButtonsStackPanel.Children.RemoveAt(MultiWordsButtonsStackPanel.Children.Count - 1);
                break;
            case Key.Enter:
                SearchButtonOnClick(sender, e);
                break;
        }
    }

    public Button GenMultiWordButton(string str)
    {
        var button = new Button
        {
            Template = FindResource("MoeMultiWordButtonTemplate") as ControlTemplate
        };
        var text = new TextBlock
        {
            Text = str,
            Margin = new Thickness(2, 0, 2, 0)
        };
        button.Content = text;
        button.Margin = new Thickness(2, 0, 0, 0);
        return button;
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
        AccountCheckedIconTextBlock.Visibility =
            Settings.SiteManager.CurrentSelectedSite.SiteSettings.LoginCookies == null
                ? Visibility.Collapsed
                : Visibility.Visible;

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

        var box = SearchHintParaControl;
        // ImageOrders
        if (cfg.ImageOrders != null)
        {
            box.OrderByGrid.Visibility = Visibility.Visible;
            box.OrderByComboBox.ItemsSource = cfg.ImageOrders;
            box.OrderByComboBox.SelectedIndex = 0;
        }
        else
        {
            box.OrderByGrid.Visibility = Visibility.Collapsed;
        }

        // IsSupportResolution
        box.FilterResolutionGroup.Visibility = cfg.IsSupportResolution ? Visibility.Visible : Visibility.Collapsed;

        box.FilterResolutionCheckBox.IsEnabled = cfg.IsSupportResolution;
        box.FilterNsfwGroup.IsEnabled = cfg.IsSupportRating;
        box.FilterStartIdGrid.Visibility = cfg.IsSupportSearchByImageLastId ? Visibility.Visible : Visibility.Collapsed;
        
        var cs = Settings.SiteManager.CurrentSelectedSite;

        //DownloadTypeComboBox.ItemsSource = cs.DownloadTypes;
        //DownloadTypeComboBox.SelectedIndex = 0;

        box.FilterStartIdBox.MaxCount = 0;
        box.FilterStartPageBox.NumCount = 1;
        var numPerPage = cs.SiteSettings.GetSetting("CountPerPage").ToInt();
        box.FilterCountBox.NumCount = numPerPage > 0 ? numPerPage : 60;
        if (cs.Mirrors != null)
        {
            box.MirrorSiteGrid.Visibility = Visibility.Visible;
            box.MirrorSiteComboBox.ItemsSource = cs.Mirrors;
            var mirrorIndex = cs.SiteSettings.GetSetting("MirrorSiteIndex").ToInt();
            box.MirrorSiteComboBox.SelectedIndex = mirrorIndex < 0 || mirrorIndex >= box.MirrorSiteComboBox.Items.Count ? 0 : mirrorIndex;
        }
        else
        {
            box.MirrorSiteGrid.Visibility = Visibility.Collapsed;
            box.MirrorSiteComboBox.ItemsSource = null;
        }

    }

        
    public void InitHistoryItems()
    {
        //CurrentHintItems.Add(new AutoHintItem { IsEnable = false, Word = "---------历史---------" });
        var history = Settings.SiteManager.CurrentSelectedSite.SiteSettings.History;
        if (history?.Count > 0)
        {
            foreach (var item in Settings.SiteManager.CurrentSelectedSite.SiteSettings.History)
            {
                CurrentHintItems.Add(item);
            }
        }
    }
    

    private void MoeSitesComboBoxOnSelectionChanged(int level)
    {
        switch (level)
        {
            case 1:
            {
                if (MoeSitesLv1ComboBox.SelectedItem is not MoeSite currentSite) return;
                Settings.SiteManager.CurrentSelectedSite = currentSite;
                currentSite.CatChangeAction += CurrentSiteOnCatChangeAction;
                KeywordTextBox.Text = "";
                ClearCurrentHintItems();
                InitHistoryItems();
                var set = currentSite.SiteSettings;
                var pbox = SearchHintParaControl;
                pbox.ProxyComboBox.SelectedItem = CurrentSiteProxySets.FirstOrDefault(s => s.ProxyMode == set.SiteProxy);
                    MoeDatePicker.SelectedDate = null;
                    
                var lv2Cat = Settings.SiteManager.CurrentSelectedSite.Lv2Cat;
                if (lv2Cat?.Any() == true)
                {
                    MoeSitesLv2ComboBox.ItemsSource = lv2Cat;
                    if (MoeSitesLv2ComboBox.SelectedIndex == 0)
                        MoeSitesComboBoxOnSelectionChanged(2);
                    else MoeSitesLv2ComboBox.SelectedIndex = 0;

                    MoeSitesLv2ComboBox.SelectedIndex = 0;
                    this.GoState(nameof(ShowSubMenuState));
                }
                else
                {
                    this.GoState(nameof(HideSubMenuState));
                    this.GoState(nameof(HideLv3MenuState));
                    this.GoState(nameof(HideLv4MenuState));
                    MaxLevel = 1;
                }

                break;
            }
            case 2:
            {
                var lv2Si = MoeSitesLv2ComboBox.SelectedIndex;
                if (lv2Si == -1) return;
                var lv3 = Settings.SiteManager.CurrentSelectedSite.Lv2Cat[lv2Si].SubCategories;
                if (lv3?.Any() == true)
                {
                    MoeSitesLv3ComboBox.ItemsSource = lv3;
                    if (MoeSitesLv3ComboBox.SelectedIndex == 0)
                        MoeSitesComboBoxOnSelectionChanged(3);
                    else MoeSitesLv3ComboBox.SelectedIndex = 0;

                    this.GoState(nameof(ShowLv3MenuState));
                }
                else
                {
                    this.GoState(nameof(HideLv3MenuState));
                    this.GoState(nameof(HideLv4MenuState));
                    MaxLevel = 2;
                }

                break;
            }
            case 3:
            {
                var lv3Si = MoeSitesLv3ComboBox.SelectedIndex;
                if (lv3Si == -1) return;
                var lv4 = (MoeSitesLv2ComboBox.SelectedItem as Category)?.SubCategories[lv3Si].SubCategories;
                if (lv4?.Any() == true)
                {
                    MoeSitesLv4ComboBox.ItemsSource = lv4;
                    if (MoeSitesLv4ComboBox.SelectedIndex == 0)
                    {
                        MoeSitesComboBoxOnSelectionChanged(4);
                        MaxLevel = 4;
                    }
                    else MoeSitesLv4ComboBox.SelectedIndex = 0;

                    this.GoState(nameof(ShowLv4MenuState));
                }
                else
                {
                    this.GoState(nameof(HideLv4MenuState));
                    MaxLevel = 3;
                }

                break;
            }
        }

        var cfg = Settings.SiteManager.CurrentSelectedSite.Config;
        //var lvv2 = Settings.SiteManager.CurrentSelectedSite.Lv2Cat;
        //if (MoeSitesLv2ComboBox.SelectedIndex != -1 && lvv2?.Any() == true)
        //{
        //    var lv2ItemCat = MoeSitesLv2ComboBox.SelectedItem as Category;
        //    if (lv2ItemCat?.Config != null) cfg = lv2ItemCat.Config;
        //    var lv3 = lvv2[MoeSitesLv2ComboBox.SelectedIndex].SubCategories;
        //    if (MoeSitesLv3ComboBox.SelectedIndex != -1 && lv3?.Any() == true)
        //    {
        //        var lv3ItemCat = MoeSitesLv3ComboBox.SelectedItem as Category;
        //        if (lv3ItemCat?.Config != null) cfg = lv3ItemCat.Config;
        //        var lv4 = lv3[MoeSitesLv3ComboBox.SelectedIndex].SubCategories;
        //        if (MoeSitesLv4ComboBox.SelectedIndex != -1 && lv4?.Any() == true)
        //        {
        //            var lv4ItemCat = MoeSitesLv4ComboBox.SelectedItem as Category;
        //            if (lv4ItemCat?.Config != null) cfg = lv4ItemCat.Config;
        //        }
        //    }
        //}
        if (MaxLevel != 1)
        {
            if (GetSitesMenuLvX(MaxLevel).SelectedItem is Category cat) cfg = cat.Config;
        }

        AdaptConfig(cfg);
    }

    private void CurrentSiteOnCatChangeAction(Categories obj)
    {
        if(obj.Count>0) MoeSitesComboBoxOnSelectionChanged(1);
    }

    public int MaxLevel { get; set; } = 1;


    public CancellationTokenSource HintCts { get; set; }
    private bool IsHintSpinSbStart { get; set; }
    private async void KeywordTextBoxOnTextChanged(object sender, TextChangedEventArgs e)
    {
        OverflowTextBlock.Visibility = KeywordTextBox.Text.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
        var searchSb = this.Sb("SearchingSpinSb");
        if (KeywordTextBox.Text.Length < 1)
        {
            searchSb.Stop();
            return;
        }
        
        HintCts?.Cancel();
         await Task.Yield();
        HintCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await ShowKeywordComboBoxItemsAsync(KeywordTextBox.Text, HintCts.Token);
        }
        catch (Exception exception)
        {
            Ex.Log($"search {KeywordTextBox.Text} hint exception catch");
            Ex.Log(exception);
        }
    }
    

    /// <summary>
    ///     获取关键字的联想
    /// </summary>
    public async Task ShowKeywordComboBoxItemsAsync(string keyword, CancellationToken token)
    {
        var searchSb = this.Sb("SearchingSpinSb");
        if (!IsHintSpinSbStart)
        {
            searchSb.Begin();
            IsHintSpinSbStart = true;
            SpinTextBlock.Visibility = Visibility.Visible;
        }
        if (keyword.IsEmpty()) return;
        await Task.Delay(600, token); // 等待0.6再开始获取，避免每输入一个字都进行网络操作 
        if (token.IsCancellationRequested) throw new TaskCanceledException();
        var para = GenSearchPara();
        try
        {
            var list = await Settings.SiteManager.CurrentSelectedSite.GetAutoHintItemsAsync(para, token);
            if (list != null && list.Any())
            {
                CurrentHintItems.Clear();
                foreach (var item in list) CurrentHintItems.Add(item);
                InitHistoryItems();
                Ex.Log($"AutoHint 搜索完成 结果个数{list.Count}");
            }
        }
        catch (Exception e)
        {
            Ex.Log(e);
        }
        searchSb.Stop();
        IsHintSpinSbStart = false;
        SpinTextBlock.Visibility =Visibility.Collapsed;
    }
        
    public SearchPara GenSearchPara()
    {
        var keys = new List<string>();
        foreach (Button button in MultiWordsButtonsStackPanel.Children)
        {
            var text = (button.Content as TextBlock)?.Text;
            keys.Add(text);
        }

        var pbox = SearchHintParaControl;
        var para = new SearchPara
        {
            Site = Settings.SiteManager.CurrentSelectedSite,
                
            Keyword = KeywordTextBox.Text,
            MultiKeywords = keys,
            Lv2MenuIndex = MoeSitesLv2ComboBox.SelectedIndex,
            Lv3MenuIndex = MoeSitesLv3ComboBox.SelectedIndex,
            Lv4MenuIndex = MoeSitesLv4ComboBox.SelectedIndex,
            Config = CurrentConfig,
            CountLimit = pbox.FilterCountBox.NumCount,
            PageIndex = pbox.FilterStartPageBox.NumCount,
            IsShowExplicit = Settings.IsXMode && pbox.FilterNsfwCheckBox.IsChecked == true,
            IsShowExplicitOnly = pbox.ShowNsfwOnlyCheckBox.IsChecked == true,
            IsFilterResolution = pbox.FilterResolutionCheckBox.IsChecked == true,
            MinWidth = pbox.FilterMinWidthBox.NumCount,
            MinHeight = pbox.FilterMinHeightBox.NumCount,
            Orientation = (ImageOrientation)pbox.OrientationComboBox.SelectedIndex,
            Date = MoeDatePicker.SelectedDate,
            PageIndexCursor = $"{pbox.FilterStartIdBox.NumCount}" == "0" ? null : $"{pbox.FilterStartIdBox.NumCount}",
            MirrorSite = pbox.MirrorSiteComboBox.SelectionBoxItem as MirrorSiteConfig,
            OrderBy = pbox.OrderByComboBox.SelectedItem as ImageOrder
        };
        return para;
    }
        
}
public class IndiSiteProxySetting
{
    public Settings.ProxyModeEnum ProxyMode { get; set; }

    public string Name { get; set; }
}

public class IndiSiteProxySettings : ObservableCollection<IndiSiteProxySetting> { }
