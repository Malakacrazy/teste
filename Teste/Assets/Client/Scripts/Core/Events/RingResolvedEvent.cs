using System;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a ring effect is resolved
    /// </summary>
    [Serializable]
    public class RingResolvedEvent : GameEvent
    {
        /// <summary>
        /// The ring that was resolved
        /// </summary>
        public Ring Ring { get; private set; }
        
        /// <summary>
        /// The effect that was chosen/resolved
        /// </summary>
        public string EffectChosen { get; private set; }
        
        /// <summary>
        /// The target of the effect (if any)
        /// </summary>
        public BaseCard EffectTarget { get; private set; }
        
        /// <summary>
        /// Ring element name
        /// </summary>
        public string RingElement { get; private set; }
        
        /// <summary>
        /// Was the ring effect resolved or cancelled?
        /// </summary>
        public bool WasResolved { get; private set; }
        
        /// <summary>
        /// Initialize ring resolved event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who resolved the ring</param>
        /// <param name="ring">Ring that was resolved</param>
        /// <param name="effectChosen">Effect that was chosen</param>
        /// <param name="effectTarget">Target of the effect</param>
        /// <param name="source">Source ability</param>
        public RingResolvedEvent(Game game, Player triggeredBy, Ring ring, string effectChosen, BaseCard effectTarget = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            Ring = ring;
            EffectChosen = effectChosen;
            EffectTarget = effectTarget;
            RingElement = ring?.element ?? "unknown";
            WasResolved = effectChosen != "not_resolved" && effectChosen != "no_targets";
            
            // Add specific event data
            AddEventData("ring_element", RingElement);
            AddEventData("effect_chosen", effectChosen);
            AddEventData("was_resolved", WasResolved);
            if (effectTarget != null)
            {
                AddEventData("target_id", effectTarget.CardId);
                AddEventData("target_name", effectTarget.Name);
                AddEventData("target_owner", effectTarget.Owner?.PlayerId);
            }
        }
        
        /// <summary>
        /// Get description of this event
        /// </summary>
        public string GetDescription()
        {
            string targetText = EffectTarget != null ? $" targeting {EffectTarget.Name}" : "";
            string resolveText = WasResolved ? "resolved" : "not resolved";
            return $"{RingElement} ring {resolveText} with effect '{EffectChosen}'{targetText}";
        }
    }
}