using System;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a menu command (alias for MenuItem for backward compatibility)
    /// </summary>
    public class MenuCommand
    {
        public string command;
        public object args;
        public string text;
        public bool disabled;

        public MenuCommand() { }
        
        public MenuCommand(string command, object args = null, string text = null, bool disabled = false)
        {
            this.command = command;
            this.args = args;
            this.text = text ?? command;
            this.disabled = disabled;
        }
    }
    
    /// <summary>
    /// Represents a menu item with command and arguments
    /// </summary>
    public class MenuItem
    {
        public string Command { get; set; }
        public object Args { get; set; }
        public string Text { get; set; }

        public MenuItem(string command, object args = null, string text = null)
        {
            Command = command;
            Args = args;
            Text = text ?? command;
        }
    }

    /// <summary>
    /// Static class handling menu commands for cards and rings
    /// </summary>
    public static class MenuCommands
    {
        /// <summary>
        /// Handle card menu click commands
        /// </summary>
        public static void CardMenuClick(MenuItem menuItem, Game game, Player player, BaseCard card)
        {
            switch (menuItem.Command)
            {
                case "bow":
                    if (card.Bowed)
                    {
                        game.AddMessage("{0} readies {1}", player, card);
                        card.Ready();
                    }
                    else
                    {
                        game.AddMessage("{0} bows {1}", player, card);
                        card.Bow();
                    }
                    break;

                case "honor":
                    game.AddMessage("{0} honors {1}", player, card);
                    card.Honor();
                    break;

                case "dishonor":
                    game.AddMessage("{0} dishonors {1}", player, card);
                    card.Dishonor();
                    break;

                case "addfate":
                    game.AddMessage("{0} adds a fate to {1}", player, card);
                    card.ModifyFate(1);
                    break;

                case "remfate":
                    game.AddMessage("{0} removes a fate from {1}", player, card);
                    card.ModifyFate(-1);
                    break;

                case "move":
                    if (game.CurrentConflict != null)
                    {
                        if (card.IsParticipating())
                        {
                            game.AddMessage("{0} moves {1} out of the conflict", player, card);
                            game.CurrentConflict.RemoveFromConflict(card);
                        }
                        else
                        {
                            game.AddMessage("{0} moves {1} into the conflict", player, card);
                            if (card.Controller.IsAttackingPlayer())
                            {
                                game.CurrentConflict.AddAttacker(card);
                            }
                            else if (card.Controller.IsDefendingPlayer())
                            {
                                game.CurrentConflict.AddDefender(card);
                            }
                        }
                    }
                    break;

                case "control":
                    if (player.Opponent != null)
                    {
                        game.AddMessage("{0} gives {1} control of {2}", player, player.Opponent, card);
                        card.SetDefaultController(player.Opponent);
                    }
                    break;

                case "reveal":
                    game.AddMessage("{0} reveals {1}", player, card);
                    card.Facedown = false;
                    break;

                case "hide":
                    game.AddMessage("{0} flips {1} facedown", player, card);
                    card.Facedown = true;
                    break;

                case "break":
                    game.AddMessage("{0} {1} {2}", player, card.IsBroken ? "unbreaks" : "breaks", card);
                    card.IsBroken = !card.IsBroken;
                    
                    if (card.Location == Locations.StrongholdProvince && card.IsBroken)
                    {
                        game.RecordWinner(player.Opponent, "conquest");
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown card menu command: {menuItem.Command}");
                    break;
            }
        }

        /// <summary>
        /// Handle ring menu click commands
        /// </summary>
        public static void RingMenuClick(MenuItem menuItem, Game game, Player player, Ring ring)
        {
            switch (menuItem.Command)
            {
                case "flip":
                    if (game.CurrentConflict?.Ring != null)
                    {
                        game.AddMessage("{0} switches the conflict type", player);
                        game.CurrentConflict.SwitchType();
                    }
                    else
                    {
                        ring.FlipConflictType();
                    }
                    break;

                case "claim":
                    game.AddMessage("{0} claims the {1} ring", player, ring.Element);
                    ring.ClaimRing(player);
                    break;

                case "unclaimed":
                    game.AddMessage("{0} sets the {1} ring to unclaimed", player, ring.Element);
                    ring.ResetRing();
                    break;

                case "contested":
                    if (game.CurrentConflict != null)
                    {
                        if (!ring.Claimed)
                        {
                            game.AddMessage("{0} switches the conflict to contest the {1} ring", player, ring.Element);
                            game.CurrentConflict.SwitchElement(ring.Element);
                        }
                        else
                        {
                            game.AddMessage("{0} tried to switch the conflict to contest the {1} ring, but it's already claimed", player, ring.Element);
                        }
                    }
                    break;

                case "addfate":
                    game.AddMessage("{0} adds a fate to the {1} ring", player, ring.Element);
                    ring.ModifyFate(1);
                    break;

                case "remfate":
                    game.AddMessage("{0} removes a fate from the {1} ring", player, ring.Element);
                    ring.ModifyFate(-1);
                    break;

                case "takefate":
                    game.AddMessage("{0} takes all the fate from the {1} ring and adds it to their pool", player, ring.Element);
                    player.ModifyFate(ring.Fate);
                    ring.Fate = 0;
                    break;

                case "conflict":
                    if (game.CurrentActionWindow?.WindowName == "preConflict")
                    {
                        game.AddMessage("{0} initiates a conflict", player);
                        var conflict = new Conflict(game, player, player.Opponent, ring, null, null);
                        game.CurrentConflict = conflict;
                        game.QueueStep(new ConflictFlow(game, conflict, true));
                        game.QueueSimpleStep(() => { game.CurrentConflict = null; return true; });
                    }
                    else
                    {
                        game.AddMessage("{0} tried to initiate a conflict, but this can only be done in a pre-conflict action window", player);
                    }
                    break;

                default:
                    Debug.LogWarning($"Unknown ring menu command: {menuItem.Command}");
                    break;
            }
        }

        /// <summary>
        /// Create a standard card menu item
        /// </summary>
        public static MenuItem CreateCardMenuItem(string command, string text = null, object args = null)
        {
            return new MenuItem(command, args, text);
        }

        /// <summary>
        /// Create a standard ring menu item
        /// </summary>
        public static MenuItem CreateRingMenuItem(string command, string text = null, object args = null)
        {
            return new MenuItem(command, args, text);
        }

        /// <summary>
        /// Get available card menu commands based on card state
        /// </summary>
        public static MenuItem[] GetAvailableCardMenuItems(BaseCard card, Game game, Player player)
        {
            var items = new System.Collections.Generic.List<MenuItem>();

            // Basic actions always available
            items.Add(CreateCardMenuItem("bow", card.Bowed ? "Ready" : "Bow"));
            items.Add(CreateCardMenuItem("honor", "Honor"));
            items.Add(CreateCardMenuItem("dishonor", "Dishonor"));
            items.Add(CreateCardMenuItem("addfate", "Add Fate"));
            
            if (card.Fate > 0)
                items.Add(CreateCardMenuItem("remfate", "Remove Fate"));

            // Conflict-related actions
            if (game.CurrentConflict != null && card.CanParticipateInConflict())
            {
                items.Add(CreateCardMenuItem("move", card.IsParticipating() ? "Move Out" : "Move In"));
            }

            // Control actions
            if (player.Opponent != null)
                items.Add(CreateCardMenuItem("control", "Give Control"));

            // Visibility actions
            if (card.Facedown)
                items.Add(CreateCardMenuItem("reveal", "Reveal"));
            else
                items.Add(CreateCardMenuItem("hide", "Hide"));

            // Province breaking
            if (card is ProvinceCard)
                items.Add(CreateCardMenuItem("break", card.IsBroken ? "Unbreak" : "Break"));

            return items.ToArray();
        }

        /// <summary>
        /// Get available ring menu commands based on ring state
        /// </summary>
        public static MenuItem[] GetAvailableRingMenuItems(Ring ring, Game game, Player player)
        {
            var items = new System.Collections.Generic.List<MenuItem>();

            // Basic ring actions
            items.Add(CreateRingMenuItem("flip", "Flip Type"));
            items.Add(CreateRingMenuItem("claim", "Claim"));
            items.Add(CreateRingMenuItem("unclaimed", "Reset"));
            items.Add(CreateRingMenuItem("addfate", "Add Fate"));
            
            if (ring.Fate > 0)
            {
                items.Add(CreateRingMenuItem("remfate", "Remove Fate"));
                items.Add(CreateRingMenuItem("takefate", "Take All Fate"));
            }

            // Conflict actions
            if (game.CurrentConflict != null)
                items.Add(CreateRingMenuItem("contested", "Switch Element"));

            if (game.CurrentActionWindow?.WindowName == "preConflict")
                items.Add(CreateRingMenuItem("conflict", "Initiate Conflict"));

            return items.ToArray();
        }
    }
}
