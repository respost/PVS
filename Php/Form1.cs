using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Net;
using System.Web;
using System.IO;
using System.Threading;
using mshtml;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Collections.Specialized;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Reflection;
using System.Management;

namespace Php
{
    public partial class Form1 : Form
    {
        //单个网站http对象
        Http http = new Http();
        //当前多个网站http对象,线程个数
        public int n = 0;
        public int m = 0;
        public static int N = 1024;
        //多个网站的http对象数组
        Http[] https = new Http[N];
        //多个网站开辟的线程数组
        Thread[] threads = new Thread[N];
        public Boolean IsWebsiteScan = true;
        //扫描用时
        public String TimeUsedStr = "";
        public int threadNumber = 0;
        //端口扫描相关
        string IP_start = null;
        int ip1, ip2, ip3, ip4;
        string[] IpArray = new string[5];
        //上锁
        public Boolean Locked = true;
        //Ini文件工具类
        private IniFiles ini = null;

        public Form1()
        {
            //加载嵌入资源
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            //加载更新程序
            LoadUpdateFile();
            //加载html模板
            LoadHtmlTemplate();
            //加载xml配置
            LoadXmlFile();
            InitializeComponent();
            //设置WEB服务器安全模块
            CheckForIllegalCrossThreadCalls = false;
            this.ini = new IniFiles("config.ini");
        }
        /// <summary>
        /// 加载嵌入资源中的全部dll文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            System.Resources.ResourceManager rm = new System.Resources.ResourceManager(GetType().Namespace + ".Properties.Resources", System.Reflection.Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            return System.Reflection.Assembly.Load(bytes);
        }
        //内存信息
        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryInfo //此处全是以字节为单位
        {
            public uint dwLength;//长度
            public uint dwMemoryLoad;//内存使用率
            public uint dwTotalPhys;//总物理内存
            public uint dwAvailPhys;//可用物理内存
            public uint dwTotalPageFile;//交换文件总大小
            public uint dwAvailPageFile;//可用交换文件大小
            public uint dwTotalVirtual;//总虚拟内存
            public uint dwAvailVirtual;//可用虚拟内存大小
        }
        //读取内存
        [DllImport("kernel32")]
        public static extern void GlobalMemoryStatus(ref MemoryInfo MemInfo);

        private void Form1_Load(object sender, EventArgs e)
        {
            //获取明文字典
            txtDictionaryPath.Text = Environment.CurrentDirectory + "\\明文字典.txt";
            //获取本机IP地址
            toolStripAddressIP.Text = Tool.getAddressIP();
            //判断在线离线事件
            if (Tool.LocalConnectionStatus())
            {
                this.toolStripInternet.Image = global::Php.Properties.Resources._in;
                this.toolStripInternet.Text = "网络已连接";
            }
            else
            {
                this.toolStripInternet.Image = global::Php.Properties.Resources._out;
                this.toolStripInternet.Text = "网络未连接";
            }
            tssLabelKillResult.Text = "";
        }
        #region //sql漏洞扫描事件

        private void radioButton＿WebsiteAdd_CheckedChanged(object sender, EventArgs e)
        {
            txtSingleUrl.Enabled = true;
            txtMultipleUrlPath.Enabled = false;
            picFile.Enabled = false;
        }

        private void radioButton＿WebsitesAdd_CheckedChanged(object sender, EventArgs e)
        {
            txtSingleUrl.Enabled = false;
            txtMultipleUrlPath.Enabled = true;
            picFile.Enabled = true;
        }

        private void button_Exit_Click(object sender, EventArgs e)
        {
            this.Dispose();
            this.Close();
        }

        public void ThreadProc_WebsiteAdd()
        {
            if (http != null)
            {
                http.Scan();
            }
        }
        public void ThreadProc_WebsitesAdd()
        {
            int n1 = n;
            Locked = false;
            if (https[n1 % N] != null)
            {
                https[n1 % N].Scan();
            }
        }

