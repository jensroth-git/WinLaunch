using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Reflection;
using System.Windows;

namespace WinLaunch
{
    public class GridManager
    {
        public ItemCollection IC;

        public Rect DisplayRect = new Rect(0.0, 0.0, 1920.0, 1080.0);

        public double Columns { get { return XItems; } set { XItems = (int)value; } }
        public double Rows { get { return YItems; } set { YItems = (int)value; } }

        public int XItems { get; set; }
        public int YItems { get; set; }

        public int MaxGridIndex
        {
            get { return (XItems * YItems) - 1; }
        }

        public GridManager()
        {
        }

        #region Get Positions

        public System.Windows.Point GetPositionFromGridIndex(int GridIndex, int Page)
        {
            int YGrid = (int)Math.Floor(((double)GridIndex) / ((double)XItems));
            int XGrid = GridIndex - (YGrid * XItems);

            double X = DisplayRect.Left + (DisplayRect.Width / (XItems + 1)) * (XGrid + 1);
            double Y = DisplayRect.Top + (DisplayRect.Height / (YItems + 1)) * (YGrid + 1);

            X += (Page * DisplayRect.Width);

            return new System.Windows.Point(X, Y);
        }

        public int GetGridIndexFromPoint(System.Windows.Point Pos)
        {
            double cellwidth = (DisplayRect.Width / ((double)XItems + 1.0));
            double XPos = Pos.X - (cellwidth / 2.0) - DisplayRect.Left;
            int Xcell = (int)Math.Floor((XPos / (cellwidth * (double)XItems)) * (double)XItems);

            if (Xcell < 0 || Xcell >= XItems)
                return -1;

            double cellheight = (DisplayRect.Height / ((double)YItems + 1.0));
            double YPos = Pos.Y - (cellheight / 2.0) - DisplayRect.Top;
            int Ycell = (int)Math.Floor((YPos / (cellheight * (double)YItems)) * (double)YItems);

            if (Ycell < 0 || Ycell >= YItems)
                return -1;

            int GridIndex = (Ycell * XItems) + Xcell;
            return GridIndex;
        }

        #endregion Get Positions

        public void InitializePositions()
        {
            int ItemsPerPage = XItems * YItems;
            int Page = 0;
            int GridIndex = 0;

            foreach (SBItem item in IC.Items)
            {
                //get position
                System.Windows.Point ItemPosition = item.CenterPointXY(GetPositionFromGridIndex(GridIndex, Page));

                //set values
                item.GridIndex = GridIndex;
                item.Page = Page;
                item.SetPositionImmediate(ItemPosition);

                GridIndex++;
                if (GridIndex == ItemsPerPage)
                {
                    GridIndex = 0;
                    Page++;
                }
            }
        }

        //Set Positions by GridIndex and Page values
        public void SetGridPositions(double Xoff = 0.0, double Yoff = 0.0, bool SetImmediate = false)
        {
            foreach (SBItem item in IC.Items)
            {
                //get position
                System.Windows.Point ItemPosition = item.CenterPointXY(GetPositionFromGridIndex(item.GridIndex, item.Page));

                ItemPosition.X += Xoff;
                ItemPosition.Y += Yoff;

                //set values
                if (SetImmediate)
                    item.SetPositionImmediate(ItemPosition);
                else
                    item.SetPosition(ItemPosition);
            }
        }

        #region Cleanup

        //cleans Empty Space
        public void CleanEmptyPages()
        {
            //find empty pages
            int current_page = 0;
            bool page_empty = true;
            while (true)
            {
                page_empty = true;
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == current_page)
                    {
                        //page is not empty
                        page_empty = false;
                        break;
                    }
                }

