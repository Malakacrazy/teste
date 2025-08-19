using UnityEngine;

namespace L5RGame
{
    public class Ring : MonoBehaviour
    {
        public string element;
        public bool claimed;
        
        public Ring(Game game, string ringElement, ConflictType conflictType)
        {
            element = ringElement;
        }
        
        public void FlipConflictType() { }
        public object GetState(object activePlayer) => null;
    }
}