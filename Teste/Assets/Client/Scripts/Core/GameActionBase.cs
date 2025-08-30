using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    #region Interfaces
    
    /// <summary>
    /// Base interface for all game action properties
    /// </summary>
    public interface IGameActionProperties
    {
        List<object> Target { get; set; }
        bool CannotBeCancelled { get; set; }
        bool Optional { get; set; }
        GameAction ParentAction { get; set; }
    }

    /// <summary>
    /// Interface for card-specific action properties
    /// </summary>
    public interface ICardActionProperties : IGameActionProperties
    {
        BaseCard CardTarget { get; set; }
    }

    /// <summary>
    /// Interface for player-specific action properties
    /// </summary>
    public interface IPlayerActionProperties : IGameActionProperties
    {
        Player PlayerTarget { get; set; }
    }

    #endregion

    #region Base Properties Classes

    /// <summary>
    /// Base properties for all game actions
    /// </summary>
    [Serializable]
    public class GameActionProperties : IGameActionProperties
    {
        public List<object> Target { get; set; } = new List<object>();
        public bool CannotBeCancelled { get; set; }
        public bool Optional { get; set; }
        public GameAction ParentAction { get; set; }

        public GameActionProperties()
        {
            Target = new List<object>();
        }

        public GameActionProperties(List<object> targets, bool cannotBeCancelled = false, bool optional = false)
        {
            Target = targets ?? new List<object>();
            CannotBeCancelled = cannotBeCancelled;
            Optional = optional;
        }
    }

    /// <summary>
    /// Properties for card-targeting actions
    /// </summary>
    [Serializable]
    public class CardActionProperties : GameActionProperties, ICardActionProperties
    {
        public BaseCard CardTarget { get; set; }

        public CardActionProperties() : base() { }

        public CardActionProperties(BaseCard target) : base()
        {
            CardTarget = target;
            if (target != null)
                Target.Add(target);
        }
    }

    /// <summary>
    /// Properties for player-targeting actions
    /// </summary>
    [Serializable]
    public class PlayerActionProperties : GameActionProperties, IPlayerActionProperties
    {
        public Player PlayerTarget { get; set; }

        public PlayerActionProperties() : base() { }

        public PlayerActionProperties(Player target) : base()
        {
            PlayerTarget = target;
            if (target != null)
                Target.Add(target);
        }
    }

    #endregion

    /// <summary>
    /// Base class for card-targeting game actions
    /// </summary>
    public abstract partial class CardGameAction : GameAction
    {
        public virtual string[] TargetType => new string[] { "card" };

        protected CardGameAction() : base() { }
        protected CardGameAction(CardActionProperties properties) : base(ConvertProperties(properties)) { }
        
        private static GameAction.GameActionProperties ConvertProperties(CardActionProperties properties)
        {
            if (properties == null) return null;
            return new GameAction.GameActionProperties(properties.Target, properties.CannotBeCancelled, properties.Optional);
        }
        protected CardGameAction(Func<AbilityContext, CardActionProperties> factory) : base((context) => ConvertProperties(factory(context))) { }

        public virtual bool CanAffect(object target, AbilityContext context, object additionalProperties = null)
        {
            return target is BaseCard card && CanAffect(card, context, additionalProperties);
        }

        public virtual bool CanAffect(BaseCard card, AbilityContext context, object additionalProperties = null)
        {
            return card != null && base.CanAffect(card, context, additionalProperties as GameAction.GameActionProperties);
        }
    }

    /// <summary>
    /// Enumerations for different game action types
    /// </summary>
    public static class GameActionTypes
    {
        public const string MoveTo = "moveTo";
        public const string Discard = "discard";
        public const string Bow = "bow";
        public const string Ready = "ready";
        public const string GainHonor = "gainHonor";
        public const string LoseHonor = "loseHonor";
        public const string GainFate = "gainFate";
        public const string SpendFate = "spendFate";
        public const string PlayCard = "playCard";
        public const string PutIntoPlay = "putIntoPlay";
        public const string RemoveFromGame = "removeFromGame";
        public const string Reveal = "reveal";
        public const string LookAt = "lookAt";
        public const string Shuffle = "shuffle";
        public const string Search = "search";
        public const string TakeControl = "takeControl";
        public const string Attach = "attach";
        public const string Detach = "detach";
        public const string Honor = "honor";
        public const string Dishonor = "dishonor";
        public const string Break = "break";
        public const string SendHome = "sendHome";
        public const string FlipDynasty = "flipDynasty";
        public const string CreateToken = "createToken";
        public const string PlaceFate = "placeFate";
        public const string RemoveFate = "removeFate";
        public const string ModifyStats = "modifyStats";
        public const string ResolveAbility = "resolveAbility";
        public const string LastingEffect = "lastingEffect";
        public const string DelayedEffect = "delayedEffect";
        public const string CardMenuCommand = "cardMenuCommand";
        public const string SelectCard = "selectCard";
        public const string ChooseAction = "chooseAction";
        public const string Duel = "duel";
        public const string MoveToConflict = "moveToConflict";
        public const string ReturnToHand = "returnToHand";
        public const string ReturnToDeck = "returnToDeck";
        public const string TurnFacedown = "turnFacedown";
        public const string AttachToRing = "attachToRing";
    }
}