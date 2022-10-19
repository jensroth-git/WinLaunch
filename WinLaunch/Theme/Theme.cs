using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Serialization;

namespace WinLaunch
{
    public class Theme
    {
        public static string CurrentThemePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch/CurrentTheme");
        public static Theme CurrentTheme = null;

        #region Bitmaps
        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource CloseBox { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource FolderIcon { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource Background { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource BlurredBackground { get; set; }

        //folder
        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource leftTop { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource leftCenter { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource leftBottomShadow { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource leftBottomBorder { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource topRim { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource center { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource bottomShadow { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource bottomBorder { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource arrow { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource rightTop { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource rightCenter { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource rightBottomShadow { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource rightBottomBorder { get; set; }

        [System.Xml.Serialization.XmlIgnore]
        public BitmapSource ExtensionsToggle { get; set; }
        #endregion

        #region Grid Settings
        public int Columns { get; set; }
        public int Rows { get; set; }

        public int FolderColumns { get; set; }
        public int FolderRows { get; set; }
        #endregion

        #region Folder styling
        public Color FolderTitleShadowColor = Color.FromArgb(0x99, 0, 0, 0);
        public Color FolderTitleColor = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
        #endregion Folder styling

        #region BackgroundEffects
        public bool UseAeroBlur { get; set; }
        public bool UseAcrylic { get; set; }
        public double BackgroundBlurRadius { get; set; }
        public double BackgroundTransparency { get; set; }
        public bool UseCustomBackground { get; set; }
        #endregion BackgroundEffects

        #region Icons
        public double IconSize { get; set; }
        public Color IconTextColor { get; set; }
        public Color IconTextShadowColor { get; set; }
        public double IconShadowOpacity { get; set; }
        #endregion Icons

        #region UI
        public Color ExtensionBarTextColor = Color.FromArgb(0xff, 0xff, 0xff, 0xff);

        public bool ExtensionIconVisible { get; set; }
        #endregion UI

        public void LoadImages()
        {
            //load all images (prefreezed)
            if (CloseBox == null)
                try { CloseBox = MiscUtils.LoadBitmapImage(CurrentThemePath + "/closebox.png"); }
                catch { }

            if (FolderIcon == null)
                try { FolderIcon = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder_icon.png", 128); }
                catch { }

            if (Background == null)
                try { Background = MiscUtils.LoadBitmapImage(CurrentThemePath + "/bg.png"); }
                catch { }

            if (BlurredBackground == null)
                try { BlurredBackground = MiscUtils.LoadBitmapImage(CurrentThemePath + "/blurred_bg.png"); }
                catch { }

            if (ExtensionsToggle == null)
                try { ExtensionsToggle = MiscUtils.LoadBitmapImage(CurrentThemePath + "/extensions.png"); }
                catch { }


            //folder
            #region folder images disk
            if (leftTop == null)
                try { leftTop = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/leftTop.png"); }
                catch { }

            if (leftCenter == null)
                try { leftCenter = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/leftCenter.png"); }
                catch { }

            if (leftBottomShadow == null)
                try { leftBottomShadow = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/leftBottomShadow.png"); }
                catch { }

            if (leftBottomBorder == null)
                try { leftBottomBorder = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/leftBottomBorder.png"); }
                catch { }


            if (topRim == null)
                try { topRim = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/topRim.png"); }
                catch { }

            if (center == null)
                try { center = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/center.png"); }
                catch { }

            if (bottomShadow == null)
                try { bottomShadow = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/bottomShadow.png"); }
                catch { }

            if (bottomBorder == null)
                try { bottomBorder = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/bottomBorder.png"); }
                catch { }


            if (arrow == null)
                try { arrow = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/arrow.png"); }
                catch { }


            if (rightTop == null)
                try { rightTop = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/rightTop.png"); }
                catch { }

            if (rightCenter == null)
                try { rightCenter = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/rightCenter.png"); }
                catch { }

            if (rightBottomShadow == null)
                try { rightBottomShadow = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/rightBottomShadow.png"); }
                catch { }

            if (rightBottomBorder == null)
                try { rightBottomBorder = MiscUtils.LoadBitmapImage(CurrentThemePath + "/folder/rightBottomBorder.png"); }
                catch { }
            #endregion


            #region folder images app
            if (leftTop == null)
                leftTop = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftTop.png"));

            if (leftCenter == null)
                leftCenter = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftCenter.png"));

            if (leftBottomShadow == null)
                leftBottomShadow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftBottomShadow.png"));

            if (leftBottomBorder == null)
                leftBottomBorder = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftBottomBorder.png"));


            if (topRim == null)
                topRim = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/topRim.png"));

            if (center == null)
                center = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/center.png"));

            if (bottomShadow == null)
                bottomShadow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/bottomShadow.png"));

            if (bottomBorder == null)
                bottomBorder = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/bottomBorder.png"));


            if (arrow == null)
                arrow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/arrow.png"));


            if (rightTop == null)
                rightTop = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightTop.png"));

            if (rightCenter == null)
                rightCenter = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightCenter.png"));

            if (rightBottomShadow == null)
                rightBottomShadow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightBottomShadow.png"));

            if (rightBottomBorder == null)
                rightBottomBorder = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightBottomBorder.png"));
            #endregion


            if (Background == null)
                Background = MiscUtils.GetCurrentWallpaper();

            if (CloseBox == null)
                CloseBox = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/closebox.png"));

            if (FolderIcon == null)
                FolderIcon = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder_icon_transparent.png"));

            if (ExtensionsToggle == null)
                ExtensionsToggle = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/extensions.png"));

            FreezeImages();
        }

        private void FreezeImages()
        {
            if (CloseBox != null && CloseBox.CanFreeze)
                CloseBox.Freeze();

            if (FolderIcon != null && FolderIcon.CanFreeze)
                FolderIcon.Freeze();

            if (Background != null && Background.CanFreeze)
                Background.Freeze();

            if (BlurredBackground != null && BlurredBackground.CanFreeze)
                BlurredBackground.Freeze();



            if (leftTop != null && leftTop.CanFreeze)
                leftTop.Freeze();

            if (leftCenter != null && leftCenter.CanFreeze)
                leftCenter.Freeze();

            if (leftBottomShadow != null)
                leftBottomShadow.Freeze();

            if (leftBottomBorder != null)
                leftBottomBorder.Freeze();


            if (topRim != null)
                topRim.Freeze();

            if (center != null)
                center.Freeze();

            if (bottomShadow != null)
                bottomShadow.Freeze();

            if (bottomBorder != null)
                bottomBorder.Freeze();


            if (arrow != null)
                arrow.Freeze();


            if (rightTop != null)
                rightTop.Freeze();

            if (rightCenter != null)
                rightCenter.Freeze();

            if (rightBottomShadow != null)
                rightBottomShadow.Freeze();

            if (rightBottomBorder != null)
                rightBottomBorder.Freeze();

            if (ExtensionsToggle != null)
                ExtensionsToggle.Freeze();
        }

        #region Save Images
        public void SaveBlurredBackground()
        {
            if (!Directory.Exists(CurrentThemePath))
                Directory.CreateDirectory(CurrentThemePath);

            if (BlurredBackground != null)
                MiscUtils.SaveBitmapImage(BlurredBackground, CurrentThemePath + "/blurred_bg.png");
        }

        public void SaveBackground()
        {
            if (!Directory.Exists(CurrentThemePath))
                Directory.CreateDirectory(CurrentThemePath);

            if (Background != null)
                MiscUtils.SaveBitmapImage(Background, CurrentThemePath + "/bg.png");
        }

        public void SaveClosebox()
        {
            if (!Directory.Exists(CurrentThemePath))
                Directory.CreateDirectory(CurrentThemePath);

            if (CloseBox != null)
                MiscUtils.SaveBitmapImage(CloseBox, CurrentThemePath + "/closebox.png");
        }


        public void SaveFolderIcon()
        {
            if (!Directory.Exists(CurrentThemePath))
                Directory.CreateDirectory(CurrentThemePath);

            if (FolderIcon != null)
                MiscUtils.SaveBitmapImage(FolderIcon, CurrentThemePath + "/folder_icon.png");
        }
        #endregion

        #region Save/Load

        /// <summary>
        /// Set the default theme values
        /// </summary>
        public Theme()
        {
            Columns = 8;
            Rows = 5;

            FolderColumns = 8;
            FolderRows = 5;

            UseAeroBlur = true;
            UseAcrylic = false;
            BackgroundBlurRadius = 0.5;
            BackgroundTransparency = 0.8;
            UseCustomBackground = false;

            IconSize = 1.4;
            IconTextColor = Colors.White;
            IconTextShadowColor = Colors.Black;
            IconShadowOpacity = 1.0;

            ExtensionIconVisible = true;
        }

        /// <summary>
        /// loads the theme information 
        /// note: The images of the theme are not loaded yet
        /// </summary>
        /// <returns></returns>
        static public Theme LoadTheme()
        {
            Theme theme = null;

            if (!System.IO.Directory.Exists(CurrentThemePath))
                return new Theme();

            try
            {
                using (FileStream fs = new FileStream(CurrentThemePath + "/theme.xml", FileMode.Open))
                {
                    using (XmlReader read = XmlReader.Create(fs))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(Theme));
                        theme = (Theme)ser.Deserialize(read);
                    }
                }
            }
            catch
            {
                theme = new Theme();
            }

            return theme;
        }

        static public bool SaveTheme(Theme theme)
        {
            try
            {
                if (!System.IO.Directory.Exists(CurrentThemePath))
                    System.IO.Directory.CreateDirectory(CurrentThemePath);

                using (FileStream fs = new FileStream(CurrentThemePath + "/theme.xml", FileMode.Create))
                {
                    using (XmlWriter write = XmlWriter.Create(fs))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(Theme));
                        ser.Serialize(write, theme);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);
            }
            return false;
        }



        #endregion Save/Load
    }
}