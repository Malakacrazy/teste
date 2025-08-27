using System;
using System.Collections.Generic;

namespace L5RGame
{
    namespace Extensions
    {
        /// <summary>
        /// Extension methods for Lists
        /// </summary>
        public static class ListExtensions
        {
            public static void AddRange<T>(this List<T> list, IEnumerable<T> items)
            {
                if (list != null && items != null)
                {
                    foreach (var item in items)
                    {
                        list.Add(item);
                    }
                }
            }

            public static T Find<T>(this List<T> list, Func<T, bool> predicate)
            {
                if (list == null || predicate == null)
                    return default(T);
                    
                foreach (var item in list)
                {
                    if (predicate(item))
                        return item;
                }
                return default(T);
            }
        }

        /// <summary>
        /// Extension methods for BaseCard
        /// </summary>
        public static class BaseCardExtensions
        {
            /// <summary>
            /// Check if a card has a specific property
            /// </summary>
            public static bool HasProperty(this BaseCard card, string propertyName)
            {
                return card != null && card.GetType().GetProperty(propertyName) != null;
            }

            /// <summary>
            /// Get a property value from a card
            /// </summary>
            public static T GetProperty<T>(this BaseCard card, string propertyName)
            {
                if (card == null) return default(T);
                
                var property = card.GetType().GetProperty(propertyName);
                if (property != null && property.PropertyType == typeof(T))
                {
                    return (T)property.GetValue(card);
                }
                return default(T);
            }
        }

        /// <summary>
        /// Extension methods for Player
        /// </summary>
        public static class PlayerExtensions
        {
            /// <summary>
            /// Check if a player has a specific property
            /// </summary>
            public static bool HasProperty(this Player player, string propertyName)
            {
                return player != null && player.GetType().GetProperty(propertyName) != null;
            }

            /// <summary>
            /// Get a property value from a player
            /// </summary>
            public static T GetProperty<T>(this Player player, string propertyName)
            {
                if (player == null) return default(T);
                
                var property = player.GetType().GetProperty(propertyName);
                if (property != null && property.PropertyType == typeof(T))
                {
                    return (T)property.GetValue(player);
                }
                return default(T);
            }
            
            /// <summary>
            /// Set a property value on a player
            /// </summary>
            public static void SetProperty<T>(this Player player, string propertyName, T value)
            {
                if (player == null) return;
                
                var property = player.GetType().GetProperty(propertyName);
                if (property != null && property.PropertyType == typeof(T) && property.CanWrite)
                {
                    property.SetValue(player, value);
                }
            }
            
            /// <summary>
            /// Remove a property from a player (sets to default value)
            /// </summary>
            public static void RemoveProperty<T>(this Player player, string propertyName)
            {
                player.SetProperty<T>(propertyName, default(T));
            }
        }
    }
}
