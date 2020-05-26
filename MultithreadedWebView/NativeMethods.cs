using System;
using System.Runtime.InteropServices;

namespace MultithreadedWebView
{
    class NativeMethods
    {
        public static bool SetWindowAsChild(IntPtr hWndParent, IntPtr hWndChild)
        {
            uint dwSyleToRemove = WS_POPUP | WS_CAPTION | WS_THICKFRAME;
            uint dwExStyleToRemove = WS_EX_DLGMODALFRAME | WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE;

            uint dwStyle = GetWindowLong(hWndChild, GWL_STYLE);
            uint dwExStyle = GetWindowLong(hWndChild, GWL_EXSTYLE);

            dwStyle &= ~dwSyleToRemove;
            dwExStyle &= ~dwExStyleToRemove;

            SetWindowLong(hWndChild, GWL_STYLE, dwStyle | WS_CHILD);
            SetWindowLong(hWndChild, GWL_EXSTYLE, dwExStyle);

            IntPtr hWndOld = SetParent(hWndChild, hWndParent);

            if (hWndOld == IntPtr.Zero) {
                System.Diagnostics.Debug.WriteLine("SetParent() Failed -> Last Error: " + Marshal.GetLastWin32Error() + "\n");
            }

            return hWndOld != IntPtr.Zero;
        }

        #region Methods / Consts for Embedding a Window
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern long SetWindowPos(IntPtr hWnd, long hWndInsertAfter, long x, long y, long cx, long cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

        public static int GWL_STYLE = -16;
        public static int GWL_EXSTYLE = -20;

        public static uint WS_CHILD = 0x40000000;
        public static uint WS_POPUP = 0x80000000;
        public static uint WS_CAPTION = 0x00C00000;
        public static uint WS_THICKFRAME = 0x00040000;

        public static uint WS_EX_DLGMODALFRAME = 0x00000001;
        public static uint WS_EX_WINDOWEDGE = 0x00000100;
        public static uint WS_EX_CLIENTEDGE = 0x00000200;
        public static uint WS_EX_STATICEDGE = 0x00020000;

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040
        }

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        #endregion
    }
}
