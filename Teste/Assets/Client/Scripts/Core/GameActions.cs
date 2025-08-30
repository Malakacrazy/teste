using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for actions that target players
    /// </summary>
    public abstract class PlayerAction : GameAction
    {
        public Player target;
        
        protected PlayerAction() : base()
        {
        }
        
        public PlayerAction(Player targetPlayer) : base()
        {
            target = targetPlayer;
        }
        
        protected PlayerAction(PlayerActionProperties properties) : base(ConvertPlayerProperties(properties))
        {
            if (properties?.PlayerTarget != null)
            {
                target = properties.PlayerTarget;
            }
        }
        
        protected PlayerAction(Func<AbilityContext, PlayerActionProperties> factory) : base((context) => ConvertPlayerProperties(factory(context)))
        {
            // Target will be set when the factory is called during execution
        }
        
        private static GameAction.GameActionProperties ConvertPlayerProperties(PlayerActionProperties properties)
        {
            if (properties == null) return null;
            return new GameAction.GameActionProperties(properties.Target, properties.CannotBeCancelled, properties.Optional);
        }
    }

    /// <summary>
    /// Base class for actions that target rings
    /// </summary>
    public abstract class RingAction : GameAction
    {
        public Ring target;
        
        protected RingAction() : base()
        {
        }
        
        public RingAction(Ring targetRing) : base()
        {
            target = targetRing;
        }
        
        protected RingAction(GameAction.GameActionProperties properties) : base(properties)
        {
            // Target ring will be extracted from properties.target
            if (properties?.target?.Count > 0 && properties.target[0] is Ring ring)
            {
                target = ring;
            }
        }
        
        protected RingAction(Func<AbilityContext, GameAction.GameActionProperties> factory) : base(factory)
        {
            // Target will be set when the factory is called during execution
        }
    }

    /// <summary>
    /// Base class for actions that work with tokens
    /// </summary>
    public abstract class TokenAction : GameAction
    {
        public BaseCard target;
        public string tokenType;
        public int amount;
        
        protected TokenAction() : base()
        {
        }
        
        public TokenAction(BaseCard targetCard, string token, int tokenAmount) : base()
        {
            target = targetCard;
            tokenType = token;
            amount = tokenAmount;
        }
        
        protected TokenAction(L5RGame.GameActionProperties properties) : base(ConvertTokenProperties(properties))
        {
            // Extract token parameters from properties if available
            if (properties?.Target?.Count > 0 && properties.Target[0] is BaseCard card)
            {
                target = card;
            }
        }
        
        protected TokenAction(Func<AbilityContext, L5RGame.GameActionProperties> factory) : base((context) => ConvertTokenProperties(factory(context)))
        {
            // Properties will be set when the factory is called during execution
        }
        
        private static GameAction.GameActionProperties ConvertTokenProperties(L5RGame.GameActionProperties properties)
        {
            if (properties == null) return null;
            return new GameAction.GameActionProperties(properties.Target, properties.CannotBeCancelled, properties.Optional);
        }
    }
    public class GameActions : MonoBehaviour
    {
        public void Initialize(Game game) { }
        
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
            return new DuelAction(properties);
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
        /// Creates a gain honor action
        /// </summary>
        public object GainHonor(Player player, int amount)
        {
            return new GainHonorAction(player, amount);
        }
        
        /// <summary>
        /// Creates a gain fate action
        /// </summary>
        public object GainFate(Player player, int amount)
        {
            return new GainFateAction(player, amount);
        }
    }
    
    public partial class GameAction
    {
        public void AddEventsToArray(List<GameEvent> events, AbilityContext context) { }
        public void ResolveWithPlayer(Player player, object context) { } // Renamed to avoid ambiguity
    }
    
    /// <summary>
    /// Duel action for character duels
    /// </summary>
    public partial class DuelAction : CardGameAction
    {
        public Dictionary<string, object> properties;
        
        public DuelAction(Dictionary<string, object> duelProperties)
        {
            properties = duelProperties;
            actionType = "duel";
        }
        
        public override bool CanExecute(AbilityContext context)
        {
            return properties.ContainsKey("challenger");
        }
        
        public override void Execute(AbilityContext context)
        {
            // Placeholder duel implementation
            Debug.Log("Executing duel action");
        }
    }
    
    /// <summary>
    /// Move card action
    /// </summary>
    public partial class MoveCardAction : CardGameAction
    {
        public BaseCard card;
        public string targetLocation;
        
        public MoveCardAction(BaseCard targetCard, string location)
        {
            card = targetCard;
            targetLocation = location;
            actionType = "moveCard";
        }
        
        public override void Execute(AbilityContext context)
        {
            if (card != null)
            {
                context.player.MoveCard(card, targetLocation);
            }
        }
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
    /// Gain honor action
    /// </summary>
    public partial class GainHonorAction : PlayerAction
    {
        public Player targetPlayer;
        public int amount;
        
        public GainHonorAction(Player player, int honorAmount) : base(player)
        {
            targetPlayer = player;
            amount = honorAmount;
            actionType = "gainHonor";
        }
        
        public override void Execute(AbilityContext context)
        {
            if (targetPlayer != null)
            {
                targetPlayer.honor += amount;
            }
        }
    }
    
    /// <summary>
    /// Gain fate action
    /// </summary>
    public partial class GainFateAction : PlayerAction
    {
        public Player targetPlayer;
        public int amount;
        
        public GainFateAction(Player player, int fateAmount) : base(player)
        {
            targetPlayer = player;
            amount = fateAmount;
            actionType = "gainFate";
        }
        
        public override void Execute(AbilityContext context)
        {
            if (targetPlayer != null)
            {
                targetPlayer.fate += amount;
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

    /// <summary>
    /// Action to reveal cards
    /// </summary>
    public partial class RevealAction : CardGameAction
    {
        public List<BaseCard> cards;

        public override void Execute(AbilityContext context)
        {
            foreach (var card in cards ?? new List<BaseCard>())
            {
                card.facedown = false;
            }
        }
    }

    /// <summary>
    /// Action to bow cards
    /// </summary>
    public partial class BowAction : CardGameAction
    {
        public List<BaseCard> cards;

        public override void Execute(AbilityContext context)
        {
            foreach (var card in cards ?? new List<BaseCard>())
            {
                card.bowed = true;
                card.ready = false;
            }
        }
    }

    /// <summary>
    /// Action for player to lose honor
    /// </summary>
    public partial class LoseHonorAction : PlayerAction
    {
        public Player player;
        public int amount;

        public LoseHonorAction(Player targetPlayer, int honorAmount) : base(targetPlayer)
        {
            player = targetPlayer;
            amount = honorAmount;
            actionType = "loseHonor";
        }

        public override void Execute(AbilityContext context)
        {
            if (player != null)
            {
                player.honor -= amount;
            }
        }
    }

    /// <summary>
    /// Action to resolve conflict ring effects
    /// </summary>
    public partial class ResolveConflictRingAction : RingAction
    {
        public Ring ring;
        public Player player;

        public ResolveConflictRingAction(Ring conflictRing, Player resolvingPlayer) : base(conflictRing)
        {
            ring = conflictRing;
            player = resolvingPlayer;
            actionType = "resolveRing";
        }

        public override void Execute(AbilityContext context)
        {
            if (ring != null && player != null)
            {
                // Placeholder for ring effect resolution
                Debug.Log($"Resolving {ring.element} ring for {player.name}");
            }
        }
    }
}