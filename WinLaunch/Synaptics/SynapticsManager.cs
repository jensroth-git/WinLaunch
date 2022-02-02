//using SYNCTRLLib;
//using System.Windows;

namespace WinLaunch
{
    /*
    public enum SynapticsActivationMethod
    {
        TouchDown,
        TouchUp,
        SlideUp,
        SlideDown,
        SlideDownSlideUp,
        SlideUpSlideDown
    }

    internal class SynapticsManager
    {
        private static SynAPICtrl api;
        private static SynDeviceCtrl device;

        private static Rect DeviceBounds = new Rect();
        private static Point pos = new Point();
        private static int touches = 0;

        #region public

        public static bool Active = false;

        public static bool Start()
        {
            Stop();

            api = new SynAPICtrl();
            api.Initialize();
            api.Activate();
            int lHandle = -1;

            int ret = api.FindDevice(SynConnectionType.SE_ConnectionAny, SynDeviceType.SE_DeviceTouchPad, lHandle);

            if (ret == -1)
            {
                api.Deactivate();
                return Active;
            }

            device = new SynDeviceCtrl();
            device.OnPacket += new _ISynDeviceCtrlEvents_OnPacketEventHandler(device_OnPacket);

            device.Select(ret);
            device.Activate();

            DeviceBounds.X = (double)device.GetLongProperty(SynDeviceProperty.SP_XLoSensor);
            DeviceBounds.Y = (double)device.GetLongProperty(SynDeviceProperty.SP_YLoSensor);
            DeviceBounds.Width = (double)device.GetLongProperty(SynDeviceProperty.SP_XHiSensor) - DeviceBounds.X;
            DeviceBounds.Height = (double)device.GetLongProperty(SynDeviceProperty.SP_YHiSensor) - DeviceBounds.Y;

            Active = true;
            return Active;
        }

        private static void device_OnPacket()
        {
            SynPacketCtrl packet = new SynPacketCtrl();
            device.LoadPacket(packet);

            touches = (int)packet.GetLongProperty((SynPacketProperty)268436245) & 3;

            //get coordinates
            pos = new Point();
            pos.X = (double)packet.GetLongProperty(SynPacketProperty.SP_X);
            pos.Y = (double)packet.GetLongProperty(SynPacketProperty.SP_Y);

            //normalize coordinates
            pos.X -= DeviceBounds.X;
            pos.Y -= DeviceBounds.Y;

            pos.X /= DeviceBounds.Width;
            pos.Y /= DeviceBounds.Height;

            pos.X = MathHelper.Clamp(pos.X);
            pos.Y = MathHelper.Clamp(pos.Y);

            //inverse Y
            pos.Y = 1.0 - pos.Y;

            if (Settings.CurrentSettings.InverseSynapticsScrolling)
            {
                //inverse X
                pos.X = 1.0 - pos.X;
            }
        }

        public static bool Stop()
        {
            if (!Active)
                return true;
            try
            {
                api.Deactivate();
                device.Deactivate();
            }
            catch { }

            Active = false;

            return true;
        }

        public static Point Position(double scale = 1.0)
        {
            return new Point(pos.X * scale, pos.Y * scale);
        }

        public static bool Touched = false;
        public static int PreviousTouches = 0;
        public static Point PreviousPosition = new Point(0, 0);
        public static Point TouchDown = new Point(0, 0);

        public static int Touches()
        {
            return touches;
        }

        #endregion public
    }*/
}