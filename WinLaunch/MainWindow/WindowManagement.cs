using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Interop;

namespace WinLaunch
{
    public class ScreenRect
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;

        public ScreenRect(System.Drawing.Rectangle r)
        {
            X = r.X;
            Y = r.Y;
            Width = r.Width;
            Height = r.Height;
        }
    }

    partial class MainWindow : Window
    {
        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        #region Disable close button

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint MF_ENABLED = 0x00000000;

        private const uint SC_CLOSE = 0xF060;

        private const int WM_SHOWWINDOW = 0x00000018;

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(new HwndSourceHook(this.hwndSourceHook));
            }
        }

        bool ShowDesktopActivated = false;

        private IntPtr hwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWWINDOW)
            {
                IntPtr hMenu = GetSystemMenu(hwnd, false);
                if (hMenu != IntPtr.Zero)
                {
                    EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
                }

                //set tool window
                int exStyle = (int)GetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE);

                exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
                SetWindowLong(hwnd, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
            }

            if (Settings.CurrentSettings.DeskMode && !Settings.CurrentSettings.LegacyDeskMode)
            {
                if (msg == WM_WINDOWPOSCHANGING && !ShowDesktopActivated)
                {
                    var ver = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));

                    if (DesktopWindow == IntPtr.Zero)
                        ver.hwndInsertAfter = HWND_BOTTOM;
                    else
                        ver.hwndInsertAfter = DesktopWindow;

                    ver.flags &= ~SWP_NOZORDER;
                    handled = true;

                    Marshal.StructureToPtr(ver, lParam, false);
                }
            }

            return IntPtr.Zero;
        }

        #endregion Disable close button

        private bool IsFullscreen = true;
        private bool IsHidden = false;

        private double GetOptimalResolutionSettings(double Width, double Height)
        {
            //768 - 0.79
            //1080 - 1.0
            if (Height >= 1080 - 40)
                return 1.0;

            if (Height < 768 - 40)
                return 0.7;

            double resolution_value = (Height - (768 - 40)) / ((1080 - 40) - (768 - 40));
            double scale = 0.79 + (1.0 - 0.79) * resolution_value;

            return scale;
        }

        private double CurrentScale = 1.0;

        private void ChangeResolution(double Left, double Top, double Width, double Height, double scale)
        {
            this.Width = Width;
            this.Height = Height;
            this.Left = Left;
            this.Top = Top;

            this.CurrentScale = scale;

            //set rendersize
            this.MainCanvas.Width = this.Width * (1.0 / scale);
            this.MainCanvas.Height = this.Height * (1.0 / scale);

            //set presentersize
            this.CanvasScale.ScaleX = scale;
            this.CanvasScale.ScaleY = scale;
        }

        private Rect GetDeskModeRectLegacy()
        {
            double DPIscale = MiscUtils.GetDPIScale();

            //desktop coordinate system always starts at 0, 0
            //convert the (random) screen coordinates to desktop coordinates

            //normalize desktop bounds
            List<ScreenRect> Bounds = new List<ScreenRect>();

            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;

            foreach (var screen in screens)
            {
                Bounds.Add(new ScreenRect(screen.WorkingArea));
            }

            //normalize the screen
            double minX = 0;
            double minY = 0;

            foreach (var screen in Bounds)
            {
                if (screen.X < minX)
                {
                    minX = screen.X;
                }

                if (screen.Y < minY)
                {
                    minY = screen.Y;
                }
            }

            //transform screens to desktop space
            for (int i = 0; i < Bounds.Count; i++)
            {
                Bounds[i].X -= minX;
                Bounds[i].Y -= minY;
            }

            return new Rect(Bounds[Settings.CurrentSettings.ScreenIndex].X,
                            Bounds[Settings.CurrentSettings.ScreenIndex].Y,
                            Bounds[Settings.CurrentSettings.ScreenIndex].Width,
                            Bounds[Settings.CurrentSettings.ScreenIndex].Height);
        }

        private Rect GetDeskModeRect()
        {
            double DPIscale = MiscUtils.GetDPIScale();

            List<ScreenRect> Bounds = new List<ScreenRect>();

            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;

            foreach (var screen in screens)
            {
                Bounds.Add(new ScreenRect(screen.WorkingArea));
            }

            if (Settings.CurrentSettings.ScreenIndex >= Bounds.Count)
            {
                Settings.CurrentSettings.ScreenIndex = 0;
            }

            return new Rect(Bounds[Settings.CurrentSettings.ScreenIndex].X / DPIscale,
                        Bounds[Settings.CurrentSettings.ScreenIndex].Y / DPIscale,
                        Bounds[Settings.CurrentSettings.ScreenIndex].Width / DPIscale,
                        Bounds[Settings.CurrentSettings.ScreenIndex].Height / DPIscale);
        }

        private Rect GetFullscreenRect()
        {
            double Left = 0;
            double Top = 0;
            double Width = 0;
            double Height = 0;

            #region DPI aware

            double DPIscale = MiscUtils.GetDPIScale();

            #endregion DPI aware

            #region Multi-Screen

            int ScreenIndex;

            if (Settings.CurrentSettings.OpenOnActiveDesktop && !Settings.CurrentSettings.DeskMode)
            {
                ScreenIndex = MiscUtils.GetActiveScreenIndex();
            }
            else
            {
                //fixed screen or desk mode
                ScreenIndex = Settings.CurrentSettings.ScreenIndex;
            }

            if (ScreenIndex > System.Windows.Forms.Screen.AllScreens.GetUpperBound(0))
                ScreenIndex = 0;

            System.Windows.Forms.Screen Screen = System.Windows.Forms.Screen.AllScreens[ScreenIndex];

            if (Settings.CurrentSettings.FillScreen && !Settings.CurrentSettings.DeskMode)
            {
                Left = (double)Screen.Bounds.Left;
                Top = (double)Screen.Bounds.Top;
                Width = (double)Screen.Bounds.Width;
                Height = (double)Screen.Bounds.Height;
            }
            else
            {
                Left = (double)Screen.WorkingArea.Left;
                Top = (double)Screen.WorkingArea.Top;
                Width = (double)Screen.WorkingArea.Width;
                Height = (double)Screen.WorkingArea.Height;
            }

            #endregion Multi-Screen

            return new Rect(Left / DPIscale, Top / DPIscale, Width / DPIscale, Height / DPIscale);
        }

        #region DeskMode
        const UInt32 SWP_NOSIZE = 0x0001;
        const UInt32 SWP_NOMOVE = 0x0002;
        const UInt32 SWP_NOZORDER = 0x0004;
        const UInt32 SWP_NOREDRAW = 0x0008;
        const UInt32 SWP_NOACTIVATE = 0x0010;
        const UInt32 SWP_FRAMECHANGED = 0x0020;
        const UInt32 SWP_SHOWWINDOW = 0x0040;
        const UInt32 SWP_HIDEWINDOW = 0x0080;
        const UInt32 SWP_NOCOPYBITS = 0x0100;
        const UInt32 SWP_NOOWNERZORDER = 0x0200;
        const UInt32 SWP_NOSENDCHANGING = 0x0400;
        const UInt32 SWP_DRAWFRAME = SWP_FRAMECHANGED;
        const UInt32 SWP_NOREPOSITION = SWP_NOOWNERZORDER;

        const UInt32 SWP_DEFERERASE = 0x2000;
        const UInt32 SWP_ASYNCWINDOWPOS = 0x4000;

        const int WM_ACTIVATEAPP = 0x001C;
        const int WM_ACTIVATE = 0x0006;
        const int WM_SETFOCUS = 0x0007;
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const int WM_WINDOWPOSCHANGING = 0x0046;

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X,
           int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpWindowClass, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("User32", CharSet = CharSet.Auto, ExactSpelling = true)]
        internal static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndParent);

        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(System.Drawing.Point p);

        private bool IsDesktopChild = false;

        IntPtr DesktopWindow;
        IntPtr OriginalParentWindow;

        Thread KeepWindowVisibleThread;

        System.Drawing.Point topLeftCorner;

        // Delegate to filter which windows to include 
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private void StartKeepWindowVisibleThread()
        {
            if (KeepWindowVisibleThread != null)
                KeepWindowVisibleThread.Abort();

            KeepWindowVisibleThread = new Thread(new ThreadStart(new Action(() =>
            {
                while (IsDesktopChild)
                {
                    IntPtr window = WindowFromPoint(topLeftCorner);

                    if (window == DesktopWindow)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            //show desktop activated
                            if (IsDesktopChild)
                            {
                                ShowDesktopActivated = true;
                                this.Topmost = true;
                                this.Topmost = false;

                                //IntPtr hWnd = new WindowInteropHelper(this).Handle;
                                //SetWindowPos(hWnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOSENDCHANGING);

                                this.Activate();
                                this.Focus();
                                ShowDesktopActivated = false;
                            }
                        }));
                    }

                    //60 fps check
                    Thread.Sleep(1000 / 60);
                }
            })));

            KeepWindowVisibleThread.Start();
        }

        public void MakeDesktopChildWindow()
        {
            if (IsDesktopChild)
                return;

            this.ShowInTaskbar = false;
            this.Topmost = false;
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            Rect FullScreenRect = GetDeskModeRect();

            ChangeResolution(FullScreenRect.X, FullScreenRect.Y,
                FullScreenRect.Width,
                FullScreenRect.Height,
                1.0
                );

            topLeftCorner = new System.Drawing.Point((int)FullScreenRect.X, (int)FullScreenRect.Y);

            //force window in the background
            DesktopWindow = IntPtr.Zero;
            IntPtr hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE);

            EnumWindows(delegate (IntPtr wnd, IntPtr param)
            {
                IntPtr ShellDll = FindWindowEx(wnd, IntPtr.Zero, "SHELLDLL_DefView", "");
                DesktopWindow = FindWindowEx(ShellDll, IntPtr.Zero, "SysListView32", "FolderView");

                if (DesktopWindow != IntPtr.Zero)
                {
                    //found the desktop, stop iterating
                    return false;
                }

                // return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            if (DesktopWindow == IntPtr.Zero)
            {
                MessageBox.Show("failed to locate the desktop!", "Error");
                return;
            }

            if (Settings.CurrentSettings.LegacyDeskMode)
            {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                OriginalParentWindow = SetParent(windowHandle, DesktopWindow);
            }
            else
            {
                if (KeepWindowVisibleThread != null)
                    KeepWindowVisibleThread.Abort();

                IsDesktopChild = true;

                StartKeepWindowVisibleThread();
            }
        }

        public void UnsetDesktopChild()
        {
            if (!IsDesktopChild)
            {
                return;
            }

            if (Settings.CurrentSettings.LegacyDeskMode)
            {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;
                SetParent(windowHandle, OriginalParentWindow);

                IsDesktopChild = false;
            }
            else
            {
                KeepWindowVisibleThread.Abort();

                IsDesktopChild = false;
            }
        }

        #endregion DeskMode

        /// <summary>
        /// updates the window position based on the current settings (deskmode, screen placement, etc.)
        /// </summary>
        public void UpdateWindowPosition()
        {
            if (Settings.CurrentSettings.DeskMode)
            {
                if (!IsDesktopChild)
                {
                    MakeDesktopChildWindow();
                }
                else
                {
                    if (Settings.CurrentSettings.LegacyDeskMode)
                    {
                        //switch screens
                        Rect FullScreenRect = GetDeskModeRectLegacy();

                        //strange bug -> call twice
                        ChangeResolution(FullScreenRect.X, FullScreenRect.Y,
                            FullScreenRect.Width,
                            FullScreenRect.Height,
                            1.0
                            );

                        ChangeResolution(FullScreenRect.X, FullScreenRect.Y,
                            FullScreenRect.Width,
                            FullScreenRect.Height,
                            1.0
                            );

                    }
                    else
                    {
                        //switch screens
                        Rect FullScreenRect = GetDeskModeRect();

                        //stop the keep window thread until we are repositioned
                        if (KeepWindowVisibleThread != null)
                            KeepWindowVisibleThread.Abort();

                        topLeftCorner = new System.Drawing.Point((int)FullScreenRect.X, (int)FullScreenRect.Y);

                        ChangeResolution(FullScreenRect.X, FullScreenRect.Y,
                            FullScreenRect.Width,
                            FullScreenRect.Height,
                            1.0
                            );

                        StartKeepWindowVisibleThread();
                    }
                }
            }
            else
            {
                MakeFullscreen();
            }
        }

        public void MakeFullscreen()
        {
            this.ShowInTaskbar = false;

            if (IsDesktopChild)
                UnsetDesktopChild();

            this.WindowStyle = System.Windows.WindowStyle.None;

            Rect FullScreenRect = GetFullscreenRect();

            ChangeResolution(FullScreenRect.X, FullScreenRect.Y,
                FullScreenRect.Width,
                FullScreenRect.Height,
                1.0
                );

            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            IsFullscreen = true;

            if (!Settings.CurrentSettings.DeskMode)
            {
                if (!this.Topmost)
                    this.Topmost = true;
            }
        }

        private void MakeDesktopWindow()
        {
            this.ShowInTaskbar = true;

            if (IsDesktopChild)
                UnsetDesktopChild();

            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.WindowStyle = System.Windows.WindowStyle.ToolWindow;

            Rect FullScreenRect = GetFullscreenRect();

            Rect CenterRect = MiscUtils.RectCenterInScreen(FullScreenRect.Width * 0.5, FullScreenRect.Height * 0.5 + 30, Settings.CurrentSettings.ScreenIndex);

            ChangeResolution(CenterRect.X, CenterRect.Y,
                CenterRect.Width,
                CenterRect.Height,
                0.5);

            IsFullscreen = false;
        }

        private async void RevealWindow()
        {
            this.Visibility = System.Windows.Visibility.Visible;
            MakeFullscreen();

            IsHidden = false;

            //manage selection
            Keyboard.Focus(tbSearch);
            SBM.UnselectItem();

            StartFlyInAnimation();
        }

        private void HideWindow()
        {
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.Hide();
            IsHidden = true;
        }

        public void ToggleLaunchpad()
        {
            if (SearchActive)
            {
                DeactivateSearch();
            }

            if (MainContextMenu.IsLoaded)
                return;

            if (extensionBarVisible)
            {
                ToggleToolbar();
                return;
            }

            SBM.UnselectItem();

            CleanMemory();

            //hide if shown and show if hidden
            if (this.Visibility == System.Windows.Visibility.Hidden)
            {
                //dont show if another app is running in fullscreen
                if (Settings.CurrentSettings.BlockOnFullscreen && FullscreenAppDetector.Detected(this))
                    return;

                RevealWindow();
            }
            else
            {
                //dont close while moving or fading out
                if (FadingOut || SBM.Moving || SBM.SP.Scrolling || SBM.JiggleModeAttempt || SBM.MoveItemAttempt)
                    return;

                //if jiggle mode is on -> stop it
                if (SBM.JiggleMode)
                {
                    SBM.StopMoveMode();
                    return;
                }

                //if folder is open close it
                if (SBM.FolderOpen)
                {
                    SBM.BeginCloseFolder();
                    return;
                }

                if (Settings.CurrentSettings.DeskMode)
                    return;

                //start fade out animation
                //Animation -> HideWinLaunch -> HideWindow
                StartFlyOutAnimation();
            }
        }

        private void ForceFadeOut()
        {
            //start fade out animation
            //Animation -> HideWinLaunch -> HideWindow
            StartFlyOutAnimation();
        }

        //should be called after the window is invisible
        public void HideWinLaunch()
        {
            //TODO: better tutorial
            //show tutorial when winlaunch closes for the first time
            if (FirstLaunch)
            {
                FirstLaunch = false;
                MiscUtils.OpenURL("http://WinLaunch.org/howto.php");
            }

            HideToolbar();

            HideWindow();

            SBM.StopMoveMode();

            if (SBM.FolderOpen)
            {
                SBM.CloseFolderInstant();
                CloseFolderGrid();
            }

            if (StartingItem)
            {
                LaunchedItem.ScaleAnim.Value = 1.0;
                LaunchedItem = null;
            }

            FadingOut = false;
            StartingItem = false;

            DeactivateSearch();

            //save items
            PerformItemBackup();
        }
    }
}