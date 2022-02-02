using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Windows;
using System.Xml;

namespace WinLaunch
{
    internal class UpdateCheck
    {
        private static Thread CheckThread;
        private static volatile bool running = false;

        public static void RunThreaded()
        {
            CheckThread = new Thread(new ThreadStart(() =>
            {
                while (true)
                {
                    try
                    {
                        Run();
                    }
                    catch { }
                    Thread.Sleep(TimeSpan.FromHours(1));
                }
            }));

            CheckThread.Start();
        }

        public static bool Run()
        {
            if (running)
                return false;

            running = true;

            Version updateVersion = null;
            string updateURL = null;
            string updateSignature = null;

            try
            {
                string UpdateInfoURL = "http://bit.ly/1sjlwOI";

                using (var wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.99 Safari/537.36");
                    string xml = wc.DownloadString(UpdateInfoURL);

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);

                    string versionText = doc.GetElementsByTagName("version")[0].InnerText;
                    updateVersion = new Version(versionText);
                    updateURL = doc.GetElementsByTagName("url")[0].InnerText;
                    updateSignature = doc.GetElementsByTagName("signature")[0].InnerText;

                    //verify signature
                    string unsignedMessage = updateURL + updateVersion;
                    byte[] unsignedMessageBytes = System.Text.Encoding.Unicode.GetBytes(unsignedMessage);
                    byte[] signedMessageBytes = Convert.FromBase64String(updateSignature);

                    //create RSA public key
                    string pubKeyString = "<RSAKeyValue><Modulus>nPnBFiUsgdANJct8U9CgFLMh0ygdBw8PiZ7G9eBn1K5g9CMlLAaIccRMXP+jl5OZ4fRs22DfiYhMYqkcF+pry31cP3osKlTx0/WsFVonuUfvm4urfM9KT8+nZwJ+37kHcq1f6MHdmb4dbS57XFWiBFWFmPRKccpkIgiXjgrh5JzBBvBS7Ig88M7eUTo/laX6etmMwAodIzPCDswILaoWLhu3QVKmO81Hci5EtREmjcnS9TWMJ6Czdh3/Z1fEAPJiQB2wTxj/CpyH7B+pS0Y/qA/4AqYgH/eTbnk7JHkmhkBSyPcA4Xy9yJrljhws/v9zWcARtSDSz3BEr+QPGnoPEQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
                    RSACryptoServiceProvider publicRSA = new RSACryptoServiceProvider();
                    publicRSA.FromXmlString(pubKeyString);

                    if (!publicRSA.VerifyData(unsignedMessageBytes, CryptoConfig.MapNameToOID("SHA512"), signedMessageBytes))
                    {
                        //invalid signature
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                running = false;
                throw e;
            }

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.CompareTo(updateVersion) < 0)
            {
                MainWindow.WindowRef.Dispatcher.Invoke(new Action(() =>
                {
                    if (MessageBox.Show(MainWindow.WindowRef, "Version " + updateVersion.ToString() + " " + TranslationSource.Instance["UpdateAvailableInfo"], TranslationSource.Instance["UpdateAvailable"], MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            //Run the updater
                            ProcessStartInfo startInfo = new ProcessStartInfo();
                            startInfo.UseShellExecute = true;
                            startInfo.FileName = "WinLaunchUpdate.exe";
                            Process.Start(startInfo);

                            MainWindow.WindowRef.Close();
                            Environment.Exit(0);
                        }
                        catch
                        {
                            MessageBox.Show(MainWindow.WindowRef, "unable to find Update.exe, please repair WinLaunch using the Setup!", "Winlaunch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Environment.Exit(0);
                        }
                    }
                }));

                running = false;
                return true;
            }

            running = false;
            return false;
        }
    }
}