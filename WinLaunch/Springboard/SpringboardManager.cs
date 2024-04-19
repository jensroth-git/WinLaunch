using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.RightsManagement;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Point = System.Windows.Point;

namespace WinLaunch
{
    public enum ItemsUpdatedAction
    {
        Added,
        Removed,
        Moved,
        FolderModified
    }

    public class ItemsUpdatedEventArgs: EventArgs
    {
        public ItemsUpdatedAction Action;
    }

    /// <summary>
    /// Manages positioning, events and Animations of SBItems
    /// </summary>
    public partial class SpringboardManager
    {
        public ItemCollection IC;
        public GridManager GM { get; set; }
        public SpringPages SP;

        public event EventHandler<ItemsUpdatedEventArgs> ItemsUpdated;

        //search 
        public bool SearchMode = false;

        //item selected
        public int SelItemInd = -1;
        public int SelItemIndFolder = -1;

        //item handling
        public SBItem HoldItem = null;

        public SBItem TargetItem = null;

        //target for moving an item into place
        public int GridCellTarget = -1;

        public int PageTarget = -1;

        //references
        public MainWindow ParentWindow;

        public Panel Host;
        public UIElementCollection container;

        //in move mode (on Lion Move Mode is always on)
        public bool MoveMode = false;

        public bool InstantMoveMode = false;

        //jiggle (delete mode)
        public bool JiggleMode = false;

        //moving an item?
        public bool Moving = false;

        #region Timers

        //timer settings
        private DispatcherTimer JiggleModeTimer = null;

        private int JiggleModeTime = 1000;

        //If an item is held and the timer starts running
        public bool JiggleModeAttempt = false;

        private DispatcherTimer MoveItemTimer = null;
        private int MoveItemTime = 150;

        //Move item Attempt
        public bool MoveItemAttempt = false;

        private DispatcherTimer ChangeGridPositionTimer = null;
        private int ChangeGridPositionTime = 500;

        //MoveOverItemAttempt
        private bool ChangeGridPositionAttempt = false;

        #endregion Timers

        //ClickCloseAttempt
        private bool ClickCloseAttempt = false;

        private double IconDarkeningAmmount = 0.3;
        private double IconMovingUpscale = 1.2;

        public enum DropAction
        {
            CreateNewFolder,
            MoveIntoFolder,
            Nothing
        }

        private DropAction ActiveDropAction = DropAction.Nothing;
        private bool CenterFocused = false;

        #region Extension

        public bool LockItems = false;
        public bool ExtensionActive = false;

        #endregion Extension

        #region ZLayers

        //folderbg = 1
        private int BackgroundIcon = 0;

        private int ForegroundIcon = 3;
        private int FolderIcon = 2;

        #endregion ZLayers

        public SpringboardManager()
        {
            IC = new ItemCollection();
            GM = new GridManager();
            SP = new SpringPages();
            GM.IC = IC;

            //setup grid
            GM.XItems = 8;
            GM.YItems = 5;
            GM.DisplayRect = new Rect(0.0, 0.0, 1920.0, 1080.0);
            SP.DisplayRect = GM.DisplayRect;

            #region Initialize Timers

            JiggleModeTimer = new DispatcherTimer();
            JiggleModeTimer.Interval = new TimeSpan(0, 0, 0, 0, JiggleModeTime);
            JiggleModeTimer.Tick += new EventHandler(JiggleModeTimer_Elapsed);

            MoveItemTimer = new DispatcherTimer();
            MoveItemTimer.Interval = new TimeSpan(0, 0, 0, 0, MoveItemTime);
            MoveItemTimer.Tick += new EventHandler(MoveItemTimer_Elapsed);

            ChangeGridPositionTimer = new DispatcherTimer();
            ChangeGridPositionTimer.Interval = new TimeSpan(0, 0, 0, 0, ChangeGridPositionTime);
            ChangeGridPositionTimer.Tick += new EventHandler(ChangeGridPosition_Elapsed);

            #endregion Initialize Timers

            //init folders
            FolderGrid = new GridManager();
            FolderGrid.DisplayRect = new Rect();
            InitFolderAnimations();

            //setup pages 
            SP.PagesFlipped += SP_PagesFlipped;
        }

        private void SP_PagesFlipped(object sender, EventArgs e)
        {
            //reset the current selected item once pages are flipped 
            SelItemInd = -1;
        }

        ~SpringboardManager()
        {
            //Cleanup
            JiggleModeTimer.Stop();
            JiggleModeTimer.Tick -= JiggleModeTimer_Elapsed;

            MoveItemTimer.Stop();
            MoveItemTimer.Tick -= MoveItemTimer_Elapsed;

            ChangeGridPositionTimer.Stop();
            ChangeGridPositionTimer.Tick -= ChangeGridPosition_Elapsed;
        }

        public void Init(MainWindow window, Panel Host)
        {
            //set initial item position
            this.ParentWindow = window;
            this.Host = Host;
            this.container = Host.Children;
        }

        public void UpdateDisplayRect(Rect DisplayRect)
        {
            GM.DisplayRect = DisplayRect;
            FolderGrid.DisplayRect = DisplayRect;
            SP.DisplayRect = DisplayRect;
        }

        #region Item Management

        #region AddItems

        public void AddItem(SBItem Item, int Page = -1, int GridIndex = -1)
        {
            if (FolderOpen)
            {
                AddItemToActiveFolder(Item, GridIndex);
            }
            else
            {
                AddItemToSpringboard(Item, Page, GridIndex);
            }

            ItemsUpdated(this, new ItemsUpdatedEventArgs() { Action = ItemsUpdatedAction.Added });
        }

        public void AddItemToActiveFolder(SBItem Item, int GridIndex = -1)
        {
            //add item to folder
            //check if folder is full
            if (ActiveFolder.IC.Items.Count == MaxFolderItems)
            {
                //folder is full -> dont add it
                return;
            }
            else
            {
                //Add it
                if (GridIndex == -1)
                    GridIndex = MaxFolderItems - 1;

                Item.ZIndex = FolderIcon;
                int FreeCell = FolderGrid.AddGridCell(GridIndex, 0);
                Item.GridIndex = FreeCell;
                Item.Page = 0;

                ActiveFolder.IC.Items.Add(Item);
                container.Add(Item.ContentRef);
                Host.UpdateLayout();

                //Redraw Folder
                ActiveFolder.UpdateFolderIcon(false);

                //resize folder
                PositionFolderAndItems(false);
            }

            if (JiggleMode)
            {
                Item.StartWiggle();
            }
        }

