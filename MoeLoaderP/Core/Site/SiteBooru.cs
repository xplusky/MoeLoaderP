using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml;
using MoeLoader.Core.Sites;
using Newtonsoft.Json.Linq;

namespace MoeLoader.Core.Site
{
    /// <summary>
    /// Booru系站点
    /// Last 20180421
    /// </summary>
    public class SiteBooru : MoeSite
    {
        /// <summary>
        /// eg. http://yande.re/post/index.xml?page={0}&limit={1}&tags={2}
        /// </summary>
        public string siteUrl;
        public string Url;
        /// <summary>
        /// eg. http://yande.re/tag/index.xml?limit={0}&order=count&name={1}
        /// </summary>
        public string tagUrl;
        protected string siteName;
        protected string shortName;
        protected string shortType;
        protected string referer;
        protected bool needMinus;
        protected BooruProcessor.SourceType srcType;
        protected MoeSession Sweb = new MoeSession();
        protected SessionHeadersCollection shc = new SessionHeadersCollection();

        /// <summary>
        /// Booru Site
        /// </summary>
        /// <param name="siteUrl">站点解析地址</param>
        /// <param name="url">图库服务器地址</param>
        /// <param name="tagUrl">tag自动提示地址</param>
        /// <param name="siteName">站点名</param>
        /// <param name="shortName">站点短名</param>
        /// <param name="referer">引用地址</param>
        /// <param name="needMinus">页码是否从0开始</param>
        /// <param name="srcType">解析类型</param>
        public SiteBooru(string siteUrl, string url, string tagUrl, string siteName, string shortName, string referer,
            bool needMinus, BooruProcessor.SourceType srcType)
        {
            this.Url = url;
            this.siteName = siteName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            this.shortName = shortName;
            this.referer = referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            //ShowExplicit = false;
            SetHeaders(srcType);
        }

        /// <summary>
        /// Use after successful login
        /// </summary>
        /// <param name="siteUrl">站点解析地址</param>
        /// <param name="url">图库服务器地址</param>
        /// <param name="tagUrl">tag自动提示地址</param>
        /// <param name="siteName">站点名</param>
        /// <param name="shortName">站点短名</param>
        /// <param name="needMinus">页码是否从0开始</param>
        /// <param name="srcType">解析类型</param>
        /// <param name="shc">Headers</param>
        public SiteBooru(string siteUrl, string url, string tagUrl, string siteName, string shortName, bool needMinus,
            BooruProcessor.SourceType srcType, SessionHeadersCollection shc)
        {
            this.Url = url;
            this.siteName = siteName;
            this.siteUrl = siteUrl;
            this.tagUrl = tagUrl;
            this.shortName = shortName;
            referer = shc.Referer;
            this.needMinus = needMinus;
            this.srcType = srcType;
            this.shc = shc;
        }

        public override string HomeUrl => siteUrl;
        public override string DisplayName => siteName;
        public override string ShortName => shortName;
        public override string Referer => referer;
        public virtual string SubReferer => ShortName;

        private void SetHeaders(BooruProcessor.SourceType srcType)
        {
            shc.Referer = referer;
            shc.Timeout = 12000;
            shc.AcceptEncoding = SessionHeadersValue.AcceptEncodingGzip;
            shc.AutomaticDecompression = System.Net.DecompressionMethods.GZip;

            SetHeaderType(srcType);
        }

        private void SetHeaderType(BooruProcessor.SourceType srcType)
        {
            switch (srcType)
            {
                case BooruProcessor.SourceType.JSON:
                case BooruProcessor.SourceType.JSONNV:
                case BooruProcessor.SourceType.JSONSku:
                    shc.Accept = shc.ContentType = SessionHeadersValue.AcceptAppJson; break;
                case BooruProcessor.SourceType.XML:
                case BooruProcessor.SourceType.XMLNV:
                    shc.Accept = shc.ContentType = SessionHeadersValue.AcceptTextXml; break;
                default:
                    shc.ContentType = SessionHeadersValue.AcceptTextHtml; break;
            }
        }

