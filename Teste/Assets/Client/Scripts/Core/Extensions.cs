using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Globalization;

namespace L5RGame
{
    namespace Extensions
    {
        /// <summary>
        /// Collection extension methods for easier manipulation of game collections
        /// </summary>
        public static class CollectionExtensions
    {
        /// <summary>
        /// Get a random element from a collection
        /// </summary>
        public static T GetRandomElement<T>(this ICollection<T> collection)
        {
            if (collection == null || collection.Count == 0)
                return default(T);

            var list = collection.ToList();
            return list[UnityEngine.Random.Range(0, list.Count)];
        }

        /// <summary>
        /// Get multiple random elements from a collection
        /// </summary>
        public static List<T> GetRandomElements<T>(this ICollection<T> collection, int count)
        {
            if (collection == null || collection.Count == 0)
                return new List<T>();

            var list = collection.ToList();
            var result = new List<T>();

            for (int i = 0; i < Mathf.Min(count, list.Count); i++)
            {
                var randomIndex = UnityEngine.Random.Range(0, list.Count);
                result.Add(list[randomIndex]);
                list.RemoveAt(randomIndex);
            }

            return result;
        }

        /// <summary>
        /// Shuffle a list in place
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        /// <summary>
        /// Get a shuffled copy of a collection
        /// </summary>
        public static List<T> Shuffled<T>(this ICollection<T> collection)
        {
            var list = collection.ToList();
            list.Shuffle();
            return list;
        }

        /// <summary>
        /// Check if collection is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
        {
            return collection == null || collection.Count == 0;
        }

        /// <summary>
        /// Check if collection has any elements
        /// </summary>
        public static bool HasElements<T>(this ICollection<T> collection)
        {
            return collection != null && collection.Count > 0;
        }

        /// <summary>
        /// Add range with null checking
        /// </summary>
        public static void AddRangeSafe<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            if (collection == null || items == null) return;

            foreach (var item in items)
            {
                if (item != null)
                    collection.Add(item);
            }
        }

