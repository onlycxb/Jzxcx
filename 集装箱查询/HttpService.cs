using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using System.Web;//添加引用

namespace 集装箱查询
{
    public class HttpService
    {
        public Action<List<Info>> showInfo;

        //查询结果集，用于绑定datagridView
        public List<Info> infoList = new List<Info>();

        private CookieContainer container = new CookieContainer();
        private string url = "https://www.hamburgsud-line.com/linerportal/pages/hsdg/tnt.xhtml";

        /// <summary>
        /// 根据BLNo及开始和结束时间，查询详情
        /// </summary>
        /// <param name="blNo"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <returns></returns>
        public List<Info> GetList(string blNo = "SUDU50650A6BP049", string dateFrom = "10-Jul-2020", string dateTo = "19-Sep-2020")
        {
            var list = new List<Info>();
        retry:
            container = new CookieContainer();
            // 获取参数
            var viewState = GetUrlEncode_ViewState();
            if (viewState == "")
                goto retry;

            // 准备查询数据
            var sd = $"javax.faces.partial.ajax=true&javax.faces.source=j_idt6%3AsearchForm%3Aj_idt8%3Asearch-submit" +
                $"&javax.faces.partial.execute=j_idt6%3AsearchForm&javax.faces.partial.render=j_idt6%3AsearchForm" +
                $"&j_idt6%3AsearchForm%3Aj_idt8%3Asearch-submit=j_idt6%3AsearchForm%3Aj_idt8%3Asearch-submit" +
                $"&j_idt6%3AsearchForm=j_idt6%3AsearchForm&j_idt6%3AsearchForm%3Aj_idt8%3A" +
                $"inputReferences={blNo}" +
                $"&j_idt6%3AsearchForm%3Aj_idt8%3AinputDateFrom_input={dateFrom}" +
                $"&j_idt6%3AsearchForm%3Aj_idt8%3AinputDateTo_input={dateTo}" +
                $"&javax.faces.ViewState={viewState}";

            // 提交查询请求
            var html = HttpPost(url, sd, url);
            if (!html.Contains("method=\"post\""))
            {
                System.Threading.Thread.Sleep(1000);
                Console.WriteLine("数据不正确 ，没有发现post");
                goto retry;
            }
            //解析返回数据
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var trs = doc.DocumentNode.SelectNodes("//*[@id='j_idt6:searchForm:j_idt24:j_idt27_data']/tr");
            if (trs == null)
            {
                Console.WriteLine("数据不正确 ，没有返回数据");
                System.Threading.Thread.Sleep(1000);
                goto retry;
            }

            //列表的第1个为标题行，从第2行开始取数据
            for (int i = 1; i < trs.Count; i++)
            {
                var tds = trs[i].SelectNodes("td");
                if (tds[0].InnerText.Contains("Qingdao CNTAO"))
                {
                    var containerNo = tds[1].InnerText.Trim();
                    list.AddRange(GetDetailInfo(blNo, dateFrom, dateTo, containerNo, i - 1, viewState));
                }
            }

            return list;
        }

