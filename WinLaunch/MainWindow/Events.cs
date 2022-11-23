using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        #region Context menu
        private void miEdit_Click(object sender, RoutedEventArgs e)
        {
            SBItem Item = ((e.Source as MenuItem).DataContext as SBItem);

            RunEditExtension(Item);

            if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly)
            {
                SortItemsAlphabetically();
            }
        }

        private void miRemove_Click(object sender, RoutedEventArgs e)
        {
            SBItem Item = ((e.Source as MenuItem).DataContext as SBItem);

            SBM.RemoveItem(Item, true);

            PerformItemBackup();
        }

        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            SBItem Item = ((e.Source as MenuItem).DataContext as SBItem);

            if (Item.IsFolder)
            {
                SBM.OpenFolder(Item);
            }
            else
            {
                ItemActivated(Item, EventArgs.Empty);
            }
        }

        private void miOpenAsAdmin_Click(object sender, RoutedEventArgs e)
        {
            SBItem Item = ((e.Source as MenuItem).DataContext as SBItem);

            if (Item.IsFolder)
            {
                SBM.OpenFolder(Item);
            }
            else
            {
                ItemActivated(Item, EventArgs.Empty, true);
            }
        }

        private void miOpenLocation_Click(object sender, RoutedEventArgs e)
        {
            SBItem Item = ((e.Source as MenuItem).DataContext as SBItem);

            if (Item.IsFolder)
                return;

            if (!Settings.CurrentSettings.DeskMode)
            {
                StartLaunchAnimations(Item);
                LaunchedItem = Item;
            }

            string filepath = Item.ApplicationPath;

            if (filepath.EndsWith(".lnk"))
            {
                if (Path.GetExtension(filepath).ToLower() == ".lnk" && ItemCollection.IsLnkInCache(Item.ApplicationPath))
                {
                    filepath = Path.Combine(PortabilityManager.LinkCachePath, filepath);
                    filepath = Path.GetFullPath(filepath);
                }

                //get actual path if its a link
                filepath = MiscUtils.GetShortcutTargetFile(filepath);

                if(string.IsNullOrEmpty(filepath))
                {
                    filepath = Item.ApplicationPath;
                }
            }

            //open explorer and highlight the file 
            Process ExplorerProc = new Process();
            ExplorerProc.StartInfo.FileName = "explorer.exe";
            ExplorerProc.StartInfo.Arguments = "/select,\"" + filepath + "\"";
            ExplorerProc.Start();
        }
        #endregion

        #region Main Context Menu
        private void miAddDefaultApps_Clicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(TranslationSource.Instance["ReallyAddDefaultApps"], TranslationSource.Instance["AddDefaultApps"], MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                AddDefaultApps();
            }
        }

        private void miAddFile_Clicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.DereferenceLinks = false;

            if ((bool)ofd.ShowDialog())
            {
                //add files 
                foreach (string fileName in ofd.FileNames)
                {
                    AddFile(fileName);
                }

                if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly)
                {
                    SortItemsAlphabetically();
                }

                PerformItemBackup();
            }
        }

        private void miAddFolder_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AddFile(ofd.SelectedPath);

                if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly )
                {
                    SortItemsAlphabetically();
                }

                PerformItemBackup();
            }
        }

        private void miAddLink_Clicked(object sender, RoutedEventArgs e)
        {
            AddLink dialog = new AddLink();
            dialog.Owner = this;

            if ((bool)dialog.ShowDialog())
            {
                AddFile(dialog.URL);

                if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly)
                {
                    SortItemsAlphabetically();
                }

                PerformItemBackup();
            }
        }

        private void miSettingsClicked(object sender, RoutedEventArgs e)
        {
            //open settings
            OpenSettingsDialog();
        }

        private void miQuitClicked(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(TranslationSource.Instance["CloseWarning"], TranslationSource.Instance["Quit"], MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                PerformItemBackup();

                hotCorner.Active = false;

                MainWindow.WindowRef.Close();
                Environment.Exit(0);
            }
        }

        private void miTutorialClicked(object sender, RoutedEventArgs e)
        {
            //open url
            HideWinLaunch();
            MiscUtils.OpenURL("http://WinLaunch.org/howto.php");
        }
        #endregion

        #region MainCanvas events

        //patch events through
        private void MainCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (FolderRenamingActive)
            {
                DeactivateFolderRenaming();
                return;
            }

            if (FadingOut)
                return;

            if (Settings.CurrentSettings.DeskMode)
            {
                //steal focus on mouse down so the keyboard works
                this.Activate();
                this.Focus();
            }

            SBM.MouseDown(sender, e);
        }

        private void MainCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (FadingOut)
                return;

            SBM.MouseUp(sender, e);
        }

        private void MainCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            if (FadingOut)
                return;

            SBM.MouseMove(sender, e);
        }

        private void MainCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            e.Handled = true;

            SBM.MouseLeave(sender, e);
        }

        private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (SBM != null)
            {
                SBM.UpdateDisplayRect(new Rect(0.0, 0.0, e.NewSize.Width, e.NewSize.Height));
                SBM.GM.SetGridPositions();

                if (SBM.FolderOpen)
                {
                    SBM.PositionFolderAndItems(false);
                }
            }

            this.FolderTitleGrid.Margin = new Thickness((180.5 / 1920.0) * MainCanvas.Width, (24.0 / 1080) * MainCanvas.Height, 0, 0);

            e.Handled = true;
        }

        #endregion MainCanvas events

        #region ItemActivated

        private SBItem LaunchedItem = null;

        //handles events from sbm
        public void ItemActivated(object sender, EventArgs e, bool RunAsAdmin = false)
        {
            try
            {
                if (FadingOut)
                    return;

                SBItem Item = sender as SBItem;

                if (Item == null)
                {
                    return;
                }

                if (EditExtensionActive)
                {
                    RunEditExtension(Item);
                }
                else
                {
                    //launch it
                    if (!Item.IsFolder)
                    {
                        try
                        {
                            string path = Item.ApplicationPath;

                            if (Path.GetExtension(path).ToLower() == ".lnk" && ItemCollection.IsLnkInCache(Item.ApplicationPath))
                            {
                                path = Path.Combine(PortabilityManager.LinkCachePath, path);
                            }

                            if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path) || Uri.IsWellFormedUriString(path, UriKind.Absolute))
                            {
                                CleanMemory();

                                if (!Settings.CurrentSettings.DeskMode)
                                {
                                    StartLaunchAnimations(Item);
                                    LaunchedItem = Item;
                                }

                                new Thread(new ThreadStart(() =>
                                {
                                    try
                                    {
                                        ProcessStartInfo startInfo = new ProcessStartInfo();
                                        startInfo.UseShellExecute = true;
                                        startInfo.FileName = path;
                                        startInfo.Arguments = Item.Arguments;

                                        if (Item.RunAsAdmin || RunAsAdmin)
                                        {
                                            startInfo.Verb = "runas";
                                        }

                                        //startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Item.ApplicationPath);
                                        Process.Start(startInfo);
                                    }
                                    catch (Exception ex) { }
                                })).Start();
                            }
                        }
                        catch //(Exception ex)
                        {
                            //MessageBox.Show(ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);
            }
        }

        #endregion ItemActivated

        #region WindowEvents

        public static bool StartHidden = false;
        private bool LoadingAssets = true;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowRef = this;

            //Init DPI Scaling
            MiscUtils.GetDPIScale();

            //setup appdata directory
            string appData = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch");
            if (!System.IO.Directory.Exists(appData))
            {
                System.IO.Directory.CreateDirectory(appData);
            }

            if (!System.IO.File.Exists(PortabilityManager.ItemsPath))
            {
                //Set autostart
                Autostart.SetAutoStart("WinLaunch", Assembly.GetExecutingAssembly().Location, " -hide");
            }

            //init gamepad
            InitGamepadInput();
            StartGamepadInput();

            //Load files
            LoadSettings();

            //initialize languages
            InitLocalization();

            //init springboard manager
            InitSBM();

            //begin loading theme
            BeginLoadTheme(() =>
            {
                //bitmaps loaded
                //begin loading icons
                BeginInitIC(() =>
                {
                    try
                    {
                        //all items loaded
                        //apply theme
                        InitTheme();

                        //add default apps on first launch
                        if (FirstLaunch)
                        {
                            //new install
                            //add default apps
                            AddDefaultApps();
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashReporter.Report(ex);
                        MessageBox.Show(ex.Message);
                    }

                    ActivatorsEnabled = true;
                    LoadingAssets = false;
                });
            });

            InitSettings();

            #region hook up events

            //input events
            RegisterInputEvents();

            this.GotKeyboardFocus += MainWindow_GotKeyboardFocus;
            this.LostKeyboardFocus += new KeyboardFocusChangedEventHandler(MainWindow_LostKeyboardFocus);

            //canvas events
            this.MainCanvas.MouseDown += new MouseButtonEventHandler(MainCanvas_MouseDown);
            this.MainCanvas.MouseMove += new MouseEventHandler(MainCanvas_MouseMove);
            this.MainCanvas.MouseUp += new MouseButtonEventHandler(MainCanvas_MouseUp);
            this.MainCanvas.MouseLeave += new MouseEventHandler(MainCanvas_MouseLeave);
            this.MainCanvas.SizeChanged += new SizeChangedEventHandler(MainCanvas_SizeChanged);

            //window events
            this.DragEnter += new DragEventHandler(MainWindow_DragEnter);
            this.DragOver += new DragEventHandler(MainWindow_DragOver);
            this.Drop += new DragEventHandler(MainWindow_Drop);
            this.Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            this.FolderTitle.MouseDown += new MouseButtonEventHandler(FolderTitle_MouseDown);
            this.FolderTitleNew.MouseDown += new MouseButtonEventHandler(FolderTitle_MouseDown);

            //framework events
            CompositionTargetEx.FrameUpdating += RenderFrame;

            //misc events
            WPWatch.WallpaperChanged += new EventHandler(WPWatch_Changed);
            WPWatch.BackgroundColorChanged += WPWatch_BackgroundColorChanged;
            WPWatch.AccentColorChanged += WPWatch_AccentColorChanged;

            SBM.ItemsUpdated += SBM_ItemsUpdated;
            #endregion hook up events

            //show if not hidden and on first ever startup
            if (!System.IO.File.Exists(PortabilityManager.ItemsPath) || !StartHidden)
            {
                //show window
                if (!Settings.CurrentSettings.DeskMode)
                {
                    Task.Factory.StartNew(() =>
                    {
                        //fix positioning bug
                        Thread.Sleep(100);
                    }).ContinueWith(t =>
                    {
                        RevealWindow();
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
            else
            {
                if (!Settings.CurrentSettings.DeskMode)
                {
                    this.Visibility = System.Windows.Visibility.Hidden;
                    IsHidden = true;
                }
            }

            if (Settings.CurrentSettings.DeskMode)
            {
                Task.Factory.StartNew(() =>
                {
                    //fix positioning bug
                    Thread.Sleep(100);
                }).ContinueWith(t =>
                {
                    RevealWindow();
                    MakeDesktopChildWindow();
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }

            e.Handled = true;
        }

        private void SBM_ItemsUpdated(object sender, EventArgs e)
        {
            if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly)
            {
                SortItemsAlphabetically();
            }

            PerformItemBackup();
        }

        private static void InitLocalization()
        {
            try
            {
                TranslationSource.Instance.CurrentCulture = new CultureInfo(Settings.CurrentSettings.SelectedLanguage);
            }
            catch { }

            if (TranslationSource.Instance.CurrentCulture == null)
            {
                TranslationSource.Instance.CurrentCulture = new CultureInfo("en-US");
            }
        }

        private void MainWindow_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            Activate();
            Focus();

            if(FolderRenamingActive)
            {
                if(Theme.CurrentTheme.UseVectorFolder)
                {
                    Keyboard.Focus(FolderTitleEdit);
                }
                else
                {
                    Keyboard.Focus(FolderTitleEditNew);
                }
            }
            else
            {
                Keyboard.Focus(tbSearch);
            }
        }

        private void MainWindow_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            //StartFlyOutAnimation();
            CleanMemory();
            //PerformItemBackup();
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //avoid alt + f4
            e.Cancel = true;
            //ToggleLaunchpad();
        }

        #endregion WindowEvents

        #region Activators
        private bool ActivatorsEnabled = false;

        #region HotCornerActivator
        private HotCorner hotCorner = null;

        private void InitHotCornerActivator()
        {
            hotCorner = new HotCorner();

            HotCorner.Corners corners = HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotTopLeft ? HotCorner.Corners.TopLeft : HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotTopRight ? HotCorner.Corners.TopRight : HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotBottomRight ? HotCorner.Corners.BottomRight : HotCorner.Corners.None;
            corners |= Settings.CurrentSettings.HotBottomLeft ? HotCorner.Corners.BottomLeft : HotCorner.Corners.None;

            hotCorner.SetCorners(corners);

            hotCorner.Activated += new EventHandler<HotCornerArgs>(hotCorner_Activated);

            if (!Settings.CurrentSettings.HotCornersEnabled)
                hotCorner.Active = false;
            else
                hotCorner.Active = true;

            hotCorner.SetDelay(Settings.CurrentSettings.HotCornerDelay);
        }

        private void hotCorner_Activated(object sender, HotCornerArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (!ActivatorsEnabled)
                    return;

                DebugLog.WriteToLogFile("HotCorner activated");

                ToggleLaunchpad();
            }));
        }
        #endregion HotCornerActivator

        #region DoubleKeytapActivation
        DoubleKeytapActivation dka = new DoubleKeytapActivation();

        private void InitDoubleKeytapActivation()
        {
            dka.Activated += Dka_Activated;

            if (Settings.CurrentSettings.DoubleTapCtrlActivationEnabled ||
                Settings.CurrentSettings.DoubleTapAltActivationEnabled)
            {
                if (Settings.CurrentSettings.DoubleTapCtrlActivationEnabled)
                    dka.CtrlActivated = true;
                else
                    dka.CtrlActivated = false;

                if (Settings.CurrentSettings.DoubleTapAltActivationEnabled)
                    dka.AltActivated = true;
                else
                    dka.AltActivated = false;

                dka.StartListening();
            }
        }

        private void Dka_Activated(object sender, EventArgs e)
        {
            if (!ActivatorsEnabled)
                return;

            DebugLog.WriteToLogFile("DoubleKey activated");

            ToggleLaunchpad();
        }
        #endregion

        #region WindowsKeyActivation
        WindowsKeyActivation wka = new WindowsKeyActivation();

        private void InitWindowsKeyActivation()
        {
            if (Settings.CurrentSettings.WindowsKeyActivationEnabled && !Settings.CurrentSettings.DeskMode)
            {
                wka.StartListening();
            }
        }

        #endregion WindowsKeyActivation

        #region MiddleMouseButtonActivator
        MiddleMouseActivation middleMouseActivator = null;
        DoubleClickEvent middleMouseDoubleClick = null;

        void InitMiddleMouseButtonActivator()
        {
            middleMouseDoubleClick = new DoubleClickEvent();
            middleMouseDoubleClick.DoubleClicked += middleMouseDoubleClick_DoubleClicked;

            middleMouseActivator = new MiddleMouseActivation();
            middleMouseActivator.Activated += middleMouseActivator_Activated;

            //skip in debug due to cursor jitter
#if !DEBUG
            middleMouseActivator.Begin();
#endif
        }

        void middleMouseActivator_Activated(object sender, MiddleMouseButtonActivatedEventArgs e)
        {
            if (!ActivatorsEnabled)
                return;

            if (Settings.CurrentSettings.MiddleMouseActivation == MiddleMouseButtonAction.Nothing)
                return;

            if (Settings.CurrentSettings.MiddleMouseActivation == MiddleMouseButtonAction.DoubleClicked)
            {
                if (middleMouseDoubleClick.Click())
                {
                    e.handled = true;
                }

                return;
            }

            //this will block the middle mouse button input systemwide
            e.handled = true;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                DebugLog.WriteToLogFile("MiddleMouse activated");

                ToggleLaunchpad();
            }));
        }

        void middleMouseDoubleClick_DoubleClicked(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                DebugLog.WriteToLogFile("MiddleMouseDoubleClick activated");

                ToggleLaunchpad();
            }));
        }
        #endregion

        #region ShortcutActivator
        private ShortcutActivation shortcutActivator = null;

        private void InitShortcutActivator()
        {
            shortcutActivator = new ShortcutActivation();
            shortcutActivator.InitListener((HwndSource)HwndSource.FromVisual(this));

            shortcutActivator.Activated += new EventHandler(shortcutActivator_Activated);
        }

        private void shortcutActivator_Activated(object sender, EventArgs e)
        {
            if (!ActivatorsEnabled)
                return;

            DebugLog.WriteToLogFile("shortcut activated");

            ToggleLaunchpad();
        }
        #endregion ShortcutActivator

        #region Hotkey
        private HotKey hotkey = null;

        private void InitHotKey()
        {
            //init configurable hotkey
            hotkey = new HotKey((HwndSource)HwndSource.FromVisual(this));

            hotkey.Modifiers |= (Settings.CurrentSettings.HotAlt ? HotKey.ModifierKeys.Alt : 0);
            hotkey.Modifiers |= (Settings.CurrentSettings.HotControl ? HotKey.ModifierKeys.Control : 0);
            hotkey.Modifiers |= (Settings.CurrentSettings.HotShift ? HotKey.ModifierKeys.Shift : 0);
            hotkey.Modifiers |= (Settings.CurrentSettings.HotWin ? HotKey.ModifierKeys.Win : 0);

            hotkey.Key = Settings.CurrentSettings.HotKeyExtend;

            hotkey.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyDown);

            try
            {
                if (Settings.CurrentSettings.HotKeyEnabled)
                    hotkey.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("could not enable hotkey" + ex.Message, "Winlaunch Error");
            }
        }

        private void HotKeyDown(object sender, HotKeyEventArgs e)
        {
            if (!ActivatorsEnabled)
                return;

            DebugLog.WriteToLogFile("HotKey activated");

            ToggleLaunchpad();
        }

        #endregion Hotkey
        #endregion Activators

        #region Input

        private void UnregisterInputEvents()
        {
            //unregister input events
            this.MouseWheel -= MainWindow_MouseWheel;

            this.KeyDown -= MainWindow_KeyDown;
            this.KeyUp -= MainWindow_KeyUp;
        }

        private void RegisterInputEvents()
        {
            this.MouseWheel += new MouseWheelEventHandler(MainWindow_MouseWheel);

            this.PreviewKeyDown += new KeyEventHandler(MainWindow_KeyDown);
            this.KeyUp += new KeyEventHandler(MainWindow_KeyUp);
        }

        #region Input events
        private void tbSearch_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ActivateSearch();
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FolderRenamingActive)
                return;

            if (tbSearch.Text == "")
            {
                SBM.EndSearch();
                return;
            }

            //search and display results
            SBM.UpdateSearch(tbSearch.Text);
        }

        private void tbSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (SearchActive)
                {
                    DeactivateSearch();
                    e.Handled = true;
                    return;
                }
                else
                {
                    ToggleLaunchpad();
                    e.Handled = true;
                    return;
                }
            }

            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)
            {
                SBM.KeyDown(sender, e);
                e.Handled = true;
                return;
            }

            ActivateSearch();
        }

        private void imCancel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DeactivateSearch();
        }

        private void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //Check MouseWheel
            if (e.Delta != 0)
            {
                if (!(this.Visibility == System.Windows.Visibility.Visible) || !ActivatorsEnabled)
                    return;

                if (e.Delta == 120)
                {
                    SBM.SP.FlipPageLeft();
                }
                else if (e.Delta == -120)
                {
                    SBM.SP.FlipPageRight(SBM.JiggleMode);
                }
            }

            e.Handled = true;
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (SearchActive)
                return;

            if (FolderRenamingActive)
                return;

            e.Handled = false;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (SearchActive)
                return;

            if (FolderRenamingActive)
                return;

            if (e.Key == Key.Escape)
            {
                ToggleLaunchpad();

                return;
            }

            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.Enter)
            {
                SBM.KeyDown(sender, e);
                e.Handled = true;
            }
        }
        #endregion
        #endregion Input

        #region DragnDrop
        private void MainWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void MainWindow_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            e.Handled = true;

            string[] FileList = (string[])e.Data.GetData(DataFormats.FileDrop, true);
            foreach (string File in FileList)
            {
                AddFile(File);
            }

            if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly)
            {
                SortItemsAlphabetically();
            }

            PerformItemBackup();

            if (!IsDesktopChild)
                Keyboard.ClearFocus();
        }

        #endregion

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (SBM.LockItems)
                e.Handled = true;
        }

        #region Background Theme Events

        private void WPWatch_Changed(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (!Theme.CurrentTheme.UseAeroBlur && !Theme.CurrentTheme.UseCustomBackground)
                {
                    SetSyncedTheme();
                }
            }));
        }

        private void WPWatch_BackgroundColorChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UpdateBackgroundColors();
            }));
        }

        private void WPWatch_AccentColorChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                UpdateBackgroundColors();
            }));
        }
        #endregion
    }
}
