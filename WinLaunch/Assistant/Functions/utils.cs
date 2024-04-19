using SocketIOClient;
using System;
using System.Windows;

namespace WinLaunch
{
    public class AssistantSearchedWeb : DependencyObject
    {
        public string Text { get; set; }
        public string Query { get; set; }
    }

    partial class MainWindow : Window
    {
        void search_web(SocketIOResponse search)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    String searchRequest = search.GetValue<string>();
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
        }
    }
}
