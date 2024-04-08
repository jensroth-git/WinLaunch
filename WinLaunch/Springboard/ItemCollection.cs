using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using KTrie;

namespace WinLaunch
{
    public class ItemCollection
    {
        public List<SBItem> Items;

        public ItemCollection()
        {
            Items = new List<SBItem>();
        }

        public void SaveToXML(string path)
        {
            try
            {
                GC.Collect();

                List<ICItem> ICItems = new List<ICItem>();
                TranslateToSerializableFormat(ICItems);

                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    using (XmlWriter write = XmlWriter.Create(fs))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(List<ICItem>));
                        ser.Serialize(write, ICItems);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public bool LoadFromXML(string path)
        {
            if (!System.IO.File.Exists(path))
                return true;

            try
            {
                //clean access pattern
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    using (XmlReader read = XmlReader.Create(fs))
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(List<ICItem>));
                        ICItems = (List<ICItem>)ser.Deserialize(read);
                    }
                }

                DeserializeToItems(ICItems);

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #region Image Loading

        public static bool IsInCache(string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            Guid id;

            if (!Guid.TryParse(filename, out id))
            {
                return false;
            }

            if (string.IsNullOrEmpty(Path.GetDirectoryName(path)))
            {
                return true;
            }

            return false;
        }

        private void LoadIconInBackground(SBItem item, Dispatcher disp)
        {
            if (item.IsFolder)
            {
                //Load folder icon
                if (item.IconPath != null)
                {
                    BitmapSource bmps = null;

                    try
                    {
                        string fullPath = "";

                        if (IsInCache(item.IconPath))
                        {
                            fullPath = Path.Combine(PortabilityManager.IconCachePath, item.IconPath);
                        }
                        else
                        {
                            //legacy support
                            fullPath = item.IconPath;
                        }

                        try
                        {
                            bmps = MiscUtils.LoadBitmapImage(fullPath, 128);
                        }
                        catch { }

                        if (bmps == null)
                        {
                            throw new Exception("error loading image");
                        }

                        disp.BeginInvoke(new Action(() =>
                        {
                            item.Icon = bmps;
                        }));
                    }
                    catch { }
                }
            }
            else
            {
                string path = item.ApplicationPath;

                if (Path.GetExtension(path).ToLower() == ".lnk" && IsInCache(item.ApplicationPath))
                {
                    path = Path.Combine(PortabilityManager.LinkCachePath, path);
                }

                //load file icon
                //only try to load icon if item exists otherwise crash / hang
                if (File.Exists(path) || Directory.Exists(path) || Uri.IsWellFormedUriString(path, UriKind.Absolute))
                {
                    BitmapSource bmps = null;

                    //if no extra icon specified load from original application
                    if (item.IconPath != null)
                    {
                        try
                        {
                            string fullPath = "";

                            if (IsInCache(item.IconPath))
                            {
                                fullPath = Path.Combine(PortabilityManager.IconCachePath, item.IconPath);
                            }
                            else
                            {
                                //legacy support
                                fullPath = item.IconPath;
                            }

                            try
                            {
                                //load bitmap and resize it to 128px width
                                bmps = MiscUtils.LoadBitmapImage(fullPath, 128);
                            }
                            catch { }

                            if (bmps == null)
                            {
                                throw new Exception("error loading image");
                            }
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                bmps = MiscUtils.GetFileThumbnail(path);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        //get icon from original app 
                        //var appPath = MiscUtils.GetShortcutTargetFile(path);

                        //if(appPath != null)
                        //{
                        //    path = appPath;
                        //}

                        try
                        {
                            bmps = MiscUtils.GetFileThumbnail(path);
                        }
                        catch { }
                    }

                    if (bmps != null)
                    {
                        disp.BeginInvoke(new Action(() =>
                        {
                            item.Icon = bmps;
                        }));
                    }
                }
            }
        }

        public void LoadIconsInBackground(Dispatcher disp, Action continueWith)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //Load all items
                    foreach (SBItem item in Items)
                    {
                        if (item.IsFolder)
                        {
                            //load items in the folder
                            foreach (SBItem folderitem in item.IC.Items)
                            {
                                LoadIconInBackground(folderitem, disp);
                            }
                        }

                        LoadIconInBackground(item, disp);
                    }

                    //rerender all folders
                    disp.BeginInvoke(new Action(() =>
                    {
                        foreach (SBItem item in Items)
                        {
                            if (item.IsFolder)
                            {
                                item.UpdateFolderIcon();
                            }
                        }
                    }));
                }
                catch (Exception ex)
                {
                    CrashReporter.Report(ex);
                    MessageBox.Show("Could not load items" + ex.Message, "Winlaunch Error");
                }
            }).ContinueWith(t =>
            {
                continueWith();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }


        #endregion Image Loading

        //serializeable format
        public List<ICItem> ICItems = null;

        private void TranslateToSerializableFormat(List<ICItem> ItemList)
        {
            if (Items.Count == 0)
                return;

            foreach (SBItem item in Items)
            {
                ICItem NewItem = new ICItem(item.GridIndex, item.Page, item.ApplicationPath, item.IconPath, item.Name, item.Keywords, item.Notes, item.Arguments, item.RunAsAdmin, item.IsFolder, item.ShowMiniatures);
                ItemList.Add(NewItem);

                if (item.IsFolder)
                {
                    item.IC.TranslateToSerializableFormat(NewItem.Items);
                }
            }
        }

        private void DeserializeToItems(List<ICItem> ItemList)
        {
            if (ItemList.Count == 0)
                return;

            foreach (ICItem item in ItemList)
            {
                try
                {
                    //create SBItem from ICItem
                    SBItem SBitem;
                    if (item.IsFolder)
                    {
                        SBitem = new SBItem(item.Name, item.Keywords, item.Notes, item.Application, item.IconPath, item.Arguments, SBItem.FolderIcon);
                        SBitem.IsFolder = item.IsFolder;
                        SBitem.ShowMiniatures = item.ShowMiniatures;
                    }
                    else
                    {
                        //use Loading image first
                        BitmapSource bmps = SBItem.LoadingImage;
                        SBitem = new SBItem(item.Name, item.Keywords, item.Notes, item.Application, item.IconPath, item.Arguments, bmps);
                        SBitem.RunAsAdmin = item.RunAsAdmin;
                    }

                    SBitem.GridIndex = item.GridIndex;
                    SBitem.Page = item.Page;

                    //add SBitem to item cache
                    Items.Add(SBitem);

                    if (item.IsFolder)
                    {
                        if (item.Items.Count != 0)
                        {
                            SBitem.IC.DeserializeToItems(item.Items);
                        }

                        SBitem.UpdateFolderIcon(true);
                    }
                }
                catch { }
            }
        }

        public AssistantItem ConstructAssistantItem(SBItem item)
        {
            string type;

            if (Uri.IsWellFormedUriString(item.ApplicationPath, UriKind.Absolute))
            {
                type = "weblink";
            }
            else
            {
                type = Path.GetExtension(item.ApplicationPath);
            }

            return new AssistantItem() { Name = item.Name, Type = type, Keywords = item.Keywords, Notes = item.Notes };
        }

        public List<AssistantItem> BuildAssistantItems()
        {
            List<AssistantItem> items = new List<AssistantItem>();

            foreach (var item in Items)
            {
                if (item.IsFolder)
                {
                    AssistantItem folder = new AssistantItem();
                    folder.Name = item.Name;
                    folder.Type = "folder";
                    folder.Notes = item.Notes;
                    folder.Items = new List<AssistantItem>();

                    foreach (var subItem in item.IC.Items)
                    {
                        folder.Items.Add(ConstructAssistantItem(subItem));
                    }

                    items.Add(folder);
                }
                else
                {
                    items.Add(ConstructAssistantItem(item));
                }
            }

            return items;
        }

        List<string> GetItemGrammarTokens(SBItem item)
        {
            List<string> tokens = new List<string>();

            tokens.AddRange(item.Name.Split(' '));
            tokens.AddRange(item.Keywords.Split(' '));

            return tokens;
        }

        public string[] BuildItemGrammar()
        {
            List<string> grammar = new List<string>();

            foreach (var item in Items)
            {
                if (item.IsFolder)
                {
                    foreach (var subItem in item.IC.Items)
                    {
                        grammar.AddRange(GetItemGrammarTokens(subItem));
                    }
                }
                else
                {
                    grammar.AddRange(GetItemGrammarTokens(item));
                }
            }

            for (int i = 0; i < grammar.Count; i++)
            {
                grammar[i] = grammar[i].ToLower();
            }

            var uniqueGrammar = new HashSet<string>(grammar.ToArray());
            uniqueGrammar.Remove(" ");
            uniqueGrammar.Remove("");

            string[] finalGrammar = new string[uniqueGrammar.Count];
            uniqueGrammar.CopyTo(finalGrammar);

            return finalGrammar;
        }

        //builds a trie for fast searching
        public StringTrie<SBItem> BuildItemTrie()
        {
            StringTrie<SBItem> list = new StringTrie<SBItem>();

            foreach (var item in Items)
            {
                if (item.IsFolder)
                {
                    foreach (var subItem in item.IC.Items)
                    {
                        try
                        {
                            list.Add(subItem.Name, subItem);
                        }
                        catch { }
                    }
                }
                else
                {
                    try
                    {
                        list.Add(item.Name, item);
                    }
                    catch { }
                }
            }

            return list;
        }
    }

    //helper class used to serialize items to xml
    public class ICItem
    {
        public List<ICItem> Items = null;

        public bool IsFolder = false;

        public int GridIndex = 0;
        public int Page = 0;

        public string Name = "";
        public string Keywords = "";
        public string Notes = "";
        public string Application = "";
        public string Arguments = "";
        public bool RunAsAdmin = false;
        public string IconPath = null;
        public bool ShowMiniatures = true;

        public ICItem()
        {
            Items = new List<ICItem>();
        }

        public ICItem(int GridIndex, int Page, string App, string IconPath, string Name, string Keywords, string Notes, string Arguments, bool RunAsAdmin, bool IsFolder, bool showMiniatures)
        {
            Items = new List<ICItem>();
            this.GridIndex = GridIndex;
            this.Page = Page;
            this.Application = App;
            this.IconPath = IconPath;
            this.Name = Name;
            this.Keywords = Keywords;
            this.Notes = Notes;
            this.Arguments = Arguments;
            this.RunAsAdmin = RunAsAdmin;
            this.IsFolder = IsFolder;
            this.ShowMiniatures = showMiniatures;
        }
    }
}