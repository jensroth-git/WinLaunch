using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                foreach (var item in Items.Items)
                {
                    ReplaceIcon(item);

                    if (item.IsFolder)
                    {
                        foreach (var subItem in item.IC.Items)
                        {
                            ReplaceIcon(subItem);
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

        private static void ReplaceIcon(SBItem item)
        {
            if (item.IconPath != null)
            {
                string dirName = new DirectoryInfo(Path.GetDirectoryName(item.IconPath)).Name;

                if (dirName != "IconCache")
                {
                    //copy file over to the new cache
                    string extension = Path.GetExtension(item.IconPath);
                    string guid = Guid.NewGuid().ToString();

                    string iconPath = Path.Combine(PortabilityManager.IconCachePath, guid + extension);

                    File.Copy(item.IconPath, iconPath);

                    item.IconPath = iconPath;
                    return;
                }

                string filename = Path.GetFileName(item.IconPath);
                string newPath = Path.Combine(IconCachePath, filename);

                item.IconPath = newPath;
            }
        }
    }
}
