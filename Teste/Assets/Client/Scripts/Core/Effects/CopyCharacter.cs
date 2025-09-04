using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class CopyCharacter : EffectValue
    {
        private List<GainAbility> actions;
        private List<GainAbility> reactions;
        private List<PersistentEffectProperties> persistentEffects;
        private Dictionary<string, CharacterAbilities> abilitiesForTargets;

        public CopyCharacter(BaseCard card) : base(card)
        {
            actions = card.Abilities.actions?.Select(action => 
                new GainAbility("action", ConvertToAbilityProperties(action))).ToList() ?? new List<GainAbility>();
            
            reactions = card.Abilities.reactions?.Select(ability => 
                new GainAbility("reaction", ConvertToAbilityProperties(ability))).ToList() ?? new List<GainAbility>();
            
            persistentEffects = card.Abilities.persistentEffects?.Select(effect => 
                new PersistentEffectProperties(effect)).ToList() ?? new List<PersistentEffectProperties>();
            
            abilitiesForTargets = new Dictionary<string, CharacterAbilities>();
        }

        public override void Apply(object target)
        {
            var card = target as BaseCard;
            if (card == null) return;

            var abilities = new CharacterAbilities
            {
                Actions = actions.Select(value =>
                {
                    value.Apply(target);
                    return value.GetValue();
                }).ToList(),
                
                Reactions = reactions.Select(value =>
                {
                    value.Apply(target);
                    return value.GetValue();
                }).ToList()
            };

            abilitiesForTargets[card.Uuid] = abilities;

            foreach (var effect in persistentEffects)
            {
                if (effect.location == Locations.PlayArea || effect.location == Locations.Any)
                {
                    effect.Ref = card.AddEffectToEngine(effect);
                }
            }
        }

        public override void Unapply(object target)
        {
            var card = target as BaseCard;
            if (card == null || !abilitiesForTargets.ContainsKey(card.Uuid)) return;

            foreach (var value in abilitiesForTargets[card.Uuid].Reactions)
            {
                if (value is ITriggeredAbility triggeredAbility)
                {
                    triggeredAbility.UnregisterEvents();
                }
            }

            foreach (var effect in persistentEffects)
            {
                if (effect.Ref != null)
                {
                    card.RemoveEffectFromEngine(effect.Ref);
                    effect.Ref = null;
                }
            }

            abilitiesForTargets.Remove(card.Uuid);
        }

        public List<object> GetActions(BaseCard target)
        {
            if (abilitiesForTargets.ContainsKey(target.Uuid))
            {
                return abilitiesForTargets[target.Uuid].Actions;
            }
            return new List<object>();
        }

        public List<object> GetReactions(BaseCard target)
        {
            if (abilitiesForTargets.ContainsKey(target.Uuid))
            {
                return abilitiesForTargets[target.Uuid].Reactions;
            }
            return new List<object>();
        }

        public List<PersistentEffectProperties> GetPersistentEffects()
        {
            return persistentEffects;
        }

        private AbilityProperties ConvertToAbilityProperties(object abilityObject)
        {
            if (abilityObject is CardAction cardAction)
            {
                return new AbilityProperties
                {
                    AbilityType = AbilityTypes.Action,
                    Card = cardAction.card
                };
            }
            
            if (abilityObject is TriggeredAbility triggeredAbility)
            {
                return new AbilityProperties
                {
                    AbilityType = triggeredAbility.abilityType,
                    Card = triggeredAbility.card
                };
            }
            
            // Fallback for unknown types
            return new AbilityProperties();
        }

        private class CharacterAbilities
        {
            public List<object> Actions { get; set; }
            public List<object> Reactions { get; set; }
        }
    }

}