        public override string GetPageString(int page, int count, string keyWord, System.Net.IWebProxy proxy)
        {
            string url, pagestr;
            int tmpID;

            SetHeaderType(srcType);
            page = needMinus ? page - 1 : page;
            pagestr = Convert.ToString(page);

            //Danbooru 1000+ page
            switch (shortName)
            {
                case "donmai":
                case "atfbooru":
                    if (page > 1000)
                    {
                        //取得1000页最后ID
                        List<ImageItem> tmpimgs = GetImages(
                                Sweb.Get(
                                    string.Format(Url, 1000, count, keyWord)
                                , proxy, shc)
                            , proxy);

                        tmpID = tmpimgs[tmpimgs.Count - 1].Id;

                        tmpID -= (page - 1001) * count;
                        pagestr = "b" + tmpID;
                    }
                    break;
            }

            if (count > 0)
                url = string.Format(Url, pagestr, count, keyWord);
            else
                url = string.Format(Url, pagestr, keyWord);

            url = keyWord.Length < 1 ? url.Substring(0, url.Length - 6) : url;

            return Sweb.Get(url, proxy, shc);
        }

        public override List<ImageItem> GetImages(string pageString, System.Net.IWebProxy proxy)
        {
            BooruProcessor nowSession = new BooruProcessor(srcType);
            return nowSession.ProcessPage(siteUrl, Url, pageString);
        }
        

        public override List<AutoHintItem> GetAutoHintItems(string word, System.Net.IWebProxy proxy)
        {
            List<AutoHintItem> re = new List<AutoHintItem>();
            if (string.IsNullOrWhiteSpace(tagUrl)) return re;

            if (tagUrl.Contains("autocomplete.json"))
            {
                string url = string.Format(tagUrl, word);
                shc.Accept = SessionHeadersValue.AcceptAppJson;
                url = Sweb.Get(url, proxy, shc);

                // [{"id":null,"name":"idolmaster_cinderella_girls","post_count":54050,"category":3,"antecedent_name":"cinderella_girls"},
                // {"id":null,"name":"cirno","post_count":24486,"category":4,"antecedent_name":null}]

                object[] jsonobj = (new JavaScriptSerializer()).DeserializeObject(url) as object[];

                foreach (Dictionary<string, object> o in jsonobj)
                {
                    string name = "", count = "";
                    if (o.ContainsKey("name"))
                        name = o["name"].ToString();
                    if (o.ContainsKey("post_count"))
                        count = o["post_count"].ToString();
                    re.Add(new AutoHintItem()
                    {
                        Word = name,
                        Count = count
                    });
                }
            }
            else
            {
                string url = string.Format(tagUrl, 8, word);

                shc.Accept = SessionHeadersValue.AcceptTextXml;
                shc.ContentType = SessionHeadersValue.AcceptAppXml;
                string xml = Sweb.Get(url, proxy, shc);

                //<?xml version="1.0" encoding="UTF-8"?>
                //<tags type="array">
                //  <tag type="3" ambiguous="false" count="955" name="neon_genesis_evangelion" id="270"/>
                //  <tag type="3" ambiguous="false" count="335" name="angel_beats!" id="26272"/>
                //  <tag type="3" ambiguous="false" count="214" name="galaxy_angel" id="243"/>
                //  <tag type="3" ambiguous="false" count="58" name="wrestle_angels_survivor_2" id="34664"/>
                //</tags>

                if (!xml.Contains("<tag")) return re;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml.ToString());

                XmlElement root = (XmlElement)(xmlDoc.SelectSingleNode("tags")); //root

                foreach (XmlNode node in root.ChildNodes)
                {
                    XmlElement tag = (XmlElement)node;

                    string name = tag.GetAttribute("name");
                    string count = tag.GetAttribute("count");

                    re.Add(new AutoHintItem() { Word = name, Count = count });
                }
            }

