using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using L5RGame.Extensions;

namespace L5RGame
{
    [System.Serializable]
    public class CardData
    {
        public string id;
        public string name;
        public string type;
        public List<string> traits = new List<string>();
        public string clan;
        public int military_bonus;
        public int political_bonus;
        public int fate;
        public bool unicity;
        public string text;
        public string flavor;
        public int glory;
        public int military;
        public int political;
        public int strength;
        public int influencePool;
        public int influenceCost;
        public int honor;
        public string side;
        public string pack_id;
    }

    [System.Serializable]
    public class CardAbilities
    {
        public List<CardAction> actions = new List<CardAction>();
        public List<TriggeredAbility> reactions = new List<TriggeredAbility>();
        public List<PersistentEffect> persistentEffects = new List<PersistentEffect>();
        public List<CustomPlayAction> playActions = new List<CustomPlayAction>();
    }

    [System.Serializable]
    public class CardMenuOption
    {
        public string command;
        public string text;
        public string arg;
        public bool disabled;
    }

    public class BaseCard : EffectSource
    {
        [Header("Card Identity")]
        public Player owner;
        public Player controller;
        public Game game;
        public CardData cardData;

        [Header("Card Properties")]
        public string id;
        public string printedName;
        public string printedType;
        public bool inConflict = false;
        public string type;
        public bool facedown = false;

        [Header("Card State")]
        public Dictionary<string, int> tokens = new Dictionary<string, int>();
        public List<CardMenuOption> menu = new List<CardMenuOption>();
        public bool showPopup = false;
        public string popupMenuText = "";
        public List<string> traits = new List<string>();
        public string printedFaction;
        public string location;
        public bool bowed = false;
        public bool isBowed => bowed;
        public bool ready = true;
        public bool isBroken = false;
        public bool covert = false;

        [Header("Card Type Flags")]
        public bool isProvince = false;
        public bool isConflict = false;
        public bool isDynasty = false;
        public bool isStronghold = false;
        public bool isNew = false;
        public bool selected = false;

        [Header("Card Relationships")]
        public List<BaseCard> attachments = new List<BaseCard>();
        public List<BaseCard> childCards = new List<BaseCard>();
        public BaseCard parent;

        [Header("Card Abilities")]
        public CardAbilities abilities = new CardAbilities();

        [Header("Keywords and Restrictions")]
        public List<string> printedKeywords = new List<string>();
        public List<string> allowedAttachmentTraits = new List<string>();
        public List<string> disguisedKeywordTraits = new List<string>();

        [Header("IronPython Integration")]
        public string scriptName;
        public bool hasCustomScript = false;
        public PythonCardScript pythonScript;
        public object reactionAbility;
        public object interruptAbility;

        // Static keyword validation
        private static readonly string[] ValidKeywords = {
            "ancestral", "restricted", "limited", "sincerity",
            "courtesy", "pride", "covert"
        };

        public virtual void Initialize(CardData data, Player cardOwner)
        {
            owner = cardOwner;
            controller = cardOwner;
            game = cardOwner.game;
            cardData = data;

            // Set basic properties
            id = data.id;
            printedName = data.name;
            printedType = data.type;
            type = data.type;
            traits = data.traits ?? new List<string>();
            printedFaction = data.clan;

            // Set script name for IronPython integration
            scriptName = GenerateScriptName();

            // Initialize as EffectSource
            Initialize(game, printedName);

            Debug.Log($"🃏 Card {printedName} initialized with script: {scriptName}");
        }

        private string GenerateScriptName()
        {
            // Convert card name to snake_case for Python script filename
            return printedName.ToLower()
                .Replace(" ", "_")
                .Replace("'", "")
                .Replace("-", "_")
                .Replace(",", "");
        }

        public virtual string GetCardType()
        {
            return type;
        }

        public virtual int GetCost()
        {
            return cardData.fate;
        }

        public bool IsInPlay()
        {
            if (facedown) return false;

            var inProvinceTypes = new[] { CardTypes.Holding, CardTypes.Province, CardTypes.Stronghold };
            if (inProvinceTypes.Contains(type))
            {
                return IsInProvince();
            }

            return location == Locations.PlayArea;
        }

