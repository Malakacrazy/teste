using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Results of target resolution for abilities
    /// </summary>
    public class AbilityTargetResults
    {
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        public bool success = false;
        public string errorMessage = "";
        
        public T GetTarget<T>(string key) where T : class
        {
            if (targets.TryGetValue(key, out object target))
            {
                return target as T;
            }
            return null;
        }
        
        public void SetTarget(string key, object target)
        {
            targets[key] = target;
        }
        
        public bool HasTarget(string key)
        {
            return targets.ContainsKey(key);
        }
    }

    /// <summary>
    /// Results of cost resolution for abilities
    /// </summary>
    public class AbilityCostResults
    {
        public Dictionary<string, object> paidCosts = new Dictionary<string, object>();
        public bool success = false;
        public string errorMessage = "";
        
        public T GetCost<T>(string key) where T : class
        {
            if (paidCosts.TryGetValue(key, out object cost))
            {
                return cost as T;
            }
            return null;
        }
        
        public void SetCost(string key, object cost)
        {
            paidCosts[key] = cost;
        }
        
        public bool HasCost(string key)
        {
            return paidCosts.ContainsKey(key);
        }
    }

    /// <summary>
    /// Helper class for various game utilities
    /// </summary>
    public static class GameHelper
    {
        public static Player GetLocalPlayer(Game game)
        {
            // Simplified - return first player for now
            var players = game.GetPlayers();
            return players.Count > 0 ? players[0] : null;
        }
    }
}
