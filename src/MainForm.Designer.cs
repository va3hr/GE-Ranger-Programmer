using System;
using System.Windows.Forms;

namespace GE_Ranger_Programmer
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private DataGridView grid;

        /// <summary>Clean up any resources being used.</summary>
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
            this.components = new System.ComponentModel.Container();
            this.grid = new System.Windows.Forms.DataGridView();
            var colCH = new DataGridViewTextBoxColumn() { HeaderText = "CH", Name = "colCH", Width = 40, ReadOnly = true };
            var colTxMHz = new DataGridViewTextBoxColumn() { HeaderText = "Tx MHz", Name = "colTxMHz", Width = 80 };
            var colRxMHz = new DataGridViewTextBoxColumn() { HeaderText = "Rx MHz", Name = "colRxMHz", Width = 80 };
            var colTxTone = new DataGridViewTextBoxColumn() { HeaderText = "Tx Tone", Name = "colTxTone", Width = 80 };
            var colRxTone = new DataGridViewTextBoxColumn() { HeaderText = "Rx Tone", Name = "colRxTone", Width = 80 };
            var colCct = new DataGridViewTextBoxColumn() { HeaderText = "cct", Name = "colCct", Width = 40 };
            var colSte = new DataGridViewTextBoxColumn() { HeaderText = "ste", Name = "colSte", Width = 40 };
            var colHex = new DataGridViewTextBoxColumn() { HeaderText = "Hex", Name = "colHex", Width = 300 };

            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();

            // grid
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.RowHeadersVisible = false;
            this.grid.Dock = DockStyle.Fill;
            this.grid.Name = "grid";
            this.grid.TabIndex = 0;
            this.grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.grid.Columns.AddRange(new DataGridViewColumn[] { colCH, colTxMHz, colRxMHz, colTxTone, colRxTone, colCct, colSte, colHex });

            // MainForm
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1000, 620);
            this.Controls.Add(this.grid);
            this.Name = "MainForm";
            this.Text = "X2212 Programmer";

            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
