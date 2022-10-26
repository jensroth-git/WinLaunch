using System;
using System.Linq;
using System.Windows;
using Xceed.Wpf.Toolkit.Primitives;

namespace WinLaunch
{
    public partial class SpringboardManager
    {
        #region Folder Properties

        //folders
        public SBItem ActiveFolder = null;

        public bool MainItemsLocked = false;
        public bool FolderOpen = false;
        public bool FolderClosing = false;
        public GridManager FolderGrid { get; set; }

        //Max Items (Rows * Columns)
        public int MaxFolderItems
        {
            get
            {
                return FolderGrid.XItems * FolderGrid.YItems;
            }
        }

        //can't be less than 1
        public int MinFolderItems = 2;

        //folder layout properties
        public double FolderArrowOffset = 0;

        public string FolderTitle = "";

        #endregion Folder Properties

        #region Animations

        private bool FolderAnimationRunning;
        public AnimationHelper FolderYOffsetAnim;
        public double FolderYOffsetOrigin;
        public double FolderYOffset;
        public AnimationHelper FolderHeightAnim;
        public double FolderHeight;

        public void InitFolderAnimations()
        {
            FolderAnimationRunning = false;
            FolderYOffsetAnim = new AnimationHelper(0, 0);
            FolderYOffset = 0;

            FolderHeightAnim = new AnimationHelper(0, 0);
            FolderHeight = 0;

            FolderHeightAnim.duration = 600;
            FolderYOffsetAnim.duration = 600;
        }

        public void StepFolderAnimations()
        {
            bool FolderHeightAnimDone = FolderHeightAnim.Step();
            bool FolderYOffsetAnimDone = FolderYOffsetAnim.Step();

            //check if folder is closed
            if (FolderHeightAnim.GetProgress() > 0.99 && FolderYOffsetAnim.GetProgress() > 0.99)
            {
                if (FolderOpen && FolderClosing)
                {
                    FinishCloseFolder();
                }
            }

            if (FolderHeightAnimDone && FolderYOffsetAnimDone)
            {
                //animations done
                FolderAnimationRunning = false;
            }
            else
            {
                //animations still running
                FolderAnimationRunning = true;
            }

            FolderHeight = FolderHeightAnim.Value;
            FolderYOffset = FolderYOffsetAnim.Value;
        }

        private SBItem LastHoveredItem = null;

        private void StartFolderSuckAnimation(SBItem HoldItem, SBItem FolderItem)
        {
            if (LastHoveredItem != null)
            {
                //Reset folder
                StopFolderSuckAnimation(null, LastHoveredItem);
            }

            LastHoveredItem = FolderItem;
            if (HoldItem != null)
            {
                HoldItem.TextOpacityAnim.ValueTo = 0.0;
                HoldItem.ScaleAnim.ValueTo = 1.0;
            }

            if (FolderItem != null)
            {
                FolderItem.ShowDrop();
            }
        }

        private void StopFolderSuckAnimation(SBItem HoldItem, SBItem FolderItem)
        {
            if (HoldItem != null)
            {
                HoldItem.TextOpacityAnim.ValueTo = 1.0;
                HoldItem.ScaleAnim.ValueTo = IconMovingUpscale;
            }

            if (FolderItem != null)
            {
                FolderItem.HideDrop(!(FolderItem == this.ActiveFolder));

                //in Jiggle Mode preview needs to stay at the bottom
                if (JiggleMode)
                    FolderItem.ScrollPreviewToBottom();
            }
        }

        #endregion Animations

        #region OpenFolder

        public void MuteSpringboardItems()
        {
            foreach (SBItem item in IC.Items)
            {
                item.StopWiggle();
                item.IsEnabled = false;

                if (item != ActiveFolder)
                {
                    item.OpacityAnim.ValueTo = 0.2;
                    item.ZIndex = BackgroundIcon;
                }
            }
        }

        public Point DebugPoint = new Point(0, 0);

        public void UpdateFolderPosition()
        {
            if (ActiveFolder != null && FolderOpen)
            {
                PositionFolderAndItems();
            }
        }

