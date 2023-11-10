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

        #region Folder styling
        public Color FolderTitleShadowColor = Color.FromArgb(0x99, 0, 0, 0);
        public Color FolderTitleColor = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
        public bool UseVectorFolder { get; set; }
        public bool StretchFolderBackground { get; set; }
        #endregion Folder styling

        #region BackgroundEffects
        public bool UseAeroBlur { get; set; }
        public bool UseAcrylic { get; set; }
        public double BackgroundBlurRadius { get; set; }
        public double BackgroundTransparency { get; set; }
        public bool UseCustomBackground { get; set; }
        #endregion BackgroundEffects

        #region Icons
        public Color IconTextColor { get; set; }
        public Color IconTextShadowColor { get; set; }
        public double IconShadowOpacity { get; set; }
        #endregion Icons

        #region UI
        public Color ExtensionBarTextColor = Color.FromArgb(0xff, 0xff, 0xff, 0xff);
        #endregion UI

        public void LoadImages(out bool shouldUseVectorFolder)
        {
            //load all images (prefreezed)
            if (CloseBox == null)
                try { CloseBox = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/closebox.png"); }
                catch { }

            if (FolderIcon == null)
                try { FolderIcon = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder_icon.png", 128); }
                catch { }

            if (Background == null)
                try { Background = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/bg.png"); }
                catch { }

            if (BlurredBackground == null)
                try { BlurredBackground = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/blurred_bg.png"); }
                catch { }

            if (ExtensionsToggle == null)
                try { ExtensionsToggle = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/extensions.png"); }
                catch { }


            //folder
            #region folder images disk
            if (leftTop == null)
                try { leftTop = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/leftTop.png"); }
                catch { }

            if (leftCenter == null)
                try { leftCenter = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/leftCenter.png"); }
                catch { }

            if (leftBottomShadow == null)
                try { leftBottomShadow = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/leftBottomShadow.png"); }
                catch { }

            if (leftBottomBorder == null)
                try { leftBottomBorder = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/leftBottomBorder.png"); }
                catch { }


            if (topRim == null)
                try { topRim = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/topRim.png"); }
                catch { }

            if (center == null)
                try { center = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/center.png"); }
                catch { }

            if (bottomShadow == null)
                try { bottomShadow = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/bottomShadow.png"); }
                catch { }

            if (bottomBorder == null)
                try { bottomBorder = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/bottomBorder.png"); }
                catch { }


            if (arrow == null)
                try { arrow = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/arrow.png"); }
                catch { }


            if (rightTop == null)
                try { rightTop = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/rightTop.png"); }
                catch { }

            if (rightCenter == null)
                try { rightCenter = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/rightCenter.png"); }
                catch { }

            if (rightBottomShadow == null)
                try { rightBottomShadow = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/rightBottomShadow.png"); }
                catch { }

            if (rightBottomBorder == null)
                try { rightBottomBorder = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + "/folder/rightBottomBorder.png"); }
                catch { }
            #endregion

            if (center != null && arrow == null)
            {
                shouldUseVectorFolder = true;
            }
            else
            {
                shouldUseVectorFolder = false;
            }


            #region folder images app
            if (leftTop == null)
                try { leftTop = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftTop.png")); }
                catch { }

            if (leftCenter == null)
                try { leftCenter = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftCenter.png")); }
                catch { }

            if (leftBottomShadow == null)
                try { leftBottomShadow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftBottomShadow.png")); }
                catch { }

            if (leftBottomBorder == null)
                try { leftBottomBorder = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/leftBottomBorder.png")); }
                catch { }


            if (topRim == null)
                try { topRim = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/topRim.png")); }
                catch { }

            if (center == null)
                try { center = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/center.png")); }
                catch { }

            if (bottomShadow == null)
                try { bottomShadow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/bottomShadow.png")); }
                catch { }

            if (bottomBorder == null)
                try { bottomBorder = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/bottomBorder.png")); }
                catch { }


            if (arrow == null)
                try { arrow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/arrow.png")); }
                catch { }


            if (rightTop == null)
                try { rightTop = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightTop.png")); }
                catch { }

            if (rightCenter == null)
                try { rightCenter = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightCenter.png")); }
                catch { }

            if (rightBottomShadow == null)
                try { rightBottomShadow = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightBottomShadow.png")); }
                catch { }

            if (rightBottomBorder == null)
                try { rightBottomBorder = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder/rightBottomBorder.png")); }
                catch { }
            #endregion

            if (CloseBox == null)
                try { CloseBox = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/closebox.png")); }
                catch { }

            if (FolderIcon == null)
                try { FolderIcon = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/folder_icon_transparent.png")); }
                catch { }

            if (ExtensionsToggle == null)
                try { ExtensionsToggle = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/extensions.png")); }
                catch { }

            if (Background == null)
                Background = MiscUtils.GetCurrentWallpaper();

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
            if (!Directory.Exists(PortabilityManager.ThemePath))
                Directory.CreateDirectory(PortabilityManager.ThemePath);

            if (BlurredBackground != null)
                MiscUtils.SaveBitmapImage(BlurredBackground, PortabilityManager.ThemePath + "/blurred_bg.png");
        }

        public void SaveBackground()
        {
            if (!Directory.Exists(PortabilityManager.ThemePath))
                Directory.CreateDirectory(PortabilityManager.ThemePath);

            if (Background != null)
                MiscUtils.SaveBitmapImage(Background, PortabilityManager.ThemePath + "/bg.png");
        }

        public void SaveClosebox()
        {
            if (!Directory.Exists(PortabilityManager.ThemePath))
                Directory.CreateDirectory(PortabilityManager.ThemePath);

            if (CloseBox != null)
                MiscUtils.SaveBitmapImage(CloseBox, PortabilityManager.ThemePath + "/closebox.png");
        }


        public void SaveFolderIcon()
        {
            if (!Directory.Exists(PortabilityManager.ThemePath))
                Directory.CreateDirectory(PortabilityManager.ThemePath);

            if (FolderIcon != null)
                MiscUtils.SaveBitmapImage(FolderIcon, PortabilityManager.ThemePath + "/folder_icon.png");
        }
        #endregion

        #region Save/Load

        /// <summary>
        /// Set the default theme values
        /// </summary>
        public Theme()
        {
            UseVectorFolder = false;
            StretchFolderBackground = false;

            UseAeroBlur = false;
            UseAcrylic = false;
            BackgroundBlurRadius = 0.5;
            BackgroundTransparency = 0.8;
            UseCustomBackground = false;

            IconTextColor = Colors.White;
            IconTextShadowColor = Colors.Black;
            IconShadowOpacity = 1.0;
        }

        /// <summary>
        /// loads the theme information 
        /// note: The images of the theme are not loaded yet
        /// </summary>
        /// <returns></returns>
        static public Theme LoadTheme()
        {
            Theme theme = null;

            if (!System.IO.Directory.Exists(PortabilityManager.ThemePath))
                return new Theme();

            try
            {
                using (FileStream fs = new FileStream(PortabilityManager.ThemePath + "/theme.xml", FileMode.Open))
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
                if (!System.IO.Directory.Exists(PortabilityManager.ThemePath))
                    System.IO.Directory.CreateDirectory(PortabilityManager.ThemePath);

                using (FileStream fs = new FileStream(PortabilityManager.ThemePath + "/theme.xml", FileMode.Create))
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