using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using ADOX;
using JRO;
using System.IO; 

//修复ACCESS模块
namespace Php
{
    public partial class FormAccess : Form
    {
        public FormAccess()
        {
            InitializeComponent();
        }

        private void FormAccess_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Access数据库|*.mdb";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDataPath.Text = openFileDialog1.FileName;
                strPathMdb = txtDataPath.Text.TrimEnd();
            }
        }
        string strPathMdb = null;
        Thread thacc;

        public void yas()
        {
            if (!File.Exists(strPathMdb)) //检查数据库是否已存在
            {

                MessageBox.Show("目标数据库不存在，请先选择Access数据库文件", "操作提示");
                btnRepair.Text = "开始修复";
                thacc.Abort();
                return;
            }
            //声明临时数据库的名称
            string temp = DateTime.Now.Year.ToString();
            temp += DateTime.Now.Month.ToString();
            temp += DateTime.Now.Day.ToString();
            temp += DateTime.Now.Hour.ToString();
            temp += DateTime.Now.Minute.ToString();
            temp += DateTime.Now.Second.ToString() + ".bak";
            temp = strPathMdb.Substring(0, strPathMdb.LastIndexOf("\\") + 1) + temp;
            //定义临时数据库的连接字符串
            string temp2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + temp;
            //定义目标数据库的连接字符串
            string strPathMdb2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + strPathMdb;
            //创建一个JetEngineClass对象的实例
            JRO.JetEngineClass jt = new JRO.JetEngineClass();
            //使用JetEngineClass对象的CompactDatabase方法压缩修复数据库
            jt.CompactDatabase(strPathMdb2, temp2);
            //拷贝临时数据库到目标数据库(覆盖)
            File.Copy(temp, strPathMdb, true);
            //最后删除临时数据库
            File.Delete(temp);
            MessageBox.Show("修复完成");
        }


        private void picClient_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Access数据库|*.mdb";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                txtDataPath.Text = openFileDialog1.FileName;
                strPathMdb = txtDataPath.Text.TrimEnd();
            }
        }

        private void btnRepair_Click(object sender, EventArgs e)
        {
            try
            {
                thacc = new Thread(new ThreadStart(yas));
                if (btnRepair.Text == "开始修复")
                {
                    thacc.Start();
                    btnRepair.Text = "暂停修复";
                }
                else
                {
                    btnRepair.Text = "开始修复";
                    thacc.Abort();
                }
            }
            catch (Exception)
            {               
                MessageBox.Show("修复失败");
            }
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }


    }
}
