using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace WinLaunch
{
    /// <summary>
    /// This class implements replacing the StartWindow with WinLaunch.
    /// </summary>
    /// <remarks>
    /// This class works by monitoring the start menu state using the shells IAppVisibility COM interface. 
    /// </remarks>
    internal class StartWindowActivation
    {
        private ComReleaser<IAppVisibility> appVisibility;
        private AppVisibilityNotificationSubscriber subscriber;
        private uint unadviseCookie;
        private Dispatcher dispatcher;

        /// <summary>
        /// Start monitoring the state of the Start window
        /// </summary>
        /// <param name="callback">callback to call when state changes</param>
        /// <remarks>NOTE: The callback will be called on the Dispatcher thread, so you can make UI calls from the callback.</remarks>
        public void StartListening(Action<bool> callback)
        {
            dispatcher = Dispatcher.CurrentDispatcher; 

            Debug.WriteLine($"{nameof(StartWindowActivation)} Start Listening");

            appVisibility = ComReleaserFactory.Create(new IAppVisibility());
            subscriber = new AppVisibilityNotificationSubscriber((visible) =>
            {
                // use the dispatcher to call the callback on the callers UI thread.
                dispatcher.Invoke(() => callback(visible));
            });

            // Advise to receive change notifications from the AppVisibility object
            // NOTE: There must be a reference held on the AppVisibility object in order to continue
            appVisibility.Item.Advise(subscriber, out var cookie);
            unadviseCookie = cookie;
        }

        /// <summary>
        /// StopListening() to state of the Start window
        /// </summary>
        public void StopListening()
        {
            Debug.WriteLine($"{nameof(StartWindowActivation)} Stop Listening");

            if (appVisibility != null)
            {
                // Unadvise from the AppVisibility component to stop receiving notifications
                if (this.unadviseCookie != 0)
                {
                    appVisibility.Item.Unadvise(this.unadviseCookie);
                }
                appVisibility.Dispose();
                appVisibility = null;
            }
        }
    }

    /// <summary>
    ///  This class implements the IAppVisibilityEvents interface and will receive notifications from the AppVisibility COM object.
    /// </summary>
    [ComVisible(true)]
    public class AppVisibilityNotificationSubscriber : IAppVisibilityEvents
    {
        private Action<bool> _callback;

        public AppVisibilityNotificationSubscriber(Action<bool> callback)
        {
            this._callback = callback;
        }

        HRESULT IAppVisibilityEvents.AppVisibilityOnMonitorChanged(HMONITOR hMonitor, MONITOR_APP_VISIBILITY previousMode, MONITOR_APP_VISIBILITY currentMode)
        {
            return HRESULT.S_OK;
        }

        HRESULT IAppVisibilityEvents.LauncherVisibilityChange(bool visible)
        {
            Debug.WriteLine($"Start Menu visibility: {visible}");
            _callback(visible);
            return HRESULT.S_OK;
        }
    }
}
