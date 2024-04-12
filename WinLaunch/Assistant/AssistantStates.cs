using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        public enum AssistantTier
        {
            Basic,
            Pro
        }

        public enum AssistantState
        {
            //The Assistant hasnt been opened yet and is not connected
            Unopened,

            //no credentials saved in settings
            //displays a message telling the user that they have to login
            Login,

            //displays a connecting animation
            Connecting,

            //chat UI is displayed
            Chat,

            //The assistant has been closed and the launchpad is visible
            //but stays connected for 15 minutes
            BackgroundClosed,

            //the assistant is still primarily displayed, but WinLaunch is hidden
            //stays connected for 15 minutes
            BackgroundHidden,

            //Disconnect socket and clear conversation (to save on tokens)
            BackgroundTimeout,

            //the Assistant is disconnected, once reopened it will change to the Connecting state
            Disconnected,

            //the Assistant is disconnected and requires a manual reconnect
            ManualReconnectRequired
        }

        AssistantState currentAssistantState = AssistantState.Unopened;

        DispatcherTimer assistantBackgroundTimer;

        void TransitionAssistantState(AssistantState newState)
        {
            if (newState == currentAssistantState)
                return;

            //transition state
            var previousState = currentAssistantState;
            currentAssistantState = newState;

            //ensure user is registered
            //check if registration is saved in settings
            if((newState == AssistantState.Connecting || newState == AssistantState.Chat))
            {
                if (string.IsNullOrEmpty(Settings.CurrentSettings.AssistantUsername) ||
                    string.IsNullOrEmpty(Settings.CurrentSettings.AssistantPassword))
                {
                    //username or password not stored in settings
                    TransitionAssistantState(AssistantState.Login);
                    return;
                }
            }

            if (newState == AssistantState.Login)
            {
                ShowAssistantUI();
                HideAssistantUIPanels();

                //display registration UI
                spAssistantLogin.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(tbxAssistantPassword.Password))
                    tbxAssistantPassword.Clear();

                Keyboard.Focus(tbxAssistantEmail);
            }
            else if (newState == AssistantState.Connecting)
            {
                if(AssistantClient != null && AssistantClient.Connected)
                {
                    //if we are connected, go directly to chat 
                    TransitionAssistantState(AssistantState.Chat);
                }
                else
                {
                    ShowAssistantUI();
                    HideAssistantUIPanels();

                    //display connecting UI
                    spAssistantConnecting.Visibility = Visibility.Visible;

                    ConnectAssistant();
                }
                
            }
            else if (newState == AssistantState.Chat)
            {
                StopAssistantBackgroundTimer();

                ShowAssistantUI();
                HideAssistantUIPanels();

                //display chat UI
                ShowChatUI();
            }
            else if (newState == AssistantState.BackgroundHidden)
            {
                if (AssistantClient != null && !AssistantClient.Connected)
                {
                    TransitionAssistantState(AssistantState.Disconnected);
                    return;
                }

                StartAssistantBackgroundTimer();
            }
            else if(newState == AssistantState.BackgroundClosed)
            {
                assistantSpeech.SpeakAsyncCancelAll();
                HideAssistant();

                if (AssistantClient != null && !AssistantClient.Connected)
                {
                    TransitionAssistantState(AssistantState.Disconnected);
                    return;
                }

                StartAssistantBackgroundTimer();
            }
            else if (newState == AssistantState.BackgroundTimeout)
            {
                StopAssistantBackgroundTimer();

                TransitionAssistantState(AssistantState.Disconnected);
            }
            else if(newState == AssistantState.Disconnected)
            {
                if(AssistantClient != null) 
                    AssistantClient.DisconnectAsync();
            }
            else if(newState == AssistantState.ManualReconnectRequired)
            {
                ShowAssistantUI();
                HideAssistantUIPanels();

                //display reconnect UI
                spAssistantManualReconnect.Visibility = Visibility.Visible;
            }
        }

        private void StopAssistantBackgroundTimer()
        {
            if (assistantBackgroundTimer != null)
            {
                assistantBackgroundTimer.Stop();
                assistantBackgroundTimer = null;
            }
        }

        private void StartAssistantBackgroundTimer()
        {
            if (assistantBackgroundTimer != null)
            {
                assistantBackgroundTimer.Stop();
                assistantBackgroundTimer = null;
            }

            assistantBackgroundTimer = new DispatcherTimer();
            assistantBackgroundTimer.Interval = TimeSpan.FromMinutes(15);
            assistantBackgroundTimer.Tick += (s, e) =>
            {
                assistantBackgroundTimer.Stop();

                TransitionAssistantState(AssistantState.BackgroundTimeout);
            };

            assistantBackgroundTimer.Start();
        }
    }
}
