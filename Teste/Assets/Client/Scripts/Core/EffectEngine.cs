using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class EffectEngine : MonoBehaviour
    {
        public void Initialize(Game game) { }
        public bool CheckEffects(bool hasChanged) => false;
        public void CheckDelayedEffects(List<GameEvent> events) { }
    }
}