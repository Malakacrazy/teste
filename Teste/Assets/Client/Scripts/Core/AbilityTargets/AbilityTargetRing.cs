using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Target handler for abilities that target rings.
    /// Perfect C# port of the original JavaScript AbilityTargetRing.
    /// </summary>
    [Serializable]
    public class AbilityTargetRing : AbilityTargetBase
    {
        [Header("Ring Target Configuration")]
        public Func<Ring, AbilityContext, bool> ringCondition;
        
        public AbilityTargetRing(string targetName, AbilityTargetProperties props, BaseAbility ability)
            : base(targetName, props, ability)
        {
            ringCondition = CreateRingCondition(props);
            
            // Set default targets for game actions
            foreach (var gameAction in properties.gameAction)
            {
                gameAction.SetDefaultTarget(context => context.rings.ContainsKey(name) ? 
                    new List<object> { context.rings[name] } : new List<object>());
            }
            
            SetupDependencies(ability);
        }
        
        /// <summary>
        /// Create ring condition that includes all validation logic
        /// </summary>
        private Func<Ring, AbilityContext, bool> CreateRingCondition(AbilityTargetProperties props)
        {
            return (ring, context) =>
            {
                var contextCopy = context.Copy();
                contextCopy.rings[name] = ring;
                
                if (name == "target")
                {
                    contextCopy.ring = ring;
                }
                
                // Check dependent cost at PreTarget stage
                if (context.stage == Stages.PreTarget && dependentCost != null && !dependentCost.CanPay(contextCopy))
                {
                    return false;
                }
                
                // Check all conditions
                bool gameActionMet = props.gameAction.Count == 0 || props.gameAction.Any(gameAction => gameAction.HasLegalTarget(contextCopy));
                bool ringConditionMet = props.ringCondition == null || props.ringCondition(ring, contextCopy);
                bool dependentTargetMet = dependentTarget == null || dependentTarget.HasLegalTarget(contextCopy);
                
                return gameActionMet && ringConditionMet && dependentTargetMet;
            };
        }
        
        public override bool CanResolve(AbilityContext context)
        {
            return !string.IsNullOrEmpty(properties.dependsOn) || HasLegalTarget(context);
        }
        
        public override bool HasLegalTarget(AbilityContext context)
        {
            if (context?.game?.rings == null)
                return false;
            
            // Match JavaScript logic with underscore _.any()
            return context.game.rings.Values.Any(ring => ringCondition(ring, context));
        }
        
        public override List<GameAction> GetGameAction(AbilityContext context)
        {
            return properties.gameAction.Where(gameAction => gameAction.HasLegalTarget(context)).ToList();
        }
        
        public override List<object> GetAllLegalTargets(AbilityContext context)
        {
            if (context?.game?.rings == null)
                return new List<object>();
            
            // Match JavaScript logic with underscore _.filter()
            return context.game.rings.Values.Where(ring => ringCondition(ring, context)).Cast<object>().ToList();
        }
        
        public override void Resolve(AbilityContext context, TargetResults targetResults)
        {
            if (targetResults.cancelled || targetResults.payCostsFirst || targetResults.delayTargeting != null)
            {
                return;
            }
            
            var player = context.choosingPlayerOverride ?? GetChoosingPlayer(context);
            
            if (player == context.player.Opponent && context.stage == Stages.PreTarget)
            {
                targetResults.delayTargeting = this;
                return;
            }
            
            var buttons = new List<MenuOption>();
            string waitingPromptTitle = "";
            
            if (context.stage == Stages.PreTarget)
            {
                if (!targetResults.noCostsFirstButton)
                {
                    buttons.Add(new MenuOption { text = "Pay costs first", arg = "costsFirst" });
                }
                buttons.Add(new MenuOption { text = "Cancel", arg = "cancel" });
                
                if (context.ability.abilityType == "action")
                {
                    waitingPromptTitle = "Waiting for opponent to take an action or pass";
                }
                else
                {
                    waitingPromptTitle = "Waiting for opponent";
                }
            }
            
            var promptProperties = new RingSelectPromptProperties
            {
                waitingPromptTitle = waitingPromptTitle,
                context = context,
                buttons = buttons.Cast<object>().ToList(),
                onSelect = (selectedPlayer, ring) =>
                {
                    context.rings[name] = ring;
                    if (name == "target")
                    {
                        context.ring = ring;
                    }
                    PublishTargetResolved(context, ring, selectedPlayer);
                    return true;
                },
                onCancel = () =>
                {
                    targetResults.cancelled = true;
                    return true;
                },
                onMenuCommand = (selectedPlayer, arg) =>
                {
                    if (arg == "costsFirst")
                    {
                        targetResults.payCostsFirst = true;
                        return true;
                    }
                    return true;
                }
            };
            
            // Merge with additional properties
            var mergedProperties = MergeProperties(promptProperties, properties);
            context.game.PromptForRingSelect(player, mergedProperties as Dictionary<string, object>);
        }
        
        public override bool CheckTarget(AbilityContext context)
        {
            if (!context.rings.ContainsKey(name))
            {
                PublishTargetValidationFailed(context, "Ring not found in context");
                return false;
            }
            
            if (context.choosingPlayerOverride != null && GetChoosingPlayer(context) == context.player)
            {
                PublishTargetValidationFailed(context, "Invalid choosing player override");
                return false;
            }
            
            var rings = context.rings[name];
            
            // Handle optional case with empty selection
            if (properties.optional && rings == null)
            {
                return true;
            }
            
            if (rings == null)
            {
                PublishTargetValidationFailed(context, "Ring target is null");
                return false;
            }
            
            bool isValid = properties.ringCondition == null || properties.ringCondition(rings as Ring, context);
            if (!isValid)
            {
                PublishTargetValidationFailed(context, "Ring target condition failed");
            }
            
            return isValid;
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context)
        {
            if (properties.gameAction.Any(action => action.HasTargetsChosenByInitiatingPlayer(context)))
            {
                return true;
            }
            
            return GetChoosingPlayer(context) == context.player;
        }
        
        /// <summary>
        /// Merge properties for prompt creation
        /// </summary>
        private object MergeProperties(RingSelectPromptProperties baseProps, AbilityTargetProperties additionalProps)
        {
            // This would typically use a more sophisticated merging system
            // For now, return the base properties with key overrides
            return baseProps;
        }
    }
    
    /// <summary>
    /// Properties for ring selection prompts
    /// </summary>
    [Serializable]
    public class RingSelectPromptProperties
    {
        public string waitingPromptTitle;
        public AbilityContext context;
        public List<object> buttons;
        public Func<Player, Ring, bool> onSelect;
        public Func<bool> onCancel;
        public Func<Player, string, bool> onMenuCommand;
    }
}
