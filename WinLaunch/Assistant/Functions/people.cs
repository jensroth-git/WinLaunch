using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.UI.HtmlControls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WinLaunch.Utils;

namespace WinLaunch
{
    public class AssistantPeopleContactsListed : DependencyObject
    {
        public string username { get; set; }

        public string info { get; set; }
    }
    public class AssistantPeopleContact : DependencyObject
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Number { get; set; }
    }

    partial class MainWindow : Window
    {
        class PeopleContact
        {
            public string id { get; set; }
            public string[] names { get; set; }
            public string[] emailAddresses { get; set; }
            public string[] phoneNumbers { get; set; }
        }

        void get_people_contacts(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var username = args.GetValue<string>();
                    var info = args.GetValue<string>(1);
                    var contacts = args.GetValue<List<PeopleContact>>(2);
                    string infoPreview = string.Join("|", contacts);

                    //check if string info is a valid datetime string

                    icAssistantContent.Items.Add(new AssistantPeopleContactsListed()
                    {
                        username = username,
                        info = info,
                    });

                    if (contacts != null)
                    {
                        //for loop in events list
                        foreach (var item in contacts)
                        {
                            //MessageBox.Show(item.Title+"\n\n"+item.Description+"\n"+item.StartDateTime + " - " + item.EndDateTime);
                            CreatePeopleEntryUI(item);
                        }
                    }

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error in get_people_contacts: " + ex.Message);
                    Debugger.Break();
                }
            }));
        }

        private void CreatePeopleEntryUI(PeopleContact item, string color = "#88ffffff")
        {
            icAssistantContent.Items.Add(new AssistantPeopleContact()
            {
                Name = string.Join("|", item.names),
                Email = string.Join("|", item.emailAddresses),
                Number = string.Join("|", item.phoneNumbers),
            });
        }

        //void add_people_contact(SocketIOResponse args)
        //{
        //    Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        try
        //        {
        //            var username = args.GetValue<string>();
        //            PeopleContact PeopleContact = args.GetValue<PeopleContact>(1);

        //            //create entry UI
        //            CreatePeopleEntryUI(PeopleContact, prefix: "Created Event: ", color: "#8ec336");

        //            AdjustAssistantMessageSpacing();
        //            scvAssistant.ScrollToBottom();

        //            AssistantDelayClose = false;
        //        }
        //        catch { }
        //    }));
        //}
        //void edit_people_contact(SocketIOResponse args)
        //{
        //    Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        try
        //        {
        //            var username = args.GetValue<string>();
        //            PeopleContact PeopleContact = args.GetValue<PeopleContact>(1);

        //            //create entry UI
        //            CreatePeopleEntryUI(PeopleContact, prefix: "Edited Event: ", color: "#eaa431");

        //            AdjustAssistantMessageSpacing();
        //            scvAssistant.ScrollToBottom();

        //            AssistantDelayClose = false;
        //        }
        //        catch { }
        //    }));
        //}
        //void remove_people_contact(SocketIOResponse args)
        //{
        //    Dispatcher.BeginInvoke(new Action(() =>
        //    {
        //        try
        //        {
        //            var username = args.GetValue<string>();
        //            PeopleContact PeopleContact = args.GetValue<PeopleContact>(1);

        //            //create entry UI
        //            CreatePeopleEntryUI(PeopleContact, prefix: "Removed Event: ", color: "#e26831");

        //            AdjustAssistantMessageSpacing();
        //            scvAssistant.ScrollToBottom();

        //            AssistantDelayClose = false;
        //        }
        //        catch { }
        //    }));
        //}
    }
}