        /// <summary>
        /// 根据containerNo 查询详情
        /// </summary>
        /// <param name="biNo"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <param name="containerNo"></param>
        /// <param name="sid"></param>
        /// <param name="viewState"></param>
        /// <returns></returns>
        private List<Info> GetDetailInfo(string biNo, string dateFrom, string dateTo, string containerNo, int sid, string viewState)
        {
            var list = new List<Info>();
            var sd = $"javax.faces.partial.ajax=true" +
                $"&javax.faces.source=j_idt6%3AsearchForm%3Aj_idt24%3Aj_idt27%3A{sid}%3AcontDetailsLink" +
                $"&javax.faces.partial.execute=j_idt6%3AsearchForm%3Aj_idt24%3Aj_idt27%3A{sid }%3AcontDetailsLink" +
                $"&javax.faces.partial.render=j_idt6%3AsearchForm" +
                $"&j_idt6%3AsearchForm%3Aj_idt24%3Aj_idt27%3A{sid}%3AcontDetailsLink=j_idt6%3AsearchForm%3Aj_idt24%3Aj_idt27%3A{sid}%3AcontDetailsLink" +
                $"&j_idt6%3AsearchForm=j_idt6%3AsearchForm" +
                $"&j_idt6%3AsearchForm%3Aj_idt8%3AinputReferences={biNo}" +
                $"&j_idt6%3AsearchForm%3Aj_idt8%3AinputDateFrom_input={dateFrom}" +
                $"&j_idt6%3AsearchForm%3Aj_idt8%3AinputDateTo_input={dateTo}" +
                $"&javax.faces.ViewState={viewState}";

            var html = HttpPost(url, sd, url);
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            //*[@id='j_idt6:searchForm:j_idt39:j_idt113_data']/tr[1]
            var trs = doc.DocumentNode.SelectNodes("//*[@id='j_idt6:searchForm:j_idt39:j_idt113_data']/tr");
            if (trs == null)
                return list;

            for (int i = 1; i < trs.Count; i++)
            {
                var tds = trs[i].SelectNodes("td");
                //Date	Place	Movement	Mode/Vendor
                Info info = new Info();
                info.BLNO = biNo;
                info.ContainerNo = containerNo;

                info.Date = tds[0].InnerText;
                info.Place = tds[1].InnerText;
                info.Movement = tds[2].InnerText;
                info.Mode_Vendor = tds[3].InnerText;

                infoList.Add(info);
                if (showInfo != null)
                    showInfo.Invoke(infoList);

                list.Add(info);
            }

            return list;
        }

        /// <summary>
        /// 获取ViewState参数值
        /// </summary>
        /// <returns></returns>
        public string GetUrlEncode_ViewState()
        {
            var res = GetHtml(url, "utf-8");
            var pattern = "id=\"j_id1:javax.faces.ViewState:0\" value=\"(.+?)\"";
            var viewState = Regex.Match(res, pattern).Groups[1].Value;
            return HttpUtility.UrlEncode(viewState);
        }

        /// <summary>
        /// 根据网址获取返回内容
        /// </summary>
        /// <param name="url"></param>
        /// <param name="StrEnCode"></param>
        /// <returns></returns>
        public string GetHtml(string url, string StrEnCode)
        {
            string result = string.Empty;
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var request = (HttpWebRequest)WebRequest.Create(url);
            if (container.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                container = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = container;
            }

            request.Method = "GET";
            request.KeepAlive = true;
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;

            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Accept = "application/xml, text/xml, */*; q=0.01";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3775.400 QQBrowser/10.6.4209.400";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            try
            {
                using (WebResponse response = request.GetResponse())
                {
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(StrEnCode)))
                    {
                        var cookies2 = container.GetCookies(response.ResponseUri);
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return result;
        }

        public string HttpPost(string Url, string postDataStr, string refererUrl = null)
        {
        retry:
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            var request = (HttpWebRequest)WebRequest.Create(Url);
            if (container.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
                container = request.CookieContainer;
            }
            else
            {
                request.CookieContainer = container;
            }
            request.Method = "POST";
            request.Timeout = 20000;
            request.ReadWriteTimeout = 20000;
            request.KeepAlive = true;
            request.Headers.Add("Faces-Request", "partial/ajax");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Accept = "application/xml, text/xml, */*; q=0.01";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3775.400 QQBrowser/10.6.4209.400";
            request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            //request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.Default);

            if (refererUrl != null)
                request.Referer = refererUrl;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = postDataStr.Length;
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(postDataStr);
            myStreamWriter.Close();
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                return retString;
            }
            catch (Exception ex)
            {
                goto retry;
            }
        }
    }

    public class Info
    {
        public string BLNO { get; set; }
        public string ContainerNo { get; set; }
        public string Date { get; set; }
        public string Place { get; set; }
        public string Movement { get; set; }
        public string Mode_Vendor { get; set; }
    }
}