        public bool IsInProvince()
        {
            var provinceLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree,
                Locations.ProvinceFour, Locations.StrongholdProvince
            };
            return provinceLocations.Contains(location);
        }

        /// <summary>
        /// Get contribution to conflict for this card
        /// </summary>
        /// <param name="conflictType">Type of conflict (military/political)</param>
        /// <returns>Skill contribution</returns>
        public virtual int GetContributionToConflict(string conflictType)
        {
            // Override in character cards to return appropriate skill
            return 0;
        }

        /// <summary>
        /// Check if this card can participate as an attacker
        /// </summary>
        /// <param name="conflictType">Type of conflict</param>
        /// <returns>True if can attack</returns>
        public virtual bool CanParticipateAsAttacker(string conflictType)
        {
            return IsInPlay() && !facedown && !bowed;
        }

        /// <summary>
        /// Check if this card can participate as a defender
        /// </summary>
        /// <param name="conflictType">Type of conflict</param>
        /// <returns>True if can defend</returns>
        public virtual bool CanParticipateAsDefender(string conflictType)
        {
            return IsInPlay() && !facedown && !bowed;
        }

        /// <summary>
        /// Get province strength for province cards
        /// </summary>
        /// <returns>Province strength</returns>
        public virtual int GetStrength()
        {
            return cardData.strength;
        }

        /// <summary>
        /// Get military skill for characters
        /// </summary>
        /// <returns>Military skill</returns>
        public virtual int GetMilitarySkill()
        {
            return cardData.military;
        }

        /// <summary>
        /// Get political skill for characters
        /// </summary>
        /// <returns>Political skill</returns>
        public virtual int GetPoliticalSkill()
        {
            return cardData.political;
        }

        /// <summary>
        /// Check if this character can be attacked
        /// </summary>
        /// <returns>True if can be attacked</returns>
        public virtual bool CanBeAttacked()
        {
            return type == CardTypes.Character && IsInPlay() && !bowed;
        }

        /// <summary>
        /// Check if this character can declare as attacker
        /// </summary>
        /// <returns>True if can declare as attacker</returns>
        public virtual bool CanDeclareAsAttacker()
        {
            return type == CardTypes.Character && IsInPlay() && !bowed && ready;
        }

        /// <summary>
        /// Get contribution to imperial favor for this card
        /// </summary>
        /// <returns>Glory contribution</returns>
        public virtual int GetContributionToImperialFavor()
        {
            return cardData.glory;
        }

        /// <summary>
        /// Check if this card is currently participating in a conflict
        /// </summary>
        /// <returns>True if participating in conflict</returns>
        public virtual bool IsParticipating()
        {
            return inConflict;
        }

        /// <summary>
        /// Check if this card is currently blank (has no abilities)
        /// </summary>
        /// <returns>True if card is blank</returns>
        public virtual bool IsBlank()
        {
            // Card is blank if it has a blank effect or is face down
            return facedown || HasEffect(EffectNames.Blank);
        }

        /// <summary>
        /// Check if this card can trigger abilities
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if can trigger abilities</returns>
        public virtual bool CanTriggerAbilities(AbilityContext context)
        {
            // Cannot trigger abilities if blank or if restricted by effects
            return !IsBlank() && !HasRestriction("cannotTriggerAbilities", context);
        }

        // Additional methods for game mechanics
        public virtual void FlipFaceup() 
        { 
            facedown = false; 
        }
        
        public virtual int GetGlory() 
        { 
            return cardData?.glory ?? 0; 
        }
        
        public virtual int GetFate() 
        { 
            return cardData?.fate ?? 0; 
        }
        
        public virtual void Play(AbilityContext context) 
        { 
            // Placeholder implementation
        }

