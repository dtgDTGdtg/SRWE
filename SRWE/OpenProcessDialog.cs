using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SRWE
{
	public partial class OpenProcessDialog : Form
	{
		private ProcessSelectorCtrl m_ctrlProcessSelector;

		public OpenProcessDialog()
		{
			InitializeComponent();

			Point location = new Point(4, 4);

			m_ctrlProcessSelector = new ProcessSelectorCtrl();
			m_ctrlProcessSelector.Location = location;
			m_ctrlProcessSelector.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
			m_ctrlProcessSelector.BTN_OPEN.Click += new EventHandler(BTN_OPEN_Click);
			m_ctrlProcessSelector.DGV_PROCESS_LIST.CellDoubleClick += new DataGridViewCellEventHandler(DGV_PROCESS_LIST_CellDoubleClick);
			m_ctrlProcessSelector.BTN_CANCEL.Click += new EventHandler(BTN_CANCEL_Click);
			this.Controls.Add(m_ctrlProcessSelector);
			this.AcceptButton = m_ctrlProcessSelector.BTN_OPEN;
			this.CancelButton = m_ctrlProcessSelector.BTN_CANCEL;
		}

		private void OpenProcessDialog_Load(object sender, EventArgs e)
		{
			if (Owner != null)
				this.Icon = this.Owner.Icon;

			m_ctrlProcessSelector.RefreshProcessList();
		}

		void BTN_OPEN_Click(object sender, EventArgs e)
		{
			if (m_ctrlProcessSelector.SelectedProcess == null)
			{
				MessageBox.Show("Please select a process.", "SRWE", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
		}

		void DGV_PROCESS_LIST_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;

			BTN_OPEN_Click(sender, EventArgs.Empty);
		}

		void BTN_CANCEL_Click(object sender, EventArgs e)
		{
			this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		}

		public Process SelectedProcess
		{
			get { return m_ctrlProcessSelector.SelectedProcess; }
		}
	}
}
