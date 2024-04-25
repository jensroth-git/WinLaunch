using EncryptionUtils;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    public class AssistantMemoryItem
    {
        public string Name { get; set; }
        public string Memory { get; set; }
    }

    public class AssistantItem
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Keywords { get; set; }
        public string Notes { get; set; }

        public List<AssistantItem> Items { get; set; }
    }

    partial class MainWindow : Window
    {
        bool AssistantActive = false;
        bool AssistantDelayClose = false;
        bool AssistantResponsePending = false;

        SocketIOClient.SocketIO AssistantClient;

        SpeechSynthesizer assistantSpeech = new SpeechSynthesizer();

        object GetLastActualAssistantMessage()
        {
            for (int i = icAssistantContent.Items.Count - 1; i > 0; i--)
            {
                if (icAssistantContent.Items[i] is AssistantPendingIndicator ||
                    icAssistantContent.Items[i] is AssistantMessageFooter ||
                    icAssistantContent.Items[i] is AssistantMessageSpacer)
                {
                    continue;
                }
                else
                {
                    return icAssistantContent.Items[i];
                }
            }

            return null;
        }

        void AppendShowItem(string name)
        {
            //find item
            var items = SBM.FindItemsByExactName(name);

            if (items.Count == 0)
                return;

            //build item
            var item = items.First();
            SBItem shownItem = new SBItem(item.Name, item.Keywords, item.Notes, item.ApplicationPath, item.IconPath, item.Arguments, item.Icon);

            //append to existing container or add to new one
            object lastItem = null;

            lastItem = GetLastActualAssistantMessage();

            if (lastItem is AssistantMessageShowItemContainer)
            {
                (lastItem as AssistantMessageShowItemContainer).Items.Add(shownItem);
            }
            else
            {
                AssistantMessageShowItemContainer container = new AssistantMessageShowItemContainer();
                container.Items.Add(shownItem);

                icAssistantContent.Items.Add(container);
            }
        }

        public AssistantPendingIndicator RemovePendingIndicator()
        {
            if (icAssistantContent.Items.Count >= 1)
            {
                //find indicator
                AssistantPendingIndicator indicator = null;
                foreach (var item in icAssistantContent.Items)
                {
                    if (item is AssistantPendingIndicator)
                    {
                        indicator = item as AssistantPendingIndicator;
                        break;
                    }
                }

                if (indicator == null)
                    return null;

                icAssistantContent.Items.Remove(indicator);

                return indicator;
            }

            return null;
        }

        void AdjustAssistantMessageSpacing()
        {
            //remove all spacers and footers
            for (int i = icAssistantContent.Items.Count - 1; i >= 0; i--)
            {
                if (icAssistantContent.Items[i] is AssistantMessageSpacer || icAssistantContent.Items[i] is AssistantMessageFooter)
                {
                    icAssistantContent.Items.RemoveAt(i);
                }
            }

            //remove pending indicator
            var pendingIndicator = RemovePendingIndicator();

            //insert spacers above each text message unless there is a header above it
            for (int i = icAssistantContent.Items.Count - 1; i >= 0; i--)
            {
                if (icAssistantContent.Items[i] is AssistantMessageTextContent)
                {
                    if (i != 0 && !(icAssistantContent.Items[i - 1] is AssistantMessageHeader))
                    {
                        icAssistantContent.Items.Insert(i, new AssistantMessageSpacer());
                    }
                }
            }

            //insert footers above each header except the first one
            for (int i = icAssistantContent.Items.Count - 1; i >= 0; i--)
            {
                if (icAssistantContent.Items[i] is AssistantMessageHeader)
                {
                    if (i != 0)
                    {
                        icAssistantContent.Items.Insert(i, new AssistantMessageFooter());
                    }
                }
            }

            //add pending indicator back
            if (pendingIndicator != null)
            {
                icAssistantContent.Items.Add(pendingIndicator);
            }
        }

        async void ConnectAssistant()
        {
            if (AssistantClient != null && AssistantClient.Connected)
                return;

            string password = null;

            if (Settings.CurrentSettings.AssistantPassword != null)
            {
                try
                {
                    password = EncryptionUtils.AesOperation.DecryptString(PasswordEncryptionKey, Settings.CurrentSettings.AssistantPassword);
                }
                catch
                {
                    TransitionAssistantState(AssistantState.Login);
                    return;
                }
            }

            AssistantClient = new SocketIOClient.SocketIO(AssistantURL, new SocketIOOptions
            {
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("username", Base64Operation.Encode(Settings.CurrentSettings.AssistantUsername)),
                    new KeyValuePair<string, string>("password", Base64Operation.Encode(password)),
                    new KeyValuePair<string, string>("version", Settings.CurrentSettings.version.ToString())
                }
            });

            AssistantClient.OnConnected += async (sender, e) =>
            {

            };

            AssistantClient.OnDisconnected += (sender, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AssistantResponsePending = false;
                    TransitionAssistantState(AssistantState.ManualReconnectRequired);
                }));
            };


            AssistantClient.On("new_session", async (args) =>
            {
                try
                {
                    string username = args.GetValue<string>();

                    //send memory
                    await AssistantClient.EmitAsync("set_memory", Settings.CurrentSettings.AssistantMemoryList);

                    //send installed items
                    await AssistantClient.EmitAsync("update_items", SBM.IC.BuildAssistantItems());

                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //clear old chat messages since this is a new session
                        ClearAssistantChat();

                        InitializeAssistantModeSwitcher();

                        TransitionAssistantState(AssistantState.Chat);
                    }));
                }
                catch { }
            });

            AssistantClient.On("run_end", (args) =>
            {
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        AssistantResponsePending = false;
                        RemovePendingIndicator();
                        scvAssistant.ScrollToBottom();

                        imAssistantSend.Source = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component" + "/res/assistant/send.png"));
                    }
                    catch { }
                }));
            });

            //system messages
            AssistantClient.On("wrong_login", wrong_login);
            AssistantClient.On("rate_limit", rate_limit);
            AssistantClient.On("system_message", system_message);
            AssistantClient.On("failed_function", failed_function);

            //text messages
            AssistantClient.On("msg_stream_update", msg_stream_update);
            AssistantClient.On("msg_stream_end", msg_stream_end);

            //item handling
            AssistantClient.On("items_listed", items_listed);
            AssistantClient.On("show_items", show_items);
            AssistantClient.On("launch_items", launch_items);

            //memory & notes
            AssistantClient.On("store_memory", store_memory);
            AssistantClient.On("forget_memory", forget_memory);
            AssistantClient.On("set_note_for_item", set_note_for_item);

            //utils
            AssistantClient.On("search_web", search_web);

            //execute commands
            AssistantClient.On("shell_execute", shell_execute);
            AssistantClient.On("run_python", run_python);

            //calendar
            AssistantClient.On("get_calendar_events", get_calendar_events);
            AssistantClient.On("add_calendar_event", add_calendar_event);

            //gmail
            AssistantClient.On("get_gmail_messages", get_gmail_messages);
            AssistantClient.On("sent_gmail_message", sent_gmail_message);

            await AssistantClient.ConnectAsync();
        }

        public void InitAssistant()
        {
            //set default response style
            RichTextBoxHelper.MDengine.DocumentStyle = FindResource("DocumentStyleWinLaunchAssistant") as Style;

            //build items trie for fast searching
            RichTextBoxHelper.itemsTrie = SBM.IC.BuildItemTrie();

            //link event
            RichTextBoxHelper.LinkOpened += RichTextBoxHelper_LinkOpened;

            //InitRecognizer();
        }

        private void RichTextBoxHelper_LinkOpened(object sender, EventArgs e)
        {
            ItemActivated((sender as AppButton).Item, EventArgs.Empty);
        }

        public void ActivateAssistant()
        {
            if (AssistantActive)
                return;

            AssistantActive = true;

            TransitionAssistantState(AssistantState.Connecting);
        }

        public async void NewAssistantSession()
        {
            //cancel speech
            assistantSpeech.SpeakAsyncCancelAll();

            //disconnect and reconnect the socket
            if (AssistantClient != null)
            {
                await AssistantClient.DisconnectAsync();
            }

            TransitionAssistantState(AssistantState.Connecting);
            //ConnectAssistant();
        }

        private async void RunAssistant(string prompt = "")
        {
            if (AssistantClient == null || !AssistantClient.Connected)
            {
                TransitionAssistantState(AssistantState.Connecting);
                return;
            }

            if (prompt == "" || string.IsNullOrWhiteSpace(prompt))
            {
                if (tbAssistant.Text == "" || string.IsNullOrWhiteSpace(tbAssistant.Text))
                    return;

                prompt = tbAssistant.Text;

                tbAssistant.Clear();
            }

            //first message sent
            if (icAssistantContent.Items.Count == 0)
            {
                //setup header
                spAssistantHeader.VerticalAlignment = VerticalAlignment.Top;
                tbAssistantHeader.Text = TranslationSource.Instance["AssistantName"];
                imAssistantIcon.Width = 70;
                icPromptSuggestions.Visibility = Visibility.Hidden;

                bdRewardTierPro.Visibility = (proMode) ? Visibility.Visible : Visibility.Collapsed;
                bdRewardTierBasic.Visibility = (!proMode) ? Visibility.Visible : Visibility.Collapsed;

                //hide mode switcher
                bdAssistantModeSwitcher.Visibility = Visibility.Collapsed;

                //setup assistant mode
                if (proMode)
                {
                    await AssistantClient.EmitAsync("set_mode", proMode ? "pro" : "basic");
                }
            }

            icAssistantContent.Items.Add(new AssistantMessageHeader(true));
            icAssistantContent.Items.Add(new AssistantMessageTextContent() { Text = prompt });

            icAssistantContent.Items.Add(new AssistantMessageHeader());
            icAssistantContent.Items.Add(new AssistantPendingIndicator());

            AdjustAssistantMessageSpacing();
            scvAssistant.ScrollToBottom();

            ////create mockup CalendarEntry
            //CalendarEvent item = new CalendarEvent()
            //{
            //    Title = "Test",
            //    Description = "description",
            //    StartDateTime = DateTime.Now.ToString(),
            //    EndDateTime = DateTime.Now.ToString(),
            //    IsAllDay = false,
            //    Location = "Germany",
            //    Link = "google.com",
            //    Attendees = new List<string>() { "some@email.com", "another@email.com" }
            //};

            //CreateCalendarEntryUI(item);
            //CreateCalendarEntryUI(item);

            //send prompt
            await AssistantClient.EmitAsync("msg", prompt, DateTime.Now.ToString("o", CultureInfo.InvariantCulture));
            AssistantResponsePending = true;

            //change send button to abort
            imAssistantSend.Source = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component" + "/res/quit.png"));


        }

        private async void AbortAssistant()
        {
            if (AssistantClient == null || !AssistantResponsePending)
            {
                return;
            }

            await AssistantClient.EmitAsync("abort_run");
        }

        private void tbAssistant_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //    if (e.Key == Key.Escape)
            //    {
            //        DeactivateAssistant();
            //        e.Handled = true;
            //        return;
            //    }

            //if (AssistantResponsePending)
            //{
            //    e.Handled = true;
            //    return;
            //}

            if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
            {
                if (e.Key == Key.Return)
                {
                    tbAssistant.Text += Environment.NewLine;
                    tbAssistant.Select(tbAssistant.Text.Length, 0);
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                if (e.Key == Key.Return)
                {
                    e.Handled = true;

                    //if (!AssistantResponsePending)
                    {
                        RunAssistant();
                        return;
                    }
                }
            }
        }

        private void AssistantShowItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SBItem Item = ((e.Source as FrameworkElement).DataContext as SBItem);

            ItemActivated(Item, EventArgs.Empty);
        }

        private void bdSuggestion_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var PromptSuggestion = ((sender as FrameworkElement).DataContext as AssistantPromptSuggestion).Prompt;

            RunAssistant(PromptSuggestion);

            e.Handled = true;
        }

        //login
        async void SetAssistantLogin()
        {
            string username = tbxAssistantEmail.Text;
            string password = tbxAssistantPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter an email and password");
                return;
            }

            //encrypt password
            password = EncryptionUtils.AesOperation.EncryptString(PasswordEncryptionKey, password);

            Settings.CurrentSettings.AssistantUsername = username;
            Settings.CurrentSettings.AssistantPassword = password;

            Settings.SaveSettings(Settings.CurrentSettings);

            //transition to connecting
            TransitionAssistantState(AssistantState.Connecting);
        }

        private void tbxAssistantPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;

                SetAssistantLogin();
                return;
            }
        }
        private void tbxAssistantPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (tbxAssistantPassword.Password != "")
            {
                tblAssistantPasswordWatermark.Visibility = Visibility.Collapsed;
            }
            else
            {
                tblAssistantPasswordWatermark.Visibility = Visibility.Visible;
            }
        }
        private void imAssistantPasswordSend_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetAssistantLogin();
        }

        private void imHideAssistantWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsHidden)
            {
                ToggleLaunchpad();
            }
        }

        private void imAssistant_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ActivateAssistant();
        }

        private void imCloseAssistant_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TransitionAssistantState(AssistantState.BackgroundClosed);
        }

        private void imAssistantSend_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AssistantResponsePending)
            {
                AbortAssistant();
            }
            else
            {
                RunAssistant();
            }
        }

        private void imAssistantClear_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NewAssistantSession();
        }

        private void bdButtonReconnect_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TransitionAssistantState(AssistantState.Connecting);
        }
    }
}
