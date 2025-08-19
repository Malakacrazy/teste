using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    public class Conflict : MonoBehaviour
    {
        public List<string> elements = new List<string>();
        public string conflictType;
        public Player attackingPlayer;
        public string declaredType;
        public bool conflictPassed;
        public string uuid;
        public bool forcedDeclaredType;
        public Player winner;
        public bool conflictTypeSwitched;
        
        public Conflict(Game game, Player attacker, Player defender, object param1, object param2, string forcedType)
        {
            attackingPlayer = attacker;
        }
        
        public bool CalculateSkill(bool hasChanged) => false;
        public void RemoveFromConflict(BaseCard card) { }
        public void AddAttacker(BaseCard card) { }
        public void AddDefender(BaseCard card) { }
        public void CheckForIllegalParticipants() { }
        public object GetSummary() => null;
    }
}