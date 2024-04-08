using System.Runtime.InteropServices;

namespace WinLaunch
{
    internal class WindowsKeyActivation
    {
        [DllImport("HookWindowsKey.dll")]
        static extern bool SetupHook();

        [DllImport("HookWindowsKey.dll")]
        static extern void RestartExplorer();

        [DllImport("HookWindowsKey.dll")]
        static extern void Unhook();

        bool isHooked = false;

        public void StartListening()
        {
            isHooked = SetupHook();
        }

        public void StopListening()
        {
            if(isHooked)
            {
                Unhook();
                isHooked = false;
            }
        }
    }
}

