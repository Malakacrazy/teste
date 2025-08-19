using System;
using System.Collections.Generic;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Represents a spectator in the game - someone who can watch but not participate
    /// </summary>
    public class Spectator : MonoBehaviour
    {
        [Header("Spectator Identity")]
        public UserInfo user;
        public string name;
        public string emailHash;
        public string id;
        public string lobbyId;

        [Header("Spectator UI")]
        public List<UIButton> buttons = new List<UIButton>();
        public string menuTitle = "Spectator mode";

        [Header("Network State")]
        public object socket;
        public bool disconnected = false;
        public bool left = false;

        /// <summary>
        /// Initialize the spectator with user information
        /// </summary>
        /// <param name="spectatorId">Unique identifier for this spectator</param>
        /// <param name="userInfo">User information including username and email hash</param>
        public void Initialize(string spectatorId, UserInfo userInfo)
        {
            id = spectatorId;
            user = userInfo;
            name = userInfo.username;
            emailHash = userInfo.emailHash;
            
            // Set default spectator menu
            menuTitle = "Spectator mode";
            buttons = new List<UIButton>();
            
            Debug.Log($"👁️ Spectator {name} initialized");
        }

        /// <summary>
        /// Spectators don't select cards, so this always returns empty state
        /// </summary>
        /// <param name="card">The card being checked (ignored for spectators)</param>
        /// <returns>Empty selection state</returns>
        public Dictionary<string, object> GetCardSelectionState(BaseCard card = null)
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Spectators don't select rings, so this always returns empty state
        /// </summary>
        /// <param name="ring">The ring being checked (ignored for spectators)</param>
        /// <returns>Empty selection state</returns>
        public Dictionary<string, object> GetRingSelectionState(Ring ring = null)
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Returns a summary of this spectator for UI display
        /// </summary>
        /// <returns>Spectator summary information</returns>
        public SpectatorSummary GetShortSummary()
        {
            return new SpectatorSummary
            {
                name = name,
                id = id,
                type = "spectator",
                emailHash = emailHash,
                lobbyId = lobbyId,
                disconnected = disconnected,
                left = left
            };
        }

        /// <summary>
        /// Gets the current state of this spectator for network synchronization
        /// </summary>
        /// <returns>Complete spectator state</returns>
        public SpectatorState GetState()
        {
            return new SpectatorState
            {
                id = id,
                name = name,
                emailHash = emailHash,
                lobbyId = lobbyId,
                menuTitle = menuTitle,
                buttons = buttons.ConvertAll(button => button.GetState()),
                disconnected = disconnected,
                left = left,
                user = user
            };
        }

        /// <summary>
        /// Handle spectator disconnect
        /// </summary>
        public void Disconnect()
        {
            disconnected = true;
            socket = null;
            Debug.Log($"👁️ Spectator {name} disconnected");
        }

        /// <summary>
        /// Handle spectator reconnect
        /// </summary>
        /// <param name="newSocket">New socket connection</param>
        public void Reconnect(object newSocket)
        {
            disconnected = false;
            socket = newSocket;
            Debug.Log($"👁️ Spectator {name} reconnected");
        }

        /// <summary>
        /// Handle spectator leaving the game
        /// </summary>
        public void Leave()
        {
            left = true;
            Debug.Log($"👁️ Spectator {name} left the game");
        }

        /// <summary>
        /// Add a UI button for the spectator
        /// </summary>
        /// <param name="button">Button to add</param>
        public void AddButton(UIButton button)
        {
            if (button != null && !buttons.Contains(button))
            {
                buttons.Add(button);
            }
        }

        /// <summary>
        /// Remove a UI button from the spectator
        /// </summary>
        /// <param name="button">Button to remove</param>
        public void RemoveButton(UIButton button)
        {
            buttons.Remove(button);
        }

        /// <summary>
        /// Clear all UI buttons
        /// </summary>
        public void ClearButtons()
        {
            buttons.Clear();
        }

        /// <summary>
        /// Set the menu title for the spectator
        /// </summary>
        /// <param name="title">New menu title</param>
        public void SetMenuTitle(string title)
        {
            menuTitle = title ?? "Spectator mode";
        }

        /// <summary>
        /// Check if this spectator is actively connected
        /// </summary>
        /// <returns>True if connected and not left</returns>
        public bool IsActive()
        {
            return !disconnected && !left;
        }

        /// <summary>
        /// Get display name for this spectator
        /// </summary>
        /// <returns>Display name</returns>
        public override string ToString()
        {
            return name;
        }

        /// <summary>
        /// Cleanup when spectator is destroyed
        /// </summary>
        private void OnDestroy()
        {
            ClearButtons();
            Debug.Log($"👁️ Spectator {name} destroyed");
        }
    }

    /// <summary>
    /// Simple spectator summary for UI lists
    /// </summary>
    [System.Serializable]
    public class SpectatorSummary
    {
        public string name;
        public string id;
        public string type;
        public string emailHash;
        public string lobbyId;
        public bool disconnected;
        public bool left;
    }

    /// <summary>
    /// Complete spectator state for network synchronization
    /// </summary>
    [System.Serializable]
    public class SpectatorState
    {
        public string id;
        public string name;
        public string emailHash;
        public string lobbyId;
        public string menuTitle;
        public List<UIButtonState> buttons;
        public bool disconnected;
        public bool left;
        public UserInfo user;
    }

    /// <summary>
    /// Anonymous spectator for unknown viewers
    /// </summary>
    public class AnonymousSpectator : Spectator
    {
        public void Initialize()
        {
            var anonymousUser = new UserInfo
            {
                username = "Anonymous",
                emailHash = "",
                lobbyId = ""
            };
            
            Initialize("anonymous_" + UnityEngine.Random.Range(1000, 9999), anonymousUser);
            menuTitle = "Anonymous Spectator";
            
            Debug.Log("👁️ Anonymous spectator created");
        }

        /// <summary>
        /// Anonymous spectators get a generic summary
        /// </summary>
        /// <returns>Anonymous spectator summary</returns>
        public new SpectatorSummary GetShortSummary()
        {
            return new SpectatorSummary
            {
                name = "Anonymous",
                id = id,
                type = "anonymous_spectator",
                emailHash = "",
                lobbyId = "",
                disconnected = disconnected,
                left = left
            };
        }
    }

    /// <summary>
    /// UI Button state for spectator interface
    /// </summary>
    [System.Serializable]
    public class UIButton
    {
        public string text;
        public string command;
        public string arg;
        public bool disabled;
        public string tooltip;

        public UIButton(string buttonText, string buttonCommand, string buttonArg = "", bool isDisabled = false)
        {
            text = buttonText;
            command = buttonCommand;
            arg = buttonArg;
            disabled = isDisabled;
        }

        public UIButtonState GetState()
        {
            return new UIButtonState
            {
                text = text,
                command = command,
                arg = arg,
                disabled = disabled,
                tooltip = tooltip
            };
        }
    }

    /// <summary>
    /// Serializable UI button state
    /// </summary>
    [System.Serializable]
    public class UIButtonState
    {
        public string text;
        public string command;
        public string arg;
        public bool disabled;
        public string tooltip;
    }

    /// <summary>
    /// Extension methods for spectator management
    /// </summary>
    public static class SpectatorExtensions
    {
        /// <summary>
        /// Create a spectator from user info
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="user">User information</param>
        /// <param name="socketId">Socket identifier</param>
        /// <returns>New spectator instance</returns>
        public static Spectator CreateSpectator(this Game game, UserInfo user, string socketId)
        {
            var spectatorGO = new GameObject($"Spectator_{user.username}");
            spectatorGO.transform.SetParent(game.transform);
            
            var spectator = spectatorGO.AddComponent<Spectator>();
            spectator.Initialize(socketId, user);
            
            return spectator;
        }

        /// <summary>
        /// Create an anonymous spectator
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>New anonymous spectator instance</returns>
        public static AnonymousSpectator CreateAnonymousSpectator(this Game game)
        {
            var spectatorGO = new GameObject("AnonymousSpectator");
            spectatorGO.transform.SetParent(game.transform);
            
            var spectator = spectatorGO.AddComponent<AnonymousSpectator>();
            spectator.Initialize();
            
            return spectator;
        }

        /// <summary>
        /// Check if a player is a spectator
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if the player is a spectator</returns>
        public static bool IsSpectator(this Player player)
        {
            return player is Spectator;
        }

        /// <summary>
        /// Get all active spectators in a game
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>List of active spectators</returns>
        public static List<Spectator> GetActiveSpectators(this Game game)
        {
            return game.GetSpectators().Where(s => s.IsActive()).ToList();
        }

        /// <summary>
        /// Get spectator count for a game
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <returns>Number of active spectators</returns>
        public static int GetSpectatorCount(this Game game)
        {
            return game.GetActiveSpectators().Count;
        }
    }
}