using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.UI.HtmlControls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinLaunch.Utils;

namespace WinLaunch
{
    public class AssistantGmailMessagesListed : DependencyObject
    {
        public string username { get; set; }
        public int count { get; set; }
        public string query { get; set; }
    }

    public class AssistantGmailMessage : DependencyObject
    {
        public string from { get; set; }
        public string to { get; set; }
        public string date { get; set; }
        public string subject { get; set; }
        public string body { get; set; }

        public AssistantGmailMessage()
        {
            OpenUriCommand = new RelayCommand(ExecuteOpenUri);
        }

        public ICommand OpenUriCommand { get; private set; }
        private void ExecuteOpenUri()
        {
            MainWindow window = (MainWindow)MainWindow.WindowRef;
            window.StartFlyOutAnimation();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    //Process.Start(htmlLink);
                }
                catch { }
            }));

        }
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
        class GmailMessage
        {
            public string Id { get; set; }
            public string From { get; set; }
            public string To { get; set; }
            public string Date { get; set; }
            public string Subject { get; set; }
            public string Snippet { get; set; }
            public string Body { get; set; }
        }


        void get_gmail_messages(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var username = args.GetValue<string>();
                    var count = args.GetValue<int>(1);
                    var query = args.GetValue<string>(2);
                    var messages = args.GetValue<List<GmailMessage>>(3);

                    icAssistantContent.Items.Add(new AssistantGmailMessagesListed()
                    {
                        username = username,
                        count = count,
                        query = query
                    });

                    foreach (var message in messages)
                    {
                        CreateGmailMessageUI(message);
                    }

                    AdjustAssistantMessageSpacing();
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

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }


        void get_gmail_message_details(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var message = args.GetValue<GmailMessage>();

                    CreateGmailMessageUI(message);

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }

        private void CreateGmailMessageUI(GmailMessage message)
        {
            string dateString = message.Date;

            DateTime date;
            if(DateTime.TryParse(message.Date, out date))
            {
                dateString = date.ToString("ddd, MMM d, yyyy h:mm tt");
            }

            icAssistantContent.Items.Add(new AssistantGmailMessage()
            {
                from = message.From,
                to = message.To,
                date = dateString,
                subject = message.Subject,
                body = (message.Body == null) ? message.Snippet : message.Body
            });
        }
    }
}
