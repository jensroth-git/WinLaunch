using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
        //string AssistantURL = "http://localhost:3000";
        string AssistantURL = "http://assistant.winlaunch.org:3000";


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
            var items = SBM.AssistantFindItemsByExactName(name);

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

        public void RemovePendingIndicator()
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
                    return;

                icAssistantContent.Items.Remove(indicator);
            }
        }

        public void MovePendingIndicatorToBottom()
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
                    return;

                icAssistantContent.Items.Remove(indicator);
                icAssistantContent.Items.Add(indicator);
            }
        }

        async void ConnectAssistant()
        {
            if (AssistantClient != null && AssistantClient.Connected)
                return;

            AssistantClient = new SocketIOClient.SocketIO(AssistantURL, new SocketIOOptions
            {
                Query = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("username", Settings.CurrentSettings.AssistantUsername),
                    new KeyValuePair<string, string>("password", Settings.CurrentSettings.AssistantPassword),
                    new KeyValuePair<string, string>("version", Settings.CurrentSettings.version.ToString())
                }
            });

            AssistantClient.OnConnected += async (sender, e) =>
            {

            };

            AssistantClient.OnDisconnected += (sender, e) =>
            {
                AssistantResponsePending = false;
                TransitionAssistantState(AssistantState.Disconnected);
            };

            AssistantClient.On("wrong_login", message =>
            {
                //wrong login
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(TranslationSource.Instance["AssistantWrongPassword"]);

                    //reset login information
                    Settings.CurrentSettings.AssistantUsername = null;
                    Settings.CurrentSettings.AssistantPassword = null;

                    Settings.SaveSettings(Settings.CurrentSettings);

                    //will be disconnected shortly
                    TransitionAssistantState(AssistantState.Register);
                }));
            });

            AssistantClient.On("unfollowed", args =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show(TranslationSource.Instance["AssistantUnfollowed"]);

                    TransitionAssistantState(AssistantState.ManualReconnectRequired);
                }));
            });

            AssistantClient.On("system_message", args =>
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
            });

            AssistantClient.On("new_session", async (args) =>
            {
                try
                {
                    string username = args.GetValue<string>();
                    string rewardTier = args.GetValue<string>(1);

                    //send memory
                    await AssistantClient.EmitAsync("set_memory", Settings.CurrentSettings.AssistantMemoryList);

                    //send installed items
                    await AssistantClient.EmitAsync("update_items", SBM.IC.BuildAssistantItems());

                    await Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //clear old chat messages since this is a new session
                        ClearAssistantChat();

                        if (rewardTier == "Free")
                        {
                            currentTier = AssistantTier.Basic;
                        }
                        else
                        {
                            currentTier = AssistantTier.Pro;
                        }

                        InitializeAssistantModeSwitcher();

                        TransitionAssistantState(AssistantState.Chat);
                    }));
                }
                catch { }
            });

            AssistantClient.On("msg_stream_update", messagePart =>
            {
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        var element = GetLastActualAssistantMessage();

                        if (element is AssistantMessageTextContent)
                        {
                            //update text
                            (element as AssistantMessageTextContent).Text += messagePart.GetValue<string>();
                        }
                        else
                        {
                            if (!(element is AssistantMessageHeader))
                            {
                                icAssistantContent.Items.Add(new AssistantMessageSpacer());
                            }

                            icAssistantContent.Items.Add(new AssistantMessageTextContent() { Text = messagePart.GetValue<string>() });
                            icAssistantContent.Items.Add(new AssistantMessageFooter());

                            MovePendingIndicatorToBottom();

                            scvAssistant.ScrollToBottom();
                        }
                    }
                    catch { }
                }));
            });

            AssistantClient.On("msg_stream_end", args =>
            {
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        //if it is not an immediate response, add a spacer
                        var element = GetLastActualAssistantMessage();

                        if (element is AssistantMessageTextContent)
                        {
                            if (Settings.CurrentSettings.AssistantTTS)
                            {
                                AssistantMessageTextContent message = element as AssistantMessageTextContent;

                                assistantSpeech.SpeakAsyncCancelAll();
                                assistantSpeech.SpeakAsync(message.Text);
                            }
                        }

                        AssistantResponsePending = false;
                        RemovePendingIndicator();
                        scvAssistant.ScrollToBottom();


                        if (AssistantDelayClose)
                        {
                            AssistantDelayClose = false;

                            await Task.Delay(2000);

                            if (!IsHidden)
                            {
                                ToggleLaunchpad();
                            }
                        }
                    }
                    catch { }
                }));
            });

            AssistantClient.On("rate_limit", message =>
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
            });

            AssistantClient.On("show_items", itemList =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var items = itemList.GetValue<List<string>>();

                        foreach (var item in items)
                        {
                            AppendShowItem(item);
                        }

                        MovePendingIndicatorToBottom();
                        scvAssistant.ScrollToBottom();
                    }
                    catch { }
                }));
            });

            AssistantClient.On("store_memory", memory =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var memoryName = memory.GetValue<string>();
                        var memoryValue = memory.GetValue<string>(1);

                        Settings.CurrentSettings.AssistantMemoryList.RemoveAll(x => x.Name == memoryName);

                        if (!string.IsNullOrEmpty(memoryValue))
                        {
                            Settings.CurrentSettings.AssistantMemoryList.Add(new AssistantMemoryItem()
                            {
                                Name = memoryName,
                                Memory = memoryValue
                            });
                        }

                        Settings.SaveSettings(Settings.CurrentSettings);

                        icAssistantContent.Items.Add(new AssistantMemoryAction()
                        {
                            Text = TranslationSource.Instance["AssistantStoreMemory"],
                            Name = memoryName,
                            Memory = memoryValue
                        });

                        MovePendingIndicatorToBottom();
                        scvAssistant.ScrollToBottom();
                    }
                    catch { }
                }));
            });

            AssistantClient.On("forget_memory", memory =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var memoryName = memory.GetValue<string>();

                        var memoryItem = Settings.CurrentSettings.AssistantMemoryList.Find(x => x.Name == memoryName);
                        if (memoryItem != null)
                        {
                            Settings.CurrentSettings.AssistantMemoryList.Remove(memoryItem);
                            Settings.SaveSettings(Settings.CurrentSettings);

                            icAssistantContent.Items.Add(new AssistantMemoryAction()
                            {
                                Text = TranslationSource.Instance["AssistantForgetMemory"],
                                Name = memoryName,
                                Memory = memoryItem.Memory
                            });

                            MovePendingIndicatorToBottom();
                            scvAssistant.ScrollToBottom();
                        }
                    }
                    catch { }
                }));
            });

            AssistantClient.On("set_note_for_item", args =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var name = args.GetValue<string>();
                        var note = args.GetValue<string>(1);

                        var InstalledItems = SBM.AssistantFindItemsByExactName(name, true);

                        if (InstalledItems.Count == 0)
                            return;

                        InstalledItems.First().Notes = note;
                        InstalledItems.First().NotesProp = note;

                        //save items
                        TriggerSaveItemsDelayed();

                        icAssistantContent.Items.Add(new AssistantSetItemNote()
                        {
                            Text = "Set Item Note",
                            Icon = InstalledItems.First().Icon,
                            Name = InstalledItems.First().Name,
                            Note = note
                        });

                        MovePendingIndicatorToBottom();
                        scvAssistant.ScrollToBottom();
                    }
                    catch { }
                }));
            });

            AssistantClient.On("launch_items", itemList =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var items = itemList.GetValue<List<string>>();

                        foreach (var item in items)
                        {
                            var InstalledItems = SBM.AssistantFindItemsByExactName(item);

                            if (InstalledItems.Count == 0)
                                continue;

                            ItemActivated(InstalledItems.First(), EventArgs.Empty, false, false);

                            icAssistantContent.Items.Add(new AssistantLaunchedApp()
                            {
                                Text = TranslationSource.Instance["AssistantLaunchedItem"],
                                Name = InstalledItems.First().Name,
                                Icon = InstalledItems.First().Icon
                            });

                            MovePendingIndicatorToBottom();

                            AssistantDelayClose = true;
                        }
                    }
                    catch { }
                }));
            });

            AssistantClient.On("search_web", query =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        String searchRequest = query.GetValue<string>();
                        System.Diagnostics.Process.Start("http://www.google.com/search?q=" + System.Uri.EscapeDataString(searchRequest));

                        icAssistantContent.Items.Add(new AssistantSearchedWeb()
                        {
                            Text = TranslationSource.Instance["AssistantSearchedWeb"],
                            Query = searchRequest
                        });

                        MovePendingIndicatorToBottom();
                        scvAssistant.ScrollToBottom();

                        AssistantDelayClose = true;
                    }
                    catch { }
                }));
            });

            AssistantClient.On("shell_execute", args =>
            {
                Dispatcher.BeginInvoke(new Action(async () =>
                {
                    string output = string.Empty;
                    try
                    {
                        string file = args.GetValue<string>();
                        string parameters = args.GetValue<string>(1);
                        bool hide = args.GetValue<bool>(2);

                        icAssistantContent.Items.Add(new AssistantExecutedCommand(file, parameters)
                        {
                            Text = TranslationSource.Instance["AssistantExecutedCommand"]
                        });

                        MovePendingIndicatorToBottom();
                        scvAssistant.ScrollToBottom();

                        if (Settings.CurrentSettings.ExecuteAssistantCommands)
                        {
                            try
                            {
                                output = ExecuteProcessAndGetOutput(file, parameters);
                                await args.CallbackAsync(output);
                            }
                            catch (Exception ex)
                            {
                                try
                                {
                                    await args.CallbackAsync(ex.Message);
                                }
                                catch { }
                            }

                            if (hide)
                            {
                                AssistantDelayClose = true;
                            }
                        }
                        else
                        {
                            try
                            {
                                await args.CallbackAsync("User disabled commands, they can be enabled again in the settings");
                            }
                            catch { }
                        }
                    }
                    catch { }
                }));
            });

            AssistantClient.On("failed_function", args =>
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

                        MovePendingIndicatorToBottom();
                        scvAssistant.ScrollToBottom();
                    }
                    catch { }
                }));
            });

            await AssistantClient.ConnectAsync();
            //send output back
           

        }

        private string ExecuteProcessAndGetOutput(string file, string parameters)
        {
            string output = string.Empty;
            parameters += " && exit";
            using (Process process = new Process())
            {
                process.StartInfo.FileName = file;
                process.StartInfo.Arguments = parameters;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.StartInfo.CreateNoWindow = true; // Do not create the black window
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Hides the window

                process.Start();

                output = process.StandardOutput.ReadToEnd();

                if (!process.WaitForExit(1)) // Wait for the process to exit with a timeout.
                {
                    process.Kill(); // Forcefully kill the process if it doesn't exit in time.
                }
            }

            return output;
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
            //Settings.CurrentSettings.AssistantPassword = null;
            //Settings.CurrentSettings.AssistantUsername = null;
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
            if (AssistantClient == null || !AssistantClient.Connected || AssistantResponsePending)
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
            icAssistantContent.Items.Add(new AssistantMessageFooter());

            icAssistantContent.Items.Add(new AssistantMessageHeader());
            icAssistantContent.Items.Add(new AssistantPendingIndicator());
            scvAssistant.ScrollToBottom();

            //send prompt
            await AssistantClient.EmitAsync("msg", prompt);
            AssistantResponsePending = true;
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
                    if (!AssistantResponsePending)
                    {
                    RunAssistant();
                    e.Handled = true;
                        return;
                    }
                    else
                    {
                        e.Handled = true;
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

        //set username
        string attemptedSetUsername;
        bool checkUsernameActive = false;
        async void AttemptSetAssistantUsername()
        {
            if (checkUsernameActive)
                return;

            try
            {
                if (string.IsNullOrEmpty(tbxAssistantPatreonEmail.Text))
                    return;

                attemptedSetUsername = tbxAssistantPatreonEmail.Text.ToLower();

                checkUsernameActive = true;
                tbxAssistantPatreonEmail.IsEnabled = false;

                WebClient client = new WebClient();

                var str = await MiscUtils.DownloadStringTaskAsync(client, AssistantURL + "/checkUser/" + attemptedSetUsername);

                if (str == "set password")
                {
                    //correct username but no password set yet
                    Settings.CurrentSettings.AssistantUsername = attemptedSetUsername;

                    TransitionAssistantState(AssistantState.SetPassword);
                }
                else if (str == "registered")
                {
                    //user already registered
                    Settings.CurrentSettings.AssistantUsername = attemptedSetUsername;

                    TransitionAssistantState(AssistantState.EnterPassword);
                }
                else
                {
                    MessageBox.Show(string.Format(TranslationSource.Instance["AssistantNoPatron"], attemptedSetUsername));
                }
            }
            catch
            {
                MessageBox.Show(TranslationSource.Instance["AssistantTroubleConnecting"]);
            }

            checkUsernameActive = false;
            tbxAssistantPatreonEmail.IsEnabled = true;
        }

        private void tbxAssistantPatreonEmail_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                AttemptSetAssistantUsername();

                e.Handled = true;
                return;
            }
        }
        private void imAssistantPatreonEmailSend_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            AttemptSetAssistantUsername();
        }


        //set password
        string GetPasswordHash(string username, string plainTextPassword)
        {
            var bytes = Encoding.UTF8.GetBytes(username.ToLower() + plainTextPassword);

            HashAlgorithm algorithm = new SHA256Managed();

            var passwordHashBytes = algorithm.ComputeHash(bytes);

            return Convert.ToBase64String(passwordHashBytes);
        }
        bool checkPasswordActive = false;
        async void HandlePasswordInput()
        {
            if (checkPasswordActive)
                return;

            if (string.IsNullOrEmpty(tbxAssistantPassword.Password))
            {
                MessageBox.Show(TranslationSource.Instance["AssistantEnterPassword"]);
                return;
            }

            //check or set password 
            try
            {
                checkPasswordActive = true;
                tbxAssistantPassword.IsEnabled = false;

                string passwordHash = GetPasswordHash(attemptedSetUsername, tbxAssistantPassword.Password);

                WebClient client = new WebClient();
                client.QueryString.Add("username", attemptedSetUsername);
                client.QueryString.Add("passwordHash", passwordHash);

                var str = await MiscUtils.DownloadStringTaskAsync(client, AssistantURL + "/checkSetPassword");

                if (str == "invalid")
                {
                    MessageBox.Show(TranslationSource.Instance["AssistantWrongPassword"]);
                }
                else if (str == "success")
                {
                    Settings.CurrentSettings.AssistantPassword = passwordHash;
                    Settings.SaveSettings(Settings.CurrentSettings);

                    attemptedSetUsername = null;

                    TransitionAssistantState(AssistantState.Connecting);
                }

                if (str == "error")
                {
                    throw new Exception();
                }
            }
            catch
            {
                MessageBox.Show(TranslationSource.Instance["Error"]);
            }

            checkPasswordActive = false;
            tbxAssistantPassword.IsEnabled = true;
        }

        private void tbxAssistantPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;

                HandlePasswordInput();
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
            HandlePasswordInput();
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
                return;

            RunAssistant();
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
