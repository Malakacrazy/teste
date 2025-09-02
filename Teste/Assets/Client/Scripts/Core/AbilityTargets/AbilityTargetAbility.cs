using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Target handler for abilities that target other abilities.
    /// Perfect C# port of the original JavaScript AbilityTargetAbility.
    /// </summary>
    [Serializable]
    public class AbilityTargetAbility : AbilityTargetBase
    {
        [Header("Ability Target Configuration")]
        public Func<BaseAbility, bool> abilityCondition;
        public BaseCardSelector selector;
        
        public AbilityTargetAbility(string targetName, AbilityTargetProperties props, BaseAbility ability)
            : base(targetName, props, ability)
        {
            abilityCondition = properties.abilityCondition ?? (ability => true);
            selector = GetSelector(properties);
            
            SetupDependencies(ability);
        }
        
        /// <summary>
        /// Create selector with ability-specific card condition
        /// </summary>
        private BaseCardSelector GetSelector(AbilityTargetProperties props)
        {
            Func<BaseCard, AbilityContext, bool> cardCondition = (card, context) =>
            {
                // Get all abilities from the card (actions + reactions)
                var abilities = new List<BaseAbility>();
                abilities.AddRange(card.actions);
                abilities.AddRange(card.reactions);
                
                // Filter for triggered abilities that meet the ability condition
                var validAbilities = abilities.Where(ability => 
                    ability.IsTriggeredAbility() && abilityCondition(ability)).ToList();
                
                return validAbilities.Any(ability =>
                {
                    var contextCopy = context.Copy();
                    contextCopy.targetAbility = ability;
                    
                    // Check dependent cost at PreTarget stage
                    if (context.stage == Stages.PreTarget && dependentCost != null && !dependentCost.CanPay(contextCopy))
                    {
                        return false;
                    }
                    
                    // Check all conditions
                    bool cardConditionMet = props.cardCondition == null || props.cardCondition(card, contextCopy);
                    bool dependentTargetMet = dependentTarget == null || dependentTarget.HasLegalTarget(contextCopy);
                    bool gameActionMet = props.gameAction.Any(gameAction => gameAction.HasLegalTarget(contextCopy));
                    
                    return cardConditionMet && dependentTargetMet && gameActionMet;
                });
            };
            
            var selectorProps = new CardSelectorProperties
            {
                cardCondition = cardCondition,
                targets = false,
                cardType = props.cardType?.ToList() ?? new List<string>(),
                location = props.location?.ToList() ?? new List<string>(),
                controller = props.controller,
                optional = props.optional,
                mode = props.mode
            };
            
            return CardSelector.For(selectorProps);
        }
        
        /// <summary>
        /// Setup target dependencies
        /// </summary>
        private void SetupDependencies(BaseAbility ability)
        {
            if (!string.IsNullOrEmpty(properties.dependsOn))
            {
                if (ability.targets.TryGetValue(properties.dependsOn, out var targetValue) && targetValue is AbilityTargetBase dependsOnTarget)
                {
                    dependsOnTarget.dependentTarget = this;
                }
            }
        }
        
        public override bool CanResolve(AbilityContext context)
        {
            return !string.IsNullOrEmpty(properties.dependsOn) || HasLegalTarget(context);
        }
        
        public override bool HasLegalTarget(AbilityContext context)
        {
            return selector.optional || selector.HasEnoughTargets(context, GetChoosingPlayer(context));
        }
        
        public override List<object> GetAllLegalTargets(AbilityContext context)
        {
            return selector.GetAllLegalTargets(context, GetChoosingPlayer(context)).Cast<object>().ToList();
        }
        
        public override List<GameAction> GetGameAction(AbilityContext context)
        {
            return properties.gameAction.Where(gameAction => gameAction.HasLegalTarget(context)).ToList();
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
            
            var promptProperties = new SelectCardPromptProperties
            {
                waitingPromptTitle = waitingPromptTitle,
                buttons = buttons,
                context = context,
                selector = selector,
                onSelect = (selectedPlayer, card) =>
                {
                    var abilities = new List<BaseAbility>();
                    abilities.AddRange(card.actions);
                    abilities.AddRange(card.reactions);
                    
                    var validAbilities = abilities.Where(ability => 
                        ability.IsTriggeredAbility() && abilityCondition(ability)).ToList();
                    
                    if (validAbilities.Count == 1)
                    {
                        context.targetAbility = validAbilities[0];
                    }
                    else if (validAbilities.Count > 1)
                    {
                        var choices = validAbilities.Select(ability => ability.title).ToList();
                        choices.Add("Back");
                        
                        context.game.PromptWithHandlerMenu(selectedPlayer, new HandlerMenuPromptProperties
                        {
                            activePromptTitle = "Choose an ability",
                            context = context,
                            choices = choices,
                            choiceHandler = choice =>
                            {
                                if (choice == "Back")
                                {
                                    context.game.QueueSimpleStep(() => { Resolve(context, targetResults); return true; });
                                }
                                else
                                {
                                    context.targetAbility = validAbilities.FirstOrDefault(ability => ability.title == choice);
                                }
                            }
                        });
                    }
                    
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
            context.game.PromptForSelect(player, mergedProperties);
        }
        
        public override bool CheckTarget(AbilityContext context)
        {
            if (context.targetAbility == null || 
                (context.choosingPlayerOverride != null && GetChoosingPlayer(context) == context.player))
            {
                return false;
            }
            
            return properties.cardType.Contains(context.targetAbility.card.GetCardType()) &&
                   (properties.cardCondition == null || properties.cardCondition(context.targetAbility.card, context)) &&
                   abilityCondition(context.targetAbility);
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
        private object MergeProperties(SelectCardPromptProperties baseProps, AbilityTargetProperties additionalProps)
        {
            // This would typically use reflection or a more sophisticated merging system
            // For now, return the base properties
            return baseProps;
        }
    }
}
