using UnityEngine;

namespace L5RGame
{
    public class ConflictOpportunities
    {
        public int military = 1;
        public int political = 1;
        public int total = 2;
        
        public void Reset()
        {
            military = 0;
            political = 0;
            total = 0;
        }
    }
}