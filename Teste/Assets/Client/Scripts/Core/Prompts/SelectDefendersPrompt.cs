using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class SelectDefendersPrompt : UiPrompt
    {
        private static readonly Dictionary<string, string> capitalize = new Dictionary<string, string>
        {
            {"military", "Military"},
            {"political", "Political"},
            {"air", "Air"},
            {"water", "Water"},
            {"earth", "Earth"},
            {"fire", "Fire"},
            {"void", "Void"}
        };

        public Player player;
        public Conflict conflict;

        public SelectDefendersPrompt(Game game, Player player, Conflict conflict) : base(game)
        {
            this.player = player;
            this.conflict = conflict;
            
            // Auto-select must be declared defenders
            var mustBeDeclared = player.cardsInPlay.Where(card =>
                card.GetEffects(EffectNames.MustBeDeclaredAsDefender)
                    .Any(effect => effect.ToString() == "both" || effect.ToString() == conflict.conflictType))
                .ToList();
                
            foreach (var card in mustBeDeclared)
            {
                if (CheckCardCondition(card) && !conflict.defenders.Contains(card))
                {
                    SelectCard(card);
                }
            }
        }

        public override bool ActiveCondition(Player player)
        {
            return player == this.player;
        }

        public override PromptInfo ActivePrompt()
        {
            var conflictTypeStr = capitalize[conflict.conflictType];
            var elementStr = capitalize[conflict.element];
            string promptTitle = $"{conflictTypeStr} {elementStr} Conflict: " +
                               $"{conflict.attackerSkill} vs {conflict.defenderSkill}";
            
            return new PromptInfo
            {
                menuTitle = "Choose defenders",
                buttons = new ButtonInfo[] { new ButtonInfo { text = "Done", arg = "done" } },
                promptTitle = promptTitle
            };
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = "Waiting for opponent to choose defenders" };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            if (player != this.player)
                return false;

            if (!CheckCardCondition(card))
                return false;

            return SelectCard(card);
        }

        private bool CheckCardCondition(BaseCard card)
        {
            if (conflict.defenders.Contains(card) && 
                card.GetEffects(EffectNames.MustBeDeclaredAsDefender)
                    .Any(effect => effect.ToString() == "both" || effect.ToString() == conflict.conflictType))
            {
                return false;
            }
            
            return card.GetCardType() == CardTypes.Character &&
                   card.controller == player &&
                   card.CanDeclareAsDefender(conflict);
        }

        private bool SelectCard(BaseCard card)
        {
            if (conflict.maxAllowedDefenders > -1 && 
                conflict.defenders.Count >= conflict.maxAllowedDefenders && 
                !conflict.defenders.Contains(card))
            {
                return false;
            }

            if (!conflict.defenders.Contains(card))
            {
                conflict.AddDefender(card);
            }
            else
            {
                conflict.RemoveFromConflict(card);
            }

            conflict.CalculateSkill(true);
            return true;
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            foreach (var card in conflict.defenders)
            {
                card.covert = false;
            }
            
            conflict.SetDefendersChosen(true);
            Complete();
            return true;
        }
    }
}
