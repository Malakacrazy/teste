using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface ISelectCardProperties : ICardActionProperties
    {
        string ActivePromptTitle { get; set; }
        string Player { get; set; }
        object CardType { get; set; } // Can be CardTypes or CardTypes[]
        string Controller { get; set; }
        object Location { get; set; } // Can be Locations or Locations[]
        Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
        bool Targets { get; set; }
        string Message { get; set; }
        Func<BaseCard, Player, ISelectCardProperties, object[]> MessageArgs { get; set; }
        GameAction GameAction { get; set; }
        string Selector { get; set; }
        TargetModes? Mode { get; set; }
        Func<BaseCard, object> SubActionProperties { get; set; }
        Action CancelHandler { get; set; }
    }

    public class SelectCardProperties : CardActionProperties, ISelectCardProperties
    {
        public string ActivePromptTitle { get; set; }
        public string Player { get; set; }
        public object CardType { get; set; }
        public string Controller { get; set; }
        public object Location { get; set; }
        public Func<BaseCard, AbilityContext, bool> CardCondition { get; set; }
        public bool Targets { get; set; }
        public string Message { get; set; }
        public Func<BaseCard, Player, ISelectCardProperties, object[]> MessageArgs { get; set; }
        public GameAction GameAction { get; set; }
        public BaseCardSelector Selector { get; set; }
        public TargetModes? Mode { get; set; }
        public Func<BaseCard, object> SubActionProperties { get; set; }
        public Action CancelHandler { get; set; }
    }

    public partial class SelectCardAction : CardGameAction
    {
        protected override ISelectCardProperties DefaultProperties => new SelectCardProperties
        {
            CardCondition = (card, context) => true,
            GameAction = null,
            SubActionProperties = card => new { target = card },
            Targets = false
        };

        public SelectCardAction() : base()
        {
            Initialize();
        }

        public SelectCardAction(CardActionProperties properties) : base(properties)
        {
            Initialize();
        }

        public SelectCardAction(Func<AbilityContext, CardActionProperties> propertiesFactory) : base(propertiesFactory)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();
            actionName = GameActionBase.SelectCard;
            eventName = EventNames.OnGameStateChanged;
        }

        public override (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("choose a target for {0}", new object[] { properties.Target });
        }

        protected override ISelectCardProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectCardProperties;
            properties.GameAction?.SetDefaultTarget(() => properties.Target);
            
            if (properties.Selector == null)
            {
                Func<BaseCard, AbilityContext, bool> cardCondition = (card, ctx) =>
                    properties.GameAction.AllTargetsLegal(ctx, MergeProperties(additionalProperties, properties.SubActionProperties(card))) &&
                    properties.CardCondition(card, ctx);

                var selectorProperties = new
                {
                    cardType = properties.CardType,
                    controller = properties.Controller,
                    location = properties.Location,
                    cardCondition = cardCondition,
                    mode = properties.Mode
                };

                properties.Selector = CardSelector.For(selectorProperties);
            }
            
            return properties;
        }

        public override bool CanAffect(BaseCard card, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var player = (properties.Targets && context.ChoosingPlayerOverride != null) ? context.ChoosingPlayerOverride :
                         (properties.Player == Players.Opponent && context.Player.Opponent != null) ? context.Player.Opponent :
                         context.Player;
            
            return properties.Selector.CanTarget(card, context, player);
        }

        public override bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var player = (properties.Targets && context.ChoosingPlayerOverride != null) ? context.ChoosingPlayerOverride :
                         (properties.Player == Players.Opponent && context.Player.Opponent != null) ? context.Player.Opponent :
                         context.Player;
            
            return properties.Selector.HasEnoughTargets(context, player);
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            
            if (properties.Player == Players.Opponent && context.Player.Opponent == null)
            {
                return;
            }
            
            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;
            var mustSelect = new List<BaseCard>();
            
            if (properties.Targets)
            {
                player = context.ChoosingPlayerOverride ?? player;
                mustSelect = properties.Selector.GetAllLegalTargets(context, player)
                    .Where(card => card.GetEffects(EffectNames.MustBeChosen)
                        .Any(restriction => restriction.IsMatch("target", context)))
                    .ToList();
            }
            
            if (!properties.Selector.HasEnoughTargets(context, player))
            {
                return;
            }
            
            var buttons = new List<object>();
            if (properties.CancelHandler != null)
            {
                buttons.Add(new { text = "Cancel", arg = "cancel" });
            }
            
            Action<Player, List<BaseCard>> onSelect = (p, cards) =>
            {
                if (!string.IsNullOrEmpty(properties.Message))
                {
                    context.Game.AddMessage(properties.Message, properties.MessageArgs(cards.FirstOrDefault(), p, properties));
                }
                properties.GameAction.AddEventsToArray(events, context, MergeProperties(additionalProperties, properties.SubActionProperties(cards.FirstOrDefault())));
                return;
            };
            
            var promptProperties = new
            {
                context = context,
                selector = properties.Selector,
                mustSelect = mustSelect,
                buttons = buttons,
                onCancel = properties.CancelHandler,
                onSelect = onSelect,
                activePromptTitle = properties.ActivePromptTitle,
                cardType = properties.CardType,
                controller = properties.Controller,
                location = properties.Location,
                cardCondition = properties.CardCondition,
                targets = properties.Targets,
                mode = properties.Mode
            };
            
            context.Game.PromptForSelect(player, promptProperties);
        }

        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Targets && properties.Player != Players.Opponent;
        }

        private object MergeProperties(object additionalProperties, object subActionProperties)
        {
            if (additionalProperties == null) return subActionProperties;
            if (subActionProperties == null) return additionalProperties;
            
            return new { additionalProperties, subActionProperties };
        }
    }
}
