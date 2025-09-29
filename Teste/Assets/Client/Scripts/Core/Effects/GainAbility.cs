using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class GainAbility : EffectValue
    {
        private string abilityType;
        private AbilityProperties properties;
        private Dictionary<string, AbilityLimit> grantedAbilityLimits;

        public GainAbility(string abilityType, AbilityProperties ability) : base()
        {
            this.abilityType = abilityType;
            this.properties = ability;
            this.grantedAbilityLimits = new Dictionary<string, AbilityLimit>();

            if (ability?.Properties != null)
            {
                var newProps = new AbilityProperties
                {
                    PrintedAbility = false,
                    AbilityIdentifier = ability.AbilityIdentifier,
                    Origin = ability.Card
                };

                if (ability.Properties.Limit != null)
                {
                    // If the copied ability has a limit, we need to create a new instantiation of it
                    newProps.Limit = AbilityLimit.Repeatable;
                }

                if (ability.Properties.Max != null)
                {
                    // Same for max
                    newProps.Max = AbilityLimit.Repeatable;
                }

                // Merge properties
                this.properties = MergeProperties(ability.Properties, newProps);
            }

            if (abilityType == AbilityTypes.Persistent && (this.properties?.Location == null || this.properties.Location == Locations.None))
            {
                if (this.properties == null)
                    this.properties = new AbilityProperties();
                
                this.properties.Location = Locations.PlayArea;
                this.properties.AbilityType = AbilityTypes.Persistent;
            }
        }

        public override void Reset()
        {
            grantedAbilityLimits.Clear();
        }

        public override void Apply(object target)
        {
            var card = target as BaseCard;
            if (card == null) return;

            var properties = MergeProperties(this.properties, new AbilityProperties { Origin = Context?.Source as BaseCard });

            if (abilityType == AbilityTypes.Persistent)
            {
                var activeLocations = new Dictionary<string, string[]>
                {
                    { "play area", new[] { Locations.PlayArea } },
                    { "province", new[] { 
                        Locations.ProvinceOne, Locations.ProvinceTwo, 
                        Locations.ProvinceThree, Locations.ProvinceFour, 
                        Locations.StrongholdProvince 
                    }}
                };

                value = properties;
                
                if (properties.Location != null && 
                    activeLocations.ContainsKey(properties.Location.ToString().ToLower()) &&
                    activeLocations[properties.Location.ToString().ToLower()].Contains(card.location))
                {
                    var persistentProps = properties as PersistentAbilityProperties;
                    if (persistentProps != null)
                    {
                        persistentProps.Ref = card.AddEffectToEngine(value);
                    }
                }
                return;
            }
            else if (abilityType == AbilityTypes.Action)
            {
                value = card.CreateAction(properties);
            }
            else
            {
                value = card.CreateTriggeredAbility(abilityType, properties);
                var triggeredAbility = value as ITriggeredAbility;
                triggeredAbility?.RegisterEvents();
            }

            var ability = value as IAbility;
            if (ability != null)
            {
                if (!grantedAbilityLimits.ContainsKey(card.Uuid))
                {
                    grantedAbilityLimits[card.Uuid] = ability.Limit;
                }
                else
                {
                    ability.Limit = grantedAbilityLimits[card.Uuid];
                }
            }
        }

        public override void Unapply(object target)
        {
            var card = target as BaseCard;
            if (card == null) return;

            var triggeredAbilityTypes = new[]
            {
                AbilityTypes.ForcedInterrupt, AbilityTypes.ForcedReaction,
                AbilityTypes.Interrupt, AbilityTypes.Reaction, AbilityTypes.WouldInterrupt
            };

            if (triggeredAbilityTypes.Contains(abilityType))
            {
                var triggeredAbility = value as ITriggeredAbility;
                triggeredAbility?.UnregisterEvents();
            }
            else if (abilityType == AbilityTypes.Persistent)
            {
                var persistentProps = value as PersistentAbilityProperties;
                if (persistentProps?.Ref != null)
                {
                    card.RemoveEffectFromEngine(persistentProps.Ref);
                    persistentProps.Ref = null;
                }
            }
        }

        private AbilityProperties MergeProperties(AbilityProperties original, AbilityProperties overrides)
        {
            if (original == null) return overrides;
            if (overrides == null) return original;

            var merged = new AbilityProperties
            {
                PrintedAbility = overrides.PrintedAbility ?? original.PrintedAbility,
                AbilityIdentifier = overrides.AbilityIdentifier ?? original.AbilityIdentifier,
                Origin = overrides.Origin ?? original.Origin,
                Limit = overrides.Limit ?? original.Limit,
                Max = overrides.Max ?? original.Max,
                Location = overrides.Location ?? original.Location,
                AbilityType = overrides.AbilityType ?? original.AbilityType
            };

            return merged;
        }
    }

    public class AbilityProperties
    {
        public bool? PrintedAbility { get; set; }
        public string AbilityIdentifier { get; set; }
        public BaseCard Origin { get; set; }
        public AbilityLimit Limit { get; set; }
        public AbilityLimit Max { get; set; }
        public string Location { get; set; }
        public string AbilityType { get; set; }
        public AbilityProperties Properties { get; set; }
        public BaseCard Card { get; set; }
    }

    public class PersistentAbilityProperties : AbilityProperties
    {
        public object Ref { get; set; }
    }

    public interface IAbility
    {
        AbilityLimit Limit { get; set; }
    }

    public interface ITriggeredAbility : IAbility
    {
        void RegisterEvents();
        void UnregisterEvents();
    }
}
