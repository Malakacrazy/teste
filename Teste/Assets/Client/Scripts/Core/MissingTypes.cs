using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    // Missing types required for compilation
    
    /// <summary>
    /// Configuration for ability targeting
    /// </summary>
    public class TargetConfiguration
    {
        public List<BaseCard> targets = new List<BaseCard>();
        public string targetType = "";
        public int maxTargets = 1;
        public bool optional = false;
        
        // Additional properties used in the codebase
        public string Mode = "";
        public string ActivePromptTitle = "";
        public string Source = "";
        public Dictionary<string, Func<AbilityContext, bool>> Choices = new Dictionary<string, Func<AbilityContext, bool>>();
        public bool AllowCancel = false;
        
        public TargetConfiguration() { }
        
        public TargetConfiguration(string type, int max = 1, bool isOptional = false)
        {
            targetType = type;
            maxTargets = max;
            optional = isOptional;
        }
    }
    

}