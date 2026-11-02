using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinLaunch
{
    // WinLaunch Win-key activation without HookWindowsKey.dll/Progman injection.
    // The low-level hook keeps system Win+key shortcuts intact.
    internal sealed class WindowsKeyActivation
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_CONTROL = 0x11;
        private const int VK_SHIFT = 0x10;
        private const int VK_MENU = 0x12;
        private const ushort VK_DUMMY = 0xFF; // Masking key; also used by Microsoft PowerToys
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint INPUT_KEYBOARD = 1;
        private const uint LLKHF_INJECTED = 0x10;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        // INPUT's union is sized by MOUSEINPUT, not KEYBDINPUT.  Without
        // this, sizeof(INPUT) is 20 on x86 instead of the required 28,
        // causing SendInput to fail with ERROR_INVALID_PARAMETER (87).
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public event EventHandler Activated;

        private IntPtr hookHandle = IntPtr.Zero;
        private LowLevelKeyboardProc hookCallback; // Keep the delegate rooted for the hook lifetime.
        private bool leftDown;
        private bool rightDown;
        private bool shortcutUsed;

        public void StartListening()
        {
            if (hookHandle != IntPtr.Zero)
                return;

            hookCallback = HookCallback;
            hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, hookCallback,
                GetModuleHandle(null), 0);

            if (hookHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                hookCallback = null;
                throw new Win32Exception(error, "Could not install Win-key keyboard hook");
            }
        }

        public void StopListening()
        {
            if (hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookHandle);
                hookHandle = IntPtr.Zero;
            }

            hookCallback = null;
            leftDown = rightDown = shortcutUsed = false;
        }

        private static bool IsPressed(int key)
        {
            return (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        private static INPUT MakeKey(ushort vk, uint flags)
        {
            INPUT input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.U.ki.wVk = vk;
            input.U.ki.dwFlags = flags;
            return input;
        }

        private static bool TryMaskAndReleaseWin(ushort releasedWinKey)
        {
            // By this point the REAL Win-down has already reached Windows.
            // Mask its normal Start-menu action BEFORE its release. Send an
            // artificial Win-up as the last event, because the physical Win-up
            // will only be suppressed if all 3 injections succeeded.
            INPUT[] inputs =
            {
                MakeKey(VK_DUMMY, 0),
                MakeKey(VK_DUMMY, KEYEVENTF_KEYUP),
                MakeKey(releasedWinKey, KEYEVENTF_KEYUP)
            };

            return SendInput((uint)inputs.Length, inputs,
                Marshal.SizeOf(typeof(INPUT))) == (uint)inputs.Length;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                bool down = message == WM_KEYDOWN || message == WM_SYSKEYDOWN;
                bool up = message == WM_KEYUP || message == WM_SYSKEYUP;

                if (down || up)
                {
                    KBDLLHOOKSTRUCT key =
                        (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                    // Ignore SendInput and other software-generated events.
                    if ((key.flags & LLKHF_INJECTED) == 0)
                    {
                        bool isLeft = key.vkCode == VK_LWIN;
                        bool isRight = key.vkCode == VK_RWIN;
                        bool isWin = isLeft || isRight;

                        if (isWin && down)
                        {
                            bool wasDown = isLeft ? leftDown : rightDown;
                            if (!wasDown)
                            {
                                if (!leftDown && !rightDown)
                                {
                                    shortcutUsed = IsPressed(VK_CONTROL) ||
                                        IsPressed(VK_SHIFT) || IsPressed(VK_MENU);
                                }
                                else
                                {
                                    shortcutUsed = true; // Both Win keys held.
                                }

                                if (isLeft) leftDown = true;
                                else rightDown = true;

                                // Do not inject anything on Win-down:
                                // the shell must see Win-down before our mask.
                            }
                        }
                        else if (!isWin && down && (leftDown || rightDown))
                        {
                            shortcutUsed = true;
                        }
                        else if (isWin && up)
                        {
                            bool wasDown = isLeft ? leftDown : rightDown;
                            if (isLeft) leftDown = false;
                            else rightDown = false;

                            if (!leftDown && !rightDown)
                            {
                                bool activate = wasDown && !shortcutUsed;
                                shortcutUsed = false;
                                if (activate)
                                {
                                    // Suppress the physical Win-up ONLY once we
                                    // know an injected Win-up is queued. This
                                    // avoids leaving the modifier stuck down.
                                    if (TryMaskAndReleaseWin((ushort)key.vkCode))
                                    {
                                        Activated?.Invoke(this, EventArgs.Empty);
                                        return new IntPtr(1);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Don't swallow Win: Windows must still see Win+E, Win+R, etc.
            return CallNextHookEx(hookHandle, nCode, wParam, lParam);
        }
    }
}
