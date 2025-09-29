using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents one of the five elemental rings in Legend of the Five Rings.
    /// Rings can be claimed by players and provide various effects and fate.
    /// </summary>
    public partial class Ring : EffectSource
    {
        [Header("Ring Identity")]
        public string element;
        public string printedType = "ring";

        [Header("Ring State")]
        public bool claimed = false;
        public string claimedBy = "";
        public string conflictType;
        public bool contested = false;
        public int fate = 0;

        [Header("Ring Attachments")]
        public List<BaseCard> attachments = new List<BaseCard>();

        [Header("Ring Menu")]
        public List<RingMenuOption> menu = new List<RingMenuOption>();
        
        [Header("Ring Identity")]
        public string uuid = System.Guid.NewGuid().ToString();

        /// <summary>
        /// Initialize the ring with element and conflict type
        /// </summary>
        /// <param name="gameInstance">Game instance</param>
        /// <param name="ringElement">Element name (air, earth, fire, void, water)</param>
        /// <param name="initialConflictType">Initial conflict type (military or political)</param>
        public void Initialize(Game gameInstance, string ringElement, string initialConflictType)
        {
            element = ringElement;
            conflictType = initialConflictType;
            
            string ringName = char.ToUpper(ringElement[0]) + ringElement.Substring(1) + " Ring";
            base.Initialize(gameInstance, ringName);
            
            SetupRingMenu();
            
            Debug.Log($"💍 {ringName} initialized as {conflictType}");
        }

        /// <summary>
        /// Set up the ring's context menu for manual mode
        /// </summary>
        private void SetupRingMenu()
        {
            menu = new List<RingMenuOption>
            {
                new RingMenuOption { command = "flip", text = "Flip" },
                new RingMenuOption { command = "claim", text = "Claim" },
                new RingMenuOption { command = "contested", text = "Switch this ring to contested" },
                new RingMenuOption { command = "unclaimed", text = "Set to unclaimed" },
                new RingMenuOption { command = "addfate", text = "Add 1 fate" },
                new RingMenuOption { command = "remfate", text = "Remove 1 fate" },
                new RingMenuOption { command = "takefate", text = "Take all fate" },
                new RingMenuOption { command = "conflict", text = "Initiate Conflict" }
            };
        }

        /// <summary>
        /// Checks if this ring is considered claimed by a player (including effects)
        /// </summary>
        /// <param name="player">Player to check for, or null to check any player</param>
        /// <returns>True if the ring is considered claimed by the player</returns>
        public bool IsConsideredClaimed(Player player = null)
        {
            bool CheckPlayer(Player p)
            {
                // Check for effects that make this ring considered claimed
                var claimEffects = GetEffects(EffectNames.ConsiderRingAsClaimed);
                foreach (var effect in claimEffects)
                {
                    if (effect is System.Func<Player, bool> matchFunc && matchFunc(p))
                        return true;
                }
                
                // Check actual claimed status
                return claimedBy == p.name;
            }

            if (player != null)
            {
                return CheckPlayer(player);
            }

            // Check if claimed by any player
            return game.GetPlayers().Any(p => CheckPlayer(p));
        }

        /// <summary>
        /// Checks if this ring matches the specified conflict type
        /// </summary>
        /// <param name="type">Conflict type to check</param>
        /// <returns>True if ring matches the type and is not unclaimed</returns>
        public bool IsConflictType(string type)
        {
            return !IsUnclaimed() && type == conflictType;
        }

        /// <summary>
        /// Checks if a player can declare this ring for conflict
        /// </summary>
        /// <param name="player">Player attempting to declare</param>
        /// <returns>True if the ring can be declared</returns>
        public bool CanDeclare(Player player)
        {
            // Check for effects preventing ring declaration
            var cannotDeclareEffects = GetEffects(EffectNames.CannotDeclareRing);
            foreach (var effect in cannotDeclareEffects)
            {
                if (effect is System.Func<Player, bool> matchFunc && matchFunc(player))
                    return false;
            }

            return !claimed;
        }

        /// <summary>
        /// Checks if the ring is unclaimed (not claimed and not contested)
        /// </summary>
        /// <returns>True if the ring is unclaimed</returns>
        public bool IsUnclaimed()
        {
            return !contested && !claimed;
        }

        /// <summary>
        /// Checks if the ring is claimed by any player
        /// </summary>
        /// <returns>True if the ring is claimed</returns>
        public bool IsClaimed()
        {
            return claimed;
        }

        /// <summary>
        /// Check if specific restrictions apply to this ring
        /// </summary>
        /// <param name="restrictionType">Type of restriction to check</param>
        /// <param name="context">Optional context</param>
        /// <returns>True if restriction passes</returns>
        public bool CheckRestrictions(string restrictionType, object context = null)
        {
            // Basic restriction checking - can be expanded
            return !AnyEffect($"cannot_{restrictionType}");
        }

        /// <summary>
        /// Flips the conflict type of the ring between military and political
        /// </summary>
        public void FlipConflictType()
        {
            if (conflictType == "military")
            {
                conflictType = "political";
            }
            else
            {
                conflictType = "military";
            }

            Debug.Log($"💍 {name} flipped to {conflictType}");
        }

        /// <summary>
        /// Gets the primary element of this ring
        /// </summary>
        /// <returns>Primary element name</returns>
        public string GetElement()
        {
            return element;
        }

        /// <summary>
        /// Gets all elements associated with this ring (including added elements)
        /// </summary>
        /// <returns>List of all elements this ring has</returns>
        public List<string> GetElements()
        {
            var elements = new List<string> { element };
            
            // Add elements from effects
            var addElementEffects = GetEffects(EffectNames.AddElement);
            elements.AddRange(addElementEffects.Cast<string>());

            // Add elements from attacking characters during conflicts
            if (game.IsDuringConflict())
            {
                foreach (var attacker in game.currentConflict.attackers)
                {
                    // Elements from the attacking character
                    var attackerElements = attacker.GetEffects(EffectNames.AddElementAsAttacker);
                    elements.AddRange(attackerElements.Cast<string>());

                    // Elements from attachments on attacking characters
                    foreach (var attachment in attacker.attachments)
                    {
                        var attachmentElements = attachment.GetEffects(EffectNames.AddElementAsAttacker);
                        elements.AddRange(attachmentElements.Cast<string>());
                    }
                }
            }

            return elements.Distinct().ToList();
        }

        /// <summary>
        /// Checks if this ring has the specified element
        /// </summary>
        /// <param name="checkElement">Element to check for</param>
        /// <returns>True if the ring has this element</returns>
        public bool HasElement(string checkElement)
        {
            return GetElements().Contains(checkElement);
        }

        /// <summary>
        /// Gets the current fate on this ring
        /// </summary>
        /// <returns>Amount of fate on the ring</returns>
        public int GetFate()
        {
            return fate;
        }

        /// <summary>
        /// Gets the ring's context menu for manual mode
        /// </summary>
        /// <returns>Menu options or null if not in manual mode</returns>
        public List<RingMenuOption> GetMenu()
        {
            if (menu.Count == 0 || !game.manualMode)
            {
                return null;
            }

            var ringMenu = new List<RingMenuOption>
            {
                new RingMenuOption { command = "click", text = "Select Ring" }
            };
            
            ringMenu.AddRange(menu);
            return ringMenu;
        }

        /// <summary>
        /// Modifies the fate on this ring
        /// </summary>
        /// <param name="fateAmount">Amount to modify fate by (can be negative)</param>
        public void ModifyFate(int fateAmount)
        {
            fate = Mathf.Max(fate + fateAmount, 0);
            
            if (fateAmount != 0)
            {
                Debug.Log($"💍 {name} fate modified by {fateAmount} (now {fate})");
            }
        }

        /// <summary>
        /// Removes all fate from this ring
        /// </summary>
        public void RemoveFate()
        {
            if (fate > 0)
            {
                Debug.Log($"💍 {name} fate removed ({fate} → 0)");
                fate = 0;
            }
        }

        /// <summary>
        /// Claims this ring for the specified player
        /// </summary>
        /// <param name="player">Player claiming the ring</param>
        public void ClaimRing(Player player)
        {
            claimed = true;
            claimedBy = player.name;
            contested = false;
            
            Debug.Log($"💍 {name} claimed by {player.name}");
            
            // Execute ring claimed effects via IronPython if available
            ExecuteRingScript("on_claimed", player);
        }

        /// <summary>
        /// Resets the ring to unclaimed state
        /// </summary>
        public void ResetRing()
        {
            bool wasClaimedOrContested = claimed || contested;
            
            claimed = false;
            claimedBy = "";
            contested = false;
            
            if (wasClaimedOrContested)
            {
                Debug.Log($"💍 {name} reset to unclaimed");
                ExecuteRingScript("on_reset");
            }
        }

        /// <summary>
        /// Return this ring to the unclaimed pool
        /// </summary>
        public virtual void ReturnToUnclaimed()
        {
            claimed = false;
            claimedBy = "";
            contested = false;
            conflictType = null;
            
            // Reset any ring-specific state
            if (fate > 0)
            {
                Debug.Log($"💍 {name} returning to unclaimed pool (removing {fate} fate)");
                fate = 0;
            }
            
            Debug.Log($"💍 {name} returned to unclaimed pool");
        }

        /// <summary>
        /// Sets the ring as contested
        /// </summary>
        public void SetContested()
        {
            if (!contested)
            {
                contested = true;
                claimed = false;
                claimedBy = "";
                
                Debug.Log($"💍 {name} is now contested");
                ExecuteRingScript("on_contested");
            }
        }

        /// <summary>
        /// Removes an attachment from this ring
        /// </summary>
        /// <param name="card">Attachment to remove</param>
        public void RemoveAttachment(BaseCard card)
        {
            if (attachments.Remove(card))
            {
                Debug.Log($"💍 Removed attachment {card.name} from {name}");
            }
        }

        /// <summary>
        /// Adds an attachment to this ring
        /// </summary>
        /// <param name="card">Attachment to add</param>
        public void AddAttachment(BaseCard card)
        {
            if (!attachments.Contains(card))
            {
                attachments.Add(card);
                // TODO: Ring attachments - parent relationship needs proper handling
                // card.parent = this; // Ring is not BaseCard, need different approach
                Debug.Log($"💍 Added attachment {card.name} to {name}");
            }
        }

        /// <summary>
        /// Checks if this ring can have attachments added
        /// </summary>
        /// <returns>True if attachments can be added</returns>
        public bool CanHaveAttachments()
        {
            // Check for effects preventing attachments
            return !AnyEffect(EffectNames.CannotHaveAttachments);
        }

        /// <summary>
        /// Gets the ring's state for network synchronization
        /// </summary>
        /// <param name="activePlayer">The player viewing the state</param>
        /// <returns>Ring state data</returns>
        public RingState GetState(Player activePlayer)
        {
            var selectionState = new Dictionary<string, object>();

            if (activePlayer != null)
            {
                // TODO: GetRingSelectionState needs to return Dictionary<string, object>
                // selectionState = activePlayer.GetRingSelectionState(this);
                selectionState = new Dictionary<string, object>(); // Temporary fix
            }

            var attachmentSummaries = attachments.Count > 0
                ? attachments.Select(attachment => new { name = attachment.name }).ToList<object>()
                : new List<object>();

            var state = new RingState
            {
                claimed = claimed,
                claimedBy = claimedBy,
                conflictType = conflictType,
                contested = contested,
                selected = false, // TODO: Implement conflict ring selection logic
                element = element,
                fate = fate,
                menu = GetMenu(),
                attachments = attachmentSummaries,
                elements = GetElements(),
                canDeclare = game.GetPlayers().Any(p => CanDeclare(p)),
                isUnclaimed = IsUnclaimed()
            };

            // Merge selection state
            foreach (var kvp in selectionState)
            {
                switch (kvp.Key)
                {
                    case "selected":
                        state.selected = (bool)kvp.Value;
                        break;
                    case "selectable":
                        state.selectable = (bool)kvp.Value;
                        break;
                    // Add other selection state properties as needed
                }
            }

            return state;
        }

        /// <summary>
        /// Gets a short summary of the ring
        /// </summary>
        /// <returns>Ring summary data</returns>
        public RingSummary GetShortSummary()
        {
            // TODO: Implement base summary when EffectSource supports it
            // var baseSummary = base.GetShortSummaryForControls(null);
            
            return new RingSummary
            {
                name = name,
                uuid = uuid,
                element = element,
                conflictType = conflictType,
                claimed = claimed,
                claimedBy = claimedBy,
                contested = contested,
                fate = fate,
                attachmentCount = attachments.Count
            };
        }

        /// <summary>
        /// IronPython integration for ring events
        /// </summary>
        /// <param name="eventType">Type of ring event</param>
        /// <param name="parameters">Event parameters</param>
        private void ExecuteRingScript(string eventType, params object[] parameters)
        {
            if (game.enablePythonScripting)
            {
                // Use element name as script identifier
                string scriptName = $"ring_{element}";
                var allParams = new List<object> { this }.Concat(parameters).ToArray();
                game.ExecuteCardScript(scriptName, eventType, allParams);
            }
        }

        /// <summary>
        /// Ring-specific event handlers for IronPython
        /// </summary>
        public void OnClaimed(Player player)
        {
            ExecuteRingScript("on_claimed", player);
        }

        public void OnContested()
        {
            ExecuteRingScript("on_contested");
        }

        public void OnReset()
        {
            ExecuteRingScript("on_reset");
        }

        public void OnConflictDeclared(Player attackingPlayer, Player defendingPlayer)
        {
            ExecuteRingScript("on_conflict_declared", attackingPlayer, defendingPlayer);
        }

        public void OnConflictResolved(Player winner, Player loser)
        {
            ExecuteRingScript("on_conflict_resolved", winner, loser);
        }

        /// <summary>
        /// Check if this ring provides a specific effect when claimed
        /// </summary>
        /// <param name="effectName">Name of the effect to check</param>
        /// <returns>True if the ring provides this effect when claimed</returns>
        public bool ProvidesEffect(string effectName)
        {
            // This can be extended to check for ring-specific effects
            // or effects added by attachments
            return GetEffects(effectName).Any();
        }

        /// <summary>
        /// Get the strength bonus this ring provides (if it's a province ring)
        /// </summary>
        /// <returns>Strength bonus amount</returns>
        public virtual int GetStrengthBonus()
        {
            // Override in province-specific ring implementations
            return 0;
        }

        /// <summary>
        /// Handle ring being targeted by an effect
        /// </summary>
        /// <param name="effect">Effect targeting this ring</param>
        /// <param name="source">Source of the effect</param>
        public void OnTargeted(object effect, EffectSource source)
        {
            ExecuteRingScript("on_targeted", effect, source);
        }

        /// <summary>
        /// Debug method to display ring status
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogStatus()
        {
            string status = $"💍 {name} ({conflictType})";
            
            if (claimed)
                status += $" - Claimed by {claimedBy}";
            else if (contested)
                status += " - Contested";
            else
                status += " - Unclaimed";
                
            if (fate > 0)
                status += $" - {fate} fate";
                
            if (attachments.Count > 0)
                status += $" - {attachments.Count} attachments";

            Debug.Log(status);
        }

        /// <summary>
        /// Get effects of a specific type (inherited from EffectSource)
        /// </summary>
        protected List<object> GetEffects(string effectName)
        {
            // This would normally be implemented in EffectSource base class
            // For now, return empty list to prevent compilation errors
            return new List<object>();
        }

        /// <summary>
        /// Check if any effect of a type exists (inherited from EffectSource)
        /// </summary>
        protected bool AnyEffect(string effectName)
        {
            return GetEffects(effectName).Count > 0;
        }

        /// <summary>
        /// Cleanup when ring is destroyed
        /// </summary>
        // Property aliases for API compatibility
        public string Element => element;
        public bool Claimed => claimed;
        public int Fate 
        { 
            get => fate; 
            set => fate = value; 
        }

        // Missing properties for compilation compatibility
        /// <summary>
        /// Gets or sets the conflict type (uppercase property alias)
        /// </summary>
        public string ConflictType 
        { 
            get => conflictType;
            set => conflictType = value;
        }
        
        /// <summary>
        /// Gets or sets the claimed by player (uppercase property alias)
        /// </summary>
        public string ClaimedBy 
        { 
            get => claimedBy;
            set => claimedBy = value;
        }
        
        /// <summary>
        /// Gets or sets whether the ring is contested (uppercase property alias)
        /// </summary>
        public bool Contested 
        { 
            get => contested;
            set => contested = value;
        }

        protected override void OnDestroy()
        {
            attachments.Clear();
            base.OnDestroy();
        }
    }

    /// <summary>
    /// Ring state for network synchronization
    /// </summary>
    [System.Serializable]
    public class RingState
    {
        public bool claimed;
        public string claimedBy;
        public string conflictType;
        public bool contested;
        public bool selected;
        public string element;
        public int fate;
        public List<RingMenuOption> menu;
        public List<object> attachments;
        public List<string> elements;
        public bool canDeclare;
        public bool isUnclaimed;
        public bool selectable;
    }

    /// <summary>
    /// Ring summary for UI lists
    /// </summary>
    [System.Serializable]
    public class RingSummary
    {
        public string name;
        public string uuid;
        public string element;
        public string conflictType;
        public bool claimed;
        public string claimedBy;
        public bool contested;
        public int fate;
        public int attachmentCount;
    }

    /// <summary>
    /// Ring menu option for manual mode
    /// </summary>
    [System.Serializable]
    public class RingMenuOption
    {
        public string command;
        public string text;
        public string arg;
        public bool disabled;
    }

    /// <summary>
    /// Ring elements enumeration (moved to Constants.cs to avoid duplicate definition)
    /// </summary>
    // RingElements class moved to Constants.cs to avoid CS0101 duplicate definition errors

    /// <summary>
    /// Ring-specific effect names
    /// </summary>
    public static partial class EffectNames
    {
        public const string ConsiderRingAsClaimed = "considerRingAsClaimed";
        public const string CannotDeclareRing = "cannotDeclareRing";
        public const string AddElement = "addElement";
        public const string AddElementAsAttacker = "addElementAsAttacker";
        public const string CannotHaveAttachments = "cannotHaveAttachments";
        public const string RingClaimedEffect = "ringClaimedEffect";
        public const string RingContestedEffect = "ringContestedEffect";
    }

    /// <summary>
    /// Extension methods for ring management
    /// </summary>
    public static class RingExtensions
    {
        /// <summary>
        /// Get all unclaimed rings in the game
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>List of unclaimed rings</returns>
        public static List<Ring> GetUnclaimedRings(this Game game)
        {
            return game.rings.Values.Where(ring => ring.IsUnclaimed()).ToList();
        }

        /// <summary>
        /// Get all rings claimed by a specific player
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="player">Player to check</param>
        /// <returns>List of rings claimed by the player</returns>
        public static List<Ring> GetRingsClaimedBy(this Game game, Player player)
        {
            return game.rings.Values.Where(ring => ring.IsConsideredClaimed(player)).ToList();
        }

        /// <summary>
        /// Get rings of a specific conflict type
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="conflictType">Conflict type to filter by</param>
        /// <returns>List of rings matching the conflict type</returns>
        public static List<Ring> GetRingsByConflictType(this Game game, string conflictType)
        {
            return game.rings.Values.Where(ring => ring.IsConflictType(conflictType)).ToList();
        }

        /// <summary>
        /// Get rings with fate on them
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>List of rings with fate</returns>
        public static List<Ring> GetRingsWithFate(this Game game)
        {
            return game.rings.Values.Where(ring => ring.fate > 0).ToList();
        }

        /// <summary>
        /// Get total fate on all rings
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>Total fate across all rings</returns>
        public static int GetTotalRingFate(this Game game)
        {
            return game.rings.Values.Sum(ring => ring.fate);
        }

        /// <summary>
        /// Reset all rings to unclaimed state
        /// </summary>
        /// <param name="game">Game instance</param>
        public static void ResetAllRings(this Game game)
        {
            foreach (var ring in game.rings.Values)
            {
                ring.ResetRing();
            }
        }

        /// <summary>
        /// Find ring by element
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="element">Element to find</param>
        /// <returns>Ring with the specified element, or null if not found</returns>
        public static Ring GetRingByElement(this Game game, string element)
        {
            return game.rings.GetValueOrDefault(element);
        }
    }
}