using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
    public class AssistantItemsListed : DependencyObject
    {
    }

    public class AssistantMessageShowItemContainer : DependencyObject
    {
        public ObservableCollection<SBItem> Items { get; set; } = new ObservableCollection<SBItem>();
    }

    public class AssistantLaunchedApp : DependencyObject
    {
        public string Text { get; set; }
        public string Name { get; set; }
        public BitmapSource Icon { get; set; }
    }

    partial class MainWindow : Window
    {
        void items_listed(SocketIOResponse query)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    icAssistantContent.Items.Add(new AssistantItemsListed());

                    MovePendingIndicatorToBottom();
                    scvAssistant.ScrollToBottom();

                    AssistantDelayClose = false;
                }
                catch { }
            }));
        }

        void show_items(SocketIOResponse itemList)
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
        }

        void launch_items(SocketIOResponse itemList)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var items = itemList.GetValue<List<string>>();

                    foreach (var item in items)
                    {
                        var InstalledItems = SBM.FindItemsByExactName(item);

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
        }
    }
}
