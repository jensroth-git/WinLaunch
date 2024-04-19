﻿using KTrie;
using MdXaml;
using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace WinLaunch
{
    public class AssistantMessageTextContent : DependencyObject
    {
        public int StreamID { get; set; }

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
                        if (before.Length == 0 || !char.IsLetterOrDigit(before.Last()))
                        {
                            if (after.Length == 0 || !char.IsLetterOrDigit(after.First()))
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

    partial class MainWindow : Window
    {
        AssistantMessageTextContent GetMessageForStreamID(int RunID)
        {
            for (int i = icAssistantContent.Items.Count - 1; i > 0; i--)
            {
                if (icAssistantContent.Items[i] is AssistantMessageTextContent)
                {
                    var message = icAssistantContent.Items[i] as AssistantMessageTextContent;

                    if (message.StreamID == RunID)
                    {
                        return message;
                    }
                }
            }

            return null;
        }

        void msg_stream_update(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    int StreamID = args.GetValue<int>(0);
                    var message = GetMessageForStreamID(StreamID);

                    if (message != null)
                    {
                        message.Text += args.GetValue<string>(1);
                    }
                    else
                    {
                        icAssistantContent.Items.Add(new AssistantMessageTextContent() { StreamID = StreamID, Text = args.GetValue<string>(1) });

                        AdjustAssistantMessageSpacing();
                        scvAssistant.ScrollToBottom();
                    }
                }
                catch { }
            }));
        }

        void msg_stream_end(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                try
                {
                    int StreamID = args.GetValue<int>(0);
                    var message = GetMessageForStreamID(StreamID);

                    if (message == null)
                    {
                        //something went wrong, just add the message to the UI
                        icAssistantContent.Items.Add(new AssistantMessageTextContent() { StreamID = StreamID, Text = args.GetValue<string>(1) });
                    }
                    else
                    {
                        //set the text 
                        message.Text = args.GetValue<string>(1);
                    }

                    AssistantResponsePending = false;

                    AdjustAssistantMessageSpacing();
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
        }


    }
}
