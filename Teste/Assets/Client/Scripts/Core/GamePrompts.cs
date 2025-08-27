using UnityEngine;
using System;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Base class for all game prompts
    /// </summary>
    public abstract class BasePrompt : IGameStep
    {
        protected Game game;
        protected Player player;
        
        public BasePrompt(Game gameInstance, Player promptPlayer)
        {
            game = gameInstance;
            player = promptPlayer;
        }
        
        public abstract bool Continue();
        public abstract void OnMenuCommand(Player player, string command, string arg, string uuid, string method);
        public abstract void OnCardClicked(Player player, BaseCard card);
        public abstract void OnRingClicked(Player player, Ring ring);
        public virtual void Initialize() { }
        public virtual void Cleanup() { }
        
        public virtual bool Execute()
        {
            return Continue();
        }
        
        public virtual bool IsComplete()
        {
            return true; // Most prompts complete after one interaction
        }
    }
    

    

    

    

    

    

    

    

    

}
