using System;
using System.Collections.Generic;
using System.Windows;

namespace WinLaunch
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    partial class App : Application
    {
        public App()
        {
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {

            bool Hide = false;
            bool Add = false;

            try
            {
                foreach (string arg in e.Args)
                {
                    if (arg.ToLower() == "-add")
                    {
                        WinLaunch.MainWindow.AddFiles = new List<string>();
                        Add = true;
                    }
                    else if (arg.ToLower() == "-hide")
                    {
                        Hide = true;
                    }
                    else
                    {
                        if (Add)
                        {
                            WinLaunch.MainWindow.AddFiles.Add(arg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReporter.Report(ex);
                MessageBox.Show(ex.Message);
            }

            WinLaunch.MainWindow.StartHidden = Hide;

        }
    }
}