        public void AddItemToSpringboard(SBItem Item, int Page = -1, int GridIndex = -1)
        {
            //position the item
            if (Page == -1)
            {
                Page = 0;
            }

            if (GridIndex == -1)
            {
                //find first free space on the page
                GM.GetFirstFreeGridIndex(Page, out Page, out GridIndex);
            }

            //add new items to the end of the current page and let them snap back 
            //free mode does not apply here
            int FreeCell = GM.AddGridCell(GridIndex, Page);
            Item.GridIndex = FreeCell;
            Item.Page = Page;
            Item.ZIndex = BackgroundIcon;

            //add the item
            IC.Items.Add(Item);
            container.Add(Item.ContentRef);
            Host.UpdateLayout();

            if (JiggleMode)
            {
                Item.StartWiggle();
            }

            //update page count
            SP.TotalPages = GM.GetUsedPages();
        }

        #endregion AddItems

        #region RemoveItems

        private bool RemoveItemPending = false;

        public void RemoveItem(SBItem Item, bool AskPermission = false) // <--- rewrite!!!
        {
            RemoveItemPending = true;

            //ask permission
            if (AskPermission)
            {
                if (Item.IsFolder)
                {
                    if (MessageBox.Show(TranslationSource.Instance["RemoveFolder"] + "\n \"" + Item.Name + "\" ", TranslationSource.Instance["Warning"], MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        RemoveItemPending = false;
                        return;
                    }
                }
                else
                {
                    if (MessageBox.Show(TranslationSource.Instance["RemoveItem"] + "\n \"" + Item.Name + "\" ", TranslationSource.Instance["Warning"], MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        RemoveItemPending = false;
                        return;
                    }
                }
            }

            if (Item == ActiveFolder) 
            {
                CloseFolderInstant();
            }

            int GridIndex = Item.GridIndex;
            int Page = Item.Page;

            //check if this is a folder or a springboard item
            if (!IC.Items.Contains(Item))
            {
                //not a top level item -> item in a folder
                if (ActiveFolder.IC.Items.Contains(Item))
                {
                    //folder item
                    ActiveFolder.IC.Items.Remove(Item);
                    FolderGrid.RemoveGridCell(GridIndex, Page);

                    //remove reference from Host container
                    if (container.Contains(Item.ContentRef))
                        container.Remove(Item.ContentRef);

                    Item.ContentRef.UpdateLayout();
                    Host.UpdateLayout();

                    //Redraw Folder
                    ActiveFolder.UpdateFolderIcon(false);

                    //resize folder
                    PositionFolderAndItems(false);

                    FolderContainsCheck(ActiveFolder);
                }
            }
            else
            {
                //springboard item
                if (Item.IsFolder)
                {
                    //delete all apps in the folder
                    Item.IC.Items.Clear();
                }

                //remove the item
                IC.Items.Remove(Item);
                GM.RemoveGridCell(GridIndex, Page, Settings.CurrentSettings.FreeItemPlacement);

                //remove reference from Host container
                if (container.Contains(Item.ContentRef))
                    container.Remove(Item.ContentRef);

                Item.ContentRef.UpdateLayout();
                Host.UpdateLayout();
            }

            if (Item == HoldItem)
                HoldItem = null;

            if (Item == ActiveFolder)
                ActiveFolder = null;

            RemoveItemPending = false;

            //update page count
            SP.TotalPages = GM.GetUsedPages();

            ItemsUpdated(this, new ItemsUpdatedEventArgs() { Action = ItemsUpdatedAction.Removed });
        }

        public void RemoveItemFromSB(SBItem Item)
        {
            //remove the item
            IC.Items.Remove(Item);
            GM.RemoveGridCell(Item.GridIndex, Item.Page, Settings.CurrentSettings.FreeItemPlacement);

            //remove reference from Host container
            if (container.Contains(Item.ContentRef))
                container.Remove(Item.ContentRef);

            //update page count
            SP.TotalPages = GM.GetUsedPages();

            Item.ContentRef.UpdateLayout();
            Host.UpdateLayout();

            if (Item == HoldItem)
                HoldItem = null;

            if (Item == ActiveFolder)
                ActiveFolder = null;
        }

        #endregion RemoveItems

        //called after loading a new IC
        public void UpdateIC()
        {
            if (IC.Items.Count == 0)
                return;

            //cleanup items while allowing for free placement
            GM.Cleanup( Settings.CurrentSettings.FreeItemPlacement);

            foreach (SBItem item in IC.Items)
            {
                container.Add(item.ContentRef);
            }

            Host.UpdateLayout();

            GM.SetGridPositions(0, 0, true);
            SP.TotalPages = GM.GetUsedPages();
        }

        public void UpdateIcons()
        {
            foreach (SBItem item in IC.Items)
            {
                if (item.IsFolder)
                {
                    if (FolderOpen && item == ActiveFolder)
                    {
                        item.UpdateFolderIcon(false);
                    }
                    else
                    {
                        item.UpdateFolderIcon(true);
                    }

                    //render items in the folder
                    foreach (SBItem folderitem in item.IC.Items)
                    {
                        folderitem.UpdateIcon();
                    }
                }
                else
                {
                    item.UpdateIcon();
                }
            }
        }

        public void RestoreItemProperties()
        {
            foreach (SBItem item in IC.Items)
            {
                if (item.IC.Items.Count > 0)
                {
                    foreach (SBItem folderitem in item.IC.Items)
                    {
                        folderitem.ScaleAnim.Value = 1.0;
                        folderitem.SetDarkness(0);
                    }
                }

                item.ScaleAnim.Value = 1.0;
                item.SetDarkness(0);
            }
        }

        public void RestoreItemDarkness()
        {
            foreach (SBItem item in IC.Items)
            {
                if (item.IC.Items.Count > 0)
                {
                    foreach (SBItem folderitem in item.IC.Items)
                    {
                        folderitem.SetDarkness(0);
                    }
                }

                item.SetDarkness(0);
            }
        }

        #endregion Item Management

        #region Mainloop

        public void Step()
        {
            #region Animations

            StepFolderAnimations();
            SP.StepPageAnimation();

            #endregion Animations

            #region Solve grids
            if (MoveMode && !SearchMode)
            {
                if (Moving)
                {
                    if (FolderOpen)
                    {
                        #region fastsolve folder Grid

                        int HoveredCell = FolderGrid.GetGridIndexFromPoint(MouseDev.GetPosition(Host));

                        // when mouse is not over a cell were going to place the item at the
                        // last cell on the page
                        if (HoveredCell == -1)
                            HoveredCell = FolderGrid.MaxGridIndex;

                        if (HoveredCell != HoldItem.GridIndex)
                        {
                            FolderGrid.MoveGridItem(HoldItem, HoveredCell, 0);
                        }

                        #endregion fastsolve folder Grid
                    }
                    else
                    {
                        #region solve SB Grid
                        if (HoldItem.IsFolder && !Settings.CurrentSettings.FreeItemPlacement)
                        {
                            //no fastsolve for folders in free placement mode
                            #region fastsolve

                            //when moving a folder we can move instantly
                            //because folders can't interact with items
                            int HoveredCell = GM.GetGridIndexFromPoint(MouseDev.GetPosition(Host));

                            // when mouse is not over a cell were going to place the item at the
                            // last cell on the page
                            if (HoveredCell == -1)
                                HoveredCell = GM.MaxGridIndex;

                            if (HoveredCell != HoldItem.GridIndex || SP.CurrentPage != HoldItem.Page)
                            {
                                GM.MoveGridItem(HoldItem, HoveredCell, (int)SP.CurrentPage, Settings.CurrentSettings.FreeItemPlacement);
                            }

                            #endregion fastsolve
                        }
                        else
                        {
                            //not moving a folder -> possibility to create one or move into one
                            int HoveredCell = GM.GetGridIndexFromPoint(MouseDev.GetPosition(Host));

                            // when mouse is not over a cell were going to place the item at the
                            // last cell on the page
                            if (HoveredCell == -1)
                                HoveredCell = GM.MaxGridIndex;

                            if (HoveredCell == HoldItem.GridIndex && SP.CurrentPage == HoldItem.Page)
                            {
                                //when item is over its own cell reset attempt and target
                                ResetChangeGridPositionAttempt();
                            }
                            else
                            {
                                //only start a new event if target has changed and is not the items gridcell anyway
                                //or the mouse has moved outside the center of the targetitem
                                if (HoveredCell != this.GridCellTarget || SP.CurrentPage != this.PageTarget || (TargetItem != null && CenterFocused != TargetItem.IsMouseOverCenter(MouseDev)))
                                {
                                    if (TargetItem != null)
                                    {
                                        CenterFocused = TargetItem.IsMouseOverCenter(MouseDev);
                                    }

                                    if (HoldItem.Page != SP.CurrentPage)
                                    {
                                        //if page is changed change position immediately
                                        //cancel attempt if running
                                        ResetChangeGridPositionAttempt();

                                        //change position
                                        GM.MoveGridItem(HoldItem, HoveredCell, (int)SP.CurrentPage, Settings.CurrentSettings.FreeItemPlacement);
                                        ActiveDropAction = DropAction.Nothing;
                                    }
                                    else
                                    {
                                        //when only cell changed start new attempt
                                        ChangeGridPositionAttempt = true;
                                        this.GridCellTarget = HoveredCell;
                                        this.PageTarget = (int)SP.CurrentPage;

                                        //restart timer
                                        ChangeGridPositionTimer.Stop();
                                        ChangeGridPositionTimer.Start();

                                        //note
                                        //in order to work properly the target has to be manually reset
                                        //everytime the event gets canceled
                                    }
                                }
                            }
                        }

                        #endregion solve SB Grid
                    }
                }
            }

            #endregion Solve grids

            #region Update Positions

            if (FolderOpen)
            {
                //set initial folder item positions
                if (FolderClosing)
                {
                    FolderGrid.SetGridPositions(0, FolderYOffsetAnim.Value - FolderYOffsetAnim.startval, !FolderYOffsetAnim.animation_done);
                }
                else
                {
                    FolderGrid.SetGridPositions(0, FolderYOffsetAnim.Value - FolderYOffsetAnim.ValueTo, !FolderYOffsetAnim.animation_done);
                }

                if (FolderAnimationRunning)
                {
                    //Update folder item clip
                    UpdateFolderItemClip();
                }
            }
            else
            {
                //set initial position
                GM.SetGridPositions();
            }

            if (MoveMode && !SearchMode)
            {
                if (Moving)
                {
                    //position MovedItem over Mouse
                    Point MousePos = HoldItem.CenterPointXY(MouseDev.GetPosition(Host));
                    HoldItem.SetFixPosition(MousePos);
                }
            }

            double TotalXOffset = SP.GetTotalXOffset();

            //Update Springboard Item positions
            foreach (SBItem item in IC.Items)
            {
                item.SetOffsetPosition(TotalXOffset, 0.0);

                //progress animation
                item.StepPosition();

                //set position
                item.ApplyPosition();
            }

            //Update Folder Item positions
            if (FolderOpen)
            {
                //update folder item positions
                foreach (SBItem item in ActiveFolder.IC.Items)
                {
                    //progress animation
                    item.StepPosition();

                    //set position
                    item.ApplyPosition();
                }
            }

            //if (Theme.CurrentTheme.BackgroundMode == BackgroundMode.Panorama)
            //{
            //    double TotalWidth = 0.5 + (double)SP.TotalPages - 1;
            //    double CurrentOffset = SP.GetXoffset() + 0.25;
            //    double NormOffset = CurrentOffset / TotalWidth;

            //    ParentWindow.SetBackgroundPanoramaOffset(NormOffset);
            //}

            #endregion Update Positions

            //Ready for another render ->
        }

        #endregion Mainloop

        #region MoveMode / JiggleMode

        public void SetInstantMoveMode(bool status)
        {
            InstantMoveMode = false;
            StopMoveMode();

            InstantMoveMode = status;

            if (status)
            {
                MoveMode = true;
            }
        }

        #region Change Grid position Handlers

        public void ResetChangeGridPositionAttempt()
        {
            //if attempt is running cancel it.
            if (ChangeGridPositionAttempt)
            {
                ChangeGridPositionAttempt = false;
                ChangeGridPositionTimer.Stop();
            }

            //reset target
            this.TargetItem = null;
            this.GridCellTarget = -1;
            this.PageTarget = -1;
            this.CenterFocused = false;

            //clear the drop action
            ActiveDropAction = DropAction.Nothing;

            //reset item properties
            StopFolderSuckAnimation(HoldItem, LastHoveredItem);
        }

        //gets called whenever the item changed the grid cell
        //handles whether or not to create a folder / move item into a folder (after dropping)
        //or to just change grid positions
        private void ChangeGridPosition_Elapsed(object sender, EventArgs e)
        {
            ChangeGridPositionTimer.Stop();

            if (ChangeGridPositionAttempt)
            {
                ChangeGridPositionAttempt = false;

                #region check if Item has been dragged onto another Item

                SBItem founditem = null;
                CenterFocused = false;

                //no multi-layer folders
                if (!HoldItem.IsFolder)
                {
                    foreach (SBItem item in IC.Items)
                    {
                        Point pos = MouseDev.GetPosition(item.ContentRef);

                        if (item.Page == SP.CurrentPage && item != HoldItem)
                        {
                            //check position
                            if (item.IsMouseOverCenter(MouseDev))
                            {
                                CenterFocused = true;
                                founditem = item;
                                break;
                            }
                        }
                    }
                }

                #endregion check if Item has been dragged onto another Item

                if (founditem != null)
                {
                    //call handler
                    ItemDraggedOnItem(HoldItem, founditem);
                }
                else
                {
                    //not dragged on any items
                    StopFolderSuckAnimation(HoldItem, LastHoveredItem);

                    //move item into position
                    GM.MoveGridItem(HoldItem, this.GridCellTarget, this.PageTarget, Settings.CurrentSettings.FreeItemPlacement);
                    ActiveDropAction = DropAction.Nothing;

                    //clear target
                    this.GridCellTarget = -1;
                    this.PageTarget = -1;
                }
            }
        }

        #endregion Change Grid position Handlers

        //Checks if item is hold long enough to be moved
        private void MoveItemTimer_Elapsed(object sender, EventArgs e)
        {
            MoveItemTimer.Stop();

            if (LockItems || SearchMode || RemoveItemPending || ActivateItemPending || HoldItem == ActiveFolder)
            {
                return;
            }

            if (MoveItemAttempt)
            {
                MoveItemAttempt = false;

                //don't move the item if permanent move mode is set
                //(it'll get picked up if mouse is moved)
                if (!InstantMoveMode || JiggleMode)
                {
                    StartMovingItem(HoldItem);
                }
            }
        }

        //Checks if Item has been holded long enough to activate move mode
        private void JiggleModeTimer_Elapsed(object sender, EventArgs e)
        {
            JiggleModeTimer.Stop();

            if (LockItems || RemoveItemPending || ActivateItemPending || HoldItem == ActiveFolder)
            {
                //if items are locked dont enable jiggle mode
                return;
            }

            if (JiggleModeAttempt)
            {
                //attempt successfull
                JiggleModeAttempt = false;

                StartMoveMode();
                StartMovingItem(HoldItem);
            }
        }

        //Begins moving an item and sets all the
        //item proprties
        private void StartMovingItem(SBItem MovedItem)
        {
            if (LockItems)
                return;

            HoldItem = MovedItem;
            Moving = true;

            //set item properties
            HoldItem.StopWiggle();
            HoldItem.ZIndex = ForegroundIcon;
            HoldItem.OpacityAnim.ValueTo = 0.7;
            HoldItem.ScaleAnim.ValueTo = IconMovingUpscale;
            HoldItem.ContentRef.IsHitTestVisible = false;

            //reset clip (in case its a clipped folder item)
            MovedItem.UnsetClip();
        }

        //drops the moved item and resets its properties.
        //also executes the active drop action
        //like creating a folder when an item is dropped onto another one...
        private void DropMovedItem()
        {
            if (Moving)
            {
                Moving = false;

                if (HoldItem != null)
                {
                    if (JiggleMode)
                    {
                        HoldItem.StartWiggle();
                    }

                    //set item properties back to normal
                    if (FolderOpen)
                        HoldItem.ZIndex = FolderIcon;
                    else
                        HoldItem.ZIndex = BackgroundIcon;

                    //reset ongoing folder suck animation
                    StopFolderSuckAnimation(HoldItem, LastHoveredItem);

                    //Reset properties to normal
                    HoldItem.OpacityAnim.ValueTo = 1.0;
                    HoldItem.ScaleAnim.ValueTo = 1.0;
                    HoldItem.TextOpacityAnim.Value = 1.0;

                    HoldItem.ContentRef.IsHitTestVisible = true;

                    //fix position
                    HoldItem.SetPositionFromFixPosition();

                    //clean pages
                    if (InstantMoveMode && !JiggleMode)
                        CleanPages();

                    //Check if Item has changed cells again
                    //if so the ChangeGridPositionAttempt is still active
                    if (ChangeGridPositionAttempt)
                    {
                        //position changed again!
                        //dont do anything

                        ChangeGridPositionAttempt = false;
                        ChangeGridPositionTimer.Stop();
                    }
                    else
                    {
                        //Item position has not changed again
                        //execute drop action

                        switch (ActiveDropAction)
                        {
                            case DropAction.CreateNewFolder:
                                {
                                    CreateNewFolder(HoldItem, TargetItem);
                                    break;
                                }
                            case DropAction.MoveIntoFolder:
                                {
                                    MoveItemIntoFolder(HoldItem, TargetItem);
                                    break;
                                }
                            case DropAction.Nothing:
                                break;
                        }
                    }

                    //clear target
                    this.GridCellTarget = -1;
                    this.PageTarget = -1;
                    TargetItem = null;
                    HoldItem = null;
                    
                    if(ActiveDropAction == DropAction.MoveIntoFolder || ActiveDropAction == DropAction.CreateNewFolder)
                    {
                        ItemsUpdated(this, new ItemsUpdatedEventArgs() { Action = ItemsUpdatedAction.FolderModified });
                    }
                    else
                    {
                        ItemsUpdated(this, new ItemsUpdatedEventArgs() { Action = ItemsUpdatedAction.Moved });
                    }

                    ActiveDropAction = DropAction.Nothing;
                }
            }
        }

        #region JiggleMode

        public void SetJiggleMode()
        {
            JiggleMode = true;

            if (FolderOpen)
            {
                foreach (SBItem item in ActiveFolder.IC.Items)
                {
                    item.StartWiggle();
                }
            }
            else
            {
                //make them Jiggle
                foreach (SBItem item in IC.Items)
                {
                    item.StartWiggle();

                    if (item.IsFolder)
                    {
                        item.ScrollPreviewToBottom();
                    }
                }
            }
        }

        public void UnsetJiggleMode()
        {
            JiggleMode = false;

            if (FolderOpen)
            {
                foreach (SBItem item in ActiveFolder.IC.Items)
                {
                    item.StopWiggle();
                }
            }

            foreach (SBItem item in IC.Items)
            {
                item.StopWiggle();

                if (item.IsFolder)
                {
                    item.ScrollPreviewToTop();
                }
            }
        }

        #endregion JiggleMode

        //if PermanentMoveMode is set just enable Jiggle style(enables deleting)
        //if not enable actual move mode
        public void StartMoveMode()
        {
            //LockItems blocks enabling of jiggle and move mode
            if (LockItems)
                return;

            if (!InstantMoveMode)
            {
                if (MoveMode)
                    return;

                MoveMode = true;
            }

            //set jiggle style
            SetJiggleMode();
        }

        //if PermanentMoveMode is set just remove jiggle style
        public void StopMoveMode()
        {
            if (!InstantMoveMode)
            {
                if (!MoveMode)
                    return;

                MoveMode = false;
            }

            if (JiggleMode)
            {
                UnsetJiggleMode();

                CleanPages();
            }
        }

        public void CleanPages()
        {
            GM.CleanEmptyPages();
            SP.TotalPages = GM.GetUsedPages();

            if (SP.TotalPages <= 0)
            {
                //no items at all
                SP.TotalPages = 1;
            }

            if (SP.CurrentPage >= SP.TotalPages)
                SP.CurrentPage = SP.TotalPages - 1;
        }

        #endregion MoveMode / JiggleMode

        #region Scrolling

        public bool MouseScrolling = false;

        #endregion Scrolling

        #region Item event handling

        //handle events from SBItems
        private bool ActivateItemPending = false;

        private void ItemActivatedHandler(object sender, EventArgs e)
        {
            if (LockItems)
                return;

            SBItem item = sender as SBItem;

            if (!ExtensionActive)
            {
                if (item.IsFolder)
                {
                    if (FolderOpen && item == ActiveFolder)
                    {
                        //close folder
                        BeginCloseFolder();
                    }
                    else
                    {
                        //open folder
                        OpenFolder(item);
                    }

                    return;
                }
            }

            ActivateItemPending = true;
            ParentWindow.ItemActivated(sender, e);
            ActivateItemPending = false;
        }

        //gets called when a click event has been noticed
        private void ItemClicked()
        {
            if (LockItems)
                return;

            SBItem ClickedItem = HoldItem;
            Point pos = MouseDev.GetPosition(ClickedItem.ContentRef);

            if (JiggleMode)
            {
                if (ClickedItem.ShowClose)
                {
                    if (ClickedItem.IsMouseOverCloseBox(MouseDev))
                    {
                        RemoveItem(ClickedItem);
                        return;
                    }
                }

                if (ClickedItem.IsFolder)
                {
                    if (ClickedItem.IsMouseOverCenter(MouseDev))
                    {
                        ItemActivatedHandler(ClickedItem, EventArgs.Empty);
                    }
                }
            }
            else
            {
                if (ClickedItem.IsMouseOverCenter(MouseDev))
                {
                    ItemActivatedHandler(ClickedItem, EventArgs.Empty);
                }
            }
        }

        private void ItemDraggedOnItem(SBItem DraggedItem, SBItem TargetItem)
        {
            this.TargetItem = TargetItem;

            ActiveDropAction = DropAction.Nothing;

            if (!DraggedItem.IsFolder)
            {
                if (TargetItem.IsFolder)
                {
                    //check folder space
                    if (TargetItem.IC.Items.Count < MaxFolderItems)
                    {
                        //Move item into folder
                        ActiveDropAction = DropAction.MoveIntoFolder;

                        //start folder animation
                        StartFolderSuckAnimation(DraggedItem, TargetItem);
                    }
                }
                else
                {
                    //Create new folder
                    ActiveDropAction = DropAction.CreateNewFolder;

                    //start item animation
                    StartFolderSuckAnimation(DraggedItem, TargetItem);
                }
            }
        }

        #endregion Item event handling

        #region Input handling

        //mouse stuff
        private MouseDevice MouseDev = null;

        private System.Windows.Point MouseDownPoint = new System.Windows.Point();

        bool RightMouseScrollAttempt = false;

        public void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (LockItems)
            {
                e.Handled = true;
                return;
            }

            MouseDev = e.MouseDevice;

            //right click -> only scrolling
            if (e.RightButton == MouseButtonState.Pressed && e.LeftButton == MouseButtonState.Released)
            {
                MouseDownPoint = e.MouseDevice.GetPosition(Host);
                RightMouseScrollAttempt = true;
            }

            //left click -> lot's of other stuff
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                if (!SP.Scrolling)
                {
                    if (FolderOpen)
                    {
                        if (!FolderClosing)
                        {
                            #region check if folder item has been clicked

                            SBItem founditem = null;

                            foreach (SBItem item in ActiveFolder.IC.Items)
                            {
                                if (item.IsMouseOver(MouseDev))
                                {
                                    founditem = item;
                                    break;
                                }
                            }

                            //check if folder itself has been clicked
                            if (founditem == null)
                            {
                                if (ActiveFolder.IsMouseOver(MouseDev))
                                {
                                    founditem = ActiveFolder;
                                }
                            }

                            //mouse down on a item
                            if (founditem != null)
                            {
                                if (!LockItems || founditem == ActiveFolder)
                                {
                                    //attempt to activate delete mode
                                    if (!JiggleMode)
                                    {
                                        HoldItem = founditem;

                                        //attempting to start movemode
                                        JiggleModeAttempt = true;
                                        JiggleModeTimer.Start();
                                    }

                                    //attempt to pickup item
                                    if (MoveMode)
                                    {
                                        HoldItem = founditem;

                                        //attempting to pickup item
                                        MoveItemAttempt = true;
                                        MoveItemTimer.Start();
                                    }

                                    if (!JiggleMode || MoveMode)
                                    {
                                        //save point for position change check
                                        MouseDownPoint = e.MouseDevice.GetPosition(Host);
                                    }

                                    //make icon dark
                                    founditem.SetDarkness(IconDarkeningAmmount);
                                }
                                else
                                {
                                    //if move mode is locked theres no need to check if move mode should be activated
                                    //or the item should be picked up.
                                    HoldItem = founditem;
                                    ItemClicked();
                                }
                            }
                            else
                            {
                                //no item clicked
                                //check if clicked outside of folder
                                //check if item got dragged out

                                Point pos = e.GetPosition(Host);
                                if (pos.Y < FolderYOffset || pos.Y > FolderYOffset + FolderHeight)
                                {
                                    //clicked outside of the folder -> close folder
                                    BeginCloseFolder();
                                }
                            }

                            #endregion check if folder item has been clicked
                        }
                    }
                    else
                    {
                        #region check if Springboard item has been clicked

                        bool found = false;
                        SBItem founditem = null;

                        foreach (SBItem item in IC.Items)
                        {
                            if (item.IsMouseOver(MouseDev))
                            {
                                found = true;
                                founditem = item;
                                break;
                            }
                        }

                        //mouse down on a item
                        if (found)
                        {
                            if (!LockItems && !SearchMode)
                            {
                                //attempt to activate delete mode
                                if (!JiggleMode)
                                {
                                    HoldItem = founditem;

                                    //attempting to start movemode
                                    JiggleModeAttempt = true;
                                    JiggleModeTimer.Start();
                                }

                                //attempt to pickup item
                                if (MoveMode)
                                {
                                    HoldItem = founditem;

                                    //attempting to pickup item
                                    MoveItemAttempt = true;
                                    MoveItemTimer.Start();
                                }

                                if (!JiggleMode || MoveMode)
                                {
                                    //save point for position change check
                                    MouseDownPoint = e.MouseDevice.GetPosition(Host);
                                }

                                //make icon dark
                                founditem.SetDarkness(IconDarkeningAmmount);
                            }
                            else
                            {
                                //if move mode is locked theres no need to check if move mode should be activated
                                //or the item should be picked up.
                                HoldItem = founditem;
                                ItemClicked();
                            }
                        }
                        else
                        {
                            //clicked on empty
                            MouseDownPoint = e.MouseDevice.GetPosition(Host);
                            SP.ScrollStart = e.GetPosition(Host);
                            ClickCloseAttempt = true;
                        }

                        #endregion check if Springboard item has been clicked
                    }
                }
            }
        }
        private bool HitRightEdge = false;
        private bool HitLeftEdge = false;

