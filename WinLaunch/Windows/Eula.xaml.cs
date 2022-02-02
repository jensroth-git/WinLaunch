using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinLaunch
{
    /// <summary>
    /// Interaktionslogik für Eula.xaml
    /// </summary>
    public partial class Eula : Window
    {
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

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(new HwndSourceHook(this.hwndSourceHook));
            }
        }

        private IntPtr hwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_SHOWWINDOW)
            {
                IntPtr hMenu = GetSystemMenu(hwnd, false);
                if (hMenu != IntPtr.Zero)
                {
                    EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
                }
            }

            return IntPtr.Zero;
        }

        #endregion Disable close button

        public static int EULAversion = 1;

        public static bool IsEULAshown()
        {
            try
            {
                int shownVersion = (int)Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\WinLaunch", "EULA", 0);

                if(shownVersion == EULAversion)
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        public static void SaveEULAshown()
        {
            try
            {
                Registry.SetValue(@"HKEY_CURRENT_USER\SOFTWARE\WinLaunch", "EULA", EULAversion);
            }
            catch { }
        }

        public static void ShowEULA()
        {
            if (!Eula.IsEULAshown())
            {
                Eula license = new Eula();
                if (license.ShowDialog() == true)
                {
                    //User agreed to the EULA
                    Eula.SaveEULAshown();
                }
                else
                {
                    //User declined the EULA
                    Environment.Exit(0);
                }
            }
        }

        public Eula(bool showButtons = true)
        {
            InitializeComponent();

            if (!showButtons)
            {
                btnConfirm.Content = "OK";
                btnDecline.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfirmClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}