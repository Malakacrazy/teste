using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

#if UNITY_EDITOR || UNITY_STANDALONE
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
#endif

namespace L5RGame
{
    [Serializable]
    public class GameDetails
    {
        public string id;
        public string name;
        public bool allowSpectators;
        public bool spectatorSquelch;
        public string owner;
        public string savedGameId;
        public string gameType;
        public string password;
        public List<PlayerDetails> players;
        public List<SpectatorDetails> spectators;
        public ClockSettings clocks;
    }

    [Serializable]
    public class PlayerDetails
    {
        public string id;
        public UserInfo user;
    }

    [Serializable]
    public class SpectatorDetails
    {
        public string id;
        public UserInfo user;
    }

    [Serializable]
    public class UserInfo
    {
        public string username;
        public string emailHash;
        public string lobbyId;
    }

    [Serializable]
    public class ClockSettings
    {
        public int mainTime = 3600; // 1 hour in seconds
        public int increment = 30;  // 30 seconds
    }

    [Serializable]
    public class ConflictRecord
    {
        public Player attackingPlayer;
        public string declaredType;
        public bool passed;
        public string uuid;
        public bool completed;
        public Player winner;
        public bool typeSwitched;
    }

    [Serializable]
    public class ShortCardData
    {
        public string id;
        public string name;
    }

    public class Game : MonoBehaviour
    {
        // Events
        public event System.Action<string> OnGameMessage;
        public event System.Action<string, string> OnGameAlert;
        public event System.Action<Player> OnGameWon;
        public event System.Action OnGameStateChanged;
        public event System.Action<object, string> OnEventTriggered;
        public event System.Action<string, string> OnPhaseChanged;
        public event System.Action<string, object> OnGameStateChangedWithData;

        // Core game components
        [Header("Game Components")]
        [SerializeField] private EffectEngine effectEngine;
        [SerializeField] private GameChat gameChat;
        [SerializeField] private ChatCommands chatCommands;
        [SerializeField] private GamePipeline pipeline;
        
        // Game state
        [Header("Game Settings")]
        public string gameId;
        public string gameName;
        public bool allowSpectators = true;
        public bool spectatorSquelch = false;
        public string owner;
        public bool started = false;
        public bool playStarted = false;
        public string gameType;
        public bool manualMode = false;
        public string currentPhase = "";
        public string password;
        public int roundNumber = 0;

        // Game objects
        private Dictionary<string, Player> playersAndSpectators = new Dictionary<string, Player>();
        private Dictionary<string, Ring> rings = new Dictionary<string, Ring>();
        private List<ConflictRecord> conflictRecord = new List<ConflictRecord>();
        private List<BaseCard> allCards = new List<BaseCard>();
        private List<BaseCard> provinceCards = new List<BaseCard>();
        private List<ShortCardData> shortCardData = new List<ShortCardData>();
        
        // Current state
        [Header("Current Game State")]
        public AbilityWindow currentAbilityWindow;
        public ActionWindow currentActionWindow;
        public EventWindow currentEventWindow;
        public Conflict currentConflict;
        public Duel currentDuel;
        
        // Game statistics
        [Header("Game Statistics")]
        public DateTime createdAt;
        public DateTime startedAt;
        public DateTime finishedAt;
        public Player winner;
        public string winReason;
        public string savedGameId;

        /// <summary>
        /// Gets the game pipeline for managing game steps
        /// </summary>
        public GamePipeline Pipeline => pipeline;
        
        // Network reference
        public IGameRouter router;

        // IronPython Integration
        [Header("IronPython Settings")]
        public bool enablePythonScripting = true;
        public bool debugPython = true;
        
#if UNITY_EDITOR || UNITY_STANDALONE
        private ScriptEngine pythonEngine;
        private Dictionary<string, ScriptSource> cardScripts = new Dictionary<string, ScriptSource>();
#endif

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (effectEngine == null)
                effectEngine = GetComponent<EffectEngine>() ?? gameObject.AddComponent<EffectEngine>();
            
            if (gameChat == null)
                gameChat = GetComponent<GameChat>() ?? gameObject.AddComponent<GameChat>();
                
            if (chatCommands == null)
                chatCommands = GetComponent<ChatCommands>() ?? gameObject.AddComponent<ChatCommands>();
                
            if (pipeline == null)
                pipeline = GetComponent<GamePipeline>() ?? gameObject.AddComponent<GamePipeline>();

            createdAt = DateTime.Now;
            
            InitializeRings();
            InitializePython();
        }

        private void InitializeRings()
        {
            rings["air"] = new Ring(this, "air", ConflictType.Military);
            rings["earth"] = new Ring(this, "earth", ConflictType.Political);
            rings["fire"] = new Ring(this, "fire", ConflictType.Military);
            rings["void"] = new Ring(this, "void", ConflictType.Political);
            rings["water"] = new Ring(this, "water", ConflictType.Military);
        }

