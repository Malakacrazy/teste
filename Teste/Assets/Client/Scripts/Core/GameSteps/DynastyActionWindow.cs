using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Action window for dynasty phase, allowing players to play cards from provinces
    /// </summary>
    public class DynastyActionWindow : ActionWindow
    {
        public event System.Action OnActionWindowComplete;

        public DynastyActionWindow(Game game) : base(game, "Play cards from provinces")
        {
            this.windowType = "dynasty";
        }

        public virtual Dictionary<string, object> GetActivePrompt()
        {
            var props = base.GetActivePrompt();
            
            return new Dictionary<string, object>
            {
                { "menuTitle", "Click pass when done" },
                { "buttons", props.ContainsKey("buttons") ? props["buttons"] : new List<object>() },
                { "promptTitle", windowName }
            };
        }

        public virtual void Pass()
        {
            currentPlayer.PassDynasty();
            
            if (currentPlayer.opponent == null || !currentPlayer.opponent.passedDynasty)
            {
                game.AddMessage("{0} is the first to pass, and gains 1 fate", currentPlayer.name);
                game.RaiseEvent(EventNames.OnPassDuringDynasty, 
                    new Dictionary<string, object> 
                    { 
                        { "player", currentPlayer }, 
                        { "firstToPass", true } 
                    }, 
                    () => 
                    {
                        currentPlayer?.ModifyFate(1);
                        return true;
                    });
            }
            else
            {
                game.AddMessage("{0} passes", currentPlayer.name);
                game.RaiseEvent(EventNames.OnPassDuringDynasty, 
                    new Dictionary<string, object> 
                    { 
                        { "player", currentPlayer }, 
                        { "firstToPass", false } 
                    });
            }
            
            if (currentPlayer.opponent == null || currentPlayer.opponent.passedDynasty)
            {
                Complete();
                OnActionWindowComplete?.Invoke();
            }
            else
            {
                NextPlayer();
            }
        }

        public virtual void NextPlayer()
        {
            Player otherPlayer = currentPlayer.opponent;
            if (otherPlayer != null && !otherPlayer.passedDynasty)
            {
                currentPlayer = otherPlayer;
            }
        }

        public bool IsActive()
        {
            return !isComplete;
        }

        public new string GetDebugInfo()
        {
            string playerInfo = currentPlayer != null ? currentPlayer.name : "No player";
            bool opponentPassed = currentPlayer?.opponent?.passedDynasty ?? false;
            return $"DynastyActionWindow - {windowName} - Current: {playerInfo} - Opponent passed: {opponentPassed}";
        }
    }
}