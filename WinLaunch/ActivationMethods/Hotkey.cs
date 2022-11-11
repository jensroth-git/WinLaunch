using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace System.Windows.Input
{
    internal class HotKeyEventArgs : EventArgs
    {
        public HotKey Hotkey { get; private set; }

        public HotKeyEventArgs(HotKey hotkey)
        {
            Hotkey = hotkey;
        }
    }

    internal class HotKey
    {
        private const int WM_HOTKEY = 786;

        [DllImport("user32", CharSet = CharSet.Ansi,
                   SetLastError = true, ExactSpelling = true)]
        private static extern int RegisterHotKey(IntPtr hwnd,
                int id, int modifiers, int key);

        [DllImport("user32", CharSet = CharSet.Ansi,
                   SetLastError = true, ExactSpelling = true)]
        private static extern int UnregisterHotKey(IntPtr hwnd, int id);

        public enum ModifierKeys : int
        {
            Alt = 1,
            Control = 2,
            Shift = 4,
            Win = 8
        }

        public Key Key { get; set; }

        public ModifierKeys Modifiers { get; set; }

        public event EventHandler<HotKeyEventArgs> HotKeyPressed;

        private int id;

        private HwndSourceHook hook;
        private HwndSource hwndSource;

        private static readonly Random rand = new Random((int)DateTime.Now.Ticks);

        public HotKey(HwndSource hwndSource)
        {
            if (hwndSource == null)
                throw new ArgumentNullException("hwndSource");

            this.hook = new HwndSourceHook(WndProc);
            this.hwndSource = hwndSource;
            hwndSource.AddHook(hook);

            id = rand.Next();
        }

        public void UpdateHwndSource(HwndSource hwndSource)
        {
            this.hwndSource = hwndSource;
        }

        private bool enabled;

        public bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if (value != enabled)
                {
                    int Handle = (int)hwndSource.Handle;
                    if (Handle != 0)
                    {
                        int result = 0;
                        if (value)
                        {
                            result = RegisterHotKey(hwndSource.Handle, id, (int)Modifiers, KeyInterop.VirtualKeyFromKey(Key));
                        }
                        else
                        {
                            result = UnregisterHotKey(hwndSource.Handle, id);
                        }

                        //if function fails result is 0
                        if (result == 0)
                        {
                            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                            if (error != 0)
                                throw new System.ComponentModel.Win32Exception(error);
                        }
                    }
                    else
                    {
                        if (value)
                            throw new ArgumentNullException("Handle");
                    }

                    enabled = value;
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
                IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                if ((int)wParam == id)
                    if (HotKeyPressed != null)
                        HotKeyPressed(this, new HotKeyEventArgs(this));
            }

            return new IntPtr(0);
        }

        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                hwndSource.RemoveHook(hook);
            }

            Enabled = false;

            disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HotKey()
        {
            Dispose();
        }
    }
}