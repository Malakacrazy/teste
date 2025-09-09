using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class InitiateConflictPrompt : UiPrompt
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

        public Conflict conflict;
        public Player choosingPlayer;
        public bool attackerChoosesRing;
        public bool canPass;
        public List<BaseCard> selectedDefenders;
        public bool covertRemaining;

        public InitiateConflictPrompt(Game game, Conflict conflict, Player choosingPlayer, 
            bool attackerChoosesRing = true, bool canPass = true) : base(game)
        {
            this.conflict = conflict;
            this.choosingPlayer = choosingPlayer;
            this.attackerChoosesRing = attackerChoosesRing;
            this.canPass = canPass && attackerChoosesRing;
            this.selectedDefenders = new List<BaseCard>();
            this.covertRemaining = false;
            CheckForMustSelect();
        }

        public override bool Continue()
        {
            if (!IsComplete)
            {
                HighlightSelectableRings();
            }

            return base.Continue();
        }

        public List<BaseCard> GetMustBeDeclared()
        {
            return choosingPlayer.cardsInPlay.Where(card =>
                card.GetEffects(EffectNames.MustBeDeclaredAsAttacker)
                    .Any(effect => effect.ToString() == "both" || effect.ToString() == conflict.conflictType))
                .ToList();
        }

        public void CheckForMustSelect()
        {
            foreach (var card in GetMustBeDeclared())
            {
                if (CheckCardCondition(card) && !conflict.attackers.Contains(card))
                {
                    SelectCard(card);
                }
            }
        }

        public void HighlightSelectableRings()
        {
            var selectableRings = game.rings.Values.Where(ring => CheckRingCondition(ring)).ToList();
            choosingPlayer.SetSelectableRings(selectableRings);
        }

        public override bool ActiveCondition(Player player)
        {
            return player == choosingPlayer;
        }

        public override PromptInfo ActivePrompt()
        {
            var buttons = new List<ButtonInfo>();
            string menuTitle = "";
            string promptTitle = "";

            if (canPass)
            {
                buttons.Add(new ButtonInfo { text = "Pass Conflict", arg = "pass" });
            }

            if (conflict.ring == null)
            {
                menuTitle = !string.IsNullOrEmpty(conflict.forcedDeclaredType) ? 
                    "Choose an elemental ring" : 
                    "Choose an elemental ring\n(click the ring again to change conflict type)";
                promptTitle = "Initiate Conflict";
            }
            else
            {
                var conflictTypeStr = capitalize[conflict.conflictType];
                var elementStr = capitalize[conflict.element];
                promptTitle = $"{conflictTypeStr} {elementStr} Conflict";
                
                if (conflict.conflictProvince == null && !conflict.isSinglePlayer)
                {
                    menuTitle = "Choose province to attack";
                }
                else if (conflict.attackers.Count == 0)
                {
                    menuTitle = "Choose attackers";
                }
                else
                {
                    if (covertRemaining)
                    {
                        menuTitle = "Choose defenders to Covert";
                    }
                    else
                    {
                        menuTitle = $"{conflictTypeStr} skill: {conflict.attackerSkill}";
                    }
                    buttons.Insert(0, new ButtonInfo { text = "Initiate Conflict", arg = "done" });
                }
            }

            return new PromptInfo
            {
                selectRing = true,
                menuTitle = menuTitle,
                buttons = buttons.ToArray(),
                promptTitle = promptTitle
            };
        }

        public override PromptInfo WaitingPrompt()
        {
            return new PromptInfo { menuTitle = "Waiting for opponent to declare conflict" };
        }

        public override bool OnCardClicked(Player player, BaseCard card)
        {
            return player == choosingPlayer && CheckCardCondition(card) && SelectCard(card);
        }

        public override bool OnRingClicked(Player player, Ring ring)
        {
            return player == choosingPlayer && CheckRingCondition(ring) && SelectRing(ring);
        }

        public bool SelectRing(Ring ring)
        {
            var player = choosingPlayer;

            if (conflict.ring == ring)
            {
                ring.FlipConflictType();
            }
            else
            {
                string type = ring.conflictType;
                if (!player.HasLegalConflictDeclaration(type, ring, conflict.conflictProvince))
                {
                    ring.FlipConflictType();
                }
                else if (conflict.attackers.Any(card => !card.CanDeclareAsAttacker(type, ring)))
                {
                    ring.FlipConflictType();
                }
                
                if (conflict.ring != null)
                {
                    conflict.ring.ResetRing();
                }
                
                conflict.ring = ring;
                ring.contested = true;
            }

            foreach (var card in conflict.attackers.ToList())
            {
                if (!card.CanDeclareAsAttacker(ring.conflictType, ring))
                {
                    RemoveFromConflict(card);
                }
            }

            conflict.CalculateSkill(true);
            RecalculateCovert();

            return true;
        }

        public bool CheckRingCondition(Ring ring)
        {
            var player = choosingPlayer;
            var province = conflict.conflictProvince;
            
            if (conflict.ring == ring)
            {
                string newType = ring.conflictType == "military" ? "political" : "military";
                if (!player.HasLegalConflictDeclaration(newType, ring, province))
                {
                    return false;
                }
                
                var mustBeDeclaredAttackers = GetMustBeDeclared().Where(card => card.inConflict);
                return mustBeDeclaredAttackers.All(card =>
                    card.CanDeclareAsAttacker(newType, ring, province) &&
                    player.HasLegalConflictDeclaration(newType, ring, province));
            }
            
            return attackerChoosesRing && player.HasLegalConflictDeclaration(ring, province);
        }

        public bool CheckCardCondition(BaseCard card)
        {
            if (card.isProvince && card.controller != choosingPlayer)
            {
                return card == conflict.conflictProvince || choosingPlayer.HasLegalConflictDeclaration(
                    conflict.conflictType, conflict.ring, card);
            }
            else if (card.type == CardTypes.Character && card.location == Locations.PlayArea)
            {
                if (card.controller == choosingPlayer)
                {
                    if (conflict.attackers.Contains(card))
                    {
                        return !card.GetEffects(EffectNames.MustBeDeclaredAsAttacker)
                            .Any(effect => effect.ToString() == "both" || effect.ToString() == conflict.conflictType);
                    }
                    return choosingPlayer.HasLegalConflictDeclaration(
                        conflict.conflictType, conflict.ring, conflict.province, card);
                }
                return selectedDefenders.Contains(card) || (!card.IsCovert() && covertRemaining);
            }
            return false;
        }

        public void RecalculateCovert()
        {
            int attackersWithCovert = conflict.attackers.Count(card => card.IsCovert());
            covertRemaining = attackersWithCovert > selectedDefenders.Count;
        }

        public bool SelectCard(BaseCard card)
        {
            if (card.isProvince)
            {
                if (conflict.conflictProvince != null)
                {
                    conflict.conflictProvince.inConflict = false;
                    conflict.conflictProvince = null;
                }
                else
                {
                    conflict.conflictProvince = card;
                    conflict.conflictProvince.inConflict = true;
                }
            }
            else if (card.type == CardTypes.Character)
            {
                if (card.controller == choosingPlayer)
                {
                    if (!conflict.attackers.Contains(card))
                    {
                        conflict.AddAttacker(card);
                    }
                    else
                    {
                        RemoveFromConflict(card);
                    }
                }
                else
                {
                    if (!selectedDefenders.Contains(card))
                    {
                        selectedDefenders.Add(card);
                        card.covert = true;
                    }
                    else
                    {
                        selectedDefenders.Remove(card);
                        card.covert = false;
                    }
                }
            }

            conflict.CalculateSkill(true);
            RecalculateCovert();

            return true;
        }

        public void RemoveFromConflict(BaseCard card)
        {
            if (card.IsCovert() && !covertRemaining && selectedDefenders.Count > 0)
            {
                var lastDefender = selectedDefenders.Last();
                selectedDefenders.Remove(lastDefender);
                lastDefender.covert = false;
            }
            conflict.RemoveFromConflict(card);
        }

        public override bool MenuCommand(Player player, string arg, string method = null)
        {
            if (arg == "done")
            {
                if (conflict.ring == null || game.rings[conflict.element] != conflict.ring ||
                    (!conflict.isSinglePlayer && conflict.conflictProvince == null) || 
                    conflict.attackers.Count == 0)
                {
                    return false;
                }
                
                conflict.declarationComplete = true;
                Complete();
                conflict.declaredRing = conflict.ring;
                conflict.declaredType = conflict.ring.conflictType;
                return true;
            }
            else if (arg == "pass")
            {
                game.PromptWithHandlerMenu(choosingPlayer, new HandlerMenuPromptProperties
                {
                    activePromptTitle = "Are you sure you want to pass your conflict opportunity?",
                    choices = new List<MenuOption> 
                    { 
                        new MenuOption { text = "Yes", arg = "yes" },
                        new MenuOption { text = "No", arg = "no" }
                    },
                    handlers = new List<System.Action>
                    {
                        () => {
                            Complete();
                            conflict.PassConflict();
                        },
                        () => { /* Do nothing */ }
                    }
                });
                return true;
            }
            return false;
        }
    }
}
