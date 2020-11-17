using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Linq;

namespace SRWE
{
	public partial class MainForm : Form
	{
		[Flags]
		private enum States
		{
			None = 0,
			SkipRefreshData = 1,
			UpdateWindowRect = 2,
			UpdateWindowStyle = 4,
			UpdateWindowExStyle = 8,
			IgnoreChangedEvents = 0x10,
			SkipUpdateGUI = 0x20,
			FixMultipleActivatedCalls = 0x40
		}

		private Process m_selectedProcess;
		private States m_states;
		private DataTable m_dtWS;
		private DataTable m_dtWS_EX;

		public MainForm()
		{
			InitializeComponent();
			this.MinimumSize = this.Size;

			m_states = States.None;

			OFD_PROFILE.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			SFD_PROFILE.InitialDirectory = OFD_PROFILE.InitialDirectory;
			TIMER_MAIN.Interval = SRWE_Settings.UpdateInterval;

			// initialize Recent profile menu with proper default elements
			for(int i = 0; i < SRWE_Defaults.MaxNumberOfRecentProfiles; i++)
			{
				var toAdd = new ToolStripMenuItem() {
					Name = "TSMI_PROFILE1",
					Text = "Profile-" + i
				};
				toAdd.Click += new System.EventHandler(this.TSMI_PROFILE_Click);
				this.TSI_PROFILE_RECENT.DropDownItems.Add(toAdd);
			}
			InitializeWindowStyles();
		}

#region Events
		private void MainForm_Load(object sender, EventArgs e)
		{
			_aboutLinkLabel.Text = Application.ProductName + " v" + Application.ProductVersion + ".  © _DTG_ / Others";
			ReflectSettingsInUI();
			UpdateCaption();
			RefreshRecentProfilesMenu();
			AutoAttachToLastProcess();
		}

		private void AutoAttachToLastProcess()
		{
			if(!SRWE_Settings.AutoAttachToLastKnownProcess)
			{
				return;
			}
			var moduleNameToProcess = new Dictionary<string, Process>();
			foreach(var process in Process.GetProcesses())
			{
				if(moduleNameToProcess.ContainsKey(process.ProcessName))
				{
					continue;
				}
				moduleNameToProcess[process.ProcessName] = process;
			}
			foreach(var lastAttachedProcess in SRWE_Settings.RecentProcesses)
			{
				Process activeProcess = null;
				if(moduleNameToProcess.TryGetValue(lastAttachedProcess, out activeProcess))
				{
					// process is active. Attach to it!
					AttachToProcess(activeProcess);
					break;
				}
			}
		}

		private void MainForm_Activated(object sender, EventArgs e)
		{
			if ((m_states & States.FixMultipleActivatedCalls) != 0) return;

			m_states |= States.FixMultipleActivatedCalls;

			UpdateGUI(false);
			RefreshData(false);
		}


		private void _forceExitSizeMoveCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SRWE_Settings.ForceExitSizeMoveMessage = _forceExitSizeMoveCheckBox.Checked;
		}


