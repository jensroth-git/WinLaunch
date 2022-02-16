using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        private void RenderFrame(object sender, EventArgs e)
        {
            if (!IsHidden)
            {
                SBM.Step();

                UpdateFolderSettings();
                UpdateAnimations();

                #region empty springboard

                if (LoadingAssets)
                {
                    if (LoadingText.Visibility != System.Windows.Visibility.Visible)
                        LoadingText.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    if (LoadingText.Visibility != System.Windows.Visibility.Hidden)
                        LoadingText.Visibility = System.Windows.Visibility.Hidden;

                    //show howto message when no items are found
                    if (SBM.IC.Items.Count == 0)
                    {
                        SBM.StopMoveMode();

                        //want items? message
                        if (EmptySBText.Visibility != System.Windows.Visibility.Visible)
                            EmptySBText.Visibility = System.Windows.Visibility.Visible;
                    }
                    else
                    {
                        if (EmptySBText.Visibility != System.Windows.Visibility.Hidden)
                            EmptySBText.Visibility = System.Windows.Visibility.Hidden;
                    }
                }

                #endregion empty springboard

                #region fps

                //update fps
                int fps = (int)(1000.0 / (FPSWatch.ElapsedMilliseconds - lasttime));
                lasttime = FPSWatch.ElapsedMilliseconds;

                if (curframe == framerun)
                {
                    curframe = 0;
                    FPSCounter.Text = fps.ToString();
                    UpdatePageCounter();
                }
                else
                {
                    curframe++;
                }

                #endregion fps
            }
        }

        #region Background Positioning
        private Rect BackgroundPosition;

        private void SetBackgroundPosition()
        {
            if (Wallpaperbottom.Source == null)
                return;

            double BackgroundWidth = (Wallpaperbottom.Source as BitmapSource).PixelWidth;
            double BackgroundHeight = (Wallpaperbottom.Source as BitmapSource).PixelHeight;
            double BackgroundHWRatio = BackgroundHeight / BackgroundWidth;

            #region Screen

            int ScreenIndex;

            if (Settings.CurrentSettings.OpenOnActiveDesktop && !Settings.CurrentSettings.DeskMode)
            {
                ScreenIndex = MiscUtils.GetActiveScreenIndex();
            }
            else
            {
                ScreenIndex = Settings.CurrentSettings.ScreenIndex;
            }

            System.Windows.Forms.Screen[] Screens = System.Windows.Forms.Screen.AllScreens;

            if (ScreenIndex > Screens.GetUpperBound(0))
                ScreenIndex = 0;

            double ScreenLeft = (double)Screens[ScreenIndex].WorkingArea.Left / MiscUtils.GetDPIScale();
            double ScreenTop = (double)Screens[ScreenIndex].WorkingArea.Top / MiscUtils.GetDPIScale();

            double ScreenBoundsLeft = (double)Screens[ScreenIndex].Bounds.Left / MiscUtils.GetDPIScale();
            double ScreenBoundsTop = (double)Screens[ScreenIndex].Bounds.Top / MiscUtils.GetDPIScale();

            double WorkAreaWidth = (double)Screens[ScreenIndex].WorkingArea.Width / MiscUtils.GetDPIScale();
            double WorkAreaHeight = (double)Screens[ScreenIndex].WorkingArea.Height / MiscUtils.GetDPIScale();

            double ScreenWidth = (double)Screens[ScreenIndex].Bounds.Width / MiscUtils.GetDPIScale();
            double ScreenHeight = (double)Screens[ScreenIndex].Bounds.Height / MiscUtils.GetDPIScale();

            if (Settings.CurrentSettings.FillScreen)
            {
                ScreenLeft = (double)Screens[ScreenIndex].Bounds.Left / MiscUtils.GetDPIScale();
                ScreenTop = (double)Screens[ScreenIndex].Bounds.Top / MiscUtils.GetDPIScale();

                WorkAreaWidth = ScreenWidth;
                WorkAreaHeight = ScreenHeight;
            }

            double ScreenHWRatio = ScreenHeight / ScreenWidth;

            #endregion Screen

            double Xoffset = 0;
            double Yoffset = 0;
            double WPWidth = 0;
            double WPHeight = 0;

            //changed to 3 in W10
            double heightOffset = 2.0;

            if (Environment.OSVersion.Version.Major >= 10)
            {
                heightOffset = 3.0;
            }

            //Fill
            if (BackgroundHWRatio < ScreenHWRatio)
            {
                //Stretch to height 
                //center width
                Yoffset = 0;
                WPHeight = ScreenHeight;

                WPWidth = ScreenHeight / BackgroundHWRatio;
                Xoffset = -1 * ((WPWidth - ScreenWidth) / 2.0);
            }
            else
            {
                //Stretch to Width
                //cut height
                Xoffset = 0;
                WPWidth = ScreenWidth;

                WPHeight = ScreenWidth * BackgroundHWRatio;
                Yoffset = -1 * ((WPHeight - ScreenHeight) / heightOffset);
            }

            //correct for taskbar positioning
            Xoffset -= (ScreenLeft - ScreenBoundsLeft);
            Yoffset -= (ScreenTop - ScreenBoundsTop);

            //else if (Theme.CurrentTheme.BackgroundMode == BackgroundMode.Center)
            //{
            //    Xoffset -= (ScreenLeft % ScreenWidth);
            //    Yoffset -= (ScreenTop % ScreenHeight);

            //    //Center
            //    WPWidth = ScreenWidth;
            //    WPHeight = WPWidth * BackgroundHWRatio;

            //    if (WPHeight < ScreenHeight)
            //    {
            //        WPHeight = ScreenHeight;
            //        WPWidth = WPHeight / BackgroundHWRatio;

            //        Xoffset -= (WPWidth - ScreenWidth) / 2.0;
            //    }
            //    else
            //    {
            //        Yoffset -= (WPHeight - ScreenHeight) / 2.0;
            //    }
            //}
            //else if (Theme.CurrentTheme.BackgroundMode == BackgroundMode.Panorama)
            //{
            //    //Center
            //    WPWidth = ScreenWidth;
            //    WPHeight = WPWidth * BackgroundHWRatio;

            //    if (WPHeight < ScreenHeight)
            //    {
            //        WPHeight = ScreenHeight;
            //        WPWidth = WPHeight / BackgroundHWRatio;

            //        Xoffset -= (WPWidth - ScreenWidth) / 2.0;
            //    }
            //    else
            //    {
            //        Yoffset -= (WPHeight - ScreenHeight) / 2.0;
            //    }

            //    PanoramaScrollWidth = WPWidth - ScreenWidth;

            //    Xoffset -= PanoramaScrollWidth * PanoramaOffset;
            //}

            //set values
            BackgroundPosition = new Rect(Xoffset, Yoffset, WPWidth, WPHeight);
            Wallpaperbottom.Width = WPWidth;
            Wallpaperbottom.Height = WPHeight;

            Wallpapernoblur.Width = WPWidth;
            Wallpapernoblur.Height = WPHeight;

            Wallpaperbottom.Margin = new Thickness(Xoffset, Yoffset, 0, 0);
            Wallpapernoblur.Margin = new Thickness(Xoffset, Yoffset, 0, 0);
        }

        #endregion Background Positioning

        #region Folder Animation

        private void SetFolderPosition(double YOrigin, double YOffset, double Width, double Height, double PointerOffset, double AnimationProgress, bool FadeIn)
        {
            #region Fade border

            double FadeOutBorderStart = 0.5;

            if (FadeIn)
            {
                FolderBackgroundGrid.Opacity = 1.0;
            }
            else
            {
                if (AnimationProgress > FadeOutBorderStart)
                    FolderBackgroundGrid.Opacity = 1 - ((AnimationProgress - FadeOutBorderStart) / (1.0 - FadeOutBorderStart));
                else
                    FolderBackgroundGrid.Opacity = 1.0;
            }

            #endregion Fade border

            //Dimensions of the pointer
            double PointerHeight = 21.0;
            double PointerWidth = 66;

            //Round Values to reduce jitter
            YOrigin = Math.Round(YOrigin);
            YOffset = Math.Round(YOffset);
            Height = Math.Round(Height);
            PointerOffset = Math.Round(PointerOffset);

            double BottomPos = PointerHeight + Height;
            if (BottomPos < PointerHeight)
                BottomPos = PointerHeight;

            double PathXClip = 100;

            //FolderShapePath.StartPoint = new Point(-PathXClip, PointerHeight);

            ////Top Pointer
            //FolderPointerLeft.Point = new Point(PointerOffset - PointerWidth / 2.0, PointerHeight);
            //FolderPointerCenter.Point = new Point(PointerOffset, 0);
            //FolderPointerRight.Point = new Point(PointerOffset + PointerWidth / 2.0, PointerHeight);
            double ActualArrowOffset = PointerOffset - (120 + (PointerWidth / 2));

            if (ActualArrowOffset < 0)
                ActualArrowOffset = 0;

            FolderArrowOffsetColumn.Width = new GridLength(ActualArrowOffset);

            //Bottom Pointer
            double PointerAnimationProgress;
            if (FadeIn)
            {
                //Hiding the Pointer
                PointerAnimationProgress = MathHelper.Animate(AnimationProgress, 1.0, 0.0);
            }
            else
            {
                //Showing the Pointer
                PointerAnimationProgress = MathHelper.Animate(AnimationProgress * 1.5, 0.0, 1.0);
            }

            //FolderBottomPointerLeft.Point = new Point(PointerOffset - (PointerWidth / 2.0) * PointerAnimationProgress, BottomPos);
            //FolderBottomPointerCenter.Point = new Point(PointerOffset, BottomPos - PointerHeight * PointerAnimationProgress);
            //FolderBottomPointerRight.Point = new Point(PointerOffset + (PointerWidth / 2.0) * PointerAnimationProgress, BottomPos);

            //FolderTopRight.Point = new Point(Width + PathXClip, PointerHeight);

            //FolderBottomRight.Point = new Point(Width + PathXClip, BottomPos);
            //FolderBottomLeft.Point = new Point(-PathXClip, BottomPos);

            FolderGrid.Height = BottomPos;
            FolderGrid.Margin = new Thickness(0, YOffset, 0, 0);
            //strokepath.Margin = new Thickness(0, YOffset, 0, 0);

        }

        private void UpdateFolderSettings()
        {
            //update values until all animations are finished
            if (!SBM.FolderHeightAnim.animation_done || !SBM.FolderYOffsetAnim.animation_done)
            {
                FolderGrid.Visibility = System.Windows.Visibility.Visible;
                //strokepath.Visibility = System.Windows.Visibility.Visible;

                SetFolderPosition(SBM.FolderYOffsetOrigin, SBM.FolderYOffset, this.MainCanvas.ActualWidth, SBM.FolderHeightAnim.Value, SBM.FolderArrowOffset,
                    SBM.FolderHeightAnim.GetProgress() * SBM.FolderYOffsetAnim.GetProgress(),
                    SBM.FolderHeightAnim.ValueTo != 0);
            }
            else
            {
                if (!SBM.FolderOpen)
                {
                    //Hide the vector graphics if they are not shown
                    FolderGrid.Visibility = System.Windows.Visibility.Hidden;
                }
            }
        }

        //closes the folder programmatically
        private void CloseFolderGrid()
        {
            SetFolderPosition(0.0, 0.0, this.MainCanvas.ActualWidth, 0.0, 0.0, 1.0, false);
        }

        #endregion Folder Animation

        #region Animations

        private void UpdateAnimations()
        {
            if (!CanvasScaleAnim.Step())
            {
                //animation not done or just finished
                this.MainCanvasScale.CenterX = this.MainCanvas.Width / 2.0;
                this.MainCanvasScale.CenterY = this.MainCanvas.Height / 2.0;
                this.MainCanvasScale.ScaleX = CanvasScaleAnim.Value;
                this.MainCanvasScale.ScaleY = CanvasScaleAnim.Value;
            }

            if (!CanvasOpacityAnim.Step())
            {
                //animation not done or just finished
                this.Wallpaperbottom.Opacity = CanvasOpacityAnim.Value;

                //stop animation
                PageCounter.BeginAnimation(StackPanel.OpacityProperty, null);
                this.PageCounter.Opacity = CanvasOpacityAnim.Value;

                this.MainCanvas.Opacity = CanvasOpacityAnim.Value;
                this.Wallpapernoblur.Opacity = 0.0;// 1.0 - CanvasOpacityAnim.Value;
            }
            else
            {
                if (FadingOut)
                {
                    //fade out complete
                    //call close handler
                    HideWinLaunch();
                }
            }
        }

        private void StartLaunchAnimations(SBItem Item)
        {
            FadingOut = true;
            StartingItem = true;

            //CanvasScaleAnim.speed = 0.32 * Theme.CurrentTheme.FlyOutAnimationSpeed;
            //CanvasScaleAnim.Value = 1.0;
            //CanvasScaleAnim.ValueTo = 0.9;

            CanvasOpacityAnim.duration = 300;
            CanvasOpacityAnim.Value = 1.0;
            CanvasOpacityAnim.ValueTo = 0.0;

            Item.ScaleAnim.duration = 300;
            Item.ScaleAnim.Value = 1.0;
            Item.ScaleAnim.ValueTo = 1.6;

        }

        private void StartFlyInAnimation()
        {
            FadingOut = false;

            CanvasScaleAnim.duration = 400;
            CanvasScaleAnim.Value = 0.9;
            CanvasScaleAnim.ValueTo = 1.0;

            CanvasOpacityAnim.duration = 450;
            CanvasOpacityAnim.Value = 0.0;
            CanvasOpacityAnim.ValueTo = 1.0;

            //reposition background
            SetBackgroundPosition();
        }

        private void StartFlyOutAnimation()
        {
            FadingOut = true;

            CanvasScaleAnim.duration = 300;
            CanvasScaleAnim.Value = 1.0;
            CanvasScaleAnim.ValueTo = 0.9;

            CanvasOpacityAnim.duration = 300;
            CanvasOpacityAnim.Value = 1.0;
            CanvasOpacityAnim.ValueTo = 0.0;

            //reposition background
            SetBackgroundPosition();
        }

        private bool AnimationsRunning()
        {
            if (!CanvasScaleAnim.animation_done || !CanvasOpacityAnim.animation_done)
                return true;

            return false;
        }

        #endregion Animations

        #region PageCounters

        private int PreviousPageCount = -1;
        private int PreviousSelectedPage = -1;

        private void UpdatePageCounter()
        {
            int Pages = SBM.GM.GetUsedPages();
            int SelectedPage = (int)SBM.SP.CurrentPage;

            if (SBM.MoveMode)
            {
                if (Pages < (SelectedPage + 1))
                    Pages = SelectedPage + 1;
            }

            //Number of pages changed
            if (Pages != PreviousPageCount)
            {
                PageCounter.Children.Clear();

                if (Pages == 0)
                {
                    Ellipse ellipse = new Ellipse();
                    ellipse.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));

                    PageCounter.Children.Add(ellipse);
                }
                else
                {
                    for (int i = 0; i < Pages; i++)
                    {
                        Ellipse ellipse = new Ellipse();
                        ellipse.MouseUp += new MouseButtonEventHandler(ellipse_MouseUp);

                        if (i == SBM.SP.CurrentPage)
                        {
                            ellipse.Fill = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0xff, 0xff));
                        }
                        else
                        {
                            ellipse.Fill = new SolidColorBrush(Color.FromArgb(0x77, 0xff, 0xff, 0xff));
                        }

                        PageCounter.Children.Add(ellipse);
                    }
                }
            }
            else
            {
                //only selected page changed
                if (SelectedPage != PreviousSelectedPage)
                {
                    //selected page changed
                    ((SolidColorBrush)((Ellipse)PageCounter.Children[PreviousSelectedPage]).Fill).
                        BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation(Color.FromArgb(0x77, 0xff, 0xff, 0xff), new Duration(TimeSpan.FromMilliseconds(300))));

                    ((SolidColorBrush)((Ellipse)PageCounter.Children[SelectedPage]).Fill).
                        BeginAnimation(
                        SolidColorBrush.ColorProperty,
                        new ColorAnimation(Color.FromArgb(0xff, 0xff, 0xff, 0xff), new Duration(TimeSpan.FromMilliseconds(300))));
                }
            }

            PreviousPageCount = Pages;
            PreviousSelectedPage = SelectedPage;
        }

        private void ellipse_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 0 && !SBM.SP.ScrollingLocked)
            {
                Ellipse ellipse = sender as Ellipse;

                int index = 0;

                foreach (Ellipse item in PageCounter.Children)
                {
                    if (ellipse == item)
                        break;

                    index++;
                }

                if (!SBM.Moving)
                    SBM.SP.SetPage(index);
            }
        }

        #endregion PageCounters
    }
}