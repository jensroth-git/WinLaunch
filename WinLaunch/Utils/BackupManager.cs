using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WinLaunch
{
    public class BackupEntry
    {
        public string path;
        public DateTime time;
    }

    //backup files manager
    public class BackupManager
    {
        int numBackups = 20;
        string directory;

        public BackupManager(string BackupDirectory, int num)
        {
            numBackups = num;
            directory = BackupDirectory;
        }

        public void AddBackup(string file)
        {
            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!File.Exists(file))
                    return;

                //add timestamp
                //save file
                //cleanup old backups
                string name = Path.GetFileNameWithoutExtension(file);
                string extension = Path.GetExtension(file);
                long time = DateTime.Now.Ticks;

                string backupPath = Path.Combine(directory, name + "." + time + extension);
                File.Copy(file, backupPath);

                //cleanup old backups
                CleanupBackups();
            }
            catch { }
        }

        public void CleanupBackups()
        {
            List<BackupEntry> backups = GetBackups();

            int count = 0;
            foreach (BackupEntry backup in backups)
            {
                count++;

                if (count > numBackups)
                {
                    try
                    {
                        File.Delete(backup.path);
                    }
                    catch { }
                }
            }
        }

        public List<BackupEntry> GetBackups()
        {
            List<BackupEntry> backups = new List<BackupEntry>();

            if (!Directory.Exists(directory))
                return backups;

            List<string> files = new List<string>(Directory.GetFiles(directory, "*.xml"));

            foreach (string file in files)
            {
                //check for valid backups
                FileInfo info = new FileInfo(file);
                if(info.Length == 0)
                {
                    //empty file
                    continue;
                }

                try
                {
                    Match match = Regex.Match(file, ".*\\.(\\d+)\\.xml");

                    if (match.Success)
                    {
                        long time = long.Parse(match.Groups[1].Value);

                        backups.Add(new BackupEntry() { path = file, time = new DateTime(time) });
                    }
                }
                catch { }
            }

            //sort the backups by time
            backups.Sort((x, y) => y.time.CompareTo(x.time));

            return backups;
        }

        public BackupEntry GetLatestBackup()
        {
            List<BackupEntry> backups = GetBackups();
            backups.Sort((x, y) => y.time.CompareTo(x.time));

            if(backups.Count >= 1)
            {
                return backups[0];
            }

            return null;
        }
    }
}
