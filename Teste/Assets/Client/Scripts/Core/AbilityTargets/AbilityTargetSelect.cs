using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Target handler for abilities that use choice-based selection.
    /// Perfect C# port of the original JavaScript AbilityTargetSelect.
    /// </summary>
    [Serializable]
    public class AbilityTargetSelect : AbilityTargetBase
    {
        [Header("Select Target Configuration")]
        public Dictionary<string, object> choices = new Dictionary<string, object>();
        
        public AbilityTargetSelect(string targetName, AbilityTargetProperties props, BaseAbility ability)
            : base(targetName, props, ability)
        {
            choices = props.choices ?? new Dictionary<string, object>();
            SetupDependencies(ability);
        }
        
        public override bool CanResolve(AbilityContext context)
        {
            return !string.IsNullOrEmpty(properties.dependsOn) || HasLegalTarget(context);
        }
        
        public override bool HasLegalTarget(AbilityContext context)
        {
            var keys = choices.Keys.ToList();
            return keys.Any(key => IsChoiceLegal(key, context));
        }
        
        /// <summary>
        /// Check if a specific choice key is legal in the current context
        /// </summary>
        public bool IsChoiceLegal(string key, AbilityContext context)
        {
            var contextCopy = context.Copy();
            contextCopy.selects[name] = new SelectChoice(key);
            
            if (name == "target")
            {
                contextCopy.select = key;
            }
            
            // Check dependent cost at PreTarget stage
            if (context.stage == Stages.PreTarget && dependentCost != null && !dependentCost.CanPay(contextCopy))
            {
                return false;
            }
            
            // Check dependent target
            if (dependentTarget != null && !dependentTarget.HasLegalTarget(contextCopy))
            {
                return false;
            }
            
            // Check the choice itself
            if (!choices.TryGetValue(key, out var choice))
                return false;
            
            if (choice is Func<AbilityContext, bool> choiceFunc)
            {
                try
                {
                    return choiceFunc(contextCopy);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Choice function error for key '{key}': {e.Message}");
                    return false;
                }
            }
            
            if (choice is GameAction choiceAction)
            {
                return choiceAction.HasLegalTarget(contextCopy);
            }
            
            return true; // Default to legal if we can't determine otherwise
        }
        
        public override List<GameAction> GetGameAction(AbilityContext context)
        {
            if (!context.selects.ContainsKey(name))
            {
                return new List<GameAction>();
            }
            
            var selectChoice = context.selects[name] as SelectChoice;
            if (selectChoice == null)
                return new List<GameAction>();
            
            if (!choices.TryGetValue(selectChoice.GetChoice(), out var choice))
                return new List<GameAction>();
            
            if (choice is Func<AbilityContext, bool>)
            {
                return new List<GameAction>(); // Functions don't return actions
            }
            
            if (choice is GameAction singleAction)
            {
                return new List<GameAction> { singleAction };
            }
            
            if (choice is IEnumerable<GameAction> multipleActions)
            {
                return multipleActions.ToList();
            }
            
            return new List<GameAction>();
        }
        
        public override List<object> GetAllLegalTargets(AbilityContext context)
        {
            return choices.Keys.Where(key => IsChoiceLegal(key, context)).Cast<object>().ToList();
        }
        
        public override void Resolve(AbilityContext context, TargetResults targetResults)
        {
            if (targetResults.cancelled || targetResults.payCostsFirst || targetResults.delayTargeting != null)
            {
                return;
            }
            
            var player = (properties.targets && context.choosingPlayerOverride != null) ? 
                         context.choosingPlayerOverride : GetChoosingPlayer(context);
            
            if (player == context.player.Opponent && context.stage == Stages.PreTarget)
            {
                targetResults.delayTargeting = this;
                return;
            }
            
            string promptTitle = properties.activePromptTitle ?? "Select one";
            
            var legalChoices = choices.Keys.Where(key => IsChoiceLegal(key, context)).ToList();
            
            var handlers = legalChoices.Select<string, Action>(choice =>
            {
                return () =>
                {
                    context.selects[name] = new SelectChoice(choice);
                    if (name == "target")
                    {
                        context.select = choice;
                    }
                };
            }).ToList();
            
            var choiceList = new List<string>(legalChoices);
            
            // Add control buttons for PreTarget stage
            if (player != context.player.Opponent && context.stage == Stages.PreTarget)
            {
                if (!targetResults.noCostsFirstButton)
                {
                    choiceList.Add("Pay costs first");
                    handlers.Add(() => targetResults.payCostsFirst = true);
                }
                
                choiceList.Add("Cancel");
                handlers.Add(() => targetResults.cancelled = true);
            }
            
            // Handle single choice or multiple choices
            if (handlers.Count == 1)
            {
                handlers[0]();
            }
            else if (handlers.Count > 1)
            {
                string waitingPromptTitle = "";
                
                if (context.stage == Stages.PreTarget)
                {
                    if (context.ability.abilityType == "action")
                    {
                        waitingPromptTitle = "Waiting for opponent to take an action or pass";
                    }
                    else
                    {
                        waitingPromptTitle = "Waiting for opponent";
                    }
                }
                
                context.game.PromptWithHandlerMenu(player, new AbilityTargetMenuPromptProperties
                {
                    waitingPromptTitle = waitingPromptTitle,
                    activePromptTitle = promptTitle,
                    context = context,
                    source = properties.source ?? context.source,
                    choices = choiceList,
                    handlers = handlers
                });
            }
        }
        
        public override bool CheckTarget(AbilityContext context)
        {
            if (properties.targets && context.choosingPlayerOverride != null && GetChoosingPlayer(context) == context.player)
            {
                return false;
            }
            
            if (!context.selects.ContainsKey(name) || context.selects[name] == null)
                return false;
            
            var selectChoice = context.selects[name] as SelectChoice;
            if (selectChoice == null) return false;
            return IsChoiceLegal(selectChoice.GetChoice(), context);
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context)
        {
            if (properties.targets)
            {
                return true;
            }
            
            var actions = choices.Values.Where(value => !(value is Func<AbilityContext, bool>)).ToList();
            return actions.Any(action => 
            {
                if (action is GameAction gameAction)
                    return gameAction.HasTargetsChosenByInitiatingPlayer(context);
                return false;
            });
        }
    }
    
    /// <summary>
    /// Properties for handler menu prompts
    /// </summary>
    [Serializable]
    public class AbilityTargetMenuPromptProperties
    {
        public string waitingPromptTitle;
        public string activePromptTitle;
        public AbilityContext context;
        public object source;
        public List<string> choices;
        public List<Action> handlers;
        public Action<string> choiceHandler;
    }
}
