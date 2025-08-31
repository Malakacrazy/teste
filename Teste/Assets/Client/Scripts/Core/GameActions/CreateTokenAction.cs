using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Creates a token copy of a character card during military conflicts
    /// </summary>
    [System.Serializable]
    public partial class CreateTokenAction : CardGameAction
    {
        /// <summary>
        /// Properties specific to creating tokens
        /// </summary>
        [System.Serializable]
        public class CreateTokenProperties : CardActionProperties
        {
            public CreateTokenProperties() : base() { }
        }
        
        #region Constructors
        
        public CreateTokenAction() : base()
        {
            Initialize();
        }
        
        public CreateTokenAction(CreateTokenProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public CreateTokenAction(System.Func<AbilityContext, CreateTokenProperties> factory) : base(context => factory(context))
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "createToken";
            eventName = EventNames.OnCreateToken;
            effectMessage = "create a token";
            targetTypes = new List<string> { CardTypes.Character, CardTypes.Holding };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new CreateTokenProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is CreateTokenProperties tokenProps)
                return tokenProps;
                
            // Convert base properties to CreateTokenProperties
            return new CreateTokenProperties()
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
                
            // Card must be face down and in a province (but not stronghold province)
            if (!card.facedown || !card.IsInProvince() || card.location == Locations.StrongholdProvince)
                return false;
                
            // Must be during a military conflict
            if (!context.game.IsDuringConflict(ConflictTypes.Military))
                return false;
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var context = gameEvent.context;
            var card = gameEvent.GetProperty("target") as BaseCard;
            
            if (card == null || context?.game == null)
                return;
            
            // Create token copy of the card
            var token = context.game.CreateToken(card);
            
            if (token == null)
            {
                LogExecution("Failed to create token for {0}", card.name);
                return;
            }
            
            // Remove original card from its pile
            card.owner.RemoveCardFromPile(card);
            
            // Check for province refill
            CheckForRefillProvince(card, gameEvent, additionalProperties);
            
            // Move original card to removed from game
            card.MoveTo(Locations.RemovedFromGame);
            
            // Put token into play
            card.owner.MoveCard(token, Locations.PlayArea);
            
            // Add token to current conflict
            var currentConflict = context.game.currentConflict;
            if (currentConflict != null)
            {
                if (context.player.IsAttackingPlayer())
                {
                    currentConflict.AddAttacker(token);
                }
                else
                {
                    currentConflict.AddDefender(token);
                }
            }
            
            // Add delayed effect to discard token when conflict ends
            var delayedEffect = EffectEngine.CreateDelayedEffect(
                trigger: new ConflictFinishedTrigger(),
                effect: GameActions.DiscardFromPlay(),
                message: "{0} returns to the deep",
                messageArgs: new object[] { token }
            );
            
            GameActions.CardLastingEffect(new LastingEffectCardAction.LastingEffectCardProperties
            {
                effect = delayedEffect
            }).Resolve(token, context);
            
            LogExecution("Created token {0} from {1}", token.name, card.name);
        }
        
        /// <summary>
        /// Check if province needs to be refilled after card is removed
        /// </summary>
        private void CheckForRefillProvince(BaseCard card, GameEvent gameEvent, GameActionProperties additionalProperties)
        {
            if (!card.IsInProvince() || card.location == Locations.StrongholdProvince)
                return;
                
            var context = gameEvent.context;
            var isReplacementEffect = additionalProperties?.ContainsKey("replacementEffect") == true;
            
            var refillContext = isReplacementEffect ? gameEvent.context.eventObject.context : context;
            refillContext.RefillProvince(card.controller, card.location);
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to make token of specific card
        /// </summary>
        public static CreateTokenAction OfCard(BaseCard card)
        {
            var action = new CreateTokenAction();
            action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Create action to make token during military conflicts
        /// </summary>
        public static CreateTokenAction DuringMilitaryConflict()
        {
            return new CreateTokenAction();
        }
        
        #endregion
    }
}