using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WinLaunch
{
    class MiddleMouseButtonActivatedEventArgs : EventArgs
    {
        public bool handled = false;
    }

    internal class MiddleMouseActivation
    {
        #region Interop
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private enum MouseMessages : uint
        {
            WM_LEFTBUTTONDOWN = 0x0201,
            WM_LEFTBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONDBLCLK = 0x0209,
            WM_RIGHTBUTTONDOWN = 0x0204,
            WM_RIGHTBUTTONUP = 0x0205
        }

        private const int WH_MOUSE_LL = 14;

        private const int MK_LBUTTON = 0x0001;
        private const int MK_RBUTTON = 0x0002;
        private const int MK_SHIFT = 0x0004;
        private const int MK_CONTROL = 0x0008;
        private const int MK_MBUTTON = 0x0010;
        private const int MK_XBUTTON1 = 0x0020;
        private const int MK_XBUTTON2 = 0x0040;

        public static int GetWheelDeltaWParam(int wparam)
        {
            return HighWord(wparam);
        }

        public static MouseButtons GetMouseButtonWParam(int wparam)
        {
            int mask = LowWord(wparam);

            if ((mask & MK_LBUTTON) == MK_LBUTTON) return MouseButtons.Left;
            if ((mask & MK_RBUTTON) == MK_RBUTTON) return MouseButtons.Right;
            if ((mask & MK_MBUTTON) == MK_MBUTTON) return MouseButtons.Middle;
            if ((mask & MK_XBUTTON1) == MK_XBUTTON1) return MouseButtons.XButton1;
            if ((mask & MK_XBUTTON2) == MK_XBUTTON2) return MouseButtons.XButton2;

            return MouseButtons.None;
        }

        public static bool IsCtrlKeyPressedWParam(int wparam)
        {
            int mask = LowWord(wparam);
            return (mask & MK_CONTROL) == MK_CONTROL;
        }

        public static bool IsShiftKeyPressedWParam(int wparam)
        {
            int mask = LowWord(wparam);
            return (mask & MK_SHIFT) == MK_SHIFT;
        }

        public static int GetXLParam(int lparam)
        {
            return LowWord(lparam);
        }

        public static int GetYLParam(int lparam)
        {
            return HighWord(lparam);
        }

        public static int LowWord(int word)
        {
            return word & 0xFFFF;
        }

        public static int HighWord(int word)
        {
            return word >> 16;
        }
        #endregion Interop


        #region private members
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc;
        #endregion private members


        #region Public members
        public event EventHandler<MiddleMouseButtonActivatedEventArgs> Activated;
        public bool Active = false;

        public void Begin()
        {
            _proc = HookCallback;
            SetHook(_proc);
        }

        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                bool success = UnhookWindowsHookEx(_hookID);

                if (hookThread != null)
                {
                    hookThread.Abort();
                }
            }
        }

        public void RefreshHook()
        {
            Stop();
            Begin();
        }
        #endregion Public members


        #region private methods
        Thread hookThread;

        private void SetHook(LowLevelMouseProc proc)
        {
            hookThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    using (Process curProcess = Process.GetCurrentProcess())
                    using (ProcessModule curModule = curProcess.MainModule)
                    {
                        _hookID = SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);

                        if (_hookID == IntPtr.Zero)
                        {
                            MessageBox.Show("Error setting mouse hook: " + System.Runtime.InteropServices.Marshal.GetLastWin32Error(), "Winlaunch Error");
                        }
                        else
                        {
                            //start message pump
                            Application.Run();
                        }
                    }
                }
                catch (Exception ex)
                {
                    CrashReporter.Report(ex);
                }
            }));

            hookThread.Start();
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && Active)
            {
                MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                if ((uint)wParam == (uint)MouseMessages.WM_MBUTTONDOWN)
                {
                    if (Activated != null)
                    {
                        try
                        {
                            MiddleMouseButtonActivatedEventArgs args = new MiddleMouseButtonActivatedEventArgs();
                            Activated(this, args);

                            if (args.handled)
                                return (IntPtr)1;

                        }
                        catch (Exception ex)
                        {
                            CrashReporter.Report(ex);
                        }
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        #endregion private methods
    }
}