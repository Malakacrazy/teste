using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class HonorBidPrompt : AllPlayerPrompt
    {
        public string menuTitle;
        public System.Action<HonorBidPrompt> costHandler;
        public Dictionary<string, List<string>> prohibitedBids;
        public Duel duel;
        public Dictionary<string, int> bid;

        public HonorBidPrompt(Game game, string menuTitle = null, 
            System.Action<HonorBidPrompt> costHandler = null, 
            Dictionary<string, List<string>> prohibitedBids = null, 
            Duel duel = null) : base(game)
        {
            this.menuTitle = menuTitle ?? "Choose a bid";
            this.costHandler = costHandler;
            this.prohibitedBids = prohibitedBids ?? new Dictionary<string, List<string>>();
            this.duel = duel;
            this.bid = new Dictionary<string, int>();
        }

        public override bool ActiveCondition(Player player)
        {
            return !bid.ContainsKey(player.uuid);
        }

        public override bool CompletionCondition(Player player)
        {
            return bid.ContainsKey(player.uuid) && bid[player.uuid] > 0;
        }

        public override bool Continue()
        {
            bool completed = base.Continue();

            if (completed)
            {
                game.RaiseEvent(EventNames.OnHonorDialsRevealed, new { duel = this.duel }, () =>
                {
                    foreach (var player in game.GetPlayers())
                    {
                        player.honorBidModifier = 0;
                        var context = game.GetFrameworkContext();
                        GameActions.SetHonorDial(new SetDialProperties { value = bid[player.uuid] })
                            .Resolve(player, context);
                    }
                });

                if (costHandler != null)
                {
                    game.QueueSimpleStep(() => costHandler(this));
                }
                else
                {
                    game.QueueSimpleStep(() => TransferHonorAfterBid());
                }
            }

            return completed;
        }

        public void TransferHonorAfterBid(AbilityContext context = null)
        {
            context = context ?? game.GetFrameworkContext();
            var firstPlayer = game.GetFirstPlayer();
            
            if (firstPlayer.opponent == null)
                return;

            int difference = firstPlayer.honorBid - firstPlayer.opponent.honorBid;
            
            if (difference > 0)
            {
                game.AddMessage($"{firstPlayer} gives {firstPlayer.opponent} {difference} honor");
                GameActions.TakeHonor(new TakeHonorProperties { amount = difference, afterBid = true })
                    .Resolve(firstPlayer, context);
            }
            else if (difference < 0)
            {
                game.AddMessage($"{firstPlayer.opponent} gives {firstPlayer} {-difference} honor");
                GameActions.TakeHonor(new TakeHonorProperties { amount = -difference, afterBid = true })
                    .Resolve(firstPlayer.opponent, context);
            }
        }

        public override PromptInfo ActivePrompt(Player player)
        {
            var prohibitedPlayerBids = prohibitedBids.ContainsKey(player.uuid) ? 
                prohibitedBids[player.uuid] : new List<string>();
            
            var buttons = new[] { "1", "2", "3", "4", "5" }
                .Where(num => !prohibitedPlayerBids.Contains(num))
                .Select(num => new ButtonInfo { text = num, arg = num })
                .ToArray();

            return new PromptInfo
            {
                promptTitle = "Honor Bid",
                menuTitle = menuTitle,
                buttons = buttons
            };
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = "Waiting for opponent to choose a bid." };
        }

        public override bool MenuCommand(Player player, string bidValue, string method = null)
        {
            game.AddMessage($"{player} has chosen a bid.");
            
            if (int.TryParse(bidValue, out int bidInt))
            {
                bid[player.uuid] = bidInt;
                return true;
            }
            
            return false;
        }
    }
}
