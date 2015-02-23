using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Text;
using log4net;

namespace NumberMatch
{
    public class NumberEngine : INumberEngine
    {
        // Fields
        private static readonly ILog Log = LogManager.GetLogger(typeof(NumberEngine));
        private static readonly char[] Missles = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '∩' };

        private const char CommandShip = '∩';
        private const int CommandShipValue = 300;
        private const int MaxInvaders = 16;
        private const int MaxInvadersViewable = 6;
        private const int MaxMissles = 30;
        private const int MaxStage = 9;
        private const char NoInvader = ' ';

        private Queue<int> _invaders = new Queue<int>();
        private List<char> _invadersViewable = new List<char>();
        private int _invadersRemaining;
        private int _lastThreeInvaderCurrentPosition;
        private int[] _lastThreeInvaderPositions = new int[3];
        private int _misslePositon;
        private int _misslesRemaining;
        private int _score;
        private int _stage;

        // Properties
        public bool GameOver { get; private set; }
        public string HighScore { get { return ConfigurationManager.AppSettings["HighScore"]; } }
        public string InvaderCount { get; private set; }
        public string Invaders { get { return GetInvadersViewable(); } }
        public int Lives { get; private set; }
        public char Missle { get; private set; }
        public string MissleCount { get; private set; }
        public string Score { get { return _score.ToString("D6"); } }
        public string Stage { get { return _stage.ToString(); } }
        public bool StageOver { get; set; }

        // Constructor
        public NumberEngine()
        {
            GameOver = false;
            StageOver = false;
            Lives = 3;
            _score = 0;

            SetStage(1);
        }

        // Methods
        public void Advance()
        {
            // If the nearest position is not an invader, there is room to advance
            if (_invadersViewable[0] == NoInvader)
            {
                // Remove the space (not an invader)
                _invadersViewable.RemoveAt(0);

                // Get the next invader, and add to the end
                _invadersViewable.Add(GetNextInvader());
            }
            else
            {
                Log.Debug("Killed.");

                // Invaders have attacked
                StageOver = true;
                Lives--;

                // Repeat this stage if lives remaining
                if (Lives > 0)
                    ResetStage();
                else
                    GameOver = true;

                // Save the high score if this is the end of the game or stage
                SaveHighScore(_score);
            }
        }

        public void Aim()
        {
            Missle = Missles[_misslePositon++ % 11];
        }

        public void Attack()
        {
            if (_misslesRemaining > 0)
            {
                // Check for a match beween the missle and the viewable invader(s)
                for (int i = 0; i < _invadersViewable.Count; i++)
                {
                    if (Missle == _invadersViewable[i])
                    {
                        CalculateScore(Missle, i);

                        SetLastInvaderPositionDestroyed(i);

                        // Remove the attacked invader
                        _invadersViewable.RemoveAt(i);

                        // Add a new invader to replace the removed invader
                        _invadersViewable.Add(GetNextInvader());

                        // All invaders have been attacked
                        if (GetTotalInvaders() == 0)
                        {
                            StageOver = true;

                            // Next stage
                            _stage++;

                            // Game is over if all stages cleared
                            if (_stage >= MaxStage)
                                GameOver = true;
                            else
                                SetStage(_stage);

                            // Save the high score if this is the end of the game or stage
                            SaveHighScore(_score);
                        }

                        // Only attack the first invader found
                        return;
                    }
                }

                // If out of missles, game is over
                if (--_misslesRemaining == 0)
                    GameOver = true;
            }
        }

        public void ClearHighScore()
        {
            SaveHighScore(0);
        }

        private void CalculateScore(char invader, int position)
        {
            // Calculate the score:
            // The 1st stage has Stage = 1, so scoring is [10][20][30][40][50][60],
            // the 2nd stage has Stage = 2, so scoring is [20][30][40][50][60][70], etc.
            // The command ship is always worth the same.
            if (invader == CommandShip)
                _score += CommandShipValue;
            else
                _score += (10 * (position + _stage));
        }

