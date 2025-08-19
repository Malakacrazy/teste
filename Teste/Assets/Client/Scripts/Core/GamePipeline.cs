using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class GamePipeline : MonoBehaviour
    {
        public void Initialize() { }
        public void Initialize(List<IGameStep> steps) { }
        public void HandleCardClicked(Player player, BaseCard card) { }
        public bool HandleRingClicked(Player player, Ring ring) => false;
        public bool HandleMenuCommand(Player player, string arg, string uuid, string method) => false;
        public T QueueStep<T>(T step) where T : IGameStep => step;
        public void Continue() { }
    }
}