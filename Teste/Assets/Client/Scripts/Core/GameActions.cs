using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GameActions : MonoBehaviour
    {
        public void Initialize(Game game) { }
        
        // Static factory methods for common actions
        public static DiscardFromPlayAction DiscardFromPlay() => new DiscardFromPlayAction();
        public static LastingEffectCardAction CardLastingEffect() => new LastingEffectCardAction();
        public static ReadyAction Ready() => new ReadyAction();
        public static HonorAction Honor() => new HonorAction();
        public static DishonorAction Dishonor() => new DishonorAction();
        public static PlaceFateAction PlaceFate() => new PlaceFateAction();
        public static RemoveFateAction RemoveFate() => new RemoveFateAction();
        public static BowAction Bow() => new BowAction();
        public static GainFateAction GainFate(Player player, int amount) => new GainFateAction(player, amount);
        public static GainHonorAction GainHonor(Player player, int amount) => new GainHonorAction(player, amount);
        
        // Missing methods for compilation
        public static GainHonorAction CreateGainHonorAction(Player player, int amount) => new GainHonorAction(player, amount);
        public static GameAction CreateTakeHonorAction(Player player, Player target, int amount) => new TakeHonorAction(player, target, amount);
        
        // Additional missing GameAction methods
        public static GameAction CreateReadyAction(BaseCard card)
        {
            var action = new ReadyAction();
            action.SetDefaultTarget(ctx => card);
            return action;
        }
        
        public static GameAction CreateBowAction(BaseCard card)
        {
            var action = new BowAction();
            action.SetDefaultTarget(ctx => card);
            return action;
        }
        
        public static GameAction CreateDrawCardsAction(Player player, int amount) => new DrawCardsAction(player, amount);
        
        public static GameAction CreateDiscardRandomAction(Player player, int amount)
        {
            var action = new RandomDiscardAction(new RandomDiscardAction.RandomDiscardProperties(amount));
            action.SetDefaultTarget(ctx => player);
            return action;
        }
        
        public static GameAction CreateDiscardAction(Player player, BaseCard card) => new DiscardAction(player, card);
        
        public static GameAction CreateHonorAction(BaseCard card)
        {
            var action = new HonorAction();
            action.SetDefaultTarget(ctx => card);
            return action;
        }
        
        public static GameAction CreateDishonorAction(BaseCard card)
        {
            var action = new DishonorAction();
            action.SetDefaultTarget(ctx => card);
            return action;
        }
        
        public static GameAction CreateRemoveFateAction(BaseCard card, int amount)
        {
            var action = new RemoveFateAction();
            action.SetDefaultTarget(ctx => card);
            return action;
        }
        
        public GameAction GetAction(string actionName, object value) => null; // Cannot instantiate abstract GameAction

        /// <summary>
        /// Take fate from ring
        /// </summary>
        public TakeFateFromRingAction TakeFateFromRing(Player player, Ring ring, int amount)
        {
            return new TakeFateFromRingAction { player = player, ring = ring, amount = amount };
        }

        /// <summary>
        /// Reveal cards
        /// </summary>
        public RevealAction Reveal(List<BaseCard> cards)
        {
            return new RevealAction { cards = cards };
        }

        /// <summary>
        /// Bow cards
        /// </summary>
        public BowAction Bow(List<BaseCard> cards)
        {
            return new BowAction { cards = cards };
        }

        /// <summary>
        /// Player loses honor
        /// </summary>
        public LoseHonorAction LoseHonor(Player player, int amount)
        {
            return new LoseHonorAction { player = player, amount = amount };
        }

        /// <summary>
        /// Resolve conflict ring
        /// </summary>
        public ResolveConflictRingAction ResolveConflictRing(Ring ring, Player player)
        {
            return new ResolveConflictRingAction { ring = ring, player = player };
        }
        
        /// <summary>
        /// Creates a duel action
        /// </summary>
        public object Duel(Dictionary<string, object> properties)
        {
            var duelProperties = new DuelAction.DuelProperties();
            
            if (properties.ContainsKey("type") && properties["type"] is string type)
                duelProperties.type = type;
            if (properties.ContainsKey("challenger") && properties["challenger"] is DrawCard challenger)
                duelProperties.challenger = challenger;
            if (properties.ContainsKey("gameAction") && properties["gameAction"] is GameAction gameAction)
                duelProperties.gameAction = gameAction;
            if (properties.ContainsKey("message") && properties["message"] is string message)
                duelProperties.message = message;
                
            return new DuelAction(duelProperties);
        }
        
        /// <summary>
        /// Creates a move card action
        /// </summary>
        public object MoveCard(BaseCard card, string location)
        {
            return new MoveCardAction(card, location);
        }
        
        /// <summary>
        /// Creates a draw cards action
        /// </summary>
        public object DrawCards(Player player, int amount)
        {
            return new DrawCardsAction(player, amount);
        }
        
        /// <summary>
        /// Creates a discard at random action
        /// </summary>
        public RandomDiscardAction DiscardAtRandom(Dictionary<string, object> properties)
        {
            var props = new RandomDiscardAction.RandomDiscardProperties();
            if (properties.ContainsKey("amount") && properties["amount"] is int amount)
            {
                props.amount = amount;
            }
            return new RandomDiscardAction(props);
        }
        
        /// <summary>
        /// Creates a move card action with dictionary properties
        /// </summary>
        public object MoveCard(Dictionary<string, object> properties)
        {
            BaseCard target = null;
            string destination = "";
            bool bottom = false;
            
            if (properties.ContainsKey("target") && properties["target"] is BaseCard card)
            {
                target = card;
            }
            
            if (properties.ContainsKey("destination") && properties["destination"] is string dest)
            {
                destination = dest;
            }
            
            if (properties.ContainsKey("bottom") && properties["bottom"] is bool isBottom)
            {
                bottom = isBottom;
            }
            
            return new MoveCardAction(target, destination, bottom);
        }
        
    }
    
    public partial class GameAction
    {
        public void AddEventsToArray(List<GameEvent> events, AbilityContext context) { }
        public void ResolveWithPlayer(Player player, object context) { } // Renamed to avoid ambiguity
    }
    
    
    
    /// <summary>
    /// Draw cards action
    /// </summary>
    public class DrawCardsAction : GameAction
    {
        public Player targetPlayer;
        public int amount;
        
        public DrawCardsAction(Player player, int cardCount)
        {
            targetPlayer = player;
            amount = cardCount;
            actionType = "drawCards";
        }
        
        public override void Execute(AbilityContext context)
        {
            if (targetPlayer != null)
            {
                targetPlayer.DrawCardsToHand(amount);
            }
        }
    }
    

    /// <summary>
    /// Action to take fate from ring
    /// </summary>
    public class TakeFateFromRingAction : GameAction
    {
        public Player player;
        public Ring ring;
        public int amount;

        public override void Execute(AbilityContext context)
        {
            if (player != null && ring != null && ring.fate >= amount)
            {
                ring.fate -= amount;
                player.fate += amount;
            }
        }
    }

}