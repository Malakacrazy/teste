using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using L5RGame.Extensions;

namespace L5RGame
{
    /// <summary>
    /// Comprehensive chat system for L5R card game supporting player communication,
    /// game notifications, chat commands, and moderation features.
    /// </summary>
    [System.Serializable]
    public class GameChat : MonoBehaviour
    {
        [Header("Chat Configuration")]
        [SerializeField] private int maxChatHistory = 200;
        [SerializeField] private int maxMessageLength = 500;
        [SerializeField] private bool enableProfanityFilter = true;
        [SerializeField] private bool enableSpectatorChat = true;
        [SerializeField] private bool enableEmotes = true;
        [SerializeField] private float messageThrottleRate = 1.0f; // seconds between messages
        
        [Header("UI References")]
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private UnityEngine.UI.ScrollRect chatScrollView;
        [SerializeField] private UnityEngine.UI.InputField chatInputField;
        [SerializeField] private Transform chatMessageParent;
        [SerializeField] private GameObject chatMessagePrefab;
        
        // Chat state
        private Game game;
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        private Dictionary<Player, DateTime> lastMessageTime = new Dictionary<Player, DateTime>();
        private HashSet<string> bannedWords = new HashSet<string>();
        private Dictionary<string, string> emoteMap = new Dictionary<string, string>();
        
        // Events
        public event Action<ChatMessage> OnMessageReceived;
        public event Action<ChatMessage> OnSystemMessage;
        public event Action<Player, string> OnPlayerMuted;
        public event Action<Player> OnPlayerUnmuted;
        
        #region Properties
        
        /// <summary>
        /// Current chat history
        /// </summary>
        public IReadOnlyList<ChatMessage> ChatHistory => chatHistory.AsReadOnly();
        
        /// <summary>
        /// Whether chat is currently active
        /// </summary>
        public bool IsChatActive { get; private set; } = true;
        
        /// <summary>
        /// Number of messages in history
        /// </summary>
        public int MessageCount => chatHistory.Count;
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// Initialize the chat system
        /// </summary>
        public void Initialize()
        {
            game = GetComponent<Game>();
            if (game == null)
            {
                Debug.LogError("GameChat must be attached to Game object");
                return;
            }
            
            InitializeProfanityFilter();
            InitializeEmotes();
            SetupUI();
            
            LogChat("Chat system initialized");
        }
        
        /// <summary>
        /// Initialize profanity filter
        /// </summary>
        private void InitializeProfanityFilter()
        {
            // Load banned words from resources or config
            // This is a basic implementation - in production, load from external source
            bannedWords.UnionWith(new[]
            {
                "spam", "cheat", "hack", "noob", "scrub"
                // Add more as needed
            });
        }
        
        /// <summary>
        /// Initialize emote system
        /// </summary>
        private void InitializeEmotes()
        {
            emoteMap = new Dictionary<string, string>
            {
                { ":)", "😊" },
                { ":(", "😞" },
                { ":D", "😄" },
                { ";)", "😉" },
                { ":P", "😛" },
                { "<3", "❤️" },
                { "GG", "🎮" },
                { "GL", "🍀" },
                { "HF", "😄" },
                { "honor", "🎎" },
                { "fate", "💰" },
                { "conflict", "⚔️" }
            };
        }
        
        /// <summary>
        /// Setup UI components
        /// </summary>
        private void SetupUI()
        {
            if (chatInputField != null)
            {
                chatInputField.onEndEdit.AddListener(OnChatInputSubmitted);
                chatInputField.characterLimit = maxMessageLength;
            }
            
            if (chatPanel != null)
            {
                chatPanel.SetActive(true);
            }
        }
        
        #endregion
        
        #region Message Handling
        
        /// <summary>
        /// Add a chat message from a player
        /// </summary>
        /// <param name="player">Player sending the message</param>
        /// <param name="message">Message content</param>
        public void AddChatMessage(Player player, string message)
        {
            if (player == null || string.IsNullOrWhiteSpace(message))
                return;
            
            // Check if player is muted
            if (IsPlayerMuted(player))
            {
                SendSystemMessageToPlayer(player, "You are currently muted and cannot send messages.");
                return;
            }
            
            // Check message throttling
            if (!CanPlayerSendMessage(player))
            {
                SendSystemMessageToPlayer(player, "Please wait before sending another message.");
                return;
            }
            
            // Process and validate message
            var processedMessage = ProcessMessage(message);
            if (string.IsNullOrWhiteSpace(processedMessage))
                return;
            
            // Create chat message
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = player,
                Content = processedMessage,
                Timestamp = DateTime.UtcNow,
                Type = ChatMessageType.Player,
                IsSpectator = game.IsSpectator(player)
            };
            
