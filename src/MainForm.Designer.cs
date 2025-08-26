// AUTO-GENERATED UI RESCUE FILE
// Replace NAMESPACE_PLACEHOLDER below with the exact namespace used in your MainForm.cs.
// This restores the DataGridView-based UI and defines both "grid" and "dataGridView1" fields
// (pointing to the same control) so either name used in your code will compile.
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;

namespace NAMESPACE_PLACEHOLDER
{
    partial class MainForm
    {
        private IContainer components = null;

        // Both names are provided to avoid mismatches
        private DataGridView grid;
        private DataGridView dataGridView1;

        /// <summary>Clean up any resources being used.</summary>
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
            this.components = new Container();
            var dgv = new DataGridView();
            this.grid = dgv;
            this.dataGridView1 = dgv;

            // Basic form
            this.SuspendLayout();
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1024, 600);
            this.Text = "X2212 Programmer";

            // DataGridView setup
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowHeadersVisible = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.CellSelect;
            dgv.Dock = DockStyle.Fill;
            dgv.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Columns matching your UI
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="CH", HeaderText="CH", ReadOnly=true, FillWeight=8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="TxMHz", HeaderText="Tx MHz", FillWeight=16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="RxMHz", HeaderText="Rx MHz", FillWeight=16 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="TxTone", HeaderText="Tx Tone", FillWeight=15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="RxTone", HeaderText="Rx Tone", FillWeight=15 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="cct", HeaderText="cct", FillWeight=8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="ste", HeaderText="ste", FillWeight=8 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn(){ Name="Hex", HeaderText="Hex", ReadOnly=true, FillWeight=22 });

            // Add control to form
            this.Controls.Add(dgv);
            this.ResumeLayout(false);
        }
        #endregion
    }
}