                if (page_empty)
                {
                    //page empty
                    //move all items one page back

                    bool items_found = false;
                    foreach (SBItem item in IC.Items)
                    {
                        if (item.Page > current_page)
                        {
                            items_found = true;
                            item.Page--;
                        }
                    }

                    if (!items_found)
                        break;
                }
                else
                {
                    //page not empty (anymore)
                    current_page++;
                }
            }
        }

        public bool FindNextHole(out int GridIndex, out int Page)
        {
            int CurrentPage = 0;
            int CurrentGridIndex = 0;
            int PageCount = GetUsedPages();

            if(PageCount == 0)
            {
                GridIndex = -1;
                Page = -1;

                return false;
            }

            while (true)
            {
                if (GetItemFromIndex(CurrentGridIndex, CurrentPage) == null && GetItemFromIndex(CurrentGridIndex + 1, CurrentPage) != null)
                {
                    //found hole
                    GridIndex = CurrentGridIndex;
                    Page = CurrentPage;
                    return true;
                }

                if (CurrentGridIndex == MaxGridIndex - 1)
                {
                    CurrentGridIndex = 0;

                    if (CurrentPage == PageCount - 1)
                        break; // all pages cleaned
                    else
                        CurrentPage++;
                }
                else
                {
                    CurrentGridIndex++;
                }
            }

            GridIndex = -1;
            Page = -1;

            return false;
        }

        public void CleanHoles()
        {
            //fix holes
            int GridIndex;
            int Page;
            while (FindNextHole(out GridIndex, out Page))
            {
                //fix hole
                RemoveGridCell(GridIndex, Page);
            }
        }

        public bool FindNextStack(out int GridIndex, out int Page, out SBItem TopStack)
        {
            int CurrentPage = 0;
            int CurrentGridIndex = 0;
            int PageCount = GetUsedPages();

            if(PageCount == 0)
            {
                GridIndex = -1;
                Page = -1;
                TopStack = null;

                return false;
            }

            while (true)
            {
                SBItem Item = GetItemFromIndex(CurrentGridIndex, CurrentPage);
                if (Item != null)
                {
                    if (GetItemFromIndex(CurrentGridIndex, CurrentPage, Item) != null)
                    {
                        //found stack
                        GridIndex = CurrentGridIndex;
                        Page = CurrentPage;
                        TopStack = Item;
                        return true;
                    }
                }

                if (CurrentGridIndex == MaxGridIndex)
                {
                    CurrentGridIndex = 0;

                    if (CurrentPage == PageCount - 1)
                        break; // all pages cleaned
                    else
                        CurrentPage++;
                }
                else
                {
                    CurrentGridIndex++;
                }
            }

            GridIndex = -1;
            Page = -1;
            TopStack = null;

            return false;
        }

        //fixes multiple items stacked ontop of each other
        public void CleanStacks()
        {
            //fix stacks
            int GridIndex;
            int Page;
            SBItem TopStack = null;

            while (FindNextStack(out GridIndex, out Page, out TopStack))
            {
                //fix stack
                TopStack.GridIndex = -1;
                int FinalGridIndex = AddGridCell(GridIndex, Page);

                //move Item
                TopStack.GridIndex = FinalGridIndex;
                TopStack.Page = Page;
            }
        }

        //removes empty pages
        //fills holes in sb
        //solves double positions
        public void Cleanup(bool freePlacement = false)
        {
            if (IC.Items.Count > 0)
            {
                CleanEmptyPages();

                if(!freePlacement)
                {
                    CleanHoles();
                }
                
                CleanStacks();
            }
        }

        #endregion Cleanup

        public double GetColumns(int Page)
        {
            int TopGridIndex = -1;

            //search the top grid index
            foreach (SBItem item in IC.Items)
            {
                if (item.Page == Page)
                {
                    if (item.GridIndex > TopGridIndex)
                        TopGridIndex = item.GridIndex;

                    if (TopGridIndex == MaxGridIndex)
                        break;
                }
            }

            //calculate column
            int YGrid = (int)Math.Floor(((double)TopGridIndex) / ((double)XItems));

            return (double)YGrid + 1;
        }

        public double GetItemColumn(SBItem item)
        {
            //calculate column
            int YGrid = (int)Math.Floor(((double)item.GridIndex) / ((double)XItems));

            return (double)YGrid;
        }

        //cleanup first to get an accurate result
        public int GetUsedPages()
        {
            int toppage = -1;
            foreach (SBItem item in IC.Items)
            {
                if (item.Page > toppage)
                    toppage = item.Page;
            }

            return toppage + 1;
        }

        public bool IsGridIndexSet(int GridIndex, int Page)
        {
            foreach (SBItem item in IC.Items)
            {
                if (item.Page == Page && item.GridIndex == GridIndex)
                {
                    return true;
                }
            }

            return false;
        }

        //positioning functions
        //GridIndex should not be used by any item for this to work properly
        public void RemoveGridCell(int GridIndex, int Page, bool allowFreePlacement = false)
        {
            //dont remove free spots when items are placed freely
            if (allowFreePlacement)
                return;

            //not a valid GridIndex or not a valid Page -> dont do anything
            if (GridIndex < 0 || Page < 0)
                return;

            //only free index if cell is free
            if (IsGridIndexSet(GridIndex, Page))
                return;

            while (true)
            {
                //search item in the following cell
                SBItem MovedItem = GetItemFromIndex(GridIndex + 1, Page);

                //no item found -> we're done
                if (MovedItem == null)
                    break;

                //move item
                MovedItem.GridIndex = GridIndex;

                GridIndex++;
            }
        }

        public int AddGridCell(int GridIndex, int Page, bool allowFreePlacement = false)
        {
            if (GridIndex < 0)
                return GridIndex;

            int current_cell = GridIndex;
            int current_page = Page;

            SBItem temp = null;
            bool PlacedEmpty = true;

            while (true)
            {
                bool FoundItem = false;
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == current_page && item.GridIndex == current_cell && item != temp)
                    {
                        //found item to be moved
                        if (current_cell == (XItems * YItems) - 1)
                        {
                            //move item to next page
                            temp = item;

                            current_cell = 0;
                            current_page++;

                            item.GridIndex = current_cell;
                            item.Page = current_page;
                        }
                        else
                        {
                            //move item one spot over
                            item.GridIndex = current_cell + 1;

                            //make room for this item next
                            temp = item;

                            //move on to the next cell
                            //eventually moving everything to the next page
                            current_cell++;
                        }

                        //item at the same spot found, needs further cleanup
                        FoundItem = true;
                        PlacedEmpty = false;
                        break;
                    }
                }

                if (!FoundItem)
                    break;
            }

            //cleanup empty space behind items
            if (PlacedEmpty && !allowFreePlacement)
            {
                current_cell = GridIndex;
                current_page = Page;

                //no empty spaces behind the last cell
                if (current_cell <= 0)
                    return 0;

                current_cell--;

                //leave out empty space
                while (true)
                {
                    bool FoundItem = false;
                    foreach (SBItem item in IC.Items)
                    {
                        //is the cell behind item empty?
                        if (item.Page == current_page && item.GridIndex == current_cell)
                        {
                            //item behind cell
                            FoundItem = true;
                            break;
                        }
                    }

                    if (FoundItem)
                        break;

                    if (current_cell <= 0)
                        return 0;

                    current_cell--;
                }

                return current_cell + 1;
            }
            else
            {
                return GridIndex;
            }
        }

        public void GetFirstFreeGridIndex(int Page, out int page, out int index)
        {
            //walk the page until no item is found in the next position

            int currentIndex = 0;
            while (true)
            {
                bool found = false;

                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page && item.GridIndex == currentIndex)
                    {
                        currentIndex++;
                        found = true;
                    }
                }

                if(currentIndex == (XItems * YItems))
                {
                    //reached end of page
                    Page++;
                    currentIndex = 0;
                }

                if(!found)
                {
                    page = Page;
                    index = currentIndex;

                    return;
                }
            }
        }

        public SBItem GetItemFromIndex(int GridIndex, int Page, SBItem exclude = null)
        {
            foreach (SBItem item in IC.Items)
            {
                if (item.Page == Page && item.GridIndex == GridIndex && item != exclude)
                    return item;
            }

            return null;
        }

        public bool MoveGridIndex(int FromGridIndex, int FromPage, int ToGridIndex, int ToPage)
        {
            //1. find item
            SBItem MovedItem = GetItemFromIndex(FromGridIndex, FromPage);

            //check if item has been found
            if (MovedItem == null)
                return false;

            MoveGridItem(MovedItem, ToGridIndex, ToPage);

            return true;
        }

        public void MoveGridItem(SBItem Item, int ToGridIndex, int ToPage, bool allowFreePlacement = false)
        {
            //TODO: needs different implementation for allowFreePlacement
            //otherwise you'll end up pushing items around forward without them going back

            //1. remove item from grid ENTIRELY
            //2. make room for item at new space
            //3. place item on new space

            int FromGridIndex = Item.GridIndex;
            int FromPage = Item.Page;

            //1. remove item from grid ENTIRELY
            Item.GridIndex = -1;
            Item.Page = -1;

            RemoveGridCell(FromGridIndex, FromPage, allowFreePlacement);

            //2. make room for item at new space
            ToGridIndex = AddGridCell(ToGridIndex, ToPage, allowFreePlacement);

            //3. place item on new space
            Item.GridIndex = ToGridIndex;
            Item.Page = ToPage;
        }

        #region Arrow Navigation
        public void GetClostestItemOnPreviousPage(int Page, int currentIndex, out int page, out int index)
        {
            int row = currentIndex / XItems;
            int column = XItems - 1;

            //find last occupied column
            List<SBItem> OccupiedColumn = new List<SBItem>();

            bool found = false;
            while (true)
            {
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page-1 && item.GridIndex % XItems == column)
                    {
                        OccupiedColumn.Add(item);
                        found = true;
                    }
                }

                if (found)
                {
                    break;
                }

                if (column == 0)
                {
                    //no items on the whole page
                    index = currentIndex;
                    page = Page;
                }

                //search the previous column
                column--;
            }

            //find the clostest match horizontaly
            SBItem closest = null;
            int clostestDistance = XItems * 2;

            foreach (SBItem item in OccupiedColumn)
            {
                int occupiedRow = item.GridIndex / XItems;

                int distance = Math.Abs(occupiedRow - row);
                if (distance < clostestDistance)
                {
                    closest = item;
                    clostestDistance = distance;
                }
            }

            page = closest.Page;
            index = closest.GridIndex;
        }

        public void GetClostestItemOnNextPage(int Page, int currentIndex, out int page, out int index)
        {
            int row = currentIndex / XItems;
            int column = 0;

            //find last occupied column
            List<SBItem> OccupiedColumn = new List<SBItem>();

            bool found = false;
            while (true)
            {
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page+1 && item.GridIndex % XItems == column)
                    {
                        OccupiedColumn.Add(item);
                        found = true;
                    }
                }

                if (found)
                {
                    break;
                }

                if (column == XItems-1)
                {
                    //no items on the whole page
                    index = currentIndex;
                    page = Page;
                }

                //search the next column
                column++;
            }

            //find the clostest match horizontaly
            SBItem closest = null;
            int clostestDistance = XItems * 2;

            foreach (SBItem item in OccupiedColumn)
            {
                int occupiedRow = item.GridIndex / XItems;

                int distance = Math.Abs(occupiedRow - row);
                if (distance < clostestDistance)
                {
                    closest = item;
                    clostestDistance = distance;
                }
            }

            page = closest.Page;
            index = closest.GridIndex;
        }

        public int FindItemLeftOnPage(int Page, int currentIndex)
        {
            if(currentIndex % XItems == 0)
            {
                //no more items to the left
                return -1;
            }

            //look to the left
            currentIndex--;

            bool found = false;

            while (true)
            {
                found = false;
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page && item.GridIndex == currentIndex)
                    {
                        found = true;
                    }
                }

                if(found)
                {
                    return currentIndex;
                }
                else
                {
                    if (currentIndex % XItems == 0)
                    {
                        //no more items to the left
                        return -1;
                    }

                    //look to the left
                    currentIndex--;
                }
            }
        }

        public int FindItemRightOnPage(int Page, int currentIndex)
        {
            if(currentIndex % XItems == XItems - 1)
            {
                //no more items to the right
                return -1;
            }

            //look to the right
            currentIndex++;

            bool found = false;

            while (true)
            {
                found = false;
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page && item.GridIndex == currentIndex)
                    {
                        found = true;
                    }
                }

                if (found)
                {
                    return currentIndex;
                }
                else
                {
                    if (currentIndex % XItems == XItems - 1)
                    {
                        //no more items to the right
                        return -1;
                    }

                    //look to the right
                    currentIndex++;
                }
            }
        }

        public int FindItemDown(int Page, int currentIndex)
        {
            int column = currentIndex % XItems;
            int row = currentIndex / XItems;

            //last row
            if (row == YItems - 1)
            {
                return currentIndex;
            }

            row++;

            //find next occupied row
            List<SBItem> OccupiedRow = new List<SBItem>();

            bool found = false;
            while (true)
            {
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page && item.GridIndex / XItems == row)
                    {
                        OccupiedRow.Add(item);
                        found = true;
                    }
                }

                if(found)
                {
                    break;
                }

                if (row == YItems - 1)
                {
                    return currentIndex;
                }

                //search the next row
                row++;
            }

            //find the clostest match horizontaly
            SBItem closest = null;
            int clostestDistance = XItems * 2;

            foreach (SBItem item in OccupiedRow)
            {
                int occupiedColumn = item.GridIndex % XItems;

                int distance = Math.Abs(occupiedColumn - column);
                if(distance < clostestDistance)
                {
                    closest = item;
                    clostestDistance = distance;
                }
            }

            return closest.GridIndex;
        }


        public int FindItemUp(int Page, int currentIndex)
        {
            int column = currentIndex % XItems;
            int row = currentIndex / XItems;

            //first row
            if (row == 0)
            {
                return currentIndex;
            }

            row--;

            //find previous occupied row
            List<SBItem> OccupiedRow = new List<SBItem>();

            bool found = false;
            while (true)
            {
                foreach (SBItem item in IC.Items)
                {
                    if (item.Page == Page && item.GridIndex / XItems == row)
                    {
                        OccupiedRow.Add(item);
                        found = true;
                    }
                }

                if (found)
                {
                    break;
                }

                if (row == 0)
                {
                    return currentIndex;
                }

                //search the previous row
                row--;
            }

            //find the clostest match horizontaly
            SBItem closest = null;
            int clostestDistance = XItems * 2;

            foreach (SBItem item in OccupiedRow)
            {
                int occupiedColumn = item.GridIndex % XItems;

                int distance = Math.Abs(occupiedColumn - column);
                if (distance < clostestDistance)
                {
                    closest = item;
                    clostestDistance = distance;
                }
            }

            return closest.GridIndex;
        }

        public void GetItemLeft(int Page, int currentIndex, out int page, out int index)
        {
            //search item to the left 
            int leftIndex = FindItemLeftOnPage(Page, currentIndex);

            if (leftIndex == -1)
            {
                //no more item found to the left
                if (Page != 0)
                {
                    //find clostest item on the previous page
                    GetClostestItemOnPreviousPage(Page, currentIndex,out page, out index);
                }
                else
                {
                    //we stay on the current Item
                    page = Page;
                    index = currentIndex;
                }
            }
            else
            {
                page = Page;
                index = leftIndex;
            }
        }

        public void GetItemRight(int Page, int currentIndex, out int page, out int index)
        {
            //search item to the left 
            int rightIndex = FindItemRightOnPage(Page, currentIndex);

            if (rightIndex == -1)
            {
                //no more item found to the right
                if (Page != GetUsedPages() - 1)
                {
                    //get the closest item on the next page
                    GetClostestItemOnNextPage(Page, currentIndex, out page, out index);
                }
                else
                {
                    //we stay on the current Item
                    page = Page;
                    index = currentIndex;
                }
            }
            else
            {
                page = Page;
                index = rightIndex;
            }
        }


        public void GetItemDown(int Page, int currentIndex, out int index)
        {
            //find the closest item downwards from the currentIndex
            int newIndex = FindItemDown(Page, currentIndex);

            index = newIndex;
        }

        public void GetItemUp(int Page, int currentIndex, out int index)
        {
            //find the closest item downwards from the currentIndex
            int newIndex = FindItemUp(Page, currentIndex);

            index = newIndex;
        }

        #endregion
    }
}