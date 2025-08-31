using System;
using System.Collections.Generic;
using System.Linq;

namespace L5RGame
{
    public interface IChooseGameChoices : IDictionary<string, GameAction>
    {
    }

    public class ChooseGameChoices : Dictionary<string, GameAction>, IChooseGameChoices
    {
    }

    public interface IChooseActionProperties : IGameActionProperties
    {
        string ActivePromptTitle { get; set; }
        IChooseGameChoices Choices { get; set; }
        Dictionary<string, object> Messages { get; set; }
        string Player { get; set; }
    }

    public class ChooseActionProperties : GameActionProperties, IChooseActionProperties
    {
        public string ActivePromptTitle { get; set; }
        public IChooseGameChoices Choices { get; set; }
        public Dictionary<string, object> Messages { get; set; }
        public string Player { get; set; }
    }

    public partial class ChooseGameAction : GameAction
    {

        protected override IChooseActionProperties DefaultProperties => new ChooseActionProperties
        {
            ActivePromptTitle = "Select an action:",
            Choices = new ChooseGameChoices(),
            Messages = new Dictionary<string, object>()
        };

        public ChooseGameAction() : base()
        {
            Initialize();
        }
        
        public ChooseGameAction(GameActionProperties properties) : base(properties) 
        {
            Initialize();
        }

        public ChooseGameAction(Func<AbilityContext, GameActionProperties> propertiesFactory) : base(propertiesFactory) 
        {
            Initialize();
        }
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "choose";
            eventName = EventNames.OnChooseAction;
            effectMessage = "choose between different actions";
        }

        protected override IChooseActionProperties GetProperties(AbilityContext context, object additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as IChooseActionProperties;
            foreach (var key in properties.Choices.Keys)
            {
                properties.Choices[key].SetDefaultTarget(() => properties.Target);
            }
            return properties;
        }

        public override bool HasLegalTarget(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Choices.Values.Any(gameAction => gameAction.HasLegalTarget(context));
        }

        public override void AddEventsToArray(List<object> events, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var activePromptTitle = properties.ActivePromptTitle;
            var choices = properties.Choices.Keys.ToList();
            choices = choices.Where(key => properties.Choices[key].HasLegalTarget(context)).ToList();
            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;

            Action<string> choiceHandler = (choice) =>
            {
                if (properties.Messages.ContainsKey(choice))
                {
                    context.Game.AddMessage(properties.Messages[choice], player, properties.Target);
                }
                context.Game.QueueSimpleStep(() => properties.Choices[choice].AddEventsToArray(events, context));
            };

            if (choices.Count == 0)
            {
                return;
            }

            var target = properties.Target;
            var promptProperties = new
            {
                activePromptTitle = activePromptTitle,
                context = context,
                choices = choices,
                choiceHandler = choiceHandler,
                target = target
            };

            context.Game.PromptWithHandlerMenu(player, promptProperties);
        }

        public override bool CanAffect(object target, AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Choices.Values.Any(gameAction => gameAction.CanAffect(target, context));
        }

        public override bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.Choices.Values.Any(gameAction => gameAction.HasTargetsChosenByInitiatingPlayer(context));
        }
    }
}
