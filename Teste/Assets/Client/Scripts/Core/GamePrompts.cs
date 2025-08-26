using UnityEngine;
using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game prompts
    /// </summary>
    public abstract class BasePrompt : IGameStep
    {
        protected Game game;
        protected Player player;
        
        public BasePrompt(Game gameInstance, Player promptPlayer)
        {
            game = gameInstance;
            player = promptPlayer;
        }
        
        public abstract bool Continue();
        public abstract void OnMenuCommand(Player player, string command, string arg, string uuid, string method);
        public abstract void OnCardClicked(Player player, BaseCard card);
        public abstract void OnRingClicked(Player player, Ring ring);
        public virtual void Initialize() { }
        public virtual void Cleanup() { }
    }
    
    /// <summary>
    /// Menu prompt for simple choice selection
    /// </summary>
    public class MenuPrompt : BasePrompt
    {
        private object contextObj;
        private MenuPromptProperties properties;
        
        public MenuPrompt(Game game, Player player, object context, MenuPromptProperties props) 
            : base(game, player)
        {
            contextObj = context;
            properties = props;
        }
        
        public override bool Continue()
        {
            // Display menu prompt
            game.AddMessage("Waiting for {0} to make a choice...", player);
            return false; // Wait for player input
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (properties.onSelect != null)
            {
                bool result = properties.onSelect(player, command);
                if (result)
                {
                    // Prompt completed successfully
                    game.pipeline.Continue();
                }
            }
        }
        
        public override void OnCardClicked(Player player, BaseCard card) { }
        public override void OnRingClicked(Player player, Ring ring) { }
    }
    
    /// <summary>
    /// Handler menu prompt for more complex choices
    /// </summary>
    public class HandlerMenuPrompt : BasePrompt
    {
        private HandlerMenuPromptProperties properties;
        
        public HandlerMenuPrompt(Game game, Player player, HandlerMenuPromptProperties props)
            : base(game, player)
        {
            properties = props;
        }
        
        public override bool Continue()
        {
            game.AddMessage("Waiting for {0} to select from available options...", player);
            return false;
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Find matching choice by command/arg
            var choice = properties.choices?.Find(c => c.ToString() == command);
            if (choice != null && properties.onSelect != null)
            {
                bool result = properties.onSelect(player, choice);
                if (result)
                {
                    game.pipeline.Continue();
                }
            }
        }
        
        public override void OnCardClicked(Player player, BaseCard card) { }
        public override void OnRingClicked(Player player, Ring ring) { }
    }
    
    /// <summary>
    /// Card selection prompt
    /// </summary>
    public class SelectCardPrompt : BasePrompt
    {
        private SelectCardPromptProperties properties;
        private List<BaseCard> selectedCards = new List<BaseCard>();
        
        public SelectCardPrompt(Game game, Player player, SelectCardPromptProperties props)
            : base(game, player)
        {
            properties = props;
        }
        
        public override bool Continue()
        {
            game.AddMessage("Waiting for {0} to select cards...", player);
            
            // Set selectable cards based on condition
            var selectableCards = new List<BaseCard>();
            
            // Find cards that match the condition
            foreach (var gamePlayer in game.GetPlayers())
            {
                var allCards = new List<BaseCard>();
                allCards.AddRange(gamePlayer.cardsInPlay);
                allCards.AddRange(gamePlayer.hand);
                allCards.AddRange(gamePlayer.provinceOne);
                allCards.AddRange(gamePlayer.provinceTwo);
                allCards.AddRange(gamePlayer.provinceThree);
                allCards.AddRange(gamePlayer.provinceFour);
                
                foreach (var card in allCards)
                {
                    if (properties.cardCondition?.Invoke(card) ?? true)
                    {
                        selectableCards.Add(card);
                    }
                }
            }
            
            player.SetSelectableCards(selectableCards);
            return false;
        }
        
        public override void OnCardClicked(Player clickingPlayer, BaseCard card)
        {
            if (clickingPlayer != player) return;
            
            if (properties.cardCondition?.Invoke(card) ?? true)
            {
                selectedCards.Add(card);
                
                if (selectedCards.Count >= properties.numCards)
                {
                    // Selection complete
                    if (properties.onSelect != null)
                    {
                        bool success = true;
                        foreach (var selectedCard in selectedCards)
                        {
                            success = success && properties.onSelect(player, selectedCard);
                        }
                        
                        if (success)
                        {
                            player.ClearSelectableCards();
                            game.pipeline.Continue();
                        }
                    }
                }
            }
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (command == "pass" || command == "done")
            {
                if (properties.optional || selectedCards.Count > 0)
                {
                    player.ClearSelectableCards();
                    game.pipeline.Continue();
                }
            }
        }
        
        public override void OnRingClicked(Player player, Ring ring) { }
    }
    
    /// <summary>
    /// Ring selection prompt
    /// </summary>
    public class SelectRingPrompt : BasePrompt
    {
        private SelectRingPromptProperties properties;
        
        public SelectRingPrompt(Game game, Player player, SelectRingPromptProperties props)
            : base(game, player)
        {
            properties = props;
        }
        
        public override bool Continue()
        {
            game.AddMessage("Waiting for {0} to select a ring...", player);
            
            // Set selectable rings based on condition
            var selectableRings = new List<Ring>();
            foreach (var ring in game.GetRings())
            {
                if (properties.ringCondition?.Invoke(ring) ?? true)
                {
                    selectableRings.Add(ring);
                }
            }
            
            player.SetSelectableRings(selectableRings);
            return false;
        }
        
        public override void OnRingClicked(Player clickingPlayer, Ring ring)
        {
            if (clickingPlayer != player) return;
            
            if (properties.ringCondition?.Invoke(ring) ?? true)
            {
                if (properties.onSelect != null)
                {
                    bool result = properties.onSelect(player, ring);
                    if (result)
                    {
                        player.ClearSelectableRings();
                        game.pipeline.Continue();
                    }
                }
            }
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (command == "pass" && properties.optional)
            {
                player.ClearSelectableRings();
                game.pipeline.Continue();
            }
        }
        
        public override void OnCardClicked(Player player, BaseCard card) { }
    }
    
    /// <summary>
    /// Honor bid prompt for the draw phase
    /// </summary>
    public class HonorBidPrompt : BasePrompt
    {
        private string title;
        private Action<int> costHandler;
        private List<int> prohibitedBids;
        private Duel duel;
        private Dictionary<Player, int> bids = new Dictionary<Player, int>();
        
        public HonorBidPrompt(Game game, string activePromptTitle, Action<int> handler, List<int> prohibited, Duel duelContext = null)
            : base(game, null) // No specific player for honor bid
        {
            title = activePromptTitle;
            costHandler = handler;
            prohibitedBids = prohibited ?? new List<int>();
            duel = duelContext;
        }
        
        public override bool Continue()
        {
            game.AddMessage(title);
            
            // Prompt all players to bid honor
            foreach (var player in game.GetPlayers())
            {
                if (!bids.ContainsKey(player))
                {
                    // Wait for bid from this player
                    return false;
                }
            }
            
            // All bids received, resolve
            ResolveBids();
            return true;
        }
        
        private void ResolveBids()
        {
            // Reveal honor bids
            game.AddMessage("Honor bids revealed:");
            foreach (var kvp in bids)
            {
                kvp.Key.SetShowBid(kvp.Value);
            }
            
            // Execute cost handler if provided
            costHandler?.Invoke(0);
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            if (command == "bid" && int.TryParse(arg, out int bidAmount))
            {
                if (!prohibitedBids.Contains(bidAmount) && bidAmount >= 0 && bidAmount <= 5)
                {
                    bids[player] = bidAmount;
                    game.AddMessage("{0} has made their honor bid", player);
                }
            }
        }
        
        public override void OnCardClicked(Player player, BaseCard card) { }
        public override void OnRingClicked(Player player, Ring ring) { }
    }
    
    /// <summary>
    /// End round prompt for cleanup
    /// </summary>
    public class EndRoundPrompt : BasePrompt
    {
        public EndRoundPrompt(Game game, Player player) : base(game, player) { }
        
        public override bool Continue()
        {
            game.AddMessage("End of round - performing cleanup...");
            
            // Reset player states
            foreach (var player in game.GetPlayers())
            {
                player.passedDynasty = false;
                player.limitedPlayed = 0;
                player.conflictOpportunities.Reset();
            }
            
            // Reset rings
            foreach (var ring in game.GetRings())
            {
                ring.ResetRing();
            }
            
            game.roundNumber++;
            return true;
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public override void OnCardClicked(Player player, BaseCard card) { }
        public override void OnRingClicked(Player player, Ring ring) { }
    }
    
    /// <summary>
    /// Game won prompt for victory conditions
    /// </summary>
    public class GameWonPrompt : BasePrompt
    {
        private Player winner;
        
        public GameWonPrompt(Game game, Player winningPlayer) : base(game, winningPlayer)
        {
            winner = winningPlayer;
        }
        
        public override bool Continue()
        {
            game.AddMessage("🎉 {0} has won the game! 🎉", winner);
            
            // Stop all clocks
            game.StopClocks();
            
            // Set final game state
            game.winner = winner;
            game.finishedAt = DateTime.Now;
            
            return true;
        }
        
        public override void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public override void OnCardClicked(Player player, BaseCard card) { }
        public override void OnRingClicked(Player player, Ring ring) { }
    }
    
    /// <summary>
    /// Simple step for basic game actions
    /// </summary>
    public class SimpleStep : IGameStep
    {
        private Game game;
        private Func<bool> stepFunction;
        
        public SimpleStep(Game gameInstance, Func<bool> step)
        {
            game = gameInstance;
            stepFunction = step;
        }
        
        public bool Continue()
        {
            try
            {
                return stepFunction?.Invoke() ?? true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in SimpleStep: {e.Message}");
                return true; // Continue despite error
            }
        }
        
        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method) { }
        public void OnCardClicked(Player player, BaseCard card) { }
        public void OnRingClicked(Player player, Ring ring) { }
        public void Initialize() { }
        public void Cleanup() { }
    }
    
    /// <summary>
    /// Game step interface
    /// </summary>
    public interface IGameStep
    {
        bool Continue();
        void OnMenuCommand(Player player, string command, string arg, string uuid, string method);
        void OnCardClicked(Player player, BaseCard card);
        void OnRingClicked(Player player, Ring ring);
        void Initialize();
        void Cleanup();
    }
    
    /// <summary>
    /// Duel system (placeholder)
    /// </summary>
    public class Duel
    {
        public Player challenger;
        public Player challenged;
        public BaseCard challengerCard;
        public BaseCard challengedCard;
        public string skill;
        public Player winner;
        
        public Duel(Player challengingPlayer, Player challengedPlayer, string skillType)
        {
            challenger = challengingPlayer;
            challenged = challengedPlayer;
            skill = skillType;
        }
        
        public void ResolveDuel()
        {
            // Placeholder duel resolution
            winner = challenger; // Simplified
        }
    }
}
