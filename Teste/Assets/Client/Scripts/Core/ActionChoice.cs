using System;

namespace L5RGame
{
    /// <summary>
    /// Represents a choice that can be made in game actions
    /// </summary>
    [Serializable]
    public class ActionChoice
    {
        /// <summary>
        /// Display text for the choice
        /// </summary>
        public string Text { get; set; }
        
        /// <summary>
        /// Unique identifier for this choice
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Whether this choice is enabled/selectable
        /// </summary>
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Additional data associated with this choice
        /// </summary>
        public object Data { get; set; }
        
        /// <summary>
        /// Initialize an action choice
        /// </summary>
        /// <param name="text">Display text</param>
        /// <param name="id">Unique identifier</param>
        /// <param name="enabled">Whether choice is enabled</param>
        /// <param name="data">Additional data</param>
        public ActionChoice(string text, string id = null, bool enabled = true, object data = null)
        {
            Text = text;
            Id = id ?? Guid.NewGuid().ToString();
            Enabled = enabled;
            Data = data;
        }
        
        /// <summary>
        /// Convert to string representation
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            var enabledText = Enabled ? "" : " (disabled)";
            return $"{Text}{enabledText}";
        }
    }
}