        /// <summary>
        /// settings only work for a 8*5 grid!
        /// Initializes folder position, height and springboard item positioning
        /// </summary>
        public void PositionFolderAndItems(bool Init = false)
        {
            Point FolderPosition = GM.GetPositionFromGridIndex(ActiveFolder.GridIndex, ActiveFolder.Page);

            //normalize position
            FolderPosition.X -= SP.CurrentPage * GM.DisplayRect.Width;

            double Columns = FolderGrid.GetColumns(0);

            if (Columns > FolderGrid.YItems)
                Columns = FolderGrid.YItems;

            //Height of a single column
            double ColumnHeight = (GM.DisplayRect.Height / 6.0);

            //Offset from the top of the folder to the first item column
            double TopOffset = 70.0;

            //Offset from the last item column to the bottom of the folder
            double BottomOffset = 0.0;

            double FolderHeight = TopOffset + (Columns * ColumnHeight) + BottomOffset;

            //offset from the folder icon to the folder top
            double TotalTopOffset = 26 * Theme.CurrentTheme.IconSize;

            //offset from the bottom of the folder to the bottom of the screen space
            double TotalBottomOffset = 13.0;

            double CanvasScale = (GM.DisplayRect.Height / ParentWindow.ActualHeight);
            double MaxBottomYPos = (ParentWindow.ActualHeight - 60.0) * CanvasScale;
            double YOffset = FolderPosition.Y + TotalTopOffset;
            double UpOffset = YOffset;

            if (YOffset + FolderHeight > MaxBottomYPos)
            {
                YOffset = MaxBottomYPos - FolderHeight;
            }

            UpOffset -= YOffset;

            //set folder animation values
            FolderYOffsetOrigin = FolderPosition.Y + TotalTopOffset;

            if (Init)
                FolderYOffsetAnim.Value = FolderPosition.Y + TotalTopOffset;

            FolderYOffsetAnim.ValueTo = YOffset;

            if (Init)
                FolderHeightAnim.Value = 0.0;

            FolderHeightAnim.ValueTo = FolderHeight;
            FolderArrowOffset = FolderPosition.X;

            double GridTop = YOffset // Global folder offset
                - (GM.DisplayRect.Height / 6.0 / 2.0) // align grid at the top of the 1. column
                + TopOffset; // add offset

            FolderGrid.DisplayRect = new Rect(0, GridTop, GM.DisplayRect.Width, GM.DisplayRect.Height);

            if (Init)
            {
                //initialize folder item positions
                FolderGrid.SetGridPositions(0, 0, true);
            }

            double DownOffset = FolderHeight - UpOffset - TotalBottomOffset;
            double FolderColumn = GM.GetItemColumn(ActiveFolder);

            //set SB item positions
            foreach (SBItem item in IC.Items)
            {
                if (item.Page == SP.CurrentPage)
                {
                    double column = GM.GetItemColumn(item);

                    if (column <= FolderColumn)
                    {
                        //move up or stay
                        Point pos = item.CenterPointXY(GM.GetPositionFromGridIndex(item.GridIndex, item.Page));
                        pos.Y -= UpOffset;
                        item.SetPosition(pos);
                    }
                    else
                    {
                        //move down
                        Point pos = item.CenterPointXY(GM.GetPositionFromGridIndex(item.GridIndex, item.Page));
                        pos.Y += DownOffset;
                        item.SetPosition(pos);
                    }
                }
            }
        }

        public void AddFolderItems()
        {
            //Add Folder Items To Host Element and set item properties
            foreach (SBItem item in ActiveFolder.IC.Items)
            {
                //TODO unhide instead of add (lagfix)
                container.Add(item.ContentRef);

                //make sure their on top
                item.ZIndex = FolderIcon;

                if (JiggleMode)
                {
                    //set Jiggle style
                    item.StartWiggle();
                }
                else
                {
                    item.StopWiggle();
                }

                //hide items at the start
                item.SetGlobalClip(0, 0);
            }

            //correct item positions
            Host.UpdateLayout();

            if (SelItemIndFolder != -1)
            {
                //select the first index in the folder
                foreach (var item in ActiveFolder.IC.Items)
                {
                    if(item.GridIndex == 0)
                    {
                        SelectItem(item);
                        break;
                    }
                }
            }
        }

