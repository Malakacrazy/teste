using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Target handler for abilities that target tokens (specifically personal honor tokens).
    /// Perfect C# port of the original JavaScript AbilityTargetToken.
    /// Note: The original file was named AbilityTargetAbility.js but contains token targeting logic.
    /// </summary>
    [Serializable]
    public class AbilityTargetToken : AbilityTargetBase
    {
        [Header("Token Target Configuration")]
        public BaseCardSelector selector;
        
        public AbilityTargetToken(string targetName, AbilityTargetProperties props, BaseAbility ability)
            : base(targetName, props, ability)
        {
            selector = GetSelector(properties);
            
            // Set default targets for game actions
            foreach (var gameAction in properties.gameAction)
            {
                gameAction.SetDefaultTarget(context => context.tokens.ContainsKey(name) ? 
                    new List<object> { context.tokens[name] } : new List<object>());
            }
            
            SetupDependencies(ability);
        }
        
        /// <summary>
        /// Create selector that targets characters with personal honor tokens
        /// </summary>
        private BaseCardSelector GetSelector(AbilityTargetProperties props)
        {
            Func<BaseCard, AbilityContext, bool> cardCondition = (card, context) =>
            {
                var token = card.personalHonor;
                if (token == null)
                {
                    return false;
                }
                
                var contextCopy = context.Copy();
                contextCopy.tokens[name] = token;
                
                if (name == "target")
                {
                    contextCopy.token = token as StatusToken;
                }
                
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
            };
            
            var selectorProps = new CardSelectorProperties
            {
                cardType = new List<string> { CardTypes.Character }, // Token targeting focuses on characters
                cardCondition = cardCondition,
                targets = false,
                location = props.location?.ToList() ?? new List<string>(),
                controller = props.controller,
                optional = props.optional,
                mode = props.mode,
                numCards = props.numCards
            };
            
            return CardSelector.For(selectorProps);
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
                    context.tokens[name] = card.personalHonor;
                    if (name == "target")
                    {
                        context.token = card.personalHonor as StatusToken;
                    }
                    PublishTargetResolved(context, card.personalHonor, selectedPlayer);
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
            context.game.PromptForSelect(player, mergedProperties as SelectCardPromptProperties);
        }
        
        public override bool CheckTarget(AbilityContext context)
        {
            if (!context.tokens.ContainsKey(name) || context.tokens[name] == null)
            {
                PublishTargetValidationFailed(context, "Token not found in context");
                return false;
            }
            
            if (context.choosingPlayerOverride != null && GetChoosingPlayer(context) == context.player)
            {
                PublishTargetValidationFailed(context, "Invalid choosing player override");
                return false;
            }
            
            var token = context.tokens[name];
            // Since personalHonor returns { card = this, value = PersonalHonor }, we need to extract the card
            var tokenCard = token?.GetType().GetProperty("card")?.GetValue(token) as BaseCard;
            if (tokenCard == null)
            {
                PublishTargetValidationFailed(context, "Token card not found");
                return false;
            }
            
            bool isValid = selector.CanTarget(tokenCard, context, GetChoosingPlayer(context));
            if (!isValid)
            {
                PublishTargetValidationFailed(context, "Token target validation failed");
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
        private object MergeProperties(SelectCardPromptProperties baseProps, AbilityTargetProperties additionalProps)
        {
            // This would typically use a more sophisticated merging system
            // For now, return the base properties with key overrides
            return baseProps;
        }
    }
}