        private void InitializePython()
        {
            if (!enablePythonScripting) return;

#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                pythonEngine = Python.CreateEngine();
                
                if (debugPython)
                    Debug.Log("🐍 IronPython initialized for card scripting!");
                    
                LoadCardScripts();
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to initialize IronPython: {e.Message}");
                enablePythonScripting = false;
            }
#else
            Debug.LogWarning("⚠️ IronPython only available in Editor/Standalone builds");
            enablePythonScripting = false;
#endif
        }

        private void LoadCardScripts()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!enablePythonScripting) return;
            
            string scriptsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "CardScripts");
            
            if (!System.IO.Directory.Exists(scriptsPath))
            {
                System.IO.Directory.CreateDirectory(scriptsPath);
                CreateExampleCardScript(scriptsPath);
                return;
            }
            
            string[] pythonFiles = System.IO.Directory.GetFiles(scriptsPath, "*.py", System.IO.SearchOption.AllDirectories);
            
            foreach (string file in pythonFiles)
            {
                LoadCardScript(file);
            }
            
            if (debugPython)
                Debug.Log($"🐍 Loaded {cardScripts.Count} Python card scripts");
#endif
        }

        private void LoadCardScript(string filePath)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            try
            {
                string scriptName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                string scriptContent = System.IO.File.ReadAllText(filePath);
                
                ScriptSource source = pythonEngine.CreateScriptSourceFromString(scriptContent, scriptName);
                cardScripts[scriptName] = source;
                
                if (debugPython)
                    Debug.Log($"🐍 Loaded card script: {scriptName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Failed to load Python script {filePath}: {e.Message}");
            }
