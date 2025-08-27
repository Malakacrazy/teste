using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base properties for all abilities
    /// </summary>
    [System.Serializable]
    public class BaseAbilityProperties
    {
        public Func<AbilityContext, bool> condition;
        public System.Action<AbilityContext> handler;
        public List<object> cost = new List<object>();
        public object target;
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        public string abilityType;
        public bool optional = false;
        public AbilityLimit limit;
    }

    /// <summary>
    /// Card action ability
    /// </summary>
    public class CardAction : CardAbility
    {
        public CardAction(Game game, BaseCard card, ActionProperties properties) 
            : base(game, card, ConvertToCardAbilityProperties(properties))
        {
        }

        private static CardAbilityProperties ConvertToCardAbilityProperties(ActionProperties properties)
        {
            return new CardAbilityProperties
            {
                title = properties.title,
                condition = properties.condition,
                handler = context => properties.effect?.Invoke(context),
                cost = properties.cost?.Cast<object>().ToList() ?? new List<object>(),
                abilityType = AbilityTypes.Action,
                limit = properties.limit
            };
        }

        public override bool IsTriggeredAbility()
        {
            return false;
        }

        public override bool IsCardAbility()
        {
            return true;
        }
    }

    /// <summary>
    /// Triggered ability (reactions, interrupts, etc.)
    /// </summary>
    public class TriggeredAbility : CardAbility
    {
        public List<string> when = new List<string>();
        public List<string> location = new List<string>();
        public bool eventType;

        public TriggeredAbility(Game game, BaseCard card, string abilityType, TriggeredAbilityProperties properties)
            : base(game, card, ConvertToCardAbilityProperties(abilityType, properties))
        {
            when = new List<string> { properties.when ?? "" };
            location = properties.location ?? new List<string>();
        }

        private static CardAbilityProperties ConvertToCardAbilityProperties(string abilityType, TriggeredAbilityProperties properties)
        {
            return new CardAbilityProperties
            {
                title = properties.title,
                condition = properties.condition != null ? context => properties.condition(null, context) : null,
                handler = properties.effect != null ? context => properties.effect(null, context) : null,
                cost = properties.cost?.Cast<object>().ToList() ?? new List<object>(),
                abilityType = abilityType,
                limit = properties.limit
            };
        }

        public void RegisterEvents()
        {
            // Register for game events based on 'when' conditions
            foreach (var eventName in when)
            {
                game.RegisterEventHandler(eventName, this);
            }
        }

        public void UnregisterEvents()
        {
            // Unregister from game events
            foreach (var eventName in when)
            {
                game.UnregisterEventHandler(eventName, this);
            }
        }
    }

    /// <summary>
    /// Custom play action for cards
    /// </summary>
    public class CustomPlayAction
    {
        public string title;
        public Func<Player, BaseCard, bool> condition;
        public Action<Player, BaseCard, AbilityContext> effect;
        public List<ICost> cost = new List<ICost>();

        public CustomPlayAction(CustomPlayActionProperties properties)
        {
            title = properties.title;
            condition = properties.condition;
            effect = properties.effect;
            cost = properties.cost ?? new List<ICost>();
        }

        public virtual bool CanExecute(Player player, BaseCard card)
        {
            return condition == null || condition(player, card);
        }

        public virtual void Execute(Player player, BaseCard card, AbilityContext context)
        {
            effect?.Invoke(player, card, context);
        }
    }

    /// <summary>
    /// Placeholder action classes for different play types
    /// </summary>
    public class PlayDisguisedCharacterAction : CustomPlayAction
    {
        public PlayDisguisedCharacterAction(BaseCard card) : base(new CustomPlayActionProperties
        {
            title = "Play as Disguised",
            condition = (player, cardContext) => true,
            effect = (player, cardContext, context) => { /* Implementation */ }
        }) { }
    }

    public class DynastyCardAction : CustomPlayAction
    {
        public DynastyCardAction(BaseCard card) : base(new CustomPlayActionProperties
        {
            title = "Play Dynasty Card",
            condition = (player, cardContext) => true,
            effect = (player, cardContext, context) => { /* Implementation */ }
        }) { }
    }

    public class PlayCharacterAction : CustomPlayAction
    {
        public PlayCharacterAction(BaseCard card) : base(new CustomPlayActionProperties
        {
            title = "Play Character",
            condition = (player, cardContext) => true,
            effect = (player, cardContext, context) => { /* Implementation */ }
        }) { }
    }

    public class PlayAttachmentOnRingAction : CustomPlayAction
    {
        public PlayAttachmentOnRingAction(BaseCard card) : base(new CustomPlayActionProperties
        {
            title = "Attach to Ring",
            condition = (player, cardContext) => true,
            effect = (player, cardContext, context) => { /* Implementation */ }
        }) { }
    }

    public class PlayAttachmentAction : CustomPlayAction
    {
        public PlayAttachmentAction(BaseCard card) : base(new CustomPlayActionProperties
        {
            title = "Play Attachment",
            condition = (player, cardContext) => true,
            effect = (player, cardContext, context) => { /* Implementation */ }
        }) { }
    }
}
