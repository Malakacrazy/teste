namespace L5RGame
{
    /// <summary>
    /// Interface for game steps that can be processed by the game pipeline
    /// </summary>
    public interface IGameStep
    {
        bool Execute();
        bool IsComplete();
        bool Continue();
        void OnMenuCommand(Player player, string command, string arg, string uuid, string method);
        void OnCardClicked(Player player, BaseCard card);
        void OnRingClicked(Player player, Ring ring);
        void Initialize();
        void Cleanup();
    }
}
