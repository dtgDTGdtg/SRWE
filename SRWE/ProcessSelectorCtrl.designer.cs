namespace SRWE
{
	partial class ProcessSelectorCtrl
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.COL_WINDOW_TITLE = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.COL_PROCESS_NAME = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.COL_ICON = new System.Windows.Forms.DataGridViewImageColumn();
			this.DGV_PROCESS_LIST = new System.Windows.Forms.DataGridView();
			this.COL_LOCATION = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.BTN_CANCEL = new System.Windows.Forms.Button();
			this.BTN_OPEN = new System.Windows.Forms.Button();
			this.BTN_REFRESH = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.DGV_PROCESS_LIST)).BeginInit();
			this.SuspendLayout();
			// 
			// COL_WINDOW_TITLE
			// 
			this.COL_WINDOW_TITLE.DataPropertyName = "WindowTitle";
			this.COL_WINDOW_TITLE.HeaderText = "Window Title";
			this.COL_WINDOW_TITLE.Name = "COL_WINDOW_TITLE";
			this.COL_WINDOW_TITLE.ReadOnly = true;
			this.COL_WINDOW_TITLE.Width = 240;
			// 
			// COL_PROCESS_NAME
			// 
			this.COL_PROCESS_NAME.DataPropertyName = "ProcessName";
			dataGridViewCellStyle1.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.COL_PROCESS_NAME.DefaultCellStyle = dataGridViewCellStyle1;
			this.COL_PROCESS_NAME.HeaderText = "Process Name";
			this.COL_PROCESS_NAME.Name = "COL_PROCESS_NAME";
			this.COL_PROCESS_NAME.ReadOnly = true;
			this.COL_PROCESS_NAME.Width = 160;
			// 
			// COL_ICON
			// 
			this.COL_ICON.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
			this.COL_ICON.DataPropertyName = "ModuleIcon";
			this.COL_ICON.HeaderText = "";
			this.COL_ICON.Name = "COL_ICON";
			this.COL_ICON.ReadOnly = true;
			this.COL_ICON.Width = 5;
			// 
			// DGV_PROCESS_LIST
			// 
			this.DGV_PROCESS_LIST.AllowUserToAddRows = false;
			this.DGV_PROCESS_LIST.AllowUserToDeleteRows = false;
			this.DGV_PROCESS_LIST.AllowUserToResizeRows = false;
			this.DGV_PROCESS_LIST.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.DGV_PROCESS_LIST.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.DGV_PROCESS_LIST.BackgroundColor = System.Drawing.SystemColors.Window;
			this.DGV_PROCESS_LIST.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle2.Padding = new System.Windows.Forms.Padding(0, 3, 0, 4);
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.DGV_PROCESS_LIST.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
			this.DGV_PROCESS_LIST.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.DGV_PROCESS_LIST.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.COL_ICON,
            this.COL_PROCESS_NAME,
            this.COL_WINDOW_TITLE,
            this.COL_LOCATION});
			this.DGV_PROCESS_LIST.Location = new System.Drawing.Point(4, 4);
			this.DGV_PROCESS_LIST.Margin = new System.Windows.Forms.Padding(4);
			this.DGV_PROCESS_LIST.MultiSelect = false;
			this.DGV_PROCESS_LIST.Name = "DGV_PROCESS_LIST";
			this.DGV_PROCESS_LIST.ReadOnly = true;
			this.DGV_PROCESS_LIST.RowHeadersVisible = false;
			this.DGV_PROCESS_LIST.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.DGV_PROCESS_LIST.Size = new System.Drawing.Size(608, 395);
			this.DGV_PROCESS_LIST.TabIndex = 0;
			this.DGV_PROCESS_LIST.SelectionChanged += new System.EventHandler(this.DGV_PROCESS_LIST_SelectionChanged);
			// 
			// COL_LOCATION
			// 
			this.COL_LOCATION.DataPropertyName = "FileName";
			this.COL_LOCATION.HeaderText = "Location";
			this.COL_LOCATION.Name = "COL_LOCATION";
			this.COL_LOCATION.ReadOnly = true;
			this.COL_LOCATION.Width = 240;
			// 
			// BTN_CANCEL
			// 
			this.BTN_CANCEL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.BTN_CANCEL.Location = new System.Drawing.Point(532, 407);
			this.BTN_CANCEL.Margin = new System.Windows.Forms.Padding(4);
			this.BTN_CANCEL.Name = "BTN_CANCEL";
			this.BTN_CANCEL.Size = new System.Drawing.Size(80, 23);
			this.BTN_CANCEL.TabIndex = 3;
			this.BTN_CANCEL.Text = "Cancel";
			this.BTN_CANCEL.UseVisualStyleBackColor = true;
			// 
			// BTN_OPEN
			// 
			this.BTN_OPEN.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.BTN_OPEN.Location = new System.Drawing.Point(444, 407);
			this.BTN_OPEN.Margin = new System.Windows.Forms.Padding(4);
			this.BTN_OPEN.Name = "BTN_OPEN";
			this.BTN_OPEN.Size = new System.Drawing.Size(80, 23);
			this.BTN_OPEN.TabIndex = 1;
			this.BTN_OPEN.Text = "Open";
			this.BTN_OPEN.UseVisualStyleBackColor = true;
			// 
			// BTN_REFRESH
			// 
			this.BTN_REFRESH.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.BTN_REFRESH.Location = new System.Drawing.Point(4, 407);
			this.BTN_REFRESH.Margin = new System.Windows.Forms.Padding(4);
			this.BTN_REFRESH.Name = "BTN_REFRESH";
			this.BTN_REFRESH.Size = new System.Drawing.Size(80, 23);
			this.BTN_REFRESH.TabIndex = 2;
			this.BTN_REFRESH.Text = "Refresh";
			this.BTN_REFRESH.UseVisualStyleBackColor = true;
			this.BTN_REFRESH.Click += new System.EventHandler(this.BTN_REFRESH_Click);
			// 
			// ProcessSelectorCtrl
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.DGV_PROCESS_LIST);
			this.Controls.Add(this.BTN_CANCEL);
			this.Controls.Add(this.BTN_OPEN);
			this.Controls.Add(this.BTN_REFRESH);
			this.Name = "ProcessSelectorCtrl";
			this.Size = new System.Drawing.Size(616, 434);
			((System.ComponentModel.ISupportInitialize)(this.DGV_PROCESS_LIST)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.DataGridViewTextBoxColumn COL_WINDOW_TITLE;
		private System.Windows.Forms.DataGridViewTextBoxColumn COL_PROCESS_NAME;
		private System.Windows.Forms.DataGridViewImageColumn COL_ICON;
		internal System.Windows.Forms.DataGridView DGV_PROCESS_LIST;
		private System.Windows.Forms.DataGridViewTextBoxColumn COL_LOCATION;
		internal System.Windows.Forms.Button BTN_CANCEL;
		internal System.Windows.Forms.Button BTN_OPEN;
		private System.Windows.Forms.Button BTN_REFRESH;

	}
}
