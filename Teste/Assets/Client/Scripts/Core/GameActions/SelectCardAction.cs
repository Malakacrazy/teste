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
        BaseCardSelector Selector { get; set; }
        string Mode { get; set; }
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
        public string Mode { get; set; }
        public Func<BaseCard, object> SubActionProperties { get; set; }
        public Action CancelHandler { get; set; }
    }

    public partial class SelectCardAction : CardGameAction
    {
        protected ISelectCardProperties DefaultProperties => new SelectCardProperties
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
            actionName = GameActionTypes.SelectCard;
            eventName = EventNames.OnGameStateChanged;
        }

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("choose a target for {0}", new object[] { properties.Target });
        }

        protected ISelectCardProperties GetProperties(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as ISelectCardProperties;
            properties.GameAction?.SetDefaultTarget(ctx => properties.Target);
            
            if (properties.Selector == null)
            {
                Func<BaseCard, AbilityContext, bool> cardCondition = (card, ctx) =>
                    properties.GameAction.AllTargetsLegal(ctx, MergeProperties(additionalProperties, properties.SubActionProperties(card)) as GameActionProperties) &&
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

        public virtual bool CanAffect(BaseCard card, AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var player = (properties.Targets && context.ChoosingPlayerOverride != null) ? context.ChoosingPlayerOverride :
                         (properties.Player == Players.Opponent && context.Player.Opponent != null) ? context.Player.Opponent :
                         context.Player;
            
            return (properties.Selector as dynamic)?.CanTarget(card, context, player) ?? false;
        }

        public bool HasLegalTarget(AbilityContext context, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var player = (properties.Targets && context.ChoosingPlayerOverride != null) ? context.ChoosingPlayerOverride :
                         (properties.Player == Players.Opponent && context.Player.Opponent != null) ? context.Player.Opponent :
                         context.Player;
            
            return (properties.Selector as dynamic)?.HasEnoughTargets(context, player) ?? false;
        }

        public void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameActionProperties additionalProperties = null)
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
                mustSelect = (properties.Selector as dynamic)?.GetAllLegalTargets(context, player)
                    ?.Where(new System.Func<BaseCard, bool>(card => card.GetEffects(EffectNames.MustBeChosen)
                        .Any(restriction => restriction != null)))
                    ?.ToList();
            }
            
            if (!((properties.Selector as dynamic)?.HasEnoughTargets(context, player) ?? false))
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
                properties.GameAction.AddEventsToArray(events, context, MergeProperties(additionalProperties, properties.SubActionProperties(cards.FirstOrDefault())) as GameActionProperties);
                return;
            };
            
            var promptProperties = new SelectCardPromptProperties
            {
                context = context,
                mode = properties.Mode,
                activePromptTitle = properties.ActivePromptTitle,
                cardType = properties.CardType?.ToString(),
                controller = properties.Controller,
                location = properties.Location?.ToString(),
                cardCondition = (card) => (properties.CardCondition?.Invoke(card, context) ?? false),
                onSelectAction = onSelect,
                onCancel = () => { properties.CancelHandler?.Invoke(); return true; },
                optional = true
            };
            
            context.Game.PromptForSelect(player, promptProperties);
        }

        public bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameActionProperties additionalProperties = null)
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
