using System.Windows.Forms;

namespace Browser
{
    partial class Main
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.pbBrowser = new System.Windows.Forms.PictureBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowser)).BeginInit();
            this.SuspendLayout();
            // 
            // pbBrowser
            // 
            this.pbBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pbBrowser.Location = new System.Drawing.Point(460, 12);
            this.pbBrowser.Name = "pbBrowser";
            this.pbBrowser.Size = new System.Drawing.Size(1317, 867);
            this.pbBrowser.TabIndex = 0;
            this.pbBrowser.TabStop = false;
            this.pbBrowser.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pbBrowser_MouseDown);
            this.pbBrowser.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pbBrowser_MouseMove);
            this.pbBrowser.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pbBrowser_MouseUp);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(12, 12);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(442, 867);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1789, 891);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.pbBrowser);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "DarkBot Browser";
            this.Load += new System.EventHandler(this.main_Load);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.main_KeyPress);
            this.Resize += new System.EventHandler(this.main_Resize);
            ((System.ComponentModel.ISupportInitialize)(this.pbBrowser)).EndInit();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.PictureBox pbBrowser;
        private RichTextBox richTextBox1;
    }
}

