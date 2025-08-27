using UnityEngine;
using System.Collections.Generic;
using System;

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
        
        [Header("Ability Functions")]
        public Func<AbilityContext, bool> condition;
        public Func<AbilityContext, object> target;
        public Action<AbilityContext> effect;
        public Action<AbilityContext> handler;
        
        public BaseAbility() 
        {
            // Default empty constructor
        }
        
        public BaseAbility(Game game, EffectSource source, Dictionary<string, object> properties)
        {
            Initialize(game, source, properties);
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
        
        // Virtual methods that can be overridden
        public virtual bool IsCardAbility() { return true; }
        public virtual bool IsCardPlayed() { return false; }
        public virtual bool IsTriggeredAbility() { return abilityType != AbilityTypes.Action; }
        public virtual bool HasLegalTargets(AbilityContext context) { return true; }
        public virtual bool CheckAllTargets(AbilityContext context) { return true; }
        
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
        
        public override string ToString()
        {
            return $"BaseAbility[{abilityType}]: {GetTitle()}";
        }
    }
    

}
