using KTrie;
using MdXaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    public class AssistantPromptSuggestion
    {
        public BitmapSource Icon { get; set; }
        public string Prompt { get; set; }
    }

    public class AssistantMessageHeader : DependencyObject
    {
        public string Name { get; set; } = TranslationSource.Instance["AssistantName"];
        public BitmapSource Icon { get; set; } = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/icon.png"));

        public AssistantMessageHeader(bool user = false)
        {
            if (user)
            {
                Name = TranslationSource.Instance["AssistantYou"];
                Icon = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/assistant/user.png"));
            }
        }
    }

    public class AppButton : Control
    {
        public SBItem Item { get; set; }

        public event EventHandler LinkOpened;

        static AppButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AppButton), new FrameworkPropertyMetadata(typeof(AppButton)));
        }

        public override void OnApplyTemplate()
        {
            Image imgIcon = GetTemplateChild("PART_imgIcon") as Image;

            try
            {
                imgIcon.Source = Item.Icon;
            }
            catch { }

            this.ToolTip = Item.Name;

            base.OnApplyTemplate();
        }

        //shell execute whatever path was clicked (works for files, folders, websites, etc.)
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                e.Handled = true;

                if (LinkOpened != null)
                    LinkOpened(this, EventArgs.Empty);
            }
        }
    }

    public class RichTextBoxHelper : DependencyObject
    {
        public static Markdown MDengine = new Markdown();

        public static StringTrie<SBItem> itemsTrie;

        public static event EventHandler LinkOpened;

        public static string GetDocumentMarkdown(DependencyObject obj)
        {
            return (string)obj.GetValue(DocumentMarkdownProperty);
        }

        public static void SetDocumentMarkdown(DependencyObject obj, string value)
        {
            obj.SetValue(DocumentMarkdownProperty, value);
        }

        class AppMatch
        {
            public string before;
            public string app;
            public string after;
        }

        private static AppMatch FindNextApp(string text)
        {
            List<AppMatch> matches = new List<AppMatch>();

            var chars = text.ToCharArray();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < chars.Length; i++)
            {
                sb.Append(chars[i]);

                var entries = itemsTrie.GetByPrefix(sb.ToString());

                //if there are any apps that start with this prefix
                if (entries.Any())
                {
                    //if there is an exact match
                    if (itemsTrie.ContainsKey(sb.ToString()))
                    {
                        //character before the app
                        string before = text.Substring(0, i - sb.Length + 1);
                        string after = text.Substring(i + 1);

                        //only insert the app if its encased in special characters or whitespace
                        if(before.Length == 0 || !char.IsLetterOrDigit(before.Last()))
                        {
                            if(after.Length == 0 || !char.IsLetterOrDigit(after.First()))
                            {
                                //insert the app
                                matches.Add(new AppMatch() { before = before, app = sb.ToString(), after = after });
                            }
                        }
                    }
                }
                else
                {
                    if (matches.Any())
                    {
                        //return the last match since its the longest
                        return matches.Last();
                    }
                    else
                    {
                        //no matches found, reset

                        //we need to rewind the index by the length of the stringbuilder - 1 to catch overlapping matches
                        i -= sb.Length - 1;

                        sb.Clear();
                    }
                }
            }

            if (matches.Any())
            {
                //return the last match since its the longest
                return matches.Last();
            }

            return null;
        }

        public static readonly DependencyProperty DocumentMarkdownProperty =
            DependencyProperty.RegisterAttached(
                "DocumentMarkdown",
                typeof(string),
                typeof(RichTextBoxHelper),
                new FrameworkPropertyMetadata
                {
                    BindsTwoWayByDefault = false,
                    PropertyChangedCallback = (obj, e) =>
                    {
                        var richTextBox = (RichTextBox)obj;

                        string markdown = GetDocumentMarkdown(richTextBox);

                        // Set the document
                        //MDengine.DisabledContextMenu = false;

                        var doc = MDengine.Transform(markdown);

                        //replace apps with UI elements
                        var blocks = doc.Blocks.ToList();
                        foreach (var block in blocks)
                        {
                            if (block is Paragraph)
                            {
                                InsertAppButtonsInInlines((block as Paragraph).Inlines);
                            }
                            else if (block is List)
                            {
                                //insert images into list items
                                var list = block as List;

                                foreach (var listItem in list.ListItems.ToList())
                                {
                                    foreach (var innerBlock in listItem.Blocks.ToList())
                                    {
                                        if (innerBlock is Paragraph)
                                        {
                                            InsertAppButtonsInInlines((innerBlock as Paragraph).Inlines);
                                        }
                                    }
                                }
                            }
                            else if (block is Table)
                            {
                                var table = block as Table;

                                foreach (var rowGroup in table.RowGroups)
                                {
                                    foreach (var row in rowGroup.Rows)
                                    {
                                        foreach (var cell in row.Cells)
                                        {
                                            foreach (var innerBlock in cell.Blocks.ToList())
                                            {
                                                if (innerBlock is Paragraph)
                                                {
                                                    InsertAppButtonsInInlines((innerBlock as Paragraph).Inlines);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        richTextBox.Document = doc;
                    }
                });

        private static void InsertAppButtonsInInlines(InlineCollection inlineCollection)
        {
            var inlines = inlineCollection.ToList();

            foreach (var run in inlines)
            {
                if (run is Bold)
                {
                    InsertAppButtonsInInlines((run as Bold).Inlines);
                    continue;
                }

                if (run is Run)
                {
                    Run toBeReplacedRun = run as Run;

                    //insert clickable images into inlines 
                    while (true)
                    {
                        //find next app
                        var appMatch = FindNextApp(toBeReplacedRun.Text);

                        if (appMatch == null)
                        {
                            //no app found, continue
                            break;
                        }

                        //found app insert image
                        AppButton appButton = new AppButton() { Item = itemsTrie[appMatch.app] };

                        appButton.LinkOpened += (sender, args) =>
                        {
                            if (LinkOpened != null)
                                LinkOpened(sender, args);
                        };

                        InlineUIContainer container = new InlineUIContainer(appButton);

                        //insert app image
                        Run afterRun = new Run(appMatch.after);
                        inlineCollection.InsertBefore(toBeReplacedRun, new Run(appMatch.before));
                        inlineCollection.InsertAfter(toBeReplacedRun, afterRun);
                        inlineCollection.InsertAfter(toBeReplacedRun, new Run(appMatch.app));
                        inlineCollection.InsertAfter(toBeReplacedRun, container);
                        inlineCollection.Remove(toBeReplacedRun);

                        toBeReplacedRun = afterRun;
                    }
                }
            }
        }
    }

    public class AssistantMessageTextContent : DependencyObject
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AssistantMessageTextContent), new PropertyMetadata(""));
    }

    public class AssistantMessageSpacer : DependencyObject
    {

    }

    public class AssistantMessageFooter : DependencyObject
    {

    }

    public class AssistantMessageShowItemContainer : DependencyObject
    {
        public ObservableCollection<SBItem> Items { get; set; } = new ObservableCollection<SBItem>();
    }

    public class AssistantPendingIndicator : DependencyObject
    {
    }

    public class AssistantLaunchedApp : DependencyObject
    {
        public string Text { get; set; }
        public string Name { get; set; }
        public BitmapSource Icon { get; set; }
    }

    public class AssistantSearchedWeb : DependencyObject
    {
        public string Text { get; set; }
        public string Query { get; set; }
    }
    public class AssistantAddedCalendarEvent : DependencyObject
    {
        public string Text { get; set; }
    }

    public class AssistantMemoryAction : DependencyObject
    {
        public string Text { get; set; }
        public string Name { get; set; }
        public string Memory { get; set; }
    }

    public class AssistantSetItemNote : DependencyObject
    {
        public string Text { get; set; }
        public string Name { get; set; }
        public BitmapSource Icon { get; set; }
        public string Note { get; set; }
    }


    public class AssistantExecutedCommand : DependencyObject
    {
        public string Text { get; set; }
        public string File { get; set; }
        public string Parameters { get; set; }

        public bool showParameters { get; set; } = false;

        public AssistantExecutedCommand(string file, string parameters)
        {
            File = file;

            if (parameters != null)
            {
                showParameters = true;
                Parameters = parameters;
            }
        }
    }

    public class AssistantFailedFunction : DependencyObject
    {
        public string Text { get; set; }
        public string Function { get; set; }
        public string Args { get; set; }
    }

    partial class MainWindow : Window
    {
        #region ModeSwitcher
        public bool proMode = false;

        public void InitializeAssistantModeSwitcher()
        {
            if (currentTier == AssistantTier.Basic)
            {
                imProModeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/assistant/locked.png"));
                SetMode(pro: false);

                return;
            }


            imProModeIcon.Source = new BitmapImage(new Uri("pack://application:,,,/WinLaunch;component/res/assistant/pro.png"));

            SetMode(pro: Settings.CurrentSettings.AssistantProMode);
        }

        public void SetMode(bool pro)
        {
            if (proMode == pro)
                return;

            proMode = pro;

            DoubleAnimation fadeOutAnimation = new DoubleAnimation();
            fadeOutAnimation.To = 0.0;
            fadeOutAnimation.Duration = TimeSpan.FromMilliseconds(200);


            DoubleAnimation fadeInAnimation = new DoubleAnimation();
            fadeInAnimation.To = 1.0;
            fadeInAnimation.Duration = TimeSpan.FromMilliseconds(200);

            if (proMode)
            {
                bdProMode.BeginAnimation(OpacityProperty, fadeInAnimation);
                bdBasicMode.BeginAnimation(OpacityProperty, fadeOutAnimation);

                tblModeBasic.Opacity = 0.5;
                tblModePro.Opacity = 1.0;
            }
            else
            {
                bdProMode.BeginAnimation(OpacityProperty, fadeOutAnimation);
                bdBasicMode.BeginAnimation(OpacityProperty, fadeInAnimation);

                tblModeBasic.Opacity = 1.0;
                tblModePro.Opacity = 0.5;
            }

            //save chosen mode in settings
            if (Settings.CurrentSettings.AssistantProMode != proMode)
            {
                Settings.CurrentSettings.AssistantProMode = proMode;
                Settings.SaveSettings(Settings.CurrentSettings);
            }
        }

        private void tblModeBasic_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetMode(pro: false);
        }

        private void tblModePro_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (currentTier == AssistantTier.Basic)
            {
                MessageBox.Show("Please upgrade your Patreon pledge to use Pro mode");
                return;
            }

            SetMode(pro: true);
        }
        #endregion

        void HideAssistantUIPanels()
        {
            //the mode switcher 
            bdAssistantModeSwitcher.Visibility = Visibility.Hidden;

            //the prompt suggestions
            icPromptSuggestions.Visibility = Visibility.Hidden;

            //the assistant input box
            bdAssistantControls.Visibility = Visibility.Hidden;

            //the assistant header
            spAssistantHeader.Visibility = Visibility.Hidden;

            //the assistant messages
            scvAssistant.Visibility = Visibility.Hidden;

            //the enter username UI 
            spAssistantEnterUsername.Visibility = Visibility.Hidden;

            //the enter password UI
            spAssistantEnterPassword.Visibility = Visibility.Hidden;

            //the connecting animation UI
            spAssistantConnecting.Visibility = Visibility.Hidden;

            //the manual reconnect UI 
            spAssistantManualReconnect.Visibility = Visibility.Hidden;
        }

        private void ClearAssistantChat()
        {
            icAssistantContent.Items.Clear();

            spAssistantHeader.VerticalAlignment = VerticalAlignment.Center;
            tbAssistantHeader.Text = TranslationSource.Instance["AssistantWelcomeMessage"];
            imAssistantIcon.Width = 100;

            icPromptSuggestions.Visibility = Visibility.Visible;
        }

        private void ShowChatUI()
        {
            scvAssistant.Visibility = Visibility.Visible;
            bdAssistantControls.Visibility = Visibility.Visible;
            spAssistantHeader.Visibility = Visibility.Visible;

            if (icAssistantContent.Items.Count == 0)
            {
                bdAssistantModeSwitcher.Visibility = Visibility.Visible;
                icPromptSuggestions.Visibility = Visibility.Visible;

                //hide tiers when showing the welcome message
                bdRewardTierPro.Visibility = Visibility.Collapsed;
                bdRewardTierBasic.Visibility = Visibility.Collapsed;
            }
            else
            {
                bdAssistantModeSwitcher.Visibility = Visibility.Hidden;
                icPromptSuggestions.Visibility = Visibility.Hidden;
            }

            Keyboard.Focus(tbAssistant);
        }

        private void ShowAssistantUI()
        {
            AssistantActive = true;

            gdAssistant.Visibility = Visibility.Visible;
            bdSearchContainer.Visibility = Visibility.Collapsed;
            MainCanvas.Visibility = Visibility.Hidden;
            PageCounterWrap.Visibility = Visibility.Hidden;

            spSearchAndAssistantContainer.Visibility = Visibility.Collapsed;

            imHideAssistantWindow.Visibility = Visibility.Visible;
            imCloseAssistant.Visibility = Visibility.Visible;

            bdAssistantModeSwitcher.Visibility = Visibility.Visible;

            tbSearch.IsEnabled = false;
        }

        private void HideAssistant()
        {
            AssistantActive = false;

            gdAssistant.Visibility = Visibility.Hidden;
            bdSearchContainer.Visibility = Visibility.Visible;
            MainCanvas.Visibility = Visibility.Visible;
            PageCounterWrap.Visibility = Visibility.Visible;

            spSearchAndAssistantContainer.Visibility = Visibility.Visible;

            imHideAssistantWindow.Visibility = Visibility.Hidden;
            imCloseAssistant.Visibility = Visibility.Hidden;

            bdAssistantModeSwitcher.Visibility = Visibility.Hidden;

            tbSearch.IsEnabled = true;
        }
    }
}
