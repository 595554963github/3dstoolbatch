namespace _3DSExtractorPacker
{
    partial class MainForm
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
            btnExtract = new Button();
            btnPack = new Button();
            txtOutput = new TextBox();
            SuspendLayout();
            // 
            // btnExtract
            // 
            btnExtract.ForeColor = Color.DarkViolet;
            btnExtract.Location = new Point(14, 16);
            btnExtract.Margin = new Padding(4);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new Size(175, 65);
            btnExtract.TabIndex = 0;
            btnExtract.Text = "解包3DS游戏";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // btnPack
            // 
            btnPack.ForeColor = Color.OrangeRed;
            btnPack.Location = new Point(196, 16);
            btnPack.Margin = new Padding(4);
            btnPack.Name = "btnPack";
            btnPack.Size = new Size(175, 65);
            btnPack.TabIndex = 1;
            btnPack.Text = "打包3DS游戏";
            btnPack.UseVisualStyleBackColor = true;
            btnPack.Click += btnPack_Click;
            // 
            // txtOutput
            // 
            txtOutput.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtOutput.Location = new Point(14, 89);
            txtOutput.Margin = new Padding(4);
            txtOutput.Multiline = true;
            txtOutput.Name = "txtOutput";
            txtOutput.ScrollBars = ScrollBars.Vertical;
            txtOutput.Size = new Size(653, 366);
            txtOutput.TabIndex = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(681, 472);
            Controls.Add(txtOutput);
            Controls.Add(btnPack);
            Controls.Add(btnExtract);
            Margin = new Padding(4);
            MinimumSize = new Size(697, 511);
            Name = "MainForm";
            Text = "3DS游戏解包/打包辅助工具";
            ResumeLayout(false);
            PerformLayout();

        }

        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.Button btnPack;
        private System.Windows.Forms.TextBox txtOutput;
    }
}