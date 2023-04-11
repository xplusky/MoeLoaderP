using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using HtmlAgilityPack;
using MoeLoaderP.Core.Sites;

namespace MoeLoaderP.Core;

/// <summary>
///     扩展方法集合
/// </summary>
public static class Ex
{
    private static string _logListOriginalString;
    private static string _logItemString;

    public static int GetXIntValue(this XElement el, string xpath)
    {
        return (el.XPathSelectElement(xpath)?.Value ?? "0").ToInt();
    }
    public static string GetXStringValue(this XElement el, string xpath)
    {
        return el.XPathSelectElement(xpath)?.Value;
    }

    public enum DateFormatEnum
    {
        /// <summary>
        ///     自动
        /// </summary>
        Auto,

        /// <summary>
        ///     总秒数
        /// </summary>
        From1970TotalSeconds
    }

    
    public enum MessagePos
    {
        InfoBar,
        Popup,
        Searching,
        Window,
        Page
    }

    public static ObservableCollection<string> LogCollection { get; set; } = new();

    public static string LogListOriginalString
    {
        get => _logListOriginalString;
        set
        {
            _logListOriginalString = value;
            if(value!=null) LogListOriginalStringAction?.Invoke(value);
        }
    }

    public static string LogItemString
    {
        get => _logItemString;
        set
        {
            _logItemString = value; LogItemStringAction?.Invoke(value);
        }
    }

    public static Action<string> LogAction { get; set; }

    public static event Action<string> LogListOriginalStringAction;
    public static event Action<string> LogItemStringAction;

    public static dynamic GetValue(this HtmlNode rootNode, CustomXpath xpath)
    {
        if (xpath == null) return null;
        if (rootNode == null) return null;
        var isMulti = xpath.IsMultiValues;
        if (isMulti)
        {
            var nodes = rootNode.SelectNodes(xpath.Path);
            if (nodes == null && xpath.PathR2 != null) nodes = rootNode.SelectNodes(xpath.PathR2);
            if (nodes == null) return null;

            var list = new List<string>();
            switch (xpath.Mode)
            {
                case nameof(CustomXpathMode.Attribute):
                    list.AddRange(nodes.Select(hnode => hnode.Attributes[xpath.Attribute]?.Value));
                    break;
                case nameof(CustomXpathMode.InnerText):
                    list.AddRange(nodes.Select(hnode => hnode.InnerText));
                    break;
                case nameof(CustomXpathMode.Node):
                    return nodes;
            }

            if (xpath.Pre != null)
                for (var i = 0; i < list.Count; i++)
                    list[i] = $"{xpath.Pre}{list[i]}";

            if (xpath.After != null)
                for (var i = 0; i < list.Count; i++)
                    list[i] = $"{list[i]}{xpath.After}";

            return list;
        }

        var node = xpath.Path.IsEmpty()? rootNode :  rootNode.SelectSingleNode(xpath.Path);
        if (node == null && xpath.PathR2 != null) node = rootNode.SelectSingleNode(xpath.PathR2);
        if (node == null) return null;
        var str = string.Empty;
        switch (xpath.Mode)
        {
            case nameof(CustomXpathMode.Attribute):
                str = node.Attributes[xpath.Attribute]?.Value;
                break;
            case nameof(CustomXpathMode.InnerText):
                str = node.InnerText;
                break;
            case nameof(CustomXpathMode.Node):
                return node;
        }

        if (xpath.Pre != null) str = $"{xpath.Pre}{str}";
        if (xpath.After != null) str = $"{str}{xpath.After}";

        if (xpath.RegexPattern != null && str!=null)
        {
            var matches = new Regex(xpath.RegexPattern).Matches(str);
            str = matches.Any() ? matches[0].Value : str;
        }

        if (xpath.Replace != null && xpath.ReplaceTo != null && str != null) str = str.Replace(xpath.Replace, xpath.ReplaceTo);

        return str;
    }

    public static string Delete(this string text, params string[] deleteStrs)
    {
        foreach (var deleteStr in deleteStrs)
        {
            text = text.Replace(deleteStr, "");
        }

        return text;
    }

    public static string ToPairsString(this Pairs pairs)
    {
        var query = string.Empty;
        var i = 0;
        if (pairs == null) return query;
        foreach (var para in pairs.Where(para => !para.Value.IsEmpty()))
        {
            if (para.Value == null)
            {
                query += $"{(i > 0 ? "&" : "?")}{para.Key}";
            }
            else
            {
                query += $"{(i > 0 ? "&" : "?")}{para.Key}={para.Value}";
            }
            
            i++;
        }

        return query;
    }

    public static dynamic GetList(dynamic dyObj)
    {
        return dyObj ?? new List<dynamic>();
    }

    public static bool IsEmpty(this string text)
    {
        return string.IsNullOrWhiteSpace(text);
    }

    public static DateTime? ToDateTime(this string dateTime, string format = null,
        DateFormatEnum dateFormat = DateFormatEnum.Auto)
    {
        switch (dateFormat)
        {
            case DateFormatEnum.Auto:
            {
                if (format != null)
                {
                    var d = DateTime.TryParseExact(dateTime, format, CultureInfo.InvariantCulture, DateTimeStyles.None,
                        out var rdate);
                    if (d) return rdate;
                }

                if (dateTime.IsEmpty()) return null;
                var timeInt = dateTime.ToUlong();
                if (timeInt != 0)
                {
                    var dt = new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(timeInt);
                    return dt;
                }

                var b = DateTime.TryParse(dateTime, out var dt2);
                if (b) return dt2;
                return null;
            }
            case DateFormatEnum.From1970TotalSeconds:
            {
                var timeInt = dateTime.ToUlong();
                var dt = new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(timeInt);
                return dt;
            }
        }

        return null;
    }

