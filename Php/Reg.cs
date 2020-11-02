using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Management;

namespace Php
{
    /// <summary>
    /// 软件注册类
    /// </summary>
    internal class Reg
    {
        private char[] charCode = new char[0x19];
        private int[] intCode = new int[0x7f];
        private int[] intNumber = new int[0x19];
        /// <summary>
        /// 获得CPU编号(不推荐，有的电脑获取不到)
        /// </summary>
        /// <returns></returns>
        private string GetCpu()
        {
            string cpuSerialNumber = null;
            ManagementObjectCollection instances = new ManagementClass("win32_Processor").GetInstances();
            foreach (ManagementObject mobj in instances)
            {
                cpuSerialNumber = mobj.Properties["Processorid"].Value.ToString();
            }
            return cpuSerialNumber;
        }
        /// <summary>
        /// 获取网卡硬件地址
        /// </summary>
        /// <returns></returns> 
        private string GetMacAddress()
        {
            var mac = "";
            var mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
            var moc = mc.GetInstances();
            foreach (var o in moc)
            {
                var mo = (ManagementObject)o;
                if (!(bool)mo["IPEnabled"]) continue;
                mac = mo["MacAddress"].ToString();
                break;
            }
            return mac;
        }
        /// <summary>
        /// 获取逻辑分区（C盘）序列号，重新格式化会改变 
        /// </summary>
        /// <returns></returns>
        private string GetDiskSerialNumber()
        {
            ManagementObject disk = new ManagementObject("win32_logicaldisk.deviceid=\"c:\"");
            disk.Get();
            return disk.GetPropertyValue("VolumeSerialNumber").ToString();
        }
        /// <summary>
        /// 获取机器码
        /// </summary>
        /// <returns></returns>
        public string GetMNum()
        {
            return (this.GetMD5WithString(this.GetMacAddress()) + this.GetDiskSerialNumber()).Substring(0, 0x18);
        }
        //获取注册码
        public string GetRNum()
        {
            this.SetIntCode();
            string mNum = this.GetMNum();
            for (int i = 1; i < this.charCode.Length; i++)
            {
                this.charCode[i] = Convert.ToChar(mNum.Substring(i - 1, 1));
            }
            for (int j = 1; j < this.intNumber.Length; j++)
            {
                this.intNumber[j] = Convert.ToInt32(this.charCode[j]) + this.intCode[Convert.ToInt32(this.charCode[j])];
            }
            string str2 = "";
            for (int k = 1; k < this.intNumber.Length; k++)
            {
                if ((((this.intNumber[k] >= 0x30) && (this.intNumber[k] <= 0x39)) || ((this.intNumber[k] >= 0x41) && (this.intNumber[k] <= 90))) || ((this.intNumber[k] >= 0x61) && (this.intNumber[k] <= 0x7a)))
                {
                    str2 = str2 + Convert.ToChar(this.intNumber[k]).ToString();
                }
                else if (this.intNumber[k] > 0x7a)
                {
                    str2 = str2 + Convert.ToChar((int)(this.intNumber[k] - 10)).ToString();
                }
                else
                {
                    str2 = str2 + Convert.ToChar((int)(this.intNumber[k] - 9)).ToString();
                }
            }
            return EncryptFromString(str2);
        }

        private void SetIntCode()
        {
            for (int i = 1; i < this.intCode.Length; i++)
            {
                this.intCode[i] = i % 9;
            }
        }
        /// <summary>
        /// 将字符串进行加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string EncryptFromString(string str)
        {
            try
            {
                MD5 md5Hash = MD5.Create();
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(str));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                string sha = sBuilder.ToString();
                string s25 = sha.Substring(7);//从32位中取最后25位
                //每隔5位插入一个"-"
                string s = System.Text.RegularExpressions.Regex.Replace(s25.ToUpper(), @"(\w{5})", "$1-").Trim('-');
                return s;
            }
            catch (Exception ex)
            {
                throw new Exception("加密失败，error:" + ex.Message);
            }
        }
        /// <summary>
        /// 计算字符串的Md5值
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string GetMD5WithString(String input)
        {
            MD5 md5Hash = MD5.Create();
            // 将输入字符串转换为字节数组并计算哈希数据  
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            // 创建一个 Stringbuilder 来收集字节并创建字符串  
            StringBuilder str = new StringBuilder();
            // 循环遍历哈希数据的每一个字节并格式化为十六进制字符串  
            for (int i = 0; i < data.Length; i++)
            {
                str.Append(data[i].ToString("x2"));//加密结果"x2"结果为32位,"x3"结果为48位,"x4"结果为64位
            }
            // 返回十六进制字符串  
            return str.ToString();
        }
    }
}
