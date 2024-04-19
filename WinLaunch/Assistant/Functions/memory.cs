using SocketIOClient;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WinLaunch
{
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

    partial class MainWindow : Window
    {
        void store_memory(SocketIOResponse memory)
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
        }

        void forget_memory(SocketIOResponse memory)
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
        }

        void set_note_for_item(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var name = args.GetValue<string>();
                    var note = args.GetValue<string>(1);

                    var InstalledItems = SBM.FindItemsByExactName(name, true);

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
        }
    }
}
