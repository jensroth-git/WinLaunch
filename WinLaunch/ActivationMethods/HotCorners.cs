using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

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

        private bool IsInRect(System.Drawing.Rectangle rect, int x, int y)
        {
            if (x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom)
                return true;

            return false;
        }

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
            foreach (var screen in Screen.AllScreens)
            {
                if ((int)(ActiveCorners & Corners.TopLeft) != 0)
                {
                    if (IsInRect(new System.Drawing.Rectangle(screen.Bounds.Left, screen.Bounds.Top, Size, Size), X, Y))
                        return Corners.TopLeft;
                }

                if ((int)(ActiveCorners & Corners.TopRight) != 0)
                {
                    if (IsInRect(new System.Drawing.Rectangle(screen.Bounds.Right - Size, screen.Bounds.Top, Size, Size), X, Y))
                        return Corners.TopRight;
                }

                if ((int)(ActiveCorners & Corners.BottomRight) != 0)
                {
                    if (IsInRect(new System.Drawing.Rectangle(screen.Bounds.Right - Size, screen.Bounds.Bottom - Size, Size, Size), X, Y))
                        return Corners.BottomRight;
                }

                if ((int)(ActiveCorners & Corners.BottomLeft) != 0)
                {
                    if (IsInRect(new System.Drawing.Rectangle(screen.Bounds.Left, screen.Bounds.Bottom - Size, Size, Size), X, Y))
                        return Corners.BottomLeft;
                }
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