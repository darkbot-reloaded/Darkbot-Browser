using System.Windows.Forms;

namespace Browser
{
    partial class FrmMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.pnlBrowserContainer = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(12, 12);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(442, 867);
            this.rtbLog.TabIndex = 2;
            this.rtbLog.Text = "";
            // 
            // pnlBrowserContainer
            // 
            this.pnlBrowserContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlBrowserContainer.Location = new System.Drawing.Point(460, 12);
            this.pnlBrowserContainer.Name = "pnlBrowserContainer";
            this.pnlBrowserContainer.Size = new System.Drawing.Size(1317, 867);
            this.pnlBrowserContainer.TabIndex = 3;
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1789, 891);
            this.Controls.Add(this.pnlBrowserContainer);
            this.Controls.Add(this.rtbLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DarkBot Browser";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.ResumeLayout(false);

        }
        private RichTextBox rtbLog;
        private Panel pnlBrowserContainer;
    }
}

