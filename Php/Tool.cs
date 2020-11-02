using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Web;
using System.Windows.Forms;

///功能：常用小功能 静态

namespace Php
{
    static class Tool
    {
        #region 网络检测
        private const int INTERNET_CONNECTION_MODEM = 1;
        private const int INTERNET_CONNECTION_LAN = 2;
        private const int INTERNET_CONNECTION_PROXY = 4;
        private const int INTERNET_CONNECTION_MODEM_BUSY = 8;
        [DllImport("wininet.dll")]
        //函数InternetGetConnectedState返回本地系统的网络连接状态
        private static extern bool InternetGetConnectedState(ref int lpdwFlags, int dwReserved);  
           
        /// <summary>
        /// 判断本地网络的连接状态（判断当前是否连接Internet）
        /// </summary>
        /// <returns></returns>
        public static bool LocalConnectionStatus()
        {
            bool flag = false;
            System.Int32 dwFlag = new Int32();
            if (!InternetGetConnectedState(ref dwFlag, 0))
            {
                //未连网
                flag = false;
            }
            else
            {
                if ((dwFlag & INTERNET_CONNECTION_MODEM) != 0)
                {
                    //采用调制解调器上网           
                }
                else if ((dwFlag & INTERNET_CONNECTION_LAN) != 0)
                {
                    //采用网卡上网
                }
                flag = true;
            }
            return flag;
        }
        #endregion

