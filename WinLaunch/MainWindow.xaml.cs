using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        bool FirstLaunch = false;
        bool JustUpdated = false;

        #region Interop

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
        public static extern bool SetForegroundWindow(IntPtr hwnd);

        public IntPtr GetWindowHandle()
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            return windowHandle;
        }
        #endregion Interop

        #region Properties

        public static Window WindowRef;

        public SpringboardManager SBM { get; set; }

        //Animations
        private AnimationHelper CanvasOpacityAnim = null;

        private AnimationHelper CanvasScaleAnim = null;

        private bool StartingItem = false;
        private bool FadingOut = false;

        //workarounds
        private WallpaperUtils WPWatch = null;

        #region fps

        private Stopwatch FPSWatch = null;
        private double lasttime = 0;
        private int framerun = 10;
        private int curframe = 0;

        #endregion fps

        #endregion Properties

        #region Init
        #region Mutex

        private Mutex mutex;
        private string MutexName = "_WinLaunchMutex_";

        private void SetMutex()
        {
            mutex = new Mutex(true, MutexName);
        }

        // true - already running
        private bool CheckMutex()
        {
            try
            {
                mutex = Mutex.OpenExisting(MutexName);
                return true;
            }
            catch //(Exception Ex)
            {
                //winLaunch not running
                return false;
            }
        }

        private bool PerformMutexCheck()
        {
            if (CheckMutex())
            {
                //2. instance - close
                return true;
            }

            //1. instance - set mutex
            SetMutex();
            return false;
        }

        #endregion Mutex

        public static List<string> AddFiles = null;

        BackupManager backupManager;
        public Theme theme { get; set; }
        public Settings settings { get; set; }

        public MainWindow()
        {
            //when autostarted path is screwed up (c:/windows/system32/)
            SetHomeDirectory();

            PortabilityManager.Init();

            //show eula
            Eula.ShowEULA();

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            //setup backup manager
            backupManager = new BackupManager(20);

            if (PerformMutexCheck())
            {
                ShortcutActivation.FindAndActivate();
                //wake main instance of WinLaunch then exit
                Environment.Exit(-1);
            }
            
            InitializeComponent();

            CanvasOpacityAnim = new AnimationHelper(0.0, 1.0);
            CanvasScaleAnim = new AnimationHelper(0.4, 1.0);

            FPSWatch = new Stopwatch();
            WPWatch = new WallpaperUtils();

            //hook up events
            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);

            //setup window
            this.AllowDrop = true;

            this.ShowInTaskbar = false;

            //load settings
            InitImageRessources();
            this.ShowActivated = true;

            //start window hidden
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.Width = 0;
            this.Height = 0;

            //load settings and setup deskmode / no deskmode
            Settings.CurrentSettings = Settings.LoadSettings();

            if (!Settings.CurrentSettings.DeskMode)
                this.Topmost = true;

            //load theme
            Theme.CurrentTheme = Theme.LoadTheme();

            //enable if aero is in use and available
            //if (Theme.CurrentTheme.UseAeroBlur && GlassUtils.IsBlurBehindAvailable())
            if(Settings.CurrentSettings.DeskMode && Settings.CurrentSettings.LegacyDeskMode)
            {
                //disable on legacy desk mode
                this.AllowsTransparency = false;
            }
            else
            {
                //with the new desk mode we can enable this all the time
                this.AllowsTransparency = true;
            }
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            CrashReporter.Report(e.Exception);

            MessageBox.Show("WinLaunch just crashed!\nplease submit a bug report to winlaunch.official@gmail.com\nerror: " + e.Exception.Message);
            Environment.Exit(1);
        }

        private void InitImageRessources()
        {
            //Set non themeable Image Ressources
            SBItem.LoadingImage = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/Loading.png"));

            if (SBItem.LoadingImage.CanFreeze)
                SBItem.LoadingImage.Freeze();

            SBItem.shadow = this.FindResource("shadow") as BitmapImage;

            if (SBItem.shadow.CanFreeze)
                SBItem.shadow.Freeze();

            SBItem.DropFolderImage = this.FindResource("drop") as BitmapImage;

            if (SBItem.DropFolderImage.CanFreeze)
                SBItem.DropFolderImage.Freeze();
        }

        private void InitSBM()
        {
            SBM = new SpringboardManager();

            SBM.Init(this, this.MainCanvas);
            SBM.ParentWindow = this;
        }

        private void BeginInitIC(Action continueWith)
        {
            try
            {
                //do in parallel?
                if (!File.Exists(PortabilityManager.ItemsPath) && backupManager.GetLatestBackup() != null || !SBM.IC.LoadFromXML(PortabilityManager.ItemsPath))
                {
                    //item loading failed
                    //backup procedure
                    List<BackupEntry> backups = backupManager.GetBackups();

                    //try loading all backups until one succeeds
                    bool success = false;
                    for (int i = 0; i < backups.Count; i++)
                    {
                        if (SBM.IC.LoadFromXML(backups[i].path))
                        {
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        //failed to load items or backups
                        CrashReporter.Report("Item loading failed, tried " + backups.Count + " backups");
                        MessageBox.Show("Item loading failed", "WinLaunch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        //immediately save new items
                        PerformItemBackup();
                    }
                }

                SBM.UpdateIC();

                FPSWatch.Start();
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);
            }

            SBM.IC.LoadIconsInBackground(Dispatcher, continueWith);
        }

        private void AddArgumentFiles()
        {
            if (AddFiles != null)
            {
                foreach (string file in AddFiles)
                {
                    AddFile(file);
                }
            }
        }

        #endregion Init

        #region Utils
        private void SetHomeDirectory()
        {
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void CleanMemory()
        {
            GC.Collect();
        }

        #endregion Utils

        #region Search
        public bool SearchActive = false;

        private void ActivateSearch()
        {
            if (SearchActive)
                return;

            SearchActive = true;

            tbSearch.CaretBrush = new SolidColorBrush(Colors.White);
            imCancel.Visibility = Visibility.Visible;

            SBM.UnselectItem();
        }

        private void DeactivateSearch()
        {
            tbSearch.Clear();

            SearchActive = false;

            tbSearch.CaretBrush = new SolidColorBrush(Colors.Transparent);
            imCancel.Visibility = Visibility.Collapsed;


            SBM.EndSearch();
        }

        #endregion

        #region Folder Title Renaming

        public bool FolderRenamingActive = false;

        private void ActivateFolderRenaming()
        {
            FolderRenamingActive = true;

            if (Theme.CurrentTheme.UseVectorFolder)
            {
                FolderTitle.Visibility = System.Windows.Visibility.Collapsed;
                FolderTitleShadow.Visibility = System.Windows.Visibility.Collapsed;

                FolderTitleEdit.Visibility = System.Windows.Visibility.Visible;

                //focus field
                Keyboard.Focus(FolderTitleEdit);

                FolderTitleEdit.Text = FolderTitle.Text;
                FolderTitleEdit.SelectAll();
            }
            else
            {
                FolderTitleNew.Visibility = System.Windows.Visibility.Collapsed;
                FolderTitleShadowNew.Visibility = System.Windows.Visibility.Collapsed;

                FolderTitleEditNew.Visibility = System.Windows.Visibility.Visible;

                //focus field
                Keyboard.Focus(FolderTitleEditNew);

                FolderTitleEditNew.Text = FolderTitleNew.Text;
                FolderTitleEditNew.SelectAll();
            }
        }

        private void DeactivateFolderRenaming()
        {
            if (!FolderRenamingActive)
                return;

            FolderRenamingActive = false;

            if (Theme.CurrentTheme.UseVectorFolder)
            {
                FolderTitle.Visibility = System.Windows.Visibility.Visible;
                FolderTitleShadow.Visibility = System.Windows.Visibility.Visible;

                FolderTitleEdit.Visibility = System.Windows.Visibility.Collapsed;

                //Set the edited text as new title
                FolderTitle.Text = ValidateFolderName(FolderTitleEdit.Text);

                SBM.ActiveFolder.Name = FolderTitle.Text;
            }
            else
            {
                FolderTitleNew.Visibility = System.Windows.Visibility.Visible;
                FolderTitleShadowNew.Visibility = System.Windows.Visibility.Visible;

                FolderTitleEditNew.Visibility = System.Windows.Visibility.Collapsed;

                //Set the edited text as new title
                FolderTitleNew.Text = ValidateFolderName(FolderTitleEditNew.Text);

                SBM.ActiveFolder.Name = FolderTitleNew.Text;
            }

            TriggerSaveItemsDelayed();
        }

        private void FolderTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ActivateFolderRenaming();
            e.Handled = true;
        }

        private string ValidateFolderName(string FolderName)
        {
            if (string.IsNullOrWhiteSpace(FolderName))
                FolderName = "NewFolder";

            return FolderName;
        }

        //Direct events sent by springboard manager
        public void FolderOpened()
        {
            if (Theme.CurrentTheme.UseVectorFolder)
            {
                //update Folder title UI text
                FolderTitle.Text = SBM.ActiveFolder.Name;
                FolderTitleEdit.Text = FolderTitle.Text;
            }
            else
            {
                //update Folder title UI text
                FolderTitleNew.Text = SBM.ActiveFolder.Name;
                FolderTitleEditNew.Text = FolderTitleNew.Text;
            }

            //fade page counters out
            PageCounter.BeginAnimation(StackPanel.OpacityProperty, new DoubleAnimation(0.0, new Duration(new TimeSpan(0, 0, 0, 0, 200))));
        }

        public void FolderClosed()
        {
            DeactivateFolderRenaming();

            strokepath.Visibility = Visibility.Collapsed;

            //fade page counters in
            PageCounter.BeginAnimation(StackPanel.OpacityProperty, new DoubleAnimation(1.0, new Duration(new TimeSpan(0, 0, 0, 0, 200))));
        }

        private void FolderTitleEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                DeactivateFolderRenaming();
            }
        }



        #endregion Folder Title Renaming
    }
}