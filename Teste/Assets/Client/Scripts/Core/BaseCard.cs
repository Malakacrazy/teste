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
        public bool bowed = false;
        public bool ready = true;

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
            base.Initialize(game, printedName);

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
