using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public abstract class UiPrompt : BaseStep
    {
        public bool completed;
        public string uuid;

        public UiPrompt(Game game) : base(game)
        {
            completed = false;
            uuid = System.Guid.NewGuid().ToString();
        }

        public override bool IsComplete()
        {
            return completed;
        }

        public virtual void Complete()
        {
            completed = true;
            game.ResetClocks();
        }

        public virtual void SetPrompt()
        {
            foreach (var player in game.GetPlayers())
            {
                if (ActiveCondition(player))
                {
                    player.SetPrompt(AddDefaultCommandToButtons(ActivePrompt(player)));
                    player.StartClock();
                }
                else
                {
                    player.SetPrompt(WaitingPrompt());
                    player.ResetClock();
                }
            }
        }

        public virtual bool ActiveCondition(Player player)
        {
            return true;
        }

        public virtual PromptInfo ActivePrompt(Player player = null)
        {
            return new PromptInfo();
        }

        public virtual PromptInfo ActivePrompt()
        {
            return ActivePrompt(null);
        }

        private PromptInfo AddDefaultCommandToButtons(PromptInfo original)
        {
            var prompt = original.Clone();
            
            if (prompt.buttons != null)
            {
                foreach (var button in prompt.buttons)
                {
                    button.command = button.command ?? "menuButton";
                    button.uuid = uuid;
                }
            }
            
            if (prompt.controls != null)
            {
                foreach (var control in prompt.controls)
                {
                    control.uuid = uuid;
                }
            }
            
            return prompt;
        }

        public virtual PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = "Waiting for opponent" };
        }

        public override bool Continue()
        {
            bool completed = IsComplete();

            if (completed)
            {
                ClearPrompts();
            }
            else
            {
                SetPrompt();
            }

            return completed;
        }

        protected void ClearPrompts()
        {
            foreach (var player in game.GetPlayers())
            {
                player.CancelPrompt();
            }
        }

        public virtual bool OnMenuCommand(Player player, string arg, string uuid, string method = null)
        {
            if (!ActiveCondition(player) || uuid != this.uuid)
                return false;

            return MenuCommand(player, arg, method);
        }

        public virtual bool MenuCommand(Player player, string arg, string method = null)
        {
            return true;
        }

        public virtual bool OnCardClicked(Player player, BaseCard card)
        {
            return false;
        }

        public virtual bool OnRingClicked(Player player, Ring ring)
        {
            return false;
        }
    }

    [Serializable]
    public class PromptInfo
    {
        public string menuTitle;
        public string promptTitle;
        public ButtonInfo[] buttons;
        public PromptControl[] controls;
        public bool selectCard;
        public bool selectRing;
        public bool selectOrder;
        public EffectSource source;

        public PromptInfo Clone()
        {
            return new PromptInfo
            {
                menuTitle = menuTitle,
                promptTitle = promptTitle,
                buttons = buttons?.Select(b => b.Clone()).ToArray(),
                controls = controls?.Select(c => c.Clone()).ToArray(),
                selectCard = selectCard,
                selectRing = selectRing,
                selectOrder = selectOrder,
                source = source
            };
        }
    }

    [Serializable]
    public class ButtonInfo
    {
        public string text;
        public string arg;
        public string command;
        public string uuid;
        public string method;
        public BaseCard card;
        public bool disabled;

        public ButtonInfo Clone()
        {
            return new ButtonInfo
            {
                text = text,
                arg = arg,
                command = command,
                uuid = uuid,
                method = method,
                card = card,
                disabled = disabled
            };
        }
    }

    [Serializable]
    public class PromptControl
    {
        public string type;
        public string source;
        public string[] targets;
        public string uuid;

        public PromptControl Clone()
        {
            return new PromptControl
            {
                type = type,
                source = source,
                targets = targets?.ToArray(),
                uuid = uuid
            };
        }
    }
}
