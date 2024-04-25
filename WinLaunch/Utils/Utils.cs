using IWshRuntimeLibrary;
using Microsoft.Win32;
using Shell32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinLaunch.Properties;
using MessageBox = System.Windows.MessageBox;

namespace WinLaunch
{
    public class DelayedAction
    {
        DispatcherTimer timer;
        Action action;

        public DelayedAction()
        {
            timer = new DispatcherTimer();
            timer.Tick += timer_Tick;
        }

        public DelayedAction(double ms, Action action = null)
        {
            timer = new DispatcherTimer();
            timer.Tick += timer_Tick;
            timer.Interval = TimeSpan.FromMilliseconds(ms);

            this.action = action;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();

            if (action != null)
            {
                action();
            }
        }

        public void SetAction(Action action)
        {
            this.action = action;
        }

        public void SetDelay(double ms)
        {
            timer.Interval = TimeSpan.FromMilliseconds(ms);
        }


        public void RunIn(double ms)
        {
            timer.Interval = TimeSpan.FromMilliseconds(ms);
            timer.Start();
        }

        public void RunIn(double ms, Action action)
        {
            this.action = action;

            RunIn(ms);
        }

        public void Run(Action action)
        {
            this.action = action;
            timer.Start();
        }

        public void RunNow()
        {
            timer.Stop();

            if (action != null)
            {
                action();
            }
        }

        public void StopExecution()
        {
            timer.Stop();
        }
    }

    public static class CompositionTargetEx
    {
        private static TimeSpan _last = TimeSpan.Zero;
        private static event EventHandler<RenderingEventArgs> _FrameUpdating;
        public static event EventHandler<RenderingEventArgs> FrameUpdating
        {
            add
            {
                if (_FrameUpdating == null)
                    CompositionTarget.Rendering += CompositionTarget_Rendering;

                _FrameUpdating += value;
            }

            remove
            {
                _FrameUpdating -= value;
                if (_FrameUpdating == null)
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
            }
        }

        private static void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            if (args.RenderingTime == _last)
                return;

            _last = args.RenderingTime;