#endif
        }

        private void CreateExampleCardScript(string scriptsPath)
        {
            string exampleScript = @"# Ashigaru Levy - Example Character Card Script
def on_card_played(card, player, context):
    print(f'🎴 {card.name} played by {player.name}!')
    
def on_enter_play(card, player):
    print(f'🎴 {card.name} enters play under {player.name} control')
    # Gain 1 fate when this character enters play
    player.fate += 1
    print(f'💰 {player.name} gains 1 fate from {card.name}')

def on_leave_play(card, player):
    print(f'🎴 {card.name} leaves play')

def on_conflict(card, conflict):
    if conflict.attacking_player == card.controller:
        # This character gets +1 military skill when attacking
        original_skill = card.military_skill
        card.military_skill += 1
        print(f'⚔️ {card.name} gets +1 military skill ({original_skill} -> {card.military_skill}) when attacking')

def can_trigger(card, event_name, event_data):
    # This card can trigger during conflict phase
    return event_name == 'conflict_declared'

def on_trigger(card, event_name, event_data):
    if event_name == 'conflict_declared':
        print(f'⚡ {card.name} triggered ability!')
        # Add custom trigger effect here
        card.controller.draw_cards(1)
        print(f'📜 {card.controller.name} draws 1 card from {card.name} ability')
";
            
            string examplePath = System.IO.Path.Combine(scriptsPath, "ashigaru_levy.py");
            System.IO.File.WriteAllText(examplePath, exampleScript);
            
            if (debugPython)
                Debug.Log($"🐍 Created example Python script: {examplePath}");
        }

        public object ExecuteCardScript(string scriptName, string functionName, params object[] parameters)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!enablePythonScripting || !cardScripts.ContainsKey(scriptName))
            {
                return null;
            }
            
            try
            {
                ScriptScope scope = pythonEngine.CreateScope();
                
                // Add game context to Python scope
                scope.SetVariable("game", this);
                scope.SetVariable("Unity", typeof(UnityEngine.Debug));
                
                // Execute the script
                cardScripts[scriptName].Execute(scope);
                
                // Get and call the function
                if (scope.ContainsVariable(functionName))
                {
                    dynamic function = scope.GetVariable(functionName);
                    return function(parameters);
                }
                else
                {
                    if (debugPython)
                        Debug.LogWarning($"🐍 Function '{functionName}' not found in script '{scriptName}'");
                    return null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error executing card script '{scriptName}.{functionName}': {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        public void Initialize(GameDetails details)
        {
            gameId = details.id;
            gameName = details.name;
            allowSpectators = details.allowSpectators;
            spectatorSquelch = details.spectatorSquelch;
            owner = details.owner;
            gameType = details.gameType;
            password = details.password;
            savedGameId = details.savedGameId;

            // Initialize players
            foreach (var playerDetail in details.players)
            {
                var player = CreatePlayer(playerDetail, details.clocks);
                playersAndSpectators[playerDetail.user.username] = player;
            }

            // Initialize spectators
            foreach (var spectatorDetail in details.spectators)
            {
                var spectator = CreateSpectator(spectatorDetail);
                playersAndSpectators[spectatorDetail.user.username] = spectator;
            }

            effectEngine.Initialize(this);
            gameChat.Initialize(this);
            chatCommands.Initialize(this);
            pipeline.Initialize(this);
        }

        private Player CreatePlayer(PlayerDetails details, ClockSettings clocks)
        {
            var playerGO = new GameObject($"Player_{details.user.username}");
            playerGO.transform.SetParent(transform);
            
            var player = playerGO.AddComponent<Player>();
            player.Initialize(details.id, details.user, owner == details.user.username, this, clocks);
            
            return player;
        }

        private Spectator CreateSpectator(SpectatorDetails details)
        {
            var spectatorGO = new GameObject($"Spectator_{details.user.username}");
            spectatorGO.transform.SetParent(transform);
            
            var spectator = spectatorGO.AddComponent<Spectator>();
            spectator.Initialize(details.id, details.user);
            
            return spectator;
        }

        // Error handling
        public void ReportError(Exception e)
        {
            Debug.LogError($"🚨 Game Error: {e.Message}\n{e.StackTrace}");
            router?.HandleError(this, e);
        }

        // Messaging
        public void AddMessage(string message, params object[] args)
        {
            string formattedMessage = gameChat.FormatMessage(message, args);
            gameChat.AddMessage(formattedMessage);
            OnGameMessage?.Invoke(formattedMessage);
        }

        public void AddAlert(string type, string message, params object[] args)
        {
            string formattedMessage = gameChat.FormatMessage(message, args);
            gameChat.AddAlert(type, formattedMessage);
            OnGameAlert?.Invoke(type, formattedMessage);
        }

        public List<string> Messages => gameChat.messages.ToList();

        // Player management
        public bool IsSpectator(Player player)
        {
            return player is Spectator;
        }

        public bool HasActivePlayer(string playerName)
        {
            return playersAndSpectators.ContainsKey(playerName) && 
                   !playersAndSpectators[playerName].left;
        }

        public List<Player> GetPlayers()
        {
            return playersAndSpectators.Values
                .Where(p => !IsSpectator(p))
                .Cast<Player>()
                .ToList();
        }

        public Player GetPlayerByName(string playerName)
        {
            if (playersAndSpectators.TryGetValue(playerName, out Player player) && !IsSpectator(player))
            {
                return player;
            }
            return null;
        }

        public List<Player> GetPlayersInFirstPlayerOrder()
        {
            return GetPlayers().OrderBy(p => p.firstPlayer ? 0 : 1).ToList();
        }

        public Dictionary<string, Player> GetPlayersAndSpectators()
        {
            return new Dictionary<string, Player>(playersAndSpectators);
        }

        public List<Spectator> GetSpectators()
        {
            return playersAndSpectators.Values
                .Where(p => IsSpectator(p))
                .Cast<Spectator>()
                .ToList();
        }

        public Player GetFirstPlayer()
        {
            return GetPlayers().FirstOrDefault(p => p.firstPlayer);
        }

        public Player GetOtherPlayer(Player player)
        {
            return GetPlayers().FirstOrDefault(p => p.name != player.name);
        }

        // Card management
        public BaseCard FindAnyCardInPlayByUuid(string cardId)
        {
            foreach (var player in GetPlayers())
            {
                var card = player.FindCardInPlayByUuid(cardId);
                if (card != null)
                    return card;
            }
            return null;
        }

        public BaseCard FindAnyCardInAnyList(string cardId)
        {
            return allCards.FirstOrDefault(card => card.uuid == cardId);
        }

        public List<BaseCard> FindAnyCardsInAnyList(System.Func<BaseCard, bool> predicate)
        {
            return allCards.Where(predicate).ToList();
        }

        public List<BaseCard> FindAnyCardsInPlay(System.Func<BaseCard, bool> predicate)
        {
            var foundCards = new List<BaseCard>();
            foreach (var player in GetPlayers())
            {
                foundCards.AddRange(player.FindCards(player.cardsInPlay, predicate));
            }
            return foundCards;
        }

        public BaseCard CreateToken(BaseCard card)
        {
            // Create a spirit token (placeholder implementation)
            var tokenGO = new GameObject($"Token_{card.name}");
            tokenGO.transform.SetParent(transform);
            var token = tokenGO.AddComponent<BaseCard>();
            token.Initialize(card);
            allCards.Add(token);
            return token;
        }

        // Game Actions accessor
        public GameActions Actions => new GameActions();

        // Conflict management
        public bool IsDuringConflict(params string[] types)
        {
            if (currentConflict == null)
                return false;
            
            if (types == null || types.Length == 0)
                return true;
                
            return types.All(type => 
                currentConflict.elements.Contains(type) || 
                currentConflict.conflictType == type);
        }

        public void RecordConflict(Conflict conflict)
        {
            var record = new ConflictRecord
            {
                attackingPlayer = conflict.attackingPlayer,
                declaredType = conflict.declaredType,
                passed = conflict.conflictPassed,
                uuid = conflict.uuid
            };
            
            conflictRecord.Add(record);
            
            conflict.attackingPlayer.conflictOpportunities.total--;
            
            if (conflict.conflictPassed || conflict.forcedDeclaredType)
            {
                conflict.attackingPlayer.conflictOpportunities.military = Mathf.Max(
                    conflict.attackingPlayer.conflictOpportunities.military,
                    conflict.attackingPlayer.conflictOpportunities.total
                );
                conflict.attackingPlayer.conflictOpportunities.political = Mathf.Max(
                    conflict.attackingPlayer.conflictOpportunities.political,
                    conflict.attackingPlayer.conflictOpportunities.total
                );
            }
            else
            {
                if (conflict.declaredType == "military")
                    conflict.attackingPlayer.conflictOpportunities.military--;
                else if (conflict.declaredType == "political")
                    conflict.attackingPlayer.conflictOpportunities.political--;
            }
        }

        public List<ConflictRecord> GetConflicts(Player player)
        {
            if (player == null)
                return new List<ConflictRecord>();
                
            return conflictRecord.Where(record => record.attackingPlayer == player).ToList();
        }

        public void RecordConflictWinner(Conflict conflict)
        {
            var record = conflictRecord.FirstOrDefault(r => r.uuid == conflict.uuid);
            if (record != null)
            {
                record.completed = true;
                record.winner = conflict.winner;
                record.typeSwitched = conflict.conflictTypeSwitched;
            }
        }

        // Clock management
        public void StopClocks()
        {
            foreach (var player in GetPlayers())
            {
                player.StopClock();
            }
        }

        public void ResetClocks()
        {
            foreach (var player in GetPlayers())
            {
                player.ResetClock();
            }
        }

        // Input handling
        public void CardClicked(string sourcePlayer, string cardId)
        {
            var player = GetPlayerByName(sourcePlayer);
            if (player == null) return;

            var card = FindAnyCardInAnyList(cardId);
            if (card == null) return;

            pipeline.HandleCardClicked(player, card);
        }

        public void FacedownCardClicked(string playerName, string location, string controllerName, bool isProvince = false)
        {
            var player = GetPlayerByName(playerName);
            var controller = GetPlayerByName(controllerName);
            
            if (player == null || controller == null) return;

            var list = controller.GetSourceList(location);
            if (list == null) return;

            var card = list.FirstOrDefault(c => c.isProvince == isProvince);
            if (card != null)
            {
                pipeline.HandleCardClicked(player, card);
            }
        }

        public void RingClicked(string sourcePlayer, string ringElement)
        {
            var player = GetPlayerByName(sourcePlayer);
            if (player == null || !rings.ContainsKey(ringElement)) return;

            var ring = rings[ringElement];

            if (pipeline.HandleRingClicked(player, ring))
                return;

            // If not conflict phase and ring not claimed, flip it
            if (currentPhase != GamePhases.Conflict && !ring.claimed)
            {
                ring.FlipConflictType();
            }
        }

        public void MenuItemClick(string sourcePlayer, string cardId, MenuCommand menuItem)
        {
            var player = GetPlayerByName(sourcePlayer);
            var card = FindAnyCardInAnyList(cardId);
            if (player == null || card == null) return;

            if (menuItem.command == "click")
            {
                CardClicked(sourcePlayer, cardId);
                return;
            }

            MenuCommandsHelper.CardMenuClick(menuItem, this, player, card);
            CheckGameState(true);
        }

        public void RingMenuItemClick(string sourcePlayer, Ring sourceRing, MenuCommand menuItem)
        {
            var player = GetPlayerByName(sourcePlayer);
            var ring = rings[sourceRing.element];
            if (player == null || ring == null) return;

            if (menuItem.command == "click")
            {
                RingClicked(sourcePlayer, ring.element);
                return;
            }
            
            MenuCommandsHelper.RingMenuClick(menuItem, this, player, ring);
            CheckGameState(true);
        }

        // Deck interaction
        public void ShowConflictDeck(string playerName)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            if (!player.showConflict)
            {
                player.ShowConflictDeck();
                AddMessage("{0} is looking at their conflict deck", player);
            }
            else
            {
                player.showConflict = false;
                AddMessage("{0} stops looking at their conflict deck", player);
            }
        }

        public void ShowDynastyDeck(string playerName)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            if (!player.showDynasty)
            {
                player.ShowDynastyDeck();
                AddMessage("{0} is looking at their dynasty deck", player);
            }
            else
            {
                player.showDynasty = false;
                AddMessage("{0} stops looking at their dynasty deck", player);
            }
        }

        public void Drop(string playerName, string cardId, string source, string target)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            player.Drop(cardId, source, target);
        }

        // Win condition checking
        public void CheckWinCondition()
        {
            foreach (var player in GetPlayersInFirstPlayerOrder())
            {
                if (player.honor >= 25)
                {
                    RecordWinner(player, "honor");
                }
                else if (player.opponent != null && player.opponent.honor <= 0)
                {
                    RecordWinner(player, "dishonor");
                }
            }
        }

        public void RecordWinner(Player winnerPlayer, string reason)
        {
            if (winner != null) return;

            AddMessage("{0} has won the game", winnerPlayer);

            winner = winnerPlayer;
            finishedAt = DateTime.Now;
            winReason = reason;

            router?.GameWon(this, reason, winnerPlayer);
            OnGameWon?.Invoke(winnerPlayer);

            QueueStep(new GameWonPrompt(this, winnerPlayer));
        }

        public void SetFirstPlayer(Player firstPlayer)
        {
            foreach (var player in GetPlayers())
            {
                player.firstPlayer = (player == firstPlayer);
            }
        }

        public void ChangeStat(string playerName, string stat, int value)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            // Use reflection or switch statement to modify the stat
            switch (stat.ToLower())
            {
                case "honor":
                    player.honor += value;
                    break;
                case "fate":
                    player.fate += value;
                    break;
                default:
                    Debug.LogWarning($"Unknown stat: {stat}");
                    return;
            }

            if (GetStatValue(player, stat) < 0)
            {
                SetStatValue(player, stat, 0);
            }
            else
            {
                AddMessage("{0} sets {1} to {2} ({3})", player, stat, GetStatValue(player, stat), (value > 0 ? "+" : "") + value);
            }
        }

        private int GetStatValue(Player player, string stat)
        {
            switch (stat.ToLower())
            {
                case "honor": return player.honor;
                case "fate": return player.fate;
                default: return 0;
            }
        }

        private void SetStatValue(Player player, string stat, int value)
        {
            switch (stat.ToLower())
            {
                case "honor": player.honor = value; break;
                case "fate": player.fate = value; break;
            }
        }

        // Chat system
        public void Chat(string playerName, string message)
        {
            var player = playersAndSpectators.ContainsKey(playerName) ? playersAndSpectators[playerName] : null;
            var args = message.Split(' ');

            if (player == null) return;

            if (!IsSpectator(player))
            {
                if (chatCommands.ExecuteCommand(player, args[0], args))
                {
                    CheckGameState(true);
                    return;
                }

                var card = shortCardData.FirstOrDefault(c => 
                    c.name.ToLower() == message.ToLower() || 
                    c.id.ToLower() == message.ToLower());

                if (card != null)
                {
                    gameChat.AddChatMessage(player, gameChat.FormatMessage("{0}", card));
                    return;
                }
            }

            if (!IsSpectator(player) || !spectatorSquelch)
            {
                gameChat.AddChatMessage(player, message);
            }
        }

        public void Concede(string playerName)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            AddMessage("{0} concedes", player);

            var otherPlayer = GetOtherPlayer(player);
            if (otherPlayer != null)
            {
                RecordWinner(otherPlayer, "concede");
            }
        }

        // Deck management
        public void SelectDeck(string playerName, Deck deck)
        {
            var player = GetPlayerByName(playerName);
            if (player != null)
            {
                player.SelectDeck(deck);
            }
        }

        public void ShuffleConflictDeck(string playerName)
        {
            var player = GetPlayerByName(playerName);
            if (player != null)
            {
                player.ShuffleConflictDeck();
            }
        }

        public void ShuffleDynastyDeck(string playerName)
        {
            var player = GetPlayerByName(playerName);
            if (player != null)
            {
                player.ShuffleDynastyDeck();
            }
        }

        // Action window management
        public ActionWindow OpenActionWindow(string windowName, string windowType, System.Action onComplete = null)
        {
            // Create and configure action window
            var windowGO = new GameObject($"ActionWindow_{windowName}");
            windowGO.transform.SetParent(transform);
            var actionWindow = windowGO.AddComponent<ActionWindow>();
            actionWindow.Initialize(this, windowName, windowType, onComplete);
            
            currentActionWindow = actionWindow;
            
            AddMessage("Opening {0} action window", windowName);
            return actionWindow;
        }
        
        /// <summary>
        /// Close the current action window
        /// </summary>
        public void CloseActionWindow()
        {
            if (currentActionWindow != null)
            {
                currentActionWindow.Complete();
                currentActionWindow = null;
            }
        }
        public void PromptWithMenu(Player player, object contextObj, MenuPromptProperties properties)
        {
            QueueStep(new MenuPrompt(this, player, contextObj, properties));
        }

        public void PromptWithHandlerMenu(Player player, HandlerMenuPromptProperties properties)
        {
            QueueStep(new HandlerMenuPrompt(this, player, properties));
        }

        public void PromptForSelect(Player player, SelectCardPromptProperties properties)
        {
            QueueStep(new SelectCardPrompt(this, player, properties));
        }

        public void PromptForRingSelect(Player player, SelectRingPromptProperties properties)
        {
            QueueStep(new SelectRingPrompt(this, player, properties));
        }

        public void PromptForHonorBid(string activePromptTitle, System.Action<int> costHandler, List<int> prohibitedBids, Duel duel = null)
        {
            QueueStep(new HonorBidPrompt(this, activePromptTitle, costHandler, prohibitedBids, duel));
        }

        public bool MenuButton(string playerName, string arg, string uuid, string method)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return false;

            return pipeline.HandleMenuCommand(player, arg, uuid, method);
        }

        // Settings
        public void TogglePromptedActionWindow(string playerName, string windowName, bool toggle)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            player.promptedActionWindows[windowName] = toggle;
        }

        public void ToggleTimerSetting(string playerName, string settingName, bool toggle)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            player.timerSettings[settingName] = toggle;
        }

        public void ToggleOptionSetting(string playerName, string settingName, bool toggle)
        {
            var player = GetPlayerByName(playerName);
            if (player == null) return;

            player.optionSettings[settingName] = toggle;
        }

        public void ToggleManualMode(string playerName)
        {
            chatCommands.Manual(playerName);
        }
        
        /// <summary>
        /// Get framework context for system operations
        /// </summary>
        /// <param name="player">Player for the context (optional)</param>
        /// <returns>Framework ability context</returns>
        public AbilityContext GetFrameworkContext(Player player = null)
        {
            return AbilityContext.CreateFrameworkContext(this, player);
        }
        
        /// <summary>
        /// Get the local player (player associated with this client)
        /// </summary>
        /// <returns>Local player or null if not found</returns>
        public Player GetLocalPlayer()
        {
            return GameHelper.GetLocalPlayer(this);
        }
        
        /// <summary>
        /// Apply a game action to the game state
        /// </summary>
        public void ApplyGameAction(AbilityContext context, Dictionary<string, object> actionData)
        {
            // Placeholder implementation
            Debug.Log($"Applying game action: {string.Join(", ", actionData.Keys)}");
        }
        
        /// <summary>
        /// Emit a game event
        /// </summary>
        public void EmitEvent(string eventName, Dictionary<string, object> eventData)
        {
            OnEventTriggered?.Invoke(eventData, currentPhase);
        }
        
        /// <summary>
        /// Queue a simple step in the game pipeline
        /// </summary>
        public void QueueSimpleStep(System.Func<bool> step)
        {
            pipeline.QueueStep(new SimpleStep(this, step));
        }
        
        /// <summary>
        /// Queue a step in the game pipeline
        /// </summary>
        public void QueueStep(IGameStep step)
        {
            pipeline.QueueStep(step);
        }
        
        /// <summary>
        /// Get all rings in the game
        /// </summary>
        public List<Ring> GetRings()
        {
            return rings.Values.ToList();
        }

        // Game flow
        public void InitializeGame()
        {
            // Remove disconnected players
            var activePlayers = playersAndSpectators.Where(kvp => !kvp.Value.left)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            playersAndSpectators = activePlayers;

            // Check for strongholds
            Player playerWithNoStronghold = null;
            foreach (var player in GetPlayers())
            {
                player.Initialize();
                if (player.stronghold == null)
                {
                    playerWithNoStronghold = player;
                }
            }

            // Gather all cards
            allCards = GetPlayers()
                .SelectMany(player => player.preparedDeck.allCards)
                .ToList();
            provinceCards = allCards.Where(card => card.isProvince).ToList();

            if (playerWithNoStronghold != null)
            {
                QueueSimpleStep(() => {
                    AddMessage("{0} does not have a stronghold in their decklist", playerWithNoStronghold);
                    return false;
                });
                Continue();
                return;
            }

            // Initialize game phases
            pipeline.Initialize(new List<IGameStep>
            {
                new SetupPhase(this),
                new SimpleStep(this, BeginRound)
            });

            playStarted = true;
            startedAt = DateTime.Now;

            Continue();
        }

        private bool BeginRound()
        {
            RaiseEvent(GameEvents.OnBeginRound);
            QueueStep(new DynastyPhase(this));
            QueueStep(new DrawPhase(this));
            QueueStep(new ConflictPhase(this));
            QueueStep(new FatePhase(this));
            QueueStep(new EndRoundPrompt(this));
            QueueStep(new SimpleStep(this, RoundEnded));
            QueueStep(new SimpleStep(this, BeginRound));
            return true;
        }

        private bool RoundEnded()
        {
            RaiseEvent(GameEvents.OnRoundEnded);
            return true;
        }

        // Pipeline management
        public T QueueStep<T>(T step) where T : IGameStep
        {
            pipeline.QueueStep(step);
            return step;
        }



        public void MarkActionAsTaken()
        {
            if (currentActionWindow != null)
            {
                currentActionWindow.MarkActionAsTaken();
            }
        }

        // Ability resolution
        public AbilityResolver ResolveAbility(AbilityContext context)
        {
            var resolver = new AbilityResolver(this, context);
            QueueStep(resolver);
            return resolver;
        }

        public void OpenSimultaneousEffectWindow(List<EffectChoice> choices)
        {
            var window = new SimultaneousEffectWindow(this);
            foreach (var choice in choices)
            {
                window.AddChoice(choice);
            }
            QueueStep(window);
        }

        // Events
        public GameEvent GetEvent(string eventName, Dictionary<string, object> parameters, System.Func<bool> handler)
        {
            return new GameEvent(eventName, parameters, handler);
        }

        public GameEvent RaiseEvent(string eventName, Dictionary<string, object> parameters = null, System.Func<bool> handler = null)
        {
            if (parameters == null) parameters = new Dictionary<string, object>();
            if (handler == null) handler = () => true;

            var gameEvent = GetEvent(eventName, parameters, handler);
            OpenEventWindow(new List<GameEvent> { gameEvent });
            return gameEvent;
        }



        public EventWindow OpenEventWindow(List<GameEvent> events)
        {
            return QueueStep(new EventWindow(this, events));
        }

        public ThenEventWindow OpenThenEventWindow(List<GameEvent> events)
        {
            if (currentEventWindow != null)
            {
                return QueueStep(new ThenEventWindow(this, events));
            }
            return (ThenEventWindow)OpenEventWindow(events);
        }

        public void RaiseInitiateAbilityEvent(Dictionary<string, object> parameters, System.Func<bool> handler)
        {
            RaiseMultipleInitiateAbilityEvents(new List<InitiateAbilityEventProps>
            {
                new InitiateAbilityEventProps { parameters = parameters, handler = handler }
            });
        }

        public void RaiseMultipleInitiateAbilityEvents(List<InitiateAbilityEventProps> eventProps)
        {
            var events = eventProps.Select(eventProp => 
                new InitiateCardAbilityEvent(eventProp.parameters, eventProp.handler)).ToList();
            QueueStep(new InitiateAbilityEventWindow(this, events));
        }





        public void InitiateConflict(Player player, bool canPass, string forcedDeclaredType)
        {
            currentConflict = new Conflict(this, player, player.opponent, null, null, forcedDeclaredType);
            QueueStep(new ConflictFlow(this, currentConflict, canPass));
        }

        public void TakeControl(Player player, BaseCard card)
        {
            if (card.controller == player || !card.CheckRestrictions("TakeControl", GetFrameworkContext()))
            {
                return;
            }

            card.controller.RemoveCardFromPile(card);
            player.cardsInPlay.Add(card);
            card.controller = player;

            if (card.IsParticipating())
            {
                currentConflict.RemoveFromConflict(card);
                if (player.IsAttackingPlayer())
                {
                    currentConflict.AddAttacker(card);
                }
                else
                {
                    currentConflict.AddDefender(card);
                }
            }

            card.UpdateEffectContexts();
            CheckGameState(true);
        }

        // Network methods
        public bool Watch(string socketId, UserInfo user)
        {
            if (!allowSpectators)
                return false;

            var spectatorGO = new GameObject($"Spectator_{user.username}");
            spectatorGO.transform.SetParent(transform);
            var spectator = spectatorGO.AddComponent<Spectator>();
            spectator.Initialize(socketId, user);
            
            playersAndSpectators[user.username] = spectator;
            AddMessage("{0} has joined the game as a spectator", user.username);

            return true;
        }

        public bool Join(string socketId, UserInfo user)
        {
            if (started || GetPlayers().Count >= 2)
                return false;

            var playerGO = new GameObject($"Player_{user.username}");
            playerGO.transform.SetParent(transform);
            var player = playerGO.AddComponent<Player>();
            player.Initialize(socketId, user, owner == user.username, this, new ClockSettings());
            
            playersAndSpectators[user.username] = player;

            return true;
        }

        public bool IsEmpty()
        {
            return playersAndSpectators.Values.All(player => 
                player.disconnected || player.left || player.id == "TBA");
        }

        public void Leave(string playerName)
        {
            if (!playersAndSpectators.ContainsKey(playerName))
                return;

            var player = playersAndSpectators[playerName];
            AddMessage("{0} has left the game", playerName);

            if (IsSpectator(player) || !started)
            {
                playersAndSpectators.Remove(playerName);
            }
            else
            {
                player.left = true;
                if (finishedAt == default(DateTime))
                {
                    finishedAt = DateTime.Now;
                }
            }
        }

        public void Disconnect(string playerName)
        {
            if (!playersAndSpectators.ContainsKey(playerName))
                return;

            var player = playersAndSpectators[playerName];
            AddMessage("{0} has disconnected", player);

            if (IsSpectator(player))
            {
                playersAndSpectators.Remove(playerName);
            }
            else
            {
                player.disconnected = true;
            }

            player.socket = null;
        }

        public void FailedConnect(string playerName)
        {
            if (!playersAndSpectators.ContainsKey(playerName))
                return;

            var player = playersAndSpectators[playerName];

            if (IsSpectator(player) || !started)
            {
                playersAndSpectators.Remove(playerName);
            }
            else
            {
                AddMessage("{0} has failed to connect to the game", player);
                player.disconnected = true;

                if (finishedAt == default(DateTime))
                {
                    finishedAt = DateTime.Now;
                }
            }
        }

        public void Reconnect(object socket, string playerName)
        {
            var player = GetPlayerByName(playerName);
            if (player == null)
                return;

            player.id = socket.ToString(); // Simplified socket handling
            player.socket = socket;
            player.disconnected = false;

            AddMessage("{0} has reconnected", player);
        }

        public void CheckGameState(bool hasChanged = false, List<GameEvent> events = null)
        {
            if (events == null) events = new List<GameEvent>();
            
            bool stateChanged = hasChanged;

            // Check effects
            if (currentConflict == null)
            {
                stateChanged = effectEngine.CheckEffects(hasChanged) || stateChanged;
            }
            else
            {
                stateChanged = currentConflict.CalculateSkill(hasChanged) || stateChanged;
            }

            if (stateChanged)
            {
                CheckWinCondition();

                // Check for illegal card states
                foreach (var player in GetPlayers())
                {
                    foreach (var card in player.cardsInPlay)
                    {
                        if (card.GetModifiedController() != player)
                        {
                            TakeControl(card.GetModifiedController(), card);
                        }
                        card.CheckForIllegalAttachments();
                    }

                    foreach (var province in player.GetProvinces())
                    {
                        province?.CheckForIllegalAttachments();
                    }

                    if (!player.CheckRestrictions("haveImperialFavor") && !string.IsNullOrEmpty(player.imperialFavor))
                    {
                        AddMessage("The imperial favor is discarded as {0} cannot have it", player.name);
                        player.LoseImperialFavor();
                    }
                }

                currentConflict?.CheckForIllegalParticipants();
                OnGameStateChanged?.Invoke();
            }

            if (events.Count > 0)
            {
                effectEngine.CheckDelayedEffects(events);
            }
        }

        public void Continue()
        {
            pipeline.Continue();
        }

        // Save state
        public GameSaveState GetSaveState()
        {
            var players = GetPlayers().Select(player => new PlayerSaveState
            {
                name = player.name,
                faction = player.faction?.name ?? player.faction?.value ?? "",
                honor = player.GetTotalHonor()
            }).ToList();

            return new GameSaveState
            {
                id = savedGameId,
                gameId = gameId,
                startedAt = startedAt,
                players = players,
                winner = winner?.name,
                winReason = winReason,
                finishedAt = finishedAt
            };
        }

        // Client state
        public GameState GetState(string activePlayerName)
        {
            var activePlayer = playersAndSpectators.ContainsKey(activePlayerName) 
                ? playersAndSpectators[activePlayerName] 
                : new AnonymousSpectator();

            var state = new GameState
            {
                id = gameId,
                manualMode = manualMode,
                name = gameName,
                owner = owner,
                phase = currentPhase,
                started = started,
                winner = winner?.name,
                messages = gameChat.messages.ToList()
            };

            if (started)
            {
                foreach (var player in GetPlayers())
                {
                    state.players[player.name] = player.GetState(activePlayer);
                }

                foreach (var ring in rings)
                {
                    state.rings[ring.Key] = ring.Value.GetState(activePlayer);
                }

                if (currentPhase == GamePhases.Conflict && currentConflict != null)
                {
                    state.conflict = currentConflict.GetSummary();
                }

                state.spectators = GetSpectators().Select(s => new SpectatorInfo
                {
                    id = s.id,
                    name = s.name
                }).ToList();
            }

            return state;
        }

        public GameSummary GetSummary(string activePlayerName)
        {
            var playerSummaries = new Dictionary<string, PlayerSummary>();

            foreach (var player in GetPlayers())
            {
                if (player.left) continue;

                DeckInfo deck;
                if (activePlayerName == player.name && player.deck != null)
                {
                    deck = new DeckInfo { name = player.deck.name, selected = player.deck.selected };
                }
                else if (player.deck != null)
                {
                    deck = new DeckInfo { selected = player.deck.selected };
                }
                else
                {
                    deck = new DeckInfo();
                }

                playerSummaries[player.name] = new PlayerSummary
                {
                    deck = deck,
                    emailHash = player.emailHash,
                    faction = player.faction?.value ?? "",
                    id = player.id,
                    lobbyId = player.lobbyId,
                    left = player.left,
                    name = player.name,
                    owner = player.owner
                };
            }

            return new GameSummary
            {
                allowSpectators = allowSpectators,
                createdAt = createdAt,
                gameType = gameType,
                id = gameId,
                manualMode = manualMode,
                messages = gameChat.messages.ToList(),
                name = gameName,
                owner = owner, // Note: In the original, this was filtered to remove sensitive data
                players = playerSummaries,
                started = started,
                startedAt = startedAt,
                spectators = GetSpectators().Select(spectator => new SpectatorInfo
                {
                    id = spectator.id,
                    lobbyId = spectator.lobbyId,
                    name = spectator.name
                }).ToList()
            };
        }

        // IronPython Hot Reload (Development only)
