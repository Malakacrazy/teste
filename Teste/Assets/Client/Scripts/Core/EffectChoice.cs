using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a choice for an effect that can be resolved
    /// Used in simultaneous effect windows and ring effect resolution
    /// </summary>
    [System.Serializable]
    public class EffectChoice
    {
        [Header("Choice Properties")]
        [SerializeField] private string title;
        [SerializeField] private string description;
        [SerializeField] private bool enabled = true;
        [SerializeField] private int priority = 0;

        // Action to execute when this choice is selected
        private Action handler;
        
        // Condition that determines if this choice is available
        private Func<bool> condition;
        
        // Additional data that can be associated with this choice
        private object data;
        
        // Source of this effect choice (card, ability, etc.)
        private object source;

        #region Properties

        /// <summary>
        /// Display title for this choice
        /// </summary>
        public string Title
        {
            get => title;
            set => title = value;
        }

        /// <summary>
        /// Optional description for this choice
        /// </summary>
        public string Description
        {
            get => description;
            set => description = value;
        }

        /// <summary>
        /// Whether this choice is currently enabled/selectable
        /// </summary>
        public bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        /// <summary>
        /// Priority for ordering multiple choices (higher = earlier)
        /// </summary>
        public int Priority
        {
            get => priority;
            set => priority = value;
        }

        /// <summary>
        /// Action to execute when this choice is selected
        /// </summary>
        public Action Handler
        {
            get => handler;
            set => handler = value;
        }

        /// <summary>
        /// Condition that determines if this choice is available
        /// </summary>
        public Func<bool> Condition
        {
            get => condition;
            set => condition = value;
        }

        /// <summary>
        /// Additional data associated with this choice
        /// </summary>
        public object Data
        {
            get => data;
            set => data = value;
        }

        /// <summary>
        /// Source of this effect choice (card, ability, etc.)
        /// </summary>
        public object Source
        {
            get => source;
            set => source = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public EffectChoice()
        {
            title = "Effect Choice";
            description = "";
            enabled = true;
            priority = 0;
            condition = () => true;
        }

        /// <summary>
        /// Constructor with title and handler
        /// </summary>
        public EffectChoice(string title, Action handler)
        {
            this.title = title;
            this.handler = handler;
            description = "";
            enabled = true;
            priority = 0;
            condition = () => true;
        }

        /// <summary>
        /// Constructor with title, handler, and condition
        /// </summary>
        public EffectChoice(string title, Action handler, Func<bool> condition)
        {
            this.title = title;
            this.handler = handler;
            this.condition = condition ?? (() => true);
            description = "";
            enabled = true;
            priority = 0;
        }

        /// <summary>
        /// Full constructor with all parameters
        /// </summary>
        public EffectChoice(string title, Action handler, Func<bool> condition, string description, int priority = 0, object source = null)
        {
            this.title = title;
            this.handler = handler;
            this.condition = condition ?? (() => true);
            this.description = description ?? "";
            this.priority = priority;
            this.source = source;
            enabled = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Check if this choice is currently available
        /// </summary>
        public bool IsAvailable()
        {
            return enabled && (condition?.Invoke() ?? true);
        }

        /// <summary>
        /// Execute this choice's handler
        /// </summary>
        public void Execute()
        {
            if (!IsAvailable())
            {
                Debug.LogWarning($"Attempted to execute unavailable effect choice: {title}");
                return;
            }

            try
            {
                handler?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing effect choice '{title}': {ex.Message}");
            }
        }

        /// <summary>
        /// Create a copy of this effect choice
        /// </summary>
        public EffectChoice Clone()
        {
            return new EffectChoice(title, handler, condition, description, priority, source)
            {
                enabled = this.enabled,
                data = this.data
            };
        }

        /// <summary>
        /// Get display text for this choice including description if available
        /// </summary>
        public string GetDisplayText()
        {
            if (string.IsNullOrEmpty(description))
                return title;
            
            return $"{title}: {description}";
        }

        /// <summary>
        /// Set the source object for this choice
        /// </summary>
        public EffectChoice SetSource(object source)
        {
            this.source = source;
            return this;
        }

        /// <summary>
        /// Set additional data for this choice
        /// </summary>
        public EffectChoice SetData(object data)
        {
            this.data = data;
            return this;
        }

        /// <summary>
        /// Set the priority for this choice
        /// </summary>
        public EffectChoice SetPriority(int priority)
        {
            this.priority = priority;
            return this;
        }

        /// <summary>
        /// Set the enabled state for this choice
        /// </summary>
        public EffectChoice SetEnabled(bool enabled)
        {
            this.enabled = enabled;
            return this;
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Create a simple effect choice with just title and handler
        /// </summary>
        public static EffectChoice Create(string title, Action handler)
        {
            return new EffectChoice(title, handler);
        }

        /// <summary>
        /// Create an effect choice with condition
        /// </summary>
        public static EffectChoice CreateConditional(string title, Action handler, Func<bool> condition)
        {
            return new EffectChoice(title, handler, condition);
        }

        /// <summary>
        /// Create an effect choice for ring effects
        /// </summary>
        public static EffectChoice CreateRingEffect(string element, Action handler, int priority = 0)
        {
            return new EffectChoice($"{element} Ring Effect", handler, () => true, $"Resolve the {element} ring effect", priority);
        }

        /// <summary>
        /// Create an effect choice for ability resolution
        /// </summary>
        public static EffectChoice CreateAbilityEffect(string abilityName, Action handler, object source = null)
        {
            return new EffectChoice(abilityName, handler, () => true, $"Resolve {abilityName}", 0, source);
        }

        /// <summary>
        /// Create an effect choice for card effects
        /// </summary>
        public static EffectChoice CreateCardEffect(BaseCard card, Action handler, string effectDescription = null)
        {
            var title = card?.name ?? "Card Effect";
            var description = effectDescription ?? $"Resolve {title}'s effect";
            return new EffectChoice(title, handler, () => true, description, 0, card);
        }

        /// <summary>
        /// Create a disabled effect choice (for display purposes)
        /// </summary>
        public static EffectChoice CreateDisabled(string title, string reason = "Not available")
        {
            return new EffectChoice(title, null, () => false, reason)
            {
                enabled = false
            };
        }

        #endregion

        #region Operator Overloads

        public override string ToString()
        {
            var status = IsAvailable() ? "Available" : "Unavailable";
            return $"EffectChoice: {title} ({status})";
        }

        public override bool Equals(object obj)
        {
            if (obj is EffectChoice other)
            {
                return title == other.title && 
                       priority == other.priority && 
                       ReferenceEquals(source, other.source);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(title, priority, source);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate that this effect choice has required properties
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(title) && handler != null;
        }

        /// <summary>
        /// Get validation errors for this effect choice
        /// </summary>
        public string[] GetValidationErrors()
        {
            var errors = new System.Collections.Generic.List<string>();
            
            if (string.IsNullOrEmpty(title))
                errors.Add("Title is required");
                
            if (handler == null)
                errors.Add("Handler is required");
                
            return errors.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for working with collections of EffectChoices
    /// </summary>
    public static class EffectChoiceExtensions
    {
        /// <summary>
        /// Filter choices to only available ones
        /// </summary>
        public static System.Collections.Generic.IEnumerable<EffectChoice> Available(this System.Collections.Generic.IEnumerable<EffectChoice> choices)
        {
            return choices.Where(choice => choice.IsAvailable());
        }

        /// <summary>
        /// Sort choices by priority (highest first)
        /// </summary>
        public static System.Collections.Generic.IEnumerable<EffectChoice> ByPriority(this System.Collections.Generic.IEnumerable<EffectChoice> choices)
        {
            return choices.OrderByDescending(choice => choice.Priority);
        }

        /// <summary>
        /// Get choices from a specific source
        /// </summary>
        public static System.Collections.Generic.IEnumerable<EffectChoice> FromSource(this System.Collections.Generic.IEnumerable<EffectChoice> choices, object source)
        {
            return choices.Where(choice => ReferenceEquals(choice.Source, source));
        }

        /// <summary>
        /// Execute all available choices in priority order
        /// </summary>
        public static void ExecuteAll(this System.Collections.Generic.IEnumerable<EffectChoice> choices)
        {
            foreach (var choice in choices.Available().ByPriority())
            {
                choice.Execute();
            }
        }
    }
}
