using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Puts characters into play, optionally participating in the current conflict
    /// </summary>
    [System.Serializable]
    public partial class PutIntoPlayAction : CardGameAction
    {
        [Header("Put Into Play Configuration")]
        [SerializeField] private bool intoConflict = true;
        
        /// <summary>
        /// Properties specific to putting cards into play
        /// </summary>
        [System.Serializable]
        public class PutIntoPlayProperties : CardActionProperties
        {
            public int fate = 0;
            public string status = "ordinary";
            public bool intoConflict = true;
            
            public PutIntoPlayProperties() : base() { }
            
            public PutIntoPlayProperties(int fate, string status = "ordinary", bool intoConflict = true) : base()
            {
                this.fate = fate;
                this.status = status;
                this.intoConflict = intoConflict;
            }
        }
        
        #region Constructors
        
        public PutIntoPlayAction() : base()
        {
            Initialize();
        }
        
        public PutIntoPlayAction(bool intoConflict = true) : base()
        {
            this.intoConflict = intoConflict;
            Initialize();
        }
        
        public PutIntoPlayAction(PutIntoPlayProperties properties) : base(properties)
        {
            this.intoConflict = properties.intoConflict;
            Initialize();
        }
        
        public PutIntoPlayAction(System.Func<AbilityContext, PutIntoPlayProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "putIntoPlay";
            eventName = EventNames.OnCharacterEntersPlay;
            costMessage = "putting {0} into play";
            effectMessage = intoConflict ? "put {0} into play in the conflict" : "put {0} into play";
            targetTypes = new List<string> { CardTypes.Character };
        }
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Get properties with proper typing
        /// </summary>
        public new PutIntoPlayProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var baseProps = base.GetProperties(context, additionalProperties);
            
            if (baseProps is PutIntoPlayProperties playProps)
                return playProps;
                
            // Convert base properties to PutIntoPlayProperties
            return new PutIntoPlayProperties()
            {
                target = baseProps.target,
                cannotBeCancelled = baseProps.cannotBeCancelled,
                optional = baseProps.optional,
                parentAction = baseProps.parentAction,
                intoConflict = this.intoConflict
            };
        }
        
        #endregion
        
        #region Messaging
        
        public override (string message, object[] args) GetEffectMessage(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var message = properties.intoConflict ? "put {0} into play in the conflict" : "put {0} into play";
            return (message, new object[] { properties.target });
        }
        
        #endregion
        
        #region Targeting
        
        public override bool CanAffect(object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            if (!(target is DrawCard card))
                return false;
                
            if (!context || !base.CanAffect(target, context, additionalProperties))
                return false;
                
            // Check for unique conflicts
            if (!context.player || card.AnotherUniqueInPlay(context.player))
                return false;
                
            // Cannot put cards already in play or face down cards into play
            if (card.location == Locations.PlayArea || card.facedown)
                return false;
                
            var properties = GetProperties(context, additionalProperties);
            
            // Additional checks for putting into conflict
            if (properties.intoConflict)
            {
                // Must have current conflict
                if (context.game.currentConflict == null)
                    return false;
                    
                // Check participation restrictions
                if (context.player.IsAttackingPlayer() && !card.CanParticipateAsAttacker())
                    return false;
                    
                if (context.player.IsDefendingPlayer() && !card.CanParticipateAsDefender())
                    return false;
                    
                // Check conflict type restrictions (dash abilities)
                if (card.HasDash(context.game.currentConflict.conflictType))
                    return false;
                    
                // Check general put into play restrictions
                if (!card.CheckRestrictions("putIntoPlay", context))
                    return false;
            }
            
            return true;
        }
        
        #endregion
        
        #region Event Management
        
        protected override void AddPropertiesToEvent(GameEvent gameEvent, object target, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var card = target as DrawCard;
            
            base.AddPropertiesToEvent(gameEvent, target, context, additionalProperties);
            
            gameEvent.AddProperty("fate", properties.fate);
            gameEvent.AddProperty("status", properties.status);
            gameEvent.AddProperty("intoConflict", properties.intoConflict);
            gameEvent.AddProperty("originalLocation", card?.location);
        }
        
        protected override void EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var card = gameEvent.GetProperty("card") as DrawCard;
            var context = gameEvent.context;
            var fate = gameEvent.GetProperty("fate", 0);
            var status = gameEvent.GetProperty("status", "ordinary");
            var intoConflict = gameEvent.GetProperty("intoConflict", false);
            
            if (card == null || context?.player == null)
                return;
            
            // Check for province refill
            CheckForRefillProvince(card, gameEvent, additionalProperties);
            
            // Mark card as new
            card.isNew = true;
            
            // Set fate if specified
            if (fate > 0)
            {
                card.fate = fate;
            }
            
            // Apply status effects
            switch (status)
            {
                case "honored":
                    card.Honor();
                    break;
                case "dishonored":
                    card.Dishonor();
                    break;
                // "ordinary" requires no action
            }
            
            // Move card to play area
            context.player.MoveCard(card, Locations.PlayArea);
            
            // Add to conflict if specified
            if (intoConflict && context.game.currentConflict != null)
            {
                if (context.player.IsAttackingPlayer())
                {
                    context.game.currentConflict.AddAttacker(card);
                }
                else
                {
                    context.game.currentConflict.AddDefender(card);
                }
            }
            
            LogExecution("Put {0} into play{1}", card.name, intoConflict ? " in the conflict" : "");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Set whether this action puts the card into the current conflict
        /// </summary>
        public void SetIntoConflict(bool intoConflict)
        {
            this.intoConflict = intoConflict;
            Initialize(); // Reinitialize with new conflict status
        }
        
        #endregion
        
        #region Static Factory Methods
        
        /// <summary>
        /// Put card into play participating in conflict
        /// </summary>
        public static PutIntoPlayAction IntoConflict(DrawCard card = null, int fate = 0)
        {
            var action = new PutIntoPlayAction(new PutIntoPlayProperties(fate, "ordinary", true));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Put card into play not participating in conflict
        /// </summary>
        public static PutIntoPlayAction OutOfConflict(DrawCard card = null, int fate = 0)
        {
            var action = new PutIntoPlayAction(new PutIntoPlayProperties(fate, "ordinary", false));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Put card into play with honor status
        /// </summary>
        public static PutIntoPlayAction Honored(DrawCard card = null, int fate = 0, bool intoConflict = true)
        {
            var action = new PutIntoPlayAction(new PutIntoPlayProperties(fate, "honored", intoConflict));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Put card into play with dishonor status
        /// </summary>
        public static PutIntoPlayAction Dishonored(DrawCard card = null, int fate = 0, bool intoConflict = true)
        {
            var action = new PutIntoPlayAction(new PutIntoPlayProperties(fate, "dishonored", intoConflict));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        /// <summary>
        /// Put card into play with specific fate amount
        /// </summary>
        public static PutIntoPlayAction WithFate(int fate, DrawCard card = null, bool intoConflict = true)
        {
            var action = new PutIntoPlayAction(new PutIntoPlayProperties(fate, "ordinary", intoConflict));
            if (card != null)
                action.SetDefaultTarget(context => card);
            return action;
        }
        
        #endregion
    }
}