        public void MouseMove(object sender, MouseEventArgs e)
        {
            if (LockItems || SearchMode)
            {
                e.Handled = true;
                return;
            }

            MouseDev = e.MouseDevice;

            if (RightMouseScrollAttempt)
            {
                if (!MathHelper.IsPointInArea(e.GetPosition(Host), MouseDownPoint, 10))
                {
                    RightMouseScrollAttempt = false;

                    if (SP.StartScrolling(e.GetPosition(Host)))
                    {
                        MouseScrolling = true;
                    }
                }
            }

            //moving items
            if (JiggleModeAttempt || MoveItemAttempt)
            {
                //check if position changed
                if (!MathHelper.IsPointInArea(e.GetPosition(Host), MouseDownPoint, 10))
                {
                    if (JiggleModeAttempt)
                    {
                        //Jiggle Mode Attempt canceled when mouse is moved
                        JiggleModeAttempt = false;
                        JiggleModeTimer.Stop();
                    }

                    if (MoveItemAttempt)
                    {
                        //stop move item attempt
                        MoveItemTimer.Stop();
                        MoveItemAttempt = false;
                    }

                    if (InstantMoveMode)
                    {
                        //pickup item
                        if (HoldItem != ActiveFolder)
                            StartMovingItem(HoldItem);
                    }
                    else
                    {
                        if (!SP.Scrolling)
                        {
                            //tablet mode
                            if (SP.StartScrolling(MouseDev.GetPosition(null)))
                            {
                                MouseScrolling = true;
                            }
                        }
                    }
                }
            }

            //scrolling
            if (ClickCloseAttempt)
            {
                if (!MathHelper.IsPointInArea(e.GetPosition(Host), MouseDownPoint, 10))
                {
                    //mouse moved
                    ClickCloseAttempt = false;

                    if (!SP.Scrolling)
                    {
                        if (SP.StartScrolling())
                        {
                            MouseScrolling = true;
                        }
                    }
                }
            }

            if (SP.Scrolling && MouseScrolling)
            {
                if (e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed)
                {
                    SP.UpdateScrolling(e.GetPosition(Host), JiggleMode);
                }
                else
                {
                    SP.EndScrolling(JiggleMode);
                    MouseScrolling = false;
                }
            }

            //the other stuff
            if (e.LeftButton == MouseButtonState.Pressed && MoveMode && Moving && !SP.Scrolling)
            {
                if (FolderOpen)
                {
                    //check if item got dragged out
                    Point pos = e.GetPosition(Host);
                    if (pos.Y < FolderYOffset || pos.Y > FolderYOffset + FolderHeight)
                    {
                        //dragged out of folder
                        MoveItemOutOfFolder(HoldItem, ActiveFolder);
                    }
                }
                else
                {
                    //check if page side has been hit
                    double DragBorderWidth = 40.0;

                    //right edge
                    if (e.GetPosition(Host).X > GM.DisplayRect.Width - DragBorderWidth)
                    {
                        if (!HitRightEdge)
                        {
                            HitRightEdge = true;
                            SP.FlipPageRight(true);
                        }
                    }
                    else
                    {
                        HitRightEdge = false;
                    }

                    //left edge
                    if (e.GetPosition(Host).X < GM.DisplayRect.Left + DragBorderWidth)
                    {
                        if (!HitLeftEdge && SP.CurrentPage > 0)
                        {
                            HitLeftEdge = true;
                            SP.FlipPageLeft();
                        }
                    }
                    else
                    {
                        HitLeftEdge = false;
                    }
                }
            }
        }

