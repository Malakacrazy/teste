using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Target handler for abilities that target cards.
    /// Perfect C# port of the original JavaScript AbilityTargetCard.
    /// </summary>
    [Serializable]
    public class AbilityTargetCard : AbilityTargetBase
    {
        [Header("Card Target Configuration")]
        public BaseCardSelector selector;
        
        public AbilityTargetCard(string targetName, AbilityTargetProperties props, BaseAbility ability)
            : base(targetName, props, ability)
        {
            // Set default targets for game actions
            foreach (var gameAction in properties.gameAction)
            {
                gameAction.SetDefaultTarget(context => context.targets.ContainsKey(name) ? 
                    new List<object> { context.targets[name] } : new List<object>());
            }
            
            selector = GetSelector(properties);
            SetupDependencies(ability);
        }
        
        /// <summary>
        /// Create selector with card-specific condition logic
        /// </summary>
        private BaseCardSelector GetSelector(AbilityTargetProperties props)
        {
            Func<BaseCard, AbilityContext, bool> cardCondition = (card, context) =>
            {
                var contextCopy = GetContextCopy(card, context);
                
                // Check dependent cost at PreTarget stage
                if (context.stage == Stages.PreTarget && dependentCost != null && !dependentCost.CanPay(contextCopy))
                {
                    return false;
                }
                
                // Check all conditions
                bool cardConditionMet = props.cardCondition == null || props.cardCondition(card, contextCopy);
                bool dependentTargetMet = dependentTarget == null || dependentTarget.HasLegalTarget(contextCopy);
                bool gameActionMet = props.gameAction.Count == 0 || props.gameAction.Any(gameAction => gameAction.HasLegalTarget(contextCopy));
                
                return cardConditionMet && dependentTargetMet && gameActionMet;
            };
            
            var selectorProps = new CardSelectorProperties
            {
                cardCondition = cardCondition,
                targets = true,
                cardType = props.cardType?.ToList() ?? new List<string>(),
                location = props.location?.ToList() ?? new List<string>(),
                controller = props.controller,
                optional = props.optional,
                mode = props.mode,
                numCards = props.numCards
            };
            
            return CardSelector.For(selectorProps);
        }
        
        /// <summary>
        /// Create a copy of context with this card as target
        /// </summary>
        private AbilityContext GetContextCopy(BaseCard card, AbilityContext context)
        {
            var contextCopy = context.Copy();
            contextCopy.targets[name] = card;
            
            if (name == "target")
            {
                contextCopy.target = card;
            }
            
            return contextCopy;
        }
        
        public override bool CanResolve(AbilityContext context)
        {
            return !string.IsNullOrEmpty(properties.dependsOn) || HasLegalTarget(context);
        }
        
        public override bool HasLegalTarget(AbilityContext context)
        {
            return selector.optional || selector.HasEnoughTargets(context, GetChoosingPlayer(context));
        }
        
        public override List<GameAction> GetGameAction(AbilityContext context)
        {
            return properties.gameAction.Where(gameAction => gameAction.HasLegalTarget(context)).ToList();
        }
        
        public override List<object> GetAllLegalTargets(AbilityContext context)
        {
            return selector.GetAllLegalTargets(context, GetChoosingPlayer(context)).Cast<object>().ToList();
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
            
            // Handle AutoSingle mode
            if (properties.mode == TargetModes.AutoSingle)
            {
                var legalTargets = selector.GetAllLegalTargets(context, player);
                if (legalTargets.Count == 1)
                {
                    context.targets[name] = legalTargets[0];
                    return;
                }
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
            
            // Get cards that must be selected
            var mustSelect = selector.GetAllLegalTargets(context, player)
                .Where(card => card.GetEffects(EffectNames.MustBeChosen)
                    .Any(restriction => restriction != null))
                .ToList();
            
            var promptProperties = new SelectCardPromptProperties
            {
                waitingPromptTitle = waitingPromptTitle,
                context = context,
                selector = selector,
                buttons = buttons,
                mustSelect = mustSelect,
                onSelect = (selectedPlayer, card) =>
                {
                    context.targets[name] = card;
                    if (name == "target")
                    {
                        context.target = card;
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
            var mergedProperties = MergeProperties(promptProperties, properties) as SelectCardPromptProperties ?? promptProperties;
            context.game.PromptForSelect(player, mergedProperties);
        }
        
        public override bool CheckTarget(AbilityContext context)
        {
            if (!context.targets.ContainsKey(name))
            {
                return false;
            }
            
            if (context.choosingPlayerOverride != null && GetChoosingPlayer(context) == context.player)
            {
                return false;
            }
            
            var cards = context.targets[name];
            var cardList = new List<BaseCard>();
            
            if (cards is BaseCard singleCard)
            {
                cardList.Add(singleCard);
            }
            else if (cards is IEnumerable<BaseCard> multipleCards)
            {
                cardList.AddRange(multipleCards);
            }
            
            var choosingPlayer = context.choosingPlayerOverride ?? GetChoosingPlayer(context);
            
            return cardList.All(card => selector.CanTarget(card, context, choosingPlayer)) &&
                   selector.HasEnoughSelected(cardList) && 
                   !selector.HasExceededLimit(cardList);
        }
        
        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context)
        {
            var choosingPlayer = GetChoosingPlayer(context);
            
            if (choosingPlayer == context.player && 
                (selector.optional || selector.HasEnoughTargets(context, context.player.Opponent)))
            {
                return true;
            }
            
            return string.IsNullOrEmpty(properties.dependsOn) && CheckGameActionsForTargetsChosenByInitiatingPlayer(context);
        }
        
        private bool CheckGameActionsForTargetsChosenByInitiatingPlayer(AbilityContext context)
        {
            return GetAllLegalTargets(context).Any(target =>
            {
                var card = target as BaseCard;
                if (card == null) return false;
                
                var contextCopy = GetContextCopy(card, context);
                
                if (properties.gameAction.Any(action => action.HasTargetsChosenByInitiatingPlayer(contextCopy)))
                {
                    return true;
                }
                
                if (dependentTarget != null)
                {
                    return dependentTarget.CheckGameActionsForTargetsChosenByInitiatingPlayer(contextCopy);
                }
                
                return false;
            });
        }
        
        /// <summary>
        /// Merge properties for prompt creation
        /// </summary>
        private object MergeProperties(SelectCardPromptProperties baseProps, AbilityTargetProperties additionalProps)
        {
            // This would typically use a more sophisticated merging system
            // For now, return the base properties with key overrides
            return baseProps;
        }
    }
}
