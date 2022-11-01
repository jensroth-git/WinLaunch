using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace WinLaunch
{
    internal class WindowsKeyActivation
    {
        private GlobalKeyboardHook hook = null;

        public event EventHandler Activated;

        public void StartListening()
        {
            if (hook != null)
                return;

            hook = new GlobalKeyboardHook();
            hook.KeyboardPressed += Hook_KeyboardPressed; ;
        }

        private void Hook_KeyboardPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            if(e.KeyboardData.Flags == 1 && e.KeyboardData.VirtualCode == GlobalKeyboardHook.VkLwin ||
                e.KeyboardData.VirtualCode == GlobalKeyboardHook.VkRwin)
            {
                Activated(this, EventArgs.Empty);

                //suppress key
                e.Handled = true;
            }
        }

        public void StopListening()
        {
            if (hook == null)
                return;

            hook.Dispose();
            hook = null;
        }
    }
}
