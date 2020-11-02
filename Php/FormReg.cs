using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Management;

namespace Php
{
    public partial class FormReg : Form
    {
        public FormReg()
        {
            InitializeComponent();
        }

        private Reg reg = new Reg();

        private void FormReg_Load(object sender, EventArgs e)
        {
            //获取机器码
            txtMacCode.Text = reg.GetMNum();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Clipboard.SetDataObject(txtMacCode.Text);
            MessageBox.Show("复制成功!","提示");
        }

    }
}
