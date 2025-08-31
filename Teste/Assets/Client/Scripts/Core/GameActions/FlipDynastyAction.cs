using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Flips dynasty cards face up
    /// </summary>
    [System.Serializable]
    public partial class FlipDynastyAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to flipping dynasty cards
        /// </summary>
        [System.Serializable]
        public class FlipDynastyProperties : CardActionProperties
        {
            public FlipDynastyProperties() : base() { }
        }
        
        #region Constructors
        
        public FlipDynastyAction() : base()
        {
            Initialize();
        }
        
        public FlipDynastyAction(FlipDynastyProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public FlipDynastyAction(System.Func<AbilityContext, FlipDynastyProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "reveal";
            eventName = EventNames.OnDynastyCardTurnedFaceup;
            effectMessage = "reveal the facedown card in {0}";
            targetTypes = new List<string> { CardTypes.Character, CardTypes.Holding };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new FlipDynastyProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is FlipDynastyProperties flipProps)
                return flipProps;
                
            // Convert base properties to FlipDynastyProperties
            return new FlipDynastyProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var firstCard = properties.target?.FirstOrDefault() as BaseCard;
            return ("reveal the facedown card in {0}", new object[] { firstCard?.location ?? "unknown location" });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            // Card must be in a province (but not stronghold province)
            if (!card.IsInProvince() || card.location == Locations.StrongholdProvince)
                return false;
                
            // Card must be dynasty and face down
            if (!card.isDynasty || !card.facedown)
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            
            if (card != null)
            {
                card.facedown = false;
                LogExecution("Flipped {0} face up in {1}", card.name, card.location);
            }
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Flip specific dynasty card
        /// </summary>
        public static FlipDynastyAction Card(BaseCard card)
        {
            var action = new FlipDynastyAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Flip dynasty card in specific province
        /// </summary>
        public static FlipDynastyAction InProvince(string provinceLocation, Player player = null)
        {
            var action = new FlipDynastyAction();
            action.SetDefaultTarget(context =>
            {
                var targetPlayer = player ?? context.player;
                var card = targetPlayer.GetDynastyCardInProvince(provinceLocation);
                return card != null ? new List<object> { card } : new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Flip all face down dynasty cards
        /// </summary>
        public static FlipDynastyAction AllFaceDown(Player player = null)
        {
            var action = new FlipDynastyAction();
            action.SetDefaultTarget(context =>
            {
                var targetPlayer = player ?? context.player;
                return targetPlayer.dynastyCards
                    .Where(c => c.facedown && c.IsInProvince() && c.location != Locations.StrongholdProvince)
                    .ToList();
            });
            return action;
        }
        
        #endregion
    }
}