		private void _aboutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("https://github.com/dtgDTGdtg/SRWE");
		}

		private void _autoAttachToLastKnownCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			SRWE_Settings.AutoAttachToLastKnownProcess = _autoAttachToLastKnownCheckBox.Checked;
		}

		private void TIMER_MAIN_Tick(object sender, EventArgs e)
		{
			if (!this.ContainsFocus) return;

			UpdateGUI(true);

			if ((m_states & States.UpdateWindowStyle) != 0)
				UpdateWindowStyles(false);
			if ((m_states & States.UpdateWindowExStyle) != 0)
				UpdateWindowStyles(true);

			RefreshData(true);
		}

		private void TSI_OPEN_PROCESS_Click(object sender, EventArgs e)
		{
			OpenProcessDialog opd = new OpenProcessDialog();

			if (opd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
			{
				AttachToProcess(opd.SelectedProcess);
			}
		}

		private void AttachToProcess(Process toAttachTo)
		{
			m_selectedProcess = toAttachTo;
			UpdateCaption();
			UpdateWindowTree();
			SRWE_Settings.AddRecentProcess(m_selectedProcess.ProcessName);
		}

		private void TSI_REFRESH_Click(object sender, EventArgs e)
		{
			UpdateWindowTree();
		}

		private void TSI_PROFILE_LOAD_Click(object sender, EventArgs e)
		{
			if (OFD_PROFILE.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

			XPathDocument xpDoc = new XPathDocument(OFD_PROFILE.FileName);
			XPathNavigator navProfile = xpDoc.CreateNavigator().SelectSingleNode("SRWE/Profile");
			XPathNodeIterator iterator = navProfile.Select("Window");

			UncheckTreeViewNodes(TV_WINDOW_TREE.Nodes);

			while (iterator.MoveNext())
				UpdateWindowFromProfile(iterator.Current);

			SRWE_Settings.AddRecentProfile(OFD_PROFILE.FileName);
			RefreshRecentProfilesMenu();
		}

		private void TSI_PROFILE_SAVE_Click(object sender, EventArgs e)
		{
			if (SFD_PROFILE.ShowDialog(this) != System.Windows.Forms.DialogResult.OK) return;

			MemoryStream ms = new MemoryStream(Properties.Resources.XML_Profile);
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(ms);
			ms.Close();

			XmlElement xmlProfile = xmlDoc.DocumentElement["Profile"];
			AddWindowsToProfile(xmlProfile, TV_WINDOW_TREE.Nodes);
			xmlDoc.Save(SFD_PROFILE.FileName);

			SRWE_Settings.AddRecentProfile(SFD_PROFILE.FileName);
			RefreshRecentProfilesMenu();
		}

		private void TSMI_PROFILE_Click(object sender, EventArgs e)
		{
			string profilePath = (sender as ToolStripDropDownItem).ToolTipText;

			if (File.Exists(profilePath))
			{
				XPathDocument xpDoc = new XPathDocument(profilePath);
				XPathNavigator navProfile = xpDoc.CreateNavigator().SelectSingleNode("SRWE/Profile");
				XPathNodeIterator iterator = navProfile.Select("Window");

				UncheckTreeViewNodes(TV_WINDOW_TREE.Nodes);

				while (iterator.MoveNext())
					UpdateWindowFromProfile(iterator.Current);

				SRWE_Settings.AddRecentProfile(profilePath); // Brings profile to TOP of the Recent Profiles list.
			}
			else
				SRWE_Settings.RemoveRecentProfile(profilePath);

			RefreshRecentProfilesMenu();
		}

		private void TV_WINDOW_TREE_AfterSelect(object sender, TreeViewEventArgs e)
		{
			UpdateGUI(false);
			RefreshData(false);
		}

		private void TV_WINDOW_TREE_AfterCheck(object sender, TreeViewEventArgs e)
		{
			UpdateGUI(false);
		}

		private void TABCTRL_MAIN_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Tab page change
			UpdateGUI(false);
			RefreshData(false);
		}

		private void EDT_WINRC_TextChanged(object sender, EventArgs e)
		{
			if ((m_states & States.IgnoreChangedEvents) != 0) return;

			m_states |= States.UpdateWindowRect;
		}

		private void BTN_ALIGN_LEFT_Click(object sender, EventArgs e)
		{
			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
				win.PosX = Screen.PrimaryScreen.WorkingArea.Left;
			else
				win.PosX = 0;

			win.ApplyChanges();
		}

		private void BTN_ALIGN_HCENTER_Click(object sender, EventArgs e)
		{
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
				win.PosX = (Screen.PrimaryScreen.WorkingArea.Width - win.Width) / 2;
			else
			{
				win.Parent.RefreshRectangles();
				win.PosX = (win.Parent.ClientRect.Width - win.Width) / 2;
			}
			win.ApplyChanges();
		}

		private void BTN_ALIGN_RIGHT_Click(object sender, EventArgs e)
		{
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
				win.PosX = Screen.PrimaryScreen.WorkingArea.Width - win.Width;
			else
			{
				win.Parent.RefreshRectangles();
				win.PosX = win.Parent.ClientRect.Width - win.Width;
			}
			win.ApplyChanges();
		}

		private void BTN_ALIGN_TOP_Click(object sender, EventArgs e)
		{
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
				win.PosY = Screen.PrimaryScreen.WorkingArea.Top;
			else
				win.PosY = 0;

			win.ApplyChanges();
		}

		private void BTN_ALIGN_VCENTER_Click(object sender, EventArgs e)
		{
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
				win.PosY = (Screen.PrimaryScreen.WorkingArea.Height - win.Height) / 2;
			else
			{
				win.Parent.RefreshRectangles();
				win.PosY = (win.Parent.ClientRect.Height - win.Height) / 2;
			}
			win.ApplyChanges();
		}

		private void BTN_ALIGN_BOTTOM_Click(object sender, EventArgs e)
		{
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
				win.PosY = Screen.PrimaryScreen.WorkingArea.Height - win.Height;
			else
			{
				win.Parent.RefreshRectangles();
				win.PosY = win.Parent.ClientRect.Height - win.Height;
			}
			win.ApplyChanges();
		}

		private void BTN_REM_BORDERS_Click(object sender, EventArgs e)
		{
			if (TV_WINDOW_TREE.SelectedNode == null) return;

			(TV_WINDOW_TREE.SelectedNode.Tag as Window).RemoveBorders();
		}

		private void BTN_FAKE_FULLSCREEN_Click(object sender, EventArgs e)
		{
			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
			{
				win.PosX = 0;
				win.PosY = 0;
				win.Width = Screen.PrimaryScreen.Bounds.Width;
				win.Height = Screen.PrimaryScreen.Bounds.Height;
				win.ApplyChanges();
			}
		}

		private void BTN_TASKBAR_MODE_Click(object sender, EventArgs e)
		{
			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (win.Parent == null)
			{
				win.PosX = Screen.PrimaryScreen.WorkingArea.Left;
				win.PosY = Screen.PrimaryScreen.WorkingArea.Top;
				win.Width = Screen.PrimaryScreen.WorkingArea.Width;
				win.Height = Screen.PrimaryScreen.WorkingArea.Height;
				win.ApplyChanges();
			}
		}

		private void DGV_WS_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;

			if (DGV_WS.IsCurrentCellDirty)
				DGV_WS.CommitEdit(DataGridViewDataErrorContexts.CurrentCellChange);

			UpdateWindowStyleFromGrid();
		}

		private void DGV_WS_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;

			DGV_WS.Rows[e.RowIndex].Cells[0].Value = !(bool)DGV_WS.Rows[e.RowIndex].Cells[0].Value;
			UpdateWindowStyleFromGrid();
		}

		private void DGV_WS_KeyUp(object sender, KeyEventArgs e)
		{
			if (DGV_WS.CurrentCell == null || DGV_WS.CurrentCell.RowIndex < 0 || DGV_WS.CurrentCell.ColumnIndex == 0) return;

			if (e.KeyCode == Keys.Space && !e.Control && !e.Alt && !e.Shift)
			{
				DGV_WS.Rows[DGV_WS.CurrentCell.RowIndex].Cells[0].Value = !(bool)DGV_WS.Rows[DGV_WS.CurrentCell.RowIndex].Cells[0].Value;
				UpdateWindowStyleFromGrid();
			}
		}

		private void DGV_WS_EX_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;

			if (DGV_WS_EX.IsCurrentCellDirty)
				DGV_WS_EX.CommitEdit(DataGridViewDataErrorContexts.CurrentCellChange);

			UpdateWindowExStyleFromGrid();
		}

		private void DGV_WS_EX_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0) return;

			DGV_WS_EX.Rows[e.RowIndex].Cells[0].Value = !(bool)DGV_WS_EX.Rows[e.RowIndex].Cells[0].Value;
			UpdateWindowExStyleFromGrid();
		}

		private void DGV_WS_EX_KeyUp(object sender, KeyEventArgs e)
		{
			if (DGV_WS_EX.CurrentCell == null || DGV_WS_EX.CurrentCell.RowIndex < 0 || DGV_WS_EX.CurrentCell.ColumnIndex == 0) return;

			if (e.KeyCode == Keys.Space && !e.Control && !e.Alt && !e.Shift)
			{
				DGV_WS_EX.Rows[DGV_WS_EX.CurrentCell.RowIndex].Cells[0].Value = !(bool)DGV_WS_EX.Rows[DGV_WS_EX.CurrentCell.RowIndex].Cells[0].Value;
				UpdateWindowExStyleFromGrid();
			}
		}

		private void EDT_WS_HEX_TextChanged(object sender, EventArgs e)
		{
			if ((m_states & States.IgnoreChangedEvents) != 0) return;

			m_states |= States.UpdateWindowStyle;
		}

		private void EDT_WSEX_HEX_TextChanged(object sender, EventArgs e)
		{
			if ((m_states & States.IgnoreChangedEvents) != 0) return;

			m_states |= States.UpdateWindowExStyle;
		}

		private void TSI_REFRESH_EnabledChanged(object sender, EventArgs e)
		{
			if (!TSI_REFRESH.Enabled) // Refresh button is disabled when no process is selected or selected process has exited.
			{
				TV_WINDOW_TREE.Nodes.Clear();
				ClearWindowInfo();
				m_selectedProcess = null;
				UpdateCaption();
			}
		}
