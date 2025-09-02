using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public interface IMenuPromptProperties : IGameActionProperties
    {
        string ActivePromptTitle { get; set; }
        string Player { get; set; }
        GameAction GameAction { get; set; }
        object Choices { get; set; } // Can be string[] or Func<object, string[]>
        Func<string, bool, IMenuPromptProperties, object> ChoiceHandler { get; set; }
    }

    public class MenuPromptProperties : GameAction.GameActionProperties, IMenuPromptProperties
    {
        public string ActivePromptTitle { get; set; }
        public string Player { get; set; }
        public GameAction GameAction { get; set; }
        public object Choices { get; set; }
        public Func<string, bool, IMenuPromptProperties, object> ChoiceHandler { get; set; }
        
        public new List<object> Target { get; set; } = new List<object>();
        public new bool CannotBeCancelled { get; set; }
        public new bool Optional { get; set; }
        public new GameAction ParentAction { get; set; }
    }

    public partial class MenuPromptAction : GameAction
    {
        public MenuPromptAction() : base()
        {
            Initialize();
        }

        public MenuPromptAction(GameActionProperties properties) : base(properties)
        {
            Initialize();
        }

        public MenuPromptAction(Func<AbilityContext, GameActionProperties> propertiesFactory) : base(propertiesFactory)
        {
            Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();
            actionName = "menuPrompt";
            eventName = EventNames.OnGameStateChanged;
        }

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context);
            return ("make a choice for {0}", new object[] { properties.Target });
        }

        protected IMenuPromptProperties GetProperties(AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = base.GetProperties(context, additionalProperties) as IMenuPromptProperties;
            
            if (properties.Choices is Func<object, string[]> choicesFunc)
            {
                properties.Choices = choicesFunc(properties);
            }
            
            return properties;
        }

        public bool CanAffect(object target, AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var choices = properties.Choices as string[];
            
            if (choices == null) return false;
            
            return choices.Any(choice =>
            {
                var childProperties = properties.ChoiceHandler(choice, false, properties);
                return properties.GameAction.CanAffect(target, context, childProperties as GameAction.GameActionProperties);
            });
        }

        public bool HasLegalTarget(AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var choices = properties.Choices as string[];
            
            if (choices == null) return false;
            
            return choices.Any(choice =>
            {
                var childProperties = properties.ChoiceHandler(choice, false, properties);
                return properties.GameAction.HasLegalTarget(context, childProperties as GameAction.GameActionProperties);
            });
        }

        public void AddEventsToArray(List<GameEvent> events, AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            var choices = properties.Choices as string[];
            
            if (choices == null || choices.Length == 0 || 
                (properties.Player == Players.Opponent && context.Player.Opponent == null))
            {
                return;
            }
            
            var player = properties.Player == Players.Opponent ? context.Player.Opponent : context.Player;
            
            Action<string> choiceHandler = choice =>
            {
                var childProperties = properties.ChoiceHandler(choice, true, properties);
                properties.GameAction.AddEventsToArray(events, context, childProperties as GameAction.GameActionProperties);
            };
            
            if (choices.Length == 1)
            {
                choiceHandler(choices[0]);
                return;
            }
            
            var promptProperties = new HandlerMenuPromptProperties
            {
                activePromptTitle = properties.ActivePromptTitle,
                context = context,
                choiceHandler = choiceHandler,
                choices = choices.Select(c => new MenuOption { text = c, arg = c }).ToList(),
                gameAction = properties.GameAction
            };
            
            context.Game.PromptWithHandlerMenu(player, promptProperties);
        }

        public bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties);
            return properties.GameAction.HasTargetsChosenByInitiatingPlayer(context);
        }
    }
}
