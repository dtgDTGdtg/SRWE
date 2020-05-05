using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SRWE
{
	public partial class ProcessSelectorCtrl : UserControl
	{
		private DataTable m_dtProcessList;
		private Process m_process;

		public ProcessSelectorCtrl()
		{
			InitializeComponent();

			m_dtProcessList = new DataTable();
			DataColumn[] pKeys = new DataColumn[1];
			pKeys[0] = m_dtProcessList.Columns.Add("ProcessID", typeof(int));
			m_dtProcessList.PrimaryKey = pKeys;
			m_dtProcessList.Columns.Add("ModuleIcon", typeof(Image));
			m_dtProcessList.Columns.Add("ProcessName");
			m_dtProcessList.Columns.Add("WindowTitle");
			m_dtProcessList.Columns.Add("FileName");

			DGV_PROCESS_LIST.AutoGenerateColumns = false;
			DGV_PROCESS_LIST.DataSource = m_dtProcessList.DefaultView;
		}

		public Process SelectedProcess
		{
			get
			{
				if (DGV_PROCESS_LIST.SelectedRows.Count > 0)
				{
					DataRowView drv = (DataRowView)DGV_PROCESS_LIST.SelectedRows[0].DataBoundItem;

					if (m_process == null || m_process.HasExited || m_process.Id != (int)drv[0])
						m_process = Process.GetProcessById((int)drv[0]);
					return m_process;
				}
				return null;
			}
		}

		private void BTN_REFRESH_Click(object sender, EventArgs e)
		{
			RefreshProcessList();
		}

		private void DGV_PROCESS_LIST_SelectionChanged(object sender, EventArgs e)
		{
			BTN_OPEN.Enabled = (DGV_PROCESS_LIST.SelectedRows.Count > 0);
		}

		public void RefreshProcessList()
		{
			m_dtProcessList.Clear();
			DataRow row;
			Process currentProcess = Process.GetCurrentProcess();

			foreach (Process process in Process.GetProcesses())
			{
				try
				{
					if (process.SessionId == currentProcess.SessionId && process.Id != currentProcess.Id)
					{
						row = m_dtProcessList.NewRow();
						row[0] = process.Id;
						row[1] = System.Drawing.Icon.ExtractAssociatedIcon(process.MainModule.FileName).ToBitmap();
						row[2] = process.MainModule.ModuleName;
						row[3] = process.MainWindowTitle;
						row[4] = process.MainModule.FileName;

						if (IsRecentProcess(process.ProcessName))
							m_dtProcessList.Rows.InsertAt(row, 0);
						else
							m_dtProcessList.Rows.Add(row);
					}
				}
				catch
                {
					// exception can be caused by anything, e.g. lack of access rights. Ignore process. 
                }
			}
			if (DGV_PROCESS_LIST.RowCount > 0)
			{
				DGV_PROCESS_LIST.ClearSelection();
				DGV_PROCESS_LIST.CurrentCell = DGV_PROCESS_LIST.Rows[0].Cells[1];
			}
		}

		private bool IsRecentProcess(string processName)
		{
			foreach (string name in SRWE_Settings.RecentProcesses)
			{
				if (!string.IsNullOrEmpty(name) && name.Equals(processName, StringComparison.CurrentCultureIgnoreCase))
					return true;
			}
			return false;
		}
	}
}
