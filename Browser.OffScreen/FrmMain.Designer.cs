using System.Windows.Forms;

namespace Browser.OffScreen
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
            this.pbBrowser = new System.Windows.Forms.PictureBox();
            this.nudFps = new System.Windows.Forms.NumericUpDown();
            this.lblFps = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowser)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFps)).BeginInit();
            this.SuspendLayout();
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(12, 40);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(442, 839);
            this.rtbLog.TabIndex = 2;
            this.rtbLog.Text = "";
            // 
            // pbBrowser
            // 
            this.pbBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbBrowser.Location = new System.Drawing.Point(460, 12);
            this.pbBrowser.Name = "pbBrowser";
            this.pbBrowser.Size = new System.Drawing.Size(1317, 867);
            this.pbBrowser.TabIndex = 3;
            this.pbBrowser.TabStop = false;
            this.pbBrowser.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbBrowser_MouseDown);
            this.pbBrowser.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbBrowser_MouseMove);
            this.pbBrowser.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbBrowser_MouseUp);
            // 
            // nudFps
            // 
            this.nudFps.Location = new System.Drawing.Point(45, 12);
            this.nudFps.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.nudFps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudFps.Name = "nudFps";
            this.nudFps.Size = new System.Drawing.Size(120, 22);
            this.nudFps.TabIndex = 4;
            this.nudFps.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            this.nudFps.ValueChanged += new System.EventHandler(this.nudFps_ValueChanged);
            // 
            // lblFps
            // 
            this.lblFps.AutoSize = true;
            this.lblFps.Location = new System.Drawing.Point(12, 14);
            this.lblFps.Name = "lblFps";
            this.lblFps.Size = new System.Drawing.Size(27, 17);
            this.lblFps.TabIndex = 5;
            this.lblFps.Text = "fps";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1789, 891);
            this.Controls.Add(this.lblFps);
            this.Controls.Add(this.nudFps);
            this.Controls.Add(this.pbBrowser);
            this.Controls.Add(this.rtbLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DarkBot Browser";
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.FrmMain_KeyPress);
            this.Resize += new System.EventHandler(this.FrmMain_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowser)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudFps)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private RichTextBox rtbLog;
        private PictureBox pbBrowser;
        private NumericUpDown nudFps;
        private Label lblFps;
    }
}

