using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface IMoveToConflictProperties : ICardActionProperties
    {
    }

    public class MoveToConflictProperties : CardActionProperties, IMoveToConflictProperties
    {
    }

    public partial class MoveToConflictAction : CardGameAction
    {
        #region Constructors
        
        public MoveToConflictAction() : base()
        {
            Initialize();
        }
        
        public MoveToConflictAction(CardActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public MoveToConflictAction(System.Func<AbilityContext, CardActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "moveToConflict";
            eventName = EventNames.OnMoveToConflict;
            effectMessage = "move {0} into the conflict";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion

        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var card = target as BaseCard;
            if (card == null) return false;
            
            if (!base.CanAffect(target, context, additionalProperties))
            {
                return false;
            }
            
            if (context.Game.CurrentConflict == null || card.IsParticipating())
            {
                return false;
            }
            
            if (card.Controller.IsAttackingPlayer())
            {
                if (!card.CanParticipateAsAttacker())
                {
                    return false;
                }
            }
            else if (!card.CanParticipateAsDefender())
            {
                return false;
            }
            
            return card.Location == Locations.PlayArea;
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            if (card != null)
            {
                if (card.Controller.IsAttackingPlayer())
                {
                    gameEvent.context.Game.CurrentConflict.AddAttacker(card);
                    LogExecution("Moved {0} into the conflict as attacker", card.name);
                }
                else
                {
                    gameEvent.context.Game.CurrentConflict.AddDefender(card);
                    LogExecution("Moved {0} into the conflict as defender", card.name);
                }
                return true;
            }
            return false;
        }
    }
}
