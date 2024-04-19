using SocketIOClient;
using System;
using System.Windows;

namespace WinLaunch
{
    public class AssistantFailedFunction : DependencyObject
    {
        public string Text { get; set; }
        public string Function { get; set; }
        public string Args { get; set; }
    }

    public class AssistantPendingIndicator : DependencyObject
    {
    }


    partial class MainWindow : Window
    {
        void wrong_login(SocketIOResponse message)
        {
            //wrong login
            Dispatcher.BeginInvoke(new Action(() =>
            {
                //MessageBox.Show(TranslationSource.Instance["AssistantWrongPassword"]);

                //reset login information
                Settings.CurrentSettings.AssistantUsername = null;
                Settings.CurrentSettings.AssistantPassword = null;

                Settings.SaveSettings(Settings.CurrentSettings);

                //will be disconnected shortly
                TransitionAssistantState(AssistantState.Login);
            }));
        }

        void system_message(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    string message = args.GetValue<string>();

                    MessageBox.Show(message);
                }
                catch { }
            }));
        }

        void rate_limit(SocketIOResponse message)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    //if it is not an immediate response, add a spacer
                    var element = GetLastActualAssistantMessage();

                    if (!(element is AssistantMessageHeader))
                    {
                        icAssistantContent.Items.Add(new AssistantMessageSpacer());
                    }

                    string rateLimitMessage = message.GetValue<string>();

                    icAssistantContent.Items.Add(new AssistantMessageTextContent() { Text = rateLimitMessage });
                    icAssistantContent.Items.Add(new AssistantMessageFooter());

                    AssistantResponsePending = false;
                    AdjustAssistantMessageSpacing();
                    RemovePendingIndicator();
                    scvAssistant.ScrollToBottom();

                    if (Settings.CurrentSettings.AssistantTTS)
                    {
                        assistantSpeech.SpeakAsyncCancelAll();
                        assistantSpeech.SpeakAsync(rateLimitMessage);
                    }
                }
                catch { }
            }));
        }

        void failed_function(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var function = args.GetValue<string>();
                    var functionArgs = args.GetValue<string>(1);

                    icAssistantContent.Items.Add(new AssistantFailedFunction()
                    {
                        Text = "Failed to execute function",
                        Function = function,
                        Args = functionArgs
                    });

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();
                }
                catch { }
            }));
        }
    }
}
