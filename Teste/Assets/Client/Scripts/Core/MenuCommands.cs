using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Helper methods for handling menu command interactions
    /// </summary>
    public static class MenuCommandsHelper
    {
        /// <summary>
        /// Handles when a card menu command is clicked
        /// </summary>
        /// <param name="menuItem">The selected menu item</param>
        /// <param name="game">Current game instance</param>
        /// <param name="player">Player who made the selection</param>
        /// <param name="card">Target card</param>
        public static void CardMenuClick(MenuCommand menuItem, Game game, Player player, BaseCard card) 
        { 
            // Handle card menu click logic
            if (menuItem != null && game != null && player != null && card != null)
            {
                game.AddMessage("{0} selected {1} for {2}", player.name, menuItem.text, card.name);
                
                // Execute the menu command based on its type
                switch (menuItem.command)
                {
                    case MenuCommands.Pass:
                        game.Pipeline?.HandleMenuCommand(player, "pass", menuItem.uuid, menuItem.method);
                        break;
                    case MenuCommands.Done:
                        game.Pipeline?.HandleMenuCommand(player, "done", menuItem.uuid, menuItem.method);
                        break;
                    case MenuCommands.Cancel:
                        game.Pipeline?.HandleMenuCommand(player, "cancel", menuItem.uuid, menuItem.method);
                        break;
                    default:
                        game.Pipeline?.HandleMenuCommand(player, menuItem.command, menuItem.uuid, menuItem.method);
                        break;
                }
            }
        }
        
        /// <summary>
        /// Handles when a ring menu command is clicked
        /// </summary>
        /// <param name="menuItem">The selected menu item</param>
        /// <param name="game">Current game instance</param>
        /// <param name="player">Player who made the selection</param>
        /// <param name="ring">Target ring</param>
        public static void RingMenuClick(MenuCommand menuItem, Game game, Player player, Ring ring) 
        { 
            // Handle ring menu click logic
            if (menuItem != null && game != null && player != null && ring != null)
            {
                game.AddMessage("{0} selected {1} for {2} ring", player.name, menuItem.text, ring.name);
                
                // Execute the menu command for the ring
                game.Pipeline?.HandleRingClicked(player, ring);
            }
        }

        /// <summary>
        /// Creates a standard menu command
        /// </summary>
        /// <param name="command">Command identifier</param>
        /// <param name="text">Display text</param>
        /// <param name="arg">Optional argument</param>
        /// <param name="uuid">Optional UUID</param>
        /// <param name="method">Optional method name</param>
        /// <returns>New MenuCommand instance</returns>
        public static MenuCommand CreateCommand(string command, string text, object arg = null, string uuid = null, string method = null)
        {
            return new MenuCommand(command, text, arg, uuid, method);
        }

        /// <summary>
        /// Creates a disabled menu command
        /// </summary>
        /// <param name="text">Display text for disabled command</param>
        /// <returns>New disabled MenuCommand instance</returns>
        public static MenuCommand CreateDisabledCommand(string text)
        {
            return new MenuCommand
            {
                command = "",
                text = text,
                disabled = true
            };
        }
    }
}
