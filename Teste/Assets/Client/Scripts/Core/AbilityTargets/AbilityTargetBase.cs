using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        
        protected AbilityTargetBase(string targetName, AbilityTargetProperties props, BaseAbility ability)
        {
            name = targetName ?? throw new ArgumentNullException(nameof(targetName));
            properties = props ?? throw new ArgumentNullException(nameof(props));
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
