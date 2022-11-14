using System;
using System.IO;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;

namespace WinLaunch
{
    public sealed class Settings
    {
        [System.Xml.Serialization.XmlIgnore]
        public static Settings CurrentSettings = null;

        [System.Xml.Serialization.XmlIgnore]
        public Version version = null;

        public bool CheckForUpdatesFrequently { get; set; }

        public bool SortItemsAlphabetically { get; set; }
        public bool SortFoldersFirst { get; set; }

        /// <summary>
        /// version information used to determine if the version changed (updates)
        /// </summary>
        public string Versionstring = "0.0.0.0";

        #region Grid Settings
        public int Columns { get; set; }
        public int Rows { get; set; }

        public int FolderColumns { get; set; }
        public int FolderRows { get; set; }
        #endregion

        public double IconSize { get; set; }

        public bool ExtensionIconVisible { get; set; }
        public bool SearchBarVisible { get; set; }
        public bool PageIndicatorsVisible { get; set; }

        /// <summary>
        /// When enabled will only allow movement of items while in wiggle mode (after holding an item for 2s)
        /// </summary>
        public bool TabletMode { get; set; }

        /// <summary>
        /// When enabled WinLaunch will fill the entire screen instead of only the WorkArea
        /// </summary>
        public bool FillScreen { get; set; }

        /// <summary>
        /// Allow Items to be placed freely
        /// </summary>
        public bool FreeItemPlacement { get; set; }

        /// <summary>
        /// When enabled WinLaunch will be placed as a child window on the desktop
        /// disabled all activation methods
        /// </summary>
        public bool DeskMode { get; set; }
        public bool LegacyDeskMode { get; set; }

        /// <summary>
        /// specifies the selected screen index
        /// </summary>
        public int ScreenIndex { get; set; }

        /// <summary>
        /// When enabled will regularly check for updates
        /// </summary>
        public bool CheckForUpdates { get; set; }

        /// <summary>
        /// specifies the current language 
        /// </summary>
        public string SelectedLanguage { get; set; }

        /// <summary>
        /// When enabled will block WinLaunch if fullscreen apps are detected
        /// </summary>
        public bool BlockOnFullscreen { get; set; }

        /// <summary>
        /// when enabled will ignore multi screen selection 
        /// and open WinLaunch on the screen the moouse is located
        /// </summary>
        public bool OpenOnActiveDesktop { get; set; }


        /// <summary>
        /// HotKey settings
        /// </summary>
        public bool HotKeyEnabled { get; set; }
        public bool HotAlt { get; set; }
        public bool HotControl { get; set; }
        public bool HotShift { get; set; }
        public bool HotWin { get; set; }

        public Key HotKeyExtend { get; set; }


        /// <summary>
        /// HotCorner settings
        /// </summary>
        public bool HotCornersEnabled { get; set; }
        public bool HotTopLeft { get; set; }
        public bool HotTopRight { get; set; }
        public bool HotBottomRight { get; set; }
        public bool HotBottomLeft { get; set; }

        public double HotCornerDelay { get; set; }

        /// <summary>
        /// Windows Key Activation
        /// </summary>
        public bool WindowsKeyActivationEnabled { get; set; }

        public bool DoubleTapCtrlActivationEnabled { get; set; }
        public bool DoubleTapAltActivationEnabled { get; set; }

        /// <summary>
        /// Middle mouse button settings
        /// </summary>
        public MiddleMouseButtonAction MiddleMouseActivation { get; set; }

        public bool GamepadActivation { get; set; }

        /// <summary>
        /// Construct a new Settings object
        /// </summary>
        public Settings()
        {
            try
            {
                version = new Version(this.Versionstring);
            }
            catch
            {
                version = new Version("0.0.0.0");
            }

            SortItemsAlphabetically = false;
            SortFoldersFirst = false;

            CheckForUpdatesFrequently = true;
            CheckForUpdates = true;

            IconSize = 1.4;
            ExtensionIconVisible = true;
            SearchBarVisible = true;
            PageIndicatorsVisible = true;

            //set default settings
            Columns = 8;
            Rows = 5;

            FolderColumns = 8;
            FolderRows = 5;

            TabletMode = false;
            FillScreen = false;
            FreeItemPlacement = false;
            DeskMode = false;
            LegacyDeskMode = false;
            ScreenIndex = 0;
            
            SelectedLanguage = "en-US";
            BlockOnFullscreen = true;
            OpenOnActiveDesktop = true;

            HotKeyEnabled = true;
            HotShift = true;
            HotKeyExtend = Key.Tab;

            HotCornersEnabled = true;
            HotTopLeft = true;
            HotCornerDelay = 0.0;

            WindowsKeyActivationEnabled = false;
            DoubleTapCtrlActivationEnabled = false;
            DoubleTapAltActivationEnabled = false;

            MiddleMouseActivation = MiddleMouseButtonAction.DoubleClicked;

            GamepadActivation = false;
        }

        /// <summary>
        /// Helper Methods used to load and save the settings
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        #region Save/Load
        static public Settings LoadSettings(string xml)
        {
            try
            {
                Settings config;

                //clean access pattern
                using (FileStream fs = new FileStream(xml, FileMode.Open))
                {
                    using (XmlReader read = XmlReader.Create(fs))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(Settings));
                        config = (Settings)ser.Deserialize(read);
                    }
                }

                try
                {
                    //initialize additional stuff
                    config.version = new Version(config.Versionstring);
                }
                catch
                {
                    config.version = new Version("0.0.0.0");
                }

                return config;
            }
            catch { }

            return new Settings();
        }

        static public bool SaveSettings(string xml, Settings config)
        {
            try
            {
                try
                {
                    config.Versionstring = config.version.ToString();
                }
                catch { }

                using (FileStream fs = new FileStream(xml, FileMode.Create))
                {
                    using (XmlWriter write = XmlWriter.Create(fs))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(Settings));
                        ser.Serialize(write, config);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
            return false;
        }
        #endregion Save/Load
    }
}