using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace WinLaunch
{
    internal class WindowsKeyActivation
    {
        #region Interop
        int WH_GETMESSAGE = 3;

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetWindowsHookEx(int hookType, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(String sClassName, String sAppName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, IntPtr lpszWindow);

        [DllImport("User32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetModuleHandle([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        #endregion

        IntPtr module = IntPtr.Zero;
        IntPtr hHookModule = IntPtr.Zero;
        IntPtr progmanHook = IntPtr.Zero;

        public void StartListening()
        {
            if (progmanHook != IntPtr.Zero)
                return;

            if(module == IntPtr.Zero)
                module = LoadLibrary("HookWindowsKey.dll");
    
            if(hHookModule == IntPtr.Zero)
                hHookModule = GetModuleHandle("HookWindowsKey.dll");

            IntPtr fn = GetProcAddress(hHookModule, "HookProgManThread");

            IntPtr progmanWindow = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Progman", IntPtr.Zero);
            uint progmanThread = GetWindowThreadProcessId(progmanWindow, IntPtr.Zero);

            //inject dll into progman
            progmanHook = SetWindowsHookEx(WH_GETMESSAGE, fn, hHookModule, progmanThread);
        }

        public void StopListening()
        {
            if (progmanHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(progmanHook);
                progmanHook = IntPtr.Zero;
            }
        }
    }
}
