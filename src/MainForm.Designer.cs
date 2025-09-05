namespace GE_Ranger_Programmer
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.dgvChannels = new System.Windows.Forms.DataGridView();
            this.colCh = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxFreq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRxFreq = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colTxTone = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRxTone = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.txtHexView = new System.Windows.Forms.TextBox();
            this.panelTop = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChannels)).BeginInit();
            this.panelTop.SuspendLayout();
            this.SuspendLayout();
            // 
            // dgvChannels
            // 
            this.dgvChannels.AllowUserToAddRows = false;
            this.dgvChannels.AllowUserToDeleteRows = false;
            this.dgvChannels.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvChannels.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCh,
            this.colTxFreq,
            this.colRxFreq,
            this.colTxTone,
            this.colRxTone});
            this.dgvChannels.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvChannels.Location = new System.Drawing.Point(0, 40);
            this.dgvChannels.Name = "dgvChannels";
            this.dgvChannels.Size = new System.Drawing.Size(584, 421);
            this.dgvChannels.TabIndex = 0;
            // 
            // colCh
            // 
            this.colCh.HeaderText = "Ch";
            this.colCh.Name = "colCh";
            this.colCh.Width = 40;
            // 
            // colTxFreq
            // 
            this.colTxFreq.HeaderText = "Tx Freq";
            this.colTxFreq.Name = "colTxFreq";
            // 
            // colRxFreq
            // 
            this.colRxFreq.HeaderText = "Rx Freq";
            this.colRxFreq.Name = "colRxFreq";
            // 
            // colTxTone
            // 
            this.colTxTone.HeaderText = "Tx Tone";
            this.colTxTone.Name = "colTxTone";
            // 
            // colRxTone
            // 
            this.colRxTone.HeaderText = "Rx Tone";
            this.colRxTone.Name = "colRxTone";
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Location = new System.Drawing.Point(12, 10);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Size = new System.Drawing.Size(75, 23);
            this.btnOpenFile.TabIndex = 1;
            this.btnOpenFile.Text = "Open File";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // txtHexView
            // 
            this.txtHexView.Dock = System.Windows.Forms.DockStyle.Right;
            this.txtHexView.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtHexView.Location = new System.Drawing.Point(584, 40);
            this.txtHexView.Multiline = true;
            this.txtHexView.Name = "txtHexView";
            this.txtHexView.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtHexView.Size = new System.Drawing.Size(200, 421);
            this.txtHexView.TabIndex = 2;
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.btnOpenFile);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(784, 40);
            this.panelTop.TabIndex = 3;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.dgvChannels);
            this.Controls.Add(this.txtHexView);
            this.Controls.Add(this.panelTop);
            this.Name = "MainForm";
            this.Text = "GE Ranger Programmer";
            ((System.ComponentModel.ISupportInitialize)(this.dgvChannels)).EndInit();
            this.panelTop.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvChannels;
        private System.Windows.Forms.Button btnOpenFile;
        private System.Windows.Forms.TextBox txtHexView;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCh;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxFreq;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRxFreq;
        private System.Windows.Forms.DataGridViewTextBoxColumn colTxTone;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRxTone;
    }
}