        private bool CommandShipEarned()
        {
            bool commandShipEarned = false;

            // If the last three destroyed invaders were in position 0, 1, and 2
            if ((_lastThreeInvaderPositions[(_lastThreeInvaderCurrentPosition) % 3] == 0) &&
                (_lastThreeInvaderPositions[(_lastThreeInvaderCurrentPosition + 1) % 3] == 1) &&
                (_lastThreeInvaderPositions[(_lastThreeInvaderCurrentPosition + 2) % 3] == 2))
                commandShipEarned = true;

            return commandShipEarned;
        }

        private void CreateInvaders(int numberOfInvaders)
        {
            Random rand = new Random();

            // Generate a list of random numbers (invaders) from 0 to 9
            for (int i = 0; i < numberOfInvaders; i++)
                _invaders.Enqueue(rand.Next(0, 10));
        }

        private string GetInvadersViewable()
        {
            StringBuilder invaders = new StringBuilder();

            // Convert the list of invaders to a StringBuilder object
            foreach (char invader in _invadersViewable)
                invaders.Append(invader);

            // Convert the StringBuilder object to a string
            return invaders.ToString();
        }

        private char GetNextInvader()
        {
            char nextInvader;

            if (_invadersRemaining > 0)
            {
                // Command ship is considered a bonus, and not an invader
                if (CommandShipEarned())
                    nextInvader = CommandShip;
                else
                {
                    nextInvader = Convert.ToChar(48 + (_invaders.Dequeue()));
                    _invadersRemaining--;
                }
            }
            else
                nextInvader = NoInvader;

            return nextInvader;
        }

        private int GetTotalInvaders()
        {
            int invadersViewable = 0;

            // Calculate the number of invaders viewabled and remaining
            foreach (char invader in _invadersViewable)
            {
                if (invader != NoInvader)
                    invadersViewable++;
            }

            Log.DebugFormat("Total: {0}", invadersViewable + _invadersRemaining);

            return invadersViewable + _invadersRemaining;
        }

        private void ResetStage()
        {
            Log.Debug("stage: " + _stage);

            StageOver = false;

            // Initialize first missle
            _misslePositon = 0;

            // Same number of invaders as before (viewable and remaining)
            SetInvaders(GetTotalInvaders());
        }

        private void SaveHighScore(int highScore)
        {
#if DEBUG
            // Force use of non-vshost config file to persist high score
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(Assembly.GetEntryAssembly().Location);
#else
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
#endif
            configuration.AppSettings.Settings["HighScore"].Value = highScore.ToString("D6");
            configuration.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
        }

        private void SetInvaders(int numberOfInvaders)
        {
            Log.Debug("Number of invaders: " + numberOfInvaders);

            _invadersRemaining = numberOfInvaders;

            CreateInvaders(numberOfInvaders);

            // Start with no invaders viewable by adding spaces except for the last viewable position
            _invadersViewable.Clear();
            for (int i = 0; i < (MaxInvadersViewable - 1); i++)
                _invadersViewable.Add(' ');

            // Advance the first invader to the last viewable position
            _invadersViewable.Add(GetNextInvader());

            // Reset last three invaders positions
            _lastThreeInvaderCurrentPosition = 3;
            for (int i = 0; i < 3; i++)
                _lastThreeInvaderPositions[(_lastThreeInvaderCurrentPosition + i) % 3] = 0;
        }

        private void SetLastInvaderPositionDestroyed(int invaderPosition)
        {
            // Keep the last three invader positions
            _lastThreeInvaderPositions[_lastThreeInvaderCurrentPosition++ % 3] = invaderPosition;

            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0}, {1}, {2}",
                    _lastThreeInvaderPositions[0],
                    _lastThreeInvaderPositions[1],
                    _lastThreeInvaderPositions[2]);
            }
        }

        private void SetStage(int stage)
        {
            _stage = stage;

            Log.Debug("stage: " + stage);

            // Initialize missle and invader count
            MissleCount = MaxMissles.ToString();
            InvaderCount = MaxInvaders.ToString();

            // Initialize missles
            _misslesRemaining = MaxMissles;
            _misslePositon = 0;

            // Initialize invaders
            SetInvaders(MaxInvaders);
        }
    }
}
