using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    internal class GetIcon
    {
        #region interop

        #region constants

        private const int SHGFI_ICON = 0x100;
        private const int SHGFI_SMALLICON = 0x1;
        private const int SHGFI_LARGEICON = 0x0;

        private const int SHIL_JUMBO = 0x4;
        private const int SHIL_SMALL = 0x1;
        private const int SHIL_EXTRALARGE = 0x2;

        #endregion constants

        #region structs

        // This structure will contain information about the file
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFO
        {
            // Handle to the icon representing the file
            public IntPtr hIcon;

            // Index of the icon within the image list
            public int iIcon;

            // Various attributes of the file
            public uint dwAttributes;

            // Path to the file
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szDisplayName;

            // File type
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        private struct IMAGELISTDRAWPARAMS
        {
            public int cbSize;
            public IntPtr himl;
            public int i;
            public IntPtr hdcDst;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int xBitmap;        // x offest from the upperleft of bitmap
            public int yBitmap;        // y offset from the upperleft of bitmap
            public int rgbBk;
            public int rgbFg;
            public int fStyle;
            public int dwRop;
            public int fState;
            public int Frame;
            public int crEffect;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            private int _Left;
            private int _Top;
            private int _Right;
            private int _Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGEINFO
        {
            public IntPtr hbmImage;
            public IntPtr hbmMask;
            public int Unused1;
            public int Unused2;
            public RECT rcImage;
        }

        #endregion structs

        #region private ImageList COM Interop (XP)

        [ComImportAttribute()]
        [GuidAttribute("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        //helpstring("Image List"),
        private interface IImageList
        {
            [PreserveSig]
            int Add(
                IntPtr hbmImage,
                IntPtr hbmMask,
                ref int pi);

            [PreserveSig]
            int ReplaceIcon(
                int i,
                IntPtr hicon,
                ref int pi);

            [PreserveSig]
            int SetOverlayImage(
                int iImage,
                int iOverlay);

            [PreserveSig]
            int Replace(
                int i,
                IntPtr hbmImage,
                IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(
                IntPtr hbmImage,
                int crMask,
                ref int pi);

            [PreserveSig]
            int Draw(
                ref IMAGELISTDRAWPARAMS pimldp);

            [PreserveSig]
            int Remove(
            int i);

            [PreserveSig]
            int GetIcon(
                int i,
                int flags,
                ref IntPtr picon);

            [PreserveSig]
            int GetImageInfo(
                int i,
                ref IMAGEINFO pImageInfo);

            [PreserveSig]
            int Copy(
                int iDst,
                IImageList punkSrc,
                int iSrc,
                int uFlags);

            [PreserveSig]
            int Merge(
                int i1,
                IImageList punk2,
                int i2,
                int dx,
                int dy,
                ref Guid riid,
                ref IntPtr ppv);

            [PreserveSig]
            int Clone(
                ref Guid riid,
                ref IntPtr ppv);

            [PreserveSig]
            int GetImageRect(
                int i,
                ref RECT prc);

            [PreserveSig]
            int GetIconSize(
                ref int cx,
                ref int cy);

            [PreserveSig]
            int SetIconSize(
                int cx,
                int cy);

            [PreserveSig]
            int GetImageCount(
            ref int pi);

            [PreserveSig]
            int SetImageCount(
                int uNewCount);

            [PreserveSig]
            int SetBkColor(
                int clrBk,
                ref int pclr);

            [PreserveSig]
            int GetBkColor(
                ref int pclr);

            [PreserveSig]
            int BeginDrag(
                int iTrack,
                int dxHotspot,
                int dyHotspot);

            [PreserveSig]
            int EndDrag();

            [PreserveSig]
            int DragEnter(
                IntPtr hwndLock,
                int x,
                int y);

            [PreserveSig]
            int DragLeave(
                IntPtr hwndLock);

            [PreserveSig]
            int DragMove(
                int x,
                int y);

            [PreserveSig]
            int SetDragCursorImage(
                ref IImageList punk,
                int iDrag,
                int dxHotspot,
                int dyHotspot);

            [PreserveSig]
            int DragShowNolock(
                int fShow);

            [PreserveSig]
            int GetDragImage(
                ref POINT ppt,
                ref POINT pptHotspot,
                ref Guid riid,
                ref IntPtr ppv);

            [PreserveSig]
            int GetItemFlags(
                int i,
                ref int dwFlags);

            [PreserveSig]
            int GetOverlayImage(
                int iOverlay,
                ref int piIndex);
        };

        #endregion private ImageList COM Interop (XP)

        #region methods

        //Add Build markers here (Project settings / Build / symbols)
#if(X86DEBUG || X86RELEASE) //32 bit build
        ///
        /// SHGetImageList is not exported correctly in XP.  See KB316931
        /// http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q316931
        /// Apparently (and hopefully) ordinal 727 isn't going to change.
        ///
        [DllImport("shell32.dll", EntryPoint = "#727")]
        private extern static int SHGetImageList(
            int iImageList,
            ref Guid riid,
            out IImageList ppv
            );

        // The signature of SHGetFileInfo (located in Shell32.dll)
        [DllImport("Shell32.dll")]
        public static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

        [DllImport("Shell32.dll")]
        public static extern int SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

#else //64 bit build

        ///
        /// SHGetImageList is not exported correctly in XP.  See KB316931
        /// http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q316931
        /// Apparently (and hopefully) ordinal 727 isn't going to change.
        ///
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private extern static int SHGetImageList(
            int iImageList,
            ref Guid riid,
            out IImageList ppv
            );

        // The signature of SHGetFileInfo (located in Shell32.dll)
        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHGetFileInfo(string pszPath, int dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, int cbFileInfo, uint uFlags);

#endif

        [DllImport("user32")]
        public static extern int DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        #endregion methods

        #endregion interop

        #region helper methods

        private static BitmapSource SourceFromIcon(System.Drawing.Icon ic)
        {
            BitmapSource ic2 = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(ic.Handle,
                                                    System.Windows.Int32Rect.Empty,
                                                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
            ic2.Freeze();
            return ic2;
        }

        private static BitmapSource BitmapToSource(System.Drawing.Bitmap bitmap)
        {
            BitmapSource destination;
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSizeOptions sizeOptions = BitmapSizeOptions.FromEmptyOptions();
            destination = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, sizeOptions);

            if (destination.CanFreeze)
                destination.Freeze();

            if (hBitmap != IntPtr.Zero)
                DeleteObject(hBitmap);

            return destination;
        }

        private static System.Drawing.Bitmap resizeJumbo(System.Drawing.Bitmap imgToResize, System.Drawing.Size size)
        {
            System.Drawing.Bitmap b = new System.Drawing.Bitmap(size.Width, size.Height);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage((System.Drawing.Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

            int sourceWidth = (int)(48 * MiscUtils.GetDPIScale());
            int sourceHeight = (int)(48 * MiscUtils.GetDPIScale());

            double WidthScale = ((double)size.Width / (double)sourceWidth);
            double HeightScale = ((double)size.Height / (double)sourceHeight);

            g.DrawImage(imgToResize, 0, 0, (int)(256 * WidthScale), (int)(256 * HeightScale));
            g.Dispose();

            return b;
        }

        private static bool IsScaledDown(System.Drawing.Bitmap bitmap)
        {
            System.Drawing.Color empty = System.Drawing.Color.FromArgb(0, 0, 0, 0);

            if (bitmap != null)
            {
                if (bitmap.Width <= 48)
                    return false;

                int checks = 5;
                double SmallImageSize = 48.0 * MiscUtils.GetDPIScale() + 1;
                double CheckDistance = (bitmap.Width - SmallImageSize) / (double)(checks + 1);

                for (int x = 0; x < checks + 1; x++)
                {
                    for (int y = 0; y < checks + 1; y++)
                    {
                        int xpos = (int)(SmallImageSize + (double)x * CheckDistance);
                        int ypos = (int)(SmallImageSize + (double)y * CheckDistance);
                        try
                        {
                            if (bitmap.GetPixel(xpos, ypos) != empty)
                            {
                                //not an empty pixel
                                return false;
                            }
                        }
                        catch { }
                    }
                }
            }

            return true;
        }

        #endregion helper methods

        public static BitmapSource FromPath(string FileName)
        {
            #region declarations

            uint SHGFI_SYSICONINDEX = 0x4000;
            int FILE_ATTRIBUTE_NORMAL = 0x80;
            int ILD_IMAGE = 0x00000020;

            // Get the System IImageList object from the Shell:
            Guid iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            SHFILEINFO shinfo = new SHFILEINFO();
            IImageList iml;
            IntPtr hIcon = IntPtr.Zero;

            uint flags = SHGFI_SYSICONINDEX;
            int size = SHIL_JUMBO;

            #endregion declarations

            //SHGetFileInfo needs to be executed from the STAMainThread!
            //To Load Images from a different thread it needs to be done from the Dispatcher
            //get SHFileInfo struct
            int hres = SHGetFileInfo(FileName, FILE_ATTRIBUTE_NORMAL, ref shinfo, Marshal.SizeOf(shinfo), flags);
            if (hres == 0)
            {
                throw (new System.IO.FileNotFoundException(FileName));
            }

            int iconIndex = shinfo.iIcon;

            //write ImageList
            hres = SHGetImageList(size, ref iidImageList, out  iml);
            if (hres != 0)
            {
                throw (new Exception("SHGetImageList failed \n" + FileName + "\nhres = " + hres + "\n\nplease report this error to winlaunch.official@gmail.com so this bug can be fixed"));
            }

            //get Icon
            hres = iml.GetIcon(iconIndex, ILD_IMAGE, ref hIcon);
            if (hIcon == IntPtr.Zero)
            {
                throw new Exception("Error retrieving hIcon \n" + FileName + "\nhres = " + hres + "\n\nplease report this error to winlaunch.official@gmail.com so this bug can be fixed");
            }

            //convert Icon
            System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(hIcon);
            System.Drawing.Bitmap bitmap = icon.ToBitmap();
            icon.Dispose();
            DestroyIcon(hIcon);

            System.Drawing.Bitmap FinalBitmap = bitmap;

            if (size == SHIL_JUMBO)
            {
                //workaround to check if icon was resized by the shell
                //(if no jumbo available shell will resize the icon to 48x48 and position it in the upper left corner)
                if (IsScaledDown(FinalBitmap))
                {
                    FinalBitmap = resizeJumbo(bitmap, new System.Drawing.Size(200, 200));
                    bitmap.Dispose();
                }
            }

            BitmapSource bs = BitmapToSource(FinalBitmap);
            FinalBitmap.Dispose();

            return bs;
        }
    }
}