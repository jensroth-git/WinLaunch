using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace WinLaunch
{
    internal class HotCornerArgs : EventArgs
    {
        public HotCorner.Corners Corner { get; private set; }

        public HotCornerArgs(HotCorner.Corners Corner)
        {
            this.Corner = Corner;
        }
    }

    //class HotCorner
    //{
    //    #region Interop
    //    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    //    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    [return: MarshalAs(UnmanagedType.Bool)]
    //    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    //    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    //    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    //    private static extern IntPtr GetModuleHandle(string lpModuleName);

    //    [StructLayout(LayoutKind.Sequential)]
    //    private struct POINT
    //    {
    //        public int X;
    //        public int Y;
    //    }

    //    [StructLayout(LayoutKind.Sequential)]
    //    private struct MSLLHOOKSTRUCT
    //    {
    //        public POINT pt;
    //        public uint mouseData;
    //        public uint flags;
    //        public uint time;
    //        public IntPtr dwExtraInfo;
    //    }

    //    private enum MouseMessages
    //    {
    //        WM_LEFTBUTTONDOWN = 0x0201,
    //        WM_LEFTBUTTONUP = 0x0202,
    //        WM_MOUSEMOVE = 0x0200,
    //        WM_MOUSEWHEEL = 0x020A,
    //        WM_RIGHTBUTTONDOWN = 0x0204,
    //        WM_RIGHTBUTTONUP = 0x0205
    //    }

    //    private const int WH_MOUSE_LL = 14;

    //    #endregion

    //    #region private members

    //    Corners ActiveCorners = Corners.TopLeft;
    //    private static IntPtr _hookID = IntPtr.Zero;
    //    private LowLevelMouseProc _proc;
    //    #endregion

    //    #region Public members
    //    public enum Corners : int
    //    {
    //        None = 0,
    //        TopLeft = 1,
    //        TopRight = 2,
    //        BottomRight = 4,
    //        BottomLeft = 8
    //    };

    //    public event EventHandler<HotCornerArgs> Activated;

    //    private bool active = false;
    //    public bool Active
    //    {
    //        get { return active; }
    //        set
    //        {
    //            if (value != active)
    //            {
    //                if (value)
    //                    Begin();
    //                else
    //                    Stop();

    //                active = value;
    //            }
    //        }
    //    }

    //    private void Begin()
    //    {
    //        _proc = HookCallback;
    //        _hookID = SetHook(_proc);

    //        if (_hookID == IntPtr.Zero)
    //        {
    //            MessageBox.Show("failed to initialize HotCorners!\nerror code: " + System.Runtime.InteropServices.Marshal.GetLastWin32Error());
    //        }
    //    }

    //    private void Stop()
    //    {
    //        UnhookWindowsHookEx(_hookID);
    //    }

    //    public int Size = 10;
    //    #endregion

    //    #region public methods
    //    public HotCorner()
    //    {
    //    }

    //    public void SetCorners(Corners corner)
    //    {
    //        ActiveCorners = corner;
    //    }
    //    #endregion

    //    #region private methods
    //    private void CallActivated(Corners corner)
    //    {
    //        if (!Active)
    //            return;

    //        if (Activated != null)
    //            Activated(this, new HotCornerArgs(corner));
    //    }

    //    private Corners CheckCorners(int X, int Y)
    //    {
    //        if((int)(ActiveCorners & Corners.TopLeft) != 0){
    //            if (X < Size && Y < Size)
    //                return Corners.TopLeft;
    //        }

    //        if((int)(ActiveCorners & Corners.TopRight) != 0){
    //            if (X > (int)SystemParameters.VirtualScreenWidth - Size && Y < Size)
    //                return Corners.TopRight;
    //        }

    //         if((int)(ActiveCorners & Corners.BottomRight) != 0){
    //             if (X > (int)SystemParameters.VirtualScreenWidth - Size && Y > (int)SystemParameters.VirtualScreenHeight - Size)
    //                return Corners.BottomRight;
    //         }

    //         if ((int)(ActiveCorners & Corners.BottomLeft) != 0)
    //         {
    //             if (X < Size && Y > (int)SystemParameters.VirtualScreenHeight - Size)
    //                 return Corners.BottomLeft;
    //         }

    //         return Corners.None;
    //    }

    //    private static IntPtr SetHook(LowLevelMouseProc proc)
    //    {
    //        using (Process curProcess = Process.GetCurrentProcess())
    //        using (ProcessModule curModule = curProcess.MainModule)
    //        {
    //            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
    //        }
    //    }

    //    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    //    private bool IsInCorner = false;
    //    private bool WasInCorner = false;

    //    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    //    {
    //        if (nCode >= 0)
    //        {
    //            MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

    //            //check corners
    //            Corners corner = CheckCorners(hookStruct.pt.X, hookStruct.pt.Y);
    //            IsInCorner = !(corner == Corners.None);

    //            if (IsInCorner)
    //            {
    //                if (!WasInCorner)
    //                {
    //                    //Debug.WriteLine("Activated at " + hookStruct.pt.X + ", " + hookStruct.pt.Y);
    //                    WasInCorner = true;
    //                    CallActivated(corner);
    //                }
    //            }
    //            else
    //            {
    //                WasInCorner = false;
    //            }
    //        }
    //        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    //    }

    //    private
    //    #endregion
    //}

    internal class HotCorner
    {
        #region Interop

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(ref System.Drawing.Point lpPoint);

        #endregion Interop

        #region private members

        private Corners ActiveCorners = Corners.TopLeft;
        private double delay = 0.0;

        #endregion private members

        #region Public members

        public enum Corners : int
        {
            None = 0,
            TopLeft = 1,
            TopRight = 2,
            BottomRight = 4,
            BottomLeft = 8
        };

        public event EventHandler<HotCornerArgs> Activated;

        private bool active = false;

        public bool Active
        {
            get { return active; }
            set
            {
                if (value != active)
                {
                    if (value)
                        Begin();
                    else
                        Stop();

                    active = value;
                }
            }
        }

        private void Begin()
        {
            if (CheckPositionThread != null)
                CheckPositionThread.Abort();

            CheckPositionThread = new Thread(new ThreadStart(CheckPositionLoop));
            CheckPositionThread.Start();
        }

        private void Stop()
        {
            if (CheckPositionThread != null)
                CheckPositionThread.Abort();
        }

        public int Size = 10;

        #endregion Public members

        #region public methods

        public HotCorner()
        {
        }

        ~HotCorner()
        {
            if (CheckPositionThread != null)
                CheckPositionThread.Abort();
        }

        public void SetCorners(Corners corner)
        {
            ActiveCorners = corner;
        }

        public void SetDelay(double s)
        {
            delay = s;
        }

        #endregion public methods

        #region private methods

        private void CallActivated(Corners corner)
        {
            if (!Active)
                return;

            if (Activated != null)
            {
                Activated(this, new HotCornerArgs(corner));
            }
        }

        private Corners CheckCorners(int X, int Y)
        {
            int ScreenLeft = (int)((SystemParameters.VirtualScreenLeft + Size) * MiscUtils.GetDPIScale());
            int ScreenRight = (int)((SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Size) * MiscUtils.GetDPIScale());
            int ScreenTop = (int)((SystemParameters.VirtualScreenTop + Size) * MiscUtils.GetDPIScale());
            int ScreenBottom = (int)((SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Size) * MiscUtils.GetDPIScale());

            if ((int)(ActiveCorners & Corners.TopLeft) != 0)
            {
                if (X < ScreenLeft && Y < ScreenTop)
                    return Corners.TopLeft;
            }

            if ((int)(ActiveCorners & Corners.TopRight) != 0)
            {
                if (X > ScreenRight && Y < ScreenTop)
                    return Corners.TopRight;
            }

            if ((int)(ActiveCorners & Corners.BottomRight) != 0)
            {
                if (X > ScreenRight && Y > ScreenBottom)
                    return Corners.BottomRight;
            }

            if ((int)(ActiveCorners & Corners.BottomLeft) != 0)
            {
                if (X < ScreenLeft && Y > ScreenBottom)
                    return Corners.BottomLeft;
            }

            return Corners.None;
        }

        public static Stopwatch Time = new Stopwatch();
        private bool IsInCorner = false;
        private bool WasInCorner = false;

        private Thread CheckPositionThread = null;

        private void CheckPositionLoop()
        {
            System.Drawing.Point MousePos = new System.Drawing.Point();
            while (true)
            {
                GetCursorPos(ref MousePos);

                //check corners
                Corners corner = CheckCorners((int)MousePos.X, (int)MousePos.Y);
                IsInCorner = !(corner == Corners.None);

                if (delay == 0.0)
                {
                    if (IsInCorner)
                    {

                        if (!WasInCorner)
                        {
                            WasInCorner = true;
                            CallActivated(corner);
                        }
                    }
                    else
                    {
                        WasInCorner = false;
                    }
                }
                else
                {
                    if(IsInCorner)
                    {
                        if (!WasInCorner)
                        {
                            WasInCorner = true;
                            Time.Restart();
                        }
                        else
                        {
                            if(Time.ElapsedMilliseconds > delay*1000)
                            {
                                Time.Reset();
                                CallActivated(corner);
                            }
                        }
                    }
                    else
                    {
                        WasInCorner = false;
                    }
                }

                Thread.Sleep(80);
            }
        }

        #endregion private methods
    }
}