using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        DesktopFileWatcher desktopWatcher;

        private void StartDesktopWatcher()
        {
            StopDesktopWatcher();

            desktopWatcher = new DesktopFileWatcher();
            desktopWatcher.FilesAdded += desktopWatcher_FilesAdded;
        }

        private void StopDesktopWatcher()
        {
            if (desktopWatcher != null)
            {
                desktopWatcher.FilesAdded -= desktopWatcher_FilesAdded;
                desktopWatcher = null;
            }
        }

        private void desktopWatcher_FilesAdded(object sender, EventArgsFilesAdded e)
        {
            SBM.CloseFolderInstant();
            SBM.EndSearch();

            foreach (var file in e.Files)
            {
                AddFile(file);

                if(Settings.CurrentSettings.DeleteDesktopLinksAfterAdding)
                {
                    File.Delete(file);
                }
            }
        }

        private void AddDefaultApps()
        {
            SBM.CloseFolderInstant();
            SBM.EndSearch();

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
                SBItem Folder = new SBItem(Path.GetFileName(directory), "", "", "Folder", null, "", SBItem.FolderIcon);
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

            TriggerSaveItemsDelayed();

            if (Settings.CurrentSettings.SortItemsAlphabetically || Settings.CurrentSettings.SortFolderContentsOnly)
            {
                SortItemsAlphabetically();
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

            if (Settings.CurrentSettings.SortFolderContentsOnly)
            {
                //sort all items in folders
                foreach (var folder in folders)
                {
                    folder.IC.Items.Sort((a, b) =>
                        a.Name.CompareTo(b.Name)
                    );

                    //adjust grid indexes for items
                    int FolderGridIndex = 0;

                    foreach (var item in folder.IC.Items)
                    {
                        item.GridIndex = FolderGridIndex;
                        item.Page = 0;

                        FolderGridIndex++;
                    }

                    //update the folder icons, but hide the text for the active folder
                    folder.UpdateFolderIcon(folder != SBM.ActiveFolder);
                }

                return;
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

            //update page count
            SBM.SP.TotalPages = SBM.GM.GetUsedPages();

            TriggerSaveItemsDelayed();
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

            //update page count
            SBM.SP.TotalPages = SBM.GM.GetUsedPages();

            TriggerSaveItemsDelayed();
        }

        private SBItem PrepareFile(string File)
        {
            try
            {
                BitmapSource bmps;
                string Name;
                string Path;

                if (Uri.IsWellFormedUriString(File, UriKind.Absolute))
                {
                    //link
                    Name = File;
                    Path = File;

                    if (Name == "")
                        return null;

                    bmps = MiscUtils.GetFileThumbnail(File);
                }
                else if ((System.IO.File.GetAttributes(File) & System.IO.FileAttributes.Directory) == System.IO.FileAttributes.Directory)
                {
                    //folder
                    string folder = File;

                    Name = new DirectoryInfo(folder).Name; // folder.Substring(folder.LastIndexOf('\\') + 1);
                    Path = folder;

                    if (Name == "")
                        return null;

                    bmps = MiscUtils.GetFileThumbnail(folder);
                }
                else
                {
                    //file
                    string file = File;
                    string Extension = System.IO.Path.GetExtension(file).ToLower();

                    Name = System.IO.Path.GetFileNameWithoutExtension(File);
                    Path = file;

                    //cache lnk files
                    if (Extension == ".lnk")
                    {
                        string cacheDir = PortabilityManager.LinkCachePath;

                        if (!Directory.Exists(cacheDir))
                        {
                            Directory.CreateDirectory(cacheDir);
                        }

                        string guid = Guid.NewGuid().ToString();
                        string cacheFile = System.IO.Path.Combine(cacheDir, guid + ".lnk");

                        System.IO.File.Copy(file, cacheFile);

                        Path = guid + ".lnk";
                    }

                    if (Name == "")
                        return null;

                    bmps = MiscUtils.GetFileThumbnail(File);
                }

                return new SBItem(Name, "", "", Path, null, "", bmps);
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);

                return null;
            }
        }

        private void AddFile(string File)
        {
            DeactivateSearch();

            var item = PrepareFile(File);

            if (item == null)
                return;

            SBM.AddItem(item, (int)SBM.SP.CurrentPage, -1);
        }
    }
}
