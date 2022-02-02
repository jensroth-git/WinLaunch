using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace WinLaunch
{
    internal class ShortcutActivation
    {
        private const int WM_TOGGLELAUNCHPAD = 0x8000 + 0x0808;

        [DllImport("User32.DLL")]
        public static extern int SendMessage(IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(String sClassName, String sAppName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, IntPtr lpszClass, string lpszWindow);

        [DllImport("User32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static bool FindAndActivate()
        {
            IntPtr hWnd = IntPtr.Zero;
            uint ProcessID = 0;

            while (true)
            {
                hWnd = FindWindowEx(IntPtr.Zero, hWnd, IntPtr.Zero, "WinLaunch");

                if (hWnd == IntPtr.Zero)
                    break;

                GetWindowThreadProcessId(hWnd, out ProcessID);

                Process p = Process.GetProcessById((int)ProcessID);

                if (p.ProcessName == "WinLaunch")
                    break;
            }

            if (hWnd == IntPtr.Zero)
                return false;

            SendMessage(hWnd, WM_TOGGLELAUNCHPAD, 0, 0);

            return true;
        }

        private HwndSourceHook hook;
        private HwndSource hwndSource;

        public event EventHandler Activated;

        public void InitListener(HwndSource hwndSource)
        {
            if (hwndSource == null)
                throw new ArgumentNullException("hwndSource");

            this.hook = new HwndSourceHook(WndProc);
            this.hwndSource = hwndSource;
            hwndSource.AddHook(hook);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TOGGLELAUNCHPAD)
            {
                Activated(this, EventArgs.Empty);
                handled = true;
            }

            return new IntPtr(0);
        }
    }
}