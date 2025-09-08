using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class AllPlayerPrompt : UiPrompt
    {
        public AllPlayerPrompt(Game game) : base(game)
        {
        }

        public virtual bool ActiveCondition(Player player)
        {
            return !CompletionCondition(player);
        }

        public virtual bool CompletionCondition(Player player)
        {
            return false;
        }

        public override bool IsComplete()
        {
            return game.GetPlayers().All(player => CompletionCondition(player));
        }
    }
}
