using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Threading;

namespace WinLaunch
{
    public class EventArgsFilesAdded : EventArgs
    {
        public List<string> Files { get; set; }
    }

    internal class DesktopFileWatcher
    {
        FileSystemWatcher deskWatchUser;
        FileSystemWatcher deskWatchPublic;
        DispatcherTimer dpTime = new DispatcherTimer();

        List<string> files = new List<string>();

        //event for added files
        public event EventHandler<EventArgsFilesAdded> FilesAdded;

        public DesktopFileWatcher()
        {
            dpTime.Interval = TimeSpan.FromMilliseconds(400);
            dpTime.Tick += Dptime_Tick;

            string deskUser = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string deskPublic = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

            //watch user desktop
            deskWatchUser = new FileSystemWatcher();
            deskWatchUser.Path = deskUser;
            deskWatchUser.Filter = "";
            deskWatchUser.NotifyFilter = NotifyFilters.LastWrite;
            deskWatchUser.Changed += FSW_Changed;
            deskWatchUser.EnableRaisingEvents = true;

            if(deskPublic != deskUser)
            {
                //public desktop
                deskWatchPublic = new FileSystemWatcher();
                deskWatchPublic.Path = deskPublic;
                deskWatchPublic.Filter = "";
                deskWatchPublic.NotifyFilter = NotifyFilters.LastWrite;
                deskWatchPublic.Changed += FSW_Changed;
                deskWatchPublic.EnableRaisingEvents = true;
            }
        }

        private void Dptime_Tick(object sender, EventArgs e)
        {
            dpTime.Stop();

            //remove duplicates from files
            files = files.Distinct().ToList();

            //send event
            if (FilesAdded != null)
                FilesAdded(this, new EventArgsFilesAdded() { Files = files });

            files.Clear();
        }

        private void FSW_Changed(object sender, FileSystemEventArgs e)
        {
            if(e.ChangeType == WatcherChangeTypes.Created || e.ChangeType == WatcherChangeTypes.Changed)
            {
                string ext = Path.GetExtension(e.FullPath);

                //check for lnk and url files
                if (ext != ".lnk" && ext != ".url")
                    return;

                dpTime.Stop();
                dpTime.Start();

                files.Add(e.FullPath);
            }            
        }
    }
}
