namespace L5RGame
{
    public interface IGameStep
    {
        bool Execute();
        bool IsComplete();
    }
}
