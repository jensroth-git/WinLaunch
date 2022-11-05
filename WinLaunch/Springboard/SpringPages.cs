using System;
using System.Windows;

namespace WinLaunch
{
    public class SpringPages
    {
        public Rect DisplayRect = new Rect(0, 0, 1080, 1920);
        public event EventHandler PagesFlipped;

        public double XOffset = 0.0;
        public int CurrentPage = 0;
        public int TotalPages = 0;

        public bool Scrolling = false;
        public bool ScrollingLocked = false;
        public Point ScrollStart;

        private AnimationHelper TotalXOffsetAnimation = new AnimationHelper(0.0, 0.0) { duration = 600 };

        public double GetTotalXOffset()
        {
            double TotalXOff;

            if (Scrolling)
            {
                TotalXOff = -1 * (CurrentPage + XOffset) * DisplayRect.Width;
            }
            else
            {
                TotalXOff = -1 * TotalXOffsetAnimation.Value * DisplayRect.Width;
            }

            return TotalXOff;
        }

        public double GetXoffset()
        {
            double TotalXOff;

            if (Scrolling)
            {
                TotalXOff = CurrentPage + XOffset;
            }
            else
            {
                TotalXOff = TotalXOffsetAnimation.Value;
            }

            return TotalXOff;
        }

        public void LockPage()
        {
            ScrollingLocked = true;
            Scrolling = false;

            XOffset = 0.0;
        }

        public void UnlockPage()
        {
            ScrollingLocked = false;
        }

        //Update TotalPages!
        public void FlipPageRight(bool AllowScrollToEmpty = false)
        {
            if (ScrollingLocked || Scrolling)
                return;

            //flip to right
            if (AllowScrollToEmpty)
            {
                CurrentPage++;
            }
            else
            {
                if ((CurrentPage < (TotalPages - 1)))
                {
                    CurrentPage++;
                }
            }

            //update animation target
            TotalXOffsetAnimation.ValueTo = CurrentPage + XOffset;

            PagesFlipped(this, EventArgs.Empty);
        }

        public void FlipPageLeft()
        {
            if (ScrollingLocked || Scrolling)
                return;

            //flip to left
            if (CurrentPage > 0)
            {
                CurrentPage--;
            }

            //update animation target
            TotalXOffsetAnimation.ValueTo = CurrentPage + XOffset;

            PagesFlipped(this, EventArgs.Empty);
        }

        public bool SetPage(int Page)
        {
            if (Scrolling || ScrollingLocked)
                return false;

            if (Page >= 0 && Page < TotalPages)
            {
                CurrentPage = Page;
            }

            //update animation target
            TotalXOffsetAnimation.ValueTo = CurrentPage + XOffset;

            PagesFlipped(this, EventArgs.Empty);

            return true;
        }

        public bool StartScrolling(Point ScrollStart)
        {
            if (ScrollingLocked || Scrolling)
                return false;

            this.ScrollStart = ScrollStart;
            Scrolling = true;

            return true;
        }

        public bool StartScrolling()
        {
            return StartScrolling(ScrollStart);
        }

        public bool UpdateScrolling(Point Scrollpos, bool AllowScrollToEmpty = false)
        {
            if (ScrollingLocked || !Scrolling)
                return false;

            double draggedX = ScrollStart.X - Scrollpos.X;
            XOffset = draggedX / DisplayRect.Width;

            //correct page offset
            if (CurrentPage + XOffset < 0)
            {
                XOffset += Math.Abs(0 - XOffset) * 0.8;
                //XOffset = (CurrentPage + 0.25) * -1;
            }

            if (CurrentPage == TotalPages - 1 && !AllowScrollToEmpty)
            {
                if (XOffset > 0)
                {
                    XOffset -= Math.Abs(0 - XOffset) * 0.8;
                }
            }

            return true;
        }

        public bool EndScrolling(bool AllowScrollToEmpty = false)
        {
            if (ScrollingLocked)
                return false;

            Scrolling = false;

            //Save Current State
            TotalXOffsetAnimation.Value = CurrentPage + XOffset;

            //decide wheter or not sites are flipping
            if (XOffset > 0.13)
            {
                FlipPageRight(AllowScrollToEmpty);
            }

            if (XOffset < -0.13)
            {
                FlipPageLeft();
            }

            XOffset = 0.0;

            //set animation target
            TotalXOffsetAnimation.ValueTo = CurrentPage + XOffset;

            return true;
        }

        public void StepPageAnimation()
        {
            if (!Scrolling)
            {
                TotalXOffsetAnimation.Step();
            }
        }
    }
}