            return re;
        }
    }

    /// <summary>
    /// 处理Booru类型站点
    /// Fixed 180712
    /// </summary>
    public class BooruProcessor
    {
        //private int mask = -1;
        //private int maskRes = -1;
        //private bool maskViewed = false;
        //private bool showExplicit = false;
        //private int lastViewed = -1;
        //private ViewedID viewedId;
        private SourceType type;

        /// <summary>
        /// 处理类型
        /// </summary>
        public enum SourceType
        {
            /// <summary>
            /// XML
            /// </summary>
            XML,
            /// <summary>
            /// JSON
            /// </summary>
            JSON,
            /// <summary>
            /// Sankaku JSON
            /// </summary>
            JSONSku,
            /// <summary>
            /// HTML
            /// </summary>
            HTML,
            /// <summary>
            /// XML No Verify
            /// </summary>
            XMLNV,
            /// <summary>
            /// JSON No Verify
            /// </summary>
            JSONNV,
            /// <summary>
            /// HTML No Verify
            /// </summary>
            HTMLNV
        }

        /// <summary>
        /// 获取图片源信息
        /// </summary>
        /// <param name="type">处理类型</param>
        public BooruProcessor(SourceType type)
        {
            //this.mask = mask;
            //this.maskRes = maskRes;
            //this.viewedId = viewedId;
            //this.showExplicit = showExplicit;
            //UseJpeg = useJpeg;
            //Url = url;
            this.type = type;
            //this.maskViewed = maskViewed;
        }

        /// <summary>
        /// 提取页面中的图片信息
        /// </summary>
        /// <param name="url">页面地址</param>
        /// <param name="pageString">页面源代码</param>
        /// <returns></returns>
        public List<ImageItem> ProcessPage(string siteUrl, string url, string pageString)
        {
            List<ImageItem> imgs = new List<ImageItem>();

            switch (type)
            {
                case SourceType.HTML:
                    ProcessHTML(siteUrl, url, pageString, imgs, "");
                    break;
                case SourceType.JSON:
                    ProcessJSON(siteUrl, url, pageString, imgs, "");
                    break;
                case SourceType.JSONSku:
                    ProcessJSON(siteUrl, url, pageString, imgs, "sku");
                    break;
                case SourceType.XML:
                    ProcessXML(siteUrl, url, pageString, imgs, "");
                    break;
                case SourceType.HTMLNV:
                    ProcessHTML(siteUrl, url, pageString, imgs, "nv");
                    break;
                case SourceType.JSONNV:
                    ProcessJSON(siteUrl, url, pageString, imgs, "nv");
                    break;
                case SourceType.XMLNV:
                    ProcessXML(siteUrl, url, pageString, imgs, "nv");
                    break;
            }

            return imgs;
        }

        /// <summary>
        /// HTML 格式信息
        /// </summary>
        /// <param name="siteUrl">站点链接</param>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        /// <param name="sub">标记 (nv 不验证完整性)</param>
        private void ProcessHTML(string siteUrl, string url, string pageString, List<ImageItem> imgs, string sub)
        {

            if (string.IsNullOrWhiteSpace(pageString)) return;

            //当前字符串位置
            int index = 0;

            while (index < pageString.Length)
            {
                index = pageString.IndexOf("Post.register({", index);
                if (index == -1)
                    break;
                string item = pageString.Substring(index + 14, pageString.IndexOf("})", index) - index - 13);

                #region Analyze json
                //替换有可能干扰分析的 [ ] "
                //item = item.Replace('[', '1').Replace(']', '1').Replace("\\\"", "");
                //JSONObject obj = JSONConvert.DeserializeObject(item);
                Dictionary<string, object> obj = (new JavaScriptSerializer()).DeserializeObject(item) as Dictionary<string, object>;

                string sample = "";
                if (obj.ContainsKey("preview_url"))
                    sample = obj["preview_url"].ToString();

                int file_size = 0;
                try
                {
                    if (obj.ContainsKey("file_size"))
                        file_size = int.Parse(obj["file_size"].ToString());
                }
                catch { }

                string created_at = "N/A";
                if (obj.ContainsKey("created_at"))
                    created_at = obj["created_at"].ToString();

                string preview_url = obj["sample_url"].ToString();
                string file_url = obj["file_url"].ToString();

                string jpeg_url = preview_url.Length > 0 ? preview_url : file_url;
                if (obj.ContainsKey("jpeg_url"))
                    jpeg_url = obj["jpeg_url"].ToString();

                string tags = obj["tags"].ToString();
                string id = obj["id"].ToString();
                string author = obj["author"].ToString();
                string source = obj["source"].ToString();
                //string width = obj["width"].ToString();
                //string height = obj["height"].ToString();
                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(obj["width"].ToString().Trim());
                    height = int.Parse(obj["height"].ToString().Trim());
                }
                catch { }

                string score = "N/A";
                if (obj.ContainsKey("rating"))
                {
                    score = "Safe ";
                    if (obj["rating"].ToString() == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (obj.ContainsKey("score"))
                        score += obj["score"].ToString();
                }

                string host = url.Substring(0, url.IndexOf('/', 8));

                preview_url = FormattedImgUrl(host, preview_url);
                file_url = FormattedImgUrl(host, file_url);
                sample = FormattedImgUrl(host, sample);
                jpeg_url = FormattedImgUrl(host, jpeg_url);

                //if (!UseJpeg)
                //jpeg_url = file_url;

                bool noVerify = sub.Length == 2 && sub.Contains("nv");

                ImageItem img = GenerateImg(siteUrl, url, id, author, source, width, height, file_size, created_at, score, sample, preview_url, file_url, jpeg_url, tags, noVerify);
                if (img != null) imgs.Add(img);
                #endregion

                index += 15;
            }
        }

        /// <summary>
        /// XML 格式信息
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        /// <param name="sub">标记 (nv 不验证完整性)</param>
        private void ProcessXML(string siteUrl, string url, string pageString, List<ImageItem> imgs, string sub)
        {
            if (string.IsNullOrWhiteSpace(pageString)) return;
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.LoadXml(pageString);
            }
            catch
            {
                xmlDoc.LoadXml(HttpUtility.HtmlDecode(pageString));
            }

            XmlElement root = null;
            if (xmlDoc.SelectSingleNode("posts") == null)
            {
                root = (XmlElement)(xmlDoc.SelectSingleNode("IbSearch/response")); //root
            }
            else root = (XmlElement)(xmlDoc.SelectSingleNode("posts")); //root

            foreach (XmlNode postN in root.ChildNodes)
            {
                XmlElement post = (XmlElement)postN;

                int file_size = 0;
                try
                {
                    if (post.HasAttribute("file_size"))
                        file_size = int.Parse(post.GetAttribute("file_size"));
                }
                catch { }

                string created_at = "N/A";
                if (post.HasAttribute("created_at"))
                    created_at = post.GetAttribute("created_at");

                string preview_url = post.GetAttribute("sample_url");
                string file_url = post.GetAttribute("file_url");

                string jpeg_url = preview_url.Length > 0 ? preview_url : file_url;
                if (post.HasAttribute("jpeg_url"))
                    jpeg_url = post.GetAttribute("jpeg_url");

                string sample = file_url;
                if (post.HasAttribute("preview_url"))
                    sample = post.GetAttribute("preview_url");

                string tags = post.GetAttribute("tags");
                string id = post.GetAttribute("id");
                string author = post.GetAttribute("author");
                string source = post.GetAttribute("source");
                int width = 0;
                int height = 0;
                try
                {
                    width = int.Parse(post.GetAttribute("width").Trim());
                    height = int.Parse(post.GetAttribute("height").Trim());
                }
                catch { }

                string score = "N/A";
                if (post.HasAttribute("rating"))
                {
                    score = "Safe ";
                    score = post.GetAttribute("rating") == "e" ? "Explicit " : "Questionable ";
                    if (post.HasAttribute("score"))
                        score += post.GetAttribute("score");
                }

                string host = url.Substring(0, url.IndexOf('/', 8));

                preview_url = FormattedImgUrl(host, preview_url);
                file_url = FormattedImgUrl(host, file_url);
                sample = FormattedImgUrl(host, sample);
                jpeg_url = FormattedImgUrl(host, jpeg_url);

                //if (!UseJpeg)
                //jpeg_url = file_url;
                bool noVerify = sub.Length == 2 && sub.Contains("nv");

                ImageItem img = GenerateImg(siteUrl, url, id, author, source, width, height, file_size, created_at, score, sample, preview_url, file_url, jpeg_url, tags, noVerify);
                if (img != null) imgs.Add(img);
            }
        }

        /// <summary>
        /// JSON format
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="url"></param>
        /// <param name="pageString"></param>
        /// <param name="imgs"></param>
        /// <param name="sub">站点标记</param>
        private void ProcessJSON(string siteUrl, string url, string pageString, List<ImageItem> imgs, string sub)
        {
            if (string.IsNullOrWhiteSpace(pageString)) return;
            object[] array = (new JavaScriptSerializer()).DeserializeObject(pageString) as object[];
            foreach (object o in array)
            {
                Dictionary<string, object> obj = o as Dictionary<string, object>;
                string
                    id = "",
                    tags = "",
                    host = "",
                    score = "N/A",
                    source = "",
                    sample = "",
                    jpeg_url = "",
                    file_url = "",
                    created_at = "N/A",
                    preview_url = "",
                    author = "";
                int width = 0, height = 0, file_size = 0;
                bool skin_parm;

                //域名
                host = url.Substring(0, url.IndexOf('/', 8));

                //图片ID
                if (obj["id"] != null)
                    id = obj["id"].ToString();

                //投稿者
                if (obj.ContainsKey("author") && obj["author"] != null)
                    author = obj["author"].ToString();
                else if (obj.ContainsKey("uploader_name") && obj["uploader_name"] != null)
                    author = obj["uploader_name"].ToString();

                //图片来源
                if (obj.ContainsKey("source") && obj["source"] != null)
                    source = obj["source"].ToString();

                //原图宽高 width height
                try
                {
                    if (obj.ContainsKey("width") && obj["width"] != null)
                    {
                        width = int.Parse(obj["width"].ToString().Trim());
                        height = int.Parse(obj["height"].ToString().Trim());
                    }
                    else if (obj.ContainsKey("image_width") && obj["image_width"] != null)
                    {
                        width = int.Parse(obj["image_width"].ToString().Trim());
                        height = int.Parse(obj["image_height"].ToString().Trim());
                    }
                }
                catch { }

                //文件大小
                try
                {
                    if (obj.ContainsKey("file_size") && obj["file_size"] != null)
                        file_size = int.Parse(obj["file_size"].ToString());
                }
                catch { }

                //上传时间
                if (obj.ContainsKey("created_at") && obj["created_at"] != null)
                {
                    if (sub == "sku")
                    {
                        Dictionary<string, object> objs = (Dictionary<string, object>)obj["created_at"];
                        if (objs.ContainsKey("s"))
                            created_at = objs["s"].ToString();
                    }
                    else
                    {
                        created_at = obj["created_at"].ToString();
                    }
                }

                //评级和评分
                if (obj.ContainsKey("rating") && obj["rating"] != null)
                {
                    score = "Safe ";
                    if (obj["rating"].ToString() == "e")
                        score = "Explicit ";
                    else score = "Questionable ";
                    if (obj.ContainsKey("score"))
                        score += obj["score"].ToString();
                    else if (obj.ContainsKey("total_score"))
                        score += obj["total_score"].ToString();
                }

                //缩略图
                if (obj.ContainsKey("preview_url") && obj["preview_url"] != null)
                    sample = obj["preview_url"].ToString();
                else if (obj.ContainsKey("preview_file_url") && obj["preview_file_url"] != null)
                    sample = obj["preview_file_url"].ToString();

                //预览图
                if (obj.ContainsKey("sample_url") && obj["sample_url"] != null)
                    preview_url = obj["sample_url"].ToString();
                else if (obj.ContainsKey("large_file_url") && obj["large_file_url"] != null)
                    preview_url = obj["large_file_url"].ToString();

                //原图
                if (obj.ContainsKey("file_url") && obj["file_url"] != null)
                    file_url = obj["file_url"].ToString();

                //JPG
                jpeg_url = preview_url.Length > 0 ? preview_url : file_url;
                if (obj.ContainsKey("jpeg_url") && obj["jpeg_url"] != null)
                    jpeg_url = obj["jpeg_url"].ToString();

                //Formatted
                skin_parm = sub.Contains("sku");

                sample = FormattedImgUrl(host, sample, skin_parm);
                preview_url = FormattedImgUrl(host, preview_url, skin_parm);
                file_url = FormattedImgUrl(host, file_url, skin_parm);
                jpeg_url = FormattedImgUrl(host, jpeg_url, skin_parm);


                //标签
                if (obj.ContainsKey("tags") && obj["tags"] != null)
                {

                    if (sub == "sku")
                    {
                        #region sankaku tags rule
                        try
                        {
                            JavaScriptSerializer jss = new JavaScriptSerializer();
                            string stags = jss.Serialize(obj["tags"]);

                            JArray jary = JArray.Parse(stags);
                            if (jary.Count > 0)
                            {
                                JArray ResultTags = new JArray();
                                JArray TagGroup = new JArray();
                                string TagTypeGroup = "3,2,4,1,8,0,9";
                                string[] TTGarr = TagTypeGroup.Split(',');

                                foreach (string tts in TTGarr)
                                {
                                    foreach (JToken tjt in jary)
                                    {
                                        if (((JObject)tjt)["type"].ToString().Equals(tts, StringComparison.CurrentCultureIgnoreCase))
                                        {
                                            TagGroup.Add(tjt);
                                        }
                                    }

                                    if (tts.Equals("0"))
                                        ResultTags.Add(TagGroup.OrderBy(jo => (string)jo["name"]));
                                    else
                                        ResultTags.Add(TagGroup.OrderByDescending(jo => Convert.ToInt32((string)jo["count"])));

                                    TagGroup.Clear();
                                }

                                int i = 0, RTcount = ResultTags.Count();
                                foreach (JToken tjt in ResultTags)
                                {
                                    i++;
                                    tags += i < RTcount ? ((JObject)tjt)["name"].ToString() + " " : ((JObject)tjt)["name"].ToString();
                                }
                            }
                        }
                        catch
                        {
                            object ov = obj["tags"];
                            StringBuilder ovsb = new StringBuilder();

                            if (ov.GetType().FullName.Contains("Object[]"))
                            {
                                (new JavaScriptSerializer()).Serialize(ov, ovsb);
                                string ovsbstr = ovsb.ToString();
                                object[] ovarr = (new JavaScriptSerializer()).DeserializeObject(ovsbstr) as object[];
                                for (int i = 0; i < ovarr.Count(); i++)
                                {
                                    obj = ovarr[i] as Dictionary<string, object>;
                                    if (obj.ContainsKey("name"))
                                        tags += i < ovarr.Count() - 1 ? obj["name"] + " " : obj["name"];
                                }
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        tags = obj["tags"].ToString();
                    }
                }
                else if (obj.ContainsKey("tag_string") && obj["tag_string"] != null)
                {
                    tags = obj["tag_string"].ToString();
                }

                bool noVerify = sub.Length == 2 && sub.Contains("nv");

                ImageItem img = GenerateImg(siteUrl, url, id, author, source, width, height, file_size, created_at, score, sample, preview_url, file_url, jpeg_url, tags, noVerify);
                if (img != null) imgs.Add(img);
            }
        }

        /// <summary>
        /// 生成 Img 对象
        /// </summary>
        /// <param name="siteUrl">主站点</param>
        /// <param name="url"></param>
        /// <param name="id"></param>
        /// <param name="author"></param>
        /// <param name="src"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="file_size"></param>
        /// <param name="created_at"></param>
        /// <param name="score"></param>
        /// <param name="sample">缩略图</param>
        /// <param name="preview_url">预览图</param>
        /// <param name="file_url">原图</param>
        /// <param name="jpeg_url">jpg</param>
        /// <param name="tags"></param>
        /// <returns></returns>
        private ImageItem GenerateImg(string siteUrl, string url, string id, string author,
            string src, int width, int height, int file_size, string created_at,
            string score, string sample, string preview_url, string file_url, string jpeg_url, string tags, bool noVerify)
        {
            int scoreInt = 0, intId = 0;
            try
            {
                intId = int.Parse(id);
            }
            catch { }
            try
            {
                scoreInt = int.Parse(score.Substring(score.IndexOf(' '), score.Length - score.IndexOf(' ')));
            }
            catch { }

            #region DateTime Convert
            //eg. Fri Aug 28 20:05:57 -0600 2009 or 1291280246
            try
            {
                //1291280246   ==   2010/12/2 16:57
                long sec = long.Parse(created_at);
                DateTime startDate = new DateTime(1970, 1, 1, 8, 0, 0, 0);
                created_at = startDate.AddSeconds(sec).ToString();
            }
            catch
            {
                //Thu Dec 31 06:54:54 +0000 2009
                //2012/01/28 01:59:10 -0500
                //1323123123
                //Dec Nov Oct Sep Aug Jul Jun May Apr Mar Feb Jan
                try
                {
                    created_at = DateTime.Parse(created_at).ToString();
                }
                catch { }
            }
            #endregion

            string detailUrl = siteUrl + "/post/show/" + id;
            if (url.Contains("index.php"))
                detailUrl = siteUrl + "/index.php?page=post&s=view&id=" + id;

            ImageItem img = new ImageItem()
            {
                Date = created_at,
                Description = tags,
                FileSize = file_size > 1048576 ? (file_size / 1048576.0).ToString("0.00MB") : (file_size / 1024.0).ToString("0.00KB"),
                Height = height,
                Id = intId,
                Author = author == "" ? "UnkwnAuthor" : author,
                IsExplicit = score.StartsWith("E"),
                JpegUrl = jpeg_url,
                OriginalUrl = file_url,
                PreviewUrl = preview_url,
                ThumbnailUrl = sample,
                Score = scoreInt,
                Source = src,
                TagsText = tags,
                Width = width,
                DetailUrl = detailUrl,
                NoVerify = noVerify
            };
            return img;
        }

        /// <summary>
        /// 图片地址格式化
        /// 2016年12月对带域名型地址格式化
        /// by YIU
        /// </summary>
        /// <param name="pr_host">图站域名</param>
        /// <param name="pr_url">预处理的URL</param>
        /// <param name="skin_parameters">不处理链接带的参数</param>
        /// <returns>处理后的图片URL</returns>
        public static string FormattedImgUrl(string pr_host, string pr_url, bool skin_parameters)
        {
            //System.Diagnostics.Trace.WriteLine("host: " + pr_host);
            try
            {
                //域名处理 - 如果有
                if (!string.IsNullOrWhiteSpace(pr_host))
                {
                    int po = pr_host.IndexOf("//");
                    string phh = pr_host.Substring(0, pr_host.IndexOf(':') + 1);
                    string phu = pr_host.Substring(po, pr_host.Length - po);

                    //地址中有主域名 去掉主域名
                    if (pr_url.StartsWith(phu))
                        pr_url = pr_host + pr_url.Replace(phu, "");

                    //地址中有子域名 补完子域名
                    else if (pr_url.StartsWith("//"))
                        pr_url = phh + pr_url;

                    //地址没有域名 补完地址
                    else if (pr_url.StartsWith("/"))
                        pr_url = pr_host + pr_url;
                }
                //过滤图片地址?后的内容
                if (!skin_parameters && pr_url.Contains("?"))
                    pr_url = pr_url.Substring(0, pr_url.LastIndexOf('?'));

                return pr_url;
            }
            catch
            {
                return pr_url;
            }
        }

        /// <summary>
        /// 图片地址格式化-All
        /// </summary>
        /// <param name="pr_host">图站域名</param>
        /// <param name="pr_url">预处理的URL</param>
        /// <returns>处理后的图片URL</returns>
        public static string FormattedImgUrl(string pr_host, string pr_url)
        {
            return FormattedImgUrl(pr_host, pr_url, false);
        }

    }
}