        public void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (LockItems)
            {
                e.Handled = true;
                return;
            }

            MouseDev = e.MouseDevice;

            if (e.RightButton == MouseButtonState.Released && e.LeftButton == MouseButtonState.Released && MouseScrolling)
            {
                SP.EndScrolling(JiggleMode);
                MouseScrolling = false;

                //stop context menu
                e.Handled = true;
            }

            if (e.RightButton == MouseButtonState.Released)
            {
                RightMouseScrollAttempt = false;
            }

            if (e.LeftButton == MouseButtonState.Released)
            {
                //restore item properties
                RestoreItemDarkness();

                if (JiggleModeAttempt || MoveItemAttempt)
                    ItemClicked();

                if (JiggleModeAttempt)
                {
                    //Jiggle Mode Attempt canceled
                    JiggleModeAttempt = false;
                    JiggleModeTimer.Stop();
                }

                if (MoveItemAttempt)
                {
                    //Move Item Attempt canceled
                    MoveItemAttempt = false;
                    MoveItemTimer.Stop();
                }

                if (Moving)
                {
                    if (ChangeGridPositionAttempt)
                    {
                        //when dropped immediately move into folder / create folder etc.
                        ChangeGridPosition_Elapsed(null, EventArgs.Empty);
                    }

                    //drop item
                    DropMovedItem();
                }
                else if (ClickCloseAttempt)
                {
                    ClickCloseAttempt = false;

                    //clicked without moving
                    //close launchpad
                    ParentWindow.ToggleLaunchpad();
                }
            }
        }