    public static int ToInt(this string idStr)
    {
        if (idStr == null) return 0;
        var b = int.TryParse(idStr.Trim(), out var id);
        if (!b)
        {
            var regex = new Regex(@"(\d+)");
            var s = regex.Matches(idStr);
            var ss = "";
            if (s.Count > 0) ss = s[0].Value;
            _ = int.TryParse(ss, out id);
        }

        return id;
    }

    public static long ToLong(this string idStr)
    {
        if (idStr == null) return 0;
        _ = long.TryParse(idStr.Trim(), out var id);
        return id;
    }

    public static ulong ToUlong(this string idStr)
    {
        if (idStr == null) return 0;
        _ = ulong.TryParse(idStr.Trim(), out var id);
        return id;
    }

    public static string ToEncodedUrl(this string orgStr)
    {
        var str = HttpUtility.UrlEncode(orgStr, Encoding.UTF8);
        return str.Replace("+", "%20");
    }

    public static string ToDecodedUrl(this string orgStr)
    {
        return HttpUtility.UrlDecode(orgStr, Encoding.UTF8);
    }

    public static void GoUrl(this string url)
    {
        if (url.IsEmpty()) return;
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false; //不使用shell启动
                p.StartInfo.RedirectStandardInput = true; //喊cmd接受标准输入
                p.StartInfo.RedirectStandardOutput = false; //不想听cmd讲话所以不要他输出
                p.StartInfo.RedirectStandardError = true; //重定向标准错误输出
                p.StartInfo.CreateNoWindow = true; //不显示窗口
                p.Start();

                //向cmd窗口发送输入信息 后面的&exit告诉cmd运行好之后就退出
                var nurl = url.Replace("&", "^&").Replace("?", "^?");
                p.StandardInput.WriteLine($"start {nurl} &exit");
                p.StandardInput.AutoFlush = true;
                p.WaitForExit(); //等待程序执行完退出进程
                p.Close();
            }
        }
        catch
        {
            if (Debugger.IsAttached) throw;
            Log($"go url:{url} fail!!");
        }
    }

    public static void GoDirectory(this string path)
    {
        if (path.IsEmpty()) return;
        try
        {
            Process.Start("explorer.exe", path);
        }
        catch
        {
            if (Debugger.IsAttached) throw;
            Log($"go path:{path} fail!!");
        }
    }

    public static void GoFile(this string path)
    {
        if (path.IsEmpty()) return;
        try
        {
            Process.Start("explorer.exe", $"/select,{path}");
        }
        catch
        {
            if (Debugger.IsAttached) throw;
            Log($"go path:{path} fail!!");
        }
    }

    public static FileInfo[] GetDirFiles(this string dirPath)
    {
        try
        {
            var dir = new DirectoryInfo(dirPath);
            return dir.GetFiles();
        }
        catch (Exception e)
        {
            Log(e);
            //if(Debugger.IsAttached) throw;
            return null;
        }
    }

    public static string GetInnerText(this HtmlNode node, string xPath)
    {
        return node.SelectSingleNode(xPath)?.InnerText.Trim();
    }

    public static void Log(params object[] objs)
    {
        var result = "";
        foreach (var obj in objs)
        {
            if ($"{obj}".IsEmpty()) continue;
            result += $"{obj}\r\n";
        }

        var str = $"{DateTime.Now:yyMMdd-HHmmss-ff}>>{result[..^2]}";
        Debug.WriteLine(str);
        LogCollection.Add(str);
        if (LogCollection.Count > 800) LogCollection.RemoveAt(0);
        LogAction?.Invoke(str);
    }

    public static event Action<string, string, MessagePos, bool> ShowMessageAction;

    /// <summary>
    ///     显示信息
    /// </summary>
    public static void ShowMessage(string message, string detailMes = null, MessagePos pos = MessagePos.Popup,
        bool isHighLight = false)
    {
        if (detailMes == null)
            Log(message);
        else
            Log(message, detailMes);
        ShowMessageAction?.Invoke(message, detailMes, pos, isHighLight);
    }

    /// <summary>
    ///     生成随机字符串
    /// </summary>
    /// <param name="length">字符串的长度</param>
    /// <returns></returns>
    public static string CreateRandomText(int length)
    {
        // 创建一个StringBuilder对象存储密码
        var sb = new StringBuilder();
        //使用for循环把单个字符填充进StringBuilder对象里面变成14位密码字符串
        for (var i = 0; i < length; i++)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            //随机选择里面其中的一种字符生成
            switch (random.Next(3))
            {
                case 0:
                    //调用生成生成随机数字的方法
                    sb.Append(CreateNum());
                    break;
                case 1:
                    //调用生成生成随机小写字母的方法
                    sb.Append(CreateSmallAbc());
                    break;
                case 2:
                    //调用生成生成随机大写字母的方法
                    sb.Append(CreateBigAbc());
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     生成单个随机数字
    /// </summary>
    private static int CreateNum()
    {
        var random = new Random(Guid.NewGuid().GetHashCode());
        var num = random.Next(10);
        return num;
    }

    /// <summary>
    ///     生成单个大写随机字母
    /// </summary>
    private static string CreateBigAbc()
    {
        //A-Z的 ASCII值为65-90
        var random = new Random(Guid.NewGuid().GetHashCode());
        var num = random.Next(65, 91);
        var abc = Convert.ToChar(num).ToString();
        return abc;
    }

    /// <summary>
    ///     生成单个小写随机字母
    /// </summary>
    private static string CreateSmallAbc()
    {
        //a-z的 ASCII值为97-122
        var random = new Random(Guid.NewGuid().GetHashCode());
        var num = random.Next(97, 123);
        var abc = Convert.ToChar(num).ToString();
        return abc;
    }
}

