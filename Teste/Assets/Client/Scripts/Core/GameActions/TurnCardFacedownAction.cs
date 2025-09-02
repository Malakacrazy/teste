using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public interface ITurnCardFacedownProperties : ICardActionProperties
    {
    }

    public class TurnCardFacedownProperties : CardActionProperties, ITurnCardFacedownProperties
    {
    }

    public partial class TurnCardFacedownAction : CardGameAction
    {
        #region Constructors
        
        public TurnCardFacedownAction() : base()
        {
            Initialize();
        }
        
        public TurnCardFacedownAction(CardActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public TurnCardFacedownAction(System.Func<AbilityContext, CardActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "turnFacedown";
            eventName = EventNames.OnCardTurnedFacedown;
            costMessage = "turning {0} facedown";
            effectMessage = "turn {0} facedown";
            targetTypes = new List<string> { CardTypes.Character, CardTypes.Holding, CardTypes.Province };
        }
        
        #endregion

        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var card = target as BaseCard;
            if (card == null) return false;
            
            return !card.Facedown && base.CanAffect(target, context, additionalProperties) && card.IsInProvince();
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as BaseCard;
            if (card != null)
            {
                card.LeavesPlay();
                
                if (card.IsConflictProvince())
                {
                    gameEvent.context.Game.AddMessage("{0} is immediately revealed again!", card);
                    card.inConflict = true;
                    
                    var revealEvent = gameEvent.context.Game.Actions.Reveal(new List<BaseCard> { card })
                        .GetEvent(card, gameEvent.context.Game.GetFrameworkContext());
                    gameEvent.context.Game.OpenThenEventWindow(new List<GameEvent> { revealEvent });
                    LogExecution("Turned {0} facedown and immediately revealed again due to conflict", card.name);
                }
                else
                {
                    card.Facedown = true;
                    LogExecution("Turned {0} facedown", card.name);
                }
                return true;
            }
            return false;
        }
    }
}
