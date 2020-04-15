using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace MoeLoaderP.Core
{
    /// <summary>
    /// 扩展方法集合
    /// </summary>
    public static class Extend
    {
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
            foreach (var para in pairs.Where(para => !string.IsNullOrEmpty(para.Value)))
            {
                query += string.Format("{2}{0}={1}", para.Key, para.Value, i > 0 ? "&" : "?");
                i++;
            }

            return query;
        }
        public static dynamic CheckListNull(dynamic dyObj)
        {
            return dyObj == null ? new List<dynamic>() : dyObj;
        }
        public static bool IsNaN(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }
        public static DateTime? ToDateTime(this string dateTime)
        {
            if (string.IsNullOrWhiteSpace(dateTime)) return null;
            var timeInt = dateTime.ToLong();
            if (timeInt != 0)
            {
                var dt = new DateTime(1970, 1, 1) + TimeSpan.FromSeconds(timeInt);
                return dt;
            }

            var b = DateTime.TryParse(dateTime, out var dt2);
            if (b) return dt2;
            return null;
        }
        public static int ToInt(this string idStr)
        {
            if (idStr == null) return 0;
            int.TryParse(idStr.Trim(), out var id);
            return id;
        }

        public static long ToLong(this string idStr)
        {
            if (idStr == null) return 0;
            long.TryParse(idStr.Trim(), out var id);
            return id;
        }



        public static string ToEncodedUrl(this string orgstr)
        {
            return HttpUtility.UrlEncode(orgstr, Encoding.UTF8);
        }

        public static string ToDecodedUrl(this string orgstr)
        {
            return HttpUtility.UrlDecode(orgstr, Encoding.UTF8);
        }

        public static void GoUrl(this string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(url);
            }
            catch
            {
                Log($"go url:{url} fail!!");
            }
        }

        public static void Log(params object[] objs)
        {
            var str = $"{DateTime.Now:yyMMdd-HHmmss-ff}>>{objs.Aggregate((o, o1) => $"{o}\r\n{o1}")}";
            Debug.WriteLine(str);
            LogAction?.Invoke(str);
        }
        public static Action<string, string, MessagePos> ShowMessageAction;

        public static Action<string> LogAction;

        /// <summary>
        /// 显示信息
        /// </summary>
        public static void ShowMessage(string message, string detailMes = null, MessagePos pos = MessagePos.Popup)
        {
            ShowMessageAction?.Invoke(message, detailMes, pos);
        }

        public enum MessagePos
        {
            InfoBar,
            Popup,
            Searching,
            Window
        }

        public static string Encode(this string str)
        {
            var key = Encoding.ASCII.GetBytes("leaf");
            var iv = Encoding.ASCII.GetBytes("1234");
            MemoryStream ms = null;
            CryptoStream cs = null;
            StreamWriter sw = null;

            var des = new DESCryptoServiceProvider();
            try
            {
                ms = new MemoryStream();
                cs = new CryptoStream(ms, des.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                sw = new StreamWriter(cs);
                sw.Write(str);
                sw.Flush();
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
            finally
            {
                sw?.Close();
                cs?.Close();
                ms?.Close();
            }
        }

        public static string Decode(this string str)
        {
            var key = Encoding.ASCII.GetBytes("leaf");
            var iv = Encoding.ASCII.GetBytes("1234");
            MemoryStream ms = null;
            CryptoStream cs = null;
            StreamReader sr = null;

            var des = new DESCryptoServiceProvider();
            try
            {
                ms = new MemoryStream(Convert.FromBase64String(str));
                cs = new CryptoStream(ms, des.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            finally
            {
                sr?.Close();
                cs?.Close();
                ms?.Close();
            }
        }
    }
}


