using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Constants for effect durations
    /// </summary>
    public static class EffectDurations
    {
        public const string Persistent = "persistent";
        public const string Temporary = "temporary";
        public const string UntilEndOfPhase = "untilEndOfPhase";
        public const string UntilEndOfRound = "untilEndOfRound";
        public const string UntilEndOfConflict = "untilEndOfConflict";
    }
    
    /// <summary>
    /// Constants for targeting players in effects
    /// </summary>
    public static class EffectPlayers
    {
        public const string Self = "self";
        public const string Opponent = "opponent";
        public const string Any = "any";
    }
    
    /// <summary>
    /// Helper class for creating game effects
    /// </summary>
    public static class GameEffects
    {
        public static object AttachmentLimit(int limit)
        {
            return new { type = EffectNames.AttachmentLimit, value = limit };
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
            return new { type = EffectNames.AttachmentFactionRestriction, value = factions };
        }
        
        public static object AttachmentTraitRestriction(List<string> traits)
        {
            return new { type = EffectNames.AttachmentTraitRestriction, value = traits };
        }
        
        public static object AttachmentRestrictTraitAmount(Dictionary<string, int> traitLimits)
        {
            return new { type = "attachmentRestrictTraitAmount", value = traitLimits };
        }
        
        public static object AddKeyword(string keyword)
        {
            return new { type = EffectNames.AddTrait, value = keyword };
        }
        
        public static object AttachmentMilitarySkillModifier(int bonus)
        {
            return new { type = "attachmentMilitarySkillModifier", value = bonus };
        }
        
        public static object AttachmentPoliticalSkillModifier(int bonus)
        {
            return new { type = "attachmentPoliticalSkillModifier", value = bonus };
        }
    }
}
