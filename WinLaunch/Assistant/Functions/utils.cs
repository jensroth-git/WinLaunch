using SocketIOClient;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

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

                    AdjustAssistantMessageSpacing();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = true;
                }
                catch { }
            }));
        }
    }
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (string.IsNullOrEmpty((string)value))
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