        /// <summary>
        /// Check if this card can be played
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="playType">Type of play action</param>
        /// <returns>True if can be played</returns>
        public virtual bool CanPlay(AbilityContext context, string playType = null)
        {
            // Basic checks for playing a card
            if (facedown && type != CardTypes.Province) return false;
            if (HasRestriction("cannotPlay", context)) return false;
            
            // Type-specific play restrictions
            switch (type)
            {
                case CardTypes.Event:
                    return CanPlayEvent(context);
                case CardTypes.Character:
                    return CanPlayCharacter(context);
                case CardTypes.Attachment:
                    return CanPlayAttachment(context);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Check if this card can initiate keyword abilities
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if can initiate keyword abilities</returns>
        public virtual bool CanInitiateKeywords(AbilityContext context)
        {
            return !IsBlank() && !HasRestriction("cannotInitiateKeywords", context);
        }

        /// <summary>
        /// Update effect contexts when card state changes
        /// </summary>
        public virtual void UpdateEffectContexts()
        {
            // Update any persistent effects that depend on this card's state
            game?.effectEngine?.CheckEffects(true);
        }

        /// <summary>
        /// Get the modified controller of this card (considering control effects)
        /// </summary>
        /// <returns>Player who currently controls this card</returns>
        public virtual Player GetModifiedController()
        {
            // Check for take control effects
            var controlEffects = GetEffects(EffectNames.TakeControl);
            if (controlEffects.Count > 0)
            {
                // Return the most recent control effect's target player
                var latestEffect = controlEffects.LastOrDefault();
                if (latestEffect is Player newController)
                {
                    return newController;
                }
            }
            
            return controller;
        }

        /// <summary>
        /// Check for illegal attachments and remove them
        /// </summary>
        public virtual void CheckForIllegalAttachments()
        {
            var illegalAttachments = new List<BaseCard>();
            
            foreach (var attachment in attachments)
            {
                if (!CanAttach(attachment))
                {
                    illegalAttachments.Add(attachment);
                }
            }
            
            foreach (var illegal in illegalAttachments)
            {
                RemoveAttachment(illegal);
                game?.AddMessage("{0} is discarded as an illegal attachment", illegal);
            }
        }

        /// <summary>
        /// Check if this card is limited (has limited keyword)
        /// </summary>
        /// <returns>True if card has limited keyword</returns>
        public virtual bool IsLimited()
        {
            return HasKeyword(Keywords.Limited);
        }

        /// <summary>
        /// Check if this card has a specific keyword
        /// </summary>
        /// <param name="keyword">Keyword to check for</param>
        /// <returns>True if card has the keyword</returns>
        public virtual bool HasKeyword(string keyword)
        {
            return printedKeywords.Contains(keyword) || 
                   traits.Contains(keyword) || 
                   HasEffect($"gain_{keyword}_keyword");
        }

        /// <summary>
        /// Check if this card has a specific effect
        /// </summary>
        /// <param name="effectName">Name of effect to check</param>
        /// <returns>True if card has the effect</returns>
        public virtual bool HasEffect(string effectName)
        {
            // This would check the game's effect engine for effects on this card
            return game?.effectEngine != null && GetEffects(effectName).Count > 0;
        }

        /// <summary>
        /// Get all effects of a specific type on this card
        /// </summary>
        /// <param name="effectName">Name of effect type</param>
        /// <returns>List of effects</returns>
        public virtual List<object> GetEffects(string effectName)
        {
            // Placeholder - would integrate with effect engine
            return new List<object>();
        }

        /// <summary>
        /// Check if this card has a specific restriction
        /// </summary>
        /// <param name="restrictionType">Type of restriction</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if restricted</returns>
        public virtual bool HasRestriction(string restrictionType, AbilityContext context)
        {
            return HasEffect($"cannot_{restrictionType}");
        }

        /// <summary>
        /// Check restrictions for specific actions
        /// </summary>
        /// <param name="actionType">Type of action to check</param>
        /// <param name="context">Ability context</param>
        /// <returns>True if action is allowed</returns>
        public virtual bool CheckRestrictions(string actionType, AbilityContext context)
        {
            return !HasRestriction(actionType, context);
        }

        /// <summary>
        /// Sum all effects of a given type on this card
        /// </summary>
        /// <param name="effectName">Name of the effect type</param>
        /// <returns>Sum of all matching effects</returns>
        public virtual int SumEffects(string effectName)
        {
            // Placeholder - would integrate with effect engine
            return 0;
        }

        /// <summary>
        /// Check if any effect of a type exists on this card
        /// </summary>
        /// <param name="effectName">Name of the effect type</param>
        /// <returns>True if any matching effect exists</returns>
        public virtual bool AnyEffect(string effectName)
        {
            return HasEffect(effectName);
        }

        /// <summary>
        /// Copy properties from another card (for tokens)
        /// </summary>
        /// <param name="template">Card to copy from</param>
        public virtual void CopyFrom(BaseCard template)
        {
            if (template == null) return;
            
            printedName = template.printedName;
            printedType = template.printedType;
            type = template.type;
            traits = new List<string>(template.traits);
            printedFaction = template.printedFaction;
            owner = template.owner;
            controller = template.controller;
            game = template.game;
            cardData = template.cardData;
        }

        /// <summary>
        /// Check if an event card can be played
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if event can be played</returns>
        protected virtual bool CanPlayEvent(AbilityContext context)
        {
            // Basic event play checks
            return location == Locations.Hand && context.player.fate >= GetCost();
        }

        /// <summary>
        /// Check if a character card can be played
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if character can be played</returns>
        protected virtual bool CanPlayCharacter(AbilityContext context)
        {
            // Basic character play checks
            return (location == Locations.Hand || IsInProvince()) && 
                   context.player.fate >= GetCost();
        }

        /// <summary>
        /// Check if an attachment card can be played
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if attachment can be played</returns>
        protected virtual bool CanPlayAttachment(AbilityContext context)
        {
            // Basic attachment play checks
            return location == Locations.Hand && 
                   context.player.fate >= GetCost() &&
                   HasValidAttachmentTargets();
        }

        /// <summary>
        /// Check if there are valid attachment targets for this card
        /// </summary>
        /// <returns>True if there are valid targets</returns>
        protected virtual bool HasValidAttachmentTargets()
        {
            if (type != CardTypes.Attachment) return true;
            
            // Find potential attachment targets based on traits
            var potentialTargets = game.FindAnyCardsInPlay(card => 
                card.controller == controller && CanAttachTo(card));
                
            return potentialTargets.Count > 0;
        }

        /// <summary>
        /// Check if this attachment can attach to a specific card
        /// </summary>
        /// <param name="targetCard">Potential attachment target</param>
        /// <returns>True if can attach</returns>
        public virtual bool CanAttachTo(BaseCard targetCard)
        {
            if (type != CardTypes.Attachment) return false;
            if (targetCard == null) return false;
            
            // Check trait restrictions
            if (allowedAttachmentTraits.Count > 0)
            {
                bool hasMatchingTrait = allowedAttachmentTraits.Any(trait => 
                    targetCard.traits.Contains(trait));
                if (!hasMatchingTrait) return false;
            }
            
            // Check other attachment restrictions
            return CheckAttachmentRestrictions(targetCard);
        }

        /// <summary>
        /// Check if an attachment can attach to this card
        /// </summary>
        /// <param name="attachment">Attachment to check</param>
        /// <returns>True if attachment can attach</returns>
        public virtual bool CanAttach(BaseCard attachment)
        {
            return attachment?.CanAttachTo(this) ?? false;
        }

        /// <summary>
        /// Check attachment-specific restrictions
        /// </summary>
        /// <param name="targetCard">Target card to attach to</param>
        /// <returns>True if restrictions pass</returns>
        protected virtual bool CheckAttachmentRestrictions(BaseCard targetCard)
        {
            // Check for attachment limit effects
            if (HasKeyword(Keywords.Restricted) && 
                targetCard.attachments.Count(a => a.HasKeyword(Keywords.Restricted)) >= 2)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Remove an attachment from this card
        /// </summary>
        /// <param name="attachment">Attachment to remove</param>
        public virtual void RemoveAttachment(BaseCard attachment)
        {
            if (attachments.Remove(attachment))
            {
                attachment.parent = null;
                // Move to discard pile
                attachment.controller.MoveCard(attachment, attachment.GetCardType() == CardTypes.Character ? 
                    Locations.DynastyDiscardPile : Locations.ConflictDiscardPile);
            }
        }

        /// <summary>
        /// Get the unique identifier for this card instance
        /// </summary>
        public string uuid => GetInstanceID().ToString();

        /// <summary>
        /// Get the display name of this card
        /// </summary>
        public string name => printedName;

        // Cleanup when destroyed
        protected virtual void OnDestroy()
        {
            // Clear references
            attachments?.Clear();
            childCards?.Clear();

            Debug.Log($"🃏 Card {printedName ?? "Unknown"} destroyed");
        }
    }
}
