using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class EffectEngine : MonoBehaviour
    {
        public object CreateDelayedEffect(object trigger, object effect)
        {
            return null;
        }
        
        public void AddEffect(object target, object effect)
        {
            // Stub implementation
        }
        
        public void RemoveEffect(object target, object effect)
        {
            // Stub implementation
        }
        
        public bool CheckEffects(object context = null)
        {
            // Stub implementation
            return false;
        }
        
        public GameEffect Add(GameEffect effect)
        {
            // Stub implementation
            return effect;
        }
        
        public void UnapplyAndRemove(object effect)
        {
            // Stub implementation
        }
        
        public void RemoveLastingEffects(object target)
        {
            // Stub implementation
        }
        
        public void Initialize(Game game = null)
        {
            // Stub implementation
        }
        
        public static object TakeControl(object card, object controller)
        {
            // Stub implementation
            return null;
        }
        
        public void CheckDelayedEffects(object context = null)
        {
            // Stub implementation
        }
    }

    public class ConflictFinishedTrigger
    {
        public ConflictFinishedTrigger() { }
    }
    
    [System.Serializable]
    public class GameEffect
    {
        public string name;
        public object target;
        public object source;
        
        public GameEffect() { }
        
        public GameEffect(string name, object target = null, object source = null)
        {
            this.name = name;
            this.target = target;
            this.source = source;
        }
    }
}