        public void OpenFolder(SBItem Folder)
        {
            if (FolderOpen || FolderClosing)
                return;

            if (Folder.IC.Items.Count < MinFolderItems)
                return;

            ActiveFolder = Folder;
            FolderGrid.IC = ActiveFolder.IC;

            //Clean Items
            FolderGrid.Cleanup();

            //lock scrolling while folder is open
            SP.LockPage();

            //Add Folder Items To Host Element and set item properties
            AddFolderItems();

            //calculate and set folder and Item positions
            PositionFolderAndItems(true);

            //set folder title
            FolderTitle = ActiveFolder.Name;

            //Rerender Folder Icon
            ActiveFolder.UpdateFolderIcon(false, true);

            //stops all Springboard item animations and fades their opacity
            MuteSpringboardItems();

            FolderOpen = true;

            ParentWindow.FolderOpened();
        }

        #endregion OpenFolder

        #region CloseFolder

        public void RemoveFolderItems()
        {
            foreach (SBItem item in ActiveFolder.IC.Items)
            {
                if (container.Contains(item.ContentRef))
                    container.Remove(item.ContentRef);
            }

            Host.UpdateLayout();
        }

        public void RepositionFolderAndItems()
        {
            //set initial springboard item positions
            GM.SetGridPositions();

            double TotalTopOffset = 26 * Theme.CurrentTheme.IconSize;

            FolderHeightAnim.ValueTo = 0;
            FolderYOffsetAnim.ValueTo = GM.GetPositionFromGridIndex(ActiveFolder.GridIndex, ActiveFolder.Page).Y + TotalTopOffset;
        }

        public void UnmuteSpringboardItems()
        {
            Random rd = new Random();
            foreach (SBItem item in IC.Items)
            {
                if (JiggleMode)
                {
                    item.StartWiggle();

                    if (item.IsFolder)
                    {
                        item.ScrollPreviewToBottom();
                    }
                }

                item.OpacityAnim.ValueTo = 1.0;
                item.ZIndex = BackgroundIcon;
                item.IsEnabled = true;
            }
        }

        //no handler for when moving an item!
        public void BeginCloseFolder()
        {
            if (!FolderOpen || FolderClosing)
                return;

            //starts the folder closing animation
            FolderClosing = true;

            //Restore everything
            RepositionFolderAndItems();
            UnmuteSpringboardItems();

            //if we selected the folder via keyboard select it again after closing the folder
            if(SelItemIndFolder != -1)
            {
                SelectItem(ActiveFolder);
                SelItemIndFolder = -1;
                SelItemInd = ActiveFolder.GridIndex;
            }
        }

        //gets called when either the animation is finished or another folder has been opened
        public void FinishCloseFolder()
        {
            if (!FolderClosing)
                return;

            //Raise event
            ParentWindow.FolderClosed();

            //Rerender Folder Icon
            ActiveFolder.UpdateFolderIcon(true);

            //Remove Items
            RemoveFolderItems();

            //Unlock everything
            SP.UnlockPage();

            FolderClosing = false;
            FolderOpen = false;
            ActiveFolder = null;

            SelItemIndFolder = -1;
        }

        public void CloseFolderInstant()
        {
            if (!FolderOpen)
                return;

            BeginCloseFolder();
            FinishCloseFolder();

            //finish all animations
            FolderYOffsetAnim.Finish();
            FolderHeightAnim.Finish();

            foreach (SBItem item in IC.Items)
            {
                item.FinishAnimations();
            }
        }

        #endregion CloseFolder

        #region Folder Item Utils

