using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using MoeLoaderP.Core;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Wpf.ControlParts;

/// <summary>
/// MoeContextMenuControl.xaml 的交互逻辑
/// </summary>
public partial class MoeContextMenuControl
{
    public WrapPanel ImageItemsWrapPanel { get; set; }
    public Popup ContextMenuPopup { get; set; }
    public ObservableCollection<MoeItemControl> SelectedImageControls { get; set; }
    public MoeContextMenuControl()
    {
        InitializeComponent();
    }
    
    public void InitContextMenu(WrapPanel imagesWrapPanel, Popup contextMenuPopup, ObservableCollection<MoeItemControl> selectedImageControls)
    {
        ImageItemsWrapPanel = imagesWrapPanel;
        ContextMenuPopup = contextMenuPopup;
        SelectedImageControls = selectedImageControls;

        ContextSelectAllButton.Click += ContextSelectAllButtonOnClick;
        ContextSelectNoneButton.Click += ContextSelectNoneButtonOnClick;
        ContextSelectReverseButton.Click += ContextSelectReverseButtonOnClick;
        
    }
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

    public void ContextSelectAllButtonOnClick(object sender, RoutedEventArgs e)
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

        var items = SelectedImageControls.Where(ctrl => ctrl.RefreshButton.Visibility == Visibility.Visible).ToList();
        if (items.Any())
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
        if (item.Tags.Count > 0)
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
}