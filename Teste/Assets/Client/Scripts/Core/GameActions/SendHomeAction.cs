using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Sends characters home from the current conflict
    /// </summary>
    [System.Serializable]
    public partial class SendHomeAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to sending home
        /// </summary>
        [System.Serializable]
        public class SendHomeProperties : CardActionProperties
        {
            public SendHomeProperties() : base() { }
        }
        
        #region Constructors
        
        public SendHomeAction() : base()
        {
            Initialize();
        }
        
        public SendHomeAction(SendHomeProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public SendHomeAction(System.Func<AbilityContext, SendHomeProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "sendHome";
            eventName = EventNames.OnSendHome;
            effectMessage = "send {0} home";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new SendHomeProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is SendHomeProperties sendProps)
                return sendProps;
                
            // Convert base properties to SendHomeProperties
            return new SendHomeProperties()
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
                
            // Card must be participating in the current conflict
            if (!card.IsParticipating())
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            var context = gameEvent.context;
            
            if (card != null && context?.game?.currentConflict != null)
            {
                context.game.currentConflict.RemoveFromConflict(card);
                LogExecution("Sent {0} home from conflict", card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Send specific character home
        /// </summary>
        public static SendHomeAction Character(BaseCard character)
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context => character);
            return action;
        }
        
        /// <summary>
        /// Send source character home
        /// </summary>
        public static SendHomeAction Self()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context => context.source);
            return action;
        }
        
        /// <summary>
        /// Send all attacking characters home
        /// </summary>
        public static SendHomeAction AllAttackers()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                return conflict?.attackers?.ToList() ?? new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Send all defending characters home
        /// </summary>
        public static SendHomeAction AllDefenders()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                return conflict?.defenders?.ToList() ?? new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Send all participating characters home
        /// </summary>
        public static SendHomeAction AllParticipants()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                return conflict?.participants?.ToList() ?? new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Send lowest cost participating character home
        /// </summary>
        public static SendHomeAction LowestCostParticipant()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                var participants = conflict?.participants?.Where(c => c.IsParticipating())
                    .OrderBy(c => c.cost).ToList();
                    
                return participants?.Any() == true ? new List<object> { participants.First() } : new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Send highest cost participating character home
        /// </summary>
        public static SendHomeAction HighestCostParticipant()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                var participants = conflict?.participants?.Where(c => c.IsParticipating())
                    .OrderByDescending(c => c.cost).ToList();
                    
                return participants?.Any() == true ? new List<object> { participants.First() } : new List<object>();
            });
            return action;
        }
        
        /// <summary>
        /// Send opposing characters home (relative to acting player)
        /// </summary>
        public static SendHomeAction OpposingCharacters()
        {
            var action = new SendHomeAction();
            action.SetDefaultTarget(context =>
            {
                var conflict = context.game.currentConflict;
                var opponents = context.player.opponent;
                
                return conflict?.participants?.Where(c => c.controller == opponents && c.IsParticipating())
                    .ToList() ?? new List<object>();
            });
            return action;
        }
        
        #endregion
    }
}