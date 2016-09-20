using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SRWE
{
	/// <summary>
	/// Window class
	/// </summary>
	class Window
	{
		private class ETWP_Param
		{
			public int m_nThreadID;
			public Window m_parent;
			public int m_hAppMainWnd;
			public List<Window> m_windowList;
		}

		[Flags]
		private enum Changes
		{
			None = 0,
			Pos = 1,
			Size = 2,
			Style = 4,
			ExStyle = 8
		}

		private int m_hWnd;
		private Window m_parent;
		private int m_nThreadID;
		private string m_hierID;
		public List<Window> m_childList;
		private WINDOWINFO m_windowInfo;
		private RECT m_relWindowRC;
		private RECT m_relClientRC;
		private string m_text;
		private string m_class;
		private Changes m_changes;

		private Window(int nHandle, Window parent, int nThreadID)
		{
			m_hWnd = nHandle;
			m_parent = parent;
			m_nThreadID = nThreadID;
			m_childList = new List<Window>();

			StringBuilder sb = new StringBuilder(256);
			WinAPI.RealGetWindowClass(m_hWnd, sb, sb.Capacity - 1);
			m_class = sb.ToString();

			m_windowInfo = new WINDOWINFO();
			m_windowInfo.cbSize = (uint)Marshal.SizeOf(m_windowInfo);

			m_relWindowRC = new RECT();
			m_relClientRC = new RECT();

			m_changes = Changes.None;

			Refresh();
			GetChildWindows();
		}

		public static List<Window> GetProcessWindows(Process process)
		{
			WinAPI.EnumWindowsProc etwp = new WinAPI.EnumWindowsProc(EnumThreadWndProc);
			ETWP_Param lParam = new ETWP_Param();
			lParam.m_hAppMainWnd = (int)process.MainWindowHandle;
			lParam.m_windowList = new List<Window>();
			GCHandle gch = GCHandle.Alloc(lParam);

			RestoreWindow(lParam.m_hAppMainWnd);

			foreach (ProcessThread thread in process.Threads)
			{
				lParam.m_nThreadID = thread.Id;
				WinAPI.EnumThreadWindows(thread.Id, etwp, GCHandle.ToIntPtr(gch));
			}
			gch.Free();

			GenerateHierarchicalIDs(lParam.m_windowList);

			return lParam.m_windowList;
		}

        public static Window GetFromHWND(int hWnd)
        {
            if (hWnd == 0 || !WinAPI.IsWindow(hWnd)) return null;

            return new Window(hWnd, null, 0);
        }

		private static void RestoreWindow(int hWnd)
		{
			if (hWnd == 0 || !WinAPI.IsWindow(hWnd)) return;

			if (WinAPI.IsIconic(hWnd)) WinAPI.ShowWindow(hWnd, WinAPI.SW_SHOWNOACTIVATE);
		}

		private static bool EnumThreadWndProc(int hwnd, IntPtr lParam)
		{
			if (lParam != IntPtr.Zero)
			{
				ETWP_Param param = (ETWP_Param)GCHandle.FromIntPtr(lParam).Target;

				if (param.m_hAppMainWnd != 0 && param.m_hAppMainWnd == hwnd)
					param.m_windowList.Insert(0, new Window(hwnd, null, param.m_nThreadID));
				else
					param.m_windowList.Add(new Window(hwnd, null, param.m_nThreadID));

				return true;
			}
			return false;
		}

		private static void GenerateHierarchicalIDs(List<Window> windowList)
		{
			for (int i = 0, iMax = windowList.Count; i < iMax; i++)
			{
				if (windowList[i].m_parent == null) windowList[i].m_hierID = (i + 1).ToString();
				else windowList[i].m_hierID = windowList[i].m_parent.m_hierID + '.' + (i + 1).ToString();
				GenerateHierarchicalIDs(windowList[i].m_childList);
			}
		}

		private void GetChildWindows()
		{
			WinAPI.EnumWindowsProc ecwp = new WinAPI.EnumWindowsProc(EnumChildProc);
			ETWP_Param lParam = new ETWP_Param();
			lParam.m_parent = this;
			lParam.m_nThreadID = m_nThreadID;
			GCHandle gch = GCHandle.Alloc(lParam);
			WinAPI.EnumChildWindows(m_hWnd, ecwp, GCHandle.ToIntPtr(gch));
			gch.Free();
		}

		private static bool EnumChildProc(int hwnd, IntPtr lParam)
		{
			if (lParam != IntPtr.Zero)
			{
				ETWP_Param param = (ETWP_Param)GCHandle.FromIntPtr(lParam).Target;
				param.m_parent.m_childList.Add(new Window(hwnd, param.m_parent, param.m_nThreadID));
				return true;
			}
			return false;
		}

		public void Refresh()
		{
			StringBuilder sb = new StringBuilder(256);
			WinAPI.GetWindowText(m_hWnd, sb, sb.Capacity - 1);
			m_text = sb.ToString();

			WinAPI.GetWindowInfo(m_hWnd, ref m_windowInfo);
			RECT.CopyRect(m_windowInfo.rcWindow, ref m_relWindowRC);
			RECT.CopyRect(m_windowInfo.rcClient, ref m_relClientRC);

			if (m_parent != null)
			{
				WINDOWINFO winInfoParent = new WINDOWINFO();
				winInfoParent.cbSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO));
				WinAPI.GetWindowInfo(m_parent.m_hWnd, ref winInfoParent);

				MakeRectRelative(winInfoParent.rcClient, ref m_relWindowRC);
				MakeRectRelative(winInfoParent.rcClient, ref m_relClientRC);
			}
		}

		public void RefreshRectangles()
		{
			WinAPI.GetWindowInfo(m_hWnd, ref m_windowInfo);
			RECT.CopyRect(m_windowInfo.rcWindow, ref m_relWindowRC);
			RECT.CopyRect(m_windowInfo.rcClient, ref m_relClientRC);

			if (m_parent != null)
			{
				WINDOWINFO winInfoParent = new WINDOWINFO();
				winInfoParent.cbSize = (uint)Marshal.SizeOf(typeof(WINDOWINFO));
				WinAPI.GetWindowInfo(m_parent.m_hWnd, ref winInfoParent);

				MakeRectRelative(winInfoParent.rcClient, ref m_relWindowRC);
				MakeRectRelative(winInfoParent.rcClient, ref m_relClientRC);
			}
		}

		public void ApplyChanges()
		{
			uint uFlags = WinAPI.SWP_NOSIZE | WinAPI.SWP_NOMOVE | WinAPI.SWP_NOZORDER | WinAPI.SWP_NOACTIVATE | WinAPI.SWP_NOOWNERZORDER | WinAPI.SWP_NOSENDCHANGING;

			if ((m_changes & Changes.Style) != 0)
			{
				WinAPI.SetWindowLong(m_hWnd, WinAPI.GWL_STYLE, m_windowInfo.dwStyle);
				uFlags |= WinAPI.SWP_FRAMECHANGED;
			}
			if ((m_changes & Changes.ExStyle) != 0)
			{
				WinAPI.SetWindowLong(m_hWnd, WinAPI.GWL_EXSTYLE, m_windowInfo.dwExStyle);
				uFlags |= WinAPI.SWP_FRAMECHANGED;
			}
			if ((m_changes & Changes.Pos) != 0)
				uFlags ^= WinAPI.SWP_NOMOVE;
			if ((m_changes & Changes.Size) != 0)
				uFlags ^= WinAPI.SWP_NOSIZE;

			if ((uFlags & (WinAPI.SWP_NOMOVE + WinAPI.SWP_NOSIZE)) != (WinAPI.SWP_NOMOVE + WinAPI.SWP_NOSIZE) && (WinAPI.IsIconic(m_hWnd) || WinAPI.IsZoomed(m_hWnd)))
				WinAPI.ShowWindow(m_hWnd, WinAPI.SW_SHOWNOACTIVATE);

			WinAPI.SetWindowPos(m_hWnd, 0, m_relWindowRC.left, m_relWindowRC.top, m_relWindowRC.Width, m_relWindowRC.Height, uFlags);
            WinAPI.SendMessage(m_hWnd, WinAPI.WM_EXITSIZEMOVE, 0, 0);
			m_changes = Changes.None;
		}

		public void RemoveBorders()
		{
			uint nStyle = (uint)WinAPI.GetWindowLong(m_hWnd, WinAPI.GWL_STYLE);
			nStyle = (nStyle | (WinAPI.WS_THICKFRAME + WinAPI.WS_DLGFRAME + WinAPI.WS_BORDER)) ^ (WinAPI.WS_THICKFRAME + WinAPI.WS_DLGFRAME + WinAPI.WS_BORDER);
			WinAPI.SetWindowLong(m_hWnd, WinAPI.GWL_STYLE, nStyle);

			nStyle = (uint)WinAPI.GetWindowLong(m_hWnd, WinAPI.GWL_EXSTYLE);
			nStyle = (nStyle | (WinAPI.WS_EX_DLGMODALFRAME + WinAPI.WS_EX_WINDOWEDGE + WinAPI.WS_EX_CLIENTEDGE + WinAPI.WS_EX_STATICEDGE)) ^ (WinAPI.WS_EX_DLGMODALFRAME + WinAPI.WS_EX_WINDOWEDGE + WinAPI.WS_EX_CLIENTEDGE + WinAPI.WS_EX_STATICEDGE);
			WinAPI.SetWindowLong(m_hWnd, WinAPI.GWL_EXSTYLE, nStyle);

			uint uFlags = WinAPI.SWP_NOSIZE | WinAPI.SWP_NOMOVE | WinAPI.SWP_NOZORDER | WinAPI.SWP_NOACTIVATE | WinAPI.SWP_NOOWNERZORDER | WinAPI.SWP_NOSENDCHANGING | WinAPI.SWP_FRAMECHANGED;
			WinAPI.SetWindowPos(m_hWnd, 0, 0, 0, 0, 0, uFlags);
		}

		public int PosX
		{
			get { return m_relWindowRC.left; }
			set
			{
				if (m_relWindowRC.left != value)
				{
					int nWidth = m_relWindowRC.Width;
					m_relWindowRC.left = value;
					m_relWindowRC.right = value + nWidth;
					m_changes |= Changes.Pos;
				}
			}
		}

		public int PosY
		{
			get { return m_relWindowRC.top; }
			set
			{
				if (m_relWindowRC.top != value)
				{
					int nHeight = m_relWindowRC.Height;
					m_relWindowRC.top = value;
					m_relWindowRC.bottom = value + nHeight;
					m_changes |= Changes.Pos;
				}
			}
		}

		public int Width
		{
			get { return m_relWindowRC.Width; }
			set
			{
				if (m_relWindowRC.Width != value)
				{
					m_relWindowRC.right = m_relWindowRC.left + value;
					m_changes |= Changes.Size;
				}
			}
		}

		public int Height
		{
			get { return m_relWindowRC.Height; }
			set
			{
				if (m_relWindowRC.Height != value)
				{
					m_relWindowRC.bottom = m_relWindowRC.top + value;
					m_changes |= Changes.Size;
				}
			}
		}

		public uint Style
		{
			get { return m_windowInfo.dwStyle; }
			set
			{
				if (m_windowInfo.dwStyle != value)
				{
					m_windowInfo.dwStyle = value;
					m_changes |= Changes.Style;
				}
			}
		}

		public uint ExStyle
		{
			get { return m_windowInfo.dwExStyle; }
			set
			{
				if (m_windowInfo.dwExStyle != value)
				{
					m_windowInfo.dwExStyle = value;
					m_changes |= Changes.ExStyle;
				}
			}
		}

		public RECT ClientRect
		{
			get { return m_relClientRC; }
		}

		public uint BorderWidth
		{
			get { return m_windowInfo.cxWindowBorders; }
		}

		public uint BorderHeight
		{
			get { return m_windowInfo.cyWindowBorders; }
		}

		public string DisplayName
		{
			get
			{
				if (string.IsNullOrEmpty(m_text))
					return m_hierID + ". (" + m_hWnd.ToString("X8") + ')';

				string winName;

				if (m_text.Length > 64)
					winName = m_text.Substring(0, 61) + "...";
				else
					winName = m_text;

				return m_hierID + ". " + winName;
			}
		}

		public int Handle
		{
			get { return m_hWnd; }
		}

		public bool IsWindow
		{
			get { return WinAPI.IsWindow(m_hWnd); }
		}

		public Window Parent
		{
			get { return m_parent; }
		}

		public string Class
		{
			get { return m_class; }
		}

		public string Text
		{
			get { return m_text; }
		}

		public string HierarchicalID
		{
			get { return m_hierID; }
		}

		public int ThreadID
		{
			get { return m_nThreadID; }
		}

		private static void MakeRectRelative(RECT rcParent, ref RECT rcDest)
		{
			rcDest.left = rcDest.left - rcParent.left;
			rcDest.top = rcDest.top - rcParent.top;
			rcDest.right = rcDest.right - rcParent.left;
			rcDest.bottom = rcDest.bottom - rcParent.top;
		}
	}

	/// <summary>
	/// WinAPI class.
	/// </summary>
	static class WinAPI
	{
		public const int GWL_STYLE = -16;
		public const int GWL_EXSTYLE = -20;

		public const uint SWP_NOSIZE = 0x01;
		public const uint SWP_NOMOVE = 0x02;
		public const uint SWP_NOZORDER = 0x04;
		public const uint SWP_NOACTIVATE = 0x10;
		public const uint SWP_NOOWNERZORDER = 0x200;
		public const uint SWP_NOSENDCHANGING = 0x400;
		public const uint SWP_FRAMECHANGED = 0x20;

		public const uint WS_THICKFRAME = 0x40000;
		public const uint WS_DLGFRAME = 0x400000;
		public const uint WS_BORDER = 0x800000;

		public const uint WS_EX_DLGMODALFRAME = 1;
		public const uint WS_EX_WINDOWEDGE = 0x100;
		public const uint WS_EX_CLIENTEDGE = 0200;
		public const uint WS_EX_STATICEDGE = 0x20000;

		public const int SW_SHOWNOACTIVATE = 4;
		public const int SW_RESTORE = 9;

        public const int WM_EXITSIZEMOVE = 0x0232;

		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
		public delegate bool EnumWindowsProc(int hwnd, IntPtr lParam);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool EnumChildWindows(int hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern int GetWindowText(int hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern uint RealGetWindowClass(int hwnd, StringBuilder pszType, int cchType);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool IsWindow(int hWnd);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool GetWindowInfo(int hwnd, ref WINDOWINFO pwi);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern int GetWindowLong(int hWnd, int nIndex);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern int SetWindowLong(int hWnd, int nIndex, uint dwNewLong);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool SetWindowPos(int hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool IsIconic(int hWnd);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool IsZoomed(int hWnd);

		[DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
		public static extern bool ShowWindow(int hWnd, int nCmdShow);

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern int GetForegroundWindow();

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern ushort GetAsyncKeyState(System.Windows.Forms.Keys vKey);

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern int SendMessage(int hWnd, int msg, int wParam, int lParam);
	}

	/// <summary>
	/// RECT struct.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;

		public int Width { get { return right - left; } }
		public int Height { get { return bottom - top; } }

		public static void CopyRect(RECT rcSrc, ref RECT rcDest)
		{
			rcDest.left = rcSrc.left;
			rcDest.top = rcSrc.top;
			rcDest.right = rcSrc.right;
			rcDest.bottom = rcSrc.bottom;
		}
	}

	/// <summary>
	/// WINDOWINFO struct.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	struct WINDOWINFO
	{
		public uint cbSize;
		public RECT rcWindow;
		public RECT rcClient;
		public uint dwStyle;
		public uint dwExStyle;
		public uint dwWindowStatus;
		public uint cxWindowBorders;
		public uint cyWindowBorders;
		public ushort atomWindowType;
		public ushort wCreatorVersion;
	}
}
