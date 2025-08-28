namespace L5RGame
{
    /// <summary>
    /// Interface for game steps that can be processed by the game pipeline
    /// </summary>
    public interface IGameStep
    {
        bool Continue();
        bool IsComplete();
        void OnMenuCommand(Player player, string command, string arg, string uuid, string method);
        void OnCardClicked(Player player, BaseCard card);
        void OnRingClicked(Player player, Ring ring);
    }
}
