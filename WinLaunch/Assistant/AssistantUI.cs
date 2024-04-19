using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    public class AssistantPromptSuggestion
    {
        public BitmapSource Icon { get; set; }
        public string Prompt { get; set; }
    }

    public class AssistantMessageHeader : DependencyObject
    {
        public string Name { get; set; } = TranslationSource.Instance["AssistantName"];
        public BitmapSource Icon { get; set; } = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/icon.png"));

        public AssistantMessageHeader(bool user = false)
        {
            if (user)
            {
                Name = TranslationSource.Instance["AssistantYou"];
                Icon = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/assistant/user.png"));
            }
        }
    }

   
    partial class MainWindow : Window
    {
        #region ModeSwitcher
        public bool proMode = false;

        public void InitializeAssistantModeSwitcher()
        {
            SetMode(pro: Settings.CurrentSettings.AssistantProMode);
        }

        public void SetMode(bool pro)
        {
            if (proMode == pro)
                return;

            proMode = pro;

            DoubleAnimation fadeOutAnimation = new DoubleAnimation();
            fadeOutAnimation.To = 0.0;
            fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(200);


            DoubleAnimation fadeInAnimation = new DoubleAnimation();
            fadeInAnimation.To = 1.0;
            fadeInAnimation.Duration = TimeSpan.FromMilliseconds(200);

            if (proMode)
            {
                bdProMode.BeginAnimation(OpacityProperty, fadeInAnimation);
                bdBasicMode.BeginAnimation(OpacityProperty, fadeOutAnimation);

                tblModeBasic.Opacity = 0.5;
                tblModePro.Opacity = 1.0;
            }
            else
            {
                bdProMode.BeginAnimation(OpacityProperty, fadeOutAnimation);
                bdBasicMode.BeginAnimation(OpacityProperty, fadeInAnimation);

                tblModeBasic.Opacity = 1.0;
                tblModePro.Opacity = 0.5;
            }

            //save chosen mode in settings
            if (Settings.CurrentSettings.AssistantProMode != proMode)
            {
                Settings.CurrentSettings.AssistantProMode = proMode;
                Settings.SaveSettings(Settings.CurrentSettings);
            }
        }

        private void tblModeBasic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetMode(pro: false);
        }

        private void tblModePro_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetMode(pro: true);
        }
        #endregion

        void HideAssistantUIPanels()
        {
            //the mode switcher 
            bdAssistantModeSwitcher.Visibility = Visibility.Hidden;

            //the prompt suggestions
            icPromptSuggestions.Visibility = Visibility.Hidden;

            //the assistant input box
            bdAssistantControls.Visibility = Visibility.Hidden;

            //the assistant header
            spAssistantHeader.Visibility = Visibility.Hidden;

            //the assistant messages
            scvAssistant.Visibility = Visibility.Hidden;

            //the enter username UI 
            spAssistantLogin.Visibility = Visibility.Hidden;

            //the connecting animation UI
            spAssistantConnecting.Visibility = Visibility.Hidden;

            //the manual reconnect UI 
            spAssistantManualReconnect.Visibility = Visibility.Hidden;
        }

        private void ClearAssistantChat()
        {
            icAssistantContent.Items.Clear();

            spAssistantHeader.VerticalAlignment = VerticalAlignment.Center;
            tbAssistantHeader.Text = TranslationSource.Instance["AssistantWelcomeMessage"];
            imAssistantIcon.Width = 100;

            icPromptSuggestions.Visibility = Visibility.Visible;
        }

        private void ShowChatUI()
        {
            scvAssistant.Visibility = Visibility.Visible;
            bdAssistantControls.Visibility = Visibility.Visible;
            spAssistantHeader.Visibility = Visibility.Visible;

            if (icAssistantContent.Items.Count == 0)
            {
                bdAssistantModeSwitcher.Visibility = Visibility.Visible;
                icPromptSuggestions.Visibility = Visibility.Visible;

                //hide tiers when showing the welcome message
                bdRewardTierPro.Visibility = Visibility.Collapsed;
                bdRewardTierBasic.Visibility = Visibility.Collapsed;
            }
            else
            {
                bdAssistantModeSwitcher.Visibility = Visibility.Hidden;
                icPromptSuggestions.Visibility = Visibility.Hidden;
            }

            Keyboard.Focus(tbAssistant);
        }

        private void ShowAssistantUI()
        {
            AssistantActive = true;

            gdAssistant.Visibility = Visibility.Visible;
            bdSearchContainer.Visibility = Visibility.Collapsed;
            MainCanvas.Visibility = Visibility.Hidden;
            PageCounterWrap.Visibility = Visibility.Hidden;

            spSearchAndAssistantContainer.Visibility = Visibility.Collapsed;

            imHideAssistantWindow.Visibility = Visibility.Visible;
            imCloseAssistant.Visibility = Visibility.Visible;

            bdAssistantModeSwitcher.Visibility = Visibility.Visible;

            tbSearch.IsEnabled = false;
        }

        private void HideAssistant()
        {
            AssistantActive = false;

            gdAssistant.Visibility = Visibility.Hidden;
            bdSearchContainer.Visibility = Visibility.Visible;
            MainCanvas.Visibility = Visibility.Visible;
            PageCounterWrap.Visibility = Visibility.Visible;

            spSearchAndAssistantContainer.Visibility = Visibility.Visible;

            imHideAssistantWindow.Visibility = Visibility.Hidden;
            imCloseAssistant.Visibility = Visibility.Hidden;

            bdAssistantModeSwitcher.Visibility = Visibility.Hidden;

            tbSearch.IsEnabled = true;
        }
    }
}
