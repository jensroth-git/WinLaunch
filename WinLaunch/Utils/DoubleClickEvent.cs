using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WinLaunch
{
    public class DoubleClickEvent
    {
        [DllImport("user32.dll")]
        static extern uint GetDoubleClickTime();

        uint doubleClickMS;
        private Stopwatch stopwatch;

        public event EventHandler DoubleClicked = null;

        public DoubleClickEvent()
        {
            doubleClickMS = GetDoubleClickTime();
            stopwatch = new Stopwatch();
        }

        public bool Click()
        {
            if (stopwatch.IsRunning && stopwatch.ElapsedMilliseconds <= doubleClickMS)
            {
                //activated
                if (DoubleClicked != null)
                    DoubleClicked(this, null);

                stopwatch.Reset();

                return true;
            }
            else
            {
                stopwatch.Restart();
            }

            return false;
        }
    }
}
