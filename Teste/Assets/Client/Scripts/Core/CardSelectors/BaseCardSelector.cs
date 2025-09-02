using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for all card selectors.
    /// Provides common functionality for card selection and validation.
    /// C# conversion of the original JavaScript BaseCardSelector with full feature parity.
    /// </summary>
    [Serializable]
    public abstract class BaseCardSelector
    {
        #region Fields
        
        [Header("Selection Parameters")]
        public int numCards = 1;
        public bool optional = false;
        public bool ordered = false;
        public bool multiSelect = false;
        public bool checkTarget = false;
        
        [Header("Filtering")]
        public string controller = Players.Any;
        public List<string> location = new List<string>();
        public List<string> cardType = new List<string>();
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        
        [Header("Display")]
        public string activePromptTitle;
        
        // Protected fields
        protected CardSelectorProperties properties;
        
        #endregion
        
        #region Constructor
        
        protected BaseCardSelector(CardSelectorProperties props)
        {
            properties = props ?? new CardSelectorProperties();
            InitializeFromProperties();
        }
        
        /// <summary>
        /// Initialize selector fields from properties
        /// </summary>
        private void InitializeFromProperties()
        {
            cardCondition = properties.cardCondition ?? ((card, context) => true);
            cardType = BuildCardTypeList(properties.cardType);
            optional = properties.optional;
            location = BuildLocationList(properties.location);
            controller = properties.controller;
            checkTarget = properties.targets;
            ordered = properties.ordered;
            multiSelect = properties.multiSelect;
            activePromptTitle = properties.activePromptTitle;
        }
        
        /// <summary>
        /// Build card type list from various input formats
        /// </summary>
        private List<string> BuildCardTypeList(List<string> inputCardType)
        {
            if (inputCardType == null || inputCardType.Count == 0)
            {
                return new List<string> { CardTypes.Character }; // Default fallback
            }
            
            return new List<string>(inputCardType);
        }
        
        /// <summary>
        /// Build location list and expand province shortcuts
        /// </summary>
        private List<string> BuildLocationList(List<string> inputLocation)
        {
            var locationList = inputLocation?.Count > 0 ? new List<string>(inputLocation) : new List<string> { Locations.PlayArea };
            
            // Handle province expansion (matching JavaScript logic)
            var provincesIndex = locationList.IndexOf(Locations.Provinces);
            if (provincesIndex >= 0)
            {
                locationList.RemoveAt(provincesIndex);
                locationList.AddRange(new[]
                {
                    Locations.ProvinceOne,
                    Locations.ProvinceTwo,
                    Locations.ProvinceThree,
                    Locations.ProvinceFour,
                    Locations.StrongholdProvince
                });
            }
            
            return locationList;
        }
        
        #endregion
        
        #region Abstract Methods
        
        /// <summary>
        /// Get the default prompt title for this selector
        /// </summary>
        public virtual string DefaultActivePromptTitle(AbilityContext context = null)
        {
            return activePromptTitle ?? "Choose cards";
        }
        
        /// <summary>
        /// Check if the selector has reached its selection limit
        /// </summary>
        public abstract bool HasReachedLimit(List<BaseCard> selectedCards, AbilityContext context = null);
        
        /// <summary>
        /// Check if adding this card would exceed the selection limit
        /// </summary>
        public abstract bool WouldExceedLimit(List<BaseCard> selectedCards, BaseCard card);
        
        /// <summary>
        /// Format the selected cards parameter for the callback
        /// </summary>
        public virtual object FormatSelectParam(List<BaseCard> selectedCards)
        {
            return selectedCards ?? new List<BaseCard>();
        }
        
        /// <summary>
        /// Check if selection should automatically fire when conditions are met
        /// </summary>
        public virtual bool AutomaticFireOnSelect(AbilityContext context = null)
        {
            return false;
        }
        
        #endregion
        
        #region Core Targeting Logic (Matching JavaScript Implementation)
        
        /// <summary>
        /// Find all possible cards that could be targeted (before filtering)
        /// Direct port of JavaScript findPossibleCards method
        /// </summary>
        public virtual List<BaseCard> FindPossibleCards(AbilityContext context)
        {
            // Handle "Any" location case
            if (location.Contains(Locations.Any))
            {
                if (controller == Players.Self)
                {
                    return context.game.GetAllCards().Where(card => card.controller == context.player).ToList();
                }
                else if (controller == Players.Opponent)
                {
                    return context.game.GetAllCards().Where(card => card.controller == context.player.Opponent).ToList();
                }
                return context.game.GetAllCards().ToList();
            }
            
            // Collect all attachments (matching JavaScript logic)
            var attachments = new List<BaseCard>();
            
            // Player's cards in play attachments
            foreach (var card in context.player.GetCardsInPlay())
            {
                attachments.AddRange(card.attachments);
            }
            
            // Province attachments (both players)
            attachments.AddRange(GetAllProvinceAttachments(context));
            
            // Ring attachments (if rings exist)
            attachments.AddRange(GetAllRingAttachments(context));
            
            // Opponent's cards in play attachments
            if (context.player.Opponent != null)
            {
                foreach (var card in context.player.Opponent.GetCardsInPlay())
                {
                    attachments.AddRange(card.attachments);
                }
            }
            
            var possibleCards = new List<BaseCard>();
            
            // Add player's cards if not restricted to opponent only
            if (controller != Players.Opponent)
            {
                possibleCards.AddRange(GetCardsFromLocations(context.player, attachments, context));
            }
            
            // Add opponent's cards if not restricted to self only
            if (controller != Players.Self && context.player.Opponent != null)
            {
                possibleCards.AddRange(GetCardsFromLocations(context.player.Opponent, attachments, context));
            }
            
            return possibleCards;
        }
        
        /// <summary>
        /// Get all province attachments from both players
        /// </summary>
        private List<BaseCard> GetAllProvinceAttachments(AbilityContext context)
        {
            var allProvinceAttachments = new List<BaseCard>();
            
            // Player's provinces
            foreach (var province in context.player.GetProvinces())
            {
                allProvinceAttachments.AddRange(province.attachments);
            }
            
            // Opponent's provinces
            if (context.player.Opponent != null)
            {
                foreach (var province in context.player.Opponent.GetProvinces())
                {
                    allProvinceAttachments.AddRange(province.attachments);
                }
            }
            
            return allProvinceAttachments;
        }
        
        /// <summary>
        /// Get all ring attachments
        /// </summary>
        private List<BaseCard> GetAllRingAttachments(AbilityContext context)
        {
            var ringAttachments = new List<BaseCard>();
            
            if (context.game.rings != null)
            {
                foreach (var ring in context.game.rings.Values)
                {
                    ringAttachments.AddRange(ring.attachments);
                }
            }
            
            return ringAttachments;
        }
        
        /// <summary>
        /// Get cards from specified locations for a player
        /// </summary>
        private List<BaseCard> GetCardsFromLocations(Player player, List<BaseCard> attachments, AbilityContext context)
        {
            var cards = new List<BaseCard>();
            
            foreach (var loc in location)
            {
                var cardsInLocation = player.GetSourceList(loc);
                
                if (loc == Locations.PlayArea)
                {
                    // Include attachments controlled by this player
                    var relevantAttachments = attachments.Where(card => card.controller == player);
                    cards.AddRange(cardsInLocation.Concat(relevantAttachments));
                }
                else
                {
                    cards.AddRange(cardsInLocation);
                }
            }
            
            return cards;
        }
        
        /// <summary>
        /// Check if a card can be targeted by this selector
        /// Direct port of JavaScript canTarget method
        /// </summary>
        public virtual bool CanTarget(BaseCard card, AbilityContext context, Player choosingPlayer, List<BaseCard> selectedCards = null)
        {
            if (card == null)
                return false;
            
            selectedCards ??= new List<BaseCard>();
            
            // Check if card can be targeted (if checkTarget is enabled)
            if (checkTarget && !card.CanBeTargeted(context, selectedCards))
                return false;
            
            // Check controller restrictions
            if (controller == Players.Self && card.controller != context.player)
                return false;
                
            if (controller == Players.Opponent && card.controller != context.player.Opponent)
                return false;
            
            // Check location restrictions
            if (!location.Contains(Locations.Any) && !location.Contains(card.location))
                return false;
            
            // Special rule: hand cards can only be targeted by their controller
            if (card.location == Locations.Hand && card.controller != choosingPlayer)
                return false;
            
            // Check card type and condition
            return cardType.Contains(card.GetCardType()) && cardCondition(card, context);
        }
        
        /// <summary>
        /// Get all legal targets for this selector
        /// Direct port of JavaScript getAllLegalTargets method
        /// </summary>
        public virtual List<BaseCard> GetAllLegalTargets(AbilityContext context, Player choosingPlayer)
        {
            return FindPossibleCards(context)
                .Where(card => CanTarget(card, context, choosingPlayer))
                .ToList();
        }
        
        #endregion
        
        #region Selection Validation
        
        /// <summary>
        /// Check if enough cards have been selected
        /// Direct port of JavaScript hasEnoughSelected method
        /// </summary>
        public virtual bool HasEnoughSelected(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return optional || (selectedCards?.Count ?? 0) > 0;
        }
        
        /// <summary>
        /// Check if there are enough valid targets available
        /// Direct port of JavaScript hasEnoughTargets method
        /// </summary>
        public virtual bool HasEnoughTargets(AbilityContext context, Player choosingPlayer)
        {
            return FindPossibleCards(context).Any(card => CanTarget(card, context, choosingPlayer));
        }
        
        /// <summary>
        /// Check if the selection has exceeded the limit
        /// </summary>
        public virtual bool HasExceededLimit(List<BaseCard> selectedCards, AbilityContext context = null)
        {
            return false; // Override in subclasses that have limits
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Get the minimum number of cards required for this selector
        /// </summary>
        public virtual int GetMinimumRequired()
        {
            return optional ? 0 : Math.Min(1, numCards);
        }
        
        /// <summary>
        /// Get the maximum number of cards allowed for this selector
        /// </summary>
        public virtual int GetMaximumAllowed()
        {
            return numCards;
        }
        
        #endregion
        
        #region Debug and Utility
        
        /// <summary>
        /// Get debug information about this selector
        /// </summary>
        public virtual object GetDebugInfo()
        {
            return new
            {
                selectorType = GetType().Name,
                numCards,
                optional,
                ordered,
                multiSelect,
                checkTarget,
                controller = controller.ToString(),
                cardTypes = string.Join(", ", cardType),
                locations = string.Join(", ", location),
                hasCardCondition = cardCondition != null,
                activePromptTitle
            };
        }
        
        /// <summary>
        /// Get a string representation of this selector
        /// </summary>
        public override string ToString()
        {
            var typeDesc = cardType.Count > 0 ? string.Join("/", cardType) : "any";
            var locationDesc = location.Count > 0 ? string.Join("/", location) : "any location";
            var controllerDesc = controller != Players.Any ? $" controlled by {controller}" : "";
            var optionalDesc = optional ? " (optional)" : "";
            
            return $"{GetType().Name}: {numCards} {typeDesc} from {locationDesc}{controllerDesc}{optionalDesc}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// Extension methods for BaseCardSelector
    /// </summary>
    public static class BaseCardSelectorExtensions
    {
        /// <summary>
        /// Make the selector target only specific card types
        /// </summary>
        public static T ForCardTypes<T>(this T selector, params string[] types) where T : BaseCardSelector
        {
            selector.cardType = types.ToList();
            return selector;
        }
        
        /// <summary>
        /// Make the selector target only specific locations
        /// </summary>
        public static T InLocations<T>(this T selector, params string[] locations) where T : BaseCardSelector
        {
            selector.location = locations.ToList();
            return selector;
        }
        
        /// <summary>
        /// Set the controller filter
        /// </summary>
        public static T ControlledBy<T>(this T selector, string controllerType) where T : BaseCardSelector
        {
            selector.controller = controllerType;
            return selector;
        }
        
        /// <summary>
        /// Make the selector optional
        /// </summary>
        public static T MakeOptional<T>(this T selector) where T : BaseCardSelector
        {
            selector.optional = true;
            return selector;
        }
        
        /// <summary>
        /// Enable target checking
        /// </summary>
        public static T EnableTargetCheck<T>(this T selector) where T : BaseCardSelector
        {
            selector.checkTarget = true;
            return selector;
        }
        
        /// <summary>
        /// Set custom prompt title
        /// </summary>
        public static T WithPromptTitle<T>(this T selector, string title) where T : BaseCardSelector
        {
            selector.activePromptTitle = title;
            return selector;
        }
        
        /// <summary>
        /// Enable multi-selection
        /// </summary>
        public static T EnableMultiSelect<T>(this T selector) where T : BaseCardSelector
        {
            selector.multiSelect = true;
            return selector;
        }
        
        /// <summary>
        /// Enable ordered selection
        /// </summary>
        public static T EnableOrdered<T>(this T selector) where T : BaseCardSelector
        {
            selector.ordered = true;
            return selector;
        }
    }
}
