using System;
using System.IO;
using System.Timers;
using System.Windows;

using RegistryUtils;
using Microsoft.Win32;
using System.Drawing;

namespace WinLaunch
{
    internal class WallpaperUtils
    {
        private FileSystemWatcher FSW;
        private Timer dptime;

        //registry watchers
        private RegistryMonitor backgroundColorWatcher;
        private RegistryMonitor accentSettingsWatcher;
        private RegistryMonitor accentColorWatcher;

        //events
        public event EventHandler WallpaperChanged;
        public event EventHandler BackgroundColorChanged;
        public event EventHandler AccentColorChanged;

        public WallpaperUtils()
        {
            try
            {
                // init timer
                dptime = new Timer(400);
                dptime.Elapsed += new ElapsedEventHandler(dptime_Elapsed);
                dptime.Enabled = false;

                #region Wallpaper watcher
                // Filesystemwatcher anlegen
                FSW = new FileSystemWatcher();

                // Pfad und Filter festlegen
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string wallpaperpath = appdata + "\\Microsoft\\Windows\\Themes\\";

                FSW.Path = wallpaperpath;

                // Events definieren
                FSW.Changed += new FileSystemEventHandler(FSW_Changed);

                // Filesystemwatcher aktivieren
                FSW.EnableRaisingEvents = true;
                #endregion

                #region Color Watchers
                accentSettingsWatcher = new RegistryMonitor(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                accentSettingsWatcher.RegChanged += AccentColorWatcher_RegChanged;
                accentSettingsWatcher.Start();

                accentColorWatcher = new RegistryMonitor(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent");
                accentColorWatcher.RegChanged += AccentColorWatcher_RegChanged;
                accentColorWatcher.Start();

                backgroundColorWatcher = new RegistryMonitor(RegistryHive.CurrentUser, @"Control Panel\Colors");
                backgroundColorWatcher.RegChanged += BackgroundColorWatcher_RegChanged; ;
                backgroundColorWatcher.Start();
                #endregion
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);
            }
        }

        public static bool IsUsingWallpaper()
        {
            var DesktopKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
            string wallpaperPath = (string)DesktopKey.GetValue("Wallpaper");

            return wallpaperPath != "";
        }

        public static bool IsUsingAccentColor()
        {
            try
            {
                //older than 7 
                if (Environment.OSVersion.Version.Major < 6)
                    return false;

                //older than windows 8
                if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor < 2)
                    return false;

                var AccentKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                int isUsingPrevalence = (int)AccentKey.GetValue("ColorPrevalence");

                return isUsingPrevalence != 0;
            }
            catch { }

            //key doesnt exist
            return false;
        }

        public static Color GetBackgroundColor()
        {
            var ColorsKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors");
            string BGColorComponents = (string)ColorsKey.GetValue("Background");

            string[] components = BGColorComponents.Split(new char[] { ' ' });

            Color BGColor = Color.FromArgb(255, int.Parse(components[0]), int.Parse(components[1]), int.Parse(components[2]));

            return BGColor;
        }

        public static Color GetAccentColor()
        {
            var AccentKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Accent");
            int color = (int)AccentKey.GetValue("StartColorMenu");

            Color StartColorMenu = Color.FromArgb(color);

            return StartColorMenu;
        }

        private void AccentColorWatcher_RegChanged(object sender, EventArgs e)
        {
            AccentColorChanged(this, EventArgs.Empty);
        }

        private void BackgroundColorWatcher_RegChanged(object sender, EventArgs e)
        {
            BackgroundColorChanged(this, EventArgs.Empty);
        }

        private void dptime_Elapsed(object sender, ElapsedEventArgs e)
        {
            dptime.Enabled = false;
            WallpaperChanged(this, EventArgs.Empty);
        }

        private void FSW_Changed(object sender, FileSystemEventArgs e)
        {
            dptime.Enabled = true;
        }
    }
}