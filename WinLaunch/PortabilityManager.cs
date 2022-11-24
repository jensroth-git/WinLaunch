using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.XPath;

namespace WinLaunch
{
    public class PortabilityManager
    {
        public static string ItemsPath
        {
            get
            {
                if (IsPortable)
                {
                    return Path.Combine(PortableDirectory, "Items.xml");
                }

                return System.IO.Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch", "Items.xml" });
            }
        }

        public static string SettingsPath
        {
            get
            {
                if (IsPortable)
                {
                    return Path.Combine(PortableDirectory, "Settings.xml");
                }

                return System.IO.Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch", "Settings.xml" });
            }
        }

        public static string ThemePath
        {
            get
            {
                if (IsPortable)
                {
                    return Path.Combine(PortableDirectory, "CurrentTheme");
                }

                return System.IO.Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch", "CurrentTheme" });
            }
        }

        public static string LinkCachePath
        {
            get
            {
                if (IsPortable)
                {
                    return Path.Combine(PortableDirectory, "LinkCache");
                }

                return System.IO.Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch", "LinkCache" });
            }
        }

        public static string IconCachePath
        {
            get
            {
                if (IsPortable)
                {
                    return Path.Combine(PortableDirectory, "IconCache");
                }

                return System.IO.Path.Combine(new string[] { Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch", "IconCache" });
            }
        }

        public static bool IsPortable = false;
        public static string PortableDirectory = "Data";

        public static void Init()
        {
            IsPortable = Directory.Exists(PortableDirectory);
        }

        public static void MakeInstalled()
        {
            IsPortable = false;

            //copy all files from appdata to data
            if (!MiscUtils.CopyDirectoryOverwrite(
                PortableDirectory,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch"),
                true))
            {
                MessageBox.Show("error while copying files from " + Path.GetFullPath(PortableDirectory));
                IsPortable = true;
                return;
            }

            try
            {
                Directory.Delete(PortableDirectory, true);
            }
            catch(Exception ex)
            {
                MessageBox.Show("Error while removing portable directory " + Path.GetFullPath(PortableDirectory) + " please delete it manually");
            }
        }

        public static void MakePortable(ItemCollection Items)
        {
            IsPortable = true;

            //copy all files from appdata to data
            if (!MiscUtils.CopyDirectoryOverwrite(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch"),
                PortableDirectory,
                true))
            {
                MessageBox.Show("error while copying files from " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WinLaunch"));
                IsPortable = false;
                return;
            }

            try
            {
                //make the icon paths relative
                //make the lnk paths relative 
                foreach (var item in Items.Items)
                {
                    MoveToIconCache(item);
                    MoveToLnkCache(item);

                    if (item.IsFolder)
                    {
                        foreach (var subItem in item.IC.Items)
                        {
                            MoveToIconCache(subItem);
                            MoveToLnkCache(subItem);
                        }
                    }
                }

                //save the new items.xml
                Items.SaveToXML(ItemsPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("error while adjusting the items " + ex.Message);
            }
        }

        private static void MoveToIconCache(SBItem item)
        {
            if (item.IconPath != null)
            {
                if(!ItemCollection.IsIconInCache(item.IconPath))
                {
                    //not a cached icon
                    //copy file over to the new cache
                    string extension = Path.GetExtension(item.IconPath);
                    string guid = Guid.NewGuid().ToString();

                    string newIconPath = guid + extension;

                    File.Copy(item.IconPath, Path.Combine(PortabilityManager.IconCachePath, newIconPath));

                    item.IconPath = newIconPath;
                    return;
                }
            }
        }

        private static void MoveToLnkCache(SBItem item)
        {
            if (Path.GetExtension(item.ApplicationPath) == ".lnk")
            {
                if (!ItemCollection.IsLnkInCache(item.ApplicationPath))
                {
                    //not a relative lnk
                    item.ApplicationPath = Path.GetFileName(item.ApplicationPath);

                    return;
                }
            }
        }
    }
}
