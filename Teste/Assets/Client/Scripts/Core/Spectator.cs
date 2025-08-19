using UnityEngine;

namespace L5RGame
{
    public class Spectator : Player
    {
        public void Initialize(string socketId, UserInfo user) 
        {
            id = socketId;
            name = user.username;
            emailHash = user.emailHash;
            lobbyId = user.lobbyId;
        }
    }
}