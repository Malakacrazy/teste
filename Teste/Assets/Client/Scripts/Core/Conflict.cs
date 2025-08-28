using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a conflict between two players in Legend of the Five Rings.
    /// Handles all aspects of conflict resolution including skill calculation, 
    /// participant management, and winner determination.
    /// </summary>
    public class Conflict : MonoBehaviour
    {
        [Header("Game Reference")]
        public Game game;
        
        [Header("Conflict Participants")]
        public Player attackingPlayer;
        public Player defendingPlayer;
        public bool isSinglePlayer = false;

        [Header("Conflict Declaration")]
        public Ring declaredRing;
        public Ring ring;
        public string declaredType;
        public string forcedDeclaredType;
        public bool declarationComplete = false;
        public bool defendersChosen = false;

        [Header("Conflict State")]
        public BaseCard conflictProvince;
        public bool conflictPassed = false;
        public bool conflictTypeSwitched = false;
        public bool conflictUnopposed = false;
        public bool winnerGoesStraightToNextConflict = false;
        public bool winnerDetermined = false;

        [Header("Participants")]
        public List<BaseCard> attackers = new List<BaseCard>();
        public List<BaseCard> defenders = new List<BaseCard>();
        public int attackerSkill = 0;
        public int defenderSkill = 0;

        [Header("Conflict Resolution")]
        public Player winner;
        public Player loser;
        public int winnerSkill = 0;
        public int loserSkill = 0;
        public int skillDifference = 0;

        [Header("Cards Played")]
        public List<object> attackerCardsPlayed = new List<object>();
        public List<object> defenderCardsPlayed = new List<object>();

        // Properties for compatibility with existing code
        public string uuid => GetInstanceID().ToString();
        public string conflictType => ConflictType;
        public List<string> elements => Elements;

        /// <summary>
        /// Initialize the conflict with attacking and defending players
        /// </summary>
        public void Initialize(Game gameInstance, Player attacker, Player defender = null, 
                             Ring conflictRing = null, BaseCard province = null, string forcedType = null)
        {
            game = gameInstance;
            
            attackingPlayer = attacker;
            isSinglePlayer = (defender == null);
            defendingPlayer = defender ?? CreateSinglePlayerDefender();
            forcedDeclaredType = forcedType;
            declaredRing = ring = conflictRing;
            conflictProvince = province;
            
            // Initialize collections
            attackers = new List<BaseCard>();
            defenders = new List<BaseCard>();
            attackerCardsPlayed = new List<object>();
            defenderCardsPlayed = new List<object>();
            
            Debug.Log($"⚔️ Conflict initialized: {attackingPlayer.name} vs {defendingPlayer.name}");
        }

        /// <summary>
        /// Constructor for compatibility with existing code
        /// </summary>
        public Conflict(Game gameInstance, Player attacker, Player defender, Ring conflictRing, 
                       BaseCard province, string forcedType)
        {
            Initialize(gameInstance, attacker, defender, conflictRing, province, forcedType);
        }

        /// <summary>
        /// Current conflict type (military or political) from the contested ring
        /// </summary>
        public string ConflictType => ring?.conflictType ?? "";

        /// <summary>
        /// Current ring element being contested
        /// </summary>
        public string Element => ring?.element ?? "";

        /// <summary>
        /// All elements associated with this conflict
        /// </summary>
        public List<string> Elements => ring?.GetElements() ?? new List<string> { ring?.element };

        /// <summary>
        /// Number of elements to resolve (usually 1 + modifiers)
        /// </summary>
        public int ElementsToResolve => SumEffects(EffectNames.ModifyConflictElementsToResolve) + 1;

        /// <summary>
        /// Maximum number of defenders allowed (from effects)
        /// </summary>
        public int MaxAllowedDefenders
        {
            get
            {
                var effects = GetEffects(EffectNames.RestrictNumberOfDefenders);
                return effects.Count == 0 ? -1 : effects.Cast<int>().Min();
            }
        }

        /// <summary>
        /// Gets a summary of the current conflict state
        /// </summary>
        /// <returns>Conflict summary for UI display</returns>
        public ConflictSummary GetSummary()
        {
            var forcedUnopposedEffects = GetEffects(EffectNames.ForceConflictUnopposed);
            bool forcedUnopposed = forcedUnopposedEffects.Count > 0;
            
            return new ConflictSummary
            {
                attackingPlayerId = attackingPlayer?.id ?? "",
                defendingPlayerId = defendingPlayer?.id ?? "",
                attackerSkill = attackerSkill,
                defenderSkill = defenderSkill,
                type = ConflictType,
                elements = Elements,
                attackerWins = attackers.Count > 0 && attackerSkill >= defenderSkill,
                breaking = conflictProvince != null && 
                          (conflictProvince.GetStrength() - (attackerSkill - defenderSkill) <= 0),
                unopposed = !(defenders.Count > 0 && !forcedUnopposed),
                declarationComplete = declarationComplete,
                defendersChosen = defendersChosen,
                conflictRing = ring?.element ?? "",
                province = conflictProvince?.name ?? "",
                winnerDetermined = winnerDetermined,
                winner = winner?.name ?? "",
                skillDifference = skillDifference
            };
        }

        /// <summary>
        /// Add multiple attackers to the conflict
        /// </summary>
        /// <param name="newAttackers">List of attacking characters</param>
        public void AddAttackers(List<BaseCard> newAttackers)
        {
            var validAttackers = newAttackers.Where(card => !IsAttacking(card)).ToList();
            if (validAttackers.Count > 0)
            {
                attackers.AddRange(validAttackers);
                MarkAsParticipating(validAttackers);
                
                Debug.Log($"⚔️ Added {validAttackers.Count} attackers to conflict");
            }
        }

        /// <summary>
        /// Add a single attacker to the conflict
        /// </summary>
        /// <param name="attacker">Attacking character</param>
        public void AddAttacker(BaseCard attacker)
        {
            if (!attackers.Contains(attacker))
            {
                attackers.Add(attacker);
                MarkAsParticipating(new List<BaseCard> { attacker });
                
                Debug.Log($"⚔️ {attacker.name} joins as attacker");
            }
        }

        /// <summary>
        /// Add multiple defenders to the conflict
        /// </summary>
        /// <param name="newDefenders">List of defending characters</param>
        public void AddDefenders(List<BaseCard> newDefenders)
        {
            var validDefenders = newDefenders.Where(card => !IsDefending(card)).ToList();
            if (validDefenders.Count > 0)
            {
                defenders.AddRange(validDefenders);
                MarkAsParticipating(validDefenders);
                
                Debug.Log($"🛡️ Added {validDefenders.Count} defenders to conflict");
            }
        }

        /// <summary>
        /// Add a single defender to the conflict
        /// </summary>
        /// <param name="defender">Defending character</param>
        public void AddDefender(BaseCard defender)
        {
            if (!defenders.Contains(defender))
            {
                defenders.Add(defender);
                MarkAsParticipating(new List<BaseCard> { defender });
                
                Debug.Log($"🛡️ {defender.name} joins as defender");
            }
        }

        /// <summary>
        /// Remove a character from the conflict
        /// </summary>
        /// <param name="card">Character to remove</param>
        public void RemoveFromConflict(BaseCard card)
        {
            if (attackers.Remove(card) || defenders.Remove(card))
            {
                card.inConflict = false;
                Debug.Log($"🏃 {card.name} removed from conflict");
            }
        }

        /// <summary>
        /// Mark characters as participating in the conflict
        /// </summary>
        /// <param name="cards">Characters to mark as participating</param>
        private void MarkAsParticipating(List<BaseCard> cards)
        {
            foreach (var card in cards)
            {
                card.inConflict = true;
            }
        }

        /// <summary>
        /// Calculate current skill totals for both sides
        /// </summary>
        /// <param name="prevStateChanged">Whether state changed in previous check</param>
        /// <returns>True if game state changed</returns>
        public bool CalculateSkill(bool prevStateChanged = false)
        {
            bool stateChanged = game?.effectEngine?.CheckEffects(prevStateChanged) ?? false;

            if (winnerDetermined) return stateChanged;

            // Calculate attacker skill
            if (attackingPlayer.AnyEffect(EffectNames.SetConflictTotalSkill))
            {
                attackerSkill = (int)attackingPlayer.MostRecentEffect(EffectNames.SetConflictTotalSkill);
            }
            else
            {
                attackerSkill = CalculateSkillFor(attackers) + attackingPlayer.SkillModifier;
                
                // Imperial favor bonus
                if (attackingPlayer.imperialFavor == ConflictType && attackers.Count > 0)
                {
                    attackerSkill++;
                }
            }

            // Calculate defender skill
            if (defendingPlayer.AnyEffect(EffectNames.SetConflictTotalSkill))
            {
                defenderSkill = (int)defendingPlayer.MostRecentEffect(EffectNames.SetConflictTotalSkill);
            }
            else
            {
                defenderSkill = CalculateSkillFor(defenders) + defendingPlayer.SkillModifier;
                
                // Imperial favor bonus
                if (defendingPlayer.imperialFavor == ConflictType && defenders.Count > 0)
                {
                    defenderSkill++;
                }
            }

            return stateChanged;
        }

        /// <summary>
        /// Calculate skill contribution for a list of characters
        /// </summary>
        /// <param name="cards">Characters to calculate skill for</param>
        /// <returns>Total skill contribution</returns>
        private int CalculateSkillFor(List<BaseCard> cards)
        {
            var skillFunction = MostRecentEffect(EffectNames.ChangeConflictSkillFunction) as System.Func<BaseCard, int> ??
                               (card => card.GetContributionToConflict(ConflictType));
            
            var cannotContributeFunctions = GetEffects(EffectNames.CannotContribute)
                .Cast<System.Func<BaseCard, bool>>().ToList();

            return cards.Sum(card =>
            {
                // Check if card cannot contribute
                bool cannotContribute = card.bowed;
                if (!cannotContribute)
                {
                    cannotContribute = cannotContributeFunctions.Any(func => func(card));
                }

                return cannotContribute ? 0 : skillFunction(card);
            });
        }

        /// <summary>
        /// Check for and remove characters that can no longer participate
        /// </summary>
        public void CheckForIllegalParticipants()
        {
            var illegalAttackers = attackers.Where(card => 
                !card.CanParticipateAsAttacker(ConflictType)).ToList();
            var illegalDefenders = defenders.Where(card => 
                !card.CanParticipateAsDefender(ConflictType)).ToList();
            
            var allIllegal = illegalAttackers.Concat(illegalDefenders).ToList();
            
            if (allIllegal.Count > 0)
            {
                string verb = allIllegal.Count > 1 ? "are" : "is";
                game.AddMessage("{0} cannot participate in the conflict any more and {1} sent home bowed", 
                               allIllegal, verb);
                
                var context = game.GetFrameworkContext();
                game.ApplyGameAction(context, new Dictionary<string, object>
                {
                    {"sendHome", allIllegal},
                    {"bow", allIllegal}
                });
            }
        }

        /// <summary>
        /// Check if a character is attacking
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if the character is attacking</returns>
        public bool IsAttacking(BaseCard card)
        {
            return attackers.Contains(card);
        }

        /// <summary>
        /// Check if a character is defending
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if the character is defending</returns>
        public bool IsDefending(BaseCard card)
        {
            return defenders.Contains(card);
        }

        /// <summary>
        /// Check if a character is participating in any way
        /// </summary>
        /// <param name="card">Character to check</param>
        /// <returns>True if the character is participating</returns>
        public bool IsParticipating(BaseCard card)
        {
            return IsAttacking(card) || IsDefending(card);
        }

        /// <summary>
        /// Get the sum of all effects of a specific type
        /// </summary>
        /// <param name="effectName">Effect name to sum</param>
        /// <returns>Sum of effect values</returns>
        private int SumEffects(string effectName)
        {
            // Placeholder implementation - would integrate with effect engine
            return 0;
        }
        
        /// <summary>
        /// Get effects of a specific type
        /// </summary>
        /// <param name="effectName">Effect name</param>
        /// <returns>List of effects</returns>
        private List<object> GetEffects(string effectName)
        {
            // Placeholder implementation - would integrate with effect engine
            return new List<object>();
        }
        
        /// <summary>
        /// Get the most recent effect of a specific type
        /// </summary>
        /// <param name="effectName">Effect name</param>
        /// <returns>Most recent effect or null</returns>
        private object MostRecentEffect(string effectName)
        {
            // Placeholder implementation - would integrate with effect engine
            return null;
        }

        /// <summary>
        /// Create dummy player for single player mode
        /// </summary>
        /// <returns>Dummy defending player</returns>
        private Player CreateSinglePlayerDefender()
        {
            var dummyGO = new GameObject("DummyPlayer");
            dummyGO.transform.SetParent(game.transform);
            var dummy = dummyGO.AddComponent<Player>();
            
            var dummyUser = new UserInfo
            {
                username = "Dummy Player",
                emailHash = "",
                lobbyId = ""
            };
            
            dummy.Initialize("dummy", dummyUser, false, game, new ClockSettings());
            dummy.Initialize(); // Initialize decks and game state
            
            return dummy;
        }

        /// <summary>
        /// Cleanup when conflict is destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (attackers != null) attackers.Clear();
            if (defenders != null) defenders.Clear();
            if (attackerCardsPlayed != null) attackerCardsPlayed.Clear();
            if (defenderCardsPlayed != null) defenderCardsPlayed.Clear();
            
            Debug.Log("⚔️ Conflict destroyed");
        }
    }

    /// <summary>
    /// Summary data for conflict state
    /// </summary>
    [System.Serializable]
    public class ConflictSummary
    {
        public string attackingPlayerId;
        public string defendingPlayerId;
        public int attackerSkill;
        public int defenderSkill;
        public string type;
        public List<string> elements;
        public bool attackerWins;
        public bool breaking;
        public bool unopposed;
        public bool declarationComplete;
        public bool defendersChosen;
        public string conflictRing;
        public string province;
        public bool winnerDetermined;
        public string winner;
        public int skillDifference;
    }

    /// <summary>
    /// Conflict-specific event names
    /// </summary>
    public static partial class EventNames
    {
        public const string OnConflictPass = "onConflictPass";
        public const string OnConflictDeclared = "onConflictDeclared";
        public const string OnAttackersChosen = "onAttackersChosen";
        public const string OnDefendersChosen = "onDefendersChosen";
        public const string OnConflictResolved = "onConflictResolved";
    }
}
