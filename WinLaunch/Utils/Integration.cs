using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;

namespace WinLaunch
{
    public enum MiddleMouseButtonAction
    {
        Nothing = 0,
        Clicked = 1,
        DoubleClicked = 2
    }

    internal static class FullscreenAppDetector
    {
        #region Interop

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr
        FindWindow([MarshalAs(UnmanagedType.LPTStr)] string lpClassName,
        [MarshalAs(UnmanagedType.LPTStr)] string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetShellWindow();

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowRect(IntPtr hwnd, out RECT rc);

        #endregion Interop

        private static IntPtr desktopHandle; //Window handle for the desktop
        private static IntPtr shellHandle; //Window handle for the shell

        public static bool Detected(System.Windows.Window window)
        {
            desktopHandle = GetDesktopWindow();
            shellHandle = GetShellWindow();

            RECT appBounds;
            Rectangle screenBounds;
            IntPtr hWnd;

            //get the active window
            hWnd = GetForegroundWindow();

            //check if we have a valid handle and din't pick ourself
            if (hWnd != null && hWnd != (new WindowInteropHelper(window).Handle))
            {
                //check if the window is a WorkerW (e.g. ShowDesktop layer window)
                StringBuilder classname = new StringBuilder(256);
                GetClassName(hWnd, classname, 256);
                string ActiveWindowClass = classname.ToString();

                //Check we haven't picked up the desktop or shell
                if (hWnd != desktopHandle && hWnd != shellHandle && ActiveWindowClass.ToLower() != "workerw")
                {
                    GetWindowRect(hWnd, out appBounds);
                    //determine if window is fullscreen
                    screenBounds = Screen.FromHandle(hWnd).Bounds;
                    if ((appBounds.Bottom - appBounds.Top) == screenBounds.Height && (appBounds.Right - appBounds.Left) == screenBounds.Width)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}