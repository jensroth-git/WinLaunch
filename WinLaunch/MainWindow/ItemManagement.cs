using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        private void AddDefaultApps()
        {
            SBM.CloseFolderInstant();

            string startMenuItems = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs";
            string additionalStartMenuItems = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\Start Menu\\Programs");

            List<string> files = new List<string>();

            if (Directory.Exists(startMenuItems))
                files.AddRange(Directory.GetFiles(startMenuItems));

            if (Directory.Exists(additionalStartMenuItems))
                files.AddRange(Directory.GetFiles(additionalStartMenuItems));


            List<string> filesToAdd = new List<string>();
            List<string> dirsToAdd = new List<string>();

            foreach (var file in files)
            {
                if (file.EndsWith(".lnk"))
                {
                    if (SBM.GetItemsByName(Path.GetFileNameWithoutExtension(file)).Count == 0)
                        filesToAdd.Add(file);
                }
            }

            List<string> subDirectories = new List<string>();

            if (Directory.Exists(startMenuItems))
                subDirectories.AddRange(Directory.GetDirectories(startMenuItems));

            if (Directory.Exists(additionalStartMenuItems))
                subDirectories.AddRange(Directory.GetDirectories(additionalStartMenuItems));

            foreach (var directory in subDirectories)
            {
                var directoryFiles = Directory.GetFiles(directory);

                //check if there are at least 2 links in there 
                int numLnk = 0;
                foreach (var file in directoryFiles)
                {
                    if (file.EndsWith(".lnk"))
                    {
                        if (SBM.GetItemsByName(Path.GetFileNameWithoutExtension(file)).Count == 0)
                            numLnk++;
                    }
                }

                if (numLnk >= 2)
                {
                    dirsToAdd.Add(directory);
                }
                else
                {
                    //not enough items for a folder, add single item instead
                    foreach (var file in directoryFiles)
                    {
                        if (file.EndsWith(".lnk"))
                        {
                            if (SBM.GetItemsByName(Path.GetFileNameWithoutExtension(file)).Count == 0)
                                filesToAdd.Add(file);
                        }
                    }
                }
            }

            //exclude entries that exist already 

            //sort files and directories 
            filesToAdd.Sort((a, b) =>
                Path.GetFileNameWithoutExtension(a).CompareTo(Path.GetFileNameWithoutExtension(b))
            );

            dirsToAdd.Sort();

            foreach (var file in filesToAdd)
            {
                AddFile(file);
            }

            foreach (var directory in dirsToAdd)
            {
                var directoryFiles = Directory.GetFiles(directory);

                //create a folder and add all items
                SBItem Folder = new SBItem(Path.GetFileName(directory), "Folder", null, "", SBItem.FolderIcon);
                Folder.IsFolder = true;

                int GridIndex = 0;
                foreach (var file in directoryFiles)
                {
                    if (file.EndsWith(".lnk"))
                    {
                        var item = PrepareFile(file);

                        item.Page = 0;
                        item.GridIndex = GridIndex;

                        Folder.IC.Items.Add(item);

                        GridIndex++;
                    }
                }

                SBM.AddItem(Folder);
                Folder.UpdateFolderIcon(true);
            }

            if (Settings.CurrentSettings.SortItemsAlphabetically)
            {
                SortItemsAlphabetically();
            }
            else
            {
                PerformItemBackup();
            }
        }

        public void SortItemsAlphabetically()
        {
            var items = new List<SBItem>();
            var folders = new List<SBItem>();

            foreach (var item in SBM.IC.Items)
            {
                if (item.IsFolder)
                {
                    folders.Add(item);
                }
                else
                {
                    items.Add(item);
                }
            }

            //sort all items alphabetically
            items.Sort((a, b) =>
                a.Name.CompareTo(b.Name)
            );

            folders.Sort((a, b) =>
                a.Name.CompareTo(b.Name)
            );

            //sort all items in folders
            foreach (var folder in folders)
            {
                folder.IC.Items.Sort((a, b) =>
                    a.Name.CompareTo(b.Name)
                );
            }

            //adjust grid indexes for items
            int ItemsPerPage = SBM.GM.XItems * SBM.GM.YItems;
            int GridIndex = 0;
            int Page = 0;

            if (Settings.CurrentSettings.SortFoldersFirst)
            {
                InsertFolders(folders, ItemsPerPage, ref GridIndex, ref Page);
            }

            foreach (var item in items)
            {
                item.GridIndex = GridIndex;
                item.Page = Page;

                GridIndex++;

                if (GridIndex == ItemsPerPage)
                {
                    GridIndex = 0;
                    Page++;
                }
            }

            if (!Settings.CurrentSettings.SortFoldersFirst)
            {
                InsertFolders(folders, ItemsPerPage, ref GridIndex, ref Page);
            }

            PerformItemBackup();
        }

        private void InsertFolders(List<SBItem> folders, int ItemsPerPage, ref int GridIndex, ref int Page)
        {
            //adjust grid indexes for folders
            foreach (var folder in folders)
            {
                folder.GridIndex = GridIndex;
                folder.Page = Page;

                //position the items in the folder
                int subGridIndex = 0;

                foreach (var subItem in folder.IC.Items)
                {
                    subItem.GridIndex = subGridIndex;
                    subItem.Page = 0;
                    subGridIndex++;
                }

                //update the folder icons, but hide the text for the active folder
                folder.UpdateFolderIcon(folder != SBM.ActiveFolder);

                GridIndex++;

                if (GridIndex == ItemsPerPage)
                {
                    GridIndex = 0;
                    Page++;
                }
            }
        }

        public void ClearAllItems()
        {
            SBM.CloseFolderInstant();

            //clear all items 
            foreach (var item in SBM.IC.Items)
            {
                SBM.container.Remove(item.ContentRef);
            }

            SBM.IC.Items.Clear();

            PerformItemBackup();
        }
    }
}
