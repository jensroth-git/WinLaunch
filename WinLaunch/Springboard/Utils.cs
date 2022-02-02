using System.Windows;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
       /* private bool EnableSynaptics()
        {
            try
            {
                if (!SynapticsManager.Start())
                {
                    if (MessageBox.Show(WinLaunch.Language.CurrentLanguage.SynapticsErrorRetry, WinLaunch.Language.CurrentLanguage.Error, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        //retry
                        return EnableSynaptics();
                    }

                    return false;
                }
            }
            catch //(Exception ex)
            {
                //MessageBox.Show(ex.Message);
                return false;
            }

            return true;
        }

        private bool DisableSynaptics()
        {
            try
            {
                SynapticsManager.Stop();
            }
            catch
            {
                return false;
            }

            return true;
        }*/
    }
}