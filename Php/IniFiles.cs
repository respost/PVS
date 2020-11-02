using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Specialized;

namespace Php
{
    /// <summary>
    /// IniFiles的类
    /// </summary>
    public class IniFiles
    {
        public string FileName; //INI文件名
        //声明读写INI文件的API函数
        [DllImport("kernel32")]
        /// <summary>
        /// 修改INI文件中内容
        /// </summary>
        /// <param name="section">欲在其中写入的节点名称</param>
        /// <param name="key">欲设置的项名</param>
        /// <param name="val">要写入的新字符串</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>非零表示成功，零表示失败</returns>
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        /// <summary>
        /// 为INI文件中指定的节点取得字符串
        /// </summary>
        /// <param name="section">欲在其中查找关键字的节点名称</param>
        /// <param name="key">欲获取的项名</param>
        /// <param name="def">指定的项没有找到时返回的默认值</param>
        /// <param name="retVal">指定一个字串缓冲区，长度至少为nSize</param>
        /// <param name="size">指定装载到lpReturnedString缓冲区的最大字符数量</param>
        /// <param name="filePath">INI文件完整路径</param>
        /// <returns>复制到ReturnedString缓冲区的字节数量，其中不包括那些NULL中止字符</returns>
        private static extern int GetPrivateProfileString(string section, string key, string def, byte[] retVal, int size, string filePath);

        //类的构造函数，传递INI文件名
        public IniFiles(string path)
        {
            FileInfo info = new FileInfo(path);
            if (!info.Exists)
            {
                //Ini文件不存在，就创建一个
                string msg = "[URL]\r\n资源驿站 = http://www.zy13.net";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);
                FileStream fs = new FileStream(Environment.CurrentDirectory + @"\" + path, FileMode.Create);
                fs.Write(bytes, 0, bytes.Length);
                fs.Close();
            }
            //必须是完全路径，不能是相对路径
            FileName = info.FullName;
        }
        //写INI文件
        public void WriteString(string Section, string Ident, string Value)
        {
            if (!WritePrivateProfileString(Section, Ident, Value, FileName))
            {
                throw (new ApplicationException("写入Ini文件出错"));
            }
        }
        //读取INI文件指定
        public string ReadString(string Section, string Ident, string Default)
        {
            Byte[] Buffer = new Byte[65535];
            int bufLen = GetPrivateProfileString(Section, Ident, Default, Buffer, Buffer.GetUpperBound(0), FileName);
            //必须设定0（系统默认的代码页）的编码方式，否则无法支持中文
            string s = Encoding.GetEncoding(0).GetString(Buffer);
            s = s.Substring(0, bufLen);
            return s.Trim();
        }
        //读整数
        public int ReadInteger(string Section, string Ident, int Default)
        {
            string intStr = ReadString(Section, Ident, Convert.ToString(Default));
            try
            {
                return Convert.ToInt32(intStr);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Default;
            }
        }

        //写整数
        public void WriteInteger(string Section, string Ident, int Value)
        {
            WriteString(Section, Ident, Value.ToString());
        }

        //读布尔
        public bool ReadBool(string Section, string Ident, bool Default)
        {
            try
            {
                return Convert.ToBoolean(ReadString(Section, Ident, Convert.ToString(Default)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return Default;
            }
        }

        //写Bool
        public void WriteBool(string Section, string Ident, bool Value)
        {
            WriteString(Section, Ident, Convert.ToString(Value));
        }

        //从Ini文件中，将指定的Section名称中的所有Ident添加到列表中
        public void ReadSection(string Section, StringCollection Idents)
        {
            Byte[] Buffer = new Byte[16384];
            //Idents.Clear();
            int bufLen = GetPrivateProfileString(Section, null, null, Buffer, Buffer.GetUpperBound(0),
                  FileName);
            //对Section进行解析
            GetStringsFromBuffer(Buffer, bufLen, Idents);
        }

        private void GetStringsFromBuffer(Byte[] Buffer, int bufLen, StringCollection Strings)
        {
            Strings.Clear();
            if (bufLen != 0)
            {
                int start = 0;
                for (int i = 0; i < bufLen; i++)
                {
                    if ((Buffer[i] == 0) && ((i - start) > 0))
                    {
                        String s = Encoding.GetEncoding(0).GetString(Buffer, start, i - start);
                        Strings.Add(s);
                        start = i + 1;
                    }
                }
            }
        }
        //从Ini文件中，读取所有的Sections的名称
        public void ReadSections(StringCollection SectionList)
        {
            //Note:必须得用Bytes来实现，StringBuilder只能取到第一个Section
            byte[] Buffer = new byte[65535];
            int bufLen = 0;
            bufLen = GetPrivateProfileString(null, null, null, Buffer,
            Buffer.GetUpperBound(0), FileName);
            GetStringsFromBuffer(Buffer, bufLen, SectionList);
        }
        /// <summary>
        /// 读取指定的Section的所有Value到列表中
        /// </summary>
        /// <param name="Section"></param>
        /// <param name="Values"></param>
        /* 调用示例：
        NameValueCollection values = new NameValueCollection();
        this.ini.ReadSectionValues("URL", values);
        foreach (string key in values.Keys)
        {
            MessageBox.Show(values[key]);
        }
         */
        public void ReadSectionValues(string Section, NameValueCollection Values)
        {
            StringCollection KeyList = new StringCollection();
            ReadSection(Section, KeyList);
            Values.Clear();
            foreach (string key in KeyList)
            {
                Values.Add(key, ReadString(Section, key, ""));
            }
        }
        public void ReadSectionValues(string Section, ArrayList Values)
        {
            StringCollection idents = new StringCollection();
            ReadSection(Section, idents);
            Values.Clear();
            foreach (string key in idents)
            {
                string value = this.ReadString(Section, key, "");
                Values.Add(value);
            }
        }
        //清除某个Section
        public void EraseSection(string Section)
        {
            if (!WritePrivateProfileString(Section, null, null, FileName))
            {
                throw (new ApplicationException("无法清除Ini文件中的Section"));
            }
        }
        //删除某个Section下的键
        public void DeleteKey(string Section, string Ident)
        {
            WritePrivateProfileString(Section, Ident, null, FileName);
        }
        //Note:对于Win9X，来说需要实现UpdateFile方法将缓冲中的数据写入文件
        //在Win NT, 2000和XP上，都是直接写文件，没有缓冲，所以，无须实现UpdateFile
        //执行完对Ini文件的修改之后，应该调用本方法更新缓冲区。
        public void UpdateFile()
        {
            WritePrivateProfileString(null, null, null, FileName);
        }

        //检查某个Section下的某个键值是否存在
        public bool ValueExists(string Section, string Ident)
        {
            StringCollection Idents = new StringCollection();
            ReadSection(Section, Idents);
            return Idents.IndexOf(Ident) > -1;
        }

        //确保资源的释放
        ~IniFiles()
        {
            UpdateFile();
        }
    }
}