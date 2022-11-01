using System;
using System.CodeDom;
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
    internal class DoubleKeytapActivation
    {
        private GlobalKeyboardHook hook = null;
        private DoubleClickEvent doubleClickCtrl = new DoubleClickEvent();
        private DoubleClickEvent doubleClickAlt = new DoubleClickEvent();

        public event EventHandler Activated;

        public bool CtrlActivated { get; set; }
        public bool AltActivated { get; set; }

        public DoubleKeytapActivation()
        {
            CtrlActivated = false;
            AltActivated = false;
        }

        public void StartListening()
        {
            if (hook != null)
                return;

            hook = new GlobalKeyboardHook();
            hook.KeyboardPressed += Hook_KeyboardPressed; ;
        }

        private void Hook_KeyboardPressed(object sender, GlobalKeyboardHookEventArgs e)
        {
            if(CtrlActivated && e.KeyboardData.Flags == 0x00 && e.KeyboardData.VirtualCode == GlobalKeyboardHook.VkControl)
            {
                if(doubleClickCtrl.Click())
                {
                    Activated(this, EventArgs.Empty);

                    //suppress key
                    e.Handled = true;
                }                
            }
            else if (AltActivated && e.KeyboardData.Flags == 0x20 && e.KeyboardData.VirtualCode == GlobalKeyboardHook.VkAlt)
            {
                if (doubleClickAlt.Click())
                {
                    Activated(this, EventArgs.Empty);

                    //suppress key
                    e.Handled = true;
                }
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
