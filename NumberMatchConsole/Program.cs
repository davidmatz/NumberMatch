using System;
using System.Configuration;
using System.Threading;
using System.Timers;
using log4net;
using Timer = System.Timers.Timer;

namespace NumberMatch
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        private static readonly string BlankLine = new string(' ', Console.WindowWidth);

        private const int DisplayLine = 3;
        private const int InformationLine = 1;
        private const int MessageInverval = 2000;
        private const int RefreshInverval = 200;
        private const int StatusLine = 2;
        private const int TitleLine = 0;

        private static int _invaderSpeed;
        private static Timer _invaderTimer;
        private static INumberEngine _numberEngine;
        private static int _refreshSpeed = RefreshInverval;
        private static Timer _refreshTimer;

        enum DifficultyLevel
        {
            Easy = 6000,
            Moderate = 3000,
            Original = 1400,
            Hard = 1000
        }

        private static void Main(string[] args)
        {
            Initialize();

            Console.CursorVisible = false;

            Log.Debug("Starting game.");
            DisplayWelcome();

            while (PlayGame()) ;

            Log.Debug("Exiting game.");
        }

        private static void ClearInformation()
        {
            Console.SetCursorPosition(0, InformationLine);
            Console.Write(BlankLine);
        }

        private static void ClearStatusAndDisplay()
        {
            Console.SetCursorPosition(0, StatusLine);
            Console.Write(BlankLine);
            Console.SetCursorPosition(0, DisplayLine);
            Console.Write(BlankLine);
        }

        private static void DisplayGameOver()
        {
            Console.SetCursorPosition(0, StatusLine);
            Console.Write("GAME OVER");
        }

        private static void DisplayHighScore()
        {
            ClearStatusAndDisplay();

            if (_numberEngine.GameOver)
                DisplayGameOver();

            Console.SetCursorPosition(0, DisplayLine);
            Console.Write(_numberEngine.HighScore.PadLeft(8));
            Thread.Sleep(MessageInverval);
        }

        private static void DisplayInvaderAndMissleCount()
        {
            ClearStatusAndDisplay();
            Console.SetCursorPosition(0, DisplayLine);
            Console.Write(string.Format("{0}-{1}", _numberEngine.InvaderCount.PadLeft(5), _numberEngine.MissleCount));
            Thread.Sleep(MessageInverval);
        }

        private static void DisplayMissleAndInvaders()
        {
            Console.SetCursorPosition(0, DisplayLine);

            string display = string.Format("{0}{1}{2}",
                _numberEngine.Missle,
                GetLives(),
                _numberEngine.Invaders);

            Console.Write(display);
            Log.Debug(display);
        }

        private static void DiplayPlayAgainOrQuit()
        {
            ClearInformation();
            Console.SetCursorPosition(0, InformationLine);
            Console.Write(". = Play Again, Esc = Quit");
        }

        private static void DisplayStageAndScore()
        {
            ClearStatusAndDisplay();

            if (_numberEngine.GameOver)
                DisplayGameOver();

            Console.SetCursorPosition(0, DisplayLine);
            string display = string.Format("{0}{1}{2}",
                _numberEngine.Stage,
                GetLives(),
                _numberEngine.Score);

            Console.Write(display);
            Log.Debug(display);

            if (!_numberEngine.GameOver)
                Thread.Sleep(MessageInverval);
        }

        private static void DisplayWelcome()
        {
            ClearStatusAndDisplay();

            Console.SetCursorPosition(0, TitleLine);
            Console.WriteLine("Number Match Game");
            Console.SetCursorPosition(0, InformationLine);
            Console.WriteLine(". = Start/Aim, Enter = Fire, Esc = Quit");
            Console.SetCursorPosition(0, DisplayLine);
            Console.WriteLine("       0.");
        }

        private static int GetInvaderSpeed()
        {
            int invaderSpeed = (int)DifficultyLevel.Original;

            try
            {
                string difficultyLevel = ConfigurationManager.AppSettings["DifficultyLevel"];

                if (!string.IsNullOrWhiteSpace(difficultyLevel))
                    invaderSpeed = (int)((DifficultyLevel)Enum.Parse(typeof(DifficultyLevel), difficultyLevel));
            }
            catch
            {
                Log.Error("Error reading difficulty level from config file, using default.");
            }

            return invaderSpeed;
        }

        private static char GetLives()
        {
            char life;

            // Return character symbol representation for number of lives
            switch (_numberEngine.Lives)
            {
                case 2:
                    life = '=';
                    break;
                case 3:
                    life = '≡';
                    break;
                case 1:
                default:
                    life = '-';
                    break;
            }

            return life;
        }

        private static bool GetPlayAgainStatus()
        {
            bool playAgain = true;

            DisplayStageAndScore();
            DiplayPlayAgainOrQuit();

            ConsoleKeyInfo keyInfo = Console.ReadKey(true);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Add:
                case ConsoleKey.Decimal:
                case ConsoleKey.OemPeriod:
                case ConsoleKey.UpArrow:
                    playAgain = true;
                    break;
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    playAgain = false;
                    break;
                default:
                    break;
            }

            return playAgain;
        }

        private static void Initialize()
        {
            _invaderSpeed = GetInvaderSpeed();

            // Set up the invader timer, which controls the speed of the invaders
            _invaderTimer = new Timer(_invaderSpeed);
            _invaderTimer.Elapsed += OnInvaderEvent;

            // Setup the refresh timer, which controls the screen refresh
            _refreshTimer = new Timer(_refreshSpeed);
            _refreshTimer.Elapsed += OnRefreshEvent;
        }

        private static void OnInvaderEvent(Object source, ElapsedEventArgs e)
        {
            _numberEngine.Advance();
        }

        private static void OnRefreshEvent(Object source, ElapsedEventArgs e)
        {
            DisplayMissleAndInvaders();
        }

        private static bool PlayGame()
        {
            _numberEngine = new NumberEngine();
            bool gameStarted = false;

            bool keepPlaying = true;
            while (!_numberEngine.GameOver && keepPlaying)
            {
                // Check if stage is over
                if (_numberEngine.StageOver)
                    StartStage();

                // Check if key has been pressed
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(true);

                    switch (consoleKeyInfo.Key)
                    {
                        case ConsoleKey.Add:
                        case ConsoleKey.Decimal:
                        case ConsoleKey.OemPeriod:
                        case ConsoleKey.UpArrow:
                            if (!gameStarted)
                            {
                                gameStarted = true;
                                StartGame();
                            }

                            _numberEngine.Aim();
                            break;
                        case ConsoleKey.Enter:
                            _numberEngine.Attack();
                            break;
                        case ConsoleKey.Escape:
                        case ConsoleKey.Q:
                            keepPlaying = false;
                            break;
                        default:
                            break;
                    }
                }
            }

            StopTimers();

            if (keepPlaying)
                keepPlaying = GetPlayAgainStatus();

            return keepPlaying;
        }

        private static void StartGame()
        {
            DisplayHighScore();
            DisplayInvaderAndMissleCount();
            StartTimers();
        }

        private static void StartStage()
        {
            // Stop game play in order to display current score
            StopTimers();
            DisplayStageAndScore();

            // Start game play again
            _numberEngine.StageOver = false;
            StartTimers();
        }

        private static void StartTimers()
        {
            _refreshTimer.Enabled = true;
            _invaderTimer.Enabled = true;
            Log.Debug("Timers enabled.");
        }

        private static void StopTimers()
        {
            _refreshTimer.Enabled = false;
            _invaderTimer.Enabled = false;
            Log.Debug("Timers disabled.");
        }
    }
}
