using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame
{
    /// <summary>
    /// Base class for all ability target handlers.
    /// Provides common functionality for targeting different object types.
    /// </summary>
    [Serializable]
    public abstract class AbilityTargetBase
    {
        [Header("Target Configuration")]
        public string name;
        public AbilityTargetProperties properties;
        
        [Header("Dependencies")]
        public AbilityTargetBase dependentTarget;
        public ICost dependentCost;
        
        [Header("Event System")]
        protected IEventBus eventBus;
        protected IUnifiedEventSystem unifiedEventSystem;
        
        protected AbilityTargetBase(string targetName, AbilityTargetProperties props, BaseAbility ability)
        {
            name = targetName ?? throw new ArgumentNullException(nameof(targetName));
            properties = props ?? throw new ArgumentNullException(nameof(props));
            
            // Initialize event system if ability has game reference
            if (ability?.game != null)
            {
                InitializeEventSystem(ability.game);
            }
        }
        
        /// <summary>
        /// Initialize event system references for target event publishing
        /// </summary>
        /// <param name="game">Game instance</param>
        public virtual void InitializeEventSystem(Game game)
        {
            if (game != null)
            {
                eventBus = game.GetEventBus();
                unifiedEventSystem = game.GetUnifiedEventSystem();
            }
        }
        
        protected virtual void SetupDependencies(BaseAbility ability)
        {
            if (!string.IsNullOrEmpty(properties.dependsOn) && ability?.targets != null)
            {
                if (ability.targets.TryGetValue(properties.dependsOn, out var targetValue) && targetValue is AbilityTargetBase dependsOnTarget)
                {
                    dependsOnTarget.dependentTarget = this;
                }
            }
        }
        
        public abstract bool CanResolve(AbilityContext context);
        public abstract bool HasLegalTarget(AbilityContext context);
        public abstract List<GameAction> GetGameAction(AbilityContext context);
        public abstract List<object> GetAllLegalTargets(AbilityContext context);
        public abstract void Resolve(AbilityContext context, TargetResults targetResults);
        public abstract bool CheckTarget(AbilityContext context);
        public abstract bool HasTargetsChosenByInitiatingPlayer(AbilityContext context);
        
        public virtual Player GetChoosingPlayer(AbilityContext context)
        {
            var playerProp = properties.player;
            
            if (playerProp is Func<AbilityContext, string> playerFunc)
            {
                playerProp = playerFunc(context);
            }
            
            return playerProp?.ToString() == Players.Opponent ? context.player.Opponent : context.player;
        }
        
        public virtual bool CheckGameActionsForTargetsChosenByInitiatingPlayer(AbilityContext context)
        {
            return false; // Override in derived classes if needed
        }
        
        #region Event Publishing Helper Methods
        
        /// <summary>
        /// Get the target type name for event publishing
        /// </summary>
        protected virtual string GetTargetType()
        {
            return GetType().Name.Replace("AbilityTarget", "").ToLower();
        }
        
        /// <summary>
        /// Publish target resolved event
        /// </summary>
        protected virtual void PublishTargetResolved(AbilityContext context, object resolvedTarget, Player choosingPlayer, string targetMode = null)
        {
            if (eventBus == null) return;
            
            try
            {
                var targetResolvedEvent = new TargetResolvedEvent(
                    game: context.Game,
                    triggeredBy: context.player,
                    targetType: GetTargetType(),
                    targetName: name,
                    resolvedTarget: resolvedTarget,
                    choosingPlayer: choosingPlayer,
                    targetMode: targetMode ?? properties.mode,
                    context: context,
                    source: this
                );
                
                // Publish as Handler event (during target resolution)
                PublishEvent(targetResolvedEvent, TimingWindow.Handler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish target resolved event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish target validation failed event
        /// </summary>
        protected virtual void PublishTargetValidationFailed(AbilityContext context, string reason, string validationStage = "validation")
        {
            if (eventBus == null) return;
            
            try
            {
                var validationFailedEvent = new TargetValidationFailedEvent(
                    game: context.Game,
                    triggeredBy: context.player,
                    targetType: GetTargetType(),
                    targetName: name,
                    reason: reason,
                    validationStage: validationStage,
                    context: context,
                    source: this
                );
                
                PublishEvent(validationFailedEvent, TimingWindow.Handler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish target validation failed event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish multiple targets selected event
        /// </summary>
        protected virtual void PublishMultipleTargetsSelected(AbilityContext context, List<object> selectedTargets, Player choosingPlayer)
        {
            if (eventBus == null || selectedTargets.Count <= 1) return;
            
            try
            {
                var multipleTargetsEvent = new MultipleTargetsSelectedEvent(
                    game: context.Game,
                    triggeredBy: context.player,
                    targetType: GetTargetType(),
                    targetName: name,
                    selectedTargets: selectedTargets,
                    choosingPlayer: choosingPlayer,
                    source: this
                );
                
                PublishEvent(multipleTargetsEvent, TimingWindow.Handler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish multiple targets selected event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish target dependency failed event
        /// </summary>
        protected virtual void PublishTargetDependencyFailed(AbilityContext context, string dependencyName, string dependencyType, string reason)
        {
            if (eventBus == null) return;
            
            try
            {
                var dependencyFailedEvent = new TargetDependencyFailedEvent(
                    game: context.Game,
                    triggeredBy: context.player,
                    targetName: name,
                    dependencyName: dependencyName,
                    dependencyType: dependencyType,
                    reason: reason,
                    source: this
                );
                
                PublishEvent(dependencyFailedEvent, TimingWindow.Handler);
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish target dependency failed event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Publish an event through the unified event system with timing awareness
        /// </summary>
        protected virtual void PublishEvent<T>(T gameEvent, TimingWindow window = TimingWindow.Handler) where T : GameEvent
        {
            if (gameEvent == null) return;
            
            try
            {
                // Use unified system if available for timing-aware processing
                if (unifiedEventSystem != null)
                {
                    unifiedEventSystem.PublishAtTiming(gameEvent, window);
                }
                // Fall back to regular event bus
                else if (eventBus != null)
                {
                    eventBus.Publish(gameEvent);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to publish {typeof(T).Name}: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Properties for ability targets
    /// </summary>
    [Serializable]
    public class AbilityTargetProperties
    {
        public List<string> cardType = new List<string>();
        public List<string> location = new List<string>();
        public string controller = Players.Any;
        public bool optional = false;
        public string mode = TargetModes.Single;
        public int numCards = 1;
        public bool targets = false;
        public string dependsOn = null;
        
        public Func<BaseCard, AbilityContext, bool> cardCondition;
        public Func<BaseAbility, bool> abilityCondition;
        public Func<Ring, AbilityContext, bool> ringCondition;
        public Dictionary<string, object> choices = new Dictionary<string, object>();
        public List<GameAction> gameAction = new List<GameAction>();
        public object player = Players.Self;
        
        // UI Properties
        public string activePromptTitle;
        public string waitingPromptTitle;
        public bool noCostsFirstButton = false;
        public object source;
    }
}