        public void googlelinkceshi()
        {

            System.Text.RegularExpressions.Regex R = new System.Text.RegularExpressions.Regex("http://\\w+([-.]\\w+)*.\\w+([-.]\\w+)*(:\\d+)*(/[-\\w%?&=]*)*");
            //System.Text.RegularExpressions.Regex R = new System.Text.RegularExpressions.Regex("http://[\\w-]+.google[\\w-]*(\\.\\w(2,4))(1,2)/");
            Match M = R.Match(richTextBox1.Text);
            //R.Replace("google", "");
            while (M.Success)
            {
                listBox1.Items.Add(M.Value);
                M = M.NextMatch();
            }
        }
        /// <summary>
        /// webBrowser加载网址
        /// </summary>
        /// <param name="address"></param>
        private void Navigate(String address)
        {
            if (String.IsNullOrEmpty(address)) return;
            if (address.Equals("about:blank")) return;
            if (!address.StartsWith("http://") &&
                !address.StartsWith("https://"))
            {
                address = "http://" + address;
            }
            try
            {
                webBrowser1.Navigate(new Uri(address));
            }
            catch (System.UriFormatException)
            {
                return;
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                this.tscbUrl.Text = webBrowser1.Url.ToString();
                //this.tsbBack.Enabled = webBrowser1.CanGoForward; //判断及设定 前进按钮是否可用
                //this.tspForward.Enabled = webBrowser1.CanGoBack; //判断及设定 后退按钮是否可用
                //屏蔽网页JS脚本错误弹窗
                webBrowser1.ScriptErrorsSuppressed = true;
            }
            catch (Exception)
            { }

        }
        Thread threadSqlScan;
        private void btnSqlScanStart_Click(object sender, EventArgs e)
        {

            threadSqlScan = new Thread(new ThreadStart(SqlScan));
            try
            {
                btnSqlScanStop.Enabled = true;
                threadSqlScan.Start();
            }
            catch
            {
            }

        }
        /// <summary>
        /// SQL漏洞扫描
        /// </summary>
        public void SqlScan()
        {
            if (radioButtonSingle.Checked)
            {//单个网站扫描 
                this.IsWebsiteScan = true;
                //开启程序执行时间统计
                Stopwatch sw1 = new Stopwatch();
                Stopwatch sw2 = Stopwatch.StartNew();
                //---------------------------------------------------------------
                sw1.Start();
                listViewScanReport.Clear();
                listViewScanReport.GridLines = true;//显示各个记录的分隔线 
                listViewScanReport.View = View.Details;//定义列表显示的方式 
                listViewScanReport.Scrollable = true; //需要时候显示滚动条
                if (listViewScanReport.Columns.Count == 0)
                {
                    listViewScanReport.Columns.Add("可能的注入点", listViewScanReport.Width - 200, HorizontalAlignment.Left);
                    listViewScanReport.Columns.Add("是否泄漏敏感信息", 120, HorizontalAlignment.Center);
                    listViewScanReport.Columns.Add("可否注入", 80, HorizontalAlignment.Center);
                }
                //开一个线程
                String website = txtSingleUrl.Text.Trim();
                if (!Tool.IsDomain(website))
                {
                    MessageBox.Show("请输入正确的网址\r\n(以http://或者https://开头的域名)", "提示");
                    return;
                }
                http = new Http(website);
                if (checkBoxScanFile.Checked)
                {
                    //扫描域名控制器
                    ScanURLController();
                }
                progressBar_Scanner.Minimum = 0;
                progressBar_Scanner.Maximum = 100;
                progressBar_Scanner.Value = 10;
                Thread t = new Thread(ThreadProc_WebsiteAdd);
                t.Start();
                Boolean wait = true;
                DateTime dt = DateTime.Now;
                while (wait)
                {
                    if (http.floor == 1)
                        progressBar_Scanner.Value = 30;
                    else if (http.floor == 2)
                        progressBar_Scanner.Value = 50;
                    else if (http.floor == 3)
                        progressBar_Scanner.Value = 70;
                    //查看是否扫描结束
                    if (http.floor_threads_num[http.floor] == -1)
                    {
                        wait = false;
                        break;
                    }
                    if (progressBar_Scanner.Value >= 70)
                    {
                        wait = false;
                        break;
                    }
                    DateTime dt1 = DateTime.Now;
                    TimeSpan dt_diff = dt1 - dt;
                    if (dt_diff.Seconds >= 25)
                    {
                        wait = false;
                        break;
                    }
                }

                t.Join(25 * 1000);
                progressBar_Scanner.Value = 100;
                //显示scan返回结果
                ArrayList alLinks = http.alPossibleInjectionPoints;
                Array aLinks = alLinks.ToArray();
                for (int i = 0; i < aLinks.GetLength(0); i++)
                {
                    ListViewItem lt = new ListViewItem();
                    lt.SubItems[0].Text = ((InjectionPoint)aLinks.GetValue(i)).Url;
                    lt.SubItems.Add(((InjectionPoint)aLinks.GetValue(i)).IsSensitive.ToString());
                    lt.SubItems.Add(((InjectionPoint)aLinks.GetValue(i)).CanInject.ToString());
                    listViewScanReport.Items.Add(lt);
                }
                ListViewItem lvt = new ListViewItem();
                http.N_Pages = http.alPossibleInjectionPoints.Count;
                http.N_Pages_secure = http.N_Pages - http.N_Pages_secure;
                int j = 0; String state = "[";
                while (http.floor_threads_num[j] != -1 && http.floor_threads_num[j] != 0)
                {
                    state += http.floor_threads_num[j++];
                    state += " ";
                }
                state += "]";
                lvt.SubItems[0].Text = "扫描完毕![" + http.RootUrl + "]" + "[" + http.DBType + "]" + state;
                lvt.SubItems.Add(http.IsSensitive.ToString() + " " + http.N_Pages_sensitive + "/" + http.N_Pages);
                lvt.SubItems.Add(http.IsInjectable.ToString() + " " + http.N_Pages_injectable + "/" + http.N_Pages);
                listViewScanReport.Items.Add(lvt);
                sw2.Stop();
                long elaspsedMilliseconds = sw1.ElapsedMilliseconds / 1000;
                int seconds = (int)elaspsedMilliseconds;
                TimeUsedStr = Tool.parseTimeSeconds(seconds, 0);
                label_timeused.Text = "扫描用时:" + TimeUsedStr;
                label_state.Text = "状态:" + "扫描完毕!";
                MessageBox.Show("扫描完成，请查看HTML扫描报告！");
            }
            else//多个网站扫描
            {
                this.IsWebsiteScan = false;
                //开启程序执行时间统计
                Stopwatch sw1 = new Stopwatch();
                Stopwatch sw2 = Stopwatch.StartNew();
                sw1.Start();
                //-----------------------------------------------
                if (txtMultipleUrlPath.Text == "")
                {
                    MessageBox.Show("请选择域名列表文件");
                    return;
                }
                label_state.Text = "状态:" + "正在读取文件 " + txtMultipleUrlPath.Text + " .......";
                ArrayList alUrls = new ArrayList();
                if (txtMultipleUrlPath.Text.Substring(txtMultipleUrlPath.Text.LastIndexOf('.')) == ".txt")
                {
                    string urls = txtMultipleUrlPath.Text;
                    //检查文件是否存在，不存在就创建
                    if (!File.Exists(urls))
                    {
                        File.Create(urls);
                    }
                    StreamReader reader = new StreamReader(urls);
                    try
                    {
                        while (reader.Peek() != -1)
                        {
                            alUrls.Add(new Website(reader.ReadLine(), ""));
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        reader.Close();
                    }
                    if (alUrls.Count == 0)
                    {
                        MessageBox.Show("没有检测到需要扫描的域名", "提示");
                        return;
                    }
                    label_state.Text = "State:" + "文件已读取,开始准备多线程执行程序......";
                    //以上对输入的txt文件检验是否存在及内容是否为空
                }
                //清空listview 
                listViewScanReport.Clear();
                listViewScanReport.GridLines = true;//显示各个记录的分隔线 
                listViewScanReport.View = View.Details;//定义列表显示的方式 
                listViewScanReport.Scrollable = true; //需要时候显示滚动条
                listViewScanReport.Sorting = SortOrder.None;
                if (listViewScanReport.Columns.Count == 0)
                {
                    listViewScanReport.Columns.Add("可能的注入点", 490);
                    listViewScanReport.Columns.Add("是否泄漏敏感信息", 90);
                    listViewScanReport.Columns.Add("可否注入", 90);
                }
                progressBar_Scanner.Minimum = 0;
                progressBar_Scanner.Maximum = 100;
                progressBar_Scanner.Value = 2;
                n = 0;

                foreach (Website ws in alUrls)
                {
                    if (ws.strurl == null || ws.strurl == "" || ws.strurl.Trim() == "") continue;
                    https[n] = new Http(ws.strurl, ws.strinfo);
                    if (checkBoxScanFile.Checked)
                    {
                        //扫描域名控制器
                        ScanURLController();
                    }
                    threads[n] = new Thread(ThreadProc_WebsitesAdd);
                    threads[n].Start();
                    while (Locked) ;
                    n++;
                    Locked = true;
                }
                threadNumber = ((n > N) ? N : n);
                //label_state.Text = "State:" + L +"个多线程已创建,主控程序开始监控多线程的执行进度......";
                progressBar_Scanner.Value = 5;
                Boolean wait = true;
                DateTime dt = DateTime.Now;
                while (wait)
                {
                    int value = 0;
                    for (int l = 0; l < threadNumber; l++)
                    {
                        if (https[l].floor_threads_num[https[l].floor] == -1)
                            value += (95 / threadNumber);
                    }
                    label_state.Text = "State:" + threadNumber + "个多线程正在执行,目前进度:" + (5 + value) + "%";
                    progressBar_Scanner.Value = 5 + value;
                    //停止进度准确滚动
                    if (progressBar_Scanner.Value > 50)
                    {
                        wait = false;
                    }
                    DateTime dt1 = DateTime.Now;
                    TimeSpan dt_diff = dt1 - dt;
                    if (dt_diff.Seconds >= 30)
                    {
                        wait = false;
                    }

                    Thread.Sleep(2 * 1000);
                }
                label_state.Text = "State:" + "开始发出的" + threadNumber + "个多线程.......";
                //等待发出的(n > 100) ? 100 : n个线程是否返回 
                for (int i = 0; i < ((n > N) ? N : n); i++)
                {
                    if (threads[i] != null)
                    {
                        threads[i].Join(30 * 1000);
                        label_state.Text = "State:" + "开始发出的" + threadNumber + "个多线程, 线程" + i + "已完成!";
                    }
                }
                label_state.Text = "State:" + "多线程执行完毕,正在显示扫描结果........";
                progressBar_Scanner.Value = 100;
                for (int j = 0; j < n; j++)
                {
                    ArrayList alLinks = https[j].alPossibleInjectionPoints;
                    Array aLinks = alLinks.ToArray();
                    for (int i = 0; i < aLinks.GetLength(0); i++)
                    {
                        ListViewItem lt = new ListViewItem();
                        lt.SubItems[0].Text = ((InjectionPoint)aLinks.GetValue(i)).Url;
                        lt.SubItems.Add(((InjectionPoint)aLinks.GetValue(i)).IsSensitive.ToString());
                        lt.SubItems.Add(((InjectionPoint)aLinks.GetValue(i)).CanInject.ToString());
                        listViewScanReport.Items.Add(lt);
                    }
                    ListViewItem lvt = new ListViewItem();
                    https[j].N_Pages = https[j].alPossibleInjectionPoints.Count;
                    https[j].N_Pages_secure = https[j].N_Pages - https[j].N_Pages_secure;
                    int k = 0; String state = "[";
                    while (https[j].floor_threads_num[k] != -1 && https[j].floor_threads_num[k] != 0)
                    {
                        state += https[j].floor_threads_num[k++];
                        state += " ";
                    }
                    state += "]";
                    lvt.SubItems[0].Text = "扫描完毕![" + https[j].RootUrl + "]" + "[" + https[j].DBType + "]" + state;
                    lvt.SubItems.Add(https[j].IsSensitive.ToString() + " " + https[j].N_Pages_sensitive + "/" + https[j].N_Pages);
                    lvt.SubItems.Add(https[j].IsInjectable.ToString() + " " + https[j].N_Pages_injectable + "/" + https[j].N_Pages);
                    listViewScanReport.Items.Add(lvt);
                }
                sw2.Stop();
                long elaspsedMilliseconds = sw1.ElapsedMilliseconds / 1000;
                int seconds = (int)elaspsedMilliseconds;
                TimeUsedStr = Tool.parseTimeSeconds(seconds, 0);
                label_timeused.Text = "扫描用时:" + TimeUsedStr;
                label_state.Text = "状态:" + "扫描完毕!";
                MessageBox.Show("扫描完成，请查看HTML扫描报告！");
            }
        }
        /// <summary>
        /// 扫描域名控制器
        /// </summary>
        private void ScanURLController()
        {
            ArrayList fileList = new ArrayList();
            //config.ini配置文件下的[Link]节点
            this.ini.ReadSectionValues("Link", fileList);
            if (fileList != null && fileList.Count > 0)
            {
                try
                {
                    foreach (string item in fileList)
                    {
                        //过滤非法字符
                        string str = Tool.UnicodeToString(item);
                        Uri urlNew = new Uri(new Uri(https[n].RootUrl), str);
                        string strNew = urlNew.AbsoluteUri.ToString();
                        InjectionPoint IPnew = new InjectionPoint(strNew, false, false, false);
                        https[n].alPossibleInjectionPoints.Add(IPnew);
                    }
                }
                catch (Exception)
                {
                }
            }
            /* 读取Access数据库
            if (textBoxScanFilePath.Text.Substring(textBoxScanFilePath.Text.LastIndexOf('.')) == ".mdb")
            {
                string strConnection = "Provider=Microsoft.Jet.OleDb.4.0;";
                strConnection += @"Data Source=" + textBoxScanFilePath.Text;//这里用的是绝对路径
                try
                {
                    OleDbConnection objConnection = new OleDbConnection(strConnection);
                    OleDbCommand objCommand = new OleDbCommand("select * from Scan_AlonePoints", objConnection);
                    objConnection.Open();
                    OleDbDataReader objDataReader = objCommand.ExecuteReader();
                    while (objDataReader.Read())
                    {
                        Uri urlNew = new Uri(new Uri(https[n].RootUrl), Convert.ToString(objDataReader["AlonePoint"]));
                        string strNew = urlNew.AbsoluteUri.ToString();
                        InjectionPoint IPnew = new InjectionPoint(strNew, false, false, false);
                        https[n].alPossibleInjectionPoints.Add(IPnew);
                    }
                    objConnection.Close();
                }
                catch
                {
                    MessageBox.Show("读取或连接Access数据库错误!");
                    return;
                }
            }
             */
        }

        private void btnHtmlrReport_Click(object sender, EventArgs e)
        {
            OutputHtmlReport();
        }
        /// <summary>
        /// 输出HTML报告
        /// </summary>
        private void OutputHtmlReport()
        {
            if (this.IsWebsiteScan && http != null && http.RootUrl != null)
            {
                StreamReader sr = new StreamReader("./log/" + "tmpl.html", System.Text.Encoding.GetEncoding("GB2312"));
                String htmlcode = sr.ReadToEnd();
                htmlcode = htmlcode.Replace("HTMLReport_timeused", TimeUsedStr);
                htmlcode = htmlcode.Replace("HTMLReport_timegen", DateTime.Now.ToString());
                htmlcode = htmlcode.Replace("HTMLReport_N_sites_all", "1");

                if (http.SecurityLevel == 0)
                {
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_cannot_connect", "1");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_secure", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_notinjectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_notsensitive_injectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_injectable", "0");
                }
                if (!http.IsSensitive && !http.IsInjectable)
                {
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_cannot_connect", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_secure", "1");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_notinjectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_notsensitive_injectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_injectable", "0");
                }
                if (http.IsSensitive && !http.IsInjectable)
                {
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_cannot_connect", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_secure", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_notinjectable", "1");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_notsensitive_injectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_injectable", "0");
                }
                if (!http.IsSensitive && http.IsInjectable)
                {
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_cannot_connect", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_secure", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_notinjectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_notsensitive_injectable", "1");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_injectable", "0");
                }
                if (http.IsSensitive && http.IsInjectable)
                {
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_cannot_connect", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_secure", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_notinjectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_notsensitive_injectable", "0");
                    htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_injectable", "1");
                }
                //write the detail info
                String DetailInfo = "<table width=100% border=0 cellpadding=0 cellspacing=0 align=center>";
                DetailInfo += "<tr align=left>";
                DetailInfo += "<td width=40%>站点URL</td>";
                DetailInfo += "<td><a href='" + http.RootUrl + "'>" + http.RootUrl + "</a></td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td>站点说明</td>";
                DetailInfo += "<td>" + http.Info + "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td>数据库类型</td>";
                DetailInfo += "<td>" + http.DBType + "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td>扫描页面总数</td>";
                DetailInfo += "<td>" + http.N_Pages + "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td>安全页面数</td>";
                DetailInfo += "<td>" + http.N_Pages_secure + "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td>泄漏敏感信息页面数</td>";
                DetailInfo += "<td>" + http.N_Pages_sensitive + "(" + http.N_Pages_sensitive + "/" + http.N_Pages + ")" + "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td>可注入页面数</td>";
                DetailInfo += "<td>" + http.N_Pages_injectable + "(" + http.N_Pages_injectable + "/" + http.N_Pages + ")" + "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td colspan=2>泄漏敏感信息的页面列表</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td colspan=2>";
                foreach (InjectionPoint IP in http.alPossibleInjectionPoints)
                {
                    if (IP.IsSensitive)
                        DetailInfo += "<a href=" + IP.Url + "'>" + IP.Url + "'</a>" + "<br/>";
                }
                DetailInfo += "</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td colspan=2>可实施SQL注入的页面列表</td>";
                DetailInfo += "</tr>";
                DetailInfo += "<tr>";
                DetailInfo += "<td colspan=2>";
                foreach (InjectionPoint IP in http.alPossibleInjectionPoints)
                {
                    if (IP.CanInject)
                        DetailInfo += "<a href='" + IP.Url + "'>" + IP.Url + "</a>" + "<br/>";
                }
                DetailInfo += "</td></tr></table>";
                htmlcode = htmlcode.Replace("HTMLReport_detail", DetailInfo);
                string domain = http.RootUrl.Replace("http://", "").Replace("https://", "").Replace("/", "_").Replace(":", "_").Trim();
                string htmlfilename = domain + DateTime.Now.ToString("_yyyyMMddHHmmssffff") + ".html";
                string logDir = Application.StartupPath + "//log";
                //检查日志目录是否存在
                if (!Directory.Exists(logDir))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(logDir);
                    directoryInfo.Create();
                }
                String HtmlPath = logDir + "//" + htmlfilename;
                StreamWriter sw = new StreamWriter(HtmlPath, false, System.Text.Encoding.GetEncoding("GB2312"));
                sw.Write(htmlcode);
                sw.Flush();
                sw.Close();
                new FormViewReport(Path.GetFullPath(HtmlPath)).Show();
                return;
            }
            if (!this.IsWebsiteScan && n != 0)
            {
                StreamReader sr = new StreamReader("./log/" + "tmpl.html", System.Text.Encoding.GetEncoding("GB2312"));
                String htmlcode = sr.ReadToEnd();
                htmlcode = htmlcode.Replace("HTMLReport_timeused", TimeUsedStr);
                htmlcode = htmlcode.Replace("HTMLReport_timegen", DateTime.Now.ToString());
                htmlcode = htmlcode.Replace("HTMLReport_N_sites_all", n.ToString());
                int N_sites_cannot_connect = 0;
                int N_sites_secure = threadNumber;
                int N_sites_sensitive_notinjectable = 0;
                int N_sites_notsensitive_injectable = 0;
                int N_sites_sensitive_injectable = 0;

                String DetailInfo = "";
                for (int i = 0; i < threadNumber; i++)
                {
                    if (https[i].SecurityLevel == 0)
                    {
                        N_sites_cannot_connect++;
                    }
                    if (https[i].IsSensitive && https[i].IsInjectable)
                    {
                        N_sites_sensitive_injectable++;
                    }
                    if (https[i].IsSensitive && !https[i].IsInjectable)
                    {
                        N_sites_sensitive_notinjectable++;
                    }
                    if (!https[i].IsSensitive && https[i].IsInjectable)
                    {
                        N_sites_notsensitive_injectable++;
                    }
                    //write the detail info
                    DetailInfo += "<table width=100% border=0 cellpadding=0 cellspacing=0 align=center>";
                    DetailInfo += "<tr align=left>";
                    DetailInfo += "<td width=40%>站点URL</td>";
                    DetailInfo += "<td><a href='" + https[i].RootUrl + "'>" + https[i].RootUrl + "</a></td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td>站点说明</td>";
                    DetailInfo += "<td>" + https[i].Info + "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td>数据库类型</td>";
                    DetailInfo += "<td>" + https[i].DBType + "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td>扫描页面总数</td>";
                    DetailInfo += "<td>" + https[i].N_Pages + "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td>安全页面数</td>";
                    DetailInfo += "<td>" + https[i].N_Pages_secure + "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td>泄漏敏感信息页面数</td>";
                    DetailInfo += "<td>" + https[i].N_Pages_sensitive + "(" + https[i].N_Pages_sensitive + "/" + https[i].N_Pages + ")" + "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td>可注入页面数</td>";
                    DetailInfo += "<td>" + https[i].N_Pages_injectable + "(" + https[i].N_Pages_injectable + "/" + https[i].N_Pages + ")" + "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td colspan=2>泄漏敏感信息的页面列表</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td colspan=2>";
                    foreach (InjectionPoint IP in https[i].alPossibleInjectionPoints)
                    {
                        if (IP.IsSensitive)
                            DetailInfo += "<a href=" + IP.Url + "'>" + IP.Url + "'</a>" + "<br/>";
                    }
                    DetailInfo += "</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td colspan=2>注入点列表</td>";
                    DetailInfo += "</tr>";
                    DetailInfo += "<tr>";
                    DetailInfo += "<td colspan=2>";
                    foreach (InjectionPoint IP in https[i].alPossibleInjectionPoints)
                    {
                        if (IP.CanInject)
                            DetailInfo += "<a href='" + IP.Url + "'>" + IP.Url + "</a>" + "<br/>";
                    }
                    DetailInfo += "</td></tr></table><hr/>";
                }
                N_sites_secure = N_sites_secure - N_sites_cannot_connect - N_sites_sensitive_injectable - N_sites_sensitive_notinjectable - N_sites_notsensitive_injectable;

                htmlcode = htmlcode.Replace("HTMLReport_N_sites_cannot_connect", N_sites_cannot_connect.ToString());
                htmlcode = htmlcode.Replace("HTMLReport_N_sites_secure", N_sites_secure.ToString());
                htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_notinjectable", N_sites_sensitive_notinjectable.ToString());
                htmlcode = htmlcode.Replace("HTMLReport_N_sites_notsensitive_injectable", N_sites_notsensitive_injectable.ToString());
                htmlcode = htmlcode.Replace("HTMLReport_N_sites_sensitive_injectable", N_sites_sensitive_injectable.ToString());

                htmlcode = htmlcode.Replace("HTMLReport_detail", DetailInfo);

                string htmlfilename = "rep_" + DateTime.Now.ToString("yyyyMMddHHmmssffff") + ".html";
                string logDir = Application.StartupPath + "//log";
                //检查日志目录是否存在
                if (!Directory.Exists(logDir))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(logDir);
                    directoryInfo.Create();
                }
                String HtmlPath = logDir + "//" + htmlfilename;
                StreamWriter sw = new StreamWriter(HtmlPath, false, System.Text.Encoding.GetEncoding("GB2312"));

                sw.Write(htmlcode);
                sw.Flush();
                sw.Close();
                new FormViewReport(Path.GetFullPath(HtmlPath)).Show();
                return;
            }
        }

        #endregion


        private void txtMultipleUrlPath_TextChanged(object sender, EventArgs e)
        {
            if (radioButtonMultiple.Checked == false)
            {
                radioButtonMultiple.Checked = true;
            }
            else
            {
                radioButtonMultiple.Checked = false;
            }
        }


        #region//web服务器安全检测事件
        private int a = 0;
        private int b = 0;
        private int c = 0;
        private bool stop = false;

        private void picWebsiteDir_Click(object sender, EventArgs e)
        {
            if (folder1.ShowDialog() == DialogResult.OK)
            {
                txtWebsiteDirPath.Text = folder1.SelectedPath;
            }
        }

        private void btKill_Click(object sender, EventArgs e)
        {
            if (txtWebsiteDirPath.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请先选择网站所在目录", "提示");
                return;
            }
            try
            {
                if (btnKill.Text == "开始查杀")
                {
                    a = 0;
                    b = 0;
                    btend();
                    picWebsiteDir.Enabled = false;
                    listView1.Items.Clear();
                    stop = false;
                    btnKill.Text = "停止查杀";
                    Thread.Sleep(22);
                    Thread T = new Thread(new ThreadStart(KillProcess));
                    T.Start();
                }
                else
                {
                    stop = true;
                    btnKill.Text = "开始查杀";

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误提示！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btstart();
            }
        }
        public delegate void setListView(string filename, string aaa, string filedir, string filetime, string filesize);//更新listview的委托
        public delegate void setbtkill(string text);//更新btkill按钮的委托
        //更新按钮的方法
        private void addbtkill(string text)
        {
            btnKill.Text = text;

        }
        //更新listview方法
        private void addlist(string filename, string aaa, string filedir, string filetime, string filesize)
        {
            ListViewItem item = new ListViewItem();
            item = listView1.Items.Add(filename);
            item.SubItems.Add(aaa);
            item.SubItems.Add(filedir);
            item.SubItems.Add(filetime);
            item.SubItems.Add(filesize);
            c = c + 1;
        }
        /// <summary>
        /// 查杀过程
        /// </summary>
        private void KillProcess()
        {
            DirectoryItem(txtWebsiteDirPath.Text);
            if (btnKill.InvokeRequired)
            {
                setbtkill ss = new setbtkill(addbtkill);
                this.BeginInvoke(ss, new object[] { "开始查杀" });
            }
            else
            {
                btnKill.Text = "开始查杀";
            }
            btstart();
            tssLabelKillResult.Text = "总共检测了" + a.ToString() + "个文件夹和" + b.ToString() + "个文件，有" + c.ToString() + "个可疑文件！";
        }
        private void DirectoryItem(string dir)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                DirectoryInfo[] dis = di.GetDirectories();
                FileInfoItem(di.GetFiles(), dir);
                foreach (DirectoryInfo d in dis)
                {
                    if (stop)
                    {
                        break;
                    }
                    else
                    {
                        a = a + 1;
                        DirectoryItem(d.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                tssLabelKillResult.Text = ex.Message;
                btstart();
            }

        }
        private void FileInfoItem(FileInfo[] filelist, string dir)
        {
            try
            {
                string strdir = "";
                foreach (FileInfo f in filelist)
                {
                    if (stop)
                    {
                        break;
                    }
                    else
                    {
                        string ext = f.Extension.ToLower();
                        if (f.Length < 502400 && ext != ".rar" && (ext == ".asp" || ext == ".asa" || ext == ".cdx" || ext == ".cer"))
                        {
                            b = b + 1;
                            strdir = dir + "\\" + f.Name;
                            duoekill.CheckVirus cv = new duoekill.CheckVirus();
                            using (StreamReader sr = new StreamReader(f.FullName, Encoding.Default))
                            {
                                string s = sr.ReadToEnd();
                                tssLabelKillResult.Text = "正在扫描分析文件：" + strdir;
                                string cvs = cv.CheckString(s);
                                if (cvs != "")
                                {
                                    setListView slv = new setListView(addlist);
                                    listView1.Invoke(slv, new object[] { f.Name, cvs, strdir, f.LastAccessTime.ToString(), (f.Length / 1024).ToString() });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tssLabelKillResult.Text = ex.Message;
                btstart();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string str = listView1.SelectedItems[0].SubItems[2].Text;
            System.Diagnostics.Process.Start(str.Substring(0, str.LastIndexOf("\\") + 1));
        }



        private void btend()
        {
            picWebsiteDir.Enabled = false;
            btSeleltALL.Enabled = false;
            btzseleall.Enabled = false;
            btdelete.Enabled = false;
            picFileDir.Enabled = false;
            btZall2.Enabled = false;
            btClear.Enabled = false;
            btselectAll2.Enabled = false;
        }
        private void btstart()
        {
            picWebsiteDir.Enabled = true;
            btSeleltALL.Enabled = true;
            btzseleall.Enabled = true;
            btdelete.Enabled = true;
            picFileDir.Enabled = true;
            btZall2.Enabled = true;
            btClear.Enabled = true;
            btselectAll2.Enabled = true;
        }

        private void btSeleltALL_Click(object sender, EventArgs e)
        {
            selectlistview(true);
        }

        private void btzseleall_Click(object sender, EventArgs e)
        {
            selectlistview(false);
        }
        private void selectlistview(bool b)
        {
            foreach (ListViewItem item in listView1.Items)
            {
                item.Checked = b;
            }
        }

        private void btdelete_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("确认要删除选定的吗？查杀的文件并不一定都是木马，又可能是网站所需的文件！\r\n如果你选择【备份删除文件】文件删除后将将在原目录做备份，原文件名后面加.bak！", "友情提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                a = 0;
                string str = "";
                foreach (ListViewItem item in listView1.CheckedItems)
                {
                    str = item.SubItems[2].Text;
                    if (cbbak.Checked)
                    {
                        File.Copy(str, str + ".bak");
                    }
                    File.Delete(str);
                    listView1.Items.Remove(item);
                    a = a + 1;
                }
                if (cbbak.Checked)
                {
                    tssLabelKillResult.Text = "  总共删除了" + a.ToString() + "个文件，并在原目录做了相应备份，可去除.bak恢复";
                }
                else
                {
                    tssLabelKillResult.Text = "  总共删除了" + a.ToString() + "个文件，没有备份，将无法恢复！";
                }
            }
            else
            {
                return;
            }
        }

        private void picFileDir_Click(object sender, EventArgs e)
        {
            if (folder1.ShowDialog() == DialogResult.OK)
            {
                txtFileDirPath.Text = folder1.SelectedPath;
            }
        }

        private void btnClean_Click(object sender, EventArgs e)
        {
            if (txtFileDirPath.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请先选择网站所在目录", "提示");
                return;
            }
            try
            {
                if (btnClean.Text == "开始清除")
                {
                    a = 0;
                    b = 0;
                    c = 0;
                    btend();
                    listView2.Items.Clear();
                    btnClean.Text = "停止";
                    stop = false;
                    Thread t = new Thread(new ThreadStart(CleanMuma));
                    t.Start();
                }
                else
                {
                    stop = true;
                    btnClean.Text = "开始清除";
                    btstart();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误提示！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btstart();
            }
        }
        /// <summary>
        ///清楚木马
        /// </summary>
        private void CleanMuma()
        {
            killmumas(txtFileDirPath.Text);
            btnClean.Text = "开始清除";
            tssLabelKillResult.Text = "总共检测了" + a.ToString() + "个文件夹和" + b.ToString() + "个文件，有" + c.ToString() + "个可疑文件！";
            btstart();
        }
        private void killmumas(string txt)//搜索所以文件夹
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(txt);
                DirectoryInfo[] dis = di.GetDirectories();
                killmumafile(di.GetFiles());
                foreach (DirectoryInfo d in dis)
                {
                    a += 1;
                    if (stop)
                    {
                        break;
                    }
                    else
                    {
                        killmumas(d.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误提示！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btstart();
            }

        }
        private void killmumafile(FileInfo[] file)//搜索所有文件
        {
            try
            {
                foreach (FileInfo f in file)
                {
                    if (stop)
                    {
                        break;
                    }
                    else
                    {
                        string filelist = f.FullName;
                        bool extt = false;
                        string[] exts = txtType.Text.Split(new char[] { '|' });
                        foreach (string ext in exts)
                        {
                            if (f.Extension.ToLower() == ext)
                            {
                                extt = true;
                                break;
                            }
                        }
                        if (extt)
                        {
                            duoekill.command cmd = new duoekill.command();
                            b += 1;
                            int filesize = 500;
                            if (cbsize.Checked)
                            {
                                filesize = 200;
                            }
                            if ((f.Length / 1024) < filesize)
                            {
                                string s = cmd.ReadContent(f.FullName);
                                string[] texts = cmd.RexStr(s, txtRule.Text).Split(new char[] { '|' });
                                foreach (string text in texts)
                                {
                                    if (text.Length > 1)
                                    {
                                        if (cbdi.Checked)
                                        {
                                            if (cmd.RexStr(text))
                                            {
                                                c += 1;
                                                ListViewItem item = listView2.Items.Add(f.Name);
                                                item.SubItems.Add(text);
                                                item.SubItems.Add(f.FullName);
                                                item.SubItems.Add("危险文件");
                                            }
                                        }
                                        else
                                        {
                                            c += 1;
                                            ListViewItem item = listView2.Items.Add(f.Name);
                                            item.SubItems.Add(text);
                                            item.SubItems.Add(f.FullName);
                                            if (cmd.RexStr(text))
                                            {
                                                item.SubItems.Add("危险文件");
                                            }
                                            else
                                            {
                                                item.SubItems.Add("低危险文件");
                                            }
                                        }
                                    }
                                }
                                tssLabelKillResult.Text = "正在扫描分析文件：" + filelist;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误提示！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btstart();
            }
        }

        private void btselectAll2_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.Items)
            {
                item.Checked = true;
            }
        }

        private void btZall2_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem item in listView2.Items)
            {
                item.Checked = false;
            }
        }

        private void btClear_Click(object sender, EventArgs e)
        {
            Thread t = new Thread(new ThreadStart(ReplaceFile));
            t.Start();
        }

        private void ReplaceFile()
        {
            try
            {
                c = 0;
                duoekill.command cmd = new duoekill.command();
                string str = "";
                foreach (ListViewItem item in listView2.CheckedItems)
                {

                    c += 1;
                    str = cmd.ReadContent(item.SubItems[2].Text);
                    str = str.Replace(item.SubItems[1].Text, "");
                    cmd.WriteContent(item.SubItems[2].Text, str);
                    listView2.Items.Remove(item);
                }
                tssLabelKillResult.Text = "已经清除了" + c + "个挂马内容！";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "错误提示！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btstart();
            }
        }
        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            this.listView1.ListViewItemSorter = new duoekill.ListViewItemComparer(e.Column, SortOrder.Descending);
            listView1.Sorting = SortOrder.Descending;
            listView1.Sort();
        }

        #endregion

        private void 退出XToolStripMenuItem_Click(object sender, EventArgs e) //退出事件
        {
            try
            {
                this.Dispose();
                Application.ExitThread();
                threadScanAdmin.Abort(); //停止扫描
                threadSqlScan.Abort();
                threads[0].Abort();
            }
            catch (Exception)
            {
                this.Close();
                Application.ExitThread();
                Application.Exit();

            }

        }

        private void 关于AboutAToolStripMenuItem_Click(object sender, EventArgs e) //关于事件
        {
            FormAbout fabout = new FormAbout();
            fabout.Show();
        }


        private void 软件注册RToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormReg freg = new FormReg();
            freg.Show();
        }

        #region //密码探测模块
        String Speacl = "";  //用来保存特殊特征码字符串
        Thread threadPass;
        private void btnPassScanStart_Click(object sender, EventArgs e)
        {
            string url = txtUrl.Text.Trim();
            if (!Tool.IsDomain(url))
            {
                MessageBox.Show("请输入正确的提交网址\r\n(以http://或者https://开头的域名)", "提示");
                txtUrl.Focus();
                return;
            }
            if (txtUserName.Text.Trim() == string.Empty)
            {
                MessageBox.Show("请输入用户名");
                txtUserName.Focus();
                return;
            }

            threadPass = new Thread(new ThreadStart(PassScan));

            try
            {
                threadPass.Start();
                btnPassScanStop.Enabled = true;
            }
            catch
            {
                btnPassScanStop.Enabled = false;
            }


        }

        //垂直滚动条发送滚动的消息开始
        [DllImport("User32.dll")]
        private static extern Int32 SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        public const int WM_VSCROLL = 0x0115;//垂直滚动条消息
        public const int SB_LINEDOWN = 1;//向下滚动一行
        public const int SB_PAGEDOWN = 3;//向下滚动一页
        public const int SB_BOTTOM = 7;//滚动到最底部
        //垂直滚动条发送滚动的消息结束

        /// <summary>
        /// 密码探测试委托事件
        /// </summary>
        public void PassScan()
        {
            WebClient web = new WebClient();
            NameValueCollection values = new NameValueCollection();
            //常用弱密码集合
            string[] passArray = { "123456", "123456789", "111111", "5201314", "12345678", "123123", "aaa111", "1314520", "123321", "7758521", "1234567", "5211314", "666666", "520520", "woaini", "520131", "11111111", "888888", "hotmail.com", "112233", "123654", "654321", "1234567890", "a123456", "88888888", "163.com", "000000", "yahoo.com.cn", "sohu.com", "yahoo.cn", "111222tianya", "163.COM", "tom.com", "139.com", "wangyut2", "pp.com", "yahoo.com", "147258369", "123123123", "147258", "987654321", "100200", "zxcvbnm", "123456a", "521521", "7758258", "111222", "110110", "1314521", "11111111", "12345678", "a321654", "111111", "123123", "5201314", "00000000", "q123456", "123123123", "aaaaaa", "a123456789", "qq123456", "11112222", "woaini1314", "a123123", "a111111", "123321", "a5201314", "z123456", "liuchang", "a000000", "1314520", "asd123", "88888888", "1234567890", "7758521", "1234567", "woaini520", "147258369", "123456789a", "woaini123", "q1q1q1q1", "a12345678", "qwe123", "123456q", "121212", "asdasd", "999999", "1111111", "123698745", "137900", "159357", "iloveyou", "222222", "31415926", "123456", "111111", "123456789", "123123", "9958123", "woaini521", "5201314", "18n28n24a5", "abc123", "password", "123qwe", "123456789", "12345678", "11111111", "dearbook", "00000000", "123123123", "1234567890", "88888888", "111111111", "147258369", "987654321", "aaaaaaaa", "1111111111", "66666666", "a123456789", "11223344", "1qaz2wsx", "xiazhili", "789456123", "password", "87654321", "qqqqqqqq", "000000000", "qwertyuiop", "qq123456", "iloveyou", "31415926", "12344321", "0000000000", "asdfghjkl", "1q2w3e4r", "123456abc", "0123456789", "123654789", "12121212", "qazwsxedc", "abcd1234", "12341234", "110110110", "asdasdasd", "123456", "22222222", "123321123", "abc123456", "a12345678", "123456123", "a1234567", "1234qwer", "qwertyui", "123456789a", "qq.com", "369369", "163.com", "ohwe1zvq", "xiekai1121", "19860210", "1984130", "81251310", "502058", "162534", "690929", "601445", "1814325", "as1230", "zz123456", "280213676", "198773", "4861111", "328658", "19890608", "198428", "880126", "6516415", "111213", "195561", "780525", "6586123", "caonima99", "168816", "123654987", "qq776491", "hahabaobao", "198541", "540707", "leqing123", "5403693", "123456", "123456789", "111111", "5201314", "123123", "12345678", "1314520", "123321", "7758521", "1234567", "5211314", "520520", "woaini", "520131", "666666", "RAND#a#8", "hotmail.com", "112233", "123654", "888888", "654321", "1234567890", "a123456", "admin", "admin123", "admin888", "aa112233", "qaz111", "qaz123", "qazwsx123", "qq1234" };
            for (int i = 0; i < passArray.Length; i++)
            {
                String Content = passArray[i];
                values.Add("username", this.txtUserName.Text.Trim());
                values.Add("password", Content);
                String ReturnMess = "";
                try
                {
                    byte[] byRemoteInfo = web.UploadValues(this.txtUrl.Text, values);
                    ReturnMess = Encoding.Default.GetString(byRemoteInfo);
                    listBoxPassProcess.Items.Add("正在校验 帐号：" + this.txtUserName.Text + " 密码：" + Content);
                    Thread.Sleep(50);
                    SendMessage(listBoxPassProcess.Handle, WM_VSCROLL, SB_LINEDOWN, 0); //向listBox1的垂直滚动条发送滚动的消息
                    if (ReturnMess.IndexOf(this.Speacl) > 0)
                    {

                        listBoxPassResult.Items.Add("密码找到了! 帐号：" + this.txtUserName.Text + " 密码：" + Content);
                        break;
                    }
                }
                catch (Exception)
                {
                    threadPass.Abort();
                }
                web.Dispose();
                values.Clear();
            }
        }

        private void btnPassScanStop_Click(object sender, EventArgs e) //密码探测停止线程事件
        {
            btnPassScanStop.Enabled = false;
            threadPass.Abort();
        }
        #endregion

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FormAccess faccess = new FormAccess();
            faccess.Show();
        }
        #region //后台扫描事件开始
        Thread threadScanAdmin;
        private void btnAdminScanStart_Click(object sender, EventArgs e)
        {
            string url = txtAdminUrl.Text.Trim();
            if (!Tool.IsDomain(url))
            {
                MessageBox.Show("请输入正确的后台网址\r\n(以http://或者https://开头的域名)", "提示");
                return;
            }
            threadScanAdmin = new Thread(new ThreadStart(AdminDataFilling));
            try
            {
                threadScanAdmin.Start();//开始扫描
                btnAdminScanStop.Enabled = true;
                btnAdminScanStart.Enabled = false;
                listBoxAdminUrlShow.Items.Clear();
                listBoxAdminScanResult.Items.Clear();
            }
            catch
            { }
        }
        /// <summary>
        /// 检测网址是否有效委托
        /// </summary>
        public void CheckWebsite(string url)
        {
            System.Net.WebResponse myRepTest;
            System.Net.WebRequest myTest = System.Net.WebRequest.Create(url);
            myTest.Timeout = 5000;
            groupBoxAdminResult.Text = "正在扫描：" + url;
            try
            {
                myRepTest = myTest.GetResponse();
                listBoxAdminScanResult.Items.Add(url);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 将预设的[后台路径集合]填充到[扫描后台]的委托事件中
        /// </summary>
        public void AdminDataFilling()
        {
            ArrayList fileList = new ArrayList();
            //config.ini配置文件下的[Admin]节点
            this.ini.ReadSectionValues("Admin", fileList);
            if (fileList != null && fileList.Count > 0)
            {
                try
                {
                    foreach (string item in fileList)
                    {
                        //过滤非法字符
                        string str = Tool.UnicodeToString(item);
                        string url = txtAdminUrl.Text.Trim() + str;
                        listBoxAdminUrlShow.Items.Add(url);
                        //检测网址是否有效
                        CheckWebsite(url);
                        //Thread.Sleep(100);
                        SendMessage(listBoxAdminUrlShow.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void btnAdminScanStop_Click(object sender, EventArgs e) //后台停止扫描事件
        {
            threadScanAdmin.Abort(); //停止扫描
            btnAdminScanStart.Enabled = true;
            btnAdminScanStop.Enabled = false;
        }
        #endregion


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                txtSingleUrl.Text = listBox1.SelectedItem.ToString(); //传值到textbox
                txtAdminUrl.Text = listBox1.SelectedItem.ToString(); //传值到textbox
                txtUrl.Text = listBox1.SelectedItem.ToString(); //传值到textbox
                tabControlMain.SelectedIndex = 1;
                radioButtonSingle.Checked = true; //为可选
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void btnSqlScanStop_Click(object sender, EventArgs e) //sql停止扫描事件
        {
            threadSqlScan.Abort();

            btnSqlScanStart.Enabled = true;
            btnSqlScanStop.Enabled = false;
        }
        /// <summary>
        /// 加载更新程序
        /// </summary>
        private void LoadUpdateFile()
        {
            string appPath = Environment.CurrentDirectory + "\\在线更新.exe";
            if (!File.Exists(appPath))
            {
                FileStream str = new FileStream(appPath, FileMode.Create);
                str.Write(global::Php.Properties.Resources.在线更新, 0, global::Php.Properties.Resources.在线更新.Length);
            }
        }
        /// <summary>
        /// 加载Html模板
        /// </summary>
        private void LoadHtmlTemplate()
        {
            //设置释放路径
            string logDir = Application.StartupPath + "//log";
            //检查日志目录是否存在
            if (!Directory.Exists(logDir))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(logDir);
                directoryInfo.Create();
            }
            string strPath = logDir + "/tmpl.html";
            if (!File.Exists(strPath))
            {
                //获取嵌入文件的字节数组
                string html = global::Php.Properties.Resources.tmpl;
                //string转byte[]
                byte[] bytes = System.Text.Encoding.Default.GetBytes(html);
                //创建文件（覆盖模式）  
                using (FileStream fs = new FileStream(strPath, FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }
        /// <summary>
        /// 加载更新配置文件
        /// </summary>
        private void LoadXmlFile()
        {
            string xmlPath = Environment.CurrentDirectory + "\\UpdateList.xml";
            if (!File.Exists(xmlPath))
            {
                //获取嵌入文件的字节数组
                string xml = global::Php.Properties.Resources.UpdateList;
                //string转byte[]
                byte[] bytes = System.Text.Encoding.Default.GetBytes(xml);
                //创建文件（覆盖模式）  
                using (FileStream fs = new FileStream(xmlPath, FileMode.Create))
                {
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }
        private void 自动升级ToolStripMenuItem_Click(object sender, EventArgs e) //自动升级事件
        {
            //加载更新程序
            LoadUpdateFile();
            try
            {
                //启动程序
                System.Diagnostics.Process.Start(Environment.CurrentDirectory + "\\在线更新.exe");
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("正在下载更新程序，稍候请重试!", "提示");
            }
        }

        private void listView_InjectionPoints_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                txtSQLin.Text = listViewScanReport.SelectedItems[0].SubItems[0].Text; //传值到textbox
                this.tabControlMain.SelectedIndex = 2;
            }
            catch (Exception)
            {
            }

        }

        private void radioButton＿WebsiteAdd_CheckedChanged_1(object sender, EventArgs e)
        {
            if (radioButtonSingle.Checked == true)
            {
                radioButtonMultiple.Checked = false;
            }
            else
            {
                radioButtonSingle.Checked = false;
            }
        }

        private void radioButton＿WebsitesAdd_CheckedChanged_1(object sender, EventArgs e)
        {
            if (radioButtonMultiple.Checked == true)
            {
                radioButtonSingle.Checked = false;
            }
            else
            {
                radioButtonMultiple.Checked = false;
            }
        }

        #region //MD5模块事件

        private void btnEncrypt_Click(object sender, EventArgs e) //加密按钮
        {
            if (txtEncryptBright.Text.Trim() == string.Empty)
            {
                MessageBox.Show("明文不能为空", "提示");
                txtEncryptBright.Focus();
                return;
            }
            txtEncryptMd5.Text = "";
            byte[] bt = UTF8Encoding.UTF8.GetBytes(txtEncryptBright.Text);//UTF8需要对Text的引用
            MD5CryptoServiceProvider objMD5;
            objMD5 = new MD5CryptoServiceProvider();
            byte[] output = objMD5.ComputeHash(bt);
            string str = BitConverter.ToString(output);
            string[] str1 = str.Split('-');
            foreach (string a in str1)
            {
                txtEncryptMd5.Text = txtEncryptMd5.Text + a;
            }
        }

        private void btnTransfer_Click(object sender, EventArgs e) //传值按钮
        {
            txtDecryptMd5.Text = txtEncryptMd5.Text;
        }
        Thread thmd5;
        private void btnCrackStart_Click(object sender, EventArgs e) //反向破解按钮
        {
            if (txtDecryptMd5.Text.Trim() == string.Empty)
            {
                MessageBox.Show("Md5码不能为空", "提示");
                txtDecryptMd5.Focus();
                return;
            }
            this.listViewMd5Result.Visible = true;
            btnCrackStart.Enabled = false;
            btnCrackStop.Enabled = true;
            listBoxPassResult.Items.Clear();
            thmd5 = new Thread(new ThreadStart(PoJie));
            thmd5.Start();
        }
        /// <summary>
        /// 反向破解
        /// </summary>
        void PoJie()
        {
            //常用弱密码集合
            string[] passArray = { "123456", "123456789", "111111", "5201314", "12345678", "123123", "aaa111", "1314520", "123321", "7758521", "1234567", "5211314", "666666", "520520", "woaini", "520131", "11111111", "888888", "hotmail.com", "112233", "123654", "654321", "1234567890", "a123456", "88888888", "163.com", "000000", "yahoo.com.cn", "sohu.com", "yahoo.cn", "111222tianya", "163.COM", "tom.com", "139.com", "wangyut2", "pp.com", "yahoo.com", "147258369", "123123123", "147258", "987654321", "100200", "zxcvbnm", "123456a", "521521", "7758258", "111222", "110110", "1314521", "11111111", "12345678", "a321654", "111111", "123123", "5201314", "00000000", "q123456", "123123123", "aaaaaa", "a123456789", "qq123456", "11112222", "woaini1314", "a123123", "a111111", "123321", "a5201314", "z123456", "liuchang", "a000000", "1314520", "asd123", "88888888", "1234567890", "7758521", "1234567", "woaini520", "147258369", "123456789a", "woaini123", "q1q1q1q1", "a12345678", "qwe123", "123456q", "121212", "asdasd", "999999", "1111111", "123698745", "137900", "159357", "iloveyou", "222222", "31415926", "123456", "111111", "123456789", "123123", "9958123", "woaini521", "5201314", "18n28n24a5", "abc123", "password", "123qwe", "123456789", "12345678", "11111111", "dearbook", "00000000", "123123123", "1234567890", "88888888", "111111111", "147258369", "987654321", "aaaaaaaa", "1111111111", "66666666", "a123456789", "11223344", "1qaz2wsx", "xiazhili", "789456123", "password", "87654321", "qqqqqqqq", "000000000", "qwertyuiop", "qq123456", "iloveyou", "31415926", "12344321", "0000000000", "asdfghjkl", "1q2w3e4r", "123456abc", "0123456789", "123654789", "12121212", "qazwsxedc", "abcd1234", "12341234", "110110110", "asdasdasd", "123456", "22222222", "123321123", "abc123456", "a12345678", "123456123", "a1234567", "1234qwer", "qwertyui", "123456789a", "qq.com", "369369", "163.com", "ohwe1zvq", "xiekai1121", "19860210", "1984130", "81251310", "502058", "162534", "690929", "601445", "1814325", "as1230", "zz123456", "280213676", "198773", "4861111", "328658", "19890608", "198428", "880126", "6516415", "111213", "195561", "780525", "6586123", "caonima99", "168816", "123654987", "qq776491", "hahabaobao", "198541", "540707", "leqing123", "5403693", "123456", "123456789", "111111", "5201314", "123123", "12345678", "1314520", "123321", "7758521", "1234567", "5211314", "520520", "woaini", "520131", "666666", "RAND#a#8", "hotmail.com", "112233", "123654", "888888", "654321", "1234567890", "a123456", "admin", "admin123", "admin888", "aa112233", "qaz111", "qaz123", "qazwsx123", "qq1234" };
            for (int i = 0; i < passArray.Length; i++)
            {
                string pass = "";
                txtDecryptBright.Text = "";
                byte[] bytes = UTF8Encoding.UTF8.GetBytes(passArray[i]);//UTF8需要对Text的引用
                MD5CryptoServiceProvider objMD5 = new MD5CryptoServiceProvider();
                byte[] output = objMD5.ComputeHash(bytes);
                string str = BitConverter.ToString(output);
                string[] strArray = str.Split('-');
                foreach (string item in strArray)
                {
                    pass = pass + item;
                }
                listViewMd5Result.Items.Add(pass);
                SendMessage(listViewMd5Result.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                //判断Md5是否匹配正确（转为大写再比较）
                string md5 = txtDecryptMd5.Text.ToUpper();
                if (pass.Equals(md5))
                {
                    txtDecryptBright.Text = passArray[i];
                    Thread.Sleep(1000);
                    btnCrackStart.Enabled = true;
                    btnCrackStop.Enabled = false;
                    thmd5.Abort();


                }
            }
        }
        private void btnCrackStop_Click(object sender, EventArgs e) //停止按钮
        {
            try
            {
                thmd5.Abort();
                //this.listView3.Visible = false;
                btnCrackStart.Enabled = true;
                btnCrackStop.Enabled = false;
            }
            catch
            {
                MessageBox.Show("线程未开始");
            }
        }

        private void btnImportDictionary_Click(object sender, EventArgs e) //导入字典按钮
        {
            OpenFileDialog fdlg = new OpenFileDialog();
            fdlg.Title = "Open File Dialog";
            fdlg.InitialDirectory = Path.GetFullPath("./");
            fdlg.Filter = "Txt files (*.txt)|*.txt";
            fdlg.FilterIndex = 1;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                txtDictionaryPath.Text = fdlg.FileName;
            }
        }
        /// <summary>
        /// 加载字典
        /// </summary>
        void LoadDictionary()
        {
            string path = txtDictionaryPath.Text;
            StreamReader result = new StreamReader(@path);
            while (!result.EndOfStream)
            {
                string password = "";
                txtDictionaryBright.Text = "";
                //获取字典里的字
                string word = result.ReadLine();
                byte[] bytes = UTF8Encoding.UTF8.GetBytes(word.ToString());//UTF8需要对Text的引用             
                MD5CryptoServiceProvider objMD5 = new MD5CryptoServiceProvider();
                byte[] output = objMD5.ComputeHash(bytes);
                string str = BitConverter.ToString(output);
                string[] strArray = str.Split('-');
                foreach (string item in strArray)
                {
                    password = password + item;
                }
                listViewMd5Result.Items.Add(password);
                SendMessage(listViewMd5Result.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                //判断Md5是否匹配正确（转为大写再比较）
                string md5 = txtDictionaryMd5.Text.ToUpper();
                if (password.Equals(md5))
                {
                    txtDictionaryBright.Text = word.ToString();
                    Thread.Sleep(1000);
                    btnDictionaryStart.Enabled = false;
                    btnDictionaryStop.Enabled = true;
                    threadMd5.Abort();
                }
            }
        }
        Thread threadMd5;
        private void btnDictionaryStart_Click(object sender, EventArgs e) //字典破解按钮
        {
            if (txtDictionaryPath.Text.Trim() == "")
            {
                MessageBox.Show("请导入字典文件");
            }
            else
                if (txtDictionaryMd5.Text.Trim() == string.Empty)
                {
                    MessageBox.Show("Md5码不能为空", "提示");
                    txtDictionaryMd5.Focus();
                    return;
                }
            {
                try
                {
                    this.listViewMd5Result.Visible = true;
                    listBoxPassResult.Items.Clear();
                    threadMd5 = new Thread(new ThreadStart(LoadDictionary));
                    threadMd5.Start();
                    btnDictionaryStart.Enabled = false;
                    btnDictionaryStop.Enabled = true;
                }
                catch
                {
                    MessageBox.Show("请导入正确的字典文件");
                }
            }
        }

        private void btnDictionaryStop_Click(object sender, EventArgs e) //停止按钮
        {
            try
            {
                threadMd5.Abort();
                btnDictionaryStart.Enabled = true;
                btnDictionaryStop.Enabled = false;
            }
            catch
            {
                MessageBox.Show("线程未开始");
            }
        }

        private void listBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.webBrowser1.Url = new Uri(listBoxAdminScanResult.Text.ToString());
            this.tabControlMain.SelectedIndex = 0;
        }
        #endregion

        private void 导出列表OToolStripMenuItem_Click(object sender, EventArgs e) //导出列表事件
        {
            string result = listBox1.Text;
            System.Windows.Forms.FolderBrowserDialog folder = new System.Windows.Forms.FolderBrowserDialog();
            if (folder.ShowDialog() == DialogResult.OK)
            {
                string path = folder.SelectedPath;
                #region 文本写入
                try
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < listBox1.Items.Count; i++)
                    {
                        sb.Append(listBox1.Items[i].ToString() + "\r\n");
                    }
                    string content = sb.ToString().Trim();
                    if (content.Length > 0)
                    {
                        using (StreamWriter sw = new StreamWriter(path + "\\域名列表.txt", false))
                        {
                            sw.Write(content);
                        }
                    }
                    MessageBox.Show("列表导出成功！", "提示");
                }
                catch (Exception)
                {
                }
                #endregion

            }


        }

        private void 导入列表DToolStripMenuItem_Click(object sender, EventArgs e) //导入列表事件
        {
            try
            {
                listBox1.Items.Clear(); //先清除
                //获取文件和路径名 一起显示在 txtbox 控件里
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Open File Dialog";
                dialog.InitialDirectory = Path.GetFullPath("./");
                dialog.Filter = "txt files (*.txt)|*.txt";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                bool flag = false;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string path = dialog.FileName;
                    StreamReader sr = new StreamReader(path, Encoding.GetEncoding("gb2312"));//读取文件
                    string str = null;
                    while ((str = sr.ReadLine()) != null)//判断行
                    {
                        if (Tool.IsDomain(str))
                        {
                            flag = true;
                            listBox1.Items.Add(str);
                        }
                    }
                    if (flag)
                    {
                        MessageBox.Show("域名导入成功！", "提示");
                    }
                    else
                    {
                        MessageBox.Show("导入失败，请检查域名格式是否正确\r\n(以http://或者https://开头的域名)", "提示");
                    }
                }
            }
            catch (Exception)
            {
            }
        }


        private void textBox14_TextChanged(object sender, EventArgs e)
        {

        }

        Thread threadPortScan;
        private void btnIpStart_Click(object sender, EventArgs e)
        {
            this.rtbIpResult.Text = null;
            IP_start = this.txtIpStart.Text.Trim();
            if (IP_start == string.Empty)
            {
                MessageBox.Show("请输入起始IP地址", "提示");
                return;
            }
            this.rtbIpResult.Text = "正在进行扫描，请稍候......\n";
            threadPortScan = new Thread(new ThreadStart(runs));

            threadPortScan.Start();
            btnIpStart.Enabled = false;
            btnIpStop.Enabled = true;
        }
        public void runs()
        {
            while (true) //循环计算出IP
            {
                string IP_stop = this.txtIpStop.Text.Trim();
                if (IP_stop == string.Empty)
                {
                    IP_stop = IP_start;
                    this.txtIpStop.Text = IP_start;
                }
                if (!IP_start.Equals(this.txtIpStop.Text))//当初始地址不等于结束地址的时候，继续操作; 如果相等就结束循环
                {
                    Startscan(IP_start);
                    ipstart_get(IP_start);
                    IPAdd();
                    IP_start = ip1.ToString() + "." + ip2.ToString() + "." + ip3.ToString() + "." + ip4.ToString();
                }
                else
                {
                    Startscan(IP_start);
                    break;
                }

            }
        }
        public void Startscan(string yy)
        {
            string strPort = cbPort.Text.Trim();
            if (string.IsNullOrEmpty(strPort))
            {
                return;
            }
            Int32 port = Convert.ToInt32(strPort);
            try
            {
                TcpClient tcp = new TcpClient(); //去连接指定IP的指定端口  如果成功  就显示 端口开放  不成功就抛出异常 端口未开放
                tcp.Connect(this.txtIpStart.Text, port);
                this.rtbIpResult.AppendText(IP_start + "   端口：" + port.ToString() + "开放\n");
            }
            catch
            {
                this.rtbIpResult.AppendText(IP_start + "  端口：" + port.ToString() + "未开放\n");

            }
        }
        public void ipstart_get(string ff)
        {
            IpArray = ff.Split('.');
            ip1 = Convert.ToInt16(IpArray[0].ToString().Trim());
            ip2 = Convert.ToInt16(IpArray[1].ToString().Trim());
            ip3 = Convert.ToInt16(IpArray[2].ToString().Trim());
            ip4 = Convert.ToInt16(IpArray[3].ToString().Trim());
        }

        public void IPAdd()   //这个算法的用处是：将你的IP 增加1  如果你将此方法用 for循环的话，你可以循环出指定IP段的所有IP地址
        {
            if (++ip4 > 255)
            {
                ip3++;
                ip4 = 1;
            }

            if (ip3 > 255)
            {
                ip2++;
                ip3 = 1;
            }

            if (ip2 > 255)
            {
                ip1++;
                ip2 = 1;
            }

            if (ip1 > 255)
            {
                ip1 = 1;
            }
        }

        private void btnIpStop_Click(object sender, EventArgs e)
        {
            btnIpStop.Enabled = false;
            btnIpStart.Enabled = true;
            threadPortScan.Abort(); //停止线程
        }

        public bool GetPage(string url)
        {
            try
            {
                // 值临时变量 r
                bool r = false;
                // 对指定的 URL 创建 HttpWebRequest 对象
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                // 发送 HttpWebRequest 并等待回应
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                // 检测 HttpWebRequest 当为 HttpStatusCode.OK 时，设置临时变量为 true
                if (myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                    r = true;
                // 释放 HttpWebRequest 使用的资源
                myHttpWebResponse.Close();
                // 函数返回临时变量 r
                return r;
            }
            catch (Exception)
            {
                //捕捉到Exception 时函数返回 false。
                return false;
            }
        }

        private void btnInScanStart_Click(object sender, EventArgs e) //开始扫描 
        {
            string url = txtSQLin.Text.Trim();
            if (!Tool.IsDomain(url))
            {
                MessageBox.Show("请输入正确的注入点网址\r\n(以http://或者https://开头的域名)", "提示");
                return;
            }
            if (this.GetPage(txtSQLin.Text + "%20and%201=1"))
            {
                listBoxInScanProcess.Items.Clear();
                listBoxInScanProcess.Items.Add("该页面可能存在 SQL 注入漏洞，可尝试扫描！正在自动猜解.....");
            }
            else
            {
                listBoxInScanProcess.Items.Clear();
                listBoxInScanProcess.Items.Add("该页面不存在 SQL 注入漏洞，无法扫描！");
            }
        }
        Thread threadCaiJieTable;
        private void btnCaiJieTableStart_Click(object sender, EventArgs e)
        {
            string url = txtSQLin.Text.Trim();
            if (!Tool.IsDomain(url))
            {
                MessageBox.Show("请输入正确的注入点网址\r\n(以http://或者https://开头的域名)", "提示");
                return;
            }
            threadCaiJieTable = new Thread(new ThreadStart(TableDataFilling));
            try
            {
                threadCaiJieTable.Start(); //线程启动
                listBoxCaiJieTable.Items.Clear();
                listBoxInScanProcess.Items.Clear();
                btnCaiJieTableStop.Enabled = true;
                btnCaiJieTableStart.Enabled = false;
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// 将预设的[表名集合]填充到[猜解表名]的委托事件中
        /// </summary>
        public void TableDataFilling()
        {
            ArrayList fileList = new ArrayList();
            //config.ini配置文件下的[Table]节点
            this.ini.ReadSectionValues("Table", fileList);
            if (fileList != null && fileList.Count > 0)
            {
                try
                {
                    foreach (string item in fileList)
                    {
                        //过滤非法字符
                        string table = Tool.UnicodeToString(item);
                        listBoxCaiJieTable.Items.Add(table);
                        listBoxInScanProcess.Items.Add(txtSQLin.Text + " 尝试猜解表：" + table);
                        Thread.Sleep(100);
                        SendMessage(listBoxCaiJieTable.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                        SendMessage(listBoxInScanProcess.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                    }
                    btnCaiJieTableStop.Enabled = false;
                    btnCaiJieTableStart.Enabled = true;
                    threadCaiJieTable.Abort();
                }
                catch (Exception)
                {
                }
            }
        }
        Thread threadCaiJieColumn;
        private void btnCaiJieColumnStart_Click(object sender, EventArgs e)
        {
            string url = txtSQLin.Text.Trim();
            if (!Tool.IsDomain(url))
            {
                MessageBox.Show("请输入正确的注入点网址\r\n(以http://或者https://开头的域名)", "提示");
                return;
            }
            threadCaiJieColumn = new Thread(new ThreadStart(ColumnDataFilling));
            try
            {
                threadCaiJieColumn.Start(); //线程启动
                listBoxCaiJieColumn.Items.Clear();
                listBoxInScanProcess.Items.Clear(); //扫描结果清空
                btnCaiJieColumnStop.Enabled = true;
                btnCaiJieColumnStart.Enabled = false;
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// 将预设的[列名集合]填充到[猜解列名]的委托事件中
        /// </summary>
        public void ColumnDataFilling()
        {
            ArrayList fileList = new ArrayList();
            //config.ini配置文件下的[Column]节点
            this.ini.ReadSectionValues("Column", fileList);
            if (fileList != null && fileList.Count > 0)
            {
                try
                {
                    foreach (string item in fileList)
                    {
                        //过滤非法字符
                        string column = Tool.UnicodeToString(item);
                        listBoxCaiJieColumn.Items.Add(column);
                        listBoxInScanProcess.Items.Add(txtSQLin.Text + " 尝试猜解列名：" + column);
                        Thread.Sleep(100);
                        SendMessage(listBoxCaiJieColumn.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                        SendMessage(listBoxInScanProcess.Handle, WM_VSCROLL, SB_LINEDOWN, 0);
                    }
                    btnCaiJieColumnStop.Enabled = false;
                    btnCaiJieColumnStart.Enabled = true;
                    threadCaiJieColumn.Abort();
                }
                catch (Exception)
                {
                }
            }
        }

        private void btnCaiJieTableStop_Click(object sender, EventArgs e)
        {
            btnCaiJieTableStop.Enabled = false;
            btnCaiJieTableStart.Enabled = true;
            threadCaiJieTable.Abort();
        }

        private void btnCaiJieColumnStop_Click(object sender, EventArgs e)
        {
            btnCaiJieColumnStop.Enabled = false;
            btnCaiJieColumnStart.Enabled = true;
            threadCaiJieColumn.Abort();
        }

        private void timerProgressBar_Tick(object sender, EventArgs e)
        {
            try
            {
                this.toolStripCpu.Text = "CPU 使用率：" + (int)(performanceCounterCpu.NextValue()) + "%";
                //读取物理内存
                MemoryInfo MemInfo = GetMemoryStatus();
                this.toolStripMemory.Text = "物理内存：" + Convert.ToInt64(MemInfo.dwMemoryLoad.ToString()) + "%";
            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// 读取内存
        /// </summary>
        /// <returns></returns>
        private static MemoryInfo GetMemoryStatus()
        {
            MemoryInfo MemInfo;
            MemInfo = new MemoryInfo();
            GlobalMemoryStatus(ref MemInfo);
            return MemInfo;
        }
        private void tsbBack_Click(object sender, EventArgs e)
        {
            //后退
            this.webBrowser1.GoBack();
        }

        private void tspForward_Click(object sender, EventArgs e)
        {
            //前进
            this.webBrowser1.GoForward();
        }

        Thread linkceshith;
        private void tspStart_Click(object sender, EventArgs e)
        {
            try
            {
                //点击开始按钮
                string url = tscbUrl.Text.Trim();
                if (url == string.Empty || url.Equals("about:blank"))
                {
                    MessageBox.Show("请先输入网址");
                    tscbUrl.Focus();
                    return;
                }
                Navigate(url);
                //清空域名列表
                listBox1.Items.Clear();
                //取得cookies
                this.textCookie.Text = this.webBrowser1.Document.Cookie.ToString();
                StreamReader sr = new StreamReader(webBrowser1.DocumentStream, Encoding.GetEncoding("gb2312"));
                richTextBox1.Text = sr.ReadToEnd();
                linkceshith = new Thread(new ThreadStart(googlelinkceshi));
                linkceshith.Start();
            }
            catch (Exception)
            {
            }
        }

        private void tscbUrl_KeyDown(object sender, KeyEventArgs e)
        {
            //如果输入的是回车键
            if (e.KeyCode == Keys.Enter)
            {
                //触发button事件
                this.tspStart_Click(sender, e);
            }
        }

        private void tspHome_Click(object sender, EventArgs e)
        {
            //主页
            Process.Start("http://www.zy13.net/");
        }

        private void picFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Open File Dialog";
            dialog.InitialDirectory = Path.GetFullPath("./");
            dialog.Filter = "txt files (*.txt)|*.txt";
            dialog.FilterIndex = 1;
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string path = dialog.FileName;
                txtMultipleUrlPath.Text = path;
                #region 域名导入
                try
                {
                    StreamReader sr = new StreamReader(path, Encoding.GetEncoding("gb2312"));//读取文件
                    string str = null;
                    while ((str = sr.ReadLine()) != null)//判断行
                    {
                        listBox1.Items.Add(str);
                    }
                    radioButtonMultiple.Checked = true;
                }
                catch (Exception)
                {
                }
                #endregion
            }
        }

        private void btnDomain_Click(object sender, EventArgs e)
        {
            string strDomain = this.txtDomain.Text.Trim();
            string pattern = @"^([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
            Regex regrx = new Regex(pattern);
            if (!regrx.IsMatch(strDomain))
            {
                MessageBox.Show("请输入正确的域名\r\n（例如：zy13.net）", "提示");
                return;
            }
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(strDomain);
                IPEndPoint ipEndPoint = new IPEndPoint(hostEntry.AddressList[0], 0);
                string ipAddress = ipEndPoint.Address.ToString();
                tabControlTool.SelectedTab = tabPagePortScan;
                txtIpStart.Text = ipAddress;
                btnIpStart_Click(sender, e);
            }
            catch (Exception)
            {
            }
        }
        public static int IPToNumber(string strIPAddress)
        {
            //将目标IP地址字符串strIPAddress转换为数字    
            string[] arrayIP = strIPAddress.Split('.');
            int sip1 = Int32.Parse(arrayIP[0]);
            int sip2 = Int32.Parse(arrayIP[1]);
            int sip3 = Int32.Parse(arrayIP[2]);
            int sip4 = Int32.Parse(arrayIP[3]);
            int tmpIpNumber;
            tmpIpNumber = (sip1 << 24) + (sip2 << 16) + (sip3 << 8) + sip4;
            return tmpIpNumber;
        }

        private void txtSingleUrl_TextChanged(object sender, EventArgs e)
        {
            radioButtonSingle.Checked = true;
        }

        private void ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //主页
            Process.Start("http://www.zy13.net/");
        }

    }
}
