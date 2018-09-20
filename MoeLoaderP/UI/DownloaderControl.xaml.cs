using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MoeLoader.Core;

namespace MoeLoader.UI
{
    public partial class DownloaderControl
    {
        public Settings Settings { get; set; }
        public DownloadItems DownloadItems { get; set; } = new DownloadItems();
        public DownloadItems DownloadingItemsPool { get; set; } = new DownloadItems();
        public DownloadItems WaitForDownloadItemsPool { get; set; } = new DownloadItems();
        public bool IsDownloading { get; set; }

        public DownloaderControl()
        {
            InitializeComponent();
            DownloadItemsListBox.ItemsSource = DownloadItems;
            DownloadItemsListBox.MouseRightButtonUp += DownloadItemsListBoxOnMouseRightButtonUp;
        }

        private void DownloadItemsListBoxOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ContextMenuPopup.IsOpen = true;
            ContextMenuPopupGrid.LargenShowSb().Begin();
        }

        public void Init(Settings settings)
        {
            Settings = settings;
        }

        public void AddDownload(ImageItem item,ImageSource img)
        {
            var downitem = new DownloadItem
            {
                Settings = Settings,
                ImageSource = img,
                ImageItem = item,
                FileName = Path.GetFileName(item.OriginalUrl)
            };
            downitem.DownloadStatusChanged += di => DownloadStatusChanged();
            DownloadItems.Add(downitem);
            //DownloadItemsListBox.Items.Refresh();
            if (DownloadingItemsPool.Count < Settings.MaxOnDownloadingImageCount)
            {
                DownloadingItemsPool.Add(downitem);
            }
            else
            {
                WaitForDownloadItemsPool.Add(downitem);
            }
            DownloadStatusChanged();
            var sv = (ScrollViewer) DownloadItemsListBox.Template.FindName("DownloadListScrollViewer", DownloadItemsListBox);
            sv.ScrollToEnd();
        }
        
        public void StopAll()
        {

        }

