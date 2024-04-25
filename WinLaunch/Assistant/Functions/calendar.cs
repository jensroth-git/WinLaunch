using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.UI.HtmlControls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WinLaunch.Utils;

namespace WinLaunch
{
    public class AssistantCalendarEventsListed : DependencyObject
    {
        public string username { get; set; }
        public string date { get; set; }
    }
    public class AssistantCalendarEvent : DependencyObject
    {
        public AssistantCalendarEvent()
        {
            OpenUriCommand = new RelayCommand(ExecuteOpenUri);
        }

        public string Title { get; set; }

        public Visibility DescriptionVisibility { get; set; } = Visibility.Collapsed;
        private string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                // Update DescriptionVisibility based on whether the description is not null or empty.
                DescriptionVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility LocationVisibility { get; set; } = Visibility.Collapsed;
        private string _location;
        public string Location
        {
            get { return _location; }
            set { 
                _location = value;
                LocationVisibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public string AttendeesDescription { get; set; }
        public Visibility AttendeesVisibility { get; set; } = Visibility.Collapsed;
        private List<string> _attendees;
        public List<string> Attendees
        {
            get { return _attendees; }
            set
            {
                bool any = (value != null && value.Count > 0);
                _attendees = value;
                AttendeesVisibility = any ? Visibility.Visible : Visibility.Collapsed;

                if(any)
                {
                    //TODO: multilanguage
                    AttendeesDescription = _attendees.Count + " Attendee" + (_attendees.Count > 1 ? "s" : "");
                }
            }
        }

        public string Time { get; set; }
        public string Date { get; set; }
        public SolidColorBrush Color { get; set; }
        public string htmlLink { get; set; }
        public ICommand OpenUriCommand { get; private set; }
        private void ExecuteOpenUri()
        {
            // Example action
            Trace.WriteLine("Opening calendar event link: " + Title + "\n" + htmlLink);
            // You might want to open the link here
            MainWindow window = (MainWindow)MainWindow.WindowRef;
            window.StartFlyOutAnimation();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Process.Start(htmlLink);

                }
                catch { }
            }));

        }
    }

    public class AssistantAddedCalendarEvent : DependencyObject
    {
        public string Text { get; set; }
    }

    partial class MainWindow : Window
    {
        class CalendarEvent
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string StartDateTime { get; set; }
            public string EndDateTime { get; set; }
            public string Link { get; set; }
            public bool IsAllDay { get; set; }
            public string Location { get; set; }
            public List<string> Attendees { get; set; }
        }

        void get_calendar_events(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var username = args.GetValue<string>();
                    var date = args.GetValue<string>(1);
                    var events = args.GetValue<List<CalendarEvent>>(2);

                    //parse date to datetime
                    DateTime parsedDate = DateTime.Parse(date);

                    icAssistantContent.Items.Add(new AssistantCalendarEventsListed()
                    {
                        username = username,
                        date = parsedDate.ToShortDateString()
                    });

                    //for loop in events list
                    foreach (var item in events)
                    {
                        //MessageBox.Show(item.Title+"\n\n"+item.Description+"\n"+item.StartDateTime + " - " + item.EndDateTime);
                        CreateCalendarEntryUI(item);
                    }

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch (Exception ex)
                {
                    Debugger.Break();
                }
            }));
        }

        private void CreateCalendarEntryUI(CalendarEvent item)
        {
            var datePreview = String.Empty;
            var timePreview = String.Empty;

            DateTime startDateTime = DateTime.Parse(item.StartDateTime);
            DateTime endDateTime = DateTime.Parse(item.EndDateTime);

            string startTime = startDateTime.ToString("HH:mm");
            string endTime = endDateTime.ToString("HH:mm");

            string startDate = startDateTime.ToShortDateString();
            string endDate = endDateTime.ToShortDateString();

            if (item.IsAllDay)
            {
                timePreview = "All Day";
            }
            else
            {
                timePreview = startTime + " - " + endTime;
            }
            if (startDate == endDate)
            {
                datePreview = startDate;
            }
            else
            {
                datePreview = startDate + " - " + endDate;
            }

            icAssistantContent.Items.Add(new AssistantCalendarEvent()
            {
                Title = item.Title,
                Description = item.Description,
                Location = item.Location,
                Attendees = item.Attendees,
                Time = timePreview,
                Date = datePreview,
                Color = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#88ffffff")),
                htmlLink = item.Link
            });
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

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }
    }
}
