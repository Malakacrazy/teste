using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Adds a token to a card
    /// </summary>
    [System.Serializable]
    public partial class AddTokenAction : CardGameAction
    {
        [Header("Add Token Configuration")]
        [SerializeField] private string tokenType = TokenTypes.Honor;
        
        /// <summary>
        /// Properties specific to adding tokens
        /// </summary>
        [System.Serializable]
        public class AddTokenProperties : CardActionProperties
        {
            public string tokenType = TokenTypes.Honor;
            
            public AddTokenProperties() : base() { }
            
            public AddTokenProperties(string tokenType) : base()
            {
                this.tokenType = tokenType;
            }
        }
        
        #region Constructors
        
        public AddTokenAction() : base()
        {
            Initialize();
        }
        
        public AddTokenAction(AddTokenProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public AddTokenAction(System.Func<AbilityContext, AddTokenProperties> factory) : base(context => factory(context))
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "addToken";
            eventName = EventNames.OnAddTokenToCard;
            effectMessage = "add a {1} token to {0}";
            targetTypes = new List<string> 
            { 
                CardTypes.Character, 
                CardTypes.Attachment, 
                CardTypes.Holding, 
                CardTypes.Province,
                CardTypes.Stronghold
            };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new AddTokenProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is AddTokenProperties addTokenProps)
                return addTokenProps;
                
            // Convert base properties to AddTokenProperties
            return new AddTokenProperties(tokenType)
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
            return ("add a {1} token to {0}", new object[] { properties.target, properties.tokenType });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is BaseCard card))
                return false;
                
            // Can't add tokens to face-down cards
            if (card.facedown)
                return false;
                
            // Holdings and Provinces can only have tokens added if they're in a province location
            if (card.type == CardTypes.Holding || card.type == CardTypes.Province)
            {
                if (!card.location.Contains("province"))
                    return false;
            }
            // Other cards must be in play area
            else if (card.location != Locations.PlayArea)
            {
                return false;
            }
            
            return base.CanAffect(target, context, additionalProperties);
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            gameEvent.AddProperty("tokenType", properties.tokenType);
        }
        
        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("target") as BaseCard;
            var tokenType = gameEvent.GetProperty("tokenType", TokenTypes.Honor);
            
            if (card != null)
            {
                card.AddToken(tokenType);
                LogExecution("Added {0} token to {1}", tokenType, card.name);
            }
            return true;
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Create action to add honor token
        /// </summary>
        public static AddTokenAction Honor(BaseCard target = null)
        {
            var action = new AddTokenAction(new AddTokenProperties(TokenTypes.Honor));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action to add dishonor token
        /// </summary>
        public static AddTokenAction Dishonor(BaseCard target = null)
        {
            var action = new AddTokenAction(new AddTokenProperties(TokenTypes.Dishonor));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        /// <summary>
        /// Create action to add fate token
        /// </summary>
        public static AddTokenAction Fate(BaseCard target = null)
        {
            var action = new AddTokenAction(new AddTokenProperties(TokenTypes.Fate));
            if (target != null)
                action.SetDefaultTarget(context => target);
            return action;
        }
        
        #endregion
    }
}