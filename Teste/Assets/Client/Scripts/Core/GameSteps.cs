using System;
using System.Collections.Generic;

namespace L5RGame
{
    public class MenuPrompt : BaseStep, IGameStep
    {
        private Player player;
        private object context;
        private MenuPromptProperties properties;

        public MenuPrompt(Game game) : base(game) { }
        public MenuPrompt(Game game, Player player, string title, string text) : base(game) { }
        public MenuPrompt(Game game, Player targetPlayer, object contextObj, MenuPromptProperties props) : base(game)
        {
            player = targetPlayer;
            context = contextObj;
            properties = props;
        }

        public override bool Continue()
        {
            return false; // Wait for player input
        }

        public override string GetDebugInfo()
        {
            return $"MenuPrompt - Player: {player?.name}";
        }
    }

    public class HonorBidPrompt : BaseStep, IGameStep
    {
        private string activePromptTitle;
        private System.Action<int> costHandler;
        private List<int> prohibitedBids;
        private Duel duel;

        public HonorBidPrompt(Game game) : base(game) { }
        public HonorBidPrompt(Game game, string title, System.Action<int> handler, List<int> prohibited, Duel associatedDuel = null) : base(game)
        {
            activePromptTitle = title;
            costHandler = handler;
            prohibitedBids = prohibited ?? new List<int>();
            duel = associatedDuel;
        }

        public override bool Continue()
        {
            return false; // Wait for player input
        }

        public override string GetDebugInfo()
        {
            return $"HonorBidPrompt - Title: {activePromptTitle} - Duel: {duel != null}";
        }
    }

    public class GameWonPrompt : BaseStep, IGameStep
    {
        private Player winner;

        public GameWonPrompt(Game game) : base(game) { }
        public GameWonPrompt(Game game, Player winnerPlayer) : base(game)
        {
            winner = winnerPlayer;
        }

        public override bool Continue()
        {
            return true; // Immediately complete
        }

        public override string GetDebugInfo()
        {
            return $"GameWonPrompt - Winner: {winner?.name ?? "Unknown"}";
        }
    }


    public class HandlerMenuPrompt : BaseStep, IGameStep
    {
        private Player player;
        private HandlerMenuPromptProperties properties;

        public HandlerMenuPrompt(Game game) : base(game) { }
        public HandlerMenuPrompt(Game game, Player player, object properties) : base(game) { }
        public HandlerMenuPrompt(Game game, Player targetPlayer, HandlerMenuPromptProperties props) : base(game)
        {
            player = targetPlayer;
            properties = props;
        }

        public override bool Continue()
        {
            return false; // Wait for player input
        }

        public override string GetDebugInfo()
        {
            return $"HandlerMenuPrompt - Player: {player?.name}";
        }
    }

    public class SelectRingPrompt : BaseStep, IGameStep
    {
        private Player player;
        private SelectRingPromptProperties properties;

        public SelectRingPrompt(Game game) : base(game) { }
        public SelectRingPrompt(Game game, Player targetPlayer, SelectRingPromptProperties props) : base(game)
        {
            player = targetPlayer;
            properties = props;
        }

        public override bool Continue()
        {
            return false; // Wait for player input
        }

        public override string GetDebugInfo()
        {
            return $"SelectRingPrompt - Player: {player?.name}";
        }
    }

    public class SetupPhase : BaseStepWithPipeline, IGameStep
    {
        public SetupPhase(Game game) : base(game) { }

        public override string GetDebugInfo()
        {
            return "SetupPhase - Game setup";
        }
    }

    public class SimultaneousEffectWindow : BaseStep, IGameStep
    {
        private List<EffectChoice> choices = new List<EffectChoice>();

        public SimultaneousEffectWindow(Game game) : base(game) { }

        public void AddChoice(EffectChoice choice)
        {
            choices.Add(choice);
        }

        public override bool Continue()
        {
            // Process all choices and complete
            foreach (var choice in choices)
            {
                choice.Execute();
            }
            return true;
        }

        public override string GetDebugInfo()
        {
            return $"SimultaneousEffectWindow - Choices: {choices.Count}";
        }
    }

    public class SelectCardPrompt : BaseStep, IGameStep
    {
        private Player player;
        private SelectCardPromptProperties properties;

        public SelectCardPrompt(Game game) : base(game) { }
        public SelectCardPrompt(Game game, Player player, object properties) : base(game) { }
        public SelectCardPrompt(Game game, Player targetPlayer, SelectCardPromptProperties props) : base(game)
        {
            player = targetPlayer;
            properties = props;
        }

        public override bool Continue()
        {
            return false; // Wait for player input
        }

        public override string GetDebugInfo()
        {
            return $"SelectCardPrompt - Player: {player?.name}";
        }
    }
}
