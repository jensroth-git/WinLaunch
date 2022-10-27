using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        private void AddDefaultApps()
        {
            //clear all items 
            //foreach (var item in SBM.IC.Items)
            //{
            //    SBM.container.Remove(item.ContentRef);
            //}

            //SBM.IC.Items.Clear();

            string startMenuItems = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs";
            string additionalStartMenuItems = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Start Menu\\Programs");

            List<string> files = new List<string>();

            if(Directory.Exists(startMenuItems))
                files.AddRange(Directory.GetFiles(startMenuItems));

            if (Directory.Exists(additionalStartMenuItems))
                files.AddRange(Directory.GetFiles(additionalStartMenuItems));


            List<string> filesToAdd = new List<string>();
            List<string> dirsToAdd = new List<string>();

            foreach (var file in files)
            {
                if (file.EndsWith(".lnk"))
                {
                    if (SBM.GetItemsByName(Path.GetFileNameWithoutExtension(file)).Count == 0)
                        filesToAdd.Add(file);
                }
            }

            List<string> subDirectories = new List<string>();

            if (Directory.Exists(startMenuItems))
                subDirectories.AddRange(Directory.GetDirectories(startMenuItems));

            if (Directory.Exists(additionalStartMenuItems))
                subDirectories.AddRange(Directory.GetDirectories(additionalStartMenuItems));

            foreach (var directory in subDirectories)
            {
                var directoryFiles = Directory.GetFiles(directory);

                //check if there are at least 2 links in there 
                int numLnk = 0;
                foreach (var file in directoryFiles)
                {
                    if (file.EndsWith(".lnk"))
                    {
                        if (SBM.GetItemsByName(Path.GetFileNameWithoutExtension(file)).Count == 0)
                            numLnk++;
                    }
                }

                if (numLnk >= 2)
                {
                    dirsToAdd.Add(directory);
                }
                else
                {
                    //not enough items for a folder, add single item instead
                    foreach (var file in directoryFiles)
                    {
                        if (file.EndsWith(".lnk"))
                        {
                            if (SBM.GetItemsByName(Path.GetFileNameWithoutExtension(file)).Count == 0)
                                filesToAdd.Add(file);
                        }
                    }
                }
            }

            //exclude entries that exist already 

            //sort files and directories 
            filesToAdd.Sort((a, b) =>
                Path.GetFileNameWithoutExtension(a).CompareTo(Path.GetFileNameWithoutExtension(b))
            );

            dirsToAdd.Sort();

            foreach (var file in filesToAdd)
            {
                AddFile(file);
            }

            foreach (var directory in dirsToAdd)
            {
                var directoryFiles = Directory.GetFiles(directory);

                //create a folder and add all items
                SBItem Folder = new SBItem(Path.GetFileName(directory), "Folder", null, "", SBItem.FolderIcon);
                Folder.IsFolder = true;

                int GridIndex = 0;
                foreach (var file in directoryFiles)
                {
                    if (file.EndsWith(".lnk"))
                    {
                        var item = PrepareFile(file);

                        item.Page = 0;
                        item.GridIndex = GridIndex;

                        Folder.IC.Items.Add(item);

                        GridIndex++;
                    }
                }

                SBM.AddItem(Folder);
                Folder.UpdateFolderIcon(true);
            }
        }

        private void LoadSettings()
        {
            //Check for updates
            UpdateCheck.RunThreaded();

            //show the welcome dialog if version is new
            if (Assembly.GetExecutingAssembly().GetName().Version > Settings.CurrentSettings.version)
            {
                JustUpdated = true;

                if (Settings.CurrentSettings.version == new Version("0.0.0.0"))
                {
                    FirstLaunch = true;
                }

                Settings.CurrentSettings.version = Assembly.GetExecutingAssembly().GetName().Version;
                Settings.SaveSettings(Settings.CurrentSettingsPath, Settings.CurrentSettings);

                Welcome welcome = new Welcome();
                welcome.ShowDialog();
            }
        }

        private void InitSettings()
        {
            //Init Controls

            #region Controls

            //when tablet mode is enabled instant move is off
            SBM.SetInstantMoveMode(!Settings.CurrentSettings.TabletMode);

            #endregion Controls

            //Init Activators
            try
            {
                InitHotCornerActivator();
                InitShortcutActivator();
                InitHotKey();
                InitMiddleMouseButtonActivator();
                InitWindowsKeyActivation();

                UpdateGridSettings();

                UpdateMiddleMouseButtonActivator();

                //update bindings
                settings = Settings.CurrentSettings;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("settings"));
            }
            catch
            {
                MessageBox.Show("Could not initialize some activation methods", "Winlaunch Error");
            }
        }

        private void UpdateGridSettings()
        {
            SBM.GM.XItems = Settings.CurrentSettings.Columns;
            SBM.GM.YItems = Settings.CurrentSettings.Rows;

            SBM.FolderGrid.XItems = Settings.CurrentSettings.FolderColumns;
            SBM.FolderGrid.YItems = Settings.CurrentSettings.FolderRows;
        }

        private void ApplySettings()
        {
            //when tablet mode is enabled instant move is off
            SBM.SetInstantMoveMode(!Settings.CurrentSettings.TabletMode);

            //update settings
            UpdateHotKeySettings();
            UpdateHotCornerSettings();
            UpdateMiddleMouseButtonActivator();
            UpdateWindowsKeyActivation();

            UpdateGridSettings();

            //update bindings
            settings = Settings.CurrentSettings;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("settings"));
        }

        private void UpdateMiddleMouseButtonActivator()
        {
            if (Settings.CurrentSettings.MiddleMouseActivation != MiddleMouseButtonAction.Nothing)
            {
                middleMouseActivator.Active = true;
            }
            else
            {
                //block activation events
                middleMouseActivator.Active = false;
            }
        }

        private void UpdateHotCornerSettings()
        {
            HotCorner.Corners corners = HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotTopLeft ? HotCorner.Corners.TopLeft : HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotTopRight ? HotCorner.Corners.TopRight : HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotBottomRight ? HotCorner.Corners.BottomRight : HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotBottomLeft ? HotCorner.Corners.BottomLeft : HotCorner.Corners.None;

            hotCorner.SetCorners(corners);
            hotCorner.SetDelay(Settings.CurrentSettings.HotCornerDelay);

            if (corners == HotCorner.Corners.None)
                Settings.CurrentSettings.HotCornersEnabled = false;

            if (!Settings.CurrentSettings.HotCornersEnabled)
                hotCorner.Active = false;
            else
                hotCorner.Active = true;
        }

        private void UpdateHotKeySettings()
        {
            if (!Settings.CurrentSettings.HotKeyEnabled)
                return;

            try
            {
                //clear old key
                if (hotkey != null)
                {
                    hotkey.UpdateHwndSource((HwndSource)HwndSource.FromVisual(this));
                    hotkey.Dispose();
                }

                //make new key
                hotkey = new HotKey((HwndSource)HwndSource.FromVisual(this));

                hotkey.Modifiers |= (Settings.CurrentSettings.HotAlt ? HotKey.ModifierKeys.Alt : 0);
                hotkey.Modifiers |= (Settings.CurrentSettings.HotControl ? HotKey.ModifierKeys.Control : 0);
                hotkey.Modifiers |= (Settings.CurrentSettings.HotShift ? HotKey.ModifierKeys.Shift : 0);
                hotkey.Modifiers |= (Settings.CurrentSettings.HotWin ? HotKey.ModifierKeys.Win : 0);

                hotkey.Key = Settings.CurrentSettings.HotKeyExtend;

                hotkey.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyDown);

                hotkey.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not enable hotkey" + ex.Message, "Winlaunch Error");
            }
        }

        private void UpdateWindowsKeyActivation()
        {
            if (Settings.CurrentSettings.WindowsKeyActivationEnabled)
            {
                wka.StartListening();
            }
            else
            {
                wka.StopListening();
            }
        }

        private void OpenSettingsDialog()
        {
            //disable hotkey
            ActivatorsEnabled = false;

            //stop move mode
            SBM.StopMoveMode();
            SBM.LockItems = true;
            SBM.HoldItem = null;

            InstantSettings settings = new InstantSettings(this);
            settings.Owner = this;
            settings.ShowDialog();

            //apply the settings
            ApplySettings();

            SBM.LockItems = false;

            CleanMemory();

            ActivatorsEnabled = true;
        }
    }
}