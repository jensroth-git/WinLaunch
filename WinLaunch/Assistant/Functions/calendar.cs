using SocketIOClient;
using System;
using System.Windows;

namespace WinLaunch
{
    public class AssistantCalendarEventsListed : DependencyObject
    {
        public string username { get; set; }
        public string date { get; set; }
    }

    public class AssistantAddedCalendarEvent : DependencyObject
    {
        public string Text { get; set; }
    }

    partial class MainWindow : Window
    {
        void get_calendar_events(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var username = args.GetValue<string>();
                    var date = args.GetValue<string>(1);

                    //parse date to datetime
                    DateTime parsedDate = DateTime.Parse(date);

                    icAssistantContent.Items.Add(new AssistantCalendarEventsListed()
                    {
                        username = username,
                        date = parsedDate.ToShortDateString()
                    });

                    MovePendingIndicatorToBottom();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }

        void add_calendar_event(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    icAssistantContent.Items.Add(new AssistantAddedCalendarEvent()
                    {
                        Text = TranslationSource.Instance["AssistantAddedCalendarEvent"],
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
