using System;
using System.Linq;
using System.Speech.Recognition;

namespace WinLaunch
{
    internal class VoiceActivation
    {
        string[] launchCommands = new string[] { "hey winlaunch", "open winlaunch", "start winlaunch" };
        string[] closeCommands = new string[] { "close winlaunch", "bye winlaunch", "close", "bye" };

        bool initialized = false;
        SpeechRecognitionEngine recognizer;
        
        void InitRecognizer()
        {
            if (initialized)
                return;

            initialized = true;

            recognizer = new SpeechRecognitionEngine();
            //recognizer.LoadGrammar(new DictationGrammar());

            //load launch grammar
            Choices launchCommandChoices = new Choices(launchCommands);

            GrammarBuilder launchGrammarBuilder = new GrammarBuilder();
            launchGrammarBuilder.Append(launchCommandChoices);
            Grammar launchGrammar = new Grammar(launchGrammarBuilder);
            recognizer.LoadGrammar(launchGrammar);


            //load close grammar
            Choices closeCommandChoices = new Choices(closeCommands);

            GrammarBuilder closeGrammarBuilder = new GrammarBuilder();
            closeGrammarBuilder.Append(closeCommandChoices);
            Grammar closeGrammar = new Grammar(closeGrammarBuilder);
            recognizer.LoadGrammar(closeGrammar);

            //load item grammar
            //Choices ItemChoices = new Choices(SBM.IC.BuildItemGrammar());

            //GrammarBuilder itemGrammarBuilder = new GrammarBuilder();
            //itemGrammarBuilder.Append(ItemChoices);
            //Grammar itemGrammar = new Grammar(itemGrammarBuilder);

            //recognizer.LoadGrammar(itemGrammar);

            // Register a handler for the SpeechRecognized event.
            recognizer.SpeechRecognized += Recognizer_SpeechRecognized;
        }

        bool active = false;
        public void StartListening()
        {
            if (active)
                return;

            active = true;

            InitRecognizer();

            // Configure the input to the speech recognizer.
            recognizer.SetInputToDefaultAudioDevice();

            // Start asynchronous, continuous speech recognition.
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void StopListening()
        {
            if (!active)
                return;
            
            active = false;

            recognizer.RecognizeAsyncStop();
        }

        public event EventHandler OpenActivated;
        public event EventHandler CloseActivated;

        private void Recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            //check open commands
            var openList = launchCommands.ToList();

            if (openList.Contains(e.Result.Text))
            {
                OpenActivated(this, EventArgs.Empty);
                return;
            }


            //check close commands
            var closeList = closeCommands.ToList();

            if (closeList.Contains(e.Result.Text))
            {
                CloseActivated(this, EventArgs.Empty);
                return;
            }
        }
    }
}
