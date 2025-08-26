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
    
    /// <summary>
    /// Results of target resolution
    /// </summary>
    [System.Serializable]
    public class TargetResults
    {
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        public bool success = false;
        public string errorMessage = "";
        
        public T GetTarget<T>(string key) where T : class
        {
            if (targets.TryGetValue(key, out object target))
            {
                return target as T;
            }
            return null;
        }
        
        public void SetTarget(string key, object target)
        {
            targets[key] = target;
        }
        
        public bool HasTarget(string key)
        {
            return targets.ContainsKey(key);
        }
    }
    
    /// <summary>
    /// Results of cost resolution
    /// </summary>
    [System.Serializable]
    public class CostResults
    {
        public Dictionary<string, object> paidCosts = new Dictionary<string, object>();
        public bool success = false;
        public string errorMessage = "";
        
        public T GetCost<T>(string key) where T : class
        {
            if (paidCosts.TryGetValue(key, out object cost))
            {
                return cost as T;
            }
            return null;
        }
        
        public void SetCost(string key, object cost)
        {
            paidCosts[key] = cost;
        }
        
        public bool HasCost(string key)
        {
            return paidCosts.ContainsKey(key);
        }
    }
    
    /// <summary>
    /// Card action ability - actions that can be triggered
    /// </summary>
    public class CardAction : BaseAbility
    {
        public CardAction(Game game, BaseCard source, ActionProperties properties) 
            : base(game, source, ConvertProperties(properties))
        {
            abilityType = AbilityTypes.Action;
        }
        
        private static Dictionary<string, object> ConvertProperties(ActionProperties props)
        {
            return new Dictionary<string, object>
            {
                {"title", props.title},
                {"condition", props.condition},
                {"target", props.target},
                {"effect", props.effect},
                {"limit", props.limit}
            };
        }
        
        public override bool IsCardAbility() { return true; }
        public override bool IsTriggeredAbility() { return false; }
    }
    
    /// <summary>
    /// Triggered ability - reactions, interrupts, etc.
    /// </summary>
    public class TriggeredAbility : BaseAbility
    {
        [Header("Triggered Ability Properties")]
        public object when;
        public List<string> location = new List<string>();
        
        public TriggeredAbility(Game game, BaseCard source, string triggerType, TriggeredAbilityProperties properties)
            : base(game, source, ConvertProperties(properties))
        {
            abilityType = triggerType;
            when = properties.when;
            location = properties.location ?? new List<string> { Locations.PlayArea };
        }
        
        private static Dictionary<string, object> ConvertProperties(TriggeredAbilityProperties props)
        {
            return new Dictionary<string, object>
            {
                {"title", props.title},
                {"condition", props.condition},
                {"target", props.target},
                {"effect", props.effect},
                {"limit", props.limit}
            };
        }
        
        public override bool IsTriggeredAbility() { return true; }
        
        public virtual void RegisterEvents()
        {
            // Register for game events - placeholder
        }
        
        public virtual void UnregisterEvents()
        {
            // Unregister from game events - placeholder
        }
        
        public virtual bool IsTriggeredBy(object gameEvent)
        {
            // Check if this ability is triggered by the given event - placeholder
            return false;
        }
    }
    
    /// <summary>
    /// Custom play action - special ways to play cards
    /// </summary>
    public class CustomPlayAction : BaseAbility
    {
        public CustomPlayAction(CustomPlayActionProperties properties)
        {
            title = properties.title;
            condition = properties.condition as Func<AbilityContext, bool>;
            target = properties.target as Func<AbilityContext, object>;
            effect = properties.effect as Action<AbilityContext>;
        }
        
        public override bool IsCardPlayed() { return true; }
    }
    
    // Placeholder classes for play actions
    public class PlayCharacterAction : BaseAbility
    {
        public PlayCharacterAction(BaseCard card)
        {
            title = $"Play {card.name}";
            abilityType = "playCharacter";
        }
        
        public override bool IsCardPlayed() { return true; }
    }
    
    public class PlayDisguisedCharacterAction : BaseAbility
    {
        public PlayDisguisedCharacterAction(BaseCard card)
        {
            title = $"Play {card.name} (Disguised)";
            abilityType = "playDisguised";
        }
        
        public override bool IsCardPlayed() { return true; }
    }
    
    public class DynastyCardAction : BaseAbility
    {
        public DynastyCardAction(BaseCard card)
        {
            title = $"Play {card.name}";
            abilityType = "playDynasty";
        }
        
        public override bool IsCardPlayed() { return true; }
    }
    
    public class PlayAttachmentAction : BaseAbility
    {
        public PlayAttachmentAction(BaseCard card)
        {
            title = $"Play {card.name}";
            abilityType = "playAttachment";
        }
        
        public override bool IsCardPlayed() { return true; }
    }
    
    public class PlayAttachmentOnRingAction : BaseAbility
    {
        public PlayAttachmentOnRingAction(BaseCard card)
        {
            title = $"Play {card.name} on Ring";
            abilityType = "playAttachmentOnRing";
        }
        
        public override bool IsCardPlayed() { return true; }
    }
}
