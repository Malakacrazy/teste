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
        
        private static readonly System.Func<object, bool> DefaultCardMatch = card => true;
        private static readonly System.Func<BaseCard, AbilityContext, bool> DefaultCondition = (card, context) => true;

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
            
            // Initialize Python script if it exists
            if (!string.IsNullOrEmpty(scriptName))
            {
                pythonScript = new PythonCardScript(this, scriptName);
                hasCustomScript = true;
            }

            // Initialize as EffectSource
            base.Initialize(game, printedName);

            // Set up card abilities
            SetupCardAbilities();
            ApplyAttachmentBonus();
            ParseKeywords(data.text);

            Debug.Log($"🃏 Card {printedName} initialized with script: {scriptName}");
        }

        public virtual void Initialize(BaseCard sourceCard)
        {
            // Copy constructor for token creation
            owner = sourceCard.owner;
            controller = sourceCard.controller;
            game = sourceCard.game;
            cardData = sourceCard.cardData;
            id = sourceCard.id;
            printedName = sourceCard.printedName;
            printedType = sourceCard.printedType;
            type = sourceCard.type;
            traits = new List<string>(sourceCard.traits);
            printedFaction = sourceCard.printedFaction;
            
            // Initialize as EffectSource
            base.Initialize(game, printedName);
            
            Debug.Log($"🃏 Token card {printedName} created from {sourceCard.printedName}");
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

        // Properties with effect consideration
        public virtual string Name
        {
            get
            {
                var copyEffect = MostRecentEffect(EffectNames.CopyCharacter);
                return copyEffect != null ? ((dynamic)copyEffect).printedName : printedName;
            }
            set { printedName = value; }
        }

        public virtual List<CardAction> Actions
        {
            get
            {
                var actions = abilities.actions.ToList();

                if (AnyEffect(EffectNames.CopyCharacter))
                {
                    var mostRecentEffect = GetRawEffects()
                        .Where(effect => effect.GetType().GetProperty("type")?.GetValue(effect)?.ToString() == EffectNames.CopyCharacter)
                        .LastOrDefault();
                    if (mostRecentEffect != null)
                    {
                        var effectValue = mostRecentEffect.GetType().GetProperty("value")?.GetValue(mostRecentEffect);
                        if (effectValue != null)
                        {
                            actions = ((dynamic)effectValue).GetActions(this);
                        }
                    }
                }

                var effectActions = GetEffects(EffectNames.GainAbility)
                    .Where(ability => ((dynamic)ability).abilityType == AbilityTypes.Action)
                    .Cast<CardAction>()
                    .ToList();

                return actions.Concat(effectActions).ToList();
            }
        }

        public virtual List<TriggeredAbility> Reactions
        {
            get
            {
                var triggeredAbilityTypes = new[]
                {
                    AbilityTypes.ForcedInterrupt,
                    AbilityTypes.ForcedReaction,
                    AbilityTypes.Interrupt,
                    AbilityTypes.Reaction,
                    AbilityTypes.WouldInterrupt
                };

                var reactions = abilities.reactions.ToList();

                if (AnyEffect(EffectNames.CopyCharacter))
                {
                    var mostRecentEffect = GetRawEffects()
                        .Where(effect => effect.GetType().GetProperty("type")?.GetValue(effect)?.ToString() == EffectNames.CopyCharacter)
                        .LastOrDefault();
                    if (mostRecentEffect != null)
                    {
                        var effectValue = mostRecentEffect.GetType().GetProperty("value")?.GetValue(mostRecentEffect);
                        if (effectValue != null)
                        {
                            reactions = ((dynamic)effectValue).GetReactions(this);
                        }
                    }
                }

                var effectReactions = GetEffects(EffectNames.GainAbility)
                    .Where(ability => triggeredAbilityTypes.Any(type => type == ((dynamic)ability).abilityType))
                    .Cast<TriggeredAbility>()
                    .ToList();

                return reactions.Concat(effectReactions).ToList();
            }
        }

        public virtual List<PersistentEffect> PersistentEffects
        {
            get
            {
                var gainedPersistentEffects = GetEffects(EffectNames.GainAbility)
                    .Where(ability => ((dynamic)ability).abilityType == AbilityTypes.Persistent)
                    .Cast<PersistentEffect>()
                    .ToList();

                if (AnyEffect(EffectNames.CopyCharacter))
                {
                    var mostRecentEffect = GetRawEffects()
                        .Where(effect => effect.GetType().GetProperty("type")?.GetValue(effect)?.ToString() == EffectNames.CopyCharacter)
                        .LastOrDefault();
                    if (mostRecentEffect != null)
                    {
                        var effectValue = mostRecentEffect.GetType().GetProperty("value")?.GetValue(mostRecentEffect);
                        if (effectValue != null)
                        {
                            var persistentEffects = ((dynamic)effectValue).GetPersistentEffects();
                            if (persistentEffects is IEnumerable<PersistentEffect> enumerableEffects)
                            {
                                return gainedPersistentEffects.Concat(enumerableEffects).ToList();
                            }
                            else if (persistentEffects is List<PersistentEffect> listEffects)
                            {
                                return gainedPersistentEffects.Concat(listEffects).ToList();
                            }
                            else
                            {
                                // Fallback: try to cast as IEnumerable<object> and filter
                                var objEnumerable = persistentEffects as System.Collections.IEnumerable;
                                if (objEnumerable != null)
                                {
                                    var convertedEffects = objEnumerable.Cast<object>()
                                        .Where(e => e is PersistentEffect)
                                        .Cast<PersistentEffect>();
                                    return gainedPersistentEffects.Concat(convertedEffects).ToList();
                                }
                            }
                        }
                    }
                }

                return IsBlank() ? gainedPersistentEffects :
                    abilities.persistentEffects.Concat(gainedPersistentEffects).ToList();
            }
        }

        // Card ability setup (to be overridden by specific cards)
        protected virtual void SetupCardAbilities()
        {
            // Base implementation - specific cards will override this
            // This is where card-specific abilities are defined
        }

        // Ability creation methods
        public void Action(ActionProperties properties)
        {
            abilities.actions.Add(CreateAction(properties));
        }

        public virtual CardAction CreateAction(ActionProperties properties)
        {
            return new CardAction(game, this, properties);
        }

        public void TriggeredAbility(string abilityType, TriggeredAbilityProperties properties)
        {
            abilities.reactions.Add(CreateTriggeredAbility(abilityType, properties));
        }

        public virtual TriggeredAbility CreateTriggeredAbility(string abilityType, TriggeredAbilityProperties properties)
        {
            return new TriggeredAbility(game, this, abilityType, properties);
        }

        public void Reaction(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.Reaction, properties);
        }

        public void ForcedReaction(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.ForcedReaction, properties);
        }

        public void WouldInterrupt(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.WouldInterrupt, properties);
        }

        public void Interrupt(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.Interrupt, properties);
        }

        public void ForcedInterrupt(TriggeredAbilityProperties properties)
        {
            TriggeredAbility(AbilityTypes.ForcedInterrupt, properties);
        }

        public void PlayAction(CustomPlayActionProperties properties)
        {
            abilities.playActions.Add(new CustomPlayAction(properties));
        }

        public void PersistentEffect(PersistentEffectProperties properties)
        {
            var allowedLocations = new[]
            {
                Locations.Any, Locations.ConflictDiscardPile,
                Locations.PlayArea, Locations.Provinces
            };

            var defaultLocationForType = new Dictionary<string, string>
            {
                {"province", Locations.Provinces},
                {"holding", Locations.Provinces},
                {"stronghold", Locations.Provinces}
            };

            string location = properties.location ??
                             defaultLocationForType.GetValueOrDefault(GetCardType(), Locations.PlayArea);

            if (!allowedLocations.Contains(location))
            {
                throw new System.Exception($"'{location}' is not a supported effect location.");
            }

            var effect = new PersistentEffect
            {
                duration = Durations.Persistent,
                location = location,
                effect = properties.effect,
                condition = properties.condition,
                match = properties.match,
                targetController = properties.targetController
            };

            abilities.persistentEffects.Add(effect);
        }

        public void AttachmentConditions(AttachmentConditionProperties properties)
        {
            var effects = new List<object>();

            if (properties.limit > 0)
            {
                effects.Add(Effects.AttachmentLimit(properties.limit));
            }

            if (properties.myControl)
            {
                effects.Add(Effects.AttachmentMyControlOnly());
            }

            if (properties.unique)
            {
                effects.Add(Effects.AttachmentUniqueRestriction());
            }

            if (properties.faction != null)
            {
                var factions = properties.faction is List<string> ?
                    (List<string>)properties.faction : new List<string> { (string)properties.faction };
                effects.Add(Effects.AttachmentFactionRestriction(factions));
            }

            if (properties.trait != null)
            {
                var traits = properties.trait is List<string> ?
                    (List<string>)properties.trait : new List<string> { (string)properties.trait };
                effects.Add(Effects.AttachmentTraitRestriction(traits));
            }

            if (properties.limitTrait != null)
            {
                var traitLimits = properties.limitTrait is List<Dictionary<string, int>> ?
                    (List<Dictionary<string, int>>)properties.limitTrait :
                    new List<Dictionary<string, int>> { (Dictionary<string, int>)properties.limitTrait };

                foreach (var traitLimit in traitLimits)
                {
                    foreach (var kvp in traitLimit)
                    {
                        effects.Add(Effects.AttachmentRestrictTraitAmount(new Dictionary<string, int> { { kvp.Key, kvp.Value } }));
                    }
                }
            }

            if (effects.Count > 0)
            {
                PersistentEffect(new PersistentEffectProperties
                {
                    location = Locations.Any,
                    effect = effects
                });
            }
        }

        public void Composure(PersistentEffectProperties properties)
        {
            // Create a condition function that checks for composure
            Func<AbilityContext, bool> composureCondition = (context) => 
            {
                if (context.player != null)
                    return ((Player)context.player).HasComposure();
                return false;
            };
            
            var composureProperties = new PersistentEffectProperties
            {
                condition = composureCondition,
                effect = properties.effect,
                match = properties.match,
                targetController = properties.targetController,
                location = properties.location
            };

            PersistentEffect(composureProperties);
        }

        // Trait and faction management
        public bool HasTrait(string trait)
        {
            trait = trait.ToLower();
            return GetTraits().Contains(trait) || GetEffects(EffectNames.AddTrait).Contains(trait);
        }

        public List<string> GetTraits()
        {
            var copyEffect = MostRecentEffect(EffectNames.CopyCharacter);
            var cardTraits = copyEffect != null ?
                ((dynamic)copyEffect).traits :
                (GetEffects(EffectNames.Blank).Any() ? new List<string>() : traits);

            var additionalTraits = GetEffects(EffectNames.AddTrait).Cast<string>().ToList();
            return cardTraits.Concat(additionalTraits).Distinct().ToList();
        }

        public bool IsFaction(string faction)
        {
            faction = faction.ToLower();
            if (faction == "neutral")
            {
                return printedFaction == faction && !AnyEffect(EffectNames.AddFaction);
            }
            return printedFaction == faction || GetEffects(EffectNames.AddFaction).Contains(faction);
        }

        // Location and state checks
        public bool IsInProvince()
        {
            var provinceLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree,
                Locations.ProvinceFour, Locations.StrongholdProvince
            };
            return provinceLocations.Contains(location);
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

        public void ApplyAnyLocationPersistentEffects()
        {
            foreach (var effect in PersistentEffects.Where(e => e.location == Locations.Any))
            {
                effect.reference = AddEffectToEngine(effect);
            }
        }

        // Card lifecycle events
        public virtual void LeavesPlay()
        {
            tokens.Clear();

            foreach (var action in abilities.actions)
            {
                action.limit?.Reset();
            }

            foreach (var reaction in abilities.reactions)
            {
                reaction.limit?.Reset();
            }

            controller = owner;
            inConflict = false;

            // Execute Python script for leaving play
            ExecutePythonScript("on_leave_play");
        }

        public virtual void MoveTo(string targetLocation)
        {
            string originalLocation = location;
            location = targetLocation;

            var visibleLocations = new[]
            {
                Locations.PlayArea, Locations.ConflictDiscardPile,
                Locations.DynastyDiscardPile, Locations.Hand
            };

            if (visibleLocations.Contains(targetLocation))
            {
                facedown = false;
            }

            if (originalLocation != targetLocation)
            {
                UpdateAbilityEvents(originalLocation, targetLocation);
                UpdateEffects(originalLocation, targetLocation);

                game.EmitEvent(EventNames.OnCardMoved, new Dictionary<string, object>
                {
                    {"card", this},
                    {"originalLocation", originalLocation},
                    {"newLocation", targetLocation}
                });

                // Execute Python script for movement
                ExecutePythonScript("on_move", originalLocation, targetLocation);
            }
        }

        public void UpdateAbilityEvents(string from, string to)
        {
            foreach (var reaction in Reactions)
            {
                reaction.limit?.Reset();

                if (type == CardTypes.Event)
                {
                    if (to == Locations.ConflictDeck ||
                        controller.IsCardInPlayableLocation(this, null) ||
                        (controller.opponent?.IsCardInPlayableLocation(this, null) ?? false))
                    {
                        reaction.RegisterEvents();
                    }
                    else
                    {
                        reaction.UnregisterEvents();
                    }
                }
                else if (reaction.location.Contains(to) && !reaction.location.Contains(from))
                {
                    reaction.RegisterEvents();
                }
                else if (!reaction.location.Contains(to) && reaction.location.Contains(from))
                {
                    reaction.UnregisterEvents();
                }
            }

            foreach (var action in abilities.actions)
            {
                action.limit?.Reset();
            }
        }

        public void UpdateEffects(string from, string to)
        {
            var activeLocations = new Dictionary<string, string[]>
            {
                {"conflict discard pile", new[] { Locations.ConflictDiscardPile }},
                {"play area", new[] { Locations.PlayArea }},
                {"province", new[] {
                    Locations.ProvinceOne, Locations.ProvinceTwo,
                    Locations.ProvinceThree, Locations.ProvinceFour,
                    Locations.StrongholdProvince
                }}
            };

            if (!activeLocations[Locations.Provinces].Contains(from) ||
                !activeLocations[Locations.Provinces].Contains(to))
            {
                RemoveLastingEffects();
            }

            foreach (var effect in PersistentEffects.Where(e => e.location != Locations.Any))
            {
                if (activeLocations.ContainsKey(effect.location))
                {
                    var locations = activeLocations[effect.location];

                    if (locations.Contains(to) && !locations.Contains(from))
                    {
                        effect.reference = AddEffectToEngine(effect);
                    }
                    else if (!locations.Contains(to) && locations.Contains(from))
                    {
                        RemoveEffectFromEngine(effect.reference);
                        effect.reference = null;
                    }
                }
            }
        }

        public void UpdateEffectContexts()
        {
            foreach (var effect in PersistentEffects.Where(e => e.reference != null))
            {
                foreach (var e in (List<object>)effect.reference)
                {
                    ((dynamic)e).RefreshContext();
                }
            }
        }

        // Ability triggers and restrictions
        public bool CanTriggerAbilities(AbilityContext context)
        {
            return !facedown && CheckRestrictions("triggerAbilities", context);
        }

        public bool CanInitiateKeywords(AbilityContext context)
        {
            return !facedown && CheckRestrictions("initiateKeywords", context);
        }

        public int GetModifiedLimitMax(Player player, CardAbility ability, int max)
        {
            var effects = GetRawEffects()
                .Where(effect => effect.GetType().GetProperty("type")?.GetValue(effect)?.ToString() == EffectNames.IncreaseLimitOnAbilities);

            return effects.Aggregate(max, (total, effect) =>
            {
                try
                {
                    var getValueMethod = effect.GetType().GetMethod("GetValue");
                    var value = getValueMethod?.Invoke(effect, new object[] { this });
                    if ((value is bool && (bool)value) || value == ability)
                    {
                        var contextProperty = effect.GetType().GetProperty("context");
                        var contextValue = contextProperty?.GetValue(effect);
                        if (contextValue != null)
                        {
                            var playerProperty = contextValue.GetType().GetProperty("player");
                            var effectPlayer = playerProperty?.GetValue(contextValue) as Player;
                            if (effectPlayer == player)
                                return total + 1;
                        }
                    }
                }
                catch
                {
                    // Ignore reflection errors
                }
                return total;
            });
        }

        // Menu system
        public List<CardMenuOption> GetMenu()
        {
            var cardMenu = new List<CardMenuOption>();

            var validLocations = new[]
            {
                Locations.ProvinceOne, Locations.ProvinceTwo, Locations.ProvinceThree,
                Locations.ProvinceFour, Locations.StrongholdProvince, Locations.PlayArea
            };

            if (menu.Count == 0 || !game.manualMode || !validLocations.Contains(location))
            {
                return null;
            }

            if (facedown)
            {
                return new List<CardMenuOption>
                {
                    new CardMenuOption { command = "reveal", text = "Reveal" }
                };
            }

            cardMenu.Add(new CardMenuOption { command = "click", text = "Select Card" });

            if (location == Locations.PlayArea || isProvince || isStronghold)
            {
                cardMenu.AddRange(menu);
            }

            return cardMenu;
        }

        // Combat state checks
        public virtual bool IsConflictProvince()
        {
            return false;
        }

        public bool IsAttacking()
        {
            return game.currentConflict?.IsAttacking(this) ?? false;
        }

        public bool IsDefending()
        {
            return game.currentConflict?.IsDefending(this) ?? false;
        }

        public bool IsParticipating()
        {
            return game.currentConflict?.IsParticipating(this) ?? false;
        }

        // Card properties
        public bool IsUnique()
        {
            return cardData.unicity;
        }

        public bool IsBlank()
        {
            return AnyEffect(EffectNames.Blank) || AnyEffect(EffectNames.CopyCharacter);
        }

        public string GetPrintedFaction()
        {
            return cardData.clan;
        }

        public virtual string GetCardType()
        {
            return type;
        }

        public virtual int GetCost()
        {
            return cardData.fate;
        }

        public virtual bool CheckRestrictions(string actionType, AbilityContext context)
        {
            return base.CheckRestrictions(actionType, context) &&
                   controller.CheckRestrictions(actionType, context);
        }

        // Token management
        public void AddToken(string tokenType, int number = 1)
        {
            if (!tokens.ContainsKey(tokenType))
            {
                tokens[tokenType] = 0;
            }
            tokens[tokenType] += number;
        }

        public int GetTokenCount(string tokenType)
        {
            return tokens.GetValueOrDefault(tokenType, 0);
        }

        public bool HasToken(string tokenType)
        {
            return tokens.ContainsKey(tokenType) && tokens[tokenType] > 0;
        }

        public void RemoveToken(string tokenType, int number)
        {
            if (!tokens.ContainsKey(tokenType)) return;

            tokens[tokenType] -= number;

            if (tokens[tokenType] <= 0)
            {
                tokens.Remove(tokenType);
            }
        }

        // Ability getters
        public List<CardAction> GetActions()
        {
            return Actions.ToList();
        }

        public List<TriggeredAbility> GetReactions()
        {
            return Reactions.ToList();
        }

        public virtual int GetProvinceStrengthBonus()
        {
            return 0;
        }

        public bool ReadiesDuringReadyPhase()
        {
            return !AnyEffect(EffectNames.DoesNotReady);
        }

        public bool HideWhenFacedown()
        {
            return !AnyEffect(EffectNames.CanBeSeenWhenFacedown);
        }

        public virtual Dictionary<string, object> CreateSnapshot()
        {
            return new Dictionary<string, object>();
        }

        // Keyword parsing
        public void ParseKeywords(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split('\n');
            var potentialKeywords = new List<string>();

            foreach (var line in lines)
            {
                    var trimmedLine = line.TrimEnd('.');
                var keywords = trimmedLine.Split(new[] { ". " }, StringSplitOptions.None);
                foreach (var keyword in keywords)
                {
                    potentialKeywords.Add(keyword);
                }
            }

            printedKeywords.Clear();
            allowedAttachmentTraits.Clear();
            disguisedKeywordTraits.Clear();

            foreach (var keyword in potentialKeywords)
            {
                if (ValidKeywords.Contains(keyword))
                {
                    printedKeywords.Add(keyword);
                }
                else if (keyword.StartsWith("disguised "))
                {
                    disguisedKeywordTraits.Add(keyword.Replace("disguised ", ""));
                }
                else if (keyword.StartsWith("no attachments except"))
                {
                    var traits = keyword.Replace("no attachments except ", "");
                    allowedAttachmentTraits = traits.Split(new[] { " or " }, StringSplitOptions.None).ToList();
                }
                else if (keyword.StartsWith("no attachments"))
                {
                    allowedAttachmentTraits = new List<string> { "none" };
                }
            }

            // Apply keyword effects
            foreach (var keyword in printedKeywords)
            {
                PersistentEffect(new PersistentEffectProperties
                {
                    effect = new List<object> { Effects.AddKeyword(keyword) }
                });
            }
        }

        // Attachment bonus application
        public void ApplyAttachmentBonus()
        {
            int militaryBonus = cardData.military_bonus;
            if (militaryBonus != 0)
            {
                Func<BaseCard, bool> matchParent = (card) => card == parent;
                PersistentEffect(new PersistentEffectProperties
                {
                    match = matchParent,
                    targetController = Players.Any,
                    effect = new List<object> { Effects.AttachmentMilitarySkillModifier(militaryBonus) }
                });
            }

            int politicalBonus = cardData.political_bonus;
            if (politicalBonus != 0)
            {
                Func<BaseCard, bool> matchParent = (card) => card == parent;
                PersistentEffect(new PersistentEffectProperties
                {
                    match = matchParent,
                    targetController = Players.Any,
                    effect = new List<object> { Effects.AttachmentPoliticalSkillModifier(politicalBonus) }
                });
            }
        }

        // IronPython Integration
        public void ExecutePythonScript(string eventType, params object[] parameters)
        {
            if (hasCustomScript && !string.IsNullOrEmpty(scriptName))
            {
                var allParams = new List<object> { this }.Concat(parameters).ToArray();
                game.ExecuteCardScript(scriptName, eventType, allParams);
            }
        }

        public virtual void OnCardPlayed()
        {
            ExecutePythonScript("on_card_played", controller, new Dictionary<string, object>());
        }

        public virtual void OnEnterPlay()
        {
            ExecutePythonScript("on_enter_play", controller);
        }

        public virtual void OnConflict(Conflict conflict)
        {
            ExecutePythonScript("on_conflict", conflict);
        }

        public virtual bool CanTrigger(string eventName, Dictionary<string, object> eventData)
        {
            if (hasCustomScript)
            {
                var result = game.ExecuteCardScript(scriptName, "can_trigger", this, eventName, eventData);
                return result is bool ? (bool)result : false;
            }
            return false;
        }

        public virtual void OnTrigger(string eventName, Dictionary<string, object> eventData)
        {
            ExecutePythonScript("on_trigger", eventName, eventData);
        }

        // Reset for conflict
        public virtual void ResetForConflict()
        {
            // Override in derived classes if needed
        }

        // Attachment system
        public void CheckForIllegalAttachments()
        {
            var context = game.GetFrameworkContext(controller);
            var illegalAttachments = attachments.Where(attachment =>
                !AllowAttachment(attachment) ||
                !attachment.CanAttach(this, context, false)).ToList();

            // Check restricted attachment limits and other restrictions
            foreach (var effectCard in GetEffects(EffectNames.CannotHaveOtherRestrictedAttachments))
            {
                illegalAttachments.AddRange(attachments.Where(card =>
                    card.IsRestricted() && card != effectCard));
            }

            // Handle attachment limits
            foreach (var card in attachments.Where(card => card.AnyEffect(EffectNames.AttachmentLimit)))
            {
                var limit = card.GetEffects(EffectNames.AttachmentLimit).Cast<int>().Max();
                var matchingAttachments = attachments.Where(attachment => attachment.id == card.id).ToList();
                if (matchingAttachments.Count > limit)
                {
                    illegalAttachments.AddRange(matchingAttachments.Skip(limit));
                }
            }

            illegalAttachments = illegalAttachments.Distinct().ToList();

            // Handle too many restricted attachments
            var restrictedAttachments = attachments.Where(card => card.IsRestricted()).ToList();
            if (restrictedAttachments.Count > 2)
            {
                game.PromptForSelect(controller, new SelectCardPromptProperties
                {
                    activePromptTitle = "Choose an attachment to discard",
                    waitingPromptTitle = "Waiting for opponent to choose an attachment to discard",
                    controller = Players.Self,
                    cardCondition = new Func<BaseCard, bool>((card) => card.parent == this && card.IsRestricted()),
                    onSelect = new Func<Player, BaseCard, bool>((player, card) =>
                    {
                        game.AddMessage("{0} discards {1} from {2} due to too many Restricted attachments",
                                       player, card, card.parent);

                        if (!illegalAttachments.Contains(card))
                        {
                            illegalAttachments.Add(card);
                        }

                        game.ApplyGameAction(context, new Dictionary<string, object>
                        {
                            {"discardFromPlay", illegalAttachments}
                        });
                        return true;
                    }),
                    source = "Too many Restricted attachments"
                });
            }
            else if (illegalAttachments.Count > 0)
            {
                game.AddMessage("{0} {1} discarded from {2} as {3} {1} no longer legally attached",
                               illegalAttachments,
                               illegalAttachments.Count > 1 ? "are" : "is",
                               this,
                               illegalAttachments.Count > 1 ? "they" : "it");

                game.ApplyGameAction(context, new Dictionary<string, object>
                {
                    {"discardFromPlay", illegalAttachments}
                });
            }
        }

        public virtual bool MustAttachToRing()
        {
            return false;
        }

        public virtual bool CanPlayOn(BaseCard card)
        {
            return true;
        }

        public bool AllowAttachment(BaseCard attachment)
        {
            if (allowedAttachmentTraits.Any(trait => attachment.HasTrait(trait)))
            {
                return true;
            }

            return IsBlank() || allowedAttachmentTraits.Count == 0;
        }

        public void WhileAttached(WhileAttachedProperties properties)
        {
            // Create condition function
            Func<AbilityContext, bool> condition = null;
            if (properties.condition != null)
            {
                condition = properties.condition as Func<AbilityContext, bool>;
            }
            if (condition == null)
            {
                condition = (context) => true;
            }
            
            // Create match function that checks if card is parent
            Func<BaseCard, bool> matchFunction;
            if (properties.match != null)
            {
                var originalMatch = properties.match;
                matchFunction = (card) => card == parent && originalMatch(card);
            }
            else
            {
                matchFunction = (card) => card == parent;
            }
            
            PersistentEffect(new PersistentEffectProperties
            {
                condition = condition,
                match = matchFunction,
                targetController = "any",
                effect = properties.effect
            });
        }

        public bool CanAttach(BaseCard parent, AbilityContext context, bool ignoreType = false)
        {
            if (parent == null || parent.GetCardType() != CardTypes.Character ||
                (!ignoreType && GetCardType() != CardTypes.Attachment))
            {
                return false;
            }

            if (AnyEffect(EffectNames.AttachmentMyControlOnly) &&
                context.player != parent.controller && controller != parent.controller)
            {
                return false;
            }

            if (AnyEffect(EffectNames.AttachmentUniqueRestriction) && !parent.IsUnique())
            {
                return false;
            }

            var factionRestrictions = GetEffects(EffectNames.AttachmentFactionRestriction);
            if (factionRestrictions.Any(factions =>
                !((List<string>)factions).Any(faction => parent.IsFaction(faction))))
            {
                return false;
            }

            var traitRestrictions = GetEffects(EffectNames.AttachmentTraitRestriction);
            if (traitRestrictions.Any(traits =>
                !((List<string>)traits).Any(trait => parent.HasTrait(trait))))
            {
                return false;
            }

            return true;
        }

        public List<object> GetPlayActions()
        {
            var actions = new List<object>();

            if (type == CardTypes.Event)
            {
                return GetActions().Cast<object>().ToList();
            }

            actions = abilities.playActions.Cast<object>().ToList();

            if (type == CardTypes.Character)
            {
                if (disguisedKeywordTraits.Count > 0)
                {
                    actions.Add(new PlayDisguisedCharacterAction(this));
                }

                if (isDynasty)
                {
                    actions.Add(new DynastyCardAction(this));
                }
                else
                {
                    actions.Add(new PlayCharacterAction(this));
                }
            }
            else if (type == CardTypes.Attachment && MustAttachToRing())
            {
                actions.Add(new PlayAttachmentOnRingAction(this));
            }
            else if (type == CardTypes.Attachment)
            {
                actions.Add(new PlayAttachmentAction(this));
            }

            return actions;
        }

        public void RemoveAttachment(BaseCard attachment)
        {
            attachments = attachments.Where(card => card.GetInstanceID() != attachment.GetInstanceID()).ToList();
        }

        public void AddChildCard(BaseCard card, string location)
        {
            childCards.Add(card);
            controller.MoveCard(card, location);
        }

        public void RemoveChildCard(BaseCard card, string location)
        {
            if (card == null) return;

            childCards.Remove(card);
            controller.MoveCard(card, location);
        }

        // Derived card types can override these methods
        public virtual bool IsRestricted()
        {
            return HasTrait("restricted") || printedKeywords.Contains("restricted");
        }
        
        public bool HasKeyword(string keyword)
        {
            return printedKeywords.Contains(keyword) || HasTrait(keyword);
        }

        public virtual Player GetModifiedController()
        {
            // Check for control-changing effects
            var controlEffects = GetEffects(EffectNames.TakeControl);
            if (controlEffects.Any())
            {
                return (Player)controlEffects.Last();
            }
            return controller;
        }

        public virtual void SetDefaultController(Player player)
        {
            controller = player;
        }

        public virtual bool AllowGameAction(string actionType)
        {
            // Check if this card allows the specified game action
            return !GetEffects($"cannot{actionType}").Any();
        }

        public virtual int GetContributionToImperialFavor()
        {
            // Override in character cards to return glory value
            return 0;
        }

        // Summary methods for UI
        public new Dictionary<string, object> GetShortSummaryForControls(Player activePlayer)
        {
            if (facedown && (activePlayer != controller || HideWhenFacedown()))
            {
                return new Dictionary<string, object>
                {
                    {"facedown", true},
                    {"isDynasty", isDynasty},
                    {"isConflict", isConflict}
                };
            }

            return new Dictionary<string, object>
            {
                {"controller", controller.name},
                {"name", Name},
                {"type", type},
                {"id", id}
            };
        }

        public virtual Dictionary<string, object> GetSummary(Player activePlayer, bool hideWhenFaceup = false)
        {
            bool isActivePlayer = activePlayer == controller;
            var selectionState = activePlayer.GetCardSelectionState(this);

            // Handle facedown or hidden cards
            if (isActivePlayer ? (facedown && HideWhenFacedown()) :
                (facedown || hideWhenFaceup || AnyEffect(EffectNames.HideWhenFaceUp)))
            {
                var hiddenState = new Dictionary<string, object>
                {
                    {"controller", controller.GetShortSummary()},
                    {"facedown", true},
                    {"inConflict", inConflict},
                    {"location", location}
                };

                foreach (var kvp in selectionState)
                {
                    hiddenState[kvp.Key] = kvp.Value;
                }

                return hiddenState;
            }

            var state = new Dictionary<string, object>
            {
                {"id", cardData.id},
                {"controlled", owner != controller},
                {"inConflict", inConflict},
                {"facedown", facedown},
                {"location", location},
                {"menu", GetMenu()},
                {"name", cardData.name},
                {"popupMenuText", popupMenuText},
                {"showPopup", showPopup},
                {"tokens", tokens},
                {"type", GetCardType()},
                {"uuid", GetInstanceID().ToString()},
                {"selected", selected},
                {"traits", GetTraits()},
                {"faction", printedFaction},
                {"cost", GetCost()},
                {"scriptName", scriptName},
                {"hasCustomScript", hasCustomScript}
            };

            foreach (var kvp in selectionState)
            {
                state[kvp.Key] = kvp.Value;
            }

            return state;
        }

        // Cleanup when destroyed
        protected virtual void OnDestroy()
        {
            // Clean up any remaining effects
            RemoveLastingEffects();

            // Clear references
            attachments.Clear();
            childCards.Clear();

            Debug.Log($"🃏 Card {printedName} destroyed");
        }
    }


}