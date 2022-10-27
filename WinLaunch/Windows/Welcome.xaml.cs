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
            "#Changelog 0.6.3.0",
            "---",
            "- added windows key activation",
            "",
            "#Changelog 0.6.3.0",
            "---",
            "- added option to hide the search bar",
            "- added bulgarian translation by Maxim Borisov"
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