using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a status token (honored/dishonored) that can be attached to characters.
    /// Provides persistent effects based on the token type and character's glory.
    /// </summary>
    public class StatusToken : EffectSource
    {
        [Header("Status Token Properties")]
        public bool honored = false;
        public bool dishonored = false;
        public BaseCard card;
        public string printedType = "token";

        [Header("Effect Management")]
        public List<PersistentEffectReference> persistentEffectReferences = new List<PersistentEffectReference>();

        /// <summary>
        /// Constructor for status tokens
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="targetCard">Card this token is attached to</param>
        /// <param name="isHonored">True for honored token, false for dishonored</param>
        public StatusToken(Game gameInstance, BaseCard targetCard, bool isHonored) : base(gameInstance, isHonored ? "Honored Token" : "Dishonored Token")
        {
            honored = isHonored;
            dishonored = !isHonored;
            card = targetCard;
            printedType = "token";
            
            Initialize(gameInstance, isHonored ? "Honored Token" : "Dishonored Token");
            ApplyHonorEffects();
        }

        /// <summary>
        /// Default constructor for Unity serialization
        /// </summary>
        public StatusToken() : base()
        {
            printedType = "token";
            persistentEffectReferences = new List<PersistentEffectReference>();
        }

        /// <summary>
        /// Initialize the status token
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="targetCard">Target card</param>
        /// <param name="isHonored">Whether this is an honored token</param>
        public void Initialize(Game gameInstance, BaseCard targetCard, bool isHonored)
        {
            game = gameInstance;
            card = targetCard;
            honored = isHonored;
            dishonored = !isHonored;
            
            string tokenName = isHonored ? "Honored Token" : "Dishonored Token";
            base.Initialize(gameInstance, tokenName);
            
            ApplyHonorEffects();
        }

        /// <summary>
        /// Applies honor/dishonor effects to the attached card
        /// </summary>
        public virtual void ApplyHonorEffects()
        {
            if (card == null || game?.EffectEngine == null)
                return;

            // Remove existing effects first
            RemoveHonorEffects();

            // Calculate skill modification based on glory
            var gloryValue = card.GetGlory();
            var skillModifier = honored ? gloryValue : -gloryValue;

            if (skillModifier != 0)
            {
                // Create persistent effect for skill modification
                var effectProperties = new EffectProperties
                {
                    match = card,
                    effect = CreateSkillModificationEffect(skillModifier),
                    duration = Durations.Persistent,
                    location = Locations.Any
                };

                var effectRef = AddEffectToEngine(effectProperties);
                persistentEffectReferences.Add(new PersistentEffectReference
                {
                    effect = effectRef,
                    properties = effectProperties
                });
            }

            Debug.Log($"🎭 {name} applied to {card.printedName} (Glory: {gloryValue}, Skill Modifier: {skillModifier})");
        }

        /// <summary>
        /// Creates the skill modification effect for honor/dishonor
        /// </summary>
        /// <param name="modifier">Amount to modify skills by</param>
        /// <returns>Effect function</returns>
        private System.Func<Game, EffectSource, EffectProperties, object> CreateSkillModificationEffect(int modifier)
        {
            return (game, source, properties) => 
            {
                return new PersistentEffect
                {
                    match = card,
                    effect = (context) => 
                    {
                        if (context.source == card)
                        {
                            // Apply military skill modification
                            card.AddStatModifier("military", modifier);
                            // Apply political skill modification
                            card.AddStatModifier("political", modifier);
                        }
                    },
                    condition = (context) => card != null && card.IsInPlay()
                };
            };
        }

        /// <summary>
        /// Removes all honor effects from the attached card
        /// </summary>
        public virtual void RemoveHonorEffects()
        {
            if (game?.EffectEngine == null)
                return;

            foreach (var effectRef in persistentEffectReferences)
            {
                if (effectRef.effect != null)
                {
                    RemoveEffectFromEngine(new List<object> { effectRef.effect });
                }
            }
            
            persistentEffectReferences.Clear();
        }

        /// <summary>
        /// Changes the card this token is attached to
        /// </summary>
        /// <param name="newCard">New card to attach to</param>
        public virtual void SetCard(BaseCard newCard)
        {
            if (card == newCard)
                return;

            // Remove effects from old card
            RemoveHonorEffects();
            
            // Set new card
            card = newCard;
            
            // Apply effects to new card
            if (newCard != null)
            {
                ApplyHonorEffects();
                Debug.Log($"🎭 {name} moved to {newCard.printedName}");
            }
        }

        /// <summary>
        /// Gets the glory value used for this token's effects
        /// </summary>
        /// <returns>Glory value</returns>
        public virtual int GetGloryValue()
        {
            return card?.GetGlory() ?? 0;
        }

        /// <summary>
        /// Gets the skill modification provided by this token
        /// </summary>
        /// <returns>Skill modification amount</returns>
        public virtual int GetSkillModification()
        {
            var glory = GetGloryValue();
            return honored ? glory : -glory;
        }

        /// <summary>
        /// Checks if this is an honored token
        /// </summary>
        /// <returns>True if honored</returns>
        public virtual bool IsHonored()
        {
            return honored;
        }

        /// <summary>
        /// Checks if this is a dishonored token
        /// </summary>
        /// <returns>True if dishonored</returns>
        public virtual bool IsDishonored()
        {
            return dishonored;
        }

        /// <summary>
        /// Gets the token type for UI display
        /// </summary>
        /// <returns>Token type string</returns>
        public virtual string GetTokenType()
        {
            return honored ? "honored" : "dishonored";
        }

        /// <summary>
        /// Gets a summary of this token for UI display
        /// </summary>
        /// <returns>Token summary</returns>
        public virtual StatusTokenSummary GetSummary()
        {
            return new StatusTokenSummary
            {
                type = GetTokenType(),
                honored = honored,
                dishonored = dishonored,
                cardName = card?.printedName ?? "No Card",
                gloryValue = GetGloryValue(),
                skillModification = GetSkillModification()
            };
        }

        /// <summary>
        /// Checks if this token can be moved to another card
        /// </summary>
        /// <param name="targetCard">Target card to move to</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if the token can be moved</returns>
        public virtual bool CanMoveTo(BaseCard targetCard, AbilityContext context = null)
        {
            if (targetCard == null)
                return false;

            // Can only attach to characters
            if (targetCard.GetCardType() != CardTypes.Character)
                return false;

            // Card must be in play
            if (!targetCard.IsInPlay())
                return false;

            // Check for restrictions
            if (targetCard.HasRestriction("cannotHaveStatusTokens", context))
                return false;

            // Check if card already has this type of token
            if (honored && targetCard.IsHonored())
                return false;

            if (dishonored && targetCard.IsDishonored())
                return false;

            return true;
        }

        /// <summary>
        /// Moves this token to a new card
        /// </summary>
        /// <param name="targetCard">Target card</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if move was successful</returns>
        public virtual bool MoveTo(BaseCard targetCard, AbilityContext context = null)
        {
            if (!CanMoveTo(targetCard, context))
                return false;

            var oldCard = card;
            SetCard(targetCard);

            // Trigger move events if needed
            if (game != null && oldCard != null)
            {
                var moveEvent = game.GetEvent(EventNames.OnStatusTokenMoved, new Dictionary<string, object>
                {
                    ["token"] = this,
                    ["fromCard"] = oldCard,
                    ["toCard"] = targetCard,
                    ["context"] = context
                });

                game.OpenSimpleEventWindow(moveEvent);
            }

            return true;
        }

        /// <summary>
        /// Removes this token from its current card
        /// </summary>
        /// <param name="context">Ability context</param>
        public virtual void RemoveFromCard(AbilityContext context = null)
        {
            if (card == null)
                return;

            var removedFromCard = card;
            
            // Remove effects
            RemoveHonorEffects();
            
            // Clear card reference
            card = null;

            // Trigger remove events if needed
            if (game != null)
            {
                var removeEvent = game.GetEvent(EventNames.OnStatusTokenRemoved, new Dictionary<string, object>
                {
                    ["token"] = this,
                    ["fromCard"] = removedFromCard,
                    ["context"] = context
                });

                game.OpenSimpleEventWindow(removeEvent);
            }

            Debug.Log($"🎭 {name} removed from {removedFromCard.printedName}");
        }

        /// <summary>
        /// Creates a copy of this status token
        /// </summary>
        /// <returns>Copied status token</returns>
        public virtual StatusToken Copy()
        {
            var copy = new StatusToken(game, card, honored);
            return copy;
        }

        /// <summary>
        /// String representation of the status token
        /// </summary>
        /// <returns>String describing the token</returns>
        public override string ToString()
        {
            var cardName = card?.printedName ?? "No Card";
            var tokenType = honored ? "Honored" : "Dishonored";
            return $"{tokenType} Token on {cardName}";
        }

        /// <summary>
        /// Cleanup when token is destroyed
        /// </summary>
        protected override void OnDestroy()
        {
            RemoveHonorEffects();
            base.OnDestroy();
        }

        // Property aliases for compatibility
        public bool Honored => honored;
        public bool Dishonored => dishonored;
        public BaseCard Card => card;
        public string Type => printedType;
    }

    /// <summary>
    /// Reference to a persistent effect created by a status token
    /// </summary>
    [Serializable]
    public class PersistentEffectReference
    {
        public object effect;
        public EffectProperties properties;
    }

    /// <summary>
    /// Summary of a status token for UI display
    /// </summary>
    [Serializable]
    public class StatusTokenSummary
    {
        public string type;
        public bool honored;
        public bool dishonored;
        public string cardName;
        public int gloryValue;
        public int skillModification;
    }

    /// <summary>
    /// Factory class for creating status tokens
    /// </summary>
    public static class StatusTokenFactory
    {
        /// <summary>
        /// Creates an honored token
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="card">Target card</param>
        /// <returns>Honored token</returns>
        public static StatusToken CreateHonoredToken(Game game, BaseCard card)
        {
            return new StatusToken(game, card, true);
        }

        /// <summary>
        /// Creates a dishonored token
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="card">Target card</param>
        /// <returns>Dishonored token</returns>
        public static StatusToken CreateDishonoredToken(Game game, BaseCard card)
        {
            return new StatusToken(game, card, false);
        }

        /// <summary>
        /// Creates a status token of the specified type
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="card">Target card</param>
        /// <param name="tokenType">Type of token (honored/dishonored)</param>
        /// <returns>Status token</returns>
        public static StatusToken CreateToken(Game game, BaseCard card, string tokenType)
        {
            switch (tokenType.ToLower())
            {
                case "honored":
                case "honor":
                    return CreateHonoredToken(game, card);
                case "dishonored":
                case "dishonor":
                    return CreateDishonoredToken(game, card);
                default:
                    Debug.LogWarning($"Unknown status token type: {tokenType}");
                    return null;
            }
        }
    }

    /// <summary>
    /// Extension methods for status token functionality
    /// </summary>
    public static class StatusTokenExtensions
    {
        /// <summary>
        /// Adds a status token to a card
        /// </summary>
        /// <param name="card">Target card</param>
        /// <param name="token">Token to add</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if token was added successfully</returns>
        public static bool AddStatusToken(this BaseCard card, StatusToken token, AbilityContext context = null)
        {
            if (token == null)
                return false;

            return token.MoveTo(card, context);
        }

        /// <summary>
        /// Removes a status token from a card
        /// </summary>
        /// <param name="card">Source card</param>
        /// <param name="token">Token to remove</param>
        /// <param name="context">Ability context</param>
        public static void RemoveStatusToken(this BaseCard card, StatusToken token, AbilityContext context = null)
        {
            if (token?.card == card)
            {
                token.RemoveFromCard(context);
            }
        }

        /// <summary>
        /// Gets all status tokens on a card
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>List of status tokens</returns>
        public static List<StatusToken> GetStatusTokens(this BaseCard card)
        {
            var tokens = new List<StatusToken>();

            // Check for honored token
            if (card.personalHonor is StatusToken honoredToken)
            {
                tokens.Add(honoredToken);
            }

            // Add other token types as needed
            return tokens;
        }

        /// <summary>
        /// Gets the honored token on a card
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>Honored token or null</returns>
        public static StatusToken GetHonoredToken(this BaseCard card)
        {
            return card.personalHonor as StatusToken;
        }

        /// <summary>
        /// Checks if a card has any status tokens
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if card has status tokens</returns>
        public static bool HasStatusTokens(this BaseCard card)
        {
            return card.GetStatusTokens().Count > 0;
        }
    }

    /// <summary>
    /// Additional event names for status tokens
    /// </summary>
    public static partial class EventNames
    {
        public const string OnStatusTokenMoved = "onStatusTokenMoved";
        public const string OnStatusTokenRemoved = "onStatusTokenRemoved";
        public const string OnStatusTokenAdded = "onStatusTokenAdded";
    }
}