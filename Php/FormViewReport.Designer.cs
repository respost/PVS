namespace Php
{
    partial class FormViewReport
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormViewReport));
            this.webBrowser_report = new System.Windows.Forms.WebBrowser();
            this.SuspendLayout();
            // 
            // webBrowser_report
            // 
            this.webBrowser_report.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser_report.Location = new System.Drawing.Point(0, 0);
            this.webBrowser_report.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser_report.Name = "webBrowser_report";
            this.webBrowser_report.Size = new System.Drawing.Size(1008, 587);
            this.webBrowser_report.TabIndex = 0;
            // 
            // FormViewReport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1008, 587);
            this.Controls.Add(this.webBrowser_report);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "FormViewReport";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "HTML¼ì²â±¨¸æ";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser_report;
    }
}