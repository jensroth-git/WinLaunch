using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinLaunch
{
    public partial class AddLink : Window
    {
        public string URL = "";

        public AddLink()
        {
            InitializeComponent();

            tbxLink.Focus();
        }

        private void tbxLink_TextChanged(object sender, TextChangedEventArgs e)
        {
            URL = tbxLink.Text;
            
            if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute) && !URL.StartsWith("http://") && !URL.StartsWith("https://") && !URL.StartsWith("ftp://") && !URL.StartsWith("file://"))
            {
                //default to http
                URL = "http://" + URL;
            }

            if (Uri.IsWellFormedUriString(URL, UriKind.Absolute))
            {
                //correct url
                btnConfirm.IsEnabled = true;
            }
            else
            {
                //incorrect url
                btnConfirm.IsEnabled = false;
            }
        }

        private void ConfirmClicked(object sender, RoutedEventArgs e)
        {
            URL = tbxLink.Text;

            if (!Uri.IsWellFormedUriString(URL, UriKind.Absolute) && !URL.StartsWith("http://") && !URL.StartsWith("https://") && !URL.StartsWith("ftp://") && !URL.StartsWith("file://"))
            {
                //default to http
                URL = "http://" + URL;
            }

            DialogResult = true;
            Close();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