        /// <summary>
        /// Remove all items that match a condition
        /// </summary>
        public static int RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate)
        {
            int removedCount = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (predicate(list[i]))
                {
                    list.RemoveAt(i);
                    removedCount++;
                }
            }
            return removedCount;
        }

        /// <summary>
        /// Get items at specific indices
        /// </summary>
        public static List<T> GetItemsAtIndices<T>(this IList<T> list, params int[] indices)
        {
            var result = new List<T>();
            foreach (int index in indices)
            {
                if (index >= 0 && index < list.Count)
                    result.Add(list[index]);
            }
            return result;
        }

        /// <summary>
        /// Move item from one index to another
        /// </summary>
        public static bool MoveItem<T>(this IList<T> list, int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= list.Count || toIndex < 0 || toIndex >= list.Count)
                return false;

            T item = list[fromIndex];
            list.RemoveAt(fromIndex);
            list.Insert(toIndex, item);
            return true;
        }
    }
        
        /// <summary>
        /// String extension methods for game text processing
        /// </summary>
        public static class StringExtensions
        {
            /// <summary>
            /// Capitalize first letter of each word
            /// </summary>
            public static string ToTitleCase(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;
                    
                return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());
            }
            
            /// <summary>
            /// Capitalize only the first letter
            /// </summary>
            public static string Capitalize(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;
                    
                return char.ToUpper(input[0]) + input.Substring(1).ToLower();
            }
            
            /// <summary>
            /// Convert to camelCase
            /// </summary>
            public static string ToCamelCase(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;
                    
                var words = input.Split(' ', '_', '-');
                var result = new StringBuilder();
                
                for (int i = 0; i < words.Length; i++)
                {
                    if (i == 0)
                        result.Append(words[i].ToLower());
                    else
                        result.Append(words[i].Capitalize());
                }
                
                return result.ToString();
            }
            
            /// <summary>
            /// Convert to snake_case
            /// </summary>
            public static string ToSnakeCase(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;
                    
                return input.Replace(' ', '_').Replace('-', '_').ToLower();
            }
            
            /// <summary>
            /// Truncate string to max length with ellipsis
            /// </summary>
            public static string Truncate(this string input, int maxLength, string ellipsis = "...")
            {
                if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
                    return input;
                    
                return input.Substring(0, maxLength - ellipsis.Length) + ellipsis;
            }
            
            /// <summary>
            /// Check if string contains any of the specified values
            /// </summary>
            public static bool ContainsAny(this string input, params string[] values)
            {
                if (string.IsNullOrEmpty(input) || values == null)
                    return false;
                    
                return values.Any(value => input.Contains(value));
            }
            
            /// <summary>
            /// Check if string contains all of the specified values
            /// </summary>
            public static bool ContainsAll(this string input, params string[] values)
            {
                if (string.IsNullOrEmpty(input) || values == null)
                    return false;
                    
                return values.All(value => input.Contains(value));
            }
            
            /// <summary>
            /// Replace multiple strings at once
            /// </summary>
            public static string ReplaceMultiple(this string input, Dictionary<string, string> replacements)
            {
                if (string.IsNullOrEmpty(input) || replacements == null)
                    return input;
                    
                string result = input;
                foreach (var replacement in replacements)
                {
                    result = result.Replace(replacement.Key, replacement.Value);
                }
                return result;
            }
            
            /// <summary>
            /// Format string with placeholder replacement (Unity-friendly alternative to string interpolation)
            /// </summary>
            public static string FormatWith(this string template, params object[] args)
            {
                if (string.IsNullOrEmpty(template))
                    return string.Empty;
                    
                try
                {
                    return string.Format(template, args);
                }
                catch (FormatException)
                {
                    Debug.LogWarning($"Failed to format string: {template}");
                    return template;
                }
            }
            
            /// <summary>
            /// Clean string for use as identifier (remove special characters)
            /// </summary>
            public static string ToSafeIdentifier(this string input)
            {
                if (string.IsNullOrEmpty(input))
                    return string.Empty;
                    
                var result = new StringBuilder();
                foreach (char c in input)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                        result.Append(c);
                    else if (char.IsWhiteSpace(c))
                        result.Append('_');
                }
                
                return result.ToString();
            }
        }
        
        /// <summary>
        /// GameObject and Component extension methods
        /// </summary>
        public static class GameObjectExtensions
        {
            /// <summary>
            /// Get or add component
            /// </summary>
            public static T GetOrAddComponent<T>(this UnityEngine.GameObject gameObject) where T : Component
            {
                var component = gameObject.GetComponent<T>();
                if (component == null)
                    component = gameObject.AddComponent<T>();
                return component;
            }
            
            /// <summary>
            /// Safe destroy that works in edit and play mode
            /// </summary>
            public static void SafeDestroy(this UnityEngine.GameObject gameObject)
            {
                if (gameObject == null) return;
                
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(gameObject);
            }
            
            /// <summary>
            /// Safe destroy component
            /// </summary>
            public static void SafeDestroy<T>(this T component) where T : Component
            {
                if (component == null) return;
                
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(component);
                else
                    UnityEngine.Object.DestroyImmediate(component);
            }
            
            /// <summary>
            /// Set active with null checking
            /// </summary>
            public static void SetActiveSafe(this UnityEngine.GameObject gameObject, bool active)
            {
                if (gameObject != null)
                    gameObject.SetActive(active);
            }
            
            /// <summary>
            /// Get components in children including inactive
            /// </summary>
            public static T[] GetComponentsInChildrenIncludingInactive<T>(this UnityEngine.GameObject gameObject) where T : Component
            {
                return gameObject.GetComponentsInChildren<T>(true);
            }
            
            /// <summary>
            /// Find child by name recursively
            /// </summary>
            public static Transform FindChildRecursive(this Transform parent, string childName)
            {
                foreach (Transform child in parent)
                {
                    if (child.name == childName)
                        return child;
                        
                    var result = child.FindChildRecursive(childName);
                    if (result != null)
                        return result;
                }
                return null;
            }
            
            /// <summary>
            /// Get path from root to this transform
            /// </summary>
            public static string GetPath(this Transform transform)
            {
                var path = transform.name;
                var parent = transform.parent;
                
                while (parent != null)
                {
                    path = parent.name + "/" + path;
                    parent = parent.parent;
                }
                
                return path;
            }
        }
        
        /// <summary>
        /// L5R Game-specific extension methods
        /// </summary>
        public static class L5RGameExtensions
        {
            /// <summary>
            /// Get all cards of a specific type
            /// </summary>
            public static List<T> GetCardsOfType<T>(this Player player) where T : BaseCard
            {
                var result = new List<T>();
                
                // Check all card locations
                result.AddRange(player.hand.OfType<T>());
                result.AddRange(player.cardsInPlay.OfType<T>());
                result.AddRange(player.dynastyDeck.OfType<T>());
                result.AddRange(player.conflictDeck.OfType<T>());
                result.AddRange(player.dynastyDiscardPile.OfType<T>());
                result.AddRange(player.conflictDiscardPile.OfType<T>());
                
                return result;
            }
            
            /// <summary>
            /// Get all characters in play
            /// </summary>
            public static List<BaseCard> GetCharactersInPlay(this Player player)
            {
                return player.cardsInPlay.Where(card => card.IsCharacter()).ToList();
            }
            
            /// <summary>
            /// Get all attachments in play
            /// </summary>
            public static List<BaseCard> GetAttachmentsInPlay(this Player player)
            {
                return player.cardsInPlay.Where(card => card.IsAttachment()).ToList();
            }
            
            /// <summary>
            /// Get all holdings in provinces
            /// </summary>
            public static List<BaseCard> GetHoldingsInProvinces(this Player player)
            {
                var holdings = new List<BaseCard>();
                holdings.AddRange(player.provinceOne.Where(card => card.IsHolding()));
                holdings.AddRange(player.provinceTwo.Where(card => card.IsHolding()));
                holdings.AddRange(player.provinceThree.Where(card => card.IsHolding()));
                holdings.AddRange(player.provinceFour.Where(card => card.IsHolding()));
                return holdings;
            }
            
            /// <summary>
            /// Check if player has composure (honor >= 12)
            /// </summary>
            public static bool HasComposure(this Player player)
            {
                return player.honor >= 12;
            }
            
            /// <summary>
            /// Check if player is striving (honor <= 6)
            /// </summary>
            public static bool IsStriving(this Player player)
            {
                return player.honor <= 6;
            }
            
            /// <summary>
            /// Get total skill for conflict type
            /// </summary>
            public static int GetTotalSkill(this List<BaseCard> characters, string conflictType)
            {
                return characters.Sum(card => card.GetSkill(conflictType));
            }
            
            /// <summary>
            /// Check if card is participating in current conflict
            /// </summary>
            public static bool IsParticipatingInConflict(this BaseCard card)
            {
                if (card.game.currentConflict == null)
                    return false;
                    
                return card.game.currentConflict.IsParticipating(card);
            }
            
            /// <summary>
            /// Check if card is attacking in current conflict
            /// </summary>
            public static bool IsAttacking(this BaseCard card)
            {
                if (card.game.currentConflict == null)
                    return false;
                    
                return card.game.currentConflict.IsAttacker(card.controller);
            }
            
            /// <summary>
            /// Check if card is defending in current conflict
            /// </summary>
            public static bool IsDefending(this BaseCard card)
            {
                if (card.game.currentConflict == null)
                    return false;
                    
                return card.game.currentConflict.IsDefender(card.controller);
            }
            
            /// <summary>
            /// Get all cards that can be targeted by an ability
            /// </summary>
            public static List<BaseCard> GetLegalTargets(this Game game, BaseCard source, string abilityType)
            {
                // Implementation would depend on targeting rules
                var allCards = new List<BaseCard>();
                
                foreach (var player in game.GetPlayers())
                {
                    allCards.AddRange(player.GetAllCards());
                }
                
                var context = new AbilityContext(new AbilityContextProperties { 
                    source = source, 
                    game = game 
                });
                return allCards.Where(card => card.CanBeTargetedBy(context)).ToList();
            }
            
            /// <summary>
            /// Get all rings that can be declared by a player
            /// </summary>
            public static List<Ring> GetDeclarableRings(this Game game, Player player)
            {
                return game.rings.Values.Where(ring => ring.CanDeclare(player)).ToList();
            }
            
            /// <summary>
            /// Check if game is in a specific phase
            /// </summary>
            public static bool IsInPhase(this Game game, string phaseName)
            {
                return game.currentPhase != null && game.currentPhase == phaseName;
            }
            
            /// <summary>
            /// Check if it's a player's turn to act
            /// </summary>
            public static bool IsPlayerTurn(this Game game, Player player)
            {
                return game.activePlayer == player;
            }
        }
        
        /// <summary>
        /// Math and utility extensions
        /// </summary>
        public static class MathExtensions
        {
            /// <summary>
            /// Clamp integer between min and max
            /// </summary>
            public static int Clamp(this int value, int min, int max)
            {
                return Mathf.Clamp(value, min, max);
            }
            
            /// <summary>
            /// Clamp float between min and max
            /// </summary>
            public static float Clamp(this float value, float min, float max)
            {
                return Mathf.Clamp(value, min, max);
            }
            
            /// <summary>
            /// Check if number is between two values (inclusive)
            /// </summary>
            public static bool IsBetween(this int value, int min, int max)
            {
                return value >= min && value <= max;
            }
            
            /// <summary>
            /// Check if number is between two values (inclusive)
            /// </summary>
            public static bool IsBetween(this float value, float min, float max)
            {
                return value >= min && value <= max;
            }
            
            /// <summary>
            /// Round to nearest multiple
            /// </summary>
            public static int RoundToNearest(this int value, int multiple)
            {
                return Mathf.RoundToInt((float)value / multiple) * multiple;
            }
            
            /// <summary>
            /// Round to nearest multiple
            /// </summary>
            public static float RoundToNearest(this float value, float multiple)
            {
                return Mathf.Round(value / multiple) * multiple;
            }
            
            /// <summary>
            /// Convert percentage to 0-1 range
            /// </summary>
            public static float ToDecimal(this int percentage)
            {
                return percentage / 100f;
            }
            
            /// <summary>
            /// Convert 0-1 range to percentage
            /// </summary>
            public static int ToPercentage(this float decimal_value)
            {
                return Mathf.RoundToInt(decimal_value * 100f);
            }
        }
        
        /// <summary>
        /// Unity Color extensions
        /// </summary>
        public static class ColorExtensions
        {
            /// <summary>
            /// Set alpha value
            /// </summary>
            public static Color WithAlpha(this Color color, float alpha)
            {
                return new Color(color.r, color.g, color.b, alpha);
            }
            
            /// <summary>
            /// Brighten color
            /// </summary>
            public static Color Brighten(this Color color, float amount)
            {
                return new Color(
                    Mathf.Clamp01(color.r + amount),
                    Mathf.Clamp01(color.g + amount),
                    Mathf.Clamp01(color.b + amount),
                    color.a
                );
            }
            
            /// <summary>
            /// Darken color
            /// </summary>
            public static Color Darken(this Color color, float amount)
            {
                return new Color(
                    Mathf.Clamp01(color.r - amount),
                    Mathf.Clamp01(color.g - amount),
                    Mathf.Clamp01(color.b - amount),
                    color.a
                );
            }
            
            /// <summary>
            /// Convert to hex string
            /// </summary>
            public static string ToHex(this Color color)
            {
                return ColorUtility.ToHtmlStringRGBA(color);
            }
            
            /// <summary>
            /// Parse hex string to color
            /// </summary>
            public static Color FromHex(string hex)
            {
                ColorUtility.TryParseHtmlString(hex, out Color color);
                return color;
            }
        }
        
        /// <summary>
        /// Vector extensions
        /// </summary>
        public static class VectorExtensions
        {
            /// <summary>
            /// Set X component
            /// </summary>
            public static Vector3 WithX(this Vector3 vector, float x)
            {
                return new Vector3(x, vector.y, vector.z);
            }
            
            /// <summary>
            /// Set Y component
            /// </summary>
            public static Vector3 WithY(this Vector3 vector, float y)
            {
                return new Vector3(vector.x, y, vector.z);
            }
            
            /// <summary>
            /// Set Z component
            /// </summary>
            public static Vector3 WithZ(this Vector3 vector, float z)
            {
                return new Vector3(vector.x, vector.y, z);
            }
            
            /// <summary>
            /// Convert Vector3 to Vector2 (drop Z)
            /// </summary>
            public static Vector2 ToVector2(this Vector3 vector)
            {
                return new Vector2(vector.x, vector.y);
            }
            
            /// <summary>
            /// Convert Vector2 to Vector3 (add Z)
            /// </summary>
            public static Vector3 ToVector3(this Vector2 vector, float z = 0f)
            {
                return new Vector3(vector.x, vector.y, z);
            }
            
            /// <summary>
            /// Flatten vector to XZ plane
            /// </summary>
            public static Vector3 FlattenToXZ(this Vector3 vector)
            {
                return new Vector3(vector.x, 0f, vector.z);
            }
            
            /// <summary>
            /// Get random point within radius
            /// </summary>
            public static Vector3 RandomPointInRadius(this Vector3 center, float radius)
            {
                var randomDirection = UnityEngine.Random.insideUnitSphere * radius;
                return center + randomDirection;
            }
        }
    }
}