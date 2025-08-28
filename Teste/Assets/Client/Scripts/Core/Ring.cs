using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Ring class for conflict resolution
    /// </summary>
    [System.Serializable]
    public partial class Ring : EffectSource
    {
        [Header("Ring Properties")]
        public string element;
        public string conflictType;
        public bool claimed = false;
        public Player claimedBy;
        public List<BaseCard> attachments = new List<BaseCard>();
        public int fate = 0;
        
        public Ring() { }
        
        public Ring(Game game, string ringElement, string initialConflictType)
        {
            Initialize(game, $"{ringElement} Ring");
            element = ringElement;
            conflictType = initialConflictType;
        }
        
        public void FlipConflictType()
        {
            conflictType = conflictType == ConflictTypes.Military ? ConflictTypes.Political : ConflictTypes.Military;
            game.AddMessage("{0} ring flipped to {1}", element, conflictType);
        }
        
        public void ClaimRing(Player player)
        {
            claimed = true;
            claimedBy = player;
            game.AddMessage("{0} claims the {1} ring", player, element);
        }
        
        public void ResetRing()
        {
            claimed = false;
            claimedBy = null;
        }
        
        public bool IsContested()
        {
            return false; // Placeholder
        }
        
        public List<string> GetElements()
        {
            // For basic rings, just return the single element
            // Override in derived classes for rings with multiple elements
            return new List<string> { element };
        }
        
        public void SetContested()
        {
            // Mark ring as contested
            // Implementation would depend on ring state system
        }
        
        public void RemoveFate()
        {
            fate = 0;
        }
        
        public List<BaseCard> GetAttachments()
        {
            return attachments != null ? new List<BaseCard>(attachments) : new List<BaseCard>();
        }
        
        public void AddAttachment(BaseCard attachment)
        {
            if (attachments == null)
                attachments = new List<BaseCard>();
            
            if (!attachments.Contains(attachment))
            {
                attachments.Add(attachment);
            }
        }
        
        public void RemoveAttachment(BaseCard attachment)
        {
            attachments?.Remove(attachment);
        }
        
        public object GetState(object activePlayer)
        {
            return new
            {
                element,
                conflictType,
                claimed,
                claimedBy = claimedBy?.name,
                attachmentCount = attachments?.Count ?? 0
            };
        }
    }
}
