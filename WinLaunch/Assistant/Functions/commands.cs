using SocketIOClient;
using System;
using System.Diagnostics;
using System.Windows;

namespace WinLaunch
{
    public class AssistantExecutedCommand : DependencyObject
    {
        public string Text { get; set; }
        public string File { get; set; }
        public string Parameters { get; set; }

        public bool showParameters { get; set; } = false;

        public AssistantExecutedCommand(string file, string parameters)
        {
            File = file;

            if (parameters != null)
            {
                showParameters = true;
                Parameters = parameters;
            }
        }
    }

    partial class MainWindow : Window
    {
        //TODO: run threaded to not block the UI thread
        private string ExecuteProcessAndGetOutput(string file, string parameters)
        {
            string output = string.Empty;
            parameters += " && exit";
            using (Process process = new Process())
            {
                process.StartInfo.FileName = file;
                process.StartInfo.Arguments = parameters;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.StartInfo.CreateNoWindow = true; // Do not create the black window
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Hides the window

                process.Start();

                output = process.StandardOutput.ReadToEnd();

                if (!process.WaitForExit(1)) // Wait for the process to exit with a timeout.
                {
                    process.Kill(); // Forcefully kill the process if it doesn't exit in time.
                }
            }

            return output;
        }

        void shell_execute(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                string output = string.Empty;
                try
                {
                    string file = args.GetValue<string>();
                    string parameters = args.GetValue<string>(1);
                    bool hide = args.GetValue<bool>(2);

                    icAssistantContent.Items.Add(new AssistantExecutedCommand(file, parameters)
                    {
                        Text = TranslationSource.Instance["AssistantExecutedCommand"]
                    });

                    MovePendingIndicatorToBottom();
                    scvAssistant.ScrollToBottom();

                    if (Settings.CurrentSettings.ExecuteAssistantCommands)
                    {
                        try
                        {
                            output = ExecuteProcessAndGetOutput(file, parameters);
                            await args.CallbackAsync(output);
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                await args.CallbackAsync(ex.Message);
                            }
                            catch { }
                        }

                        if (hide)
                        {
                            AssistantDelayClose = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            await args.CallbackAsync("User disabled commands, they can be enabled again in the settings");
                        }
                        catch { }
                    }
                }
                catch { }
            }));
        }

        private string RunPythonAndGetOutput(string code)
        {
            string output = string.Empty;
            string error = string.Empty;

            //write text into a .py file
            string tempFile = System.IO.Path.GetTempFileName();
            System.IO.File.WriteAllText(tempFile, code);

            //print the code to trace
            //Console.WriteLine("Executing Python Code:" + code);

            //run python
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "python";
                process.StartInfo.Arguments = tempFile;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.StartInfo.CreateNoWindow = true; // Do not create the black window
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; // Hides the window

                process.Start();

                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(1)) // Wait for the process to exit with a timeout.
                {
                    process.Kill(); // Forcefully kill the process if it doesn't exit in time.
                }
            }

            return output + "\n" + error;
        }

        void run_python(SocketIOResponse args)
        {
            Dispatcher.BeginInvoke(new Action(async () =>
            {
                string output = string.Empty;
                try
                {
                    string code = args.GetValue<string>();
                    icAssistantContent.Items.Add(new AssistantExecutedCommand("Python code Run", code)
                    {
                        Text = TranslationSource.Instance["AssistantExecutedCommand"]
                    });

                    MovePendingIndicatorToBottom();
                    scvAssistant.ScrollToBottom();

                    if (Settings.CurrentSettings.ExecuteAssistantCommands)
                    {
                        try
                        {
                            Trace.WriteLine("Executing Python Code:\n" + code);//Debugging
                            output = RunPythonAndGetOutput(code);
                            Trace.WriteLine("Python Output\n" + output);
                            await args.CallbackAsync(output);//Debugging
                        }
                        catch (Exception ex)
                        {
                            try
                            {
                                await args.CallbackAsync(ex.Message);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        try
                        {
                            await args.CallbackAsync("User disabled python scripting, they can be enabled again in the settings");
                        }
                        catch { }
                    }
                }
                catch { }
            }));
        }
    }
}