        public void MouseLeave(object sender, MouseEventArgs e)
        {
            //if mouse leaves end scrolling & drop item immediately
            SP.EndScrolling(JiggleMode);
            DropMovedItem();
            MouseScrolling = false;
            RightMouseScrollAttempt = false;

            //also restore item properties
            RestoreItemDarkness();
        }

        SBItem CurrentSelectedItem = null;

        public void SelectItem(SBItem item)
        {
            UnselectItem();

            if(item != null)
            {
                item.SelectionBorder = new System.Windows.Media.SolidColorBrush(Colors.White);
            }            

            CurrentSelectedItem = item;
        }

        public void UnselectItem()
        {
            if (CurrentSelectedItem != null)
            {
                CurrentSelectedItem.SelectionBorder = new System.Windows.Media.SolidColorBrush(Colors.Transparent);
            }

            CurrentSelectedItem = null;
        }

        public void KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (JiggleMode && !Moving)
                {
                    StopMoveMode();
                }

                e.Handled = true;
            }

            // Key Arrow functions
            if(FolderOpen)
            {
                //in folders we dont have to worry about the page index 
                if (e.Key == Key.Left)
                {
                    int newPage = -1;
                    int newIndex = -1;

                    FolderGrid.GetItemLeft(0, SelItemIndFolder, out newPage, out newIndex);

                    var selectItem = FolderGrid.GetItemFromIndex(newIndex, newPage);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemIndFolder = newIndex;

                    e.Handled = true;
                }

                if (e.Key == Key.Right)
                {
                    int newPage = -1;
                    int newIndex = -1;

                    FolderGrid.GetItemRight(0, SelItemIndFolder, out newPage, out newIndex);

                    var selectItem = FolderGrid.GetItemFromIndex(newIndex, newPage);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemIndFolder = newIndex;

                    e.Handled = true;
                }

                if (e.Key == Key.Up)
                {
                    int newIndex = -1;
                    FolderGrid.GetItemUp(0, SelItemIndFolder, out newIndex);

                    if(newIndex == SelItemIndFolder)
                    {
                        //close folder
                        BeginCloseFolder();
                    }
                    else
                    {
                        var selectItem = FolderGrid.GetItemFromIndex(newIndex, 0);

                        //select the new item
                        SelectItem(selectItem);

                        SelItemIndFolder = newIndex;
                    }
                    
                    e.Handled = true;
                }

                if (e.Key == Key.Down)
                {
                    int newIndex = -1;
                    FolderGrid.GetItemDown(0, SelItemIndFolder, out newIndex);

                    var selectItem = FolderGrid.GetItemFromIndex(newIndex, 0);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemIndFolder = newIndex;
                    e.Handled = true;
                }

                if (e.Key == Key.Enter)
                {
                    if(!ParentWindow.FolderRenamingActive)
                    {
                        var selectItem = FolderGrid.GetItemFromIndex(SelItemIndFolder, 0);

                        ParentWindow.ItemActivated(selectItem, EventArgs.Empty);
                    }

                    e.Handled = true;
                }
            }
            else
            {
                if (e.Key == Key.Left)
                {
                    int newPage = -1;
                    int newIndex = -1;

                    GM.GetItemLeft(SP.CurrentPage, SelItemInd, out newPage, out newIndex);

                    if (newPage < SP.CurrentPage)
                    {
                        SP.FlipPageLeft();
                    }

                    var selectItem = GM.GetItemFromIndex(newIndex, newPage);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemInd = newIndex;

                    e.Handled = true;
                }

                if (e.Key == Key.Right)
                {
                    int newPage = -1;
                    int newIndex = -1;

                    if (SelItemInd == -1)
                    {
                        //get first item on page
                        newIndex = GM.GetFirstItemOnPage(SP.CurrentPage);
                        newPage = SP.CurrentPage;
                    }
                    else
                    {
                        GM.GetItemRight(SP.CurrentPage, SelItemInd, out newPage, out newIndex);

                        if (newPage > SP.CurrentPage)
                        {
                            SP.FlipPageRight();
                        }
                    }

                    var selectItem = GM.GetItemFromIndex(newIndex, newPage);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemInd = newIndex;

                    e.Handled = true;
                }

                if (e.Key == Key.Up)
                {
                    int newIndex = -1;
                    GM.GetItemUp(SP.CurrentPage, SelItemInd, out newIndex);

                    var selectItem = GM.GetItemFromIndex(newIndex, SP.CurrentPage);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemInd = newIndex;
                    e.Handled = true;
                }

                if (e.Key == Key.Down)
                {
                    int newIndex = -1;

                    if (SelItemInd == -1)
                    {
                        //get first item on page
                        newIndex = GM.GetFirstItemOnPage(SP.CurrentPage);
                    }
                    else
                    {
                        GM.GetItemDown(SP.CurrentPage, SelItemInd, out newIndex);
                    }

                    var selectItem = GM.GetItemFromIndex(newIndex, SP.CurrentPage);

                    //select the new item
                    SelectItem(selectItem);

                    SelItemInd = newIndex;
                    e.Handled = true;
                }

                // Launch the selected Item, currently Folder Items are not suported
                if (e.Key == Key.Enter)
                {
                    if(SelItemInd != -1)
                    {
                        var selectItem = GM.GetItemFromIndex(SelItemInd, SP.CurrentPage);

                        if(selectItem != null)
                        {
                            if (selectItem.IsFolder)
                            {
                                //select the first item in the folder by setting this to something other than -1
                                SelItemIndFolder = 0;

                                OpenFolder(selectItem);
                            }
                            else
                            {
                                ParentWindow.ItemActivated(selectItem, EventArgs.Empty);
                            }
                        }
                    }

                    e.Handled = true;
                }
            }
            
            if (e.Key == Key.F3 && !Moving)
            {
                if (!JiggleMode)
                    StartMoveMode();
                else
                {
                    StopMoveMode();
                }

                e.Handled = true;
            }
        }

        #endregion Input handling

        #region Search

        class PagePosition
        {
            public int Page { get; set; }
            public int GridIndex { get; set; }
        }

        Dictionary<SBItem, PagePosition> PagePositions;
        private List<SBItem> AllItems;

        //search by name and keywords
        public List<SBItem> FindItemsByExactName(string name, bool includeFolders = false)
        {
            List<SBItem> results = new List<SBItem>();

            foreach (var item in IC.Items)
            {
                if (item.IsFolder)
                {
                    if(includeFolders)
                    {
                        if (item.Name == name)
                        {
                            results.Add(item);
                        }
                    }

                    foreach (var subItem in item.IC.Items)
                    {
                        if (subItem.Name == name)
                        {
                            results.Add(subItem);
                        }
                    }
                }
                else
                {
                    if (item.Name == name)
                    {
                        results.Add(item);
                    }
                }
            }

            return results;
        }

        public List<SBItem> FindItemsByName(string name)
        {
            List<SBItem> results = new List<SBItem>();

            foreach (var item in AllItems)
            {
                if (item.IsFolder)
                {
                    foreach (var subItem in item.IC.Items)
                    {
                        if (subItem.Name.ToLower().Contains(name.ToLower()) || 
                            subItem.Keywords.ToLower().Contains(name.ToLower()))
                        {
                            results.Add(subItem);
                        }
                    }
                }
                else
                {
                    if (item.Name.ToLower().Contains(name.ToLower()) ||
                        item.Keywords.ToLower().Contains(name.ToLower()))
                    {
                        results.Add(item);
                    }
                }
            }

            results.Sort((a,b)=> a.Name.CompareTo(b.Name));

            return results;
        }

        public void StartSearch()
        {
            SearchMode = true;

            CloseFolderInstant();

            AllItems = new List<SBItem>();

            //backup all grid positions 
            PagePositions = new Dictionary<SBItem, PagePosition>();
            foreach (var item in IC.Items)
            {
                PagePositions[item] = new PagePosition() { Page = item.Page, GridIndex = item.GridIndex };

                if(item.IsFolder)
                {
                    foreach (var subItem in item.IC.Items)
                    {
                        PagePositions[subItem] = new PagePosition() { Page = subItem.Page, GridIndex = subItem.GridIndex };
                    }
                }
            }

            //remove all items from the board
            foreach (var item in IC.Items)
            {
                AllItems.Add(item);
                container.Remove(item.ContentRef);
            }

            IC.Items.Clear();

            Host.UpdateLayout();
        }

        private void ClearContainerItems()
        {
            List<ContentControl> itemsToRemove = new List<ContentControl>();

            foreach (var item in container)
            {
                if(item is ContentControl)
                {
                    itemsToRemove.Add(item as ContentControl);
                }
            }

            foreach (var item in itemsToRemove)
            {
                container.Remove(item);
            }
        }

        public void UpdateSearch(string search)
        {
            if(!SearchMode)
            {
                StartSearch();
            }

            SP.SetPage(0);

            var items = FindItemsByName(search);

            ClearContainerItems();

            IC.Items.Clear();

            foreach (var item in items)
            {
                IC.Items.Add(item);
                container.Add(item.ContentRef);
                item.ContentRef.UpdateLayout();

                //unset clip only works after the item has a proper height (after UpdateLayout)
                item.UnsetClip();
            }

            GM.SetSearchPositions(items);

            foreach (var item in items)
            {
                item.ApplyPosition();
                item.ContentRef.UpdateLayout();
            }

            Host.UpdateLayout();

            //select the first item 
            SelItemInd = 0;
            try
            {
                SelectItem(items.First());
            }
            catch
            { }
        }

        public void EndSearch()
        {
            if (!SearchMode)
                return;

            SearchMode = false;

            //clear selection 
            SelItemInd = -1;
            UnselectItem();

            ClearContainerItems();

            IC.Items.Clear();

            foreach (var item in AllItems)
            {
                IC.Items.Add(item);
                container.Add(item.ContentRef);
            }

            //reset all grid positions
            foreach (var item in IC.Items)
            {
                item.Page = PagePositions[item].Page;
                item.GridIndex = PagePositions[item].GridIndex;
                item.SetOffsetPosition(0, 0);

                if (item.IsFolder)
                {
                    foreach (var subItem in item.IC.Items)
                    {
                        subItem.Page = PagePositions[subItem].Page;
                        subItem.GridIndex = PagePositions[subItem].GridIndex;
                        subItem.SetOffsetPosition(0, 0);
                    }
                }
            }

            AllItems.Clear();

            GM.SetGridPositions(0, 0, true);

            Host.UpdateLayout();
        }
        #endregion Search
    }
}