        // if folder contains less then MinFolderItems items
        // the folder will be dissolved
        public void FolderContainsCheck(SBItem Folder)
        {
            if (Folder.IC.Items.Count < MinFolderItems)
            {
                //Close the folder
                BeginCloseFolder();
                FinishCloseFolder();

                //move all items to the springboard
                foreach (SBItem item in Folder.IC.Items)
                {
                    item.UnsetClip();

                    //correct position
                    Point pos = item.GetPosition();

                    item.SetPositionImmediate(new Point(pos.X - SP.GetTotalXOffset(), pos.Y));
                    this.AddItemToSpringboard(item, Folder.Page, Folder.GridIndex);
                }

                //remove the folder
                Folder.IC.Items.Clear();
                RemoveItemFromSB(Folder);
            }
        }

        public void MoveItemIntoFolder(SBItem Item, SBItem Folder)
        {
            //Remove Item from SB
            RemoveItemFromSB(Item);

            //Add Item to folder
            int GridIndex = Folder.IC.Items.Count;

            Item.GridIndex = GridIndex;
            Item.Page = 0;
            Item.ClearOffsetPosition();

            Folder.IC.Items.Add(Item);

            //Redraw folder
            Folder.UpdateFolderIcon(true);
        }

        public void MoveItemOutOfFolder(SBItem Item, SBItem Folder)
        {
            //make sure that folder is closed
            BeginCloseFolder();
            FinishCloseFolder();

            //remove item from folder
            if (Folder.IC.Items.Contains(Item))
                Folder.IC.Items.Remove(Item);

            //check if grid index has been removed
            if (!FolderGrid.IsGridIndexSet(Item.GridIndex, 0))
            {
                //cell empty -> remove it
                FolderGrid.RemoveGridCell(Item.GridIndex, 0);
            }

            //Unset clip in case folderanimation wasn't completed
            Item.UnsetClip();

            this.AddItem(Item, Folder.Page);

            //reset properties of the item
            //since add item destroys them
            StartMovingItem(Item);

            //Redraw Folder
            Folder.UpdateFolderIcon(!FolderOpen);

            FolderContainsCheck(Folder);
        }

        public void CreateNewFolder(SBItem ItemA, SBItem ItemB, string folderName = "NewFolder")
        {
            //Create Folder
            SBItem Folder = new SBItem(folderName, "Folder", null, "", SBItem.FolderIcon);
            Folder.IsFolder = true;

            int GridIndex = Math.Min(ItemA.GridIndex, ItemB.GridIndex);
            int Page = ItemA.Page;

            //Remove both items from Springboard
            RemoveItemFromSB(ItemA);
            RemoveItemFromSB(ItemB);

            //Add items to folder
            Folder.IC.Items.Add(ItemA);
            Folder.IC.Items.Add(ItemB);

            //set new item positions
            ItemA.GridIndex = 0;
            ItemA.Page = 0;

            ItemB.GridIndex = 1;
            ItemB.Page = 0;

            ItemA.ClearOffsetPosition();
            ItemB.ClearOffsetPosition();

            //Finish Item animations
            ItemA.FinishAnimations();
            ItemB.FinishAnimations();

            //Render Folder
            Folder.UpdateFolderIcon(true);

            //place folder on Springboard
            this.AddItem(Folder, Page, GridIndex);

            //set position
            Point pos = Folder.CenterPointXY(GM.GetPositionFromGridIndex(GridIndex, Page));
            Folder.SetPositionImmediate(pos);
            Folder.ApplyPosition();
            Folder.ScaleAnim.Value = 1.2;
            Folder.ScaleAnim.ValueTo = 1.0;

            if (Settings.CurrentSettings.OpenFolderWhenCreated)
            {
                OpenFolder(Folder);
            }
        }

        public void UpdateFolderItemClip()
        {
            foreach (SBItem item in ActiveFolder.IC.Items)
            {
                if (MoveMode && Moving && item == HoldItem)
                {
                    item.UnsetClip();
                }
                else
                {
                    item.SetGlobalClip(FolderYOffset, FolderYOffset + FolderHeight + 21 - 1);
                }
            }
        }

        #endregion Folder Item Utils
    }
}