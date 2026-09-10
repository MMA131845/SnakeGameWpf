using System.Windows.Media;

namespace SnakeGame
{
    public abstract class GameModeBase
    {
        public abstract string Name { get; }
        public abstract int FoodCount { get; }
        public abstract int EnemyCount { get; }
        public abstract float AiDifficulty { get; }
        public virtual bool HasCaptureZone => false;
        public virtual bool HasTimedRanking => false;

        public MainWindow Form { get; private set; }
        public void SetForm(MainWindow form) => Form = form;

        public virtual void Initialize() { }
        public virtual void UpdateBeforeMove() { }
        public virtual void UpdateAfterMove() { }
        public virtual void UpdateAI() { }
        public virtual void DrawUI(DrawingContext dc) { }
    }
}