            _FrameUpdating(sender, args);
        }
    }

    //Utils
    public static class VisualTreeHelperExtensions
    {
        public static T FindAncestor<T>(DependencyObject dependencyObject)
            where T : class
        {
            DependencyObject target = dependencyObject;
            do
            {
                target = VisualTreeHelper.GetParent(target);
            }
            while (target != null && !(target is T));
            return target as T;
        }

        public static T FindChild<T>(DependencyObject dependencyObject)
            where T : class
        {
            DependencyObject target = dependencyObject;
            do
            {
                target = VisualTreeHelper.GetChild(target, 0);
            }
            while (target != null && !(target is T));
            return target as T;
        }

        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Search all children
            if (parent == null) return null;
            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                T childType = child as T;
                if (childType == null)
                {
                    foundChild = FindChild<T>(child, childName);
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }
    }

    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string _resourceKey;
        public LocalizedDescriptionAttribute(string resourceKey)
        {
            _resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                string displayName = TranslationSource.Instance[_resourceKey];

                return string.IsNullOrEmpty(displayName)
                    ? string.Format("[[{0}]]", _resourceKey)
                    : displayName;
            }
        }
    }

    public static class EnumExtensions
    {
        public static string GetDescription(this Enum enumValue)
        {
            FieldInfo fi = enumValue.GetType().GetField(enumValue.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return enumValue.ToString();
        }
    }

    public static class MiscUtils
    {
        private static double DPIScale = -1.0;

        public static double GetDPIScale()
        {
            if (DPIScale == -1.0)
            {
                Matrix m = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow).CompositionTarget.TransformToDevice;
                double dx = m.M11;
                //double dy = m.M22;

                DPIScale = dx;
            }

            return DPIScale;
        }

        public static void UpdateDPIScale()
        {
            DPIScale = -1.0;
            GetDPIScale();
        }

        //async WebClient DownloadString method
        public static Task<string> DownloadStringTaskAsync(WebClient client, string url)
        {
            var tcs = new TaskCompletionSource<string>();

            DownloadStringCompletedEventHandler completed = null;
            completed = (sender, args) =>
            {
                client.DownloadStringCompleted -= completed;

                if (args.Error != null)
                    tcs.TrySetException(args.Error);
                else if (args.Cancelled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(args.Result);
            };

            client.DownloadStringCompleted += completed;
            client.DownloadStringAsync(new Uri(url));

            return tcs.Task;
        }

        public static string GetShortcutTargetFile(string shortcutFilename)
        {
            try
            {
                string pathOnly = System.IO.Path.GetDirectoryName(shortcutFilename);
                string filenameOnly = System.IO.Path.GetFileName(shortcutFilename);

                Shell shell = new Shell();
                Shell32.Folder folder = shell.NameSpace(pathOnly);

                if (folder == null)
                {
                    return null;
                }

                FolderItem folderItem = folder.ParseName(filenameOnly);
                if (folderItem != null)
                {
                    Shell32.ShellLinkObject link = (Shell32.ShellLinkObject)folderItem.GetLink;

                    return link.Path;
                }
            }
            catch { }

            return null;
        }

        public static string GetShortcutWorkingDirectory(string shortcutFilename)
        {
            try
            {
                IWshShell shell = new WshShell();
                var lnk = shell.CreateShortcut(shortcutFilename) as IWshShortcut;
                if (lnk != null)
                {
                    return lnk.WorkingDirectory;
                }
            }
            catch { }

            return null;
        }

        public static void RestartApplication()
        {
            ProcessStartInfo Info = new ProcessStartInfo();
            Info.Arguments = "/C ping 127.0.0.1 -n 10 & \"" + Assembly.GetExecutingAssembly().Location + "\"";
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);

            Environment.Exit(0);
        }

        public static void OpenURL(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                //fails on systems without default webbrowser
                return;
            }
        }
        /// <summary>
        /// Centers the Window in screen.
        /// </summary>
        /// <param name="window">The window.</param>
        public static void CenterInScreen(this Window window, int screenIndex = -1)
        {
            double width = window.ActualWidth;
            double height = window.ActualHeight;

            if (screenIndex == -1)
            {
                // Set Left and Top manually and calculate center of screen.
                window.Left = (SystemParameters.WorkArea.Width / GetDPIScale() - width) / 2
                    + SystemParameters.WorkArea.Left / GetDPIScale();
                window.Top = (SystemParameters.WorkArea.Height / GetDPIScale() - height) / 2
                    + SystemParameters.WorkArea.Top / GetDPIScale();
            }
            else
            {
                System.Windows.Forms.Screen[] Screens = System.Windows.Forms.Screen.AllScreens;

                if (screenIndex > Screens.GetUpperBound(0))
                    screenIndex = 0;

                window.Left = Screens[screenIndex].WorkingArea.Left / GetDPIScale() + ((Screens[screenIndex].WorkingArea.Width / GetDPIScale() - width) / 2.0);
                window.Top = Screens[screenIndex].WorkingArea.Top / GetDPIScale() + ((Screens[screenIndex].WorkingArea.Height / GetDPIScale() - height) / 2.0);
            }
        }

        public static int GetScreenIndexFromPoint(System.Drawing.Point p)
        {
            Screen activeScreen = Screen.FromPoint(p);

            if (activeScreen == null)
                return -1;

            for (int i = 0; i < Screen.AllScreens.Length; i++)
            {
                if (Screen.AllScreens[i].Bounds == activeScreen.Bounds)
                    return i;
            }

            return -1;
        }

        public static int GetActiveScreenIndex()
        {
            int activeIndex = GetScreenIndexFromPoint(System.Windows.Forms.Cursor.Position);

            if (activeIndex == -1)
                return 0;

            return activeIndex;
        }

        public static Rect RectCenterInScreen(double width, double height, int screenIndex = -1)
        {
            Rect ret = new Rect();

            ret.Width = width;
            ret.Height = height;

            if (screenIndex == -1)
            {
                // Set Left and Top manually and calculate center of screen.
                ret.X = (SystemParameters.WorkArea.Width - width) / 2
                    + SystemParameters.WorkArea.Left;
                ret.Y = (SystemParameters.WorkArea.Height - height) / 2
                    + SystemParameters.WorkArea.Top;
            }
            else
            {
                System.Windows.Forms.Screen[] Screens = System.Windows.Forms.Screen.AllScreens;

                if (screenIndex > Screens.GetUpperBound(0))
                    screenIndex = 0;

                ret.X = Screens[screenIndex].WorkingArea.Left + ((Screens[screenIndex].WorkingArea.Width - width) / 2.0);
                ret.Y = Screens[screenIndex].WorkingArea.Top + ((Screens[screenIndex].WorkingArea.Height - height) / 2.0);
            }

            return ret;
        }

        public static bool CopyDirectoryOverwrite(string SourcePath, string DestinationPath, bool OverwriteExisting)
        {
            bool ret = false;
            try
            {
                SourcePath = SourcePath.EndsWith(@"\") ? SourcePath : SourcePath + @"\";
                DestinationPath = DestinationPath.EndsWith(@"\") ? DestinationPath : DestinationPath + @"\";

                if (Directory.Exists(SourcePath))
                {
                    if (Directory.Exists(DestinationPath) == false)
                        Directory.CreateDirectory(DestinationPath);

                    foreach (string fls in Directory.GetFiles(SourcePath))
                    {
                        try
                        {
                            FileInfo flinfo = new FileInfo(fls);
                            flinfo.CopyTo(DestinationPath + flinfo.Name, OverwriteExisting);
                        }
                        catch { }
                    }

                    foreach (string drs in Directory.GetDirectories(SourcePath))
                    {
                        DirectoryInfo drinfo = new DirectoryInfo(drs);
                        if (CopyDirectoryOverwrite(drs, DestinationPath + drinfo.Name, OverwriteExisting) == false)
                            ret = false;
                    }
                }
                ret = true;
            }
            catch (Exception ex)
            {
                ret = false;
            }
            return ret;
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <returns>The relative path from the start directory to the end path or <c>toPath</c> if the paths are not related.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) { return toPath; } // path can't be made relative.

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        #region Images

        public static BitmapSource GetFileThumbnail(string file)
        {
            //check for url
            //TODO: load favicon?
            if (Uri.IsWellFormedUriString(file, UriKind.Absolute))
            {
                return new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/web.png"));
            }

            string Extension = System.IO.Path.GetExtension(file).ToLower();

            BitmapSource bmps = null;

            //load thumbnails for image files
            if (Extension == ".png" || Extension == ".jpg" || Extension == ".jpeg" || Extension == ".bmp" || Extension == ".gif" || Extension == ".ico")
            {
                try
                {
                    bmps = LoadBitmapImage(file, 128);
                }
                catch { }
            }

            if (bmps == null)
            {
                bmps = GetIcon.FromPath(file);
            }

            return bmps;
        }

        public static BitmapSource DecoupleBitmap(BitmapSource bmp)
        {
            int height = bmp.PixelHeight;
            int width = bmp.PixelWidth;
            int stride = width * ((bmp.Format.BitsPerPixel + 7) / 8);

            byte[] bits = new byte[height * stride];
            bmp.CopyPixels(bits, stride, 0);

            WriteableBitmap DecoupledBitmap = new WriteableBitmap(width, height, bmp.DpiX, bmp.DpiY, bmp.Format, bmp.Palette);
            DecoupledBitmap.WritePixels(new Int32Rect(0, 0, width, height), bits, stride, 0);

            if (DecoupledBitmap.CanFreeze)
                DecoupledBitmap.Freeze();

            return (BitmapSource)DecoupledBitmap;
        }

        public static void SaveBitmapImage(BitmapSource image, string path)
        {
            try
            {
                FileStream stream = new FileStream(path, FileMode.Create);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Interlace = PngInterlaceOption.On;
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                stream.Close();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static BitmapSource LoadBitmapImage(string path, int DecodePixelWidth = -1)
        {
            if (!System.IO.File.Exists(path))
                return null;

            try
            {
                MemoryStream ms = new MemoryStream();
                FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                ms.SetLength(stream.Length);
                stream.Read(ms.GetBuffer(), 0, (int)stream.Length);

                ms.Flush();
                stream.Close();

                BitmapImage src = new BitmapImage();
                src.BeginInit();
                src.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

                if (DecodePixelWidth != -1)
                    src.DecodePixelWidth = DecodePixelWidth;

                src.CacheOption = BitmapCacheOption.OnLoad;
                src.StreamSource = ms;
                src.EndInit();

                if (src.CanFreeze)
                    src.Freeze();

                return DecoupleBitmap(src);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static double RemapRangeClamped(double value, double from1, double to1, double from2, double to2)
        {
            if (value < from1)
                return from2;

            if (value > to1)
                return to2;

            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static BitmapSource BlurImage(BitmapSource SourceImage, double radius)
        {
            if (SourceImage == null)
                return null;

            try
            {
                //smart scale for blur
                double scale = RemapRangeClamped(radius, 0.0, 20.0, 1.0, 0.1);

                //compenstate for scale
                radius *= RemapRangeClamped(scale, 0.1, 1.0, 1.0, 3.0);

                //construct scene
                Grid maingrid = new Grid();
                //maingrid.Margin = new Thickness(-radius);

                //Image img = new Image();
                //img.Source = SourceImage;
                //img.VerticalAlignment = VerticalAlignment.Center;
                //img.HorizontalAlignment = HorizontalAlignment.Center;
                //img.Stretch = Stretch.UniformToFill;

                ImageBrush imgbrush = new ImageBrush(SourceImage);
                imgbrush.TileMode = TileMode.FlipXY;
                imgbrush.ViewportUnits = BrushMappingMode.Absolute;
                imgbrush.Viewport = new Rect(0.0, 0.0, SourceImage.PixelWidth * scale, SourceImage.PixelHeight * scale);
                imgbrush.AlignmentX = AlignmentX.Center;

                Grid rendergrid = new Grid();
                rendergrid.Width = SourceImage.PixelWidth * 3;
                rendergrid.Height = SourceImage.PixelHeight * 3;
                rendergrid.Margin = new Thickness(-SourceImage.PixelWidth * scale, -SourceImage.PixelHeight * scale, 0, 0);

                rendergrid.Background = imgbrush;
                maingrid.Children.Add(rendergrid);

                maingrid.Width = SourceImage.PixelWidth * scale;
                maingrid.Height = SourceImage.PixelHeight * scale;

                maingrid.RenderTransform = new ScaleTransform(-1.0, -1.0, maingrid.Width / 2.0, maingrid.Height / 2.0);

                BlurEffect blur = new BlurEffect();
                blur.KernelType = KernelType.Gaussian;
                blur.Radius = radius;

                rendergrid.Effect = blur;

                //render
                double Width = SourceImage.PixelWidth * scale;
                double Height = SourceImage.PixelHeight * scale;

                maingrid.UpdateLayout();
                maingrid.Arrange(new Rect(0.0, 0.0, Width, Height));
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)Width, (int)Height, 96.0, 96.0, PixelFormats.Pbgra32);

                rtb.Render(maingrid);

                if (rtb.CanFreeze)
                    rtb.Freeze();

                return DecoupleBitmap(rtb);
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                System.Windows.MessageBox.Show(ex.Message);

                return null;
            }
        }

        #endregion Images

        #region Wallpaper stuff

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 SystemParametersInfo(UInt32 action,
            UInt32 uParam, string vParam, UInt32 winIni);

        private static readonly UInt32 SPI_GETDESKWALLPAPER = 0x73;
        private static uint MAX_PATH = 260;

        public static string GetWallpaperPath()
        {
            try
            {
                string wallpaper = new string('\0', (int)MAX_PATH);
                SystemParametersInfo(SPI_GETDESKWALLPAPER, MAX_PATH, wallpaper, 0);
                //RegistryKey rkWallPaper = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", false);
                //string WallpaperPath = rkWallPaper.GetValue("WallPaper").ToString();

                return wallpaper.Substring(0, wallpaper.IndexOf('\0'));
            }
            catch
            {
                return null;
            }
        }

        public static BitmapSource GetCurrentWallpaper()
        {
            try
            {
                //loads and freezes the image
                return MiscUtils.LoadBitmapImage(GetWallpaperPath());
            }
            catch { }

            return null;
        }

        #endregion Wallpaper stuff

        public static string SavePathString(string Input)
        {
            return System.Text.RegularExpressions.Regex.Replace(Input, @"[\\/:*?""<>|]", string.Empty);
        }

        public static System.Windows.Input.Key WinformsToWPFKey(System.Windows.Forms.Keys inputKey)
        {
            try
            {
                return (System.Windows.Input.Key)Enum.Parse(typeof(System.Windows.Input.Key), inputKey.ToString());
            }
            catch
            {
                // There wasn't a direct mapping...
                return System.Windows.Input.Key.None;
            }
        }

        #region TreeView

        /// <summary>
        /// Walks the tree items to find the node corresponding with
        /// the given item, then sets it to be selected.
        /// </summary>
        /// <param name="treeView">The tree view to set the selected
        /// item on</param>
        /// <param name="item">The item to be selected</param>
        /// <returns><c>true</c> if the item was found and set to be
        /// selected</returns>
        static public bool SetSelectedItem(
            this System.Windows.Controls.TreeView treeView, object item)
        {
            return SetSelected(treeView, item);
        }

        static private bool SetSelected(ItemsControl parent,
            object child)
        {
            if (parent == null || child == null)
            {
                return false;
            }

            TreeViewItem childNode = parent.ItemContainerGenerator
                .ContainerFromItem(child) as TreeViewItem;

            if (childNode != null)
            {
                childNode.Focus();
                return childNode.IsSelected = true;
            }

            if (parent.Items.Count > 0)
            {
                foreach (object childItem in parent.Items)
                {
                    ItemsControl childControl = parent
                        .ItemContainerGenerator
                        .ContainerFromItem(childItem)
                        as ItemsControl;

                    if (SetSelected(childControl, child))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion TreeView

        #region String utils

        public static string GetLionFormattedText(string text, FontFamily font, double size, double MaxWidth)
        {
            string formattedText = text;
            FormattedText fmt = new FormattedText(formattedText, new System.Globalization.CultureInfo("en"), System.Windows.FlowDirection.LeftToRight, new Typeface(font.ToString()), size, Brushes.White);
            fmt.MaxLineCount = 1;
            int removedchars = 2;

            while (fmt.WidthIncludingTrailingWhitespace > MaxWidth)
            {
                //trimm text
                string part1 = text.Substring(0, text.Length / 2 - removedchars);
                string part2 = text.Substring(text.Length / 2 + removedchars);
                formattedText = part1 + "..." + part2;
                removedchars++;

                if (formattedText == "...")
                    return "";

                fmt = new FormattedText(formattedText, new System.Globalization.CultureInfo("en"), System.Windows.FlowDirection.LeftToRight, new Typeface(font.ToString()), size, Brushes.White);
                fmt.MaxLineCount = 1;
            }

            return formattedText;
        }

        #endregion String utils

        #region Directory Utils

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        #endregion Directory Utils

        public static void CreateFileShortcut(string file, string directory)
        {
            string shortcutAddress = Path.Combine(directory, Path.GetFileNameWithoutExtension(file) + ".lnk");

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutAddress);

            shortcut.TargetPath = file;
            shortcut.Save();
        }
    }

    internal static class MathHelper
    {
        public static double Clamp(double num, double min = 0.0, double max = 1.0)
        {
            return (((num < min) ? min : num) > max) ? max : ((num < min) ? min : num);
        }

        public static double Animate(double Progress, double Start, double End)
        {
            return Start + (End - Start) * Clamp(Progress);
        }

        public static double GetDistance(double X1, double X2)
        {
            return Math.Max(X1, X2) - Math.Min(X1, X2);
        }

        public static bool IsPointInArea(Point pos, Point center, double areasize)
        {
            if (pos.X > center.X - areasize && pos.X < center.X + areasize)
            {
                if (pos.Y > center.Y - areasize && pos.Y < center.Y + areasize)
                {
                    return true;
                }
            }

            return false;
        }

        public static Point NormPoint(Point pos, double Width, double Height)
        {
            return new Point(pos.X / Width, pos.Y / Height);
        }

        public static double ToEven(double num)
        {
            int roundnum = (int)Math.Floor(num);
            if ((roundnum & 1) == 1)
            {
                //odd number
                roundnum++;
            }

            return (double)roundnum;
        }

        public static string Sha256(string input)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input), 0, System.Text.Encoding.UTF8.GetByteCount(input));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }
    }

    public static class ExplorerContextMenu
    {
        //Extension - Extension of the file (.zip, .txt etc.)
        //MenuName - Name for the menu item (Play, Open etc.)
        //MenuDescription - The actual text that will be shown
        //MenuCommand - Path to executable
        public static bool AddContextMenuItem(string Extension,
          string MenuName, string MenuDescription, string MenuCommand, string IconPath)
        {
            try
            {
                bool ret = false;
                RegistryKey rkey = Registry.ClassesRoot.OpenSubKey(Extension + "\\shell", true);

                if (rkey != null)
                {
                    //check if key already exists
                    RegistryKey akey = rkey.OpenSubKey(MenuName, true);

                    if (akey != null)
                    {
                        //delete key
                        rkey.DeleteSubKeyTree(MenuName);
                    }

                    //create application key
                    RegistryKey subky = rkey.CreateSubKey(MenuName);

                    if (subky != null)
                    {
                        subky.SetValue("", MenuDescription);

                        if (IconPath != null)
                        {
                            subky.SetValue("Icon", IconPath);
                        }

                        RegistryKey commandkey = subky.CreateSubKey("command");
                        subky.Close();

                        if (commandkey != null)
                        {
                            commandkey.SetValue("", MenuCommand);
                            commandkey.Close();
                        }

                        ret = true;
                    }

                    rkey.Close();
                }

                return ret;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    public static class DebugLog
    {
        public static string LogFile = "DebugLog.log";

        public static void WriteToLogFile(string logMessage)
        {
            return;

            string strLogMessage = string.Empty;
            string strLogFile = LogFile;
            StreamWriter swLog;

            strLogMessage = string.Format("{0}: {1}", DateTime.Now.ToString("G"), logMessage);

            if (!System.IO.File.Exists(strLogFile))
            {
                swLog = new StreamWriter(strLogFile);
            }
            else
            {
                swLog = System.IO.File.AppendText(strLogFile);
            }

            swLog.WriteLine(strLogMessage);
            swLog.WriteLine();

            swLog.Close();
        }
    }

    public static class CrashReporter
    {
        public static bool Enabled = true;

        public static void Report(object ex,
                    [CallerMemberName] string sourceMemberName = "",
                    [CallerFilePath] string sourceFilePath = "",
                    [CallerLineNumber] int sourceLineNo = 0)
        {
            if (!Enabled)
                return;

            try
            {
                Exception innerException = null;

                try
                {
                    innerException = (ex as Exception).InnerException;
                }
                catch { }

                sourceFilePath = Path.GetFileName(sourceFilePath);

                string environment = "CurrentDirectory: " + Environment.CurrentDirectory +
                    "\nHasShutdownStarted: " + Environment.HasShutdownStarted +
                    "\nMachineName: " + Environment.MachineName +
                    "\nCommandLine: " + Environment.CommandLine +
                    "\nOSVersion: " + Environment.OSVersion.ToString() +
                    "\nSystemDirectory: " + Environment.SystemDirectory +
                    "\nUserDomainName: " + Environment.UserDomainName +
                    "\nUserInteractive: " + Environment.UserInteractive +
                    "\nUserName: " + Environment.UserName +
                    "\nVersion: " + Environment.Version.ToString() +
                    "\nWorkingSet: " + Environment.WorkingSet;

                string url = "https://WinLaunch.org/reports/crashreport.php";
                string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                string info = "environment:\n" + environment + "\n\nsource:\n" + sourceFilePath + " (" + sourceLineNo + ") " + sourceMemberName + "\n\nexception:\n" + ex.ToString() + "\n\ninnerexception:\n" + ((object)innerException ?? "null").ToString();

                using (var wb = new WebClient())
                {
                    var data = new NameValueCollection();
                    data["version"] = version;
                    data["info"] = info;

                    var response = wb.UploadValues(url, "POST", data);
                }
            }
            catch { }
        }
    }
}