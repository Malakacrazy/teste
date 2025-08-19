using UnityEngine;

namespace L5RGame
{
    public class SimpleStep : MonoBehaviour, IGameStep
    {
        public SimpleStep(Game game, System.Func<bool> handler) { }
    }
}