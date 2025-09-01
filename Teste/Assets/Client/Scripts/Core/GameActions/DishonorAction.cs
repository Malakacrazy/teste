using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Dishonors a character
    /// </summary>
    [System.Serializable]
    public partial class DishonorAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to dishonoring
        /// </summary>
        [System.Serializable]
        public class DishonorProperties : CardActionProperties
        {
            public DishonorProperties() : base() { }
        }
        
        #region Constructors
        
        public DishonorAction() : base()
        {
            Initialize();
        }
        
        public DishonorAction(DishonorProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public DishonorAction(System.Func<AbilityContext, DishonorProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "dishonor";
            eventName = EventNames.OnCardDishonored;
            costMessage = "dishonoring {0}";
            effectMessage = "dishonor {0}";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new DishonorProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is DishonorProperties dishonorProps)
                return dishonorProps;
                
            // Convert base properties to DishonorProperties
            return new DishonorProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction
            };
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            // Must be a character in play area
            if (card.location != Locations.PlayArea || card.type != CardTypes.Character)
                return false;
                
            // Cannot dishonor already dishonored characters
            if (card.IsDishonored())
                return false;
                
            // Check restrictions for receiving dishonor tokens
            // If not honored, check if can receive dishonor token
            if (!card.IsHonored() && !card.CheckRestrictions("receiveDishonorToken", context))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            
            if (card != null)
            {
                card.Dishonor();
                LogExecution("Dishonored {0}", card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to dishonor specific character
        /// </summary>
        public static DishonorAction Character(BaseCard character)
        {
            var action = new DishonorAction();
            action.SetDefaultTarget(context => character);
            return action;
        }
        
        /// <summary>
        /// Create action to dishonor opposing character
        /// </summary>
        public static DishonorAction OpposingCharacter()
        {
            var action = new DishonorAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                var opposingPlayer = context.player.opponent;
                return conflict?.GetParticipants()
                    .Where(c => c.controller == opposingPlayer && c.type == CardTypes.Character && !c.IsDishonored())
                    .ToList() ?? new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Create action to dishonor lowest cost character
        /// </summary>
        public static DishonorAction LowestCostCharacter(Player targetPlayer = null)
        {
            var action = new DishonorAction();
            action.SetDefaultTarget(context =>
            {
                var player = targetPlayer ?? context.player.opponent;
                var eligibleCharacters = player.cardsInPlay
                    .Where(c => c.type == CardTypes.Character && !c.IsDishonored())
                    .OrderBy(c => c.cost)
                    .ToList();
                    
                return eligibleCharacters.Any() ? new List<object> { eligibleCharacters.First() } : new List<object>();
            });
            return action;
        }
        
        #endregion
    }
}