using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        private void BeginLoadTheme(Action continueWith)
        {
            //if (JustUpdated)
            //{
            //    //TODO: remove for next version
            //    //for 0.5.0.0 enable Aero to show it off 
            //    if (!Settings.CurrentSettings.DeskMode)
            //    {
            //        Theme.CurrentTheme.UseAeroBlur = true;
            //        Theme.SaveTheme(Theme.CurrentTheme);
            //    }
            //}

            Task.Factory.StartNew(() =>
            {
                try
                {
                    bool shouldUseVectorFolder;

                    Theme.CurrentTheme.LoadImages(out shouldUseVectorFolder);

                    //set folder theme based on what images were loaded 
                    Theme.CurrentTheme.UseVectorFolder = shouldUseVectorFolder;
                }
                catch (Exception ex)
                {
                    CrashReporter.Report(ex);
                    MessageBox.Show(ex.Message);
                }
            }).ContinueWith(t =>
            {
                continueWith?.Invoke();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void InitTheme()
        {
            SetupThemes();

            //update bitmaps
            SBItem.FolderIcon = Theme.CurrentTheme.FolderIcon;
            SBItem.CloseBoxImage = Theme.CurrentTheme.CloseBox;

            theme = Theme.CurrentTheme;
            DataContext = this;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs("theme"));

            //redraw all icons
            SBM.UpdateIcons();

            UpdateFolder();
        }

        void UpdateFolder()
        {
            //update folder background
            try
            {
                FolderBackgroundTileBrush.ImageSource = Theme.CurrentTheme.center;

                //if (Theme.CurrentTheme.StretchFolderBG)
                //{
                //    double ratio = (double)Theme.CurrentTheme.FolderBackgroundTile.PixelWidth / (double)Theme.CurrentTheme.FolderBackgroundTile.PixelHeight;
                //    Rect screen = GetFullscreenRect();

                //    FolderBackgroundTileBrush.Viewport = new Rect(0, 0, screen.Width, screen.Width / ratio);
                //}
                //else
                {
                    FolderBackgroundTileBrush.Viewport = new Rect(0, 0, Theme.CurrentTheme.center.PixelWidth, Theme.CurrentTheme.center.PixelHeight);
                }
            }
            catch { }
        }

        public void SetupThemes()
        {
            //possible background settings
            //- aero (with or without accent colors)
            //- sync wallpaper
            //- custom wallpaper

            if (Theme.CurrentTheme.UseAeroBlur && GlassUtils.IsBlurBehindAvailable() && AllowsTransparency && !Settings.CurrentSettings.DeskMode)
            {
                //setup aero theme
                SetAeroBlurTheme();
            }
            else if (Theme.CurrentTheme.UseCustomBackground)
            {
                //custom theme
                SetCustomBackgroundTheme();
            }
            else
            {
                //fallback to synced
                SetSyncedTheme();
            }
        }

        public void SetAeroBlurTheme()
        {
            //hide wallpapers
            Wallpaperbottom.Visibility = System.Windows.Visibility.Collapsed;
            Wallpapernoblur.Visibility = System.Windows.Visibility.Collapsed;

            UpdateBackgroundColors();

            GlassUtils.EnableBlurBehind(this, Theme.CurrentTheme.UseAcrylic);
        }

        public void SetCustomBackgroundTheme()
        {
            //custom wallpaper
            GlassUtils.DisableBlurBehind(this);

            //force a re-blur
            Theme.CurrentTheme.BlurredBackground = null;

            //set the image
            SetBackgroundImage(Theme.CurrentTheme.Background);
        }

        public void SetSyncedTheme()
        {
            GlassUtils.DisableBlurBehind(this);

            //setup sync theme (wallpaper synced or solid background color synced)
            if (WallpaperUtils.IsUsingWallpaper())
            {
                //using wallpaper
                Theme.CurrentTheme.Background = MiscUtils.GetCurrentWallpaper();

                //force a re-blur
                Theme.CurrentTheme.BlurredBackground = null;

                //set the image
                SetBackgroundImage(Theme.CurrentTheme.Background);

                Background = new SolidColorBrush(Color.FromArgb(0x01, 0x00, 0x00, 0x00));
            }
            else
            {
                //using solid color

                //hide wallpapers
                Wallpaperbottom.Visibility = System.Windows.Visibility.Collapsed;
                Wallpapernoblur.Visibility = System.Windows.Visibility.Collapsed;

                UpdateBackgroundColors();
            }
        }

        public void UpdateBackgroundColors()
        {
            Color bgColor = Colors.Transparent;

            if (Theme.CurrentTheme.UseAeroBlur)
            {
                //set color according to start menu color
                if (WallpaperUtils.IsUsingAccentColor())
                {
                    //uses accent color
                    var AccentColor = WallpaperUtils.GetAccentColor();

                    byte a = (byte)(Theme.CurrentTheme.BackgroundTransparency * 255);
                    byte r = AccentColor.B;
                    byte g = AccentColor.G;
                    byte b = AccentColor.R;

                    bgColor = Color.FromArgb(a, r, g, b);
                }
                else
                {
                    //uses default color
                    bgColor = Color.FromArgb((byte)(Theme.CurrentTheme.BackgroundTransparency * 255), 0, 0, 0);
                }
            }
            else
            {
                //not using aero
                //set to solid color if we are not using a wallpaper
                if (!WallpaperUtils.IsUsingWallpaper())
                {
                    var color = WallpaperUtils.GetBackgroundColor();

                    bgColor = Color.FromArgb(255, color.R, color.G, color.B);
                }
            }

            Background = new SolidColorBrush(bgColor);
        }

        public void SetBackgroundImage(BitmapSource image, Action continueWith = null)
        {
            Task.Factory.StartNew(() => { }).ContinueWith(t =>
            {
                try
                {
                    Theme.CurrentTheme.Background = image;

                    BlurBackgroundImage();

                    //show wallpaper
                    Wallpaperbottom.Visibility = System.Windows.Visibility.Visible;
                    Wallpapernoblur.Visibility = System.Windows.Visibility.Visible;

                    //Wallpapernoblur.Source = Theme.CurrentTheme.Background;
                    Wallpaperbottom.Source = Theme.CurrentTheme.BlurredBackground;

                    SetBackgroundPosition();

                    //Fade in wallpaper
                    BGGrid.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(500)));
                }
                catch { }

                continueWith?.Invoke();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void BlurBackgroundImage()
        {
            try
            {
                if (Theme.CurrentTheme.BackgroundBlurRadius > 0.0)
                {
                    if (Theme.CurrentTheme.BlurredBackground == null)
                    {
                        Theme.CurrentTheme.BlurredBackground = MiscUtils.BlurImage(Theme.CurrentTheme.Background, 20.0 * Theme.CurrentTheme.BackgroundBlurRadius);
                        Theme.CurrentTheme.SaveBlurredBackground();
                    }
                }
                else
                {
                    //no blur required
                    Theme.CurrentTheme.BlurredBackground = Theme.CurrentTheme.Background;
                }
            }
            catch { }
        }

        public void ForceBackgroundUpdate(Action continueWith = null)
        {
            Task.Factory.StartNew(() => { }).ContinueWith(t =>
            {
                Theme.CurrentTheme.BlurredBackground = null;
                SetBackgroundImage(Theme.CurrentTheme.Background, continueWith);

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}