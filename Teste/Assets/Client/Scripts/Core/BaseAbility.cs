using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace L5RGame
{
    /// <summary>
    /// Base ability class for card abilities
    /// </summary>
    public partial class BaseAbility : MonoBehaviour
    {
        [Header("Ability Properties")]
        public string title = "";
        public string abilityType = AbilityTypes.Action;
        public AbilityLimit limit;
        public List<ICost> cost = new List<ICost>();
        public bool cannotTargetFirst = false;
        public int max = 0;
        public string maxIdentifier;
        public int Priority = 0;
        public bool collectiveTrigger = false;
        
        [Header("References")]
        public Game game;
        public BaseCard card;
        public EffectSource source;
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        
        [Header("Ability Functions")]
        public Func<AbilityContext, bool> condition;
        public Func<AbilityContext, object> target;
        public Action<AbilityContext> effect;
        public Action<AbilityContext> handler;
        
        // Property aliases for API compatibility
        public string MaxIdentifier => maxIdentifier;
        public int Max => max;
        
        // Virtual properties that can be overridden
        public virtual string Title => title;
        public virtual bool CannotTargetFirst => cannotTargetFirst;
        public virtual int DefaultPriority => Priority;
        
        // Methods
        public virtual void DisplayMessage(AbilityContext context, string message)
        {
            context?.Game?.AddMessage($"{title}: {message}");
        }
        
        // Missing methods for compilation
        public virtual void SetTargetConfiguration(Dictionary<string, object> config)
        {
            // Placeholder implementation for target configuration
        }
        
        public virtual void SetTargetConfiguration(TargetConfiguration config)
        {
            // Convert TargetConfiguration to dictionary for compatibility
            var dict = new Dictionary<string, object>
            {
                ["Mode"] = config.Mode,
                ["ActivePromptTitle"] = config.ActivePromptTitle,
                ["Source"] = config.Source,
                ["CardTypeFilter"] = config.CardTypeFilter,
                ["AllowCancel"] = config.AllowCancel,
                ["MaxTargets"] = config.MaxTargets,
                ["MinTargets"] = config.MinTargets,
                ["LocationFilter"] = config.LocationFilter,
                ["ControllerFilter"] = config.ControllerFilter,
                ["TargetingType"] = config.TargetingType
            };
            
            // Add custom properties
            foreach (var kvp in config.Properties)
            {
                dict[kvp.Key] = kvp.Value;
            }
            
            SetTargetConfiguration(dict);
        }
        
        public virtual void CompleteExecution(AbilityContext context)
        {
            // Placeholder implementation for completing ability execution
            DisplayMessage(context, "Ability execution completed");
        }

        
        public BaseAbility() 
        {
            // Default empty constructor
        }
        
        public BaseAbility(Game game, EffectSource source, Dictionary<string, object> properties)
        {
            this.game = game;
            this.source = source;
            Initialize(game, source, properties);
        }
        
        public BaseAbility(Game game, BaseCard card, BaseAbilityProperties properties)
        {
            this.game = game;
            this.card = card;
            this.source = card;
            InitializeFromProperties(properties);
        }
        
        public BaseAbility(Game game, BaseCard card, CardAbilityProperties properties)
        {
            this.game = game;
            this.card = card;
            this.source = card;
            InitializeFromCardProperties(properties);
        }
        
        public virtual void InitializeFromProperties(BaseAbilityProperties properties)
        {
            if (properties == null) return;
            
            // Set basic properties
            condition = properties.condition;
            handler = properties.handler;
            cost = properties.cost?.Cast<ICost>().ToList() ?? new List<ICost>();
            target = properties.target as Func<AbilityContext, object>;
            targets = properties.targets ?? new Dictionary<string, object>();
            abilityType = properties.abilityType ?? AbilityTypes.Action;
            cannotTargetFirst = properties.optional;
            limit = properties.limit;
        }
        
        public virtual void InitializeFromCardProperties(CardAbilityProperties properties)
        {
            if (properties == null) return;
            
            // Set basic properties from CardAbilityProperties
            title = properties.title;
            condition = properties.condition;
            handler = properties.handler;
            cost = properties.cost?.Cast<ICost>().ToList() ?? new List<ICost>();
            target = properties.target as Func<AbilityContext, object>;
            targets = properties.targets ?? new Dictionary<string, object>();
            abilityType = properties.abilityType ?? AbilityTypes.Action;
            cannotTargetFirst = properties.cannotTargetFirst;
            limit = properties.limit as AbilityLimit;
        }
        
        public virtual List<object> GetGameActions(AbilityContext context)
        {
            // This should return game actions for the ability
            return new List<object>();
        }
        
        public virtual void Initialize(Game game, EffectSource source, Dictionary<string, object> properties)
        {
            if (properties == null) return;
            
            // Set basic properties
            if (properties.ContainsKey("title"))
                title = properties["title"] as string ?? "";
            if (properties.ContainsKey("abilityType"))
                abilityType = properties["abilityType"] as string ?? AbilityTypes.Action;
            if (properties.ContainsKey("limit"))
                limit = properties["limit"] as AbilityLimit;
            if (properties.ContainsKey("cost"))
                cost = properties["cost"] as List<ICost> ?? new List<ICost>();
            if (properties.ContainsKey("cannotTargetFirst"))
                cannotTargetFirst = (bool)(properties["cannotTargetFirst"] ?? false);
            if (properties.ContainsKey("max"))
                max = (int)(properties["max"] ?? 0);
            if (properties.ContainsKey("maxIdentifier"))
                maxIdentifier = properties["maxIdentifier"] as string;
                
            // Set functional properties
            if (properties.ContainsKey("condition"))
                condition = properties["condition"] as Func<AbilityContext, bool>;
            if (properties.ContainsKey("target"))
                target = properties["target"] as Func<AbilityContext, object>;
            if (properties.ContainsKey("effect"))
                effect = properties["effect"] as Action<AbilityContext>;
            if (properties.ContainsKey("handler"))
                handler = properties["handler"] as Action<AbilityContext>;
        }
        
        public virtual void Initialize(BaseCard card, Game game)
        {
            this.card = card;
            this.game = game;
            this.source = card;
        }
        
        public virtual void ExecuteAbility(AbilityContext context)
        {
            Execute(context);
        }
        
        // Virtual methods that can be overridden
        public virtual bool IsCardAbility() { return true; }
        public virtual bool IsCardPlayed() { return false; }
        public virtual bool IsTriggeredAbility() { return abilityType != AbilityTypes.Action; }
        public virtual bool IsKeywordAbility() { return title?.ToLower().Contains("keyword") ?? false; }
        public virtual bool HasLegalTargets(AbilityContext context) { return true; }
        public virtual bool CheckAllTargets(AbilityContext context) { return true; }
        public virtual bool IsInValidLocation(AbilityContext context) 
        { 
            // Check if the ability can be triggered from its current location
            return card?.location != null && card.location != Locations.Limbo;
        }
        
        public virtual TargetResults ResolveTargets(AbilityContext context) 
        { 
            var results = new TargetResults();
            
            if (target != null)
            {
                try
                {
                    var targetResult = target(context);
                    results.targets.Add("default", targetResult);
                    results.success = true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error resolving targets for {title}: {e.Message}");
                    results.success = false;
                }
            }
            else
            {
                results.success = true;
            }
            
            return results;
        }
        
        public virtual void ResolveCosts(AbilityContext context, CostResults results) 
        {
            if (cost == null || cost.Count == 0)
            {
                results.success = true;
                return;
            }
            
            try
            {
                foreach (var costItem in cost)
                {
                    if (costItem.CanPay(context))
                    {
                        costItem.Pay(context);
                    }
                    else
                    {
                        results.success = false;
                        return;
                    }
                }
                results.success = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error resolving costs for {title}: {e.Message}");
                results.success = false;
            }
        }
        
        public virtual TargetResults ResolveRemainingTargets(AbilityContext context, TargetResults results) 
        { 
            return results; 
        }
        
        public virtual void DisplayMessage(AbilityContext context) 
        {
            if (!string.IsNullOrEmpty(title))
            {
                context.game.AddMessage("{0} uses {1}", context.player, title);
            }
        }
        
        public virtual void Execute(AbilityContext context)
        {
            ExecuteHandler(context);
        }
        
        public virtual void ExecuteHandler(AbilityContext context) 
        {
            try
            {
                if (handler != null)
                {
                    handler(context);
                }
                else if (effect != null)
                {
                    effect(context);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing ability {title}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get the reduced cost for this ability in the given context
        /// </summary>
        public virtual int GetReducedCost(AbilityContext context)
        {
            // Base implementation returns 0 - abilities can override this
            // TODO: Implement proper cost reduction logic based on cost reducers and game state
            return 0;
        }
        
        public virtual bool CanExecute(AbilityContext context)
        {
            if (condition != null)
            {
                try
                {
                    return condition(context);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error checking condition for {title}: {e.Message}");
                    return false;
                }
            }
            return true;
        }
        
        // Limit checking
        public virtual bool IsAtMax(Player player)
        {
            return limit?.IsAtMax(player) ?? false;
        }
        
        public virtual void IncrementLimit(Player player)
        {
            limit?.Increment(player);
        }
        
        public virtual void ResetLimit()
        {
            limit?.Reset();
        }
        
        // Utility methods
        public string GetTitle()
        {
            return !string.IsNullOrEmpty(title) ? title : "Untitled Ability";
        }
        
        /// <summary>
        /// Create a new ability context for this ability
        /// </summary>
        /// <param name="player">Player triggering the ability</param>
        /// <returns>New ability context</returns>
        public virtual AbilityContext CreateContext(Player player)
        {
            return AbilityContext.CreateContext(this, player);
        }
        
        /// <summary>
        /// Try to execute this ability with the given context
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if the ability was executed successfully</returns>
        public virtual bool TryExecute(AbilityContext context)
        {
            try
            {
                if (!CanExecute(context))
                {
                    return false;
                }
                
                ExecuteAbility(context);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error executing ability {GetTitle()}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Check if this ability requires a target
        /// </summary>
        public virtual bool RequiresTarget => false;
        
        public override string ToString()
        {
            return $"BaseAbility[{abilityType}]: {GetTitle()}";
        }
    }
}
