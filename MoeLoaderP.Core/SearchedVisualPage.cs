using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MoeLoaderP.Core;

/// <summary>
///     搜索结果的其中一页（虚拟页）
/// </summary>
public class SearchedVisualPage : BindingObject
{
    private bool _isCurrentPage;
    private int _visualIndex;
    public SearchedPages RealPages { get; set; } = [];

    public bool IsCurrentPage
    {
        get => _isCurrentPage;
        set
        {
            _isCurrentPage = value;
            OnPropertyChanged(nameof(IsCurrentPage));
        }
    }

    public bool IsSearchComplete { get; set; } = false;

    public int VisualIndex
    {
        get => _visualIndex;
        set
        {
            _visualIndex = value;
            OnPropertyChanged(nameof(VisualIndex));
        }
    }

    public int FirstRealPageIndex { get; set; }

    public void LoadStart()
    {
        LoadStartEvent?.Invoke(this);
    }

    public void LoadEnd()
    {
        LoadEndEvent?.Invoke(this);
    }

    public void GetEnd()
    {
        GetEndEvent?.Invoke(this);
    }

    public event Action<SearchedVisualPage> LoadStartEvent;
    public event Action<SearchedVisualPage> GetEndEvent;
    public event Action<SearchedVisualPage> LoadEndEvent;

    public new event PropertyChangedEventHandler PropertyChanged
    {
        add => base.PropertyChanged += value;
        remove => base.PropertyChanged -= value;
    }
}

public sealed class SearchedVisualPages : ObservableCollection<SearchedVisualPage>
{
    private int _currentPageIndex;

    public SearchedVisualPages()
    {
        PropertyChanged += OnPropertyChanged;
    }

    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        set
        {
            if (value.Equals(_currentPageIndex)) return;
            _currentPageIndex = value;
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(CurrentPageIndex)));
        }
    }

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CurrentPageIndex))
        {
            foreach (var page in Items) page.IsCurrentPage = false;

            if (CurrentPageIndex < Items.Count) Items[CurrentPageIndex].IsCurrentPage = true;
        }
    }

    public new void Add(SearchedVisualPage page)
    {
        base.Add(page);
        page.VisualIndex = Count;
        AddEvent?.Invoke(page);
    }

    public event Action<SearchedVisualPage> AddEvent;

    public new event PropertyChangedEventHandler PropertyChanged
    {
        add => base.PropertyChanged += value;
        remove => base.PropertyChanged -= value;
    }
}