#endregion
		

		private void InitializeWindowStyles()
		{
			// Init DGV_WS
			m_dtWS = new DataTable();
			DataColumn[] pmKey = new DataColumn[1];
			pmKey[0] = m_dtWS.Columns.Add("Value", typeof(uint)); // ColIndex = 0
			m_dtWS.PrimaryKey = pmKey;
			m_dtWS.Columns.Add("Activated", typeof(bool)); // ColIndex = 1
			m_dtWS.Columns.Add("Name"); // ColIndex = 2

			// Load from XML
			MemoryStream ms = new MemoryStream(Properties.Resources.XML_WindowStyles);
			XPathDocument xpDoc = new XPathDocument(ms);
			ms.Close();

			XPathNodeIterator iterator = xpDoc.CreateNavigator().Select("WindowStyles/WS/Item");
			DataRow row;

			while (iterator.MoveNext())
			{
				row = m_dtWS.NewRow();
				row[0] = uint.Parse(iterator.Current.SelectSingleNode("@Value").Value.Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
				row[1] = false;
				row[2] = iterator.Current.SelectSingleNode("@Name").Value;
				m_dtWS.Rows.Add(row);
			}
			DGV_WS.AutoGenerateColumns = false;
			DGV_WS.DataSource = m_dtWS.DefaultView;

			// Init DGV_WS_EX
			m_dtWS_EX = new DataTable();
			pmKey = new DataColumn[1];
			pmKey[0] = m_dtWS_EX.Columns.Add("Value", typeof(uint)); // ColIndex = 0
			m_dtWS_EX.PrimaryKey = pmKey;
			m_dtWS_EX.Columns.Add("Activated", typeof(bool)); // ColIndex = 1
			m_dtWS_EX.Columns.Add("Name"); // ColIndex = 2

			// Load from XML
			ms = new MemoryStream(Properties.Resources.XML_WindowStyles);
			xpDoc = new XPathDocument(ms);
			ms.Close();

			iterator = xpDoc.CreateNavigator().Select("WindowStyles/WS_EX/Item");

			while (iterator.MoveNext())
			{
				row = m_dtWS_EX.NewRow();
				row[0] = uint.Parse(iterator.Current.SelectSingleNode("@Value").Value.Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier);
				row[1] = false;
				row[2] = iterator.Current.SelectSingleNode("@Name").Value;
				m_dtWS_EX.Rows.Add(row);
			}
			DGV_WS_EX.AutoGenerateColumns = false;
			DGV_WS_EX.DataSource = m_dtWS_EX.DefaultView;
		}

		private void UpdateCaption()
		{
			if (m_selectedProcess != null && IsProcessRunning(m_selectedProcess))
				this.Text = Application.ProductName + " v" + Application.ProductVersion + " - " + m_selectedProcess.MainModule.ModuleName;
			else
				this.Text = Application.ProductName + " v" + Application.ProductVersion;
		}

		private static bool IsProcessRunning(Process process)
		{
			try { return !process.HasExited; }
			catch { return false; }
		}

		private void RefreshRecentProfilesMenu()
		{
			int nRecentProfilesMax = SRWE_Settings.RecentProfiles.Count;

			for (int i = 0, iMax = TSI_PROFILE_RECENT.DropDownItems.Count; i < iMax; i++)
			{
				if (i < nRecentProfilesMax)
				{
					TSI_PROFILE_RECENT.DropDownItems[i].ToolTipText = SRWE_Settings.RecentProfiles[i];
					TSI_PROFILE_RECENT.DropDownItems[i].Text = Path.GetFileName(TSI_PROFILE_RECENT.DropDownItems[i].ToolTipText);
					TSI_PROFILE_RECENT.DropDownItems[i].Visible = true;
				}
				else
					TSI_PROFILE_RECENT.DropDownItems[i].Visible = false;
			}
		}

		private void UpdateGUI(bool isCallerTimer)
		{
			if (!isCallerTimer)
				m_states |= States.SkipUpdateGUI;
			else if ((m_states & States.SkipUpdateGUI) != 0)
			{
				m_states ^= States.SkipUpdateGUI;
				return;
			}

			TSI_REFRESH.Enabled = IsProcessRunning(m_selectedProcess);
			TSI_PROFILE_LOAD.Enabled = (TSI_REFRESH.Enabled && TV_WINDOW_TREE.Nodes.Count > 0);
			TSI_PROFILE_SAVE.Enabled = (TSI_PROFILE_LOAD.Enabled && HasCheckedTreeNode(TV_WINDOW_TREE.Nodes));
			TSI_PROFILE_RECENT.Enabled = TSI_PROFILE_LOAD.Enabled;

			bool canEdit = (TV_WINDOW_TREE.SelectedNode != null && (TV_WINDOW_TREE.SelectedNode.Tag as Window).IsWindow);

			if (TABCTRL_MAIN.SelectedTab == TABPG_GENERAL)
			{
				EDT_WINRC_X.Enabled = canEdit;
				EDT_WINRC_Y.Enabled = canEdit;
				EDT_WINRC_WIDTH.Enabled = canEdit;
				EDT_WINRC_HEIGHT.Enabled = canEdit;
				EDT_WINRC_SCALE.Enabled = canEdit;
				_setWindowPositionAndSizeButton.Enabled = canEdit;
				_setWindowResolutionFromMegapixels.Enabled = canEdit;
				EDT_WINRC_MPX.Enabled = canEdit;
				EDT_WINRC_ASPRAT.Enabled = canEdit;

				BTN_ALIGN_LEFT.Enabled = canEdit;
				BTN_ALIGN_HCENTER.Enabled = canEdit;
				BTN_ALIGN_RIGHT.Enabled = canEdit;
				BTN_ALIGN_TOP.Enabled = canEdit;
				BTN_ALIGN_VCENTER.Enabled = canEdit;
				BTN_ALIGN_BOTTOM.Enabled = canEdit;

				BTN_REM_BORDERS.Enabled = canEdit;

				BTN_FAKE_FULLSCREEN.Enabled = (canEdit && (TV_WINDOW_TREE.SelectedNode.Tag as Window).Parent == null);
				BTN_TASKBAR_MODE.Enabled = BTN_FAKE_FULLSCREEN.Enabled;
				UpdateOutputResolution();
			}
			else if (TABCTRL_MAIN.SelectedTab == TABPG_STYLES)
			{
				DGV_WS.Enabled = canEdit;
				DGV_WS_EX.Enabled = canEdit;
				EDT_WS_HEX.Enabled = canEdit;
				EDT_WSEX_HEX.Enabled = canEdit;
			}
		}

		private bool HasCheckedTreeNode(TreeNodeCollection treeNodes)
		{
			foreach (TreeNode tn in treeNodes)
			{
				if (tn.Checked || HasCheckedTreeNode(tn.Nodes))
					return true;
			}
			return false;
		}

		private void UpdateWindowRect()
		{
			m_states = (m_states | States.UpdateWindowRect) ^ States.UpdateWindowRect;
			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			win.PosX = SRWE_Utility.SAFE_String_2_Int(EDT_WINRC_X.Text, win.PosX);
			win.PosY = SRWE_Utility.SAFE_String_2_Int(EDT_WINRC_Y.Text, win.PosY);
			win.Scale = SRWE_Utility.SAFE_String_2_Float(EDT_WINRC_SCALE.Text);
			win.Width = (int)(SRWE_Utility.SAFE_String_2_Int(EDT_WINRC_WIDTH.Text, win.Width)*win.Scale);
			win.Height = (int)(SRWE_Utility.SAFE_String_2_Int(EDT_WINRC_HEIGHT.Text, win.Height)*win.Scale);
			win.ApplyChanges();
			EDT_WINRC_SCALE.Text = "1";

		}

		private int[] CalculateResolution()
        {
			// Width and Height
			int[] resolution = { 0, 0 };
			float ratio = SRWE_Utility.SAFE_ParseRatio(EDT_WINRC_ASPRAT.Text);
			float mpx = SRWE_Utility.SAFE_String_2_Float(EDT_WINRC_MPX.Text)*1e6f;
			resolution[1] = (int)Math.Sqrt(mpx / ratio);
			resolution[0] = (int)(resolution[1] * ratio);

			return resolution;
        }

		private void UpdateOutputResolution()
        {
			var resolution = CalculateResolution();
			EDT_OUTPUT_RESOLUTION.Text = String.Format("{0}x{1}", resolution[0], resolution[1]);
        }

		private void UpdateWindowFromMegapixels()
		{
			m_states = (m_states | States.UpdateWindowRect) ^ States.UpdateWindowRect;
			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			var resolution = CalculateResolution();
			win.Width = resolution[0];
			win.Height = resolution[1];

			win.ApplyChanges();

		}

		private void UpdateWindowStyles(bool exStyles)
		{
			if (exStyles)
				m_states = (m_states | States.UpdateWindowExStyle) ^ States.UpdateWindowExStyle;
			else
				m_states = (m_states | States.UpdateWindowStyle) ^ States.UpdateWindowStyle;

			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			if (exStyles)
			{
				win.ExStyle = (uint)SRWE_Utility.SAFE_HexString_2_Int(EDT_WSEX_HEX.Text, (int)win.ExStyle);
				foreach (DataRow dr in m_dtWS_EX.Rows) dr[1] = (win.ExStyle & (uint)dr[0]) != 0;
			}
			else
			{
				win.Style = (uint)SRWE_Utility.SAFE_HexString_2_Int(EDT_WS_HEX.Text, (int)win.Style);
				foreach (DataRow dr in m_dtWS.Rows) dr[1] = (win.Style & (uint)dr[0]) != 0;
			}
			win.ApplyChanges();
		}

		private void RefreshData(bool isCallerTimer)
		{
			if (!isCallerTimer)
				m_states |= States.SkipRefreshData;
			else if ((m_states & States.SkipRefreshData) != 0)
			{
				m_states ^= States.SkipRefreshData;
				return;
			}

			m_states |= States.IgnoreChangedEvents;

			if (TV_WINDOW_TREE.SelectedNode == null) return;

			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;
			win.Refresh();

			if (TABCTRL_MAIN.SelectedTab == TABPG_GENERAL)
				RefreshGeneralTab(win);
			else if (TABCTRL_MAIN.SelectedTab == TABPG_STYLES)
				RefreshWindowStylesTab(win);

			m_states ^= States.IgnoreChangedEvents;
		}

		private void RefreshGeneralTab(Window win)
		{
			if (!EDT_WINRC_X.Focused) EDT_WINRC_X.Text = win.PosX.ToString();
			if (!EDT_WINRC_Y.Focused) EDT_WINRC_Y.Text = win.PosY.ToString();
			if (!EDT_WINRC_WIDTH.Focused) EDT_WINRC_WIDTH.Text = win.Width.ToString();
			if (!EDT_WINRC_HEIGHT.Focused) EDT_WINRC_HEIGHT.Text = win.Height.ToString();

			EDT_HANDLE.Text = win.Handle.ToString("X8");
			EDT_CLASS.Text = win.Class;
			EDT_TEXT.Text = win.Text;
			EDT_HIER_NUM.Text = win.HierarchicalID;
			EDT_THREAD_ID.Text = win.ThreadID.ToString("X8");

			EDT_CL_POSX.Text = win.ClientRect.left.ToString();
			EDT_CL_POSY.Text = win.ClientRect.top.ToString();
			EDT_CL_WIDTH.Text = win.ClientRect.Width.ToString();
			EDT_CL_HEIGHT.Text = win.ClientRect.Height.ToString();

			EDT_BORDER_CX.Text = win.BorderWidth.ToString();
			EDT_BORDER_CY.Text = win.BorderHeight.ToString();
		}

		private void RefreshWindowStylesTab(Window win)
		{
			foreach (DataRow dr in m_dtWS.Rows) dr[1] = (win.Style & (uint)dr[0]) != 0;
			foreach (DataRow dr in m_dtWS_EX.Rows) dr[1] = (win.ExStyle & (uint)dr[0]) != 0;

			if (!EDT_WS_HEX.Focused) EDT_WS_HEX.Text = win.Style.ToString("X8");
			if (!EDT_WSEX_HEX.Focused) EDT_WSEX_HEX.Text = win.ExStyle.ToString("X8");
		}

		private void UpdateWindowTree()
		{
			TV_WINDOW_TREE.Nodes.Clear();
			ClearWindowInfo();

			if (m_selectedProcess != null && IsProcessRunning(m_selectedProcess))
			{
				List<Window> wndList = Window.GetProcessWindows(m_selectedProcess);
				CreateTreeElements(TV_WINDOW_TREE.Nodes, wndList);
				TV_WINDOW_TREE.Focus();
				if(TV_WINDOW_TREE.Nodes.Count > 0)
				{
					TV_WINDOW_TREE.SelectedNode = TV_WINDOW_TREE.Nodes[0];
				}
			}
		}

		private void ClearWindowInfo()
		{
			EDT_WINRC_X.Text = "";
			EDT_WINRC_Y.Text = "";
			EDT_WINRC_WIDTH.Text = "";
			EDT_WINRC_HEIGHT.Text = "";
			EDT_HANDLE.Text = "";
			EDT_CLASS.Text = "";
			EDT_TEXT.Text = "";
			EDT_HIER_NUM.Text = "";
			EDT_THREAD_ID.Text = "";
			EDT_CL_POSX.Text = "";
			EDT_CL_POSY.Text = "";
			EDT_CL_WIDTH.Text = "";
			EDT_CL_HEIGHT.Text = "";
			EDT_BORDER_CX.Text = "";
			EDT_BORDER_CY.Text = "";

			foreach (DataRow dRow in m_dtWS.Rows) dRow[1] = false;
			foreach (DataRow dRow in m_dtWS_EX.Rows) dRow[1] = false;

			EDT_WS_HEX.Text = "";
			EDT_WSEX_HEX.Text = "";
		}

		private void CreateTreeElements(TreeNodeCollection treeNodes, List<Window> wndList)
		{
			if (wndList == null || wndList.Count < 1) return;

			foreach (Window wi in wndList)
			{
				TreeNode newNode = treeNodes.Add(wi.DisplayName);
				newNode.Tag = wi;
				CreateTreeElements(newNode.Nodes, wi.m_childList);
			}
		}

		private void UncheckTreeViewNodes(TreeNodeCollection treeNodes)
		{
			foreach (TreeNode tn in treeNodes)
			{
				tn.Checked = false;
				UncheckTreeViewNodes(tn.Nodes);
			}
		}

		private void UpdateWindowFromProfile(XPathNavigator navWindow)
		{
			string hierID = navWindow.SelectSingleNode("@HierID").Value;
			string[] numbers = hierID.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
			int[] ordinals = new int[numbers.Length];
			for (int i = 0, iMax = ordinals.Length; i < iMax; i++) ordinals[i] = int.Parse(numbers[i]) - 1;

			TreeNodeCollection treeNodes = TV_WINDOW_TREE.Nodes;
			TreeNode tNode = null;
			int nTopOrdinal = ordinals.Length - 1;

			for (int i = 0, iMax = ordinals.Length; i < iMax; i++)
			{
				if (ordinals[i] >= treeNodes.Count) throw new Exception("A window with hierarchical ID '" + hierID + "' not found");
				tNode = treeNodes[ordinals[i]];
				if (i < nTopOrdinal) treeNodes = tNode.Nodes;
			}

			if (tNode != null)
			{
				tNode.Checked = true;
				Window win = (Window)tNode.Tag;
				win.Refresh();

				win.PosX = SRWE_Utility.SAFE_String_2_Int(navWindow.SelectSingleNode("@PosX").Value, win.PosX);
				win.PosY = SRWE_Utility.SAFE_String_2_Int(navWindow.SelectSingleNode("@PosY").Value, win.PosY);
				win.Width = SRWE_Utility.SAFE_String_2_Int(navWindow.SelectSingleNode("@Width").Value, win.Width);
				win.Height = SRWE_Utility.SAFE_String_2_Int(navWindow.SelectSingleNode("@Height").Value, win.Height);
				win.Style = (uint)SRWE_Utility.SAFE_HexString_2_Int(navWindow.SelectSingleNode("@Style").Value, (int)win.Style);
				win.ExStyle = (uint)SRWE_Utility.SAFE_HexString_2_Int(navWindow.SelectSingleNode("@ExStyle").Value, (int)win.ExStyle);
				win.ApplyChanges();
			}
		}

		private void AddWindowsToProfile(XmlElement xmlProfile, TreeNodeCollection treeNodes)
		{
			if (treeNodes == null || treeNodes.Count < 1) return;

			foreach (TreeNode tNode in treeNodes)
			{
				if (tNode.Checked)
				{
					Window win = (Window)tNode.Tag;
					win.Refresh();
					XmlElement xmlWindow = xmlProfile.OwnerDocument.CreateElement("Window");
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("HierID")).Value = win.HierarchicalID;
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("PosX")).Value = win.PosX.ToString();
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("PosY")).Value = win.PosY.ToString();
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("Width")).Value = win.Width.ToString();
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("Height")).Value = win.Height.ToString();
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("Style")).Value = win.Style.ToString("X8");
					xmlWindow.Attributes.Append(xmlProfile.OwnerDocument.CreateAttribute("ExStyle")).Value = win.ExStyle.ToString("X8");
					xmlProfile.AppendChild(xmlWindow);
				}
				AddWindowsToProfile(xmlProfile, tNode.Nodes);
			}
		}

		private void UpdateWindowStyleFromGrid()
		{
			if (TV_WINDOW_TREE.SelectedNode == null) return;
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			uint dwStyle = 0;

			foreach (DataRow dr in m_dtWS.Rows)
			{
				dwStyle |= (uint)dr[0];
				if ((bool)dr[1] == false) dwStyle ^= (uint)dr[0];
			}

			win.Style = dwStyle;
			win.ApplyChanges();

			m_states |= States.IgnoreChangedEvents;
			EDT_WS_HEX.Text = dwStyle.ToString("X8");
			m_states ^= States.IgnoreChangedEvents;
		}

		private void UpdateWindowExStyleFromGrid()
		{
			if (TV_WINDOW_TREE.SelectedNode == null) return;
			Window win = (Window)TV_WINDOW_TREE.SelectedNode.Tag;

			uint dwExStyle = 0;

			foreach (DataRow dr in m_dtWS_EX.Rows)
			{
				dwExStyle |= (uint)dr[0];
				if ((bool)dr[1] == false) dwExStyle ^= (uint)dr[0];
			}

			win.ExStyle = dwExStyle;
			win.ApplyChanges();

			m_states |= States.IgnoreChangedEvents;
			EDT_WSEX_HEX.Text = dwExStyle.ToString("X8");
			m_states ^= States.IgnoreChangedEvents;
		}

        private void TIMER_HOTKEYS_Tick(object sender, EventArgs e)
        {
            bool ctrl = (WinAPI.GetAsyncKeyState(Keys.ControlKey) & 0x8000) != 0;
            bool alt = (WinAPI.GetAsyncKeyState(Keys.Menu) & 0x8000) != 0;
            bool shift = (WinAPI.GetAsyncKeyState(Keys.ShiftKey) & 0x8000) != 0;

            foreach (SRWE_HotKey hotKey in SRWE_Settings.HotKeys)
            {
                if (!hotKey.HotKey.HasValue || hotKey.CTRL != ctrl || hotKey.ALT != alt || hotKey.SHIFT != shift) continue;

                if ((WinAPI.GetAsyncKeyState(hotKey.HotKey.Value) & 0x8000) != 0)
                {
                    switch (hotKey.Name)
                    {
                        case "RemoveBorders":
                            ActiveWindow_RemoveBorders(); break;
                        case "FakeFullScreen":
                            ActiveWindow_FakeFullScreen(); break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ActiveWindow_RemoveBorders()
        {
            int hWnd = WinAPI.GetForegroundWindow();
            if (hWnd == 0 || hWnd == (int)this.Handle) return;
            Window window = Window.GetFromHWND(hWnd);

            if (window != null) window.RemoveBorders();
        }

        private void ActiveWindow_FakeFullScreen()
        {
            int hWnd = WinAPI.GetForegroundWindow();
            if (hWnd == 0 || hWnd == (int)this.Handle) return;
            Window window = Window.GetFromHWND(hWnd);

            if (window != null)
            {
                window.PosX = 0;
                window.PosY = 0;
                window.Width = Screen.PrimaryScreen.Bounds.Width;
                window.Height = Screen.PrimaryScreen.Bounds.Height;
                window.ApplyChanges();
            }
        }


		private void ReflectSettingsInUI()
		{
			_forceExitSizeMoveCheckBox.Checked = SRWE_Settings.ForceExitSizeMoveMessage;
			_autoAttachToLastKnownCheckBox.Checked = SRWE_Settings.AutoAttachToLastKnownProcess;
			// grab the first profile (if any) and use that folder as the default folder for profiles. 
			var firstProfile = SRWE_Settings.RecentProfiles.FirstOrDefault();
			if(!string.IsNullOrEmpty(firstProfile))
			{
				try
				{
					OFD_PROFILE.InitialDirectory = Path.GetDirectoryName(firstProfile);
					SFD_PROFILE.InitialDirectory = OFD_PROFILE.InitialDirectory;
				}
				catch
				{
					// folder name is wrong, nothing we can do, silently skip it. 
				}
			}
		}

		private void _setWindowPositionAndSizeButton_Click(object sender, EventArgs e)
		{
			UpdateWindowRect();
		}

		private void _setWindowResolutionFromMegapixels_Click(object sender, EventArgs e)
		{
			UpdateWindowFromMegapixels();
		}

    }
}