            // Check spectator restrictions
            if (chatMessage.IsSpectator && !enableSpectatorChat)
            {
                SendSystemMessageToPlayer(player, "Spectator chat is disabled.");
                return;
            }
            
            // Add to history and broadcast
            AddMessageToHistory(chatMessage);
            BroadcastMessage(chatMessage);
            
            // Update throttling
            lastMessageTime[player] = DateTime.UtcNow;
            
            LogChat($"Player message: [{player.name}] {processedMessage}");
        }
        
        /// <summary>
        /// Add a system message
        /// </summary>
        /// <param name="message">System message content</param>
        /// <param name="type">Type of system message</param>
        public void AddSystemMessage(string message, SystemMessageType type = SystemMessageType.Info)
        {
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = null,
                Content = message,
                Timestamp = DateTime.UtcNow,
                Type = ChatMessageType.System,
                SystemType = type
            };
            
            AddMessageToHistory(chatMessage);
            BroadcastMessage(chatMessage);
            
            OnSystemMessage?.Invoke(chatMessage);
            LogChat($"System message: {message}");
        }
        
        /// <summary>
        /// Add a game event message
        /// </summary>
        /// <param name="message">Game event message</param>
        public void AddGameMessage(string message)
        {
            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Sender = null,
                Content = message,
                Timestamp = DateTime.UtcNow,
                Type = ChatMessageType.Game
            };
            
            AddMessageToHistory(chatMessage);
            BroadcastMessage(chatMessage);
            
            LogChat($"Game message: {message}");
        }
        
        /// <summary>
        /// Format a message with parameters
        /// </summary>
        /// <param name="format">Message format string</param>
        /// <param name="args">Format arguments</param>
        /// <returns>Formatted message</returns>
        public string FormatMessage(string format, params object[] args)
        {
            try
            {
                if (args == null || args.Length == 0)
                    return format;
                
                // Convert objects to display-friendly strings
                var displayArgs = args.Select(arg => arg switch
                {
                    Player player => player.name,
                    BaseCard card => card.name,
                    Ring ring => ring.element,
                    null => "Unknown",
                    _ => arg.ToString()
                }).ToArray();
                
                return string.Format(format, displayArgs);
            }
            catch (Exception ex)
            {
                LogError($"Error formatting message '{format}': {ex.Message}");
                return format; // Return unformatted message as fallback
            }
        }
        
        #endregion
        
        #region Message Processing
        
        /// <summary>
        /// Process and filter a chat message
        /// </summary>
        /// <param name="message">Raw message</param>
        /// <returns>Processed message</returns>
        private string ProcessMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return string.Empty;
            
            // Trim and normalize
            message = message.Trim();
            
            if (message.Length > maxMessageLength)
            {
                message = message.Substring(0, maxMessageLength);
            }
            
            // Apply profanity filter
            if (enableProfanityFilter)
            {
                message = ApplyProfanityFilter(message);
            }
            
            // Process emotes
            if (enableEmotes)
            {
                message = ProcessEmotes(message);
            }
            
            // Escape HTML to prevent injection
            message = EscapeHtml(message);
            
            return message;
        }
        
        /// <summary>
        /// Apply profanity filter to message
        /// </summary>
        /// <param name="message">Message to filter</param>
        /// <returns>Filtered message</returns>
        private string ApplyProfanityFilter(string message)
        {
            foreach (var bannedWord in bannedWords)
            {
                var pattern = $@"\b{Regex.Escape(bannedWord)}\b";
                var replacement = new string('*', bannedWord.Length);
                message = Regex.Replace(message, pattern, replacement, RegexOptions.IgnoreCase);
            }
            
            return message;
        }
        
        /// <summary>
        /// Process emotes in message
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <returns>Message with emotes replaced</returns>
        private string ProcessEmotes(string message)
        {
            foreach (var emote in emoteMap)
            {
                message = message.Replace(emote.Key, emote.Value);
            }
            
            return message;
        }
        
        /// <summary>
        /// Escape HTML characters
        /// </summary>
        /// <param name="message">Message to escape</param>
        /// <returns>Escaped message</returns>
        private string EscapeHtml(string message)
        {
            return message
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#x27;");
        }
        
        #endregion
        
        #region Message History Management
        
        /// <summary>
        /// Add message to chat history
        /// </summary>
        /// <param name="message">Message to add</param>
        private void AddMessageToHistory(ChatMessage message)
        {
            chatHistory.Add(message);
            
            // Trim history if too long
            if (chatHistory.Count > maxChatHistory)
            {
                var removeCount = chatHistory.Count - maxChatHistory;
                chatHistory.RemoveRange(0, removeCount);
            }
        }
        
        /// <summary>
        /// Clear chat history
        /// </summary>
        public void ClearChatHistory()
        {
            chatHistory.Clear();
            RefreshChatUI();
            LogChat("Chat history cleared");
        }
        
        /// <summary>
        /// Get chat history for a specific player (respecting visibility rules)
        /// </summary>
        /// <param name="player">Player requesting history</param>
        /// <returns>Visible chat history</returns>
        public List<ChatMessage> GetChatHistoryForPlayer(Player player)
        {
            var visibleMessages = new List<ChatMessage>();
            
            foreach (var message in chatHistory)
            {
                if (IsMessageVisibleToPlayer(message, player))
                {
                    visibleMessages.Add(message);
                }
            }
            
            return visibleMessages;
        }
        
        /// <summary>
        /// Check if a message is visible to a specific player
        /// </summary>
        /// <param name="message">Message to check</param>
        /// <param name="player">Player viewing</param>
        /// <returns>True if visible</returns>
        private bool IsMessageVisibleToPlayer(ChatMessage message, Player player)
        {
            // System and game messages are always visible
            if (message.Type == ChatMessageType.System || message.Type == ChatMessageType.Game)
                return true;
            
            // Spectator messages may be restricted
            if (message.IsSpectator && !enableSpectatorChat)
            {
                // Only show to other spectators
                return game.IsSpectator(player);
            }
            
            return true;
        }
        
        #endregion
        
        #region UI Management
        
        /// <summary>
        /// Broadcast message to all clients
        /// </summary>
        /// <param name="message">Message to broadcast</param>
        private void BroadcastMessage(ChatMessage message)
        {
            // Update UI
            DisplayMessageInUI(message);
            
            // Trigger events
            OnMessageReceived?.Invoke(message);
            
            // Network broadcast would go here in full implementation
            // NetworkBroadcastChatMessage(message);
        }
        
        /// <summary>
        /// Display message in chat UI
        /// </summary>
        /// <param name="message">Message to display</param>
        private void DisplayMessageInUI(ChatMessage message)
        {
            if (chatMessagePrefab == null || chatMessageParent == null)
                return;
            
            try
            {
                var messageObject = Instantiate(chatMessagePrefab, chatMessageParent);
                var messageComponent = messageObject.GetComponent<ChatMessageUI>();
                
                if (messageComponent != null)
                {
                    messageComponent.SetMessage(message);
                }
                
                // Auto-scroll to bottom
                if (chatScrollView != null)
                {
                    Canvas.ForceUpdateCanvases();
                    chatScrollView.verticalNormalizedPosition = 0f;
                }
            }
            catch (Exception ex)
            {
                LogError($"Error displaying chat message: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Refresh entire chat UI
        /// </summary>
        private void RefreshChatUI()
        {
            if (chatMessageParent == null)
                return;
            
            // Clear existing messages
            foreach (Transform child in chatMessageParent)
            {
                if (child.gameObject != chatMessagePrefab)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Redisplay all messages
            foreach (var message in chatHistory)
            {
                DisplayMessageInUI(message);
            }
        }
        
        /// <summary>
        /// Handle chat input submission
        /// </summary>
        /// <param name="input">Input text</param>
        private void OnChatInputSubmitted(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;
            
            // Get local player
            var localPlayer = game.GetLocalPlayer();
            if (localPlayer == null)
                return;
            
            // Check if it's a command
            if (input.StartsWith("/"))
            {
                // Handle chat commands through ChatCommands system
                var chatCommands = game.GetComponent<ChatCommands>();
                if (chatCommands != null)
                {
                    chatCommands.ExecuteCommand(localPlayer, input);
                }
            }
            else
            {
                // Regular chat message
                AddChatMessage(localPlayer, input);
            }
            
            // Clear input field
            if (chatInputField != null)
            {
                chatInputField.text = "";
                chatInputField.ActivateInputField();
            }
        }
        
        #endregion
        
        #region Player Management
        
        /// <summary>
        /// Check if player can send a message (throttling)
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if player can send message</returns>
        private bool CanPlayerSendMessage(Player player)
        {
            if (!lastMessageTime.ContainsKey(player))
                return true;
            
            var timeSinceLastMessage = DateTime.UtcNow - lastMessageTime[player];
            return timeSinceLastMessage.TotalSeconds >= messageThrottleRate;
        }
        
        /// <summary>
        /// Check if player is muted
        /// </summary>
        /// <param name="player">Player to check</param>
        /// <returns>True if muted</returns>
        public bool IsPlayerMuted(Player player)
        {
            // In a full implementation, this would check a mute list
            // For now, return false (no one is muted)
            return player.HasProperty("muted") && player.GetProperty<bool>("muted");
        }
        
        /// <summary>
        /// Mute a player
        /// </summary>
        /// <param name="player">Player to mute</param>
        /// <param name="reason">Reason for muting</param>
        public void MutePlayer(Player player, string reason = "")
        {
            if (player == null)
                return;
            
            player.SetProperty("muted", true);
            player.SetProperty("muteReason", reason);
            
            AddSystemMessage($"{player.name} has been muted. {reason}", SystemMessageType.Warning);
            SendSystemMessageToPlayer(player, $"You have been muted. {reason}");
            
            OnPlayerMuted?.Invoke(player, reason);
            LogChat($"Player muted: {player.name} - {reason}");
        }
        
        /// <summary>
        /// Unmute a player
        /// </summary>
        /// <param name="player">Player to unmute</param>
        public void UnmutePlayer(Player player)
        {
            if (player == null)
                return;
            
            player.SetProperty("muted", false);
            player.RemoveProperty("muteReason");
            
            AddSystemMessage($"{player.name} has been unmuted.", SystemMessageType.Info);
            SendSystemMessageToPlayer(player, "You have been unmuted.");
            
            OnPlayerUnmuted?.Invoke(player);
            LogChat($"Player unmuted: {player.name}");
        }
        
        /// <summary>
        /// Send a system message to a specific player
        /// </summary>
        /// <param name="player">Target player</param>
        /// <param name="message">Message to send</param>
        private void SendSystemMessageToPlayer(Player player, string message)
        {
            // In a full implementation, this would send a private message
            // For now, we'll just log it
            LogChat($"Private message to {player.name}: {message}");
        }
        
        #endregion
        
        #region Chat Settings
        
        /// <summary>
        /// Enable or disable spectator chat
        /// </summary>
        /// <param name="enabled">Whether to enable</param>
        public void SetSpectatorChatEnabled(bool enabled)
        {
            enableSpectatorChat = enabled;
            
            var status = enabled ? "enabled" : "disabled";
            AddSystemMessage($"Spectator chat has been {status}.", SystemMessageType.Info);
        }
        
        /// <summary>
        /// Enable or disable emotes
        /// </summary>
        /// <param name="enabled">Whether to enable</param>
        public void SetEmotesEnabled(bool enabled)
        {
            enableEmotes = enabled;
            LogChat($"Emotes {(enabled ? "enabled" : "disabled")}");
        }
        
        /// <summary>
        /// Set message throttle rate
        /// </summary>
        /// <param name="seconds">Seconds between messages</param>
        public void SetMessageThrottleRate(float seconds)
        {
            messageThrottleRate = Mathf.Max(0.1f, seconds);
            LogChat($"Message throttle rate set to {messageThrottleRate} seconds");
        }
        
        /// <summary>
        /// Add word to profanity filter
        /// </summary>
        /// <param name="word">Word to ban</param>
        public void AddBannedWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                bannedWords.Add(word.ToLower());
                LogChat($"Added banned word: {word}");
            }
        }
        
        /// <summary>
        /// Remove word from profanity filter
        /// </summary>
        /// <param name="word">Word to unban</param>
        public void RemoveBannedWord(string word)
        {
            if (!string.IsNullOrWhiteSpace(word))
            {
                bannedWords.Remove(word.ToLower());
                LogChat($"Removed banned word: {word}");
            }
        }
        
        #endregion
        
        #region Logging
        
        /// <summary>
        /// Log chat system message
        /// </summary>
        /// <param name="message">Message to log</param>
        private void LogChat(string message)
        {
            Debug.Log($"💬 GameChat: {message}");
        }
        
        /// <summary>
        /// Log chat error
        /// </summary>
        /// <param name="message">Error message</param>
        private void LogError(string message)
        {
            Debug.LogError($"💬❌ GameChat Error: {message}");
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get chat statistics
        /// </summary>
        /// <returns>Chat statistics</returns>
        public ChatStatistics GetChatStatistics()
        {
            return new ChatStatistics
            {
                TotalMessages = chatHistory.Count,
                PlayerMessages = chatHistory.Count(m => m.Type == ChatMessageType.Player),
                SystemMessages = chatHistory.Count(m => m.Type == ChatMessageType.System),
                GameMessages = chatHistory.Count(m => m.Type == ChatMessageType.Game),
                SpectatorMessages = chatHistory.Count(m => m.IsSpectator),
                MutedPlayers = game.GetPlayers().Count(p => IsPlayerMuted(p)),
                ChatEnabled = IsChatActive
            };
        }
        
        /// <summary>
        /// Export chat history as text
        /// </summary>
        /// <returns>Chat history as formatted text</returns>
        public string ExportChatHistory()
        {
            var export = new System.Text.StringBuilder();
            export.AppendLine($"Chat History Export - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            export.AppendLine($"Game: {game.gameName}");
            export.AppendLine(new string('=', 50));
            
            foreach (var message in chatHistory)
            {
                var timestamp = message.Timestamp.ToString("HH:mm:ss");
                var sender = message.Sender?.name ?? "System";
                export.AppendLine($"[{timestamp}] {sender}: {message.Content}");
            }
            
            return export.ToString();
        }
        
        #endregion
    }
    
    #region Supporting Classes
    
    /// <summary>
    /// Represents a chat message
    /// </summary>
    [System.Serializable]
    public class ChatMessage
    {
        public string Id;
        public Player Sender;
        public string Content;
        public DateTime Timestamp;
        public ChatMessageType Type;
        public SystemMessageType SystemType;
        public bool IsSpectator;
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Types of chat messages
    /// </summary>
    public enum ChatMessageType
    {
        Player,
        System,
        Game
    }
    
    /// <summary>
    /// Types of system messages
    /// </summary>
    public enum SystemMessageType
    {
        Info,
        Warning,
        Error,
        Success
    }
    
    /// <summary>
    /// Chat statistics for monitoring
    /// </summary>
    public class ChatStatistics
    {
        public int TotalMessages;
        public int PlayerMessages;
        public int SystemMessages;
        public int GameMessages;
        public int SpectatorMessages;
        public int MutedPlayers;
        public bool ChatEnabled;
    }
    
    /// <summary>
    /// UI component for displaying individual chat messages
    /// </summary>
    public class ChatMessageUI : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Text senderText;
        [SerializeField] private UnityEngine.UI.Text contentText;
        [SerializeField] private UnityEngine.UI.Text timestampText;
        [SerializeField] private UnityEngine.UI.Image backgroundImage;
        
        public void SetMessage(ChatMessage message)
        {
            if (senderText != null)
            {
                senderText.text = message.Sender?.name ?? "System";
                senderText.color = GetSenderColor(message);
            }
            
            if (contentText != null)
            {
                contentText.text = message.Content;
            }
            
            if (timestampText != null)
            {
                timestampText.text = message.Timestamp.ToString("HH:mm");
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = GetBackgroundColor(message);
            }
        }
        
        private Color GetSenderColor(ChatMessage message)
        {
            return message.Type switch
            {
                ChatMessageType.Player => message.IsSpectator ? Color.gray : Color.white,
                ChatMessageType.System => Color.yellow,
                ChatMessageType.Game => Color.cyan,
                _ => Color.white
            };
        }
        
        private Color GetBackgroundColor(ChatMessage message)
        {
            return message.Type switch
            {
                ChatMessageType.System when message.SystemType == SystemMessageType.Error => 
                    new Color(1f, 0.2f, 0.2f, 0.3f),
                ChatMessageType.System when message.SystemType == SystemMessageType.Warning => 
                    new Color(1f, 1f, 0.2f, 0.3f),
                ChatMessageType.System when message.SystemType == SystemMessageType.Success => 
                    new Color(0.2f, 1f, 0.2f, 0.3f),
                _ => new Color(0f, 0f, 0f, 0.1f)
            };
        }
    }
    
    #endregion
}
