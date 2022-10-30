using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WinLaunch
{
    public partial class Welcome : Window
    {
        private string[] changelog = new string[]{
            "#Welcome to WinLaunch",
            "---",
            "Close this window to start WinLaunch",
            "",
            "#Changelog 0.6.8.3",
            "---",
            "- updated french translation by Sébastien Gellet",
            "- fixed bug where folder items were sometimes not shown",
            "",
            "#Changelog 0.6.8.2",
            "---",
            "- added sort folders first option",
            "- added indonesian translation by jovanzers",
            "",
            "#Changelog 0.6.8.1",
            "---",
            "- fixed folder renaming not working",
            "- added search cancel button",
            "- added auto select all on folder renaming",
            "",
            "#Changelog 0.6.8.0",
            "---",
            "- fixed pin to desktop on high dpi monitors",
            "- added an option to sort items alphabetically",
            "- added a clear items button in the settings",
            "",
            "#Changelog 0.6.7.4",
            "---",
            "- fixed focus issue on restart",
            "",
            "#Changelog 0.6.7.3",
            "---",
            "- updated translations",
            "",
            "#Changelog 0.6.7.2",
            "---",
            "- fixed issue were items werent being saved",
            "",
            "#Changelog 0.6.7.0",
            "---",
            "- added new pin to desktop mode",
            "- updated italian translation by Simone Broglia"
        };

        public Welcome()
        {
            InitializeComponent();

            VersionHeader.Text = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();

            foreach (string line in changelog)
            {
                AddLine(line);
            }
        }

        private void AddLine(string line)
        {
            if (line == "---")
            {
                MainContainer.Children.Add(new Rectangle());
            }
            else
            {
                if (line.StartsWith("-"))
                {
                    line = line.Substring(1);

                    StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0,5,0,5) };

                    MainContainer.Children.Add(sp);

                    sp.Children.Add(new Ellipse());
                    sp.Children.Add(new TextBlock() { Text = line });
                }
                else if (line.StartsWith("#"))
                {
                    //center block
                    line = line.Substring(1);

                    MainContainer.Children.Add(new TextBlock() { Text = line, FontSize = 16, Foreground=Brushes.Black });
                }
                else if (line.StartsWith("|"))
                {
                    //indent block
                    line = line.Substring(1);

                    MainContainer.Children.Add(new TextBlock() { Text = line, Margin = new Thickness(15, 5, 0, 0) });
                }
                else
                    MainContainer.Children.Add(new TextBlock() { Text = line });
            }
        }
    }
}