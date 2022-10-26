using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    public sealed class SBItem : DependencyObject
    {
        #region styles

        //Image ressources
        public static BitmapSource shadow;

        public static BitmapSource FolderIcon;
        public static BitmapSource CloseBoxImage;
        public static BitmapSource LoadingImage;
        public static BitmapSource DropFolderImage;

        public static double TextSize = 1.0;

        public static FontFamily ItemFont = new FontFamily("Lucida Sans Unicode");

        #endregion styles

        //Item Properties
        public ItemCollection IC;

        public bool IsFolder;

        public string Name { get; set; }

        public string ApplicationPath { get; set; }

        public string IconPath { get; set; }

        public string Arguments { get; set; }

        public bool RunAsAdmin { get; set; }

        public int Page = -1;
        public int GridIndex = -1;

        static Random rd = new Random();
        public void StartWiggle()
        {
            RotateAngle = (WiggleAngle * -1) + (rd.NextDouble() * (WiggleAngle * 2));
            Wiggle = true;

            ShowClose = true;
        }

        public void StopWiggle()
        {
            Wiggle = false;
            ShowClose = false;
        }

        public SBItem(string Name, string ApplicationPath, string IconPath, string Arguments, BitmapSource Icon, double XPos = 0.0, double YPos = 0.0)
        {
            this.IC = new ItemCollection();
            this.Name = Name;
            this.ApplicationPath = ApplicationPath;
            this.IconPath = IconPath;
            this.Arguments = Arguments;
            this.Icon = Icon;

            this.UpdateIcon();

            this.ContentRef = new ContentControl();
            this.ContentRef.Content = this;

            this.ContentRef.SetValue(Canvas.ZIndexProperty, (int)-1);
            this.ContentRef.SetValue(Canvas.LeftProperty, XPos);
            this.ContentRef.SetValue(Canvas.TopProperty, YPos);
            this.ContentRef.Focusable = false;

            this.ContentRef.SnapsToDevicePixels = true;
            //RenderOptions.SetBitmapScalingMode(this.ContentRef, BitmapScalingMode.LowQuality);

            InitAnimations();
        }

        //dependency properties

        #region PROPDP



        public bool IsEnabled
        {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register("IsEnabled", typeof(bool), typeof(SBItem), new PropertyMetadata(true));



        public SolidColorBrush SelectionBorder
        {
            get { return (SolidColorBrush)GetValue(SelectionBorderProperty); }
            set { SetValue(SelectionBorderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionBorder.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionBorderProperty =
            DependencyProperty.Register("SelectionBorder", typeof(SolidColorBrush), typeof(SBItem), new PropertyMetadata(Brushes.Transparent));



        public Effect IconEffect
        {
            get { return (Effect)GetValue(IconEffectProperty); }
            set { SetValue(IconEffectProperty, value); }
        }

        public static readonly DependencyProperty IconEffectProperty =
            DependencyProperty.Register("IconEffect", typeof(Effect), typeof(SBItem), new UIPropertyMetadata(null));


        public double RotateAngle
        {
            get { return (double)GetValue(RotateAngleProperty); }
            set { SetValue(RotateAngleProperty, value); }
        }

        public static readonly DependencyProperty RotateAngleProperty =
            DependencyProperty.Register("RotateAngle", typeof(double), typeof(SBItem), new UIPropertyMetadata(0.0));


        public bool ShowClose
        {
            get { return (bool)GetValue(ShowCloseProperty); }
            set { SetValue(ShowCloseProperty, value); }
        }
        public static readonly DependencyProperty ShowCloseProperty =
            DependencyProperty.Register("ShowClose", typeof(bool), typeof(SBItem), new UIPropertyMetadata(false));


        public BitmapSource CloseBox
        {
            get { return (BitmapSource)GetValue(CloseBoxProperty); }
            set { SetValue(CloseBoxProperty, value); }
        }

        public static readonly DependencyProperty CloseBoxProperty =
            DependencyProperty.Register("CloseBox", typeof(BitmapSource), typeof(SBItem), new UIPropertyMetadata(null));


        public Thickness CloseBoxMargin
        {
            get { return (Thickness)GetValue(CloseBoxMarginProperty); }
            set { SetValue(CloseBoxMarginProperty, value); }
        }

        public static readonly DependencyProperty CloseBoxMarginProperty =
            DependencyProperty.Register("CloseBoxMargin", typeof(Thickness), typeof(SBItem), new UIPropertyMetadata(new Thickness(10, 20, 0, 0)));


        public double Scale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("Scale", typeof(double), typeof(SBItem), new UIPropertyMetadata(1.0));


        public Rect ClipRectangle
        {
            get { return (Rect)GetValue(ClipRectangleProperty); }
            set { SetValue(ClipRectangleProperty, value); }
        }

        private static readonly DependencyProperty ClipRectangleProperty =
            DependencyProperty.Register("ClipRectangle", typeof(Rect), typeof(SBItem), new UIPropertyMetadata(new Rect(0, 0, 180, 185)));
        #region Icon properties


        #region Drop properties
        public Thickness DropMargin
        {
            get { return (Thickness)GetValue(DropMarginProperty); }
            set { SetValue(DropMarginProperty, value); }
        }

        public static readonly DependencyProperty DropMarginProperty =
            DependencyProperty.Register("DropMargin", typeof(Thickness), typeof(SBItem), new UIPropertyMetadata(new Thickness(0)));


        public double DropScale
        {
            get { return (double)GetValue(DropScaleProperty); }
            set { SetValue(DropScaleProperty, value); }
        }

        public static readonly DependencyProperty DropScaleProperty =
            DependencyProperty.Register("DropScale", typeof(double), typeof(SBItem), new UIPropertyMetadata(1.0));


        public double DropOpacity
        {
            get { return (double)GetValue(DropOpacityProperty); }
            set { SetValue(DropOpacityProperty, value); }
        }

        public static readonly DependencyProperty DropOpacityProperty =
            DependencyProperty.Register("DropOpacity", typeof(double), typeof(SBItem), new UIPropertyMetadata(0.0));


        public BitmapSource DropImage
        {
            get { return (BitmapSource)GetValue(DropImageProperty); }
            set { SetValue(DropImageProperty, value); }
        }

        public static readonly DependencyProperty DropImageProperty =
            DependencyProperty.Register("DropImage", typeof(BitmapSource), typeof(SBItem), new UIPropertyMetadata(null));
        #endregion Drop properties


        public BitmapSource Shadow
        {
            get { return (BitmapSource)GetValue(ShadowProperty); }
            set { SetValue(ShadowProperty, value); }
        }

        public static readonly DependencyProperty ShadowProperty =
            DependencyProperty.Register("Shadow", typeof(BitmapSource), typeof(SBItem), new UIPropertyMetadata(null));


        public double ShadowOpacity
        {
            get { return (double)GetValue(ShadowOpacityProperty); }
            set { SetValue(ShadowOpacityProperty, value); }
        }

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register("ShadowOpacity", typeof(double), typeof(SBItem), new UIPropertyMetadata(1.0));


        public double IconWidth
        {
            get { return (double)GetValue(IconWidthProperty); }
            set { SetValue(IconWidthProperty, value); }
        }

        public static readonly DependencyProperty IconWidthProperty =
            DependencyProperty.Register("IconWidth", typeof(double), typeof(SBItem), new UIPropertyMetadata(70.0));


        public double IconHeight
        {
            get { return (double)GetValue(IconHeightProperty); }
            set { SetValue(IconHeightProperty, value); }
        }

        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register("IconHeight", typeof(double), typeof(SBItem), new UIPropertyMetadata(70.0));


        public double MiniatureWidth
        {
            get { return (double)GetValue(MiniatureWidthProperty); }
            set { SetValue(MiniatureWidthProperty, value); }
        }

        public static readonly DependencyProperty MiniatureWidthProperty =
            DependencyProperty.Register("MiniatureWidth", typeof(double), typeof(SBItem), new UIPropertyMetadata(50.0));


        public Thickness MiniatureMargin
        {
            get { return (Thickness)GetValue(MiniatureMarginProperty); }
            set { SetValue(MiniatureMarginProperty, value); }
        }

        public static readonly DependencyProperty MiniatureMarginProperty =
            DependencyProperty.Register("MiniatureMargin", typeof(Thickness), typeof(SBItem), new UIPropertyMetadata(new Thickness(0)));


        public BitmapSource Icon
        {
            get { return (BitmapSource)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(BitmapSource), typeof(SBItem), new UIPropertyMetadata(null));


        public double IconOpacity
        {
            get { return (double)GetValue(IconOpacityProperty); }
            set { SetValue(IconOpacityProperty, value); }
        }

        public static readonly DependencyProperty IconOpacityProperty =
            DependencyProperty.Register("IconOpacity", typeof(double), typeof(SBItem), new UIPropertyMetadata(1.0));


        public BitmapSource FolderMiniatures
        {
            get { return (BitmapSource)GetValue(FolderMiniaturesProperty); }
            set { SetValue(FolderMiniaturesProperty, value); }
        }

        public static readonly DependencyProperty FolderMiniaturesProperty =
            DependencyProperty.Register("FolderMiniatures", typeof(BitmapSource), typeof(SBItem), new UIPropertyMetadata(null));


        public FontFamily FontFamily
        {
            get { return (FontFamily)GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register("FontFamily", typeof(FontFamily), typeof(SBItem), new UIPropertyMetadata(new FontFamily()));


        public double FontSize
        {
            get { return (double)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register("FontSize", typeof(double), typeof(SBItem), new UIPropertyMetadata(0.0));


        public SolidColorBrush FontColor
        {
            get { return (SolidColorBrush)GetValue(FontColorProperty); }
            set { SetValue(FontColorProperty, value); }
        }

        public static readonly DependencyProperty FontColorProperty =
            DependencyProperty.Register("FontColor", typeof(SolidColorBrush), typeof(SBItem), new UIPropertyMetadata(Brushes.White));


        public Color FontShadowColor
        {
            get { return (Color)GetValue(FontShadowColorProperty); }
            set { SetValue(FontShadowColorProperty, value); }
        }

        public static readonly DependencyProperty FontShadowColorProperty =
            DependencyProperty.Register("FontShadowColor", typeof(Color), typeof(SBItem), new UIPropertyMetadata(Colors.Black));


        public double FontShadowOpacity
        {
            get { return (double)GetValue(FontShadowOpacityProperty); }
            set { SetValue(FontShadowOpacityProperty, value); }
        }

        public static readonly DependencyProperty FontShadowOpacityProperty =
            DependencyProperty.Register("FontShadowOpacity", typeof(double), typeof(SBItem), new UIPropertyMetadata(1.0));


        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(SBItem), new UIPropertyMetadata(""));


        public Visibility TextVisible
        {
            get { return (Visibility)GetValue(TextVisibleProperty); }
            set { SetValue(TextVisibleProperty, value); }
        }

        public static readonly DependencyProperty TextVisibleProperty =
            DependencyProperty.Register("TextVisible", typeof(Visibility), typeof(SBItem), new UIPropertyMetadata(Visibility.Visible));


        public double TextOpacity
        {
            get { return (double)GetValue(TextOpacityProperty); }
            set { SetValue(TextOpacityProperty, value); }
        }

        public static readonly DependencyProperty TextOpacityProperty =
            DependencyProperty.Register("TextOpacity", typeof(double), typeof(SBItem), new UIPropertyMetadata(1.0));


        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }

        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register("TextMargin", typeof(Thickness), typeof(SBItem), new UIPropertyMetadata(new Thickness(0, 10, 0, 0)));
        #endregion Icon properties


        #endregion PROPDP
        public ContentControl ContentRef;

        #region RenderEngine

        public void UpdateFolderIcon(bool TextVisible = true, bool SetInstant = false)
        {
            if (this.IsFolder)
            {
                if (this.IconPath == null && this.Icon != SBItem.FolderIcon)
                    this.Icon = SBItem.FolderIcon;

                this.FolderMiniatures = SBItem.RenderFolderMiniatureIcons(this);

                //Animate text opacity
                double SetTextOpacity = (double)(TextVisible ? 1.0 : 0.0);
                TextOpacityAnim.Value = (double)(SetInstant ? SetTextOpacity : TextOpacityAnim.Value);
                TextOpacityAnim.ValueTo = SetTextOpacity;

                UpdateIcon();
            }
        }

        public void UpdateIcon()
        {
            try
            {
                //Update images
                this.DropImage = SBItem.DropFolderImage;
                this.Shadow = SBItem.shadow;
                this.CloseBox = SBItem.CloseBoxImage;

                //Update Text & Font
                if (SBItem.TextSize != 0)
                {
                    this.FontSize = (double)13 * SBItem.TextSize;
                    this.Text = Name;//MiscUtils.GetLionFormattedText(Name, SBItem.ItemFont, this.FontSize, 150);
                    this.FontColor = new SolidColorBrush(Theme.CurrentTheme.IconTextColor);
                    this.FontShadowColor = Theme.CurrentTheme.IconTextShadowColor;

                    //update font shadow opacity
                    this.FontShadowOpacity = (double)Theme.CurrentTheme.IconTextShadowColor.ScA;

                    this.TextVisible = Visibility.Visible;
                }
                else
                {
                    //Hide text
                    this.TextVisible = Visibility.Hidden;
                }

                this.FontFamily = SBItem.ItemFont;

                //Update Shadow Opacity
                this.ShadowOpacity = Theme.CurrentTheme.IconShadowOpacity;

                //Update positions
                //only even numbers are displayed sharp ?!
                this.IconWidth = MathHelper.ToEven(70.0 * Settings.CurrentSettings.IconSize);
                this.IconHeight = MathHelper.ToEven(this.IconWidth + 10.0);
                this.MiniatureWidth = MathHelper.ToEven(58.0 * Settings.CurrentSettings.IconSize);
                this.TextMargin = new Thickness(0, 10.0 + IconHeight, 0, 0);

                double progress = (Settings.CurrentSettings.IconSize - 1.0) * (1.0 / 0.4);
                this.CloseBoxMargin = new Thickness(15 - 7 * progress, 5, 0, 0);

                if (this.ContentRef != null)
                    this.ContentRef.UpdateLayout();
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);
            }
        }

        public void ScrollPreviewToBottom()
        {
            if (this.IsFolder)
            {
                double Items = (double)this.IC.Items.Count;
                int Rows = (int)Math.Ceiling(Items / 3.0);

                double RowHeight = (this.MiniatureWidth / 113.0) * (32.0 + 6.0);

                int ScrolledRows = Rows - 2;

                if (ScrolledRows < 0)
                    ScrolledRows = 0;

                if (ScrolledRows > 8)
                    ScrolledRows = 8;

                //scroll
                PreviewMarginAnimation.ValueTo = -1 * ScrolledRows * RowHeight;
            }
        }

        public void ScrollPreviewToTop()
        {
            PreviewMarginAnimation.ValueTo = 0;
        }

        public void ShowDrop()
        {
            if (this.IsFolder)
            {
                IconOpacityAnim.ValueTo = 0.0;
            }

            //adjust margin
            //1.0 -45
            double progress = (Settings.CurrentSettings.IconSize - 1.0) * (1.0 / 0.4);
            DropMargin = new Thickness(0, -50 + (4 * progress), 0, 0);

            DropScaleAnim.Value = 0.5 * Settings.CurrentSettings.IconSize;
            DropScaleAnim.ValueTo = 0.65 * Settings.CurrentSettings.IconSize;

            DropOpacityAnim.Value = 0.0;
            DropOpacityAnim.ValueTo = 1.0;

            TextOpacityAnim.ValueTo = 0.0;

            ScrollPreviewToBottom();
        }

        public void HideDrop(bool TextVisible = true)
        {
            if (this.IsFolder)
            {
                IconOpacityAnim.ValueTo = 1.0;
            }

            DropScaleAnim.ValueTo = 0.5 * Settings.CurrentSettings.IconSize;
            DropOpacityAnim.ValueTo = 0.0;

            if (TextVisible)
                TextOpacityAnim.ValueTo = 1.0;

            ScrollPreviewToTop();
        }

        public static BitmapSource RenderFolderMiniatureIcons(SBItem Folder)
        {
            Grid InnerFolderGrid = new Grid();
            InnerFolderGrid.FlowDirection = TranslationSource.Instance.direction;
            InnerFolderGrid.Background = new SolidColorBrush(Color.FromArgb(0x01, 0x00, 0x00, 0x00));

            WrapPanel IconWrap = new WrapPanel();
            RenderOptions.SetBitmapScalingMode(IconWrap, BitmapScalingMode.HighQuality);
            IconWrap.Width = 113;
            InnerFolderGrid.Children.Add(IconWrap);

            int CurrentGridIndex = 0;
            SBItem CurrentItem = null;

            while (true)
            {
                CurrentItem = null;

                //get next grid index
                foreach (SBItem item in Folder.IC.Items)
                {
                    if (item.GridIndex == CurrentGridIndex)
                    {
                        CurrentGridIndex++;
                        CurrentItem = item;
                        break;
                    }
                }

                if (CurrentItem == null)
                    break;

                Grid frame = new Grid();
                frame.Width = 31;
                frame.Height = 32;
                frame.Margin = new Thickness(3);
                frame.SnapsToDevicePixels = true;

                Image miniature = new Image();
                miniature.Source = CurrentItem.Icon;
                miniature.Width = 28;
                frame.Children.Add(miniature);

                DropShadowEffect dse = new DropShadowEffect();
                dse.BlurRadius = 2;
                dse.ShadowDepth = 1;
                dse.Direction = 270;
                frame.Effect = dse;

                IconWrap.Children.Add(frame);
            }

            InnerFolderGrid.UpdateLayout();
            InnerFolderGrid.Arrange(new Rect(new Size(113, 450)));

            RenderTargetBitmap bmp = new RenderTargetBitmap((int)InnerFolderGrid.ActualWidth, (int)InnerFolderGrid.ActualHeight, 96, 96, PixelFormats.Pbgra32);

            bmp.Render(InnerFolderGrid);

            if (bmp.CanFreeze)
                bmp.Freeze();

            return MiscUtils.DecoupleBitmap(bmp);
        }

        #endregion RenderEngine

        #region Clipping

        private bool ClipActive = false;

        //local coords
        public void SetLocalClip(double Y1, double Y2)
        {
            ClipActive = true;

            if (Y1 < 0)
                Y1 = 0;

            if (Y2 > this.ContentRef.ActualHeight)
                Y2 = this.ContentRef.ActualHeight;

            if (Y2 < 0)
            {
                //just to make it foolproof
                Y2 = 0;
            }

            if (Y1 > Y2)
            {
                //crossed -> hide everything
                Y1 = 0;
                Y2 = 0;
            }

            Rect rect = new Rect(0.0, Y1, this.ContentRef.ActualWidth, Y2 - Y1);
            ClipRectangle = rect;
        }

        //global
        public void SetGlobalClip(double Y1, double Y2)
        {
            //convert coordinates to local
            Point pos = this.GetPosition();

            SetLocalClip(Y1 - pos.Y, Y2 - pos.Y);
        }

        public void UnsetClip()
        {
            if (ClipActive)
                ClipActive = false;

            SetLocalClip(0.0, this.ContentRef.ActualHeight);
        }

        #endregion Clipping

        #region Effects

        public void SetDarkness(double Darkness)
        {
            if (Darkness == 0)
            {
                //delete effect
                this.IconEffect = null;
            }
            else
            {
                //create if effect is not yet created
                if (this.IconEffect == null)
                    this.IconEffect = new Darkening();

                (this.IconEffect as Darkening).Darkness = (float)Darkness;
            }
        }

        #endregion Effects

        #region Positioning / Animation

        public bool Wiggle = false;
        public double WiggleAngle = 1.3;
        public double WiggleSpeed = 0.335;
        private bool WiggleDirection = false;

        #region Animations

        private AnimationHelper XPosAnim = new AnimationHelper(0.0, 0.0);
        private AnimationHelper YPosAnim = new AnimationHelper(0.0, 0.0);

        public AnimationHelper TextOpacityAnim = new AnimationHelper(1.0, 1.0);
        public AnimationHelper OpacityAnim = new AnimationHelper(1.0, 1.0);
        public AnimationHelper ScaleAnim = new AnimationHelper(1.0, 1.0);
        public AnimationHelper IconOpacityAnim = new AnimationHelper(1.0, 1.0);

        //Drop animations
        public AnimationHelper DropOpacityAnim = new AnimationHelper(0.0, 0.0);

        public AnimationHelper DropScaleAnim = new AnimationHelper(0.7, 0.7);

        //Preview animations
        public AnimationHelper PreviewMarginAnimation = new AnimationHelper(0.0, 0.0);

        public void InitAnimations()
        {
            XPosAnim.duration = 600;
            YPosAnim.duration = 600;
        }

        #endregion Animations

        public double XPos
        {
            get { return (double)ContentRef.GetValue(Canvas.LeftProperty); }
            set { ContentRef.SetValue(Canvas.LeftProperty, value); }
        }

        public double YPos
        {
            get { return (double)ContentRef.GetValue(Canvas.TopProperty); }
            set { ContentRef.SetValue(Canvas.TopProperty, value); }
        }

        public int ZIndex
        {
            get { return (int)this.ContentRef.GetValue(Canvas.ZIndexProperty); }
            set { this.ContentRef.SetValue(Canvas.ZIndexProperty, (int)value); }
        }

        public double Opacity
        {
            get { return this.ContentRef.Opacity; }
            set { this.ContentRef.Opacity = value; }
        }

        //returns wheter the mouse is within the entire layout (150x150 px)
        public bool IsMouseOverLayout
        {
            get { return ContentRef.IsMouseOver; }
        }

        //returns wheter the mouse is over the closebox image
        public bool IsMouseOverCloseBox(MouseDevice MouseDev)
        {
            Point pos = MouseDev.GetPosition(this.ContentRef);

            //check position
            if (pos.X > CloseBoxMargin.Left && pos.X < CloseBoxMargin.Left + 30.0)
            {
                if (pos.Y > CloseBoxMargin.Top && pos.Y < CloseBoxMargin.Top + 30.0)
                {
                    return true;
                }
            }

            return false;
        }

        //returns wheter the mouse is generally over the icon, including text and closebox etc.
        public bool IsMouseOver(MouseDevice MouseDev)
        {
            if (ShowClose)
            {
                if (IsMouseOverCloseBox(MouseDev))
                {
                    return true;
                }
            }

            Point pos = MouseDev.GetPosition(this.ContentRef);

            //calculate positions
            double Width = this.IconWidth + 30;
            double Height = this.IconHeight + 25;

            double X1 = (this.ContentRef.ActualWidth / 2.0) - (Width / 2.0);
            double X2 = X1 + Width;

            double Y1 = 5;
            double Y2 = Y1 + Height;

            //check position
            if (pos.X > X1 && pos.X < X2)
            {
                if (pos.Y > Y1 && pos.Y < Y2)
                {
                    return true;
                }
            }

            return false;
        }

        //returns wheter the mouse is over the center of the icon, does not include text and closebox
        public bool IsMouseOverCenter(MouseDevice MouseDev)
        {
            Point pos = MouseDev.GetPosition(this.ContentRef);

            //calculate positions
            double Width = this.IconWidth;
            double Height = this.IconHeight;

            double X1 = (this.ContentRef.ActualWidth / 2.0) - (Width / 2.0);
            double X2 = X1 + Width;

            double Y1 = 20;
            double Y2 = Y1 + Height;

            //check position
            if (pos.X > X1 && pos.X < X2)
            {
                if (pos.Y > Y1 && pos.Y < Y2)
                {
                    return true;
                }
            }

            return false;
        }

        //is this item moved
        public bool Moved = false;

        #region CenterCoords

        //centers the position based on item size
        public Point CenterPointX(Point point)
        {
            return new Point(point.X - (this.ContentRef.ActualWidth / 2.0), point.Y);
        }

        //centers the position based on item size
        public Point CenterPointY(Point point)
        {
            return new Point(point.X, point.Y - (this.ContentRef.ActualHeight / 2.0));
        }

        //centers the position based on item size
        public Point CenterPointXY(Point point)
        {
            return CenterPointX(CenterPointY(point));
        }

        #endregion CenterCoords

        #region Offset

        private double XOffset = 0.0;
        private double YOffset = 0.0;

        //set OffsetPosition (scrolling etc.)
        public void SetOffsetPosition(double XOffset = 0.0, double YOffset = 0.0)
        {
            this.XOffset = XOffset;
            this.YOffset = YOffset;
        }

        public void ClearOffsetPosition()
        {
            this.XOffset = 0.0;
            this.YOffset = 0.0;
        }

        public Point GetOffset()
        {
            return new Point(XOffset, YOffset);
        }

        //sets position immediatly (no animation)
        public void SetPositionImmediate(Point pos)
        {
            XPosAnim.Value = pos.X;
            YPosAnim.Value = pos.Y;
        }

        #endregion Offset

        //sets the position the item will animate towards
        public void SetPosition(Point pos)
        {
            XPosAnim.ValueTo = pos.X;
            YPosAnim.ValueTo = pos.Y;
        }

        public Point GetPosition()
        {
            return new Point(XPosAnim.Value, YPosAnim.Value);
        }

        #region FixPosition

        private bool PositionFixed = false;
        private Point FixPosition = new Point();

        public void SetFixPosition(Point pos)
        {
            FixPosition = pos;
            PositionFixed = true;
        }

        public void ClearFixPosition()
        {
            PositionFixed = false;
        }

        public void SetPositionFromFixPosition()
        {
            ClearFixPosition();

            XPosAnim.Value = FixPosition.X - XOffset;
            YPosAnim.Value = FixPosition.Y - YOffset;
        }

        #endregion FixPosition

        public void FinishAnimations()
        {
            XPosAnim.Finish();
            YPosAnim.Finish();

            TextOpacityAnim.Finish();
            OpacityAnim.Finish();
            IconOpacityAnim.Step();
            ScaleAnim.Finish();

            DropOpacityAnim.Finish();
            DropScaleAnim.Finish();
        }

        //steps the animation
        public void StepPosition()
        {
            #region Step Animations

            if (!PositionFixed)
            {
                XPosAnim.Step();
                YPosAnim.Step();
            }

            TextOpacityAnim.Step();
            OpacityAnim.Step();
            IconOpacityAnim.Step();
            ScaleAnim.Step();

            DropOpacityAnim.Step();
            DropScaleAnim.Step();

            //Set Animation Values
            this.TextOpacity = this.TextOpacityAnim.Value;
            this.Opacity = this.OpacityAnim.Value;
            this.Scale = this.ScaleAnim.Value;
            this.IconOpacity = this.IconOpacityAnim.Value;

            this.DropOpacity = this.DropOpacityAnim.Value;
            this.DropScale = this.DropScaleAnim.Value;

            if (IsFolder)
            {
                if (!PreviewMarginAnimation.Step())
                {
                    MiniatureMargin = new Thickness(0, PreviewMarginAnimation.Value, 0, 0);
                }
            }

            #endregion Step Animations

            #region Wiggle Animation

            if (Wiggle)
            {
                double CurrentAngle = RotateAngle;

                if (WiggleDirection)
                {
                    //left
                    CurrentAngle -= WiggleSpeed;

                    if (CurrentAngle <= -1.0 * WiggleAngle)
                    {
                        WiggleDirection = false;
                    }
                }
                else if (!WiggleDirection)
                {
                    //right
                    CurrentAngle += WiggleSpeed;

                    if (CurrentAngle >= WiggleAngle)
                    {
                        WiggleDirection = true;
                    }
                }

                RotateAngle = CurrentAngle;
            }
            else
            {
                if (RotateAngle != 0)
                    RotateAngle = 0;
            }

            #endregion Wiggle Animation
        }

        //apply's position to rendered element
        public void ApplyPosition()
        {
            if (PositionFixed)
            {
                if (XPos != FixPosition.X)
                {
                    XPos = FixPosition.X;
                }

                if (XPos != FixPosition.Y)
                {
                    YPos = FixPosition.Y;
                }
            }
            else
            {
                if (XPos != XOffset + XPosAnim.Value)
                {
                    XPos = XOffset + XPosAnim.Value;
                }

                if (YPos != YOffset + YPosAnim.Value)
                {
                    YPos = YOffset + YPosAnim.Value;
                }
            }
        }

        #endregion Positioning / Animation
    }
}