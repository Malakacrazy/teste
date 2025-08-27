using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class for objects that can be sources of effects (cards, rings, etc.)
    /// </summary>
    public class EffectSource : MonoBehaviour
    {
        [Header("Effect Source")]
        public string uuid;
        public string name;
        public Game game;
        
        protected List<object> effects = new List<object>();
        protected List<object> lastingEffects = new List<object>();
        
        /// <summary>
        /// Initialize the effect source
        /// </summary>
        public virtual void Initialize(Game gameInstance, string sourceName)
        {
            game = gameInstance;
            name = sourceName;
            uuid = System.Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// Check restrictions for this effect source
        /// </summary>
        public virtual bool CheckRestrictions(string actionType, AbilityContext context)
        {
            // Base implementation - can be overridden
            return true;
        }
        
        /// <summary>
        /// Get effects of a specific type
        /// </summary>
        public List<object> GetEffects(string effectName)
        {
            return effects.Where(effect => GetEffectType(effect) == effectName).ToList();
        }
        
        /// <summary>
        /// Get raw effects list
        /// </summary>
        public List<object> GetRawEffects()
        {
            return effects.ToList();
        }
        
        /// <summary>
        /// Check if any effect of a specific type exists
        /// </summary>
        public bool AnyEffect(string effectName)
        {
            return effects.Any(effect => GetEffectType(effect) == effectName);
        }
        
        /// <summary>
        /// Get the most recent effect of a specific type
        /// </summary>
        public object MostRecentEffect(string effectName)
        {
            return effects.LastOrDefault(effect => GetEffectType(effect) == effectName);
        }
        
        /// <summary>
        /// Add an effect to the game engine
        /// </summary>
        public object AddEffectToEngine(object effect)
        {
            // Placeholder - would integrate with EffectEngine
            effects.Add(effect);
            return effect;
        }
        
        /// <summary>
        /// Remove an effect from the game engine
        /// </summary>
        public void RemoveEffectFromEngine(object effectReference)
        {
            if (effectReference != null)
            {
                effects.Remove(effectReference);
            }
        }
        
        /// <summary>
        /// Remove all lasting effects
        /// </summary>
        public void RemoveLastingEffects()
        {
            foreach (var effect in lastingEffects.ToList())
            {
                RemoveEffectFromEngine(effect);
            }
            lastingEffects.Clear();
        }
        
        /// <summary>
        /// Get the type of an effect (placeholder implementation)
        /// </summary>
        protected virtual string GetEffectType(object effect)
        {
            // Placeholder - would examine effect structure
            var effectType = effect?.GetType().Name;
            return effectType ?? "Unknown";
        }
        
        /// <summary>
        /// Get short summary for UI controls
        /// </summary>
        public virtual Dictionary<string, object> GetShortSummaryForControls(Player activePlayer)
        {
            return new Dictionary<string, object>
            {
                {"uuid", uuid},
                {"name", name},
                {"type", GetType().Name}
            };
        }
        
        /// <summary>
        /// Get short summary
        /// </summary>
        public virtual Dictionary<string, object> GetShortSummary()
        {
            return new Dictionary<string, object>
            {
                {"name", name},
                {"uuid", uuid}
            };
        }
        
        /// <summary>
        /// Has keyword check (for cards that have keywords)
        /// </summary>
        public virtual bool HasKeyword(string keyword)
        {
            return false; // Override in BaseCard
        }
    }
    

    
    /// <summary>
    /// Static class for creating common effects
    /// </summary>
    public static class Effects
    {
        public static object AddKeyword(string keyword)
        {
            return new { type = "addKeyword", keyword = keyword };
        }
        
        public static object AttachmentLimit(int limit)
        {
            return new { type = EffectNames.AttachmentLimit, limit = limit };
        }
        
        public static object AttachmentMyControlOnly()
        {
            return new { type = EffectNames.AttachmentMyControlOnly };
        }
        
        public static object AttachmentUniqueRestriction()
        {
            return new { type = EffectNames.AttachmentUniqueRestriction };
        }
        
        public static object AttachmentFactionRestriction(List<string> factions)
        {
            return new { type = EffectNames.AttachmentFactionRestriction, factions = factions };
        }
        
        public static object AttachmentTraitRestriction(List<string> traits)
        {
            return new { type = EffectNames.AttachmentTraitRestriction, traits = traits };
        }
        
        public static object AttachmentRestrictTraitAmount(Dictionary<string, int> traitLimits)
        {
            return new { type = "attachmentRestrictTraitAmount", traitLimits = traitLimits };
        }
        
        public static object AttachmentMilitarySkillModifier(int bonus)
        {
            return new { type = "attachmentMilitarySkillModifier", bonus = bonus };
        }
        
        public static object AttachmentPoliticalSkillModifier(int bonus)
        {
            return new { type = "attachmentPoliticalSkillModifier", bonus = bonus };
        }
    }
    

}
