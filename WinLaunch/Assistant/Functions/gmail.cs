using SocketIOClient;
using System;
using System.Windows;

namespace WinLaunch
{
    public class AssistantGmailMessagesListed : DependencyObject
    {
        public string username { get; set; }
        public int count { get; set; }
        public string query { get; set; }
    }

    public class AssistantGmailMessageSent : DependencyObject
    {
        public string username { get; set; }
        public string to { get; set; }
        public string subject { get; set; }
        public string message { get; set; }
    }

    partial class MainWindow : Window
    {
        void get_gmail_messages(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var username = args.GetValue<string>();
                    var count = args.GetValue<int>(1);
                    var query = args.GetValue<string>(2);

                    icAssistantContent.Items.Add(new AssistantGmailMessagesListed()
                    {
                        username = username,
                        count = count,
                        query = query
                    });

                    MovePendingIndicatorToBottom();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }

        void sent_gmail_message(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var username = args.GetValue<string>();
                    var to = args.GetValue<string>(1);
                    var subject = args.GetValue<string>(2);
                    var message = args.GetValue<string>(3);

                    icAssistantContent.Items.Add(new AssistantGmailMessageSent()
                    {
                        username = username,
                        to = to,
                        subject = subject,
                        message = message
                    });

                    MovePendingIndicatorToBottom();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }
    }
}
