using SocketIOClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace WinLaunch
{
    partial class MainWindow : Window
    {
        string PasswordEncryptionKey = "oDuh7NN3Kw7EqEFv0FrRCJTKZizzmUz7FGjsRojGSN4=";
        string AssistantURL = "http://localhost:3001";
        //string AssistantURL = "http://assistant.winlaunch.org:3000";
    }
}
