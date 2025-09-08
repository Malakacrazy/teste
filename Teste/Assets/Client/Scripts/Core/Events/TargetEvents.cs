using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when an ability target is successfully resolved
    /// </summary>
    [Serializable]
    public class TargetResolvedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public TargetResolvedEvent() : base() { }
        
        /// <summary>
        /// Type of target (card, ring, ability, token, choice)
        /// </summary>
        public string TargetType { get; private set; }
        
        /// <summary>
        /// Name of the target in the ability context
        /// </summary>
        public string TargetName { get; private set; }
        
        /// <summary>
        /// The resolved target object
        /// </summary>
        public object ResolvedTarget { get; private set; }
        
        /// <summary>
        /// The player who chose the target
        /// </summary>
        public Player ChoosingPlayer { get; private set; }
        
        /// <summary>
        /// Targeting mode used (single, multiple, auto, etc.)
        /// </summary>
        public string TargetMode { get; private set; }
        
        /// <summary>
        /// Ability context where targeting occurred
        /// </summary>
        public AbilityContext Context { get; private set; }
        
        public override string EventName => "target_resolved";
        
        /// <summary>
        /// Initialize target resolved event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the ability</param>
        /// <param name="targetType">Type of target</param>
        /// <param name="targetName">Target name in context</param>
        /// <param name="resolvedTarget">The resolved target object</param>
        /// <param name="choosingPlayer">Player who chose the target</param>
        /// <param name="targetMode">Targeting mode used</param>
        /// <param name="context">Ability context</param>
        /// <param name="source">Source of the targeting</param>
        public TargetResolvedEvent(Game game, Player triggeredBy, string targetType, string targetName,
            object resolvedTarget, Player choosingPlayer, string targetMode = null, 
            AbilityContext context = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            TargetType = targetType;
            TargetName = targetName;
            ResolvedTarget = resolvedTarget;
            ChoosingPlayer = choosingPlayer;
            TargetMode = targetMode;
            Context = context;
            
            // Add specific event data
            AddEventData("target_type", targetType);
            AddEventData("target_name", targetName);
            AddEventData("target_mode", targetMode ?? "unknown");
            AddEventData("choosing_player", choosingPlayer?.Name ?? "unknown");
            
            // Add target-specific data
            if (resolvedTarget is BaseCard card)
            {
                AddEventData("target_card_name", card.name);
                AddEventData("target_card_id", card.id);
                AddEventData("target_card_type", card.GetCardType());
            }
            else if (resolvedTarget is Ring ring)
            {
                AddEventData("target_ring_element", ring.element);
                AddEventData("target_ring_claimed", ring.claimed);
            }
            else if (resolvedTarget is BaseAbility ability)
            {
                AddEventData("target_ability_type", ability.abilityType);
                AddEventData("target_ability_title", ability.Title);
            }
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["target_type"] = TargetType;
            data["target_name"] = TargetName;
            data["target_mode"] = TargetMode ?? "unknown";
            data["choosing_player"] = ChoosingPlayer?.Name ?? "unknown";
            
            return data;
        }
    }
    
    /// <summary>
    /// Event published when target validation fails
    /// </summary>
    [Serializable]
    public class TargetValidationFailedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public TargetValidationFailedEvent() : base() { }
        
        /// <summary>
        /// Type of target that failed validation
        /// </summary>
        public string TargetType { get; private set; }
        
        /// <summary>
        /// Name of the target in the ability context
        /// </summary>
        public string TargetName { get; private set; }
        
        /// <summary>
        /// Reason for validation failure
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Stage where validation failed
        /// </summary>
        public string ValidationStage { get; private set; }
        
        /// <summary>
        /// Ability context where validation failed
        /// </summary>
        public AbilityContext Context { get; private set; }
        
        public override string EventName => "target_validation_failed";
        
        /// <summary>
        /// Initialize target validation failed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the ability</param>
        /// <param name="targetType">Type of target that failed</param>
        /// <param name="targetName">Target name in context</param>
        /// <param name="reason">Reason for failure</param>
        /// <param name="validationStage">Stage where validation failed</param>
        /// <param name="context">Ability context</param>
        /// <param name="source">Source of the targeting attempt</param>
        public TargetValidationFailedEvent(Game game, Player triggeredBy, string targetType, string targetName,
            string reason, string validationStage = "validation", AbilityContext context = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            TargetType = targetType;
            TargetName = targetName;
            Reason = reason;
            ValidationStage = validationStage;
            Context = context;
            
            // Add specific event data
            AddEventData("target_type", targetType);
            AddEventData("target_name", targetName);
            AddEventData("reason", reason);
            AddEventData("validation_stage", validationStage);
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["target_type"] = TargetType;
            data["target_name"] = TargetName;
            data["reason"] = Reason;
            data["validation_stage"] = ValidationStage;
            
            return data;
        }
    }
    
    /// <summary>
    /// Event published when multiple targets are selected for a single ability
    /// </summary>
    [Serializable]
    public class MultipleTargetsSelectedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public MultipleTargetsSelectedEvent() : base() { }
        
        /// <summary>
        /// Type of targets selected
        /// </summary>
        public string TargetType { get; private set; }
        
        /// <summary>
        /// Name of the target group in the ability context
        /// </summary>
        public string TargetName { get; private set; }
        
        /// <summary>
        /// Number of targets selected
        /// </summary>
        public int TargetCount { get; private set; }
        
        /// <summary>
        /// List of selected targets
        /// </summary>
        public List<object> SelectedTargets { get; private set; }
        
        /// <summary>
        /// The player who chose the targets
        /// </summary>
        public Player ChoosingPlayer { get; private set; }
        
        public override string EventName => "multiple_targets_selected";
        
        /// <summary>
        /// Initialize multiple targets selected event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the ability</param>
        /// <param name="targetType">Type of targets</param>
        /// <param name="targetName">Target name in context</param>
        /// <param name="selectedTargets">List of selected targets</param>
        /// <param name="choosingPlayer">Player who chose the targets</param>
        /// <param name="source">Source of the targeting</param>
        public MultipleTargetsSelectedEvent(Game game, Player triggeredBy, string targetType, string targetName,
            List<object> selectedTargets, Player choosingPlayer, object source = null) 
            : base(game, triggeredBy, source)
        {
            TargetType = targetType;
            TargetName = targetName;
            SelectedTargets = new List<object>(selectedTargets);
            TargetCount = selectedTargets.Count;
            ChoosingPlayer = choosingPlayer;
            
            // Add specific event data
            AddEventData("target_type", targetType);
            AddEventData("target_name", targetName);
            AddEventData("target_count", TargetCount);
            AddEventData("choosing_player", choosingPlayer?.Name ?? "unknown");
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["target_type"] = TargetType;
            data["target_name"] = TargetName;
            data["target_count"] = TargetCount;
            data["choosing_player"] = ChoosingPlayer?.Name ?? "unknown";
            
            return data;
        }
    }
    
    /// <summary>
    /// Event published when target dependencies are not met
    /// </summary>
    [Serializable]
    public class TargetDependencyFailedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public TargetDependencyFailedEvent() : base() { }
        
        /// <summary>
        /// Name of the target that failed dependency check
        /// </summary>
        public string TargetName { get; private set; }
        
        /// <summary>
        /// Name of the dependency that failed
        /// </summary>
        public string DependencyName { get; private set; }
        
        /// <summary>
        /// Type of dependency (target, cost)
        /// </summary>
        public string DependencyType { get; private set; }
        
        /// <summary>
        /// Reason for dependency failure
        /// </summary>
        public string Reason { get; private set; }
        
        public override string EventName => "target_dependency_failed";
        
        /// <summary>
        /// Initialize target dependency failed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who triggered the ability</param>
        /// <param name="targetName">Name of the target</param>
        /// <param name="dependencyName">Name of the failed dependency</param>
        /// <param name="dependencyType">Type of dependency</param>
        /// <param name="reason">Reason for failure</param>
        /// <param name="source">Source of the targeting attempt</param>
        public TargetDependencyFailedEvent(Game game, Player triggeredBy, string targetName,
            string dependencyName, string dependencyType, string reason, object source = null) 
            : base(game, triggeredBy, source)
        {
            TargetName = targetName;
            DependencyName = dependencyName;
            DependencyType = dependencyType;
            Reason = reason;
            
            // Add specific event data
            AddEventData("target_name", targetName);
            AddEventData("dependency_name", dependencyName);
            AddEventData("dependency_type", dependencyType);
            AddEventData("reason", reason);
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["target_name"] = TargetName;
            data["dependency_name"] = DependencyName;
            data["dependency_type"] = DependencyType;
            data["reason"] = Reason;
            
            return data;
        }
    }
}