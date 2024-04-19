using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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

        BitmapSource LoadThemedImage(string themePath, string defaultAppPath)
        {
            BitmapSource bitmapSource = null;

            //try loading themed image from disk 
            try
            {
                bitmapSource = MiscUtils.LoadBitmapImage(PortabilityManager.ThemePath + themePath);

                if (bitmapSource != null)
                {
                    return bitmapSource;
                }
            }
            catch { }

            if (defaultAppPath == null)
            {
                return null;
            }

            //load default image from resources
            try { return new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component" + defaultAppPath)); }
            catch { }

            return null;
        }

        public void LoadImages(out bool shouldUseVectorFolder)
        {
            CloseBox = LoadThemedImage("/closebox.png", "/res/closebox.png");
            FolderIcon = LoadThemedImage("/folder_icon.png", "/res/folder_icon_transparent.png");
            Background = LoadThemedImage("/bg.png", null);
            BlurredBackground = LoadThemedImage("/blurred_bg.png", null);
            ExtensionsToggle = LoadThemedImage("/extensions.png", "/res/extensions.png");

            //folder
            #region folder images disk
            leftTop = LoadThemedImage("/folder/leftTop.png", "/res/folder/leftTop.png");
            leftCenter = LoadThemedImage("/folder/leftCenter.png", "/res/folder/leftCenter.png");
            leftBottomShadow = LoadThemedImage("/folder/leftBottomShadow.png", "/res/folder/leftBottomShadow.png");
            leftBottomBorder = LoadThemedImage("/folder/leftBottomBorder.png", "/res/folder/leftBottomBorder.png");

            topRim = LoadThemedImage("/folder/topRim.png", "/res/folder/topRim.png");
            center = LoadThemedImage("/folder/center.png", "/res/folder/center.png");
            bottomShadow = LoadThemedImage("/folder/bottomShadow.png", "/res/folder/bottomShadow.png");
            bottomBorder = LoadThemedImage("/folder/bottomBorder.png", "/res/folder/bottomBorder.png");

            arrow = LoadThemedImage("/folder/arrow.png", "/res/folder/arrow.png");

            rightTop = LoadThemedImage("/folder/rightTop.png", "/res/folder/rightTop.png");
            rightCenter = LoadThemedImage("/folder/rightCenter.png", "/res/folder/rightCenter.png");
            rightBottomShadow = LoadThemedImage("/folder/rightBottomShadow.png", "/res/folder/rightBottomShadow.png");
            rightBottomBorder = LoadThemedImage("/folder/rightBottomBorder.png", "/res/folder/rightBottomBorder.png");
            #endregion

            //if theme specifies a center image but no arrow image, use vector folder
            if (File.Exists(PortabilityManager.ThemePath + "/folder/center.png") &&
                !File.Exists(PortabilityManager.ThemePath + "/folder/arrow.png"))
            {
                shouldUseVectorFolder = true;
            }
            else
            {
                shouldUseVectorFolder = false;
            }

            if (Background == null)
                Background = MiscUtils.GetCurrentWallpaper();

            FreezeImages();
        }

        private void FreezeImages()
        {
            List<BitmapSource> images = new List<BitmapSource>{
                CloseBox,
                FolderIcon,
                Background,
                BlurredBackground,
                leftTop,
                leftCenter,
                leftBottomShadow,
                leftBottomBorder,
                topRim,
                center,
                bottomShadow,
                bottomBorder,
                arrow,
                rightTop,
                rightCenter,
                rightBottomShadow,
                rightBottomBorder,
                ExtensionsToggle
            };

            foreach (var image in images)
            {
                if (image != null && image.CanFreeze)
                    image.Freeze();
            }
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