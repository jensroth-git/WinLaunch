using System;
using System.Diagnostics;
using System.Windows.Media.Animation;

namespace WinLaunch
{
    public sealed class AnimationHelper
    {
        public static Stopwatch Time = new Stopwatch();
        public static ExponentialEase EaseMode = new ExponentialEase() { EasingMode = EasingMode.EaseOut, Exponent = 10 };

        static AnimationHelper()
        {
            Time.Start();
        }

        private long startTime = 0;
        private double currentProgress = 1.0;

        //public
        public double duration = 200;

        public bool animation_done = true;
        public double startval = 0;

        //private
        private double val = 0;

        public double Value
        {
            get { return val; }
            set
            {
                //jump to position
                this.val = value;
                this.val_to = this.val;
                this.startval = this.val;
                animation_done = true;
                currentProgress = 1.0;
            }
        }

        private double val_to = 0;

        public double ValueTo
        {
            get { return val_to; }
            set
            {
                if (val_to != value)
                {
                    startTime = Time.ElapsedMilliseconds;
                    currentProgress = 0.0;

                    val_to = value;
                    startval = val;
                    animation_done = false;
                }
            }
        }

        //returns true if animation is finished
        public bool Step()
        {
            if (!animation_done)
            {
                currentProgress = (Time.ElapsedMilliseconds - startTime) / duration;

                if (currentProgress >= 1)
                {
                    currentProgress = 1;
                    animation_done = true;
                }

                //ease
                currentProgress = EaseMode.Ease(currentProgress);

                val = MathHelper.Animate(currentProgress, startval, val_to);

                return false;
            }

            //animation done
            return true;
        }

        public void Finish()
        {
            val = val_to;
            animation_done = true;
        }

        public double GetProgress()
        {
            return currentProgress;
        }

        public AnimationHelper(double value, double val_to)
        {
            startTime = Time.ElapsedMilliseconds;

            this.startval = value;
            this.val = value;
            this.val_to = val_to;

            if (value != val_to)
                animation_done = false;
        }
    }
}