using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Types of duels that can be initiated
    /// </summary>
    public static class DuelTypes
    {
        public const string Military = "military";
        public const string Political = "political";
        public const string Glory = "glory";
    }

    /// <summary>
    /// Represents a duel between characters in Legend of the Five Rings.
    /// Duels involve honor bidding and comparing character statistics.
    /// </summary>
    public class Duel : MonoBehaviour
    {
        [Header("Duel Participants")]
        public BaseCard challenger;
        public List<BaseCard> targets = new List<BaseCard>();
        
        [Header("Duel Properties")]
        public string duelType;
        public EffectSource source;
        public bool bidFinished = false;
        
        [Header("Duel Results")]
        public object winner; // Can be BaseCard or List<BaseCard>
        public object loser;  // Can be BaseCard or List<BaseCard>
        
        [Header("Duel Chain")]
        public Duel previousDuel;
        
        // Custom statistic function for special duels
        private System.Func<BaseCard, int> customStatistic;
        
        /// <summary>
        /// Reference to the game instance
        /// </summary>
        public Game game { get; private set; }

        /// <summary>
        /// Initialize the duel with participants and type
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="challengingCard">Character initiating the duel</param>
        /// <param name="targetCards">Characters being challenged</param>
        /// <param name="type">Type of duel (military, political, glory)</param>
        /// <param name="statistic">Custom statistic function (optional)</param>
        public void Initialize(Game gameInstance, BaseCard challengingCard, List<BaseCard> targetCards, 
                             string type, System.Func<BaseCard, int> statistic = null)
        {
            game = gameInstance;
            duelType = type;
            source = game.GetFrameworkContext().source as EffectSource;
            challenger = challengingCard;
            targets = targetCards ?? new List<BaseCard>();
            customStatistic = statistic;
            bidFinished = false;
            winner = null;
            loser = null;
            previousDuel = null;

            Debug.Log($"⚔️ Duel initiated: {challenger.name} vs {GetTargetName()} ({type})");
        }

        /// <summary>
        /// Initialize with a single target
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="challengingCard">Character initiating the duel</param>
        /// <param name="targetCard">Character being challenged</param>
        /// <param name="type">Type of duel</param>
        /// <param name="statistic">Custom statistic function (optional)</param>
        public void Initialize(Game gameInstance, BaseCard challengingCard, BaseCard targetCard, 
                             string type, System.Func<BaseCard, int> statistic = null)
        {
            Initialize(gameInstance, challengingCard, new List<BaseCard> { targetCard }, type, statistic);
        }

        /// <summary>
        /// Get the skill statistic for a card in this duel
        /// </summary>
        /// <param name="card">Card to get statistic for</param>
        /// <returns>Skill value for the duel type</returns>
        public int GetSkillStatistic(BaseCard card)
        {
            if (card == null) return 0;

            // Use custom statistic if provided
            if (customStatistic != null)
            {
                return customStatistic(card);
            }

            // Use standard duel statistics
            switch (duelType)
            {
                case DuelTypes.Military:
                    return card.GetMilitarySkill();
                case DuelTypes.Political:
                    return card.GetPoliticalSkill();
                case DuelTypes.Glory:
                    return card.GetGlory();
                default:
                    Debug.LogWarning($"⚠️ Unknown duel type: {duelType}");
                    return 0;
            }
        }

        /// <summary>
        /// Check if a card is involved in this duel
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if the card is participating in this duel</returns>
        public bool IsInvolved(BaseCard card)
        {
            if (card == null || card.location != Locations.PlayArea) return false;
            
            return card == challenger || targets.Contains(card);
        }

        /// <summary>
        /// Check if a card is involved in this duel or any previous duel in the chain
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if the card is involved in any duel in the chain</returns>
        public bool IsInvolvedInAnyDuel(BaseCard card)
        {
            if (IsInvolved(card)) return true;
            
            return previousDuel?.IsInvolvedInAnyDuel(card) ?? false;
        }

        /// <summary>
        /// Get display text showing the duel totals
        /// </summary>
        /// <returns>Formatted string showing duel state</returns>
        public string GetTotalsForDisplay()
        {
            string challengerTotal = GetChallengerStatisticTotal().ToString();
            string targetTotal = GetTargetStatisticTotal().ToString();
            string targetName = GetTargetName();
            
            return $"{challenger.name}: {challengerTotal} vs {targetTotal}: {targetName}";
        }

        /// <summary>
        /// Get the challenger's total skill including honor bid
        /// </summary>
        /// <returns>Total skill value, or "-" if challenger is not in play</returns>
        public object GetChallengerStatisticTotal()
        {
            if (challenger?.location != Locations.PlayArea)
            {
                return "-";
            }

            int total = GetSkillStatistic(challenger);
            
            // Add honor bid if bidding is finished
            if (bidFinished && challenger.controller != null)
            {
                total += challenger.controller.HonorBid;
            }

            return total;
        }

        /// <summary>
        /// Get the target's total skill including honor bid
        /// </summary>
        /// <returns>Total skill value, or "-" if no targets are in play</returns>
        public object GetTargetStatisticTotal()
        {
            // Check if any targets are in play
            if (targets.All(card => card?.location != Locations.PlayArea))
            {
                return "-";
            }

            // Sum skill from all targets in play
            int total = targets
                .Where(card => card?.location == Locations.PlayArea)
                .Sum(card => GetSkillStatistic(card));

            // Add honor bid if bidding is finished
            if (bidFinished && challenger?.controller?.opponent != null)
            {
                total += challenger.controller.opponent.HonorBid;
            }

            return total;
        }

        /// <summary>
        /// Get display name for all targets
        /// </summary>
        /// <returns>Formatted string of target names</returns>
        public string GetTargetName()
        {
            if (targets.Count == 0) return "No Target";
            if (targets.Count == 1) return targets[0]?.name ?? "Unknown";
            
            return string.Join(" and ", targets.Where(t => t != null).Select(t => t.name));
        }

        /// <summary>
        /// Mark that honor bidding has finished and dueling skill should be calculated
        /// </summary>
        public void ModifyDuelingSkill()
        {
            bidFinished = true;
            
            Debug.Log($"⚔️ Duel bidding finished: {GetTotalsForDisplay()}");
        }

        /// <summary>
        /// Determine the winner and loser of the duel
        /// </summary>
        public void DetermineResult()
        {
            var challengerTotal = GetChallengerStatisticTotal();
            var targetTotal = GetTargetStatisticTotal();
            
            // Handle cases where participants are dead/not in play
            if (challengerTotal.Equals("-"))
            {
                if (!targetTotal.Equals("-") && (int)targetTotal > 0)
                {
                    // Challenger dead, target alive and has skill
                    winner = targets.Count == 1 ? (object)targets[0] : targets;
                    loser = null; // Dead characters don't lose duels
                }
                // Both dead or target has no skill - no winner
            }
            else if (targetTotal.Equals("-"))
            {
                if ((int)challengerTotal > 0)
                {
                    // Challenger alive and has skill, target dead
                    winner = challenger;
                    loser = null; // Dead characters don't lose duels
                }
                // Challenger has no skill - no winner
            }
            else
            {
                // Both sides alive, compare totals
                int challengerValue = (int)challengerTotal;
                int targetValue = (int)targetTotal;
                
                if (challengerValue > targetValue)
                {
                    // Challenger wins
                    winner = challenger;
                    loser = targets.Count == 1 ? (object)targets[0] : targets;
                }
                else if (challengerValue < targetValue)
                {
                    // Target wins
                    winner = targets.Count == 1 ? (object)targets[0] : targets;
                    loser = challenger;
                }
                // Equal totals - no winner or loser
            }

            // Apply restrictions to losing
            ProcessLoserRestrictions();
            ProcessWinnerCleanup();

            string winnerName = GetWinnerName();
            string loserName = GetLoserName();
            
            if (!string.IsNullOrEmpty(winnerName))
            {
                Debug.Log($"🏆 Duel result: {winnerName} defeats {loserName}");
            }
            else
            {
                Debug.Log($"⚔️ Duel result: No winner");
            }
        }

        /// <summary>
        /// Process restrictions on losing duels
        /// </summary>
        private void ProcessLoserRestrictions()
        {
            if (loser == null) return;

            var context = game.GetFrameworkContext();

            if (loser is List<BaseCard> loserList)
            {
                // Filter out cards that cannot lose duels
                var validLosers = loserList.Where(card => 
                    card != null && card.CheckRestrictions("loseDuels", context)).ToList();

                if (validLosers.Count == 0)
                {
                    loser = null;
                }
                else if (validLosers.Count == 1)
                {
                    loser = validLosers[0];
                }
                else
                {
                    loser = validLosers;
                }
            }
            else if (loser is BaseCard loserCard)
            {
                if (loserCard == null || !loserCard.CheckRestrictions("loseDuels", context))
                {
                    loser = null;
                }
            }
        }

        /// <summary>
        /// Clean up winner to handle single vs multiple winners
        /// </summary>
        private void ProcessWinnerCleanup()
        {
            if (winner is List<BaseCard> winnerList)
            {
                var validWinners = winnerList.Where(card => card != null).ToList();
                
                if (validWinners.Count == 0)
                {
                    winner = null;
                }
                else if (validWinners.Count == 1)
                {
                    winner = validWinners[0];
                }
                else
                {
                    winner = validWinners;
                }
            }
        }

        /// <summary>
        /// Get winner name for display
        /// </summary>
        /// <returns>Winner name or empty string</returns>
        public string GetWinnerName()
        {
            if (winner == null) return "";
            
            if (winner is BaseCard card)
                return card.name;
            
            if (winner is List<BaseCard> cards)
                return string.Join(" and ", cards.Select(c => c.name));
            
            return "";
        }

        /// <summary>
        /// Get loser name for display
        /// </summary>
        /// <returns>Loser name or empty string</returns>
        public string GetLoserName()
        {
            if (loser == null) return "";
            
            if (loser is BaseCard card)
                return card.name;
            
            if (loser is List<BaseCard> cards)
                return string.Join(" and ", cards.Select(c => c.name));
            
            return "";
        }

        /// <summary>
        /// Check if this duel has a winner
        /// </summary>
        /// <returns>True if there is a winner</returns>
        public bool HasWinner()
        {
            return winner != null;
        }

        /// <summary>
        /// Check if this duel has a loser
        /// </summary>
        /// <returns>True if there is a loser</returns>
        public bool HasLoser()
        {
            return loser != null;
        }

        /// <summary>
        /// Get winner as BaseCard (null if multiple winners)
        /// </summary>
        /// <returns>Winning card or null</returns>
        public BaseCard GetWinnerCard()
        {
            return winner as BaseCard;
        }

        /// <summary>
        /// Get loser as BaseCard (null if multiple losers)
        /// </summary>
        /// <returns>Losing card or null</returns>
        public BaseCard GetLoserCard()
        {
            return loser as BaseCard;
        }

        /// <summary>
        /// Get all winning cards
        /// </summary>
        /// <returns>List of winning cards</returns>
        public List<BaseCard> GetWinnerCards()
        {
            if (winner == null) return new List<BaseCard>();
            
            if (winner is BaseCard card)
                return new List<BaseCard> { card };
            
            if (winner is List<BaseCard> cards)
                return cards.ToList();
            
            return new List<BaseCard>();
        }

        /// <summary>
        /// Get all losing cards
        /// </summary>
        /// <returns>List of losing cards</returns>
        public List<BaseCard> GetLoserCards()
        {
            if (loser == null) return new List<BaseCard>();
            
            if (loser is BaseCard card)
                return new List<BaseCard> { card };
            
            if (loser is List<BaseCard> cards)
                return cards.ToList();
            
            return new List<BaseCard>();
        }

        /// <summary>
        /// Chain this duel with a previous duel
        /// </summary>
        /// <param name="priorDuel">Previous duel in the chain</param>
        public void SetPreviousDuel(Duel priorDuel)
        {
            previousDuel = priorDuel;
        }

        /// <summary>
        /// Get summary of duel for UI display
        /// </summary>
        /// <returns>Duel summary</returns>
        public DuelSummary GetSummary()
        {
            return new DuelSummary
            {
                challengerName = challenger?.name ?? "Unknown",
                targetNames = targets.Where(t => t != null).Select(t => t.name).ToList(),
                duelType = duelType,
                challengerTotal = GetChallengerStatisticTotal(),
                targetTotal = GetTargetStatisticTotal(),
                bidFinished = bidFinished,
                winnerName = GetWinnerName(),
                loserName = GetLoserName(),
                displayText = GetTotalsForDisplay()
            };
        }

        /// <summary>
        /// IronPython integration for duel events
        /// </summary>
        /// <param name="eventType">Type of duel event</param>
        /// <param name="parameters">Event parameters</param>
        public void ExecuteDuelScript(string eventType, params object[] parameters)
        {
            if (game.enablePythonScripting)
            {
                var allParams = new List<object> { this }.Concat(parameters).ToArray();
                game.ExecuteCardScript("duel_engine", eventType, allParams);
            }
        }

        /// <summary>
        /// Duel event handlers for IronPython
        /// </summary>
        public void OnDuelInitiated()
        {
            ExecuteDuelScript("on_duel_initiated", challenger, targets, duelType);
        }

        public void OnBiddingFinished()
        {
            ExecuteDuelScript("on_bidding_finished", challenger.controller.HonorBid, 
                             challenger.controller.opponent.HonorBid);
        }

        public void OnDuelResolved()
        {
            ExecuteDuelScript("on_duel_resolved", winner, loser, duelType);
        }

        /// <summary>
        /// Cleanup when duel is destroyed
        /// </summary>
        private void OnDestroy()
        {
            targets?.Clear();
            Debug.Log($"⚔️ Duel destroyed: {challenger?.name ?? "Unknown"} vs {GetTargetName()}");
        }
    }

    /// <summary>
    /// Summary data for duel UI display
    /// </summary>
    [System.Serializable]
    public class DuelSummary
    {
        public string challengerName;
        public List<string> targetNames;
        public string duelType;
        public object challengerTotal;
        public object targetTotal;
        public bool bidFinished;
        public string winnerName;
        public string loserName;
        public string displayText;
    }

    /// <summary>
    /// Extension methods for duel management
    /// </summary>
    public static class DuelExtensions
    {
        /// <summary>
        /// Check if game is currently during a duel
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>True if in a duel</returns>
        public static bool IsDuringDuel(this Game game)
        {
            return game.currentDuel != null;
        }

        /// <summary>
        /// Check if a card is involved in the current duel
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if involved in current duel</returns>
        public static bool IsInvolvedInCurrentDuel(this BaseCard card)
        {
            return card.game.currentDuel?.IsInvolved(card) ?? false;
        }

        /// <summary>
        /// Check if a card is involved in any active duel
        /// </summary>
        /// <param name="card">Card to check</param>
        /// <returns>True if involved in any duel</returns>
        public static bool IsInvolvedInAnyDuel(this BaseCard card)
        {
            return card.game.currentDuel?.IsInvolvedInAnyDuel(card) ?? false;
        }

        /// <summary>
        /// Initiate a military duel
        /// </summary>
        /// <param name="challenger">Challenging character</param>
        /// <param name="target">Target character</param>
        /// <returns>Created duel</returns>
        public static Duel InitiateMilitaryDuel(this BaseCard challenger, BaseCard target)
        {
            return InitiateDuel(challenger, target, DuelTypes.Military);
        }

        /// <summary>
        /// Initiate a political duel
        /// </summary>
        /// <param name="challenger">Challenging character</param>
        /// <param name="target">Target character</param>
        /// <returns>Created duel</returns>
        public static Duel InitiatePoliticalDuel(this BaseCard challenger, BaseCard target)
        {
            return InitiateDuel(challenger, target, DuelTypes.Political);
        }

        /// <summary>
        /// Initiate a glory duel
        /// </summary>
        /// <param name="challenger">Challenging character</param>
        /// <param name="target">Target character</param>
        /// <returns>Created duel</returns>
        public static Duel InitiateGloryDuel(this BaseCard challenger, BaseCard target)
        {
            return InitiateDuel(challenger, target, DuelTypes.Glory);
        }

        /// <summary>
        /// Initiate a duel of any type
        /// </summary>
        /// <param name="challenger">Challenging character</param>
        /// <param name="target">Target character</param>
        /// <param name="duelType">Type of duel</param>
        /// <param name="customStatistic">Custom statistic function</param>
        /// <returns>Created duel</returns>
        public static Duel InitiateDuel(this BaseCard challenger, BaseCard target, string duelType, 
                                       System.Func<BaseCard, int> customStatistic = null)
        {
            var duelGO = new GameObject($"Duel_{challenger.name}_vs_{target.name}");
            duelGO.transform.SetParent(challenger.game.transform);
            
            var duel = duelGO.AddComponent<Duel>();
            duel.Initialize(challenger.game, challenger, target, duelType, customStatistic);
            
            // Set as current duel
            challenger.game.currentDuel = duel;
            
            duel.OnDuelInitiated();
            
            return duel;
        }

        /// <summary>
        /// Initiate a multi-target duel
        /// </summary>
        /// <param name="challenger">Challenging character</param>
        /// <param name="targets">Target characters</param>
        /// <param name="duelType">Type of duel</param>
        /// <param name="customStatistic">Custom statistic function</param>
        /// <returns>Created duel</returns>
        public static Duel InitiateDuel(this BaseCard challenger, List<BaseCard> targets, string duelType,
                                       System.Func<BaseCard, int> customStatistic = null)
        {
            string targetNames = string.Join("_", targets.Select(t => t.name));
            var duelGO = new GameObject($"Duel_{challenger.name}_vs_{targetNames}");
            duelGO.transform.SetParent(challenger.game.transform);
            
            var duel = duelGO.AddComponent<Duel>();
            duel.Initialize(challenger.game, challenger, targets, duelType, customStatistic);
            
            // Set as current duel
            challenger.game.currentDuel = duel;
            
            duel.OnDuelInitiated();
            
            return duel;
        }
    }
}