using UnityEngine;

namespace L5RGame
{
    public class BaseCard : MonoBehaviour
    {
        public string uuid;
        public string name;
        public bool isProvince;
        public bool isStronghold;
        public Player controller;
        
        public void Initialize(BaseCard card) { }
        public bool CheckRestrictions(string restriction, AbilityContext context) => true;
        public bool IsParticipating() => false;
        public void UpdateEffectContexts() { }
        public Player GetModifiedController() => controller;
        public void CheckForIllegalAttachments() { }
    }
}