using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Handles conflict resolution mechanics including skill calculation and winner determination
    /// </summary>
    public class ConflictResolution : IGameStep
    {
        private Game game;
        private Conflict conflict;

        public ConflictResolution(Game game, Conflict conflict)
        {
            this.game = game;
            this.conflict = conflict;
        }

        /// <summary>
        /// Resolve the conflict and determine winner/loser
        /// </summary>
        public ConflictResult ResolveConflict()
        {
            var result = new ConflictResult();

            // Calculate final skill values
            int attackerSkill = CalculateAttackerSkill();
            int defenderSkill = CalculateDefenderSkill();

            result.attackerSkill = attackerSkill;
            result.defenderSkill = defenderSkill;
            result.skillDifference = Mathf.Abs(attackerSkill - defenderSkill);

            // Determine winner
            if (attackerSkill > defenderSkill)
            {
                result.winner = conflict.attackingPlayer;
                result.loser = conflict.defendingPlayer;
                result.winnerSkill = attackerSkill;
                result.loserSkill = defenderSkill;
            }
            else if (defenderSkill > attackerSkill)
            {
                result.winner = conflict.defendingPlayer;
                result.loser = conflict.attackingPlayer;
                result.winnerSkill = defenderSkill;
                result.loserSkill = attackerSkill;
            }
            else
            {
                // Tie - no winner
                result.winner = null;
                result.loser = null;
                result.winnerSkill = attackerSkill;
                result.loserSkill = defenderSkill;
            }

            // Apply results to conflict
            conflict.winner = result.winner;
            conflict.loser = result.loser;
            conflict.attackerSkill = attackerSkill;
            conflict.defenderSkill = defenderSkill;
            conflict.skillDifference = result.skillDifference;
            conflict.winnerSkill = result.winnerSkill;
            conflict.loserSkill = result.loserSkill;

            return result;
        }

        private int CalculateAttackerSkill()
        {
            return conflict.attackers.Sum(card => GetCardSkillValue(card, conflict.conflictType));
        }

        private int CalculateDefenderSkill()
        {
            return conflict.defenders.Sum(card => GetCardSkillValue(card, conflict.conflictType));
        }

        private int GetCardSkillValue(BaseCard card, string conflictType)
        {
            if (conflictType == "military")
                return card.GetMilitarySkill();
            else if (conflictType == "political")
                return card.GetPoliticalSkill();
            
            return 0;
        }

        public void Cleanup()
        {
            // Cleanup any resources
        }

        // IGameStep implementation
        public bool Continue()
        {
            // Resolve the conflict and return true when complete
            return true;
        }

        public bool IsComplete()
        {
            return true;
        }

        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Handle menu commands during conflict resolution
        }

        public void OnCardClicked(Player player, BaseCard card)
        {
            // Handle card clicks during conflict resolution
        }

        public void OnRingClicked(Player player, Ring ring)
        {
            // Handle ring clicks during conflict resolution
        }
    }

    /// <summary>
    /// Result data from conflict resolution
    /// </summary>
    [System.Serializable]
    public class ConflictResult
    {
        public Player winner;
        public Player loser;
        public int attackerSkill;
        public int defenderSkill;
        public int winnerSkill;
        public int loserSkill;
        public int skillDifference;
        public bool wasUnopposed;
    }
}
