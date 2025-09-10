using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Action window for conflict resolution, allowing players to play cards and abilities
    /// </summary>
    public class ConflictActionWindow : ActionWindow
    {
        private static readonly Dictionary<string, string> capitalize = new Dictionary<string, string>
        {
            { "military", "Military" },
            { "political", "Political" },
            { "air", "Air" },
            { "water", "Water" },
            { "earth", "Earth" },
            { "fire", "Fire" },
            { "void", "Void" }
        };

        private Conflict conflict;
        private bool displayTotals;

        public ConflictActionWindow(Game game, string title, Conflict conflict) : base(game, title)
        {
            this.conflict = conflict;
            this.windowType = "conflict";
            this.displayTotals = false;
        }

        public new bool Continue()
        {
            bool completed = base.Continue();
            
            if (!completed && displayTotals)
            {
                string conflictText = GetConflictText();
                game.AddMessage("{0} - Attacker: {1} Defender: {2}", 
                              conflictText, conflict.attackerSkill, conflict.defenderSkill);
                
                string winnerText = GetWinnerText();
                game.AddMessage(winnerText, conflict.conflictProvince);
                
                displayTotals = false;
            }
            
            return completed;
        }

        public virtual Dictionary<string, object> GetActivePrompt()
        {
            var props = base.GetActivePrompt();
            
            string conflictText = GetConflictText();
            string skillText = $"Attacker: {conflict.attackerSkill} Defender: {conflict.defenderSkill}";
            
            return new Dictionary<string, object>
            {
                { "menuTitle", $"{conflictText}\n{skillText}" },
                { "buttons", props.ContainsKey("buttons") ? props["buttons"] : new List<object>() },
                { "promptTitle", windowName }
            };
        }

        public virtual void PostResolutionUpdate(object resolver)
        {
            // Call base method if it exists
            if (!game.manualMode)
            {
                displayTotals = true;
            }
        }

        private string GetConflictText()
        {
            string conflictTypeCap = capitalize.ContainsKey(conflict.conflictType) 
                ? capitalize[conflict.conflictType] 
                : conflict.conflictType;
            string elementCap = capitalize.ContainsKey(conflict.element) 
                ? capitalize[conflict.element] 
                : conflict.element;
            
            return $"{conflictTypeCap} {elementCap} conflict";
        }

        private string GetWinnerText()
        {
            string winnerText = "Attacker is winning the conflict";
            
            if (conflict.attackerSkill == 0 && conflict.defenderSkill == 0)
            {
                winnerText = "No-one is winning the conflict";
            }
            else if (conflict.defenderSkill > conflict.attackerSkill)
            {
                winnerText = "Defender is winning the conflict";
            }
            else if (conflict.conflictProvince != null && 
                     !conflict.conflictProvince.isBroken && 
                     conflict.conflictProvince.AllowGameAction("break") &&
                     conflict.attackerSkill >= conflict.defenderSkill + conflict.conflictProvince.GetStrength())
            {
                winnerText += " - {0} is breaking!";
            }
            
            return winnerText;
        }

        public new string GetDebugInfo()
        {
            string conflictInfo = conflict != null ? $"Conflict: {GetConflictText()}" : "No conflict";
            return $"ConflictActionWindow - {windowName} - {conflictInfo} - Display totals: {displayTotals}";
        }
    }
}