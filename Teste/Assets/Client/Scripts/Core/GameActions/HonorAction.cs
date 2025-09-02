using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Honors a character
    /// </summary>
    [System.Serializable]
    public partial class HonorAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to honoring
        /// </summary>
        [System.Serializable]
        public class HonorProperties : CardActionProperties
        {
            public HonorProperties() : base() { }
        }
        
        #region Constructors
        
        public HonorAction() : base()
        {
            Initialize();
        }
        
        public HonorAction(HonorProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public HonorAction(System.Func<AbilityContext, HonorProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "honor";
            eventName = EventNames.OnCardHonored;
            costMessage = "honoring {0}";
            effectMessage = "honor {0}";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new HonorProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is HonorProperties honorProps)
                return honorProps;
                
            // Convert base properties to HonorProperties
            return new HonorProperties()
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
                
            // Cannot honor already honored characters
            if (card.IsHonored())
                return false;
                
            // Check restrictions for receiving honor tokens
            // If not dishonored, check if can receive honor token
            if (!card.IsDishonored() && !card.CheckRestrictions("receiveHonorToken", context))
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
                card.Honor();
                LogExecution("Honored {0}", card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to honor specific character
        /// </summary>
        public static HonorAction Character(BaseCard character)
        {
            var action = new HonorAction();
            action.SetDefaultTarget(context => character);
            return action;
        }
        
        /// <summary>
        /// Create action to honor source character
        /// </summary>
        public static HonorAction Self()
        {
            var action = new HonorAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        /// <summary>
        /// Create action to honor a character participating in conflict
        /// </summary>
        public static HonorAction ParticipatingCharacter()
        {
            var action = new HonorAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                return conflict?.participants?.Where(c => c.type == CardTypes.Character && !c.IsHonored()).ToList() ?? new List<BaseCard>();
            });
            return action;
        }
        
        #endregion
    }
}