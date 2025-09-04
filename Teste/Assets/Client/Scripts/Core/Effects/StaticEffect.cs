using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class StaticEffect : IEffect
    {
        private static readonly HashSet<string> BinaryCardEffects = new HashSet<string>
        {
            EffectNames.Blank,
            EffectNames.CanBeSeenWhenFacedown,
            EffectNames.CannotParticipateAsAttacker,
            EffectNames.CannotParticipateAsDefender,
            EffectNames.AbilityRestrictions,
            EffectNames.DoesNotBow,
            EffectNames.DoesNotReady,
            EffectNames.ShowTopConflictCard
        };

        private static readonly HashSet<string> MilitaryModifiers = new HashSet<string>
        {
            EffectNames.ModifyBaseMilitarySkillMultiplier,
            EffectNames.ModifyMilitarySkill,
            EffectNames.ModifyMilitarySkillMultiplier,
            EffectNames.ModifyBothSkills
        };

        private static readonly HashSet<string> PoliticalModifiers = new HashSet<string>
        {
            EffectNames.ModifyBasePoliticalSkillMultiplier,
            EffectNames.ModifyPoliticalSkill,
            EffectNames.ModifyPoliticalSkillMultiplier,
            EffectNames.ModifyBothSkills
        };

        private static readonly Dictionary<string, Func<BaseCard, bool>> HasDashChecks = 
            new Dictionary<string, Func<BaseCard, bool>>
            {
                { "modifyBaseMilitarySkillMultiplier", card => card.HasDash("military") },
                { "modifyBasePoliticalSkillMultiplier", card => card.HasDash("political") },
                { "modifyBothSkills", card => card.HasDash("military") && card.HasDash("political") },
                { "modifyMilitarySkill", card => card.HasDash("military") },
                { "modifyMilitarySkillMultiplier", card => card.HasDash("military") },
                { "modifyPoliticalSkill", card => card.HasDash("political") },
                { "modifyPoliticalSkillMultiplier", card => card.HasDash("political") },
                { "setBaseMilitarySkill", card => card.HasDash("military") },
                { "setBasePoliticalSkill", card => card.HasDash("political") },
                { "setMilitarySkill", card => card.HasDash("military") },
                { "setPoliticalSkill", card => card.HasDash("political") }
            };

        private static readonly Dictionary<string, Func<object, object, IEffect[]>> ConflictingEffectsChecks = 
            new Dictionary<string, Func<object, object, IEffect[]>>
            {
                { "modifyBaseMilitarySkillMultiplier", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetBaseMilitarySkill).Cast<IEffect>().ToArray() },
                { "modifyBasePoliticalSkillMultiplier", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetBasePoliticalSkill).Cast<IEffect>().ToArray() },
                { "modifyGlory", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetGlory).Cast<IEffect>().ToArray() },
                { "modifyMilitarySkill", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetMilitarySkill).Cast<IEffect>().ToArray() },
                { "modifyMilitarySkillMultiplier", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetMilitarySkill).Cast<IEffect>().ToArray() },
                { "modifyPoliticalSkill", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetPoliticalSkill).Cast<IEffect>().ToArray() },
                { "modifyPoliticalSkillMultiplier", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetPoliticalSkill).Cast<IEffect>().ToArray() },
                { "setBaseMilitarySkill", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetMilitarySkill).Cast<IEffect>().ToArray() },
                { "setBasePoliticalSkill", (target, value) => 
                    ((BaseCard)target).Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == EffectNames.SetPoliticalSkill).Cast<IEffect>().ToArray() }
            };

        public string Type { get; protected set; }
        public EffectValue Value { get; protected set; }
        public AbilityContext Context { get; protected set; }
        public string Duration { get; set; }
        public bool IsConditional { get; set; }

        public StaticEffect(string type, object value = null)
        {
            Type = type;
            
            if (value is EffectValue effectValue)
            {
                Value = effectValue;
            }
            else
            {
                Value = new EffectValue(value);
            }
            
            Value.Reset();
            Context = null;
            Duration = null;
        }

        public virtual void Apply(object target)
        {
            var card = target as BaseCard;
            card?.AddEffect(this);
            Value.Apply(target);
        }

        public virtual void Unapply(object target)
        {
            var card = target as BaseCard;
            card?.RemoveEffect(this);
            Value.Unapply(target);
        }

        public object GetValue()
        {
            return Value.GetValue();
        }

        public virtual bool Recalculate()
        {
            return false;
        }

        public virtual void SetContext(AbilityContext context)
        {
            Context = context;
            Value.SetContext(context);
        }

        public bool CanBeApplied(object target)
        {
            if (!HasDashChecks.ContainsKey(Type))
                return true;

            var card = target as BaseCard;
            return card == null || !HasDashChecks[Type](card);
        }

        public bool IsMilitaryModifier()
        {
            return MilitaryModifiers.Contains(Type);
        }

        public bool IsPoliticalModifier()
        {
            return PoliticalModifiers.Contains(Type);
        }

        public bool IsSkillModifier()
        {
            return IsMilitaryModifier() || IsPoliticalModifier();
        }

        public bool CheckConflictingEffects(string type, object target)
        {
            if (BinaryCardEffects.Contains(type))
            {
                var card = target as BaseCard;
                if (card == null) return true;

                var matchingEffects = card.Effects.Where(effect => effect is IEffect && ((IEffect)effect).Type == type).Cast<IEffect>();
                return matchingEffects.All(effect => HasLongerDuration(effect) || effect.IsConditional);
            }

            if (ConflictingEffectsChecks.ContainsKey(type))
            {
                var matchingEffects = ConflictingEffectsChecks[type](target, GetValue()).Where(effect => effect != null);
                return matchingEffects.All(effect => HasLongerDuration(effect) || effect.IsConditional);
            }

            if (type == EffectNames.ModifyBothSkills)
            {
                return CheckConflictingEffects(EffectNames.ModifyMilitarySkill, target) || 
                       CheckConflictingEffects(EffectNames.ModifyPoliticalSkill, target);
            }

            if (type == EffectNames.HonorStatusDoesNotModifySkill)
            {
                return CheckConflictingEffects(EffectNames.ModifyMilitarySkill, target) || 
                       CheckConflictingEffects(EffectNames.ModifyPoliticalSkill, target);
            }

            if (type == EffectNames.HonorStatusReverseModifySkill)
            {
                return CheckConflictingEffects(EffectNames.ModifyMilitarySkill, target) || 
                       CheckConflictingEffects(EffectNames.ModifyPoliticalSkill, target);
            }

            return true;
        }

        public bool HasLongerDuration(IEffect effect)
        {
            var durations = new[]
            {
                Durations.UntilEndOfDuel,
                Durations.UntilEndOfConflict,
                Durations.UntilEndOfPhase,
                Durations.UntilEndOfRound
            };

            var thisDurationIndex = !string.IsNullOrEmpty(Duration) ? Array.IndexOf(durations, Duration) : -1;
            var otherDurationIndex = !string.IsNullOrEmpty(effect.Duration) ? Array.IndexOf(durations, effect.Duration) : -1;

            return thisDurationIndex > otherDurationIndex;
        }

        public EffectDebugInfo GetDebugInfo()
        {
            return new EffectDebugInfo
            {
                Type = Type,
                Value = Value
            };
        }
    }

    public class EffectDebugInfo
    {
        public string Type { get; set; }
        public EffectValue Value { get; set; }
    }

    public interface IEffect
    {
        string Type { get; }
        void Apply(object target);
        void Unapply(object target);
        object GetValue();
        bool Recalculate();
        void SetContext(AbilityContext context);
        string Duration { get; set; }
        bool IsConditional { get; set; }
    }
}
