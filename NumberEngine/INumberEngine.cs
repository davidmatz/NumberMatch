
namespace NumberMatch
{
    public interface INumberEngine
    {
        bool GameOver { get; }
        string HighScore { get; }
        string InvaderCount { get; }
        string Invaders { get; }
        int Lives { get; }
        char Missle { get; }
        string MissleCount { get; }
        string Score { get; }
        string Stage { get; }
        bool StageOver { get; set; }

        void Advance();
        void Aim();
        void Attack();
        void ClearHighScore();
    }
}
