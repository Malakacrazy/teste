using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a triggered ability that responds to game events
    /// </summary>
    public partial class TriggeredAbility : CardAbility
    {
        [Header("Trigger Properties")]
        public string triggerEventName;
        public new string abilityType = AbilityTypes.Reaction;
        public bool isOptional = true;
        public int maxTriggers = -1; // -1 = unlimited
        
        // State tracking
        private int triggersUsed = 0;
        private bool isRegistered = false;

        public TriggeredAbility(BaseAbilityProperties properties) : base(null, null, new CardAbilityProperties { title = properties.title, condition = properties.condition, handler = properties.handler })
        {
        }

        public TriggeredAbility(Game game, BaseCard card, TriggeredAbilityProperties properties) : base(game, card, ConvertToCardAbilityProperties("reaction", properties))
        {
        }

        /// <summary>
        /// Check if this ability is currently active
        /// </summary>
        public virtual bool IsActive(AbilityContext context)
        {
            return true; // Placeholder - override in derived classes
        }

        /// <summary>
        /// Check if this ability can trigger for the given event
        /// </summary>
        public virtual bool CanTrigger(object eventObj, AbilityContext context)
        {
            if (!isOptional && !IsActive(context))
                return false;

            if (maxTriggers >= 0 && triggersUsed >= maxTriggers)
                return false;

            return CheckTriggerCondition(eventObj, context);
        }

        /// <summary>
        /// Check the specific trigger condition for this ability
        /// </summary>
        protected virtual bool CheckTriggerCondition(object eventObj, AbilityContext context)
        {
            // Override in derived classes for specific trigger logic
            return true;
        }

        /// <summary>
        /// Execute the triggered ability
        /// </summary>
        public virtual void Execute(AbilityContext context)
        {
            if (!CanTrigger(context.eventObj, context))
                return;

            base.Execute(context);
            triggersUsed++;
        }

        /// <summary>
        /// Register this ability with the ability window system
        /// </summary>
        public void Register(AbilityWindow abilityWindow)
        {
            if (!isRegistered && !string.IsNullOrEmpty(triggerEventName))
            {
                abilityWindow.RegisterAbility(triggerEventName, abilityType, source as BaseCard, this, (context) => CanTrigger(context.eventObj, context));
                isRegistered = true;
            }
        }

        /// <summary>
        /// Unregister this ability from the ability window system
        /// </summary>
        public void Unregister(AbilityWindow abilityWindow)
        {
            if (isRegistered && !string.IsNullOrEmpty(triggerEventName))
            {
                abilityWindow.UnregisterAbility(triggerEventName, source as BaseCard, this);
                isRegistered = false;
            }
        }

        /// <summary>
        /// Reset trigger usage count
        /// </summary>
        public void ResetTriggerCount()
        {
            triggersUsed = 0;
        }

        /// <summary>
        /// Get remaining triggers
        /// </summary>
        public int GetRemainingTriggers()
        {
            if (maxTriggers < 0) return int.MaxValue;
            return Mathf.Max(0, maxTriggers - triggersUsed);
        }

        /// <summary>
        /// Check if ability has triggers remaining
        /// </summary>
        public bool HasTriggersRemaining()
        {
            return maxTriggers < 0 || triggersUsed < maxTriggers;
        }
    }

    /// <summary>
    /// Specific type of triggered ability that responds to card events
    /// </summary>
    public class CardTriggeredAbility : TriggeredAbility
    {
        public Func<BaseCard, bool> cardCondition;

        public CardTriggeredAbility(BaseAbilityProperties properties) : base(properties)
        {
        }

        protected override bool CheckTriggerCondition(object eventObj, AbilityContext context)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                if (gameEvent.Parameters.ContainsKey("card"))
                {
                    var card = gameEvent.Parameters["card"] as BaseCard;
                    return cardCondition?.Invoke(card) ?? true;
                }
            }
            return base.CheckTriggerCondition(eventObj, context);
        }
    }

    /// <summary>
    /// Triggered ability that responds to conflict events
    /// </summary>
    public class ConflictTriggeredAbility : TriggeredAbility
    {
        public Func<Conflict, bool> conflictCondition;

        public ConflictTriggeredAbility(BaseAbilityProperties properties) : base(properties)
        {
        }

        protected override bool CheckTriggerCondition(object eventObj, AbilityContext context)
        {
            if (eventObj is IGameEvent gameEvent)
            {
                if (gameEvent.Parameters.ContainsKey("conflict"))
                {
                    var conflict = gameEvent.Parameters["conflict"] as Conflict;
                    return conflictCondition?.Invoke(conflict) ?? true;
                }
            }
            return base.CheckTriggerCondition(eventObj, context);
        }
    }
}
