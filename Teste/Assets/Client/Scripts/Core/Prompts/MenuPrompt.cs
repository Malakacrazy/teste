using UnityEngine;

namespace L5RGame
{
    public class MenuPrompt : UiPrompt
    {
        public Player player;
        public object context;
        public PromptProperties properties;

        public MenuPrompt(Game game, Player player, object context, PromptProperties properties) : base(game)
        {
            this.player = player;
            this.context = context;
            
            if (properties.source != null && string.IsNullOrEmpty(properties.waitingPromptTitle))
            {
                properties.waitingPromptTitle = $"Waiting for opponent to use {properties.source.name}";
            }
            
            this.properties = properties;
        }

        public override bool ActiveCondition(Player player)
        {
            return player == this.player;
        }

        public override PromptInfo ActivePrompt()
        {
            string promptTitle = properties.promptTitle ?? 
                (properties.source != null ? properties.source.name : null);
            
            var prompt = properties.activePrompt;
            prompt.promptTitle = promptTitle;
            
            return prompt;
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = properties.waitingPromptTitle ?? "Waiting for opponent" };
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (context == null || string.IsNullOrEmpty(method))
                return false;

            // Use reflection to call the method on the context object
            var methodInfo = context.GetType().GetMethod(method);
            if (methodInfo == null)
                return false;

            var result = methodInfo.Invoke(context, new object[] { player, arg, properties.context });
            
            if (result is bool boolResult && boolResult)
            {
                Complete();
            }

            return true;
        }

        public bool HasMethodButton(string method)
        {
            if (properties.activePrompt?.buttons == null)
                return false;
                
            foreach (var button in properties.activePrompt.buttons)
            {
                if (button.method == method)
                    return true;
            }
            
            return false;
        }
    }
}