        public void DownloadStatusChanged()
        {
            for (var i = 0; i < DownloadingItemsPool.Count; i++)
            {
                var item = DownloadingItemsPool[i];

                switch (item.DownloadStatus)
                {
                    case DownloadStatusEnum.WaitForDownload:
                    {
                        var _ = item.DownloadFileAsync();
                            break;
                    }
                    case DownloadStatusEnum.Downloading:break;
                    default:
                    {
                        DownloadingItemsPool.RemoveAt(i);
                        i--;
                        if (WaitForDownloadItemsPool.Count > 0)
                        {
                            var first = WaitForDownloadItemsPool.First();
                            DownloadingItemsPool.Add(first);
                            WaitForDownloadItemsPool.RemoveAt(0);
                        }break;
                    }
                }
            }
        }
    }
    //public partial class DownloaderControl
    //{
    //    public Settings Settings { get; set; }
    //    public DownloadItems DownloadItems { get; set; } = new DownloadItems();//下载对象
    //    public SiteManager SiteManager { get; set; }

    //    private const string MoeExt = ".moe";
    //    private const string Dlerrtxt = "下载失败下载未完成";

    //    //一个下载任务
    //    private class DownloadTask
    //    {
    //        public string Url { get; set; }
    //        public string SaveLocation { set; get; }
    //        public bool IsStop { set; get; }
    //        public string NeedReferer { get; set; }
    //        public bool NoVerify { get; set; }

    //        /// <summary>
    //        /// 下载任务
    //        /// </summary>
    //        /// <param name="url">目标地址</param>
    //        /// <param name="saveLocation">保存位置</param>
    //        /// <param name="referer">是否需要伪造Referer</param>
    //        public DownloadTask(string url, string saveLocation, string referer, bool noVerify)
    //        {
    //            SaveLocation = saveLocation;
    //            Url = url;
    //            NeedReferer = referer;
    //            NoVerify = noVerify;
    //            IsStop = false;
    //        }
    //    }


    //    //downloadItems的副本，用于快速查找
    //    private Dictionary<string, DownloadItem> downloadItemsDic = new Dictionary<string, DownloadItem>();

    //    /// <summary>
    //    /// 是否正在下载
    //    /// </summary>
    //    public bool IsDownloading { get; set; }

    //    /// <summary>
    //    /// 重试次数
    //    /// </summary>
    //    private int retryCount;

    //    private static string saveLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    //    /// <summary>
    //    /// 下载的保存位置
    //    /// </summary>
    //    public static string SaveLocation { get => saveLocation;
    //        set => saveLocation = value;
    //    }

    //    private int numSaved = 0;
    //    private int numLeft = 0;

    //    //正在下载的链接
    //    private Dictionary<string, DownloadTask> webs = new Dictionary<string, DownloadTask>();

    //    public DownloaderControl()
    //    {
    //        InitializeComponent();
    //    }

    //    public void Init(Settings setings)
    //    {
    //        Settings = setings;

    //        //删除上次临时目录
    //        try
    //        {
    //            if (Directory.Exists(AppRes.AppTempDir)) Directory.Delete(AppRes.AppTempDir, true);
    //        }
    //        catch (Exception ex)
    //        {
    //            Extend.Log("删除临时目录失败", ex);
    //        }

    //        DownloadStatusTextBlock.Text = "当前无下载任务";

    //        DownloadItemsListBox.DataContext = this;
    //    }

    //    public void AddDownloadTask(ImageItem item)
    //    {
    //        var downitem = new DownloadItem(item);
    //    }

    //    /// <summary>
    //    /// 重置重试次数
    //    /// </summary>
    //    public void ResetRetryCount()
    //    {
    //        retryCount = 3;
    //    }

    //    /// <summary>
    //    /// 添加下载任务
    //    /// </summary>
    //    /// <param name="urls"></param>
    //    public void AddDownload(IEnumerable<MiniDownloadItem> items)
    //    {
    //        foreach (MiniDownloadItem item in items)
    //        {
    //            string fileName = item.fileName;
    //            if (fileName == null || fileName.Trim().Length == 0)
    //                fileName = Uri.UnescapeDataString(item.url.Substring(item.url.LastIndexOf('/') + 1));

    //            try
    //            {
    //                DownloadItem itm = new DownloadItem(fileName, item.url, item.host, item.author, item.localName, item.localfileName, item.id, item.noVerify,item.searchWord);

    //                downloadItemsDic.Add(item.url, itm);
    //                DownloadItems.Add(itm);
    //                numLeft++;
    //            }
    //            catch (ArgumentException) { }//duplicate entry
    //        }

    //        if (!IsDownloading)
    //        {
    //            IsDownloading = true;
    //        }

    //        RefreshList();
    //    }



    //    /// <summary>
    //    /// 刷新下载状态
    //    /// </summary>
    //    private void RefreshList()
    //    {
    //        TotalProgressChanged();

    //        //根据numOnce及正在下载的情况生成下载
    //        int downloadingCount = /*NumOnce*/2 - webs.Count;
    //        for (int j = 0; j < downloadingCount; j++)
    //        {
    //            if (numLeft > 0)
    //            {
    //                DownloadItem dlitem = DownloadItems[DownloadItems.Count - numLeft];

    //                string url = dlitem.Url;
    //                string file = dlitem.FileName.Replace("\r\n", "");
    //                string path = GetLocalPath(dlitem);

    //                //检查目录长度
    //                if (path.Length > 248)
    //                {
    //                    DownloadItems[DownloadItems.Count - numLeft].DownloadStatus = DownloadStatusEnum.Failed;
    //                    DownloadItems[DownloadItems.Count - numLeft].Size = "路径太长";
    //                    Extend.Log(url + ": 路径太长");
    //                    j--;
    //                }
    //                else
    //                {
    //                    dlitem.LocalFileName = ReplaceInvalidPathChars(file, path, 0);
    //                    file = dlitem.LocalName = path + dlitem.LocalFileName;

    //                    //检查全路径长度
    //                    if (file.Length > 258)
    //                    {
    //                        DownloadItems[DownloadItems.Count - numLeft].DownloadStatus = DownloadStatusEnum.Failed;
    //                        DownloadItems[DownloadItems.Count - numLeft].Size = "路径太长";
    //                        Extend.Log(url + ": 路径太长");
    //                        j--;
    //                    }
    //                }

    //                if (File.Exists(file))
    //                {
    //                    DownloadItems[DownloadItems.Count - numLeft].DownloadStatus = DownloadStatusEnum.IsExist;
    //                    DownloadItems[DownloadItems.Count - numLeft].Size = "已存在跳过";
    //                    j--;
    //                }
    //                else
    //                {
    //                    if (!Directory.Exists(path))
    //                        Directory.CreateDirectory(path);

    //                    DownloadItems[DownloadItems.Count - numLeft].DownloadStatus = DownloadStatusEnum.Downloading;

    //                    DownloadTask task = new DownloadTask(url, file, IsNeedReferer(url), dlitem.NoVerify);
    //                    webs.Add(url, task);

    //                    //异步下载开始
    //                    System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(Download));
    //                    thread.Start(task);
    //                }

    //                numLeft = numLeft > 0 ? --numLeft : 0;
    //            }
    //            else break;
    //        }
    //        RefreshStatus();
    //    }


    //    /// <summary>
    //    /// 下载，另一线程
    //    /// </summary>
    //    /// <param name="o"></param>
    //    private void Download(object o)
    //    {
    //        DownloadTask task = (DownloadTask)o;
    //        FileStream fs = null;
    //        Stream str = null;
    //        MoeSession sc = new MoeSession();
    //        System.Net.WebResponse res = null;
    //        double downed = 0;
    //        DownloadItem item = downloadItemsDic[task.Url];

    //        try
    //        {
    //            res = sc.GetWebResponse(
    //                task.Url,
    //                Settings.Proxy,
    //                task.NeedReferer
    //                );

    //            /////////开始写入文件
    //            str = res.GetResponseStream();
    //            byte[] bytes = new byte[5120];
    //            fs = new FileStream(task.SaveLocation + MoeExt, FileMode.Create);

    //            int bytesReceived = 0;
    //            DateTime last = DateTime.Now;
    //            int osize = str.Read(bytes, 0, bytes.Length);
    //            downed = osize;
    //            while (!task.IsStop && osize > 0)
    //            {
    //                fs.Write(bytes, 0, osize);
    //                bytesReceived += osize;
    //                DateTime now = DateTime.Now;
    //                double speed = -1;
    //                if ((now - last).TotalSeconds > 0.6)
    //                {
    //                    speed = downed / (now - last).TotalSeconds / 1024.0;
    //                    downed = 0;
    //                    last = now;
    //                }
    //                Dispatcher.Invoke(new DownloadHandler(web_DownloadProgressChanged),
    //                    res.ContentLength, bytesReceived / (double)res.ContentLength * 100.0, task.Url, speed);
    //                osize = str.Read(bytes, 0, bytes.Length);
    //                downed += osize;
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            //Dispatcher.Invoke(new UIdelegate(delegate(object sender) { StopLoadImg(re.Key, re.Value); }), "");
    //            task.IsStop = true;
    //            Dispatcher.Invoke(new Action(delegate ()
    //            {
    //                //下载失败
    //                if (downloadItemsDic.ContainsKey(task.Url))
    //                {
    //                    item.DownloadStatus = DownloadStatusEnum.Failed;
    //                    item.Size = "下载失败";
    //                    Extend.Log(task.Url);
    //                    Extend.Log(task.SaveLocation);
    //                    Extend.Log(ex.Message + "\r\n");

    //                    try
    //                    {
    //                        if (fs != null)
    //                            fs.Close();
    //                        if (str != null)
    //                            str.Close();
    //                        if (res != null)
    //                            res.Close();

    //                        File.Delete(task.SaveLocation + MoeExt);
    //                        DelDLItemNullDirector(item);
    //                    }
    //                    catch { }
    //                }
    //            }));
    //        }
    //        finally
    //        {
    //            if (fs != null)
    //                fs.Close();
    //            if (str != null)
    //                str.Close();
    //            if (res != null)
    //                res.Close();
    //        }

    //        if (task.IsStop)
    //        {
    //            //任务被取消
    //            Dispatcher.Invoke(new Action(delegate ()
    //            {
    //                if (downloadItemsDic.ContainsKey(task.Url))
    //                {
    //                    if (!Dlerrtxt.Contains(item.Size))
    //                    {
    //                        item.DownloadStatus = DownloadStatusEnum.Cancel;
    //                    }
    //                }
    //            }));

    //            try
    //            {
    //                File.Delete(task.SaveLocation + MoeExt);
    //                DelDLItemNullDirector(item);
    //            }
    //            catch { }
    //        }
    //        else
    //        {
    //            //下载成功完成
    //            Dispatcher.Invoke(new Action(delegate ()
    //            {
    //                try
    //                {
    //                    //DownloadTask task1 = obj as DownloadTask;

    //                    //判断完整性
    //                    if (!item.NoVerify && 100 - item.Progress > 0.001)
    //                    {
    //                        task.IsStop = true;
    //                        item.DownloadStatus = DownloadStatusEnum.Failed;
    //                        item.Size = "下载未完成";
    //                        try
    //                        {
    //                            File.Delete(task.SaveLocation + MoeExt);
    //                            DelDLItemNullDirector(item);
    //                        }
    //                        catch { }
    //                    }
    //                    else
    //                    {
    //                        //修改后缀名
    //                        File.Move(task.SaveLocation + MoeExt, task.SaveLocation);

    //                        item.Progress = 100.0;
    //                        item.DownloadStatus = DownloadStatusEnum.Success;
    //                        //downloadItemsDic[task.Url].Size = (downed > 1048576
    //                        //? (downed / 1048576.0).ToString("0.00MB")
    //                        //: (downed / 1024.0).ToString("0.00KB"));
    //                        numSaved++;
    //                    }
    //                }
    //                catch { }
    //            }));
    //        }

    //        //下载结束
    //        Dispatcher.Invoke(new Action(delegate ()
    //        {
    //            webs.Remove(task.Url);
    //            RefreshList();
    //        }));
    //    }

    //    /// <summary>
    //    /// 更新状态显示
    //    /// </summary>
    //    private void RefreshStatus()
    //    {
    //        if (webs.Count > 0)
    //        {
    //            DownloadStatusTextBlock.Text = "已保存 " + numSaved + " 剩余 " + numLeft + " 正在下载 " + webs.Count;
    //        }
    //        else
    //        {
    //            IsDownloading = false;
    //            DownloadStatusTextBlock.Text = "已保存 " + numSaved + " 剩余 " + numLeft + " 下载完毕 ";
    //            if (retryCount > 0)
    //            {
    //                retryCount--;
    //                ExecuteDownloadListTask(DLWorkMode.AutoRetryAll);
    //            }
    //        }

    //        if (DownloadItems.Count == 0)
    //            blkTip.Visibility = Visibility.Visible;
    //        else
    //            blkTip.Visibility = Visibility.Collapsed;
    //    }

    //    /// <summary>
    //    /// 下载进度发生改变
    //    /// </summary>
    //    /// <param name="total"></param>
    //    /// <param name="percent"></param>
    //    /// <param name="url"></param>
    //    void web_DownloadProgressChanged(long total, double percent, string url, double speed)
    //    {
    //        try
    //        {
    //            string size = total > 1048576 ? (total / 1048576.0).ToString("0.00MB") : (total / 1024.0).ToString("0.00KB");
    //            downloadItemsDic[url].Size = size;
    //            downloadItemsDic[url].Progress = percent > 100 ? 100 : percent;
    //            if (speed > 0)
    //                downloadItemsDic[url].SetSpeed(speed);
    //        }
    //        catch { }
    //    }

    //    /// <summary>
    //    /// 总下载进度，根据下载完成的图片数量计算
    //    /// </summary>
    //    private void TotalProgressChanged()
    //    {
    //        if (DownloadItems.Count > 0)
    //        {
    //            double percent = (DownloadItems.Count - numLeft - webs.Count) / (double)DownloadItems.Count * 100.0;

    //            //Win7TaskBar.ChangeProcessValue(MainWindow.Hwnd, (uint)percent);

    //            if (Math.Abs(percent - 100.0) < 0.001)
    //            {
    //                //Win7TaskBar.StopProcess(MainWindow.Hwnd);
    //                //if (GlassHelper.GetForegroundWindow() != MainWindow.Hwnd)
    //                //{
    //                //    //System.Media.SystemSounds.Beep.Play();
    //                //    GlassHelper.FlashWindow(MainWindow.Hwnd, true);
    //                //}

    //                #region 关机
    //                if (itmAutoClose.IsChecked)
    //                {
    //                    //关机
    //                    System.Timers.Timer timer = new System.Timers.Timer()
    //                    {
    //                        //20秒后关闭
    //                        Interval = 20000,
    //                        Enabled = false,
    //                        AutoReset = false
    //                    };
    //                    //todo
    //                    //timer.Elapsed += delegate { GlassHelper.ExitWindows(GlassHelper.ShutdownType.PowerOff); };
    //                    timer.Start();

    //                    if (MessageBox.Show("系统将于20秒后自动关闭，若要取消请点击确定", AppRes.ProgramName, MessageBoxButton.OK, MessageBoxImage.Information) == MessageBoxResult.OK)
    //                    {
    //                        timer.Stop();
    //                    }
    //                }
    //                #endregion
    //            }
    //        }
    //        else
    //        {
    //            //Win7TaskBar.ChangeProcessValue(MainWindow.Hwnd, 0);
    //            //Win7TaskBar.StopProcess(MainWindow.Hwnd);
    //        }
    //    }

    //    /// <summary>
    //    /// 去掉文件名中的无效字符,如 \ / : * ? " < > | 
    //    /// </summary>
    //    /// <param name="file">待处理的文件名</param>
    //    /// <param name="replace">替换字符</param>
    //    /// <returns>处理后的文件名</returns>
    //    public static string ReplaceInvalidPathChars(string file, string replace)
    //    {
    //        if (file.IndexOf('?', (file.LastIndexOf('.') < 1 ? file.Length : file.LastIndexOf('.'))) > 0)
    //        {
    //            //adfadsf.jpg?adfsdf   remove trailing ?param
    //            file = file.Substring(0, file.IndexOf('?'));
    //        }

    //        foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
    //            file = file.Replace(rInvalidChar.ToString(), replace);
    //        return file;
    //    }
    //    /// <summary>
    //    /// 去掉文件名中的无效字符,如 \ / : * ? " < > | 
    //    /// </summary>
    //    public static string ReplaceInvalidPathChars(string file)
    //    {
    //        return ReplaceInvalidPathChars(file, "_");
    //    }
    //    /// <summary>
    //    /// 去掉文件名中无效字符的同时裁剪过长文件名
    //    /// </summary>
    //    /// <param name="file">文件名</param>
    //    /// <param name="path">所在路径</param>
    //    /// <param name="any">任何数</param>
    //    /// <returns></returns>
    //    public static string ReplaceInvalidPathChars(string file, string path, int any)
    //    {
    //        if (path.Length + file.Length > 258 && file.Contains("<!<"))
    //        {
    //            string last = file.Substring(file.LastIndexOf("<!<"));
    //            file = file.Substring(0, 258 - last.Length - path.Length - last.Length) + last;
    //        }
    //        file = file.Replace("<!<", "");
    //        return ReplaceInvalidPathChars(file);
    //    }

    //    /// <summary>
    //    /// 复制地址
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void itmCopy_Click(object sender, RoutedEventArgs e)
    //    {
    //        DownloadItem i = (DownloadItem)DownloadItemsListBox.SelectedItem;
    //        if (i == null) return;
    //        string text = i.Url;
    //        try
    //        {
    //            Clipboard.SetText(text);
    //        }
    //        catch { }
    //    }

    //    private void ExecuteDownloadListTask(DLWorkMode dlworkmode)
    //    {
    //        int selectcs, delitemfile = 0;
    //        List<DownloadItem> selected = new List<DownloadItem>();
    //        if (dlworkmode == DLWorkMode.RetryAll || dlworkmode == DLWorkMode.AutoRetryAll
    //            || dlworkmode == DLWorkMode.StopAll || dlworkmode == DLWorkMode.RemoveAll)
    //        {
    //            foreach (object o in DownloadItemsListBox.Items)
    //            {
    //                //转存集合，防止selected改变
    //                DownloadItem item = (DownloadItem)o;
    //                selected.Add(item);
    //            }
    //        }
    //        else
    //        {
    //            foreach (object o in DownloadItemsListBox.SelectedItems)
    //            {
    //                DownloadItem item = (DownloadItem)o;
    //                selected.Add(item);
    //            }
    //        }
    //        selectcs = selected.Count;

    //        foreach (DownloadItem item in selected)
    //        {
    //            switch (dlworkmode)
    //            {
    //                case DLWorkMode.Retry:
    //                case DLWorkMode.RetryAll:
    //                case DLWorkMode.AutoRetryAll:
    //                    if (item.DownloadStatus == DownloadStatusEnum.Failed || item.DownloadStatus == DownloadStatusEnum.Cancel || item.DownloadStatus == DownloadStatusEnum.IsExist)
    //                    {
    //                        if (dlworkmode == DLWorkMode.AutoRetryAll && item.DownloadStatus == DownloadStatusEnum.Cancel) break;
    //                        numLeft = numLeft > selectcs ? selectcs : numLeft;
    //                        DownloadItems.Remove(item);
    //                        downloadItemsDic.Remove(item.Url);
    //                        AddDownload(new MiniDownloadItem[] {
    //                            new MiniDownloadItem(item.FileName, item.Url, item.Host, item.Author, item.LocalName, item.LocalFileName,
    //                            item.Id, item.NoVerify)
    //                        });
    //                    }
    //                    break;

    //                case DLWorkMode.Stop:
    //                case DLWorkMode.StopAll:
    //                    if (item.DownloadStatus == DownloadStatusEnum.Downloading || item.DownloadStatus == DownloadStatusEnum.WaitForDownload)
    //                    {
    //                        if (webs.ContainsKey(item.Url))
    //                        {
    //                            webs[item.Url].IsStop = true;
    //                            webs.Remove(item.Url);
    //                        }
    //                        else
    //                            numLeft = numLeft > 0 ? --numLeft : 0;

    //                        if (dlworkmode == DLWorkMode.StopAll)
    //                        {
    //                            numLeft = 0;
    //                        }
    //                        item.DownloadStatus = DownloadStatusEnum.Cancel;
    //                        item.Size = "已取消";

    //                        try
    //                        {
    //                            File.Delete(item.LocalFileName + MoeExt);
    //                            DelDLItemNullDirector(item);
    //                        }
    //                        catch { }
    //                    }
    //                    break;

    //                case DLWorkMode.Del:
    //                case DLWorkMode.Remove:
    //                case DLWorkMode.RemoveAll:
    //                    if (dlworkmode == DLWorkMode.Del && delitemfile < 1)
    //                    {
    //                        if (MessageBox.Show("QwQ 真的要把任务和文件一起删除么？",
    //                            AppRes.ProgramName,
    //                            MessageBoxButton.YesNo,
    //                            MessageBoxImage.Warning) == MessageBoxResult.No)
    //                        { delitemfile = 2; }
    //                        else
    //                        { delitemfile = 1; }
    //                    }
    //                    if (delitemfile > 1)
    //                        break;

    //                    if (item.DownloadStatus == DownloadStatusEnum.Downloading)
    //                    {
    //                        if (webs.ContainsKey(item.Url))
    //                        {
    //                            webs[item.Url].IsStop = true;
    //                            webs.Remove(item.Url);
    //                        }
    //                    }
    //                    else if (item.DownloadStatus == DownloadStatusEnum.Success || item.DownloadStatus == DownloadStatusEnum.IsExist)
    //                        numSaved = numSaved > 0 ? --numSaved : 0;
    //                    else if (item.DownloadStatus == DownloadStatusEnum.WaitForDownload || item.DownloadStatus == DownloadStatusEnum.Cancel)
    //                        numLeft = numLeft > 0 ? --numLeft : 0;

    //                    DownloadItems.Remove(item);
    //                    downloadItemsDic.Remove(item.Url);

    //                    //删除文件
    //                    string fname = item.LocalName;
    //                    if (dlworkmode == DLWorkMode.Del)
    //                    {
    //                        if (File.Exists(fname))
    //                        {
    //                            File.Delete(fname);
    //                            DelDLItemNullDirector(item);
    //                        }
    //                    }
    //                    break;
    //            }
    //        }
    //        if (dlworkmode == DLWorkMode.Stop || dlworkmode == DLWorkMode.Remove)
    //        {
    //            RefreshList();
    //        }
    //        if (dlworkmode == DLWorkMode.Remove)
    //        {
    //            RefreshStatus();
    //        }
    //    }

    //    /// <summary>
    //    /// 删除空目录
    //    /// </summary>
    //    /// <param name="item">下载项</param>
    //    private void DelDLItemNullDirector(DownloadItem item)
    //    {
    //        try
    //        {
    //            new Thread(new ThreadStart(delegate
    //            {
    //                string lpath = GetLocalPath(item);
    //                DirectoryInfo di = new DirectoryInfo(lpath);

    //                while (Directory.Exists(lpath) && di.GetFiles().Length + di.GetDirectories().Length < 1 && lpath.Contains(saveLocation))
    //                {
    //                    Directory.Delete(lpath);
    //                    Thread.Sleep(666);
    //                    int last = lpath.LastIndexOf("\\", lpath.Length - 2);
    //                    lpath = lpath.Substring(0, last > 0 ? last : lpath.Length);
    //                    di = new DirectoryInfo(lpath);
    //                }
    //            })).Start();
    //        }
    //        catch { }
    //    }
    //    //================================================================================
    //    /// <summary>
    //    /// 重试
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void itmRetry_Click(object sender, RoutedEventArgs e)
    //    {
    //        ResetRetryCount();
    //        ExecuteDownloadListTask(DLWorkMode.Retry);
    //    }

    //    /// <summary>
    //    /// 停止某个任务
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void itmStop_Click(object sender, RoutedEventArgs e)
    //    {
    //        ExecuteDownloadListTask(DLWorkMode.Stop);
    //    }

    //    /// <summary>
    //    /// 移除某个任务
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void itmDelete_Click(object sender, RoutedEventArgs e)
    //    {
    //        ExecuteDownloadListTask(DLWorkMode.Remove);
    //    }

    //    /// <summary>
    //    /// 停止所有下载
    //    /// </summary>
    //    public void StopAll()
    //    {
    //        DownloadItems.Clear();
    //        downloadItemsDic.Clear();
    //        foreach (DownloadTask item in webs.Values)
    //        {
    //            item.IsStop = true;
    //        }
    //    }

    //    /// <summary>
    //    /// 清空已成功任务
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void itmClearDled_Click(object sender, RoutedEventArgs e)
    //    {
    //        int i = 0;
    //        while (true)
    //        {
    //            if (i >= DownloadItems.Count) break;
    //            DownloadItem item = DownloadItems[i];
    //            if (item.DownloadStatus == DownloadStatusEnum.Success)
    //            {
    //                DownloadItems.RemoveAt(i);
    //                downloadItemsDic.Remove(item.Url);
    //            }
    //            else
    //            {
    //                i++;
    //            }
    //        }
    //        numSaved = 0;
    //        RefreshStatus();
    //    }

    //    /// <summary>
    //    /// 右键菜单即将打开
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void menu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    //    {
    //        if (DownloadItemsListBox.SelectedItems == null || DownloadItemsListBox.SelectedItems.Count == 0)
    //        {
    //            itmLst.IsEnabled =
    //                itmCopy.IsEnabled =
    //                itmRetry.IsEnabled =
    //                itmStop.IsEnabled =
    //                itmDelete.IsEnabled =
    //                itmLstPic.IsEnabled =
    //                itmDeleteFile.IsEnabled = false;
    //        }
    //        else
    //        {
    //            itmCopy.IsEnabled = DownloadItemsListBox.SelectedItems.Count == 1;
    //            itmLst.IsEnabled =
    //                itmRetry.IsEnabled =
    //                itmStop.IsEnabled =
    //                itmDelete.IsEnabled =
    //                itmLstPic.IsEnabled =
    //                itmDeleteFile.IsEnabled = true;
    //        }

    //        itmRetryAll.IsEnabled =
    //            itmStopAll.IsEnabled =
    //            itmDeleteAll.IsEnabled = DownloadItemsListBox.Items.Count > 0;
    //    }

    //    /// <summary>
    //    /// 文件拖拽事件
    //    /// </summary>
    //    public void UserControl_DragEnter(object sender, DragEventArgs e)
    //    {
    //        try
    //        {
    //            string fileName = ((string[])(e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop)))[0];
    //            if (fileName != null && Path.GetExtension(fileName).ToLower() == ".lst")
    //            {
    //                e.Effects = DragDropEffects.Copy;
    //            }
    //            else e.Effects = DragDropEffects.None;
    //        }
    //        catch (Exception) { e.Effects = DragDropEffects.None; }
    //    }

    //    /// <summary>
    //    /// 从lst文件添加下载
    //    /// </summary>
    //    /// <param name="fileName"></param>
    //    public void DownLoadFromFile(string fileName)
    //    {
    //        if (fileName != null && Path.GetExtension(fileName).ToLower() == ".lst")
    //        {
    //            List<string> lines = new List<string>(File.ReadAllLines(fileName));
    //            List<MiniDownloadItem> items = new List<MiniDownloadItem>();
    //            MiniDownloadItem di = new MiniDownloadItem();
    //            //提取地址
    //            foreach (string line in lines)
    //            {
    //                //移除空行
    //                if (line.Trim().Length == 0) continue;
    //                string[] parts = line.Split('|');

    //                //url
    //                if (parts.Length > 0 && parts[0].Trim().Length < 1)
    //                    continue;
    //                else
    //                    di.url = parts[0];

    //                //文件名
    //                if (parts.Length > 1 && parts[1].Trim().Length > 0)
    //                {
    //                    string ext = di.url.Substring(di.url.LastIndexOf('.'), di.url.Length - di.url.LastIndexOf('.'));
    //                    di.fileName = parts[1].EndsWith(ext) ? parts[1] : parts[1] + ext;
    //                }

    //                //域名
    //                if (parts.Length > 2 && parts[2].Trim().Length > 0)
    //                    di.host = parts[2];

    //                //上传者
    //                if (parts.Length > 3 && parts[3].Trim().Length > 0)
    //                    di.author = parts[3];

    //                //ID
    //                if (parts.Length > 4 && parts[4].Trim().Length > 0)
    //                {
    //                    try
    //                    {
    //                        di.id = int.Parse(parts[4]);
    //                    }
    //                    catch { }
    //                }

    //                //免文件校验
    //                if (parts.Length > 5 && parts[5].Trim().Length > 0)
    //                    di.noVerify = parts[5].Contains('v');

    //                //搜索时关键词
    //                if (parts.Length > 6 && parts[6].Trim().Length > 0)
    //                    di.searchWord = parts[6];

    //                items.Add(di);
    //            }

    //            //添加至下载列表
    //            AddDownload(items);
    //        }

    //        ResetRetryCount();
    //    }

    //    /// <summary>
    //    /// 文件被拖入
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    public void UserControl_Drop(object sender, DragEventArgs e)
    //    {
    //        try
    //        {
    //            string fileName = ((string[])(e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop)))[0];
    //            DownLoadFromFile(fileName);
    //        }
    //        catch (Exception ex)
    //        {
    //            MessageBox.Show("从文件添加下载失败\r\n" + ex.Message, AppRes.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
    //        }
    //    }

    //    /// <summary>
    //    /// 打开保存目录
    //    /// </summary>
    //    private void itmOpenSave_Click(object sender, RoutedEventArgs e)
    //    {
    //        try
    //        {
    //            DownloadItem dlItem = (DownloadItem)DownloadItemsListBox.SelectedItem;

    //            if (File.Exists(dlItem.LocalName))
    //            {
    //                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo("explorer.exe");
    //                psi.Arguments = "/e,/select," + dlItem.LocalName;
    //                System.Diagnostics.Process.Start(psi);
    //            }
    //            else
    //            {
    //                System.Diagnostics.Process.Start(GetLocalPath(dlItem));
    //            }

    //        }
    //        catch
    //        {
    //            System.Diagnostics.Process.Start(saveLocation);
    //        }
    //    }

    //    /// <summary>

    //    /// 双击一个表项执行的操作
    //    /// </summary>
    //    private void grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    //    {
    //        if (e.ClickCount == 2)
    //        {
    //            DownloadItem dcitem = (DownloadItem)DownloadItemsListBox.SelectedItem;
    //            switch (dcitem.DownloadStatus)
    //            {
    //                case DownloadStatusEnum.Success:
    //                case DownloadStatusEnum.IsExist:
    //                    if (File.Exists(dcitem.LocalName))
    //                        System.Diagnostics.Process.Start(dcitem.LocalName);
    //                    else
    //                        MessageBox.Show("无法打开文件！可能已被更名、删除或移动", AppRes.ProgramName, MessageBoxButton.OK, MessageBoxImage.Warning);
    //                    break;
    //                case DownloadStatusEnum.Cancel:
    //                case DownloadStatusEnum.Failed:
    //                    ExecuteDownloadListTask(DLWorkMode.Retry);
    //                    break;
    //                default:
    //                    ExecuteDownloadListTask(DLWorkMode.Stop);
    //                    break;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// 清除所有失败任务
    //    /// </summary>
    //    private void itmClearDled_Click_1(object sender, RoutedEventArgs e)
    //    {
    //        int i = 0;
    //        while (true)
    //        {
    //            if (i >= DownloadItems.Count) break;
    //            DownloadItem item = DownloadItems[i];
    //            if (item.DownloadStatus == DownloadStatusEnum.Failed)
    //            {
    //                DownloadItems.RemoveAt(i);
    //                downloadItemsDic.Remove(item.Url);
    //            }
    //            else
    //            {
    //                i++;
    //            }
    //        }
    //        RefreshStatus();
    //    }

    //    /// <summary>
    //    /// 仅清除已取消和已存在的任务
    //    /// </summary>
    //    private void itmClearDled_Click_2(object sender, RoutedEventArgs e)
    //    {
    //        int i = 0;
    //        while (true)
    //        {
    //            if (i >= DownloadItems.Count) break;
    //            DownloadItem item = DownloadItems[i];

    //            if (item.DownloadStatus == DownloadStatusEnum.Cancel || item.DownloadStatus == DownloadStatusEnum.IsExist)
    //            {
    //                DownloadItems.RemoveAt(i);
    //                downloadItemsDic.Remove(item.Url);
    //            }
    //            else
    //            {
    //                i++;
    //            }
    //        }
    //        RefreshStatus();
    //    }

    //    /// <summary>
    //    /// 全选
    //    /// </summary>
    //    private void itmSelAll_Click(object sender, RoutedEventArgs e)
    //    {
    //        DownloadItemsListBox.SelectAll();
    //    }

    //    /// <summary>
    //    /// 重试所有任务
    //    /// </summary>
    //    private void itmRetryAll_Click(object sender, RoutedEventArgs e)
    //    {
    //        ResetRetryCount();
    //        ExecuteDownloadListTask(DLWorkMode.RetryAll);
    //    }

    //    /// <summary>
    //    /// 停止所有任务
    //    /// </summary>
    //    private void itmStopAll_Click(object sender, RoutedEventArgs e)
    //    {
    //        ExecuteDownloadListTask(DLWorkMode.StopAll);
    //    }

    //    /// <summary>
    //    /// 移除所有任务
    //    /// </summary>
    //    private void itmDeleteAll_Click(object sender, RoutedEventArgs e)
    //    {
    //        ExecuteDownloadListTask(DLWorkMode.RemoveAll);
    //    }

    //    /// <summary>
    //    /// 任务和文件一起删除
    //    /// </summary>
    //    private void itmDeleteFile_Click(object sender, RoutedEventArgs e)
    //    {
    //        ExecuteDownloadListTask(DLWorkMode.Del);
    //    }


    //    /// <summary>
    //    /// 当做下载列表快捷键
    //    /// </summary>
    //    /// <param name="sender"></param>
    //    /// <param name="e"></param>
    //    private void dlList_KeyDown(object sender, KeyEventArgs e)
    //    {
    //        if (Keyboard.IsKeyDown(Key.LeftCtrl))
    //        {
    //            int dlselect = DownloadItemsListBox.SelectedItems.Count;

    //            if (e.Key == Key.U)
    //            {   //反选
    //                itmSelInvert_Click(null, null);
    //            }
    //            else if (dlselect > 0)
    //            {
    //                if (e.Key == Key.L)
    //                {//导出下载列表
    //                }
    //                else if (e.Key == Key.C && dlselect == 1)
    //                {   //复制地址
    //                    itmCopy_Click(null, null);
    //                }
    //                else if (e.Key == Key.Z)
    //                {
    //                    //导出图片下载链接列表
    //                }
    //                else if (e.Key == Key.R)
    //                {    //重试
    //                    itmRetry_Click(null, null);
    //                }
    //                else if (e.Key == Key.S)
    //                {    //停止
    //                    itmStop_Click(null, null);
    //                }
    //                else if (e.Key == Key.D)
    //                {    //移除
    //                    itmDelete_Click(null, null);
    //                }
    //                else if (e.Key == Key.X)
    //                {    //和文件一起删除
    //                    itmDeleteFile_Click(null, null);
    //                }
    //            }
    //            if (e.Key == Key.G)
    //            {   //停止所有任务
    //                itmStopAll_Click(null, null);
    //            }
    //            else if (e.Key == Key.V)
    //            {   //清空所有任务
    //                itmDeleteAll_Click(null, null);
    //            }
    //            else if (e.Key == Key.T)
    //            {   //重试所有任务
    //                itmRetryAll_Click(null, null);
    //            }
    //        }
    //    }



    //    /// <summary>
    //    /// 构建文件名 generate file name
    //    /// </summary>
    //    private string GenFileName(ImageItem img, string url)
    //    {
    //        var site = img.MoeSite;
    //        //namePatter
    //        var file = Settings.SaveFileNameFormat;
    //        if (string.IsNullOrWhiteSpace(file))
    //            return Path.GetFileName(url);

    //        //%site站点 %id编号 %tag标签 %desc描述 %author作者 %date图片时间 %imgid[2]图册中图片编号[补n个零]
    //        file = file.Replace("%site", site.ShortName);
    //        file = file.Replace("%id", img.Id.ToString());
    //        file = file.Replace("%tag", DownloaderControl.ReplaceInvalidPathChars(img.Tags.Replace("\r\n", "")));
    //        file = file.Replace("%desc", DownloaderControl.ReplaceInvalidPathChars(img.Desc.Replace("\r\n", "")));
    //        file = file.Replace("%author", DownloaderControl.ReplaceInvalidPathChars(img.Author.Replace("\r\n", "")));
    //        file = file.Replace("%date", FormatFileDateTime(img.Date));
    //        #region 图册页数格式化
    //        try
    //        {
    //            var reg = new Regex(@"(?<all>%imgp\[(?<zf>[0-9]+)\])");
    //            var mc = reg.Matches(file);
    //            Match result;
    //            var imgpPatter = "";
    //            var zerofill = 0;
    //            var resc = mc.Count;

    //            for (var i = 0; i < resc; i++)
    //            {
    //                result = mc[i];
    //                imgpPatter = result.Groups["all"].ToString();
    //                zerofill = int.Parse(result.Groups["zf"].ToString());
    //                if (string.IsNullOrWhiteSpace(img.ImgP))
    //                    file = file.Replace(imgpPatter, "0".PadLeft(zerofill, '0'));
    //                else
    //                    file = file.Replace(imgpPatter, img.ImgP.PadLeft(zerofill, '0'));
    //            }

    //            if (resc < 1)
    //            {
    //                //如果图册有数量就强制加序号
    //                if (int.Parse(img.ImgP) > 0)
    //                    file += img.ImgP.PadLeft(5, '0');
    //            }
    //        }
    //        catch { }
    //        #endregion

    //        return file;
    //    }

    //    /// <summary>
    //    /// 格式化杂乱字符串为适用于文件名的时间格式
    //    /// </summary>
    //    /// <param name="timeStr">时间字符串</param>
    //    /// <returns></returns>
    //    private string FormatFileDateTime(string timeStr)
    //    {
    //        if (timeStr.Trim() == "") return timeStr;
    //        //空格切分日期时间
    //        timeStr = Regex.Replace(timeStr, @"\s", ">");
    //        //替换英文月份
    //        var emonth = "Jan,Feb,Mar,Apr,May,Jun,Jul,Aug,Sept,Oct,Nov,Dec";
    //        var esmonth = "January,February,March,April,May,June,July,August,September,October,November,December";
    //        for (var j = 0; j < 2; j++)
    //        {
    //            var smonth = Regex.Split(j > 0 ? emonth : esmonth, @",");
    //            var smos = smonth.Length;
    //            for (var i = 0; i < smos; i++)
    //            {
    //                if (timeStr.ToLower().Contains(smonth[i]))
    //                {
    //                    timeStr = timeStr.Replace(smonth[i], i + 1 + "");
    //                }
    //            }
    //        }
    //        //格式交换
    //        var mca = Regex.Match(timeStr, @">(?<num>\d{4})$");
    //        var mcb = Regex.Match(timeStr, @">(?<num>\d{4})>");
    //        var yeara = mca.Groups["num"].ToString();
    //        var yearb = mcb.Groups["num"].ToString();
    //        var month = "";
    //        if (yeara != "")
    //        {
    //            mca = Regex.Match(timeStr, @"(?<num>\d+)>");
    //            month = mca.Groups["num"].ToString();
    //            timeStr = yeara + new Regex(@"(\d+)>").Replace(timeStr, month, 1);
    //            timeStr = Regex.Replace(timeStr, yeara + @".*?>" + month, yeara + "<" + month);
    //        }
    //        else if (yearb != "")
    //        {
    //            mcb = Regex.Match(timeStr, @"(?<num>\d+>\d+)");
    //            month = mcb.Groups["num"].ToString();
    //            month = Regex.Replace(month, @">", "<");
    //            timeStr = Regex.Replace(timeStr, yearb + @">", yearb + "<" + month + ">");
    //            timeStr = Regex.Replace(timeStr, @".*?>" + yearb, yearb);
    //        }
    //        //杂字过滤
    //        timeStr = Regex.Replace(timeStr, @"[^\d|>]", "<");
    //        //取时间区域
    //        timeStr = Regex.Match(timeStr, @"\d[\d|<|>]+[<|>]+\d+").ToString();
    //        //缩减重复字符
    //        timeStr = Regex.Replace(timeStr, "<+", "<");
    //        timeStr = Regex.Replace(timeStr, ">+", ">");
    //        timeStr = Regex.Replace(timeStr, "[<|>]+[<|>]+", ">");

    //        if (timeStr.Contains(">"))
    //        {
    //            var strs = Regex.Split(timeStr, ">");
    //            timeStr = Regex.Replace(strs[0], "<", "-");
    //            timeStr = timeStr + " " + Regex.Replace(strs[1], "<", "：");
    //        }
    //        else
    //        {
    //            timeStr = Regex.Replace(timeStr, "<", "：");
    //        }
    //        return timeStr;
    //    }
    //}


    //    public ImageSource CreateImageSrc(Stream str)
    //    {
    //    ImageSource imgS = null;
    //    Dispatcher.Invoke(delegate
    //    {
    //        imgS = BitmapDecoder.Create(str, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default).Frames[0];
    //    });
    //    return imgS;
    //}
}