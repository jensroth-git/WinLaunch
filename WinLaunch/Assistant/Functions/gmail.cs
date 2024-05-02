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
        public bool expandable { get; set; }
        public string username { get; set; }
        public string id { get; set; }
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
            if (!expandable)
            {
                return;
            }

            MainWindow window = (MainWindow)MainWindow.WindowRef;
            
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    window.AssistantClient.EmitAsync("get_gmail_message_details", username, id);
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
            public string BodyText { get; set; }
            public string BodyHTML { get; set; }
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

                    //create expandable messages
                    foreach (var message in messages)
                    {
                        CreateGmailMessageUI(username, message, true);
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
                    var username = args.GetValue<string>();
                    var message = args.GetValue<GmailMessage>(1);

                    CreateGmailMessageUI(username, message);

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }

        private void CreateGmailMessageUI(string username, GmailMessage message, bool expandable = false)
        {
            string dateString = message.Date;

            DateTime date;
            if(DateTime.TryParse(message.Date, out date))
            {
                dateString = date.ToString("ddd, MMM d, yyyy h:mm tt");
            }

            var UImessage = new AssistantGmailMessage()
            {
                expandable = expandable,
                username = username,
                id = message.Id,
                from = message.From,
                to = message.To,
                date = dateString,
                subject = message.Subject
            };

            if(message.BodyText == null)
            {
                if(message.BodyHTML != null)
                {
                    //TODO: html
                    UImessage.body = "message contains HTML only!";
                }
                else
                {
                    UImessage.body = message.Snippet;
                }
            }
            else
            {
                UImessage.body = message.BodyText;
            }

            icAssistantContent.Items.Add(UImessage);
        }
    }
}
