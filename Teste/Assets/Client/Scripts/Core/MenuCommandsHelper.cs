using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Helper class for handling menu commands on cards and rings
    /// </summary>
    public static class MenuCommandsHelper
    {
        /// <summary>
        /// Handle menu command clicked on a card
        /// </summary>
        public static void CardMenuClick(MenuCommand menuItem, Game game, Player player, BaseCard card)
        {
            if (menuItem == null || game == null || player == null || card == null)
                return;

            Debug.Log($"Card menu click: {menuItem.command} on {card.name} by {player.name}");
            // Basic implementation - can be expanded
        }

        /// <summary>
        /// Handle menu command clicked on a ring
        /// </summary>
        public static void RingMenuClick(MenuCommand menuItem, Game game, Player player, Ring ring)
        {
            if (menuItem == null || game == null || player == null || ring == null)
                return;

            Debug.Log($"Ring menu click: {menuItem.command} on {ring.element} by {player.name}");
            // Basic implementation - can be expanded
        }
    }
}