#if UNITY_EDITOR
        private void Update()
        {
            if (debugPython && Input.GetKeyDown(KeyCode.F5))
            {
                ReloadAllCardScripts();
                Debug.Log("🐍 Hot-reloaded all Python card scripts!");
            }
        }
#endif

        public void ReloadAllCardScripts()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!enablePythonScripting) return;
            
            cardScripts.Clear();
            LoadCardScripts();
#endif
        }

        public void ReloadCardScript(string scriptName)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!enablePythonScripting) return;
            
            string scriptsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "CardScripts");
            string filePath = System.IO.Path.Combine(scriptsPath, scriptName + ".py");
            
            if (System.IO.File.Exists(filePath))
            {
                LoadCardScript(filePath);
            }
#endif
        }
    }

    // Supporting classes and enums
    public static class GamePhases
    {
        public const string Setup = "setup";
        public const string Dynasty = "dynasty";
        public const string Draw = "draw";
        public const string Conflict = "conflict";
        public const string Fate = "fate";
    }

    public static class GameEvents
    {
        public const string OnBeginRound = "onBeginRound";
        public const string OnRoundEnded = "onRoundEnded";
    }

    public enum ConflictType
    {
        Military,
        Political
    }

    [Serializable]
    public class GameState
    {
        public string id;
        public bool manualMode;
        public string name;
        public string owner;
        public string phase;
        public bool started;
        public string winner;
        public List<string> messages = new List<string>();
        public Dictionary<string, object> players = new Dictionary<string, object>();
        public Dictionary<string, object> rings = new Dictionary<string, object>();
        public object conflict;
        public List<SpectatorInfo> spectators = new List<SpectatorInfo>();
    }

    [Serializable]
    public class GameSummary
    {
        public bool allowSpectators;
        public DateTime createdAt;
        public string gameType;
        public string id;
        public bool manualMode;
        public List<string> messages;
        public string name;
        public string owner;
        public Dictionary<string, PlayerSummary> players;
        public bool started;
        public DateTime startedAt;
        public List<SpectatorInfo> spectators;
    }

    [Serializable]
    public class GameSaveState
    {
        public string id;
        public string gameId;
        public DateTime startedAt;
        public List<PlayerSaveState> players;
        public string winner;
        public string winReason;
        public DateTime finishedAt;
    }

    [Serializable]
    public class PlayerSaveState
    {
        public string name;
        public string faction;
        public int honor;
    }

    [Serializable]
    public class PlayerSummary
    {
        public DeckInfo deck;
        public string emailHash;
        public string faction;
        public string id;
        public string lobbyId;
        public bool left;
        public string name;
        public bool owner;
    }

    [Serializable]
    public class DeckInfo
    {
        public string name;
        public bool selected;
    }

    [Serializable]
    public class SpectatorInfo
    {
        public string id;
        public string name;
        public string lobbyId;
    }

    // Note: MenuCommand class moved to separate MenuCommand.cs file

    [Serializable]
    public class InitiateAbilityEventProps
    {
        public Dictionary<string, object> parameters;
        public System.Func<bool> handler;
    }

    // Interface for game router (will be implemented by network layer)
    public interface IGameRouter
    {
        void HandleError(Game game, Exception error);
        void GameWon(Game game, string reason, Player winner);
    }
}