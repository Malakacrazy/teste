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
        public CardData cardData;

        [Header("Card Properties")]
        public string id;
        public string printedName;
        public string printedType;
        public bool inConflict = false;
        public string type;
        public string CardType => type; // Property alias for compatibility

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
        public List<BaseAbility> actions => abilities.actions.Cast<BaseAbility>().ToList();
        public List<BaseAbility> reactions => abilities.reactions.Cast<BaseAbility>().ToList();
        public List<PersistentEffect> persistentEffects => abilities.persistentEffects;
        
        /// <summary>
        /// Alternative name for abilities property (compatibility)
        /// </summary>
        public CardAbilities Abilities => abilities;
        
        /// <summary>
        /// Alternative name for type property (compatibility)
        /// </summary>
        public string Type => type;

        [Header("Keywords and Restrictions")]
        public List<string> printedKeywords = new List<string>();
        public List<string> allowedAttachmentTraits = new List<string>();
        public List<string> disguisedKeywordTraits = new List<string>();

        [Header("IronPython Integration")]
        public string scriptName;
        public bool hasCustomScript = false;
        public PythonScriptInfo pythonScript;
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
        /// Get short summary for UI display
        /// </summary>
        /// <returns>Card summary object</returns>
        public virtual object GetShortSummary()
        {
            return new 
            {
                id = id,
                name = printedName,
                type = type,
                location = location,
                selected = selected,
                facedown = facedown
            };
        }

        /// <summary>
        /// Get short summary for control displays with player context
        /// </summary>
        /// <param name="player">Player viewing the controls</param>
        /// <returns>Card summary object for controls</returns>
        public virtual object GetShortSummaryForControls(Player player)
        {
            return new 
            {
                id = id,
                name = printedName,
                type = type,
                location = location,
                selected = selected,
                facedown = facedown,
                canSelect = player == controller
            };
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
        /// Alternative name for uuid property (compatibility)
        /// </summary>
        public string Uuid => uuid;

        /// <summary>
        /// Get the display name of this card
        /// </summary>
        public string name => printedName;

        /// <summary>
        /// Get or set fate tokens on this card
        /// </summary>
        public int fate
        {
            get => GetTokenCount("fate");
            set 
            {
                int current = GetTokenCount("fate");
                if (value > current)
                {
                    AddTokens("fate", value - current);
                }
                else if (value < current)
                {
                    RemoveToken("fate", current - value);
                }
            }
        }

        /// <summary>
        /// Modify fate tokens on this card
        /// </summary>
        /// <param name="amount">Amount to modify (can be negative)</param>
        public void ModifyFate(int amount)
        {
            if (amount > 0)
            {
                AddTokens("fate", amount);
            }
            else if (amount < 0)
            {
                RemoveToken("fate", Math.Abs(amount));
            }
        }

        /// <summary>
        /// Add tokens to this card
        /// </summary>
        /// <param name="tokenType">Type of token to add</param>
        /// <param name="amount">Number of tokens to add</param>
        public virtual void AddTokens(string tokenType, int amount)
        {
            if (amount <= 0) return;
            
            if (!tokens.ContainsKey(tokenType))
            {
                tokens[tokenType] = 0;
            }
            tokens[tokenType] += amount;
        }

        /// <summary>
        /// Remove tokens from this card (method overload for single token)
        /// </summary>
        /// <param name="tokenType">Type of token to remove</param>
        public virtual void RemoveToken(string tokenType)
        {
            RemoveToken(tokenType, 1);
        }
        
        /// <summary>
        /// Remove tokens from this card
        /// </summary>
        /// <param name="tokenType">Type of token to remove</param>
        /// <param name="amount">Number of tokens to remove</param>
        public virtual void RemoveToken(string tokenType, int amount)
        {
            if (tokens.ContainsKey(tokenType))
            {
                tokens[tokenType] = Mathf.Max(0, tokens[tokenType] - amount);
                if (tokens[tokenType] == 0)
                {
                    tokens.Remove(tokenType);
                }
            }
        }

        /// <summary>
        /// Get token count for a specific token type
        /// </summary>
        /// <param name="tokenType">Type of token to count</param>
        /// <returns>Number of tokens</returns>
        public virtual int GetTokenCount(string tokenType)
        {
            return tokens.ContainsKey(tokenType) ? tokens[tokenType] : 0;
        }

        /// <summary>
        /// Ready this card (un-bow it)
        /// </summary>
        public virtual void Ready()
        {
            bowed = false;
            ready = true;
        }

        /// <summary>
        /// Bow this card
        /// </summary>
        public virtual void Bow()
        {
            bowed = true;
            ready = false;
        }

        /// <summary>
        /// Check if this card is an attachment
        /// </summary>
        /// <returns>True if card is an attachment</returns>
        public virtual bool IsAttachment()
        {
            return type == CardTypes.Attachment;
        }

        /// <summary>
        /// Check if this card is a holding
        /// </summary>
        /// <returns>True if card is a holding</returns>
        public virtual bool IsHolding()
        {
            return type == CardTypes.Holding;
        }

        /// <summary>
        /// Check if this card can be targeted by an ability
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if card can be targeted</returns>
        public virtual bool CanBeTargetedBy(AbilityContext context)
        {
            if (context == null) return false;
            
            // Basic targeting checks
            if (facedown && type != CardTypes.Province) return false;
            if (HasRestriction("cannotBeTargeted", context)) return false;
            
            return true;
        }

        /// <summary>
        /// Check if this card can be targeted in a selection context
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <param name="selectedCards">Already selected cards</param>
        /// <returns>True if card can be targeted</returns>
        public virtual bool CanBeTargeted(AbilityContext context, List<BaseCard> selectedCards = null)
        {
            return CanBeTargetedBy(context);
        }

        /// <summary>
        /// Check if this card readies during the ready phase
        /// </summary>
        /// <returns>True if card readies during ready phase</returns>
        public virtual bool ReadiesDuringReadyPhase()
        {
            return bowed && !HasEffect("doesNotReady");
        }

        /// <summary>
        /// Move this card to a specific location
        /// </summary>
        /// <param name="newLocation">Location to move to</param>
        public virtual void MoveTo(string newLocation)
        {
            string oldLocation = location;
            location = newLocation;
            
            // Update game state
            game?.OnCardMoved(this, oldLocation, newLocation);
        }

        /// <summary>
        /// Check if a specific game action is allowed on this card
        /// </summary>
        /// <param name="actionType">Type of action to check</param>
        /// <param name="context">Game action context</param>
        /// <returns>True if action is allowed</returns>
        public virtual bool AllowGameAction(string actionType, object context = null)
        {
            // Basic action allowance checks
            switch (actionType)
            {
                case "discardFromPlay":
                    return IsInPlay() && !HasRestriction("cannotBeDiscarded", context as AbilityContext);
                case "removeFate":
                    return GetTokenCount("fate") > 0;
                case "bow":
                    return !bowed && !HasRestriction("cannotBeBowed", context as AbilityContext);
                case "ready":
                    return bowed;
                default:
                    return !HasRestriction($"cannot{char.ToUpper(actionType[0]) + actionType.Substring(1)}", context as AbilityContext);
            }
        }

        // Property aliases for API compatibility
        public bool Bowed => bowed;
        public bool IsBroken 
        { 
            get => isBroken; 
            set => isBroken = value; 
        }
        public bool Facedown 
        { 
            get => facedown; 
            set => facedown = value; 
        }
        public Player Controller => controller;
        public string Location => location;
        public int Fate => tokens.ContainsKey("fate") ? tokens["fate"] : 0;
        
        // Properties missing from errors
        public bool IsBowed => bowed;
        public Player Owner => owner;
        public int Power => cardData?.military ?? 0; // Using military as power
        public int FateTokens => GetTokenCount("fate");
        public bool HasActionAbilities => actions?.Count > 0;
        public bool IsParticipatingInConflict => inConflict;
        public bool HasBowTriggeredAbilities => reactions?.Count > 0; // Simplified
        
        public void Honor() { /* TODO: Implement honor logic */ }
        public void Dishonor() { /* TODO: Implement dishonor logic */ }
        public void SetDefaultController(Player newController) { controller = newController; }
        public bool CanParticipateInConflict() { return !bowed && IsInPlay(); }

        // Cleanup when destroyed
        protected virtual void OnDestroy()
        {
            // Clear references
            attachments?.Clear();
            childCards?.Clear();

            Debug.Log($"🃏 Card {printedName ?? "Unknown"} destroyed");
        }

        // Missing methods for compilation
        public virtual bool IsAncestral() => HasKeyword("Ancestral");
        public virtual bool IsProvince() => type == "province";
        public virtual bool AllowAttachment(DrawCard attachment) => true;
        public virtual void AddToken(object token) { /* stub */ }
        public virtual void AddAttachment(DrawCard attachment) 
        { 
            if (attachments == null) attachments = new List<BaseCard>();
            attachments.Add(attachment);
        }
        public virtual bool IsDishonored() => HasEffect("dishonored");
        public virtual bool IsHonored() => HasEffect("honored");
        public virtual void RemoveAttachment(DrawCard attachment) 
        {
            if (attachments != null) attachments.Remove(attachment);
        }
        public virtual int PersonalHonor { get; set; } = 0;
        public virtual object personalHonor 
        { 
            get { return PersonalHonor > 0 ? new { card = this, value = PersonalHonor } : null; }
        }
        public virtual void MakeOrdinary() { /* stub */ }
        
        // Additional missing methods for compilation
        public virtual bool IsUnique() => cardData?.unicity ?? false;
        public virtual bool AnotherUniqueInPlay(Player player) => false; // Stub
        public virtual void AddStatModifier(string stat, int modifier) { /* stub */ }
        public virtual int cost => cardData?.fate ?? 0;
        public virtual bool HasDash(string conflictType) => false; // Stub 
        public virtual List<CardAbility> GetPlayActions() => new List<CardAbility>();
        public virtual bool LeavesPlay() => false; // Stub
        public virtual bool IsConflictProvince() => type == "province" && isConflict;
        public virtual bool InConflict() => inConflict;
        public virtual void SetPersonalHonor(int value) => PersonalHonor = value;
        
        /// <summary>
        /// Create an action from this card (stub)
        /// </summary>
        public virtual GameAction CreateAction(object actionProps = null)
        {
            return new SequentialAction();
        }
        
        /// <summary>
        /// Create a triggered ability from this card (stub)
        /// </summary>
        public virtual BaseAbility CreateTriggeredAbility(object abilityProps = null)
        {
            return new BaseAbility();
        }
        
        /// <summary>
        /// Create a triggered ability from this card with specific type (stub)
        /// </summary>
        public virtual BaseAbility CreateTriggeredAbility(string abilityType, object abilityProps = null)
        {
            return new BaseAbility();
        }
        
        /// <summary>
        /// Add an effect to this card
        /// </summary>
        public virtual void AddEffect(object effect)
        {
            // Stub implementation - would integrate with effect engine
        }
        
        /// <summary>
        /// Remove an effect from this card
        /// </summary>
        public virtual void RemoveEffect(object effect)
        {
            // Stub implementation - would integrate with effect engine
        }
        
        /// <summary>
        /// Get all effects on this card
        /// </summary>
        public virtual List<object> Effects => new List<object>(); // Stub
        
        /// <summary>
        /// Add effect to game engine
        /// </summary>
        public virtual object AddEffectToEngine(object effect)
        {
            // Stub implementation - would integrate with effect engine
            return effect;
        }
        
        /// <summary>
        /// Remove effect from game engine
        /// </summary>
        public virtual void RemoveEffectFromEngine(object effectRef)
        {
            // Stub implementation - would integrate with effect engine
        }
        
        /// <summary>
        /// Get the printed faction of this card
        /// </summary>
        public virtual string GetPrintedFaction()
        {
            return printedFaction ?? cardData?.clan ?? "neutral";
        }
        
        /// <summary>
        /// Check if this card has a specific trait
        /// </summary>
        public virtual bool HasTrait(string trait)
        {
            return traits?.Contains(trait) ?? false;
        }
        
        /// <summary>
        /// Check if this card belongs to a specific faction
        /// </summary>
        public virtual bool IsFaction(string faction)
        {
            return GetPrintedFaction() == faction;
        }
        
        /// <summary>
        /// Get the printed cost of this card
        /// </summary>
        public virtual int printedCost => cardData?.fate ?? 0;

        // Missing properties from compilation errors
        public virtual string Name => printedName;
        public virtual string CardId => id;
        public virtual bool CanBeHonored => type == CardTypes.Character && !IsHonored();
        public virtual bool CanBeDishonored => type == CardTypes.Character && !IsDishonored();
        public virtual bool HasAbilities => (actions?.Count > 0) || (reactions?.Count > 0) || (persistentEffects?.Count > 0);
        public virtual bool HasLeavesPlayAbilities => reactions?.Any(r => r.Title.Contains("leaves play")) ?? false;
        public virtual bool HasSpecialAbilities => HasAbilities;
        public virtual int Cost => GetCost();

        /// <summary>
        /// Get abilities with a specific trigger
        /// </summary>
        public virtual List<BaseAbility> GetAbilitiesWithTrigger(string trigger)
        {
            var matchingAbilities = new List<BaseAbility>();
            
            if (reactions != null)
            {
                matchingAbilities.AddRange(reactions.Where(r => r.Title.Contains(trigger)));
            }
            
            return matchingAbilities;
        }
        
        /// <summary>
        /// Check if this card can be bowed
        /// </summary>
        public virtual bool CanBeBowed() 
        {
            return !bowed && location == Locations.PlayArea;
        }
        
        /// <summary>
        /// Check if this card can be bowed with context
        /// </summary>
        public virtual bool CanBeBowed(object context)
        {
            return CanBeBowed();
        }
        
        /// <summary>
        /// Check if this card can be readied
        /// </summary>
        public virtual bool CanBeReadied()
        {
            return bowed && location == Locations.PlayArea;
        }
        
        /// <summary>
        /// Check if this card can be readied with context
        /// </summary>
        public virtual bool CanBeReadied(object context)
        {
            return CanBeReadied();
        }
        
        /// <summary>
        /// Get the fate cost of this card
        /// </summary>
        public virtual int FateCost => Cost;
    }
}
