namespace DataProcessSystem
{
    partial class Form1
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
            this.btnSelectFile = new System.Windows.Forms.Button();
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.btnRun = new System.Windows.Forms.Button();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.btnSelectFolder = new System.Windows.Forms.Button();
            this.treeViewData = new System.Windows.Forms.TreeView();
            this.panel3D = new System.Windows.Forms.Panel();
            this.flowLayoutPanelImages = new System.Windows.Forms.FlowLayoutPanel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.rtbPrompt = new System.Windows.Forms.RichTextBox();
            this.btnToggleView = new System.Windows.Forms.Button();
            this.btnPrev = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSelectFile
            // 
            this.btnSelectFile.Location = new System.Drawing.Point(42, 36);
            this.btnSelectFile.Name = "btnSelectFile";
            this.btnSelectFile.Size = new System.Drawing.Size(155, 25);
            this.btnSelectFile.TabIndex = 0;
            this.btnSelectFile.Text = "选择单文件 (.prt)";
            this.btnSelectFile.UseVisualStyleBackColor = true;
            this.btnSelectFile.Click += new System.EventHandler(this.btnSelectFile_Click);
            // 
            // txtFilePath
            // 
            this.txtFilePath.Location = new System.Drawing.Point(218, 51);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(662, 25);
            this.txtFilePath.TabIndex = 1;
            // 
            // btnRun
            // 
            this.btnRun.Font = new System.Drawing.Font("宋体", 15F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnRun.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnRun.Location = new System.Drawing.Point(450, 91);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(149, 46);
            this.btnRun.TabIndex = 2;
            this.btnRun.Text = "开始提取";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // rtbLog
            // 
            this.rtbLog.Location = new System.Drawing.Point(-4, 466);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.Size = new System.Drawing.Size(1102, 151);
            this.rtbLog.TabIndex = 3;
            this.rtbLog.Text = "";
            // 
            // btnSelectFolder
            // 
            this.btnSelectFolder.Location = new System.Drawing.Point(42, 67);
            this.btnSelectFolder.Name = "btnSelectFolder";
            this.btnSelectFolder.Size = new System.Drawing.Size(155, 25);
            this.btnSelectFolder.TabIndex = 5;
            this.btnSelectFolder.Text = "选择文件夹 (批量)";
            this.btnSelectFolder.UseVisualStyleBackColor = true;
            this.btnSelectFolder.Click += new System.EventHandler(this.btnSelectFolder_Click);
            // 
            // treeViewData
            // 
            this.treeViewData.Location = new System.Drawing.Point(22, 206);
            this.treeViewData.Name = "treeViewData";
            this.treeViewData.Size = new System.Drawing.Size(293, 227);
            this.treeViewData.TabIndex = 6;
            // 
            // panel3D
            // 
            this.panel3D.Location = new System.Drawing.Point(369, 205);
            this.panel3D.Name = "panel3D";
            this.panel3D.Size = new System.Drawing.Size(337, 227);
            this.panel3D.TabIndex = 7;
            // 
            // flowLayoutPanelImages
            // 
            this.flowLayoutPanelImages.AutoScroll = true;
            this.flowLayoutPanelImages.Location = new System.Drawing.Point(757, 204);
            this.flowLayoutPanelImages.Name = "flowLayoutPanelImages";
            this.flowLayoutPanelImages.Size = new System.Drawing.Size(312, 228);
            this.flowLayoutPanelImages.TabIndex = 8;
            // 
            // groupBox1
            // 
            this.groupBox1.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox1.Location = new System.Drawing.Point(22, 175);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(264, 26);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "一维多模态工艺知识 ";
            // 
            // groupBox2
            // 
            this.groupBox2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox2.Location = new System.Drawing.Point(757, 175);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(264, 26);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "二维多视角几何特征图 ";
            // 
            // groupBox3
            // 
            this.groupBox3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupBox3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox3.Location = new System.Drawing.Point(369, 175);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(264, 26);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "三维stl文件预览";
            // 
            // groupBox4
            // 
            this.groupBox4.Location = new System.Drawing.Point(-4, 447);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(147, 22);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "系统运行日志";
            // 
            // rtbPrompt
            // 
            this.rtbPrompt.Location = new System.Drawing.Point(22, 206);
            this.rtbPrompt.Name = "rtbPrompt";
            this.rtbPrompt.ReadOnly = true;
            this.rtbPrompt.Size = new System.Drawing.Size(292, 226);
            this.rtbPrompt.TabIndex = 13;
            this.rtbPrompt.Text = "";
            this.rtbPrompt.Visible = false;
            // 
            // btnToggleView
            // 
            this.btnToggleView.Location = new System.Drawing.Point(314, 237);
            this.btnToggleView.Name = "btnToggleView";
            this.btnToggleView.Size = new System.Drawing.Size(29, 162);
            this.btnToggleView.TabIndex = 14;
            this.btnToggleView.Text = "切换数据";
            this.btnToggleView.UseVisualStyleBackColor = true;
            this.btnToggleView.Click += new System.EventHandler(this.btnToggleView_Click);
            // 
            // btnPrev
            // 
            this.btnPrev.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPrev.Location = new System.Drawing.Point(450, 438);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(31, 28);
            this.btnPrev.TabIndex = 15;
            this.btnPrev.Text = "<";
            this.btnPrev.UseVisualStyleBackColor = true;
            this.btnPrev.Click += new System.EventHandler(this.btnPrev_Click);
            // 
            // btnNext
            // 
            this.btnNext.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnNext.Location = new System.Drawing.Point(568, 438);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(31, 28);
            this.btnNext.TabIndex = 16;
            this.btnNext.Text = ">";
            this.btnNext.UseVisualStyleBackColor = true;
            this.btnNext.Click += new System.EventHandler(this.btnNext_Click);
            // 
            // lblPage
            // 
            this.lblPage.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPage.Location = new System.Drawing.Point(489, 441);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(72, 25);
            this.lblPage.TabIndex = 17;
            this.lblPage.Text = "0 / 0";
            this.lblPage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1096, 614);
            this.Controls.Add(this.lblPage);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.btnPrev);
            this.Controls.Add(this.btnToggleView);
            this.Controls.Add(this.rtbPrompt);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.flowLayoutPanelImages);
            this.Controls.Add(this.panel3D);
            this.Controls.Add(this.treeViewData);
            this.Controls.Add(this.btnSelectFolder);
            this.Controls.Add(this.rtbLog);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.txtFilePath);
            this.Controls.Add(this.btnSelectFile);
            this.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblPage;

        private System.Windows.Forms.Button btnNext;

        private System.Windows.Forms.Button btnPrev;

        private System.Windows.Forms.Button btnToggleView;

        private System.Windows.Forms.RichTextBox rtbPrompt;

        private System.Windows.Forms.GroupBox groupBox4;

        private System.Windows.Forms.GroupBox groupBox3;

        private System.Windows.Forms.GroupBox groupBox2;

        private System.Windows.Forms.GroupBox groupBox1;

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelImages;

        private System.Windows.Forms.Panel panel3D;

        private System.Windows.Forms.TreeView treeViewData;

        private System.Windows.Forms.Button btnSelectFolder;

        private System.Windows.Forms.RichTextBox rtbLog;

        private System.Windows.Forms.Button btnRun;

        private System.Windows.Forms.TextBox txtFilePath;

        private System.Windows.Forms.Button btnSelectFile;

        #endregion
    }
}