using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Base class inherited by Ring and BaseCard that provides effect management capabilities.
    /// Also represents Framework effects that are not tied to specific game objects.
    /// </summary>
    public class EffectSource : MonoBehaviour
    {
        /// <summary>
        /// Constructor for creating effect sources
        /// </summary>
        public EffectSource() : base()
        {
            activeEffects = new List<object>();
        }
        
        /// <summary>
        /// Constructor with Game and source name
        /// </summary>
        public EffectSource(Game gameInstance, string sourceName = "Framework effect") : base()
        {
            game = gameInstance;
            name = sourceName;
            activeEffects = new List<object>();
        }
        
        /// <summary>
        /// Constructor with just source name
        /// </summary>
        public EffectSource(string sourceName) : base()
        {
            name = sourceName;
            activeEffects = new List<object>();
        }
        
        /// <summary>
        /// Static factory method to create EffectSource with game and source name
        /// </summary>
        public static EffectSource CreateEffectSource(Game gameInstance, string sourceName = "Framework effect")
        {
            var go = new UnityEngine.GameObject(sourceName);
            var effectSource = go.AddComponent<EffectSource>();
            effectSource.Initialize(gameInstance, sourceName);
            return effectSource;
        }
        
        /// <summary>
        /// Static factory method to create EffectSource with just source name
        /// </summary>
        public static EffectSource CreateEffectSource(string sourceName = "Framework effect")
        {
            var go = new UnityEngine.GameObject(sourceName);
            var effectSource = go.AddComponent<EffectSource>();
            effectSource.name = sourceName;
            effectSource.activeEffects = new List<object>();
            return effectSource;
        }
        [Header("Effect Source")]
        public List<object> activeEffects = new List<object>();
        [System.NonSerialized]
        public Game game;
        public Game Game => game;
        
        [Header("Card Properties")]
        [System.NonSerialized]
        public Player controller;
        public List<object> persistentEffects = new List<object>();
        [System.NonSerialized]
        public bool facedown = false;
        
        /// <summary>
        /// Initialize the EffectSource
        /// </summary>
        /// <param name="gameInstance">The game instance</param>
        /// <param name="sourceName">Name of the effect source (defaults to "Framework effect")</param>
        public virtual void Initialize(Game gameInstance, string sourceName = "Framework effect")
        {
            game = gameInstance;
            name = sourceName;
            activeEffects = new List<object>();
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the end of the current duel.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilEndOfDuel(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilEndOfDuel;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the end of the current conflict.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilEndOfConflict(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilEndOfConflict;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the end of the phase.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilEndOfPhase(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilEndOfPhase;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the end of the round.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilEndOfRound(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilEndOfRound;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the current player passes priority.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilPassPriority(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilPassPriority;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the opponent passes priority.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilOpponentPassPriority(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilOpponentPassPriority;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies an immediate effect which lasts until the next time any player passes priority.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void UntilNextPassPriority(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.UntilNextPassPriority;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Applies a lasting effect which lasts until an event contained in the
        /// 'until' property for the effect has occurred.
        /// </summary>
        /// <param name="propertyFactory">Function that creates effect properties using AbilityDsl</param>
        public void LastingEffect(System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.Custom;
            properties.location = Locations.Any;
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Adds a persistent/lasting/delayed effect to the effect engine.
        /// </summary>
        /// <param name="properties">Properties for the effect</param>
        /// <returns>List of effect references that were added</returns>
        public List<object> AddEffectToEngine(EffectProperties properties)
        {
            var effect = properties.effect;
            var effectProperties = ClonePropertiesWithoutEffect(properties);
            
            List<object> addedEffects;

            if (effect is List<object> effectList)
            {
                // Handle multiple effects
                addedEffects = effectList.Select(factory =>
                {
                    var effectInstance = CreateEffectInstance(factory, effectProperties);
                    return (object)game.EffectEngine.Add(effectInstance as GameEffect);
                }).ToList();
            }
            else
            {
                // Handle single effect
                var effectInstance = CreateEffectInstance(effect, effectProperties);
                addedEffects = new List<object> { game.EffectEngine.Add(effectInstance as GameEffect) };
            }

            // Track active effects for cleanup
            activeEffects.AddRange(addedEffects);
            
            return addedEffects;
        }

        /// <summary>
        /// Creates an effect instance from a factory function or direct effect
        /// </summary>
        /// <param name="factory">Effect factory or direct effect</param>
        /// <param name="properties">Effect properties</param>
        /// <returns>Effect instance</returns>
        private object CreateEffectInstance(object factory, EffectProperties properties)
        {
            if (factory is System.Func<Game, EffectSource, EffectProperties, object> effectFactory)
            {
                return effectFactory(game, this, properties);
            }
            else if (factory is IEffectFactory factoryInterface)
            {
                return factoryInterface.Create(game, this, properties);
            }
            else
            {
                // Direct effect object
                return factory;
            }
        }

        /// <summary>
        /// Clones effect properties without the effect field
        /// </summary>
        /// <param name="original">Original properties</param>
        /// <returns>Cloned properties without effect</returns>
        private EffectProperties ClonePropertiesWithoutEffect(EffectProperties original)
        {
            return new EffectProperties
            {
                duration = original.duration,
                location = original.location,
                condition = original.condition,
                match = original.match,
                targetController = original.targetController,
                until = original.until,
                multipleTrigger = original.multipleTrigger,
                when = original.when
                // Note: effect is intentionally omitted
            };
        }

        /// <summary>
        /// Removes specific effects from the effect engine.
        /// </summary>
        /// <param name="effectArray">Array of effects to remove</param>
        public void RemoveEffectFromEngine(List<object> effectArray)
        {
            if (effectArray == null || effectArray.Count == 0) return;

            game.EffectEngine.UnapplyAndRemove((System.Func<GameEffect, bool>)(effect => effectArray.Contains(effect)));
            
            // Remove from our active effects tracking
            foreach (var effect in effectArray)
            {
                activeEffects.Remove(effect);
            }
        }

        /// <summary>
        /// Removes all lasting effects originating from this source.
        /// </summary>
        public void RemoveLastingEffects()
        {
            if (game?.EffectEngine != null)
            {
                game.EffectEngine.RemoveLastingEffects(this);
            }
            
            // Clear our tracking list since all effects from this source are removed
            activeEffects?.Clear();
        }

        /// <summary>
        /// Gets all active effects from this source
        /// </summary>
        /// <returns>List of active effects</returns>
        public List<object> GetActiveEffects()
        {
            return activeEffects.ToList();
        }

        /// <summary>
        /// Removes a specific effect from tracking (called by effect engine)
        /// </summary>
        /// <param name="effect">Effect to stop tracking</param>
        public void StopTrackingEffect(object effect)
        {
            activeEffects.Remove(effect);
        }

        /// <summary>
        /// Checks if this source has any active effects
        /// </summary>
        /// <returns>True if there are active effects</returns>
        public bool HasActiveEffects()
        {
            return activeEffects.Count > 0;
        }

        /// <summary>
        /// Gets the number of active effects from this source
        /// </summary>
        /// <returns>Number of active effects</returns>
        public int GetActiveEffectCount()
        {
            return activeEffects.Count;
        }
        
        /// <summary>
        /// Gets a short summary of this effect source for display purposes
        /// </summary>
        /// <returns>Short summary string</returns>
        public string GetShortSummary()
        {
            return name ?? "Unknown Effect Source";
        }

        /// <summary>
        /// Creates a temporary effect that lasts for a specific number of rounds
        /// </summary>
        /// <param name="rounds">Number of rounds the effect should last</param>
        /// <param name="propertyFactory">Function that creates effect properties</param>
        public void ForRounds(int rounds, System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.Custom;
            properties.location = Locations.Any;
            
            // Add until condition for specific number of rounds
            int currentRound = game.roundNumber;
            properties.until = new Dictionary<string, object>
            {
                {EventNames.OnRoundEnded, (System.Func<Dictionary<string, object>, bool>)(eventData =>
                {
                    return game.roundNumber >= currentRound + rounds;
                })}
            };
            
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Creates an effect that lasts until a specific condition is met
        /// </summary>
        /// <param name="condition">Condition function that returns true when effect should end</param>
        /// <param name="propertyFactory">Function that creates effect properties</param>
        public void UntilCondition(System.Func<bool> condition, System.Func<AbilityDsl, EffectProperties> propertyFactory)
        {
            var properties = propertyFactory(new AbilityDsl());
            properties.duration = Durations.Custom;
            properties.location = Locations.Any;
            
            // Check condition on each game state change
            properties.until = new Dictionary<string, object>
            {
                {EventNames.OnGameStateChanged, (System.Func<Dictionary<string, object>, bool>)(eventData => condition())}
            };
            
            AddEffectToEngine(properties);
        }

        /// <summary>
        /// Cleanup when the effect source is destroyed
        /// </summary>
        protected virtual void OnDestroy()
        {
            RemoveLastingEffects();
        }

        /// <summary>
        /// Debug method to log all active effects from this source
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogActiveEffects()
        {
            Debug.Log($"🔮 {name} has {activeEffects.Count} active effects:");
            for (int i = 0; i < activeEffects.Count; i++)
            {
                Debug.Log($"  {i + 1}. {activeEffects[i]?.GetType().Name ?? "null"}");
            }
        }
    }


    /// <summary>
    /// Interface for effect factories
    /// </summary>
    public interface IEffectFactory
    {
        object Create(Game game, EffectSource source, EffectProperties properties);
    }

    /// <summary>
    /// AbilityDsl class for creating effects (simplified version)
    /// </summary>
    public class AbilityDsl
    {
        public EffectDsl effects => new EffectDsl();
        public LimitDsl limit => new LimitDsl();
        public CostDsl costs => new CostDsl();
        public TargetDsl targets => new TargetDsl();
    }

    /// <summary>
    /// Effect creation helpers
    /// </summary>
    public class EffectDsl
    {
        public object ModifySkill(int amount) => new SkillModifierEffect(amount);
        public object ModifyHonor(int amount) => new HonorModifierEffect(amount);
        public object ModifyFate(int amount) => new FateModifierEffect(amount);
        public object AddTrait(string trait) => new AddTraitEffect(trait);
        public object RemoveTrait(string trait) => new RemoveTraitEffect(trait);
        public object AddFaction(string faction) => new AddFactionEffect(faction);
        public object Blank() => new BlankEffect();
        public object CannotParticipateInConflicts() => new CannotParticipateEffect();
        public object CannotBeDeclaredAsAttacker() => new CannotAttackEffect();
        public object CannotBeDeclaredAsDefender() => new CannotDefendEffect();
    }

    /// <summary>
    /// Limit creation helpers
    /// </summary>
    public class LimitDsl
    {
        public object PerRound(int times) => new PerRoundLimit(times);
        public object PerPhase(int times) => new PerPhaseLimit(times);
        public object PerConflict(int times) => new PerConflictLimit(times);
        public object Unlimited() => new UnlimitedLimit();
    }

    /// <summary>
    /// Cost creation helpers
    /// </summary>
    public class CostDsl
    {
        public object Fate(int amount) => new FateCost(amount);
        public object Honor(int amount) => new HonorCost(amount);
        public object Bow() => new BowCost();
        public object Sacrifice() => new SacrificeCost();
    }

    /// <summary>
    /// Target creation helpers
    /// </summary>
    public class TargetDsl
    {
        public object Self() => new SelfTarget();
        public object Character() => new CharacterTarget();
        public object Attachment() => new AttachmentTarget();
        public object Ring() => new RingTarget();
    }

    // Effect implementation classes (simplified)
    public class SkillModifierEffect { public int Amount { get; } public SkillModifierEffect(int amount) { Amount = amount; } }
    public class HonorModifierEffect { public int Amount { get; } public HonorModifierEffect(int amount) { Amount = amount; } }
    public class FateModifierEffect { public int Amount { get; } public FateModifierEffect(int amount) { Amount = amount; } }
    public class AddTraitEffect { public string Trait { get; } public AddTraitEffect(string trait) { Trait = trait; } }
    public class RemoveTraitEffect { public string Trait { get; } public RemoveTraitEffect(string trait) { Trait = trait; } }
    public class AddFactionEffect { public string Faction { get; } public AddFactionEffect(string faction) { Faction = faction; } }
    public class BlankEffect { }
    public class CannotParticipateEffect { }
    public class CannotAttackEffect { }
    public class CannotDefendEffect { }

    // Limit implementation classes
    public class PerRoundLimit { public int Times { get; } public PerRoundLimit(int times) { Times = times; } }
    public class PerPhaseLimit { public int Times { get; } public PerPhaseLimit(int times) { Times = times; } }
    public class PerConflictLimit { public int Times { get; } public PerConflictLimit(int times) { Times = times; } }
    public class UnlimitedLimit { }

    // Cost implementation classes - removed FateCost and HonorCost duplicates (using GameCosts.cs versions)
    public class BowCost { }
    public class SacrificeCost { }

    // Target implementation classes
    public class SelfTarget { }
    public class CharacterTarget { }
    public class AttachmentTarget { }
    public class RingTarget { }


    /// <summary>
    /// Additional event names for EffectSource
    /// </summary>
    public static partial class EventNames
    {
        public const string OnGameStateChanged = "onGameStateChanged";
        public const string OnPassPriority = "onPassPriority";
        public const string OnDuelEnded = "onDuelEnded";
        // OnHonorDialsRevealed is now defined in EventNames.cs
    }
    
    /// <summary>
    /// Effect names for various game effects
    /// </summary>
    public static partial class EffectNames
    {
        public const string MustBeDeclaredAsAttacker = "mustBeDeclaredAsAttacker";
        public const string MustBeDeclaredAsDefender = "mustBeDeclaredAsDefender";
    }
}