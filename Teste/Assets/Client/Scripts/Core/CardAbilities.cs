using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for all card actions
    /// </summary>
    [System.Serializable]
    public class CardAction
    {
        public Game game;
        public BaseCard source;
        public ActionProperties properties;
        public AbilityLimit limit;
        
        public CardAction(Game gameInstance, BaseCard sourceCard, ActionProperties actionProperties)
        {
            game = gameInstance;
            source = sourceCard;
            properties = actionProperties;
        }
        
        public virtual bool CanExecute(Player player, AbilityContext context = null)
        {
            return true; // Placeholder
        }
        
        public virtual void Execute(Player player, AbilityContext context = null)
        {
            // Placeholder - implement specific action logic
        }
        
        public virtual List<object> GetActions(BaseCard card)
        {
            return new List<object> { this };
        }
    }
    
    /// <summary>
    /// Base class for triggered abilities (reactions, interrupts, etc.)
    /// </summary>
    [System.Serializable]
    public class TriggeredAbility
    {
        public Game game;
        public BaseCard source;
        public string abilityType;
        public TriggeredAbilityProperties properties;
        public AbilityLimit limit;
        public List<string> location = new List<string>();
        
        public TriggeredAbility(Game gameInstance, BaseCard sourceCard, string type, TriggeredAbilityProperties abilityProperties)
        {
            game = gameInstance;
            source = sourceCard;
            abilityType = type;
            properties = abilityProperties;
            location = abilityProperties.location ?? new List<string> { Locations.PlayArea };
        }
        
        public virtual bool CanTrigger(object eventObj, AbilityContext context)
        {
            return true; // Placeholder
        }
        
        public virtual void Trigger(Player player, object eventObj, AbilityContext context)
        {
            // Placeholder - implement triggering logic
        }
        
        public virtual void RegisterEvents()
        {
            // Register this ability to listen for relevant events
        }
        
        public virtual void UnregisterEvents()
        {
            // Unregister event listeners
        }
        
        public virtual List<object> GetReactions(BaseCard card)
        {
            return new List<object> { this };
        }
    }
    
    /// <summary>
    /// Represents a custom play action for cards
    /// </summary>
    [System.Serializable]
    public class CustomPlayAction
    {
        public CustomPlayActionProperties properties;
        
        public CustomPlayAction(CustomPlayActionProperties actionProperties)
        {
            properties = actionProperties;
        }
        
        public virtual bool CanPlay(Player player, BaseCard card)
        {
            return true; // Placeholder
        }
        
        public virtual void Play(Player player, BaseCard card)
        {
            // Placeholder - implement play logic
        }
    }
    
    /// <summary>
    /// Base class for card abilities used in ability system
    /// </summary>
    [System.Serializable]
    public class CardAbility : BaseAbility
    {
        public BaseCard card;
        
        public CardAbility() : base() { }
        
        public CardAbility(BaseCard sourceCard)
        {
            card = sourceCard;
        }
        
        public override bool IsCardAbility()
        {
            return true;
        }
        
        public override bool IsCardPlayed()
        {
            return false; // Override in specific ability types
        }
        
        public override bool IsTriggeredAbility()
        {
            return false; // Override in specific ability types
        }
    }
    
    /// <summary>
    /// Standard play actions for different card types
    /// </summary>
    public class PlayCharacterAction : CustomPlayAction
    {
        public BaseCard character;
        
        public PlayCharacterAction(BaseCard card) : base(new CustomPlayActionProperties())
        {
            character = card;
        }
        
        public override bool CanPlay(Player player, BaseCard card)
        {
            return player.fate >= card.GetCost();
        }
        
        public override void Play(Player player, BaseCard card)
        {
            player.ModifyFate(-card.GetCost());
            player.MoveCard(card, Locations.PlayArea);
        }
    }
    
    public class DynastyCardAction : CustomPlayAction
    {
        public BaseCard card;
        
        public DynastyCardAction(BaseCard dynastyCard) : base(new CustomPlayActionProperties())
        {
            card = dynastyCard;
        }
    }
    
    public class PlayDisguisedCharacterAction : CustomPlayAction
    {
        public BaseCard character;
        
        public PlayDisguisedCharacterAction(BaseCard card) : base(new CustomPlayActionProperties())
        {
            character = card;
        }
    }
    
    public class PlayAttachmentAction : CustomPlayAction
    {
        public BaseCard attachment;
        
        public PlayAttachmentAction(BaseCard card) : base(new CustomPlayActionProperties())
        {
            attachment = card;
        }
    }
    
    public class PlayAttachmentOnRingAction : CustomPlayAction
    {
        public BaseCard attachment;
        
        public PlayAttachmentOnRingAction(BaseCard card) : base(new CustomPlayActionProperties())
        {
            attachment = card;
        }
    }
}
