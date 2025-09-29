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

        private static GameAction.GameActionProperties ConvertProperties(CardActionProperties properties)
        {
            if (properties == null) return null;
            var result = new GameAction.GameActionProperties();
            result.target = properties.Target;
            result.cannotBeCancelled = properties.CannotBeCancelled;
            result.optional = properties.Optional;
            return result;
        }

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
    /// Enumerations for different game action types (moved to Constants.cs to avoid duplicate definition)
    /// </summary>
    // GameActionTypes class moved to Constants.cs to avoid CS0101 duplicate definition errors
}