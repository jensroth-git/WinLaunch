using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    /// <summary>
    /// Interaktionslogik für RenameItem.xaml
    /// </summary>
    partial class EditItem : Window
    {
        #region Properties

        private double PreferredPointerOffset = 110;

        private SBItem ActiveItem;
        private MainWindow ClientWindow;

        #endregion Properties

        #region item properties backup

        private BitmapSource IconBackup;
        private string ApplicationPathBackup;
        private string NameBackup;
        private string KeywordsBackup;
        private string NotesBackup;
        private string IconPathBackup;
        private string ArgumentsBackup;
        private bool RunAsAdminBackup;
        private bool ShowMiniaturesBackup;

        private void PerformBackup(SBItem Item)
        {
            IconBackup = Item.Icon;
            ApplicationPathBackup = Item.ApplicationPath;
            NameBackup = Item.Name;
            KeywordsBackup = Item.Keywords;
            NotesBackup = Item.Notes;
            IconPathBackup = Item.IconPath;
            ArgumentsBackup = Item.Arguments;
            RunAsAdminBackup = Item.RunAsAdmin;
            ShowMiniaturesBackup = Item.ShowMiniatures;
        }

        private void RestoreBackup(SBItem Item)
        {
            Item.Icon = IconBackup;
            Item.ApplicationPath = ApplicationPathBackup;
            Item.Name = NameBackup;
            Item.Keywords = KeywordsBackup;
            Item.Notes = NotesBackup;
            Item.IconPath = IconPathBackup;
            Item.Arguments = ArgumentsBackup;
            Item.RunAsAdmin = RunAsAdminBackup;
            Item.ShowMiniatures = ShowMiniaturesBackup;

            Item.UpdateIcon();
        }

        #endregion item properties backup

        private void PositionWindowAtItem(SBItem Item, MainWindow hWnd)
        {
            //Get local item position
            Point ItemOnScreen = Item.GetPosition();
            ItemOnScreen.X += Item.GetOffset().X;
            ItemOnScreen.X += Item.ContentRef.ActualWidth / 2.0;
            ItemOnScreen.Y += Item.ContentRef.ActualHeight / 2.0;

            double scale = hWnd.CanvasScale.ScaleX;
            ItemOnScreen.X *= scale;
            ItemOnScreen.Y *= scale;

            if (hWnd.WindowStyle != System.Windows.WindowStyle.None)
            {
                //add window borders
                //TODO: style independent
                ItemOnScreen.X += 2.0;
                ItemOnScreen.Y += 20.0;
            }

            ItemOnScreen.X += hWnd.Left;
            ItemOnScreen.Y += hWnd.Top;

            //Initialize pointer
            SetPointerPosition(PreferredPointerOffset, true);

            System.Drawing.Rectangle s;
            int screenIndex = MiscUtils.GetScreenIndexFromPoint(new System.Drawing.Point((int)ItemOnScreen.X, (int)ItemOnScreen.Y));

            if (screenIndex == -1)
            {
                //center on active screen
                //center window and hide pointer
                SetPointerPosition(PreferredPointerOffset, false);
                MiscUtils.CenterInScreen(this, MiscUtils.GetActiveScreenIndex());

                return;
            }
            else
            {
                //check if we are in bounds
                System.Drawing.Rectangle sDPI = System.Windows.Forms.Screen.AllScreens[screenIndex].Bounds;

                s = new System.Drawing.Rectangle((int)(sDPI.Left / MiscUtils.GetDPIScale()), (int)(sDPI.Top / MiscUtils.GetDPIScale()), (int)(sDPI.Width / MiscUtils.GetDPIScale()), (int)(sDPI.Height / MiscUtils.GetDPIScale()));

                if (!s.Contains((int)ItemOnScreen.X, (int)ItemOnScreen.Y))
                {
                    //center on active screen
                    //center window and hide pointer
                    SetPointerPosition(PreferredPointerOffset, false);
                    MiscUtils.CenterInScreen(this, MiscUtils.GetActiveScreenIndex());

                    return;
                }

                //we are visible
            }

            double XBorder = 59;
            double YBorder = 12;

            Point FinalPosition = new Point(ItemOnScreen.X - XBorder, ItemOnScreen.Y - YBorder);

            //try to position it at pointer
            FinalPosition.X -= PreferredPointerOffset;

            if (FinalPosition.X + XBorder < s.Left)
            {
                //offscreen left
                //move pointer instead
                FinalPosition.X = s.Left - XBorder;
                SetPointerPosition(ItemOnScreen.X, true);
            }
            else if (FinalPosition.X + MainGrid.Width - XBorder > s.Right)
            {
                //offscreen right
                //move pointer instead
                FinalPosition.X = s.Right - MainGrid.Width + XBorder;
                double PointerOffset = ItemOnScreen.X - FinalPosition.X - XBorder;
                SetPointerPosition(PointerOffset, true);
            }

            //Correct Y
            if (FinalPosition.Y + MainGrid.Height - YBorder > s.Bottom)
            {
                FinalPosition.Y = ItemOnScreen.Y - MainGrid.Height + YBorder;
                double PointerOffset = ItemOnScreen.X - FinalPosition.X - XBorder;
                SetPointerPosition(PointerOffset, false, true);
            }

            //set position
            this.Left = FinalPosition.X;
            this.Top = FinalPosition.Y;
        }

        private void SetPointerPosition(double Xoffset, bool ShowTopPointer = true, bool ShowBottomPointer = false)
        {
            if (Xoffset < 20)
                Xoffset = 20;

            if (Xoffset > 462)
                Xoffset = 462;

            PointerLeft.Point = new Point(Xoffset - 10.0, PointerLeft.Point.Y);
            PointerCenter.Point = new Point(Xoffset, (ShowTopPointer ? PointerLeft.Point.Y - 10.0 : PointerLeft.Point.Y));
            PointerRight.Point = new Point(Xoffset + 10.0, PointerRight.Point.Y);

            PointerRightBottom.Point = new Point(Xoffset + 10.0, PointerRightBottom.Point.Y);
            PointerCenterBottom.Point = new Point(Xoffset, (ShowBottomPointer ? PointerLeftBottom.Point.Y + 10.0 : PointerLeftBottom.Point.Y));
            PointerLeftBottom.Point = new Point(Xoffset - 10.0, PointerLeftBottom.Point.Y);
        }

        public EditItem(MainWindow MainWindow, SBItem Item)
        {
            InitializeComponent();
            this.KeyDown += new KeyEventHandler(EditItem_KeyDown);
            this.Loaded += new RoutedEventHandler(EditItem_Loaded);

            if (Item.IsFolder)
            {
                PathGrid.IsEnabled = false;
                ArgumentsGrid.IsEnabled = false;
                this.cbAdmin.IsEnabled = false;
                this.cbAdmin.Visibility = Visibility.Collapsed;
                this.cbShowMiniatures.Visibility = Visibility.Visible;
                this.gdKeywords.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.cbShowMiniatures.Visibility = Visibility.Collapsed;
            }

            this.NameBox.Text = Item.Name;
            this.KeywordsBox.Text = Item.Keywords;
            this.AssistantNotesBox.Text = Item.Notes;

            string filepath = Item.ApplicationPath;

            if (filepath.EndsWith(".lnk"))
            {
                if (ItemCollection.IsInCache(filepath))
                {
                    filepath = Path.Combine(PortabilityManager.LinkCachePath, filepath);
                    filepath = Path.GetFullPath(filepath);
                }

                //get actual path if its a link
                filepath = MiscUtils.GetShortcutTargetFile(filepath);

                if (string.IsNullOrEmpty(filepath))
                {
                    filepath = Item.ApplicationPath;
                }
            }

            this.PathBox.Text = filepath;
            this.ArgumentsBox.Text = Item.Arguments;
            this.cbAdmin.IsChecked = Item.RunAsAdmin;
            this.cbShowMiniatures.IsChecked = Item.ShowMiniatures;

            this.ActiveItem = Item;
            this.ClientWindow = MainWindow;

            //Set Icon Preview
            IconFrame.Source = this.ActiveItem.Icon;

            //backup settings
            PerformBackup(Item);
        }

        private void EditItem_Loaded(object sender, RoutedEventArgs e)
        {
            PositionWindowAtItem(this.ActiveItem, this.ClientWindow);
            (Resources["WindowOpenAnimation"] as Storyboard).Begin();
        }

        private void EditItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                RestoreBackup(this.ActiveItem);
                this.Close();
            }
        }

        private void cbShowMiniatures_Checked(object sender, RoutedEventArgs e)
        {
            if (this.ActiveItem != null)
            {
                this.ActiveItem.ShowMiniatures = (bool)cbShowMiniatures.IsChecked;
                this.ActiveItem.UpdateFolderIcon();
            }
        }

        private void ConfirmClicked(object sender, RoutedEventArgs e)
        {
            this.ActiveItem.Name = NameBox.Text;
            this.ActiveItem.Notes = AssistantNotesBox.Text;

            if (this.ActiveItem.IsFolder)
            {
                this.ActiveItem.ShowMiniatures = (bool)cbShowMiniatures.IsChecked;
            }
            else
            {
                this.ActiveItem.Keywords = KeywordsBox.Text;

                this.ActiveItem.ApplicationPath = PathBox.Text;
                this.ActiveItem.Arguments = ArgumentsBox.Text;
                this.ActiveItem.RunAsAdmin = (bool)cbAdmin.IsChecked;
            }


            this.ActiveItem.UpdateIcon();

            if (this.ActiveItem.IsFolder)
            {
                this.ActiveItem.UpdateFolderIcon();
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            RestoreBackup(this.ActiveItem);

            this.DialogResult = false;
            this.Close();
        }

        private void ResetIconButton_Click(object sender, RoutedEventArgs e)
        {
            //restore icon path
            this.ActiveItem.IconPath = null;

            if (this.ActiveItem.IsFolder)
            {
                this.ActiveItem.Icon = SBItem.FolderIcon;

                if (this.ActiveItem.IsFolder)
                {
                    this.ActiveItem.UpdateFolderIcon();
                }
            }
            else
            {
                string path = this.ActiveItem.ApplicationPath;

                if (Path.GetExtension(path).ToLower() == ".lnk" && ItemCollection.IsInCache(path))
                {
                    path = Path.Combine(PortabilityManager.LinkCachePath, path);
                }

                this.ActiveItem.Icon = SBItem.LoadingImage;

                try
                {
                    this.ActiveItem.Icon = MiscUtils.GetFileThumbnail(path);
                }
                catch { }
            }

            IconFrame.Source = this.ActiveItem.Icon;
        }

        private void IconFrame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                //Load new Icon
                OpenFileDialog op = new OpenFileDialog();
                op.Title = TranslationSource.Instance["SelectIcon"];
                op.Filter = "Image files (*.png)|*.png|Image files (*.jpg)|*.jpg|All Files (*.*)|*.*";
                if (op.ShowDialog() == true)
                {
                    this.ActiveItem.Icon = SBItem.LoadingImage;

                    //try to load the replacement icon
                    try
                    {
                        this.ActiveItem.Icon = MiscUtils.LoadBitmapImage(op.FileName, 128);

                        //save icon to cache
                        string extension = Path.GetExtension(op.FileName);
                        string guid = Guid.NewGuid().ToString();

                        string iconPath = guid + extension;

                        //Set as IconPath
                        this.ActiveItem.IconPath = iconPath;

                        if(!Directory.Exists(PortabilityManager.IconCachePath))
                            Directory.CreateDirectory(PortabilityManager.IconCachePath);

                        MiscUtils.SaveBitmapImage(this.ActiveItem.Icon, Path.Combine(PortabilityManager.IconCachePath, iconPath));
                    }
                    catch { }

                    IconFrame.Source = this.ActiveItem.Icon;

                    if (this.ActiveItem.IsFolder)
                    {
                        this.ActiveItem.UpdateFolderIcon();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.InnerException.Message);
            }
        }

        private void ChoosePathButton_Click(object sender, RoutedEventArgs e)
        {
            ChoosePathContextMenu.PlacementTarget = ChoosePathButton;
            ChoosePathContextMenu.IsOpen = true;
        }

        private void FileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            if (op.ShowDialog() == true)
            {
                this.ActiveItem.ApplicationPath = op.FileName;
                this.PathBox.Text = this.ActiveItem.ApplicationPath;

                //only load icon if no replacement icon is set
                if (this.ActiveItem.IconPath == null)
                {
                    this.ActiveItem.Icon = SBItem.LoadingImage;

                    try
                    {
                        this.ActiveItem.Icon = MiscUtils.GetFileThumbnail(this.ActiveItem.ApplicationPath);
                    }
                    catch { }

                    IconFrame.Source = this.ActiveItem.Icon;
                }
            }
        }

        private void FolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = ofd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.ActiveItem.ApplicationPath = ofd.SelectedPath;
                this.PathBox.Text = this.ActiveItem.ApplicationPath;

                //only load icon if no replacement icon is set
                if (this.ActiveItem.IconPath == null)
                {
                    this.ActiveItem.Icon = SBItem.LoadingImage;

                    try
                    {
                        this.ActiveItem.Icon = MiscUtils.GetFileThumbnail(this.ActiveItem.ApplicationPath);
                    }
                    catch { }

                    IconFrame.Source = this.ActiveItem.Icon;
                }
            }
        }


    }
}