        /// <summary>
        /// 获取服务器IP
        /// </summary>
        /// <returns></returns>
        public static string getAddressIP()
        {
            ///获取服务端
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="url">指定的Url地址</param>
        /// <param name="Isbool">是否去除网页源码中的回车换行符，true 去除</param>
        /// <returns></returns>
        static  public string GetHtmlSource(string url,bool Isbool)
        {
            if (url == "")
                return "";

            string charSet = "";
            WebClient myWebClient = new WebClient();


            //获取或设置用于对向 Internet 资源的请求进行身份验证的网络凭据。   
            myWebClient.Credentials = CredentialCache.DefaultCredentials;


            byte[] myDataBuffer;
            string strWebData;

            try
            {
                //从资源下载数据并返回字节数组。（加@是因为网址中间有"/"符号）   
                myDataBuffer = myWebClient.DownloadData(@url);
                strWebData = Encoding.Default.GetString(myDataBuffer);
            }
            catch (System.Net.WebException  ) 
            {
                return "";
            }

            //获取此页面的编码格式
            Match charSetMatch = Regex.Match(strWebData, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string webCharSet = charSetMatch.Groups[2].Value;
            if (charSet == null || charSet == "")
                charSet = webCharSet;

            if (charSet != null && charSet != "" && Encoding.GetEncoding(charSet) != Encoding.Default)
                strWebData = Encoding.GetEncoding(charSet).GetString(myDataBuffer);

            if (Isbool == true)
            {
                strWebData = Regex.Replace(strWebData, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                strWebData.Replace(@"\r\n", "");
            }

            return strWebData;

        }

        //去除字符串的回车换行符号
        public static string ClearFlag(string str)
        {
            str = Regex.Replace(str, @"([\r\n])[\s]+", "", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            str.Replace(@"\r\n", "");

            return str;
        }

        

        static private int CountUrl(string WebUrl)
        {
            int Count = 0;
            Match Para = Regex.Match(WebUrl, "{.*}");
            
            return Count;
        }

        //将大写字母转成小写字母
        public static string LetterToLower(string str)
        {
            if (str == "" || str == null)
                return "";

            string lowerStr="";
            char c;

            for (int i=0;i<str.Length ;i++)
            {
                c =char .Parse ( str.Substring(i, 1));

                if (Char.IsUpper(c))
                {
                    c = Char.ToLower(c);
                }
                lowerStr += c;
            }

            return lowerStr;
           
        }

        //将字符串中的字符（转义的）进行替换
        //这个替换真的有点头晕，对正则理解还不到位，不知道是否可以一次按照分组那样的形式对应进行替换
        //请高手修改此类，这样写实在是不得已，呵呵
        public static string ReplaceTrans(string str)
        {
            if (str == "" || str==null )
                return "";

            string conStr="";
            if (Regex.IsMatch(str, "['\"<>&]"))
            {
                Regex re = new Regex("&", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&amp;");
                re = null;

                re = new Regex("<", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&lt;");
                re = null;

                re = new Regex(">", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&gt;");
                re = null;

                re = new Regex("'", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&apos;");
                re = null;

                re = new Regex("\"", RegexOptions.IgnoreCase);
                str = re.Replace(str, "&quot;");
                re = null;
                conStr = str;

            }
            else
            {
                conStr = str;
            }
            return conStr;
        }

        //正则表达式转义
        public static string RegexReplaceTrans(string str)
        {
            if (str == "" || str == null)
                return "";

            string conStr = "";
            if (Regex.IsMatch(str, @"[\$\*\[\]\?\\\(\)]"))
            {
                Regex re = new Regex(@"\\", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\\");
                re = null;

                re = new Regex(@"\$", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\$");
                re = null;

                //re = new Regex(@"\.", RegexOptions.IgnoreCase);
                //str = re.Replace(str, @"\.");
                //re = null;

                re = new Regex(@"\*", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\*");
                re = null;

                re = new Regex(@"\[", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\[");
                re = null;

                re = new Regex(@"\]", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\]");
                re = null;

                re = new Regex(@"\?", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\?");
                re = null;

                re = new Regex(@"\(", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\(");
                re = null;

                re = new Regex(@"\)", RegexOptions.IgnoreCase);
                str = re.Replace(str, @"\)");
                re = null;

                conStr = str;

            }
            else
            {
                conStr = str;
            }
            return conStr;
        }

        //用于将字符串转换成UTF-8编码
        public static string ToUtf8(string str)
        {
            if (str == null)
            {
                return string.Empty;
            }
            else
            {
                char[] hexDigits = {  '0', '1', '2', '3', '4','5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

                Encoding utf8 = Encoding.UTF8;
                StringBuilder result = new StringBuilder();

                for (int i = 0; i < str.Length; i++)
                {
                    string sub = str.Substring(i, 1);
                    byte[] bytes = utf8.GetBytes(sub);

                    for (int j = 0; j < bytes.Length; j++)
                    {
                        result.Append("%" + hexDigits[bytes[j] >> 4] + hexDigits[bytes[j] & 0XF]);
                    }
                }

                return result.ToString();
            }
        }

        //将UTF-8编码转换成字符串
        public static string FromUtf8(string str)
        {
            char[] hexDigits = {  '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};
            List<byte> byteList = new List<byte>(str.Length / 3);

            if (str != null)
            {
                List<string> strList = new List<string>();
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < str.Length; ++i)
                {
                    if (str[i] == '%')
                    {
                        strList.Add(str.Substring(i, 3));
                    }
                }

                foreach (string tempStr in strList)
                {
                    int num = 0;
                    int temp = 0;
                    for (int j = 0; j < hexDigits.Length; ++j)
                    {
                        if (hexDigits[j].Equals(tempStr[1]))
                        {
                            temp = j;
                            num = temp << 4;
                        }
                    }

                    for (int j = 0; j < hexDigits.Length; ++j)
                    {
                        if (hexDigits[j].Equals(tempStr[2]))
                        {
                            num += j;
                        }
                    }

                    byteList.Add((byte)num);
                }
            }

            return Encoding.UTF8.GetString(byteList.ToArray());
        }

        public static string UTF8ToGB2312(string str)
        {
            try
            {
                Encoding utf8 = Encoding.GetEncoding(65001);
                Encoding gb2312 = Encoding.GetEncoding("gb2312");//Encoding.Default ,936
                byte[] temp = utf8.GetBytes(str);
                byte[] temp1 = Encoding.Convert(utf8, gb2312, temp);
                string result = gb2312.GetString(temp1);
                return result;
            }
            catch (Exception)//(UnsupportedEncodingException ex)
            {
                return null;
            }
        }
        /// <summary>
        /// 返回相对于Soukey采摘的相对路径
        /// </summary>
        /// <param name="absPath"></param>
        /// <returns></returns>
        public static string GetRelativePath(string absPath)
        {
            string mainDir = Program.getPrjPath();

            //if (!mainDir.EndsWith("\\"))
            //{
            //    mainDir += "\\";
            //}

            int intIndex = -1, intPos = mainDir.IndexOf('\\');

            while (intPos >= 0)
            {
                intPos++;
                if (string.Compare(mainDir, 0, absPath, 0, intPos, true) != 0) break;
                intIndex = intPos;
                intPos = mainDir.IndexOf('\\', intPos);
            }

            if (intIndex >= 0)
            {
                absPath = absPath.Substring(intIndex);
                intPos = mainDir.IndexOf("\\", intIndex);
                while (intPos >= 0)
                {
                    absPath = "..\\" + absPath;
                    intPos = mainDir.IndexOf("\\", intPos + 1);
                }
            }

            return absPath;
        }
        /// <summary>
        /// 判断指定的文件所在的目录是否存在，如果不存在则建立
        /// </summary>
        /// <param name="strDir">传入的参数必须是文件，如果不是文件，则需以"\"结尾</param>
        public static void CreateDirectory(string strDir)
        {
            //需要提取文件目录
            strDir = Path.GetDirectoryName(strDir);

            if (!Directory.Exists(strDir))
            {
                //创建此目录
                Directory.CreateDirectory(strDir);
            }
        }
        ///<summary>
        ///由秒数得到日期几天几小时。。。
        ///</summary
        ///<param name="t">秒数</param>
        ///<param name="type">0：转换后带秒，1:转换后不带秒</param>
        ///<returns>几天几小时几分几秒</returns>
        public static string parseTimeSeconds(int t, int type = 0)
        {
            string r = "";
            int day, hour, minute, second;
            if (t >= 86400) //天,
            {
                day = Convert.ToInt16(t / 86400);
                hour = Convert.ToInt16((t % 86400) / 3600);
                minute = Convert.ToInt16((t % 86400 % 3600) / 60);
                second = Convert.ToInt16(t % 86400 % 3600 % 60);
                if (type == 0)
                    r = day + ("天") + hour + ("时") + minute + ("分") + second + ("秒");
                else
                    r = day + ("天") + hour + ("时") + minute + ("分");

            }
            else if (t >= 3600)//时,
            {
                hour = Convert.ToInt16(t / 3600);
                minute = Convert.ToInt16((t % 3600) / 60);
                second = Convert.ToInt16(t % 3600 % 60);
                if (type == 0)
                    r = hour + ("时") + minute + ("分") + second + ("秒");
                else
                    r = hour + ("时") + minute + ("分");
            }
            else if (t >= 60)//分
            {
                minute = Convert.ToInt16(t / 60);
                second = Convert.ToInt16(t % 60);
                r = minute + ("分") + second + ("秒");
            }
            else
            {
                second = Convert.ToInt16(t);
                r = second + ("秒");
            }
            return r;
        }
        /// <summary>
        /// 验证域名是否合法
        /// </summary>
        /// <param name="str">指定字符串</param>
        /// <returns></returns>
        public static bool IsDomain(string str)
        {
            string pattern = @"^(http://|https://)([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?$";
            Regex reg = new Regex(pattern);
            return reg.IsMatch(str);
        }
        // <summary>
        /// <summary>
        /// 字符串转Unicode
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns>Unicode编码后的字符串</returns>
        public static string StringToUnicode(string source)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(source);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i += 2)
            {
                stringBuilder.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
            }
            return stringBuilder.ToString();
        }
        /// <summary>
        /// Unicode转字符串
        /// </summary>
        /// <param name="source">经过Unicode编码的字符串</param>
        /// <returns>正常字符串</returns>
        public static string UnicodeToString(string source)
        {
            return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
        }
    }
}
