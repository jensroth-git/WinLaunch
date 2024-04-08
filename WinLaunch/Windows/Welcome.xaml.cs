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
            "#Changelog 0.8.0.0 beta",
            "---",
            "- added WinLaunch Assistant",
            "- added option to hide item and folder text",
            "- added voice activation",
            "- increased icon size range",
            "- fixed Windows key activation on Windows 11",
            "- fixed horizontal scrolling skipping pages",
            "- fixed taskbar position update while in desk mode",
            "- updated item saving",
            "- added refresh all installed apps option",
            "- updated patron list",
            "",
            "#Changelog 0.7.3.0",
            "---",
            "- added horizontal scrolling to change pages",
            "- fixed icon restore",
            "- updated patron list",
            "",
            "#Changelog 0.7.2.2",
            "---",
            "- added 3 extra column spaces",
            "- fixed misconfigured working directory",
            "- updated the Spanish translation by Damián Roig",
            "",
            "#Changelog 0.7.2.1",
            "---",
            "- updated portable mode",
            "- added option to sort folder contents only",
            "- updated the Spanish translation by Damián Roig",
            "",
            "#Changelog 0.7.2.0",
            "---",
            "- updated the settings",
            "- added option to make WinLaunch portable",
            "- updated icon loading and made it more robust",
            "- added option to update silently",
            "- updated the Spanish translation by Damián Roig",
            "",
            "#Changelog 0.7.1.2",
            "---",
            "- added an option to hide miniature icons from folders",
            "- added search keywords to items",
            "- fixed overflow issue in theme loading",
            "- fixed not being able to use the context menu in tablet mode",
            "",
            "#Changelog 0.7.1.1",
            "---",
            "- removed miniature icons from folders with custom icons",
            "- added an option to hide page indicators",
            "- updated desk mode",
            "- fix selection not working sometimes",
            "",
            "#Changelog 0.7.1.0",
            "---",
            "- added gamepad support for selecting items",
            "- added gamepad support for activating WinLaunch",
            "- fix open location on .lnk files",
            "- fix crash when hitting enter while no search results were found",
            "- update crash reporter to use SSL",
            "- updated the Indonesian translation by jovanzers",
            "",
            "#Changelog 0.7.0.2",
            "---",
            "- fix item deletion didnt check free item placement",
            "- fix crash when closing window sometimes",
            "",
            "#Changelog 0.7.0.1",
            "---",
            "- updated french translation by Sébastien Gellet",
            "- fix hotkey not applying settings sometimes",
            "- fade in search bar",
            "",
            "#Changelog 0.7.0.0",
            "---",
            "- fixed windows key activation blocking shortcuts",
            "- updated the Indonesian translation by jovanzers",
            "",
            "#Changelog 0.6.9.0",
            "---",
            "- added double tap Ctrl activation",
            "- added double tap Alt activation",
            "- fixed pages not updating sometimes",
            "- fixed sort checkbox being checked when not activated",
            "- updated the german translation by a lovely discord user",
            "",
            "#Changelog 0.6.8.5",
            "---",
            "- added 2 additional optional columns",
            "- fixed automatically sort after adjusting the columns / rows",
            "- fixed restore from backup not working sometimes",
            "- updated the Indonesian translation by jovanzers",
            "",
            "#Changelog 0.6.8.4",
            "---",
            "- fix items get deleted when searching while a folder was open",
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