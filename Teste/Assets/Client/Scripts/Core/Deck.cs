using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a deck entry with card data and count
    /// </summary>
    [System.Serializable]
    public class DeckCardEntry
    {
        public CardData card;
        public int count;

        public DeckCardEntry(CardData cardData, int cardCount)
        {
            card = cardData;
            count = cardCount;
        }
    }

    /// <summary>
    /// Raw deck data structure for loading from JSON or deck builder
    /// </summary>
    [System.Serializable]
    public class DeckData
    {
        public string name;
        public string id;
        public Faction faction;
        public List<DeckCardEntry> conflictCards = new List<DeckCardEntry>();
        public List<DeckCardEntry> dynastyCards = new List<DeckCardEntry>();
        public List<DeckCardEntry> provinceCards = new List<DeckCardEntry>();
        public List<DeckCardEntry> stronghold = new List<DeckCardEntry>();
        public List<DeckCardEntry> role = new List<DeckCardEntry>();
        public bool selected = false;
    }

    /// <summary>
    /// Prepared deck with instantiated cards ready for play
    /// </summary>
    [System.Serializable]
    public class PreparedDeck
    {
        public Faction faction;
        public List<BaseCard> conflictCards = new List<BaseCard>();
        public List<BaseCard> dynastyCards = new List<BaseCard>();
        public List<BaseCard> provinceCards = new List<BaseCard>();
        public StrongholdCard stronghold;
        public RoleCard role;
        public List<BaseCard> allCards = new List<BaseCard>();
    }

    /// <summary>
    /// Faction information for deck building
    /// </summary>
    [System.Serializable]
    public class Faction
    {
        public string id;
        public string name;
        public string value;
        public string clanChampion;
        public List<string> traits = new List<string>();
    }

    /// <summary>
    /// Handles deck preparation and card instantiation for players
    /// </summary>
    public class Deck : MonoBehaviour
    {
        [Header("Deck Data")]
        public DeckData data;
        public bool selected = false;

        [Header("Card Registry")]
        [SerializeField] private Dictionary<string, System.Type> cardRegistry = new Dictionary<string, System.Type>();

        /// <summary>
        /// Initialize the deck with data
        /// </summary>
        /// <param name="deckData">Raw deck data</param>
        public void Initialize(DeckData deckData)
        {
            data = deckData;
            selected = deckData.selected;
            
            // Initialize card registry with custom card implementations
            InitializeCardRegistry();
            
            Debug.Log($"🎴 Deck initialized: {data.name} ({data.faction.name})");
        }

        /// <summary>
        /// Create a deck from data (factory method)
        /// </summary>
        /// <param name="deckData">Raw deck data</param>
        /// <returns>New deck instance</returns>
        public static Deck Create(DeckData deckData)
        {
            var deckGO = new UnityEngine.GameObject($"Deck_{deckData.name}");
            var deck = deckGO.AddComponent<Deck>();
            deck.Initialize(deckData);
            return deck;
        }

        /// <summary>
        /// Prepare the deck for a player by instantiating all cards
        /// </summary>
        /// <param name="player">Player who will own the deck</param>
        /// <returns>Prepared deck with instantiated cards</returns>
        public PreparedDeck Prepare(Player player)
        {
            var result = new PreparedDeck
            {
                faction = data.faction,
                conflictCards = new List<BaseCard>(),
                dynastyCards = new List<BaseCard>(),
                provinceCards = new List<BaseCard>(),
                stronghold = null,
                role = null,
                allCards = new List<BaseCard>()
            };

            Debug.Log($"🎴 Preparing deck for {player.name}: {data.name}");

            // Prepare conflict cards
            EachRepeatedCard(data.conflictCards, cardData => {
                if (cardData != null && IsConflictSide(cardData.side))
                {
                    var conflictCard = CreateCard<DrawCard>(player, cardData);
                    conflictCard.location = Locations.ConflictDeck;
                    conflictCard.isConflict = true;
                    result.conflictCards.Add(conflictCard);
                }
            });

            // Prepare dynasty cards
            EachRepeatedCard(data.dynastyCards, cardData => {
                if (cardData != null && IsDynastySide(cardData.side))
                {
                    var dynastyCard = CreateCard<DrawCard>(player, cardData);
                    dynastyCard.location = Locations.DynastyDeck;
                    dynastyCard.isDynasty = true;
                    result.dynastyCards.Add(dynastyCard);
                }
            });

            // Prepare province cards
            EachRepeatedCard(data.provinceCards, cardData => {
                if (cardData != null && cardData.type == CardTypes.Province)
                {
                    var provinceCard = CreateCard<ProvinceCard>(player, cardData);
                    provinceCard.location = Locations.ProvinceDeck;
                    provinceCard.isProvince = true;
                    result.provinceCards.Add(provinceCard);
                }
            });

            // Prepare stronghold (should be exactly one)
            EachRepeatedCard(data.stronghold, cardData => {
                if (cardData != null && cardData.type == CardTypes.Stronghold)
                {
                    var strongholdCard = CreateCard<StrongholdCard>(player, cardData);
                    strongholdCard.location = Locations.StrongholdProvince;
                    strongholdCard.isStronghold = true;
                    result.stronghold = strongholdCard;
                }
            });

            // Prepare role (optional, one maximum)
            EachRepeatedCard(data.role, cardData => {
                if (cardData != null && cardData.type == CardTypes.Role)
                {
                    var roleCard = CreateCard<RoleCard>(player, cardData);
                    roleCard.location = Locations.Role;
                    result.role = roleCard;
                }
            });

            // Combine all cards for easy access
            result.allCards.AddRange(result.provinceCards);
            result.allCards.AddRange(result.conflictCards);
            result.allCards.AddRange(result.dynastyCards);

            if (result.stronghold != null)
            {
                result.allCards.Add(result.stronghold);
            }

            if (result.role != null)
            {
                result.allCards.Add(result.role);
            }

            Debug.Log($"🎴 Deck prepared: {result.conflictCards.Count} conflict, " +
                     $"{result.dynastyCards.Count} dynasty, {result.provinceCards.Count} provinces");

            return result;
        }

        /// <summary>
        /// Execute a function for each card considering duplicate counts
        /// </summary>
        /// <param name="cardEntries">List of deck card entries</param>
        /// <param name="action">Action to execute for each card instance</param>
        private void EachRepeatedCard(List<DeckCardEntry> cardEntries, System.Action<CardData> action)
        {
            foreach (var cardEntry in cardEntries)
            {
                for (int i = 0; i < cardEntry.count; i++)
                {
                    action(cardEntry.card);
                }
            }
        }

        /// <summary>
        /// Create a card instance with the appropriate type
        /// </summary>
        /// <typeparam name="T">Base card type</typeparam>
        /// <param name="player">Player who will own the card</param>
        /// <param name="cardData">Card data</param>
        /// <returns>Instantiated card</returns>
        private T CreateCard<T>(Player player, CardData cardData) where T : BaseCard
        {
            // Try to find custom implementation first
            System.Type cardClass = GetCardClass(cardData.id) ?? typeof(T);

            // Create GameObject for the card
            var cardGO = new UnityEngine.GameObject($"{cardData.name}_{Guid.NewGuid().ToString("N")[..8]}");
            cardGO.transform.SetParent(player.transform);

            // Add the appropriate card component
            var card = cardGO.AddComponent(cardClass) as T;
            
            if (card == null)
            {
                Debug.LogError($"❌ Failed to create card {cardData.name} as type {typeof(T).Name}");
                Destroy(cardGO);
                return null;
            }

            // Initialize the card
            card.Initialize(cardData, player);

            return card;
        }

        /// <summary>
        /// Get the appropriate card class for a card ID
        /// </summary>
        /// <param name="cardId">Card ID to look up</param>
        /// <returns>Card class type, or null if not found</returns>
        private System.Type GetCardClass(string cardId)
        {
            if (cardRegistry.TryGetValue(cardId, out System.Type cardType))
            {
                return cardType;
            }

            // Try to find card class by reflection (for development)
            return FindCardClassByReflection(cardId);
        }

        /// <summary>
        /// Find card class using reflection (fallback method)
        /// </summary>
        /// <param name="cardId">Card ID to find class for</param>
        /// <returns>Card class type, or null if not found</returns>
        private System.Type FindCardClassByReflection(string cardId)
        {
            try
            {
                // Convert card ID to class name (e.g., "ashigaru-levy" -> "AshigaruLevy")
                string className = ConvertIdToClassName(cardId);
                
                // Look for the class in the current assembly
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var fullClassName = $"L5RGame.Cards.{className}";
                
                return assembly.GetType(fullClassName);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"⚠️ Could not find custom class for card {cardId}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert card ID to potential class name
        /// </summary>
        /// <param name="cardId">Card ID (e.g., "ashigaru-levy")</param>
        /// <returns>Class name (e.g., "AshigaruLevy")</returns>
        private string ConvertIdToClassName(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return "";

            // Split by common separators and capitalize each part
            var parts = cardId.Split(new char[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = "";

            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result += char.ToUpper(part[0]) + part.Substring(1).ToLower();
                }
            }

            return result;
        }

        /// <summary>
        /// Initialize the card registry with known custom card implementations
        /// </summary>
        private void InitializeCardRegistry()
        {
            cardRegistry.Clear();

            // Scan for custom card classes in the assembly
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var cardTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseCard)) && !t.IsAbstract)
                .ToList();

            foreach (var cardType in cardTypes)
            {
                // Try to get card ID from attribute or class name
                var cardId = GetCardIdFromType(cardType);
                if (!string.IsNullOrEmpty(cardId))
                {
                    cardRegistry[cardId] = cardType;
                }
            }

            Debug.Log($"🎴 Card registry initialized with {cardRegistry.Count} custom cards");
        }

        /// <summary>
        /// Get card ID from card type (using attribute or class name)
        /// </summary>
        /// <param name="cardType">Card type to get ID for</param>
        /// <returns>Card ID, or null if not determinable</returns>
        private string GetCardIdFromType(System.Type cardType)
        {
            // Check for CardId attribute
            var cardIdAttribute = cardType.GetCustomAttributes(typeof(CardIdAttribute), false)
                .FirstOrDefault() as CardIdAttribute;
            
            if (cardIdAttribute != null)
            {
                return cardIdAttribute.CardId;
            }

            // Fallback: convert class name to card ID
            return ConvertClassNameToId(cardType.Name);
        }

        /// <summary>
        /// Convert class name to card ID format
        /// </summary>
        /// <param name="className">Class name (e.g., "AshigaruLevy")</param>
        /// <returns>Card ID (e.g., "ashigaru-levy")</returns>
        private string ConvertClassNameToId(string className)
        {
            if (string.IsNullOrEmpty(className)) return "";

            var result = "";
            for (int i = 0; i < className.Length; i++)
            {
                char c = className[i];
                
                if (char.IsUpper(c) && i > 0)
                {
                    result += "-";
                }
                
                result += char.ToLower(c);
            }

            return result;
        }

        /// <summary>
        /// Check if a card side is conflict
        /// </summary>
        /// <param name="side">Card side</param>
        /// <returns>True if conflict side</returns>
        private bool IsConflictSide(string side)
        {
            return side == "conflict";
        }

        /// <summary>
        /// Check if a card side is dynasty
        /// </summary>
        /// <param name="side">Card side</param>
        /// <returns>True if dynasty side</returns>
        private bool IsDynastySide(string side)
        {
            return side == "dynasty";
        }

        /// <summary>
        /// Validate deck composition according to L5R rules
        /// </summary>
        /// <returns>Validation result with errors</returns>
        public DeckValidationResult ValidateDeck()
        {
            var result = new DeckValidationResult();

            // Check stronghold count
            if (data.stronghold.Count != 1)
            {
                result.AddError("Deck must have exactly one stronghold");
            }

            // Check province count
            var provinceCount = data.provinceCards.Sum(entry => entry.count);
            if (provinceCount < 4)
            {
                result.AddError($"Deck must have at least 4 provinces (has {provinceCount})");
            }
            else if (provinceCount > 5)
            {
                result.AddError($"Deck cannot have more than 5 provinces (has {provinceCount})");
            }

            // Check conflict deck size
            var conflictSize = data.conflictCards.Sum(entry => entry.count);
            if (conflictSize < 40)
            {
                result.AddError($"Conflict deck must have at least 40 cards (has {conflictSize})");
            }
            else if (conflictSize > 45)
            {
                result.AddError($"Conflict deck cannot have more than 45 cards (has {conflictSize})");
            }

            // Check dynasty deck size
            var dynastySize = data.dynastyCards.Sum(entry => entry.count);
            if (dynastySize < 40)
            {
                result.AddError($"Dynasty deck must have at least 40 cards (has {dynastySize})");
            }
            else if (dynastySize > 45)
            {
                result.AddError($"Dynasty deck cannot have more than 45 cards (has {dynastySize})");
            }

            // Check card limits (max 3 of any card)
            CheckCardLimits(data.conflictCards, result, "Conflict");
            CheckCardLimits(data.dynastyCards, result, "Dynasty");

            // Check role count
            if (data.role.Count > 1)
            {
                result.AddError("Deck cannot have more than one role");
            }

            return result;
        }

        /// <summary>
        /// Check card count limits for a deck section
        /// </summary>
        /// <param name="cards">Cards to check</param>
        /// <param name="result">Validation result to add errors to</param>
        /// <param name="deckType">Type of deck for error messages</param>
        private void CheckCardLimits(List<DeckCardEntry> cards, DeckValidationResult result, string deckType)
        {
            foreach (var entry in cards)
            {
                if (entry.count > 3)
                {
                    result.AddError($"{deckType} deck: Cannot have more than 3 copies of {entry.card.name} (has {entry.count})");
                }

                // Check for limited cards (max 1)
                if (entry.card.traits.Contains("limited") && entry.count > 1)
                {
                    result.AddError($"{deckType} deck: Cannot have more than 1 copy of limited card {entry.card.name} (has {entry.count})");
                }
            }
        }

        /// <summary>
        /// Get deck statistics for UI display
        /// </summary>
        /// <returns>Deck statistics</returns>
        public DeckStatistics GetStatistics()
        {
            return new DeckStatistics
            {
                name = data.name,
                faction = data.faction.name,
                conflictCount = data.conflictCards.Sum(entry => entry.count),
                dynastyCount = data.dynastyCards.Sum(entry => entry.count),
                provinceCount = data.provinceCards.Sum(entry => entry.count),
                hasStronghold = data.stronghold.Count > 0,
                hasRole = data.role.Count > 0,
                totalCards = data.conflictCards.Sum(entry => entry.count) + 
                           data.dynastyCards.Sum(entry => entry.count) +
                           data.provinceCards.Sum(entry => entry.count) +
                           data.stronghold.Sum(entry => entry.count) +
                           data.role.Sum(entry => entry.count)
            };
        }

        /// <summary>
        /// Create a summary of the deck for network sync
        /// </summary>
        /// <returns>Deck summary</returns>
        public DeckSummary GetSummary()
        {
            return new DeckSummary
            {
                id = data.id,
                name = data.name,
                faction = data.faction,
                selected = selected,
                cardCount = GetStatistics().totalCards,
                isValid = ValidateDeck().IsValid
            };
        }

        /// <summary>
        /// Cleanup when deck is destroyed
        /// </summary>
        private void OnDestroy()
        {
            cardRegistry.Clear();
            Debug.Log($"🎴 Deck {data?.name} destroyed");
        }
    }

    /// <summary>
    /// Attribute for marking card classes with their card ID
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class CardIdAttribute : System.Attribute
    {
        public string CardId { get; }

        public CardIdAttribute(string cardId)
        {
            CardId = cardId;
        }
    }

    /// <summary>
    /// Result of deck validation
    /// </summary>
    public class DeckValidationResult
    {
        public List<string> errors = new List<string>();
        public bool IsValid => errors.Count == 0;

        public void AddError(string error)
        {
            errors.Add(error);
        }
    }

    /// <summary>
    /// Deck statistics for UI display
    /// </summary>
    [System.Serializable]
    public class DeckStatistics
    {
        public string name;
        public string faction;
        public int conflictCount;
        public int dynastyCount;
        public int provinceCount;
        public bool hasStronghold;
        public bool hasRole;
        public int totalCards;
    }

    /// <summary>
    /// Deck summary for network sync
    /// </summary>
    [System.Serializable]
    public class DeckSummary
    {
        public string id;
        public string name;
        public Faction faction;
        public bool selected;
        public int cardCount;
        public bool isValid;
    }

    /// <summary>
    /// Specific card types for L5R
    /// </summary>
    public partial class DrawCard : BaseCard
    {
        // Default implementation for most cards
        // Specific cards can inherit from this and override behavior
    }

    public partial class ProvinceCard : BaseCard
    {
        public override void Initialize(CardData data, Player cardOwner)
        {
            base.Initialize(data, cardOwner);
            strength = data.strength;
            isProvince = true;
        }

        public virtual int GetProvinceStrengthBonus()
        {
            return SumEffects("modifyProvinceStrength");
        }
    }

    public partial class StrongholdCard : BaseCard
    {
        [Header("Stronghold Specific")]
        public int startingHonor;
        public int fateValue;
        public int influencePool;

        public override void Initialize(CardData data, Player cardOwner)
        {
            base.Initialize(data, cardOwner);
            startingHonor = data.honor;
            fateValue = data.fate;
            influencePool = data.influencePool;
            isStronghold = true;
        }

        public int GetStartingHonor()
        {
            return startingHonor + SumEffects("modifyStartingHonor");
        }

        public int GetFateValue()
        {
            return fateValue + SumEffects("modifyStrongholdFate");
        }
    }

    public partial class RoleCard : BaseCard
    {
        [Header("Role Specific")]
        public List<string> traits = new List<string>();
        public List<string> elements = new List<string>();

        public override void Initialize(CardData data, Player cardOwner)
        {
            base.Initialize(data, cardOwner);
            traits = data.traits ?? new List<string>();
        }

        protected virtual void SetupCardAbilities()
        {
            // Role cards typically provide ongoing effects
            // These will be implemented by specific role cards
        }
    }

    /// <summary>
    /// Extension methods for deck management
    /// </summary>
    public static class DeckExtensions
    {
        /// <summary>
        /// Load deck from JSON file
        /// </summary>
        /// <param name="filePath">Path to deck JSON file</param>
        /// <returns>Loaded deck data</returns>
        public static DeckData LoadFromFile(string filePath)
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                return JsonUtility.FromJson<DeckData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load deck from {filePath}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save deck to JSON file
        /// </summary>
        /// <param name="deckData">Deck data to save</param>
        /// <param name="filePath">Path to save to</param>
        public static void SaveToFile(this DeckData deckData, string filePath)
        {
            try
            {
                string json = JsonUtility.ToJson(deckData, true);
                System.IO.File.WriteAllText(filePath, json);
                Debug.Log($"💾 Deck saved to {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to save deck to {filePath}: {e.Message}");
            }
        }

        /// <summary>
        /// Create a sample deck for testing
        /// </summary>
        /// <param name="factionName">Faction to create deck for</param>
        /// <returns>Sample deck data</returns>
        public static DeckData CreateSampleDeck(string factionName = "Crab")
        {
            return new DeckData
            {
                name = $"Sample {factionName} Deck",
                id = Guid.NewGuid().ToString(),
                faction = new Faction { name = factionName, value = factionName.ToLower() },
                // This would be populated with actual card data
                conflictCards = new List<DeckCardEntry>(),
                dynastyCards = new List<DeckCardEntry>(),
                provinceCards = new List<DeckCardEntry>(),
                stronghold = new List<DeckCardEntry>(),
                role = new List<DeckCardEntry>()
            };
        }
    }
}