using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace WinLaunch
{
    internal static class GlassUtils
    {
        #region DWM Blur Behind
        #region Windows 7
        [StructLayout(LayoutKind.Sequential)]
        public struct BlurBehind
        {
            public BlurBehindFlags dwFlags;
            public bool fEnable;
            public IntPtr hRgnBlur;
            public bool fTransitionOnMaximized;
        }

        public enum BlurBehindFlags : int
        {
            Enable = 0x00000001,
            BlurRegion = 0x00000002,
            TransitionOnMaximized = 0x00000004
        }

        [DllImport("dwmapi.dll")]
        public static extern IntPtr DwmEnableBlurBehindWindow(IntPtr hWnd, ref BlurBehind pBlurBehind);

        [DllImport("dwmapi.dll")]
        public static extern IntPtr DwmIsCompositionEnabled(out bool pfEnabled);
        #endregion

        #region Windows 10
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19
            // ...
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        #endregion

        public static bool IsBlurBehindAvailable()
        {
            //only support windows 10 & 7
            if(Environment.OSVersion.Version.Major >= 10)
            {
                //windows 10
                return true;
            }

            if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1)
            {
                //windows 7
                bool compositionEnabled = false;

                return DwmIsCompositionEnabled(out compositionEnabled) == IntPtr.Zero && compositionEnabled;
            }

            //windows 8 & 8.1
            return false;
        }

        public static bool EnableBlurBehind(Window hWnd, bool acrylic = false)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                //disable first to start with a clean slate
                DisableBlurBehind(hWnd);

                //windows 10
                var windowHelper = new WindowInteropHelper(hWnd);

                var accent = new AccentPolicy();
                var accentStructSize = Marshal.SizeOf(accent);

                if(acrylic)
                {
                    accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
                    accent.GradientColor = (0x01 << 24) | (0xFFFFFF & 0xFFFFFF);
                }
                else
                {
                    accent.AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND;
                    //accent.GradientColor = (0x01 << 24) | (0xFFFFFF & 0xFFFFFF);
                }

                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                int ret = SetWindowCompositionAttribute(windowHelper.Handle, ref data);

                Marshal.FreeHGlobal(accentPtr);
                return true;
            }

            //windows 7
            bool compositionEnabled = false;
            if (DwmIsCompositionEnabled(out compositionEnabled) == IntPtr.Zero && compositionEnabled)
            {
                IntPtr Handle = new WindowInteropHelper(hWnd).Handle;

                if (Handle != IntPtr.Zero)
                {
                    HwndSource mainWindowSrc = (HwndSource)HwndSource.FromHwnd(Handle);
                    mainWindowSrc.CompositionTarget.BackgroundColor = Colors.Transparent;

                    BlurBehind bb = new BlurBehind();
                    bb.dwFlags = BlurBehindFlags.Enable;
                    bb.fEnable = true;
                    bb.hRgnBlur = (IntPtr)0;

                    if (DwmEnableBlurBehindWindow(Handle, ref bb) == IntPtr.Zero)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool DisableBlurBehind(Window hWnd)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                //windows 10
                var windowHelper = new WindowInteropHelper(hWnd);

                var accent = new AccentPolicy();
                var accentStructSize = Marshal.SizeOf(accent);

                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                SetWindowCompositionAttribute(windowHelper.Handle, ref data);

                Marshal.FreeHGlobal(accentPtr);
                return true;
            }

            //win 7
            IntPtr Handle = new WindowInteropHelper(hWnd).Handle;

            if (Handle != IntPtr.Zero)
            {
                HwndSource mainWindowSrc = (HwndSource)HwndSource.FromHwnd(Handle);

                BlurBehind bb = new BlurBehind();
                bb.dwFlags = BlurBehindFlags.Enable;
                bb.fEnable = false;
                bb.hRgnBlur = (IntPtr)0;

                if (DwmEnableBlurBehindWindow(Handle, ref bb) == IntPtr.Zero)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion DWM Blur Behind
    }
}