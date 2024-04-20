using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace WinLaunch
{
    public class AssistantCalendarEventsListed : DependencyObject
    {
        public string username { get; set; }
        public string date { get; set; }
    }
    public class AssistantCalendarEvent : DependencyObject
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public SolidColorBrush Color { get; set; }
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
                    var events = args.GetValue<string>(2);


                    //print to console
                    Console.WriteLine("Username: " + username);
                    Console.WriteLine("Date: " + date);
                    Console.WriteLine("Events: " + events);

                    //parse events json
                    var eventsJson = JArray.Parse(events);

                    //parse date to datetime
                    DateTime parsedDate = DateTime.Parse(date);

                    icAssistantContent.Items.Add(new AssistantCalendarEventsListed()
                    {
                        username = username,
                        date = parsedDate.ToShortDateString()
                    });
                    foreach (var item in eventsJson)
                    {
                        JObject obj = item as JObject;

                        //check if description is exist in the event
                        var keys = obj.Properties().Select(p => p.Name).ToList();

                        //if description is not in add it
                        if (!keys.Contains("description"))
                        {
                            obj["description"] = "";
                        }

                        //fields to show user
                        string datePreview = String.Empty;
                        string timePreview = String.Empty;

                        try
                        {
                            //if the event is all day
                            datePreview = item["start"]["date"].ToString() + " - " + item["end"]["date"].ToString();
                            timePreview = "All Day";
                        }
                        catch
                        {
                            //if the event is not all day
                            string StartdateTimeString = item["start"]["dateTime"].ToString();
                            DateTime dateTime = DateTime.Parse(StartdateTimeString);
                            string startTime = dateTime.ToString("HH:mm");
                            string startDate = dateTime.ToShortDateString();

                            string EnddateTimeString = item["end"]["dateTime"].ToString();
                            DateTime endDateTime = DateTime.Parse(EnddateTimeString);
                            string endTime = endDateTime.ToString("HH:mm");
                            string endDate = endDateTime.ToShortDateString();

                            timePreview = startTime + " - " + endTime;
                            datePreview = startDate + " - " + endDate;
                        }

                        icAssistantContent.Items.Add(new AssistantCalendarEvent()
                        {
                            Title = obj["summary"].ToString(),
                            //Description = obj["description"].ToString(),
                            Description = obj.ToString(),
                            Time = timePreview,
                            Date = datePreview,
                            Color = new SolidColorBrushu
                        });
                    }


                    AdjustAssistantMessageSpacing();
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

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }
    }
}
