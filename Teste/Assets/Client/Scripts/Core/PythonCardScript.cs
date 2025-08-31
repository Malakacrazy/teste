using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Collections;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting;
using System.Text;

namespace L5RGame
{
    /// <summary>
    /// PythonManager handles all IronPython integration for the L5R card game,
    /// including dynamic card scripts, phase scripts, and runtime Python execution.
    /// Provides hot-reload capabilities, error handling, and script caching.
    /// </summary>
    public class PythonManager : MonoBehaviour
    {
        [Header("Python Engine Settings")]
        public bool enablePythonScripting = true;
        public bool enableHotReload = true;
        public bool enableScriptCaching = true;
        public bool enableDebugLogging = false;
        public float scriptTimeout = 5f;

        [Header("Script Directories")]
        public string cardScriptsPath = "StreamingAssets/Scripts/Cards";
        public string phaseScriptsPath = "StreamingAssets/Scripts/Phases";
        public string utilityScriptsPath = "StreamingAssets/Scripts/Utilities";
        public string gameActionsPath = "StreamingAssets/Scripts/GameActions";

        [Header("Performance Settings")]
        public int maxCachedScripts = 100;
        public float cacheCleanupInterval = 300f; // 5 minutes
        public bool preloadCommonScripts = true;

        // Python engine and scope
        private ScriptEngine pythonEngine;
        private ScriptScope globalScope;
        private ScriptScope gameScope;

        // Script caching
        private Dictionary<string, CompiledCode> compiledScriptCache = new Dictionary<string, CompiledCode>();
        private Dictionary<string, DateTime> scriptLastModified = new Dictionary<string, DateTime>();
        private Dictionary<string, ScriptScope> scriptScopes = new Dictionary<string, ScriptScope>();

        // Error handling
        private Dictionary<string, int> scriptErrorCounts = new Dictionary<string, int>();
        private const int MAX_SCRIPT_ERRORS = 5;

        // Hot reload
        private FileSystemWatcher cardScriptWatcher;
        private FileSystemWatcher phaseScriptWatcher;
        private HashSet<string> scriptsToReload = new HashSet<string>();

        // Game context
        private Game gameInstance;
        private Dictionary<string, object> gameContext = new Dictionary<string, object>();

        // Events
        public System.Action<string> OnScriptLoaded;
        public System.Action<string> OnScriptReloaded;
        public System.Action<string, Exception> OnScriptError;
        public System.Action<string> OnScriptCacheCleared;

        #region Unity Lifecycle
        private void Awake()
        {
            if (enablePythonScripting)
            {
                InitializePythonEngine();
                SetupDirectories();
                if (enableHotReload)
                {
                    SetupFileWatchers();
                }
            }
        }

        private void Start()
        {
            if (enablePythonScripting)
            {
                LoadUtilityScripts();
                if (preloadCommonScripts)
                {
                    PreloadCommonScripts();
                }
                
                InvokeRepeating(nameof(CleanupCache), cacheCleanupInterval, cacheCleanupInterval);
            }
        }

        private void Update()
        {
            if (enableHotReload && scriptsToReload.Count > 0)
            {
                ProcessScriptReloads();
            }

            // Handle F5 for manual reload in editor
            #if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.F5))
            {
                ReloadAllScripts();
            }
            #endif
        }

        private void OnDestroy()
        {
            CleanupPythonEngine();
            CleanupFileWatchers();
        }
        #endregion

        #region Python Engine Initialization
        private void InitializePythonEngine()
        {
            try
            {
                LogDebug("Initializing IronPython engine...");
                
                pythonEngine = Python.CreateEngine();
                globalScope = pythonEngine.CreateScope();
                gameScope = pythonEngine.CreateScope();

                // Set up Python paths
                var searchPaths = pythonEngine.GetSearchPaths();
                searchPaths.Add(Path.Combine(Application.streamingAssetsPath, "Scripts"));
                searchPaths.Add(Path.Combine(Application.streamingAssetsPath, "Scripts", "Utilities"));
                pythonEngine.SetSearchPaths(searchPaths);

                // Initialize global variables
                SetupGlobalVariables();
                
                LogDebug("IronPython engine initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize IronPython engine: {ex.Message}");
                enablePythonScripting = false;
            }
        }

        private void SetupGlobalVariables()
        {
            // Add Unity and game references to Python scope
            globalScope.SetVariable("Unity", typeof(UnityEngine.Object));
            globalScope.SetVariable("Debug", typeof(Debug));
            globalScope.SetVariable("Time", typeof(Time));
            globalScope.SetVariable("Application", typeof(Application));
            
            // Add game-specific imports
            globalScope.SetVariable("CardTypes", typeof(CardTypes));
            globalScope.SetVariable("Locations", typeof(Locations));
            globalScope.SetVariable("GamePhases", typeof(GamePhases));
            globalScope.SetVariable("EffectNames", typeof(EffectNames));
            
            // Execute initialization script
            ExecuteInitializationScript();
        }

        private void ExecuteInitializationScript()
        {
            string initScript = @"
import clr
import sys

# Add Unity assemblies
clr.AddReference('UnityEngine')
clr.AddReference('UnityEngine.CoreModule')

# Add game assemblies  
clr.AddReference('Assembly-CSharp')

# Import commonly used Unity types
from UnityEngine import Debug, Time, Application, Vector3, Quaternion
from System import String, Int32, Single, Boolean
from System.Collections.Generic import List, Dictionary

# Helper functions
def log(message):
    Debug.Log(str(message))

def log_warning(message):
    Debug.LogWarning(str(message))

def log_error(message):
    Debug.LogError(str(message))

# Game context helpers
game_context = {}

def get_game():
    return game_context.get('game', None)

def get_player(name):
    game = get_game()
    if game:
        return game.GetPlayerByName(name)
    return None

def get_card(card_id):
    game = get_game()
    if game:
        return game.GetCardById(card_id)
    return None
";

            try
            {
                pythonEngine.Execute(initScript, globalScope);
                LogDebug("Python initialization script executed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing Python initialization script: {ex.Message}");
            }
        }
        #endregion

        #region Directory and File Management
        private void SetupDirectories()
        {
            var directories = new[]
            {
                cardScriptsPath,
                phaseScriptsPath,
                utilityScriptsPath,
                gameActionsPath
            };

            foreach (var dir in directories)
            {
                var fullPath = Path.Combine(Application.streamingAssetsPath, dir.Replace("StreamingAssets/", ""));
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                    LogDebug($"Created directory: {fullPath}");
                }
            }

            CreateExampleScripts();
        }

        private void CreateExampleScripts()
        {
            CreateExampleCardScript();
            CreateExamplePhaseScript();
            CreateExampleUtilityScript();
        }

        private void CreateExampleCardScript()
        {
            var cardScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards", "example_character.py");
            
            if (!File.Exists(cardScriptPath))
            {
                string exampleScript = @"# Example Character Card Script
# This script shows common patterns for character cards

def on_enter_play(card, controller):
    """"""Called when this character enters play""""""
    log(f""{card.name} enters play under {controller.name}'s control"")
    
    # Example: Gain fate when played
    controller.fate += 1
    log(f""{controller.name} gains 1 fate from {card.name}"")

def on_leaves_play(card):
    """"""Called when this character leaves play""""""
    log(f""{card.name} leaves play"")
    
    # Example: Return fate to owner
    if card.fate > 0:
        card.controller.fate += card.fate
        log(f""{card.controller.name} recovers {card.fate} fate"")

def on_conflict(card, conflict):
    """"""Called when this character participates in a conflict""""""
    if conflict.attacking_player == card.controller:
        # Example: Bonus skill when attacking
        card.military_skill += 1
        log(f""{card.name} gains +1 military skill while attacking"")

def on_bowed(card):
    """"""Called when this character is bowed""""""
    log(f""{card.name} is bowed"")

def on_readied(card):
    """"""Called when this character is readied""""""
    log(f""{card.name} is readied"")

def on_honor_status_changed(card, is_honored, is_dishonored):
    """"""Called when this character's honor status changes""""""
    if is_honored:
        log(f""{card.name} becomes honored"")
        card.ready()  # Example: Ready when honored
    elif is_dishonored:
        log(f""{card.name} becomes dishonored"")
    else:
        log(f""{card.name} becomes ordinary"")

def can_trigger(card, event_name, event_data):
    """"""Determines if this card can trigger its ability""""""
    # Example: Can only trigger during conflicts
    game = get_game()
    return game and game.current_phase == 'conflict'

def on_trigger(card, event_name, event_data):
    """"""Executes this card's triggered ability""""""
    log(f""{card.name} triggers its ability!"")
    
    # Example triggered effect
    card.controller.draw_cards(1)
    log(f""{card.controller.name} draws 1 card"")
";

                File.WriteAllText(cardScriptPath, exampleScript);
                LogDebug("Created example card script");
            }
        }

        private void CreateExamplePhaseScript()
        {
            var phaseScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases", "dynasty_phase.py");
            
            if (!File.Exists(phaseScriptPath))
            {
                string exampleScript = @"# Dynasty Phase Script
# Handles dynasty phase events and modifications

def on_dynasty_phase_start():
    """"""Called when dynasty phase begins""""""
    log(""Dynasty phase begins"")

def on_dynasty_phase_began():
    """"""Called after dynasty phase setup is complete""""""
    log(""Dynasty phase setup complete"")

def on_dynasty_cards_reveal_start():
    """"""Called before dynasty cards are revealed""""""
    log(""Revealing dynasty cards..."")

def on_dynasty_card_revealed(card, player, province_location):
    """"""Called when each dynasty card is revealed""""""
    log(f""{player.name} reveals {card.name} in {province_location}"")

def on_fate_collection_start():
    """"""Called before fate collection""""""
    log(""Collecting fate from strongholds..."")

def on_player_fate_collected(player, fate_amount, old_fate, new_fate):
    """"""Called when a player collects fate""""""
    log(f""{player.name} collects {fate_amount} fate"")

def on_dynasty_action_window_start():
    """"""Called when dynasty action window opens""""""
    log(""Dynasty action window opens"")

def on_card_played_from_province(player, card, province_location, cost):
    """"""Called when a card is played from a province""""""
    log(f""{player.name} plays {card.name} from {province_location} for {cost} fate"")

def on_dynasty_phase_end():
    """"""Called when dynasty phase ends""""""
    log(""Dynasty phase ends"")
";

                File.WriteAllText(phaseScriptPath, exampleScript);
                LogDebug("Created example phase script");
            }
        }

        private void CreateExampleUtilityScript()
        {
            var utilityScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Utilities", "game_helpers.py");
            
            if (!File.Exists(utilityScriptPath))
            {
                string exampleScript = @"# Game Helper Utilities
# Common functions used across card and phase scripts

def find_characters_with_trait(player, trait):
    """"""Find all characters a player controls with a specific trait""""""
    characters = []
    for card in player.get_characters_in_play():
        if card.has_trait(trait):
            characters.append(card)
    return characters

def count_cards_in_play_with_cost(player, cost):
    """"""Count cards in play with specific cost""""""
    count = 0
    for card in player.get_cards_in_play():
        if hasattr(card, 'get_cost') and card.get_cost() == cost:
            count += 1
    return count

def get_total_skill_in_conflict(conflict, skill_type):
    """"""Calculate total skill for a conflict type""""""
    attacking_skill = sum(card.get_skill(skill_type) for card in conflict.attackers)
    defending_skill = sum(card.get_skill(skill_type) for card in conflict.defenders)
    return attacking_skill, defending_skill

def find_province_with_most_strength(player):
    """"""Find the player's province with the highest strength""""""
    strongest_province = None
    max_strength = -1
    
    for province in player.get_provinces():
        if not province.is_broken and province.get_strength() > max_strength:
            max_strength = province.get_strength()
            strongest_province = province
            
    return strongest_province

def apply_effect_to_matching_cards(player, card_filter, effect_function):
    """"""Apply an effect to all cards matching a filter""""""
    matching_cards = [card for card in player.get_cards_in_play() if card_filter(card)]
    for card in matching_cards:
        effect_function(card)
    return len(matching_cards)

# Combat helpers
def get_conflict_winner(conflict):
    """"""Determine the winner of a conflict""""""
    attacking_skill, defending_skill = get_total_skill_in_conflict(conflict, conflict.conflict_type)
    
    if attacking_skill > defending_skill:
        return conflict.attacking_player
    elif defending_skill > attacking_skill:
        return conflict.defending_player
    else:
        return None  # Tie

# Resource helpers
def can_afford_card(player, card):
    """"""Check if player can afford a card""""""
    if hasattr(card, 'get_cost'):
        return player.fate >= card.get_cost()
    return True

def get_cheapest_card_in_hand(player):
    """"""Get the cheapest card in player's hand""""""
    hand = player.get_conflict_hand()
    if not hand:
        return None
        
    cheapest = hand[0]
    for card in hand[1:]:
        if hasattr(card, 'get_cost') and hasattr(cheapest, 'get_cost'):
            if card.get_cost() < cheapest.get_cost():
                cheapest = card
    return cheapest
";

                File.WriteAllText(utilityScriptPath, exampleScript);
                LogDebug("Created example utility script");
            }
        }
        #endregion

        #region Hot Reload System
        private void SetupFileWatchers()
        {
            try
            {
                var cardScriptsFullPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards");
                var phaseScriptsFullPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases");

                if (Directory.Exists(cardScriptsFullPath))
                {
                    cardScriptWatcher = new FileSystemWatcher(cardScriptsFullPath, "*.py");
                    cardScriptWatcher.Changed += OnScriptFileChanged;
                    cardScriptWatcher.Created += OnScriptFileChanged;
                    cardScriptWatcher.EnableRaisingEvents = true;
                }

                if (Directory.Exists(phaseScriptsFullPath))
                {
                    phaseScriptWatcher = new FileSystemWatcher(phaseScriptsFullPath, "*.py");
                    phaseScriptWatcher.Changed += OnScriptFileChanged;
                    phaseScriptWatcher.Created += OnScriptFileChanged;
                    phaseScriptWatcher.EnableRaisingEvents = true;
                }

                LogDebug("File watchers setup for hot reload");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to setup file watchers: {ex.Message}");
            }
        }

        private void OnScriptFileChanged(object sender, FileSystemEventArgs e)
        {
            scriptsToReload.Add(e.FullPath);
            LogDebug($"Script marked for reload: {e.Name}");
        }

        private void ProcessScriptReloads()
        {
            var scriptsToProcess = new List<string>(scriptsToReload);
            scriptsToReload.Clear();

            foreach (var scriptPath in scriptsToProcess)
            {
                try
                {
                    var scriptName = Path.GetFileName(scriptPath);
                    ReloadScript(scriptName);
                    OnScriptReloaded?.Invoke(scriptName);
                    LogDebug($"Hot reloaded script: {scriptName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to hot reload script {scriptPath}: {ex.Message}");
                }
            }
        }

        public void ReloadAllScripts()
        {
            LogDebug("Reloading all scripts...");
            
            compiledScriptCache.Clear();
            scriptScopes.Clear();
            scriptLastModified.Clear();
            
            LoadUtilityScripts();
            OnScriptCacheCleared?.Invoke("All scripts reloaded");
            
            LogDebug("All scripts reloaded");
        }

        private void ReloadScript(string scriptName)
        {
            if (compiledScriptCache.ContainsKey(scriptName))
            {
                compiledScriptCache.Remove(scriptName);
            }
            
            if (scriptScopes.ContainsKey(scriptName))
            {
                scriptScopes.Remove(scriptName);
            }
            
            if (scriptLastModified.ContainsKey(scriptName))
            {
                scriptLastModified.Remove(scriptName);
            }
        }
        #endregion

        #region Script Loading and Execution
        private void LoadUtilityScripts()
        {
            var utilityPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Utilities");
            if (Directory.Exists(utilityPath))
            {
                foreach (var scriptFile in Directory.GetFiles(utilityPath, "*.py"))
                {
                    LoadAndExecuteScript(scriptFile, globalScope);
                }
            }
        }

        private void PreloadCommonScripts()
        {
            var commonScripts = new[]
            {
                "dynasty_phase.py",
                "draw_phase.py", 
                "conflict_phase.py",
                "fate_phase.py",
                "setup_phase.py"
            };

            foreach (var scriptName in commonScripts)
            {
                PreloadScript(scriptName, "Phases");
            }
        }

        private void PreloadScript(string scriptName, string subfolder)
        {
            var scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", subfolder, scriptName);
            if (File.Exists(scriptPath))
            {
                try
                {
                    CompileScript(scriptName, scriptPath);
                    LogDebug($"Preloaded script: {scriptName}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to preload script {scriptName}: {ex.Message}");
                }
            }
        }

        private CompiledCode CompileScript(string scriptName, string scriptPath)
        {
            if (enableScriptCaching && compiledScriptCache.ContainsKey(scriptName))
            {
                var lastModified = File.GetLastWriteTime(scriptPath);
                if (scriptLastModified.ContainsKey(scriptName) && 
                    scriptLastModified[scriptName] >= lastModified)
                {
                    return compiledScriptCache[scriptName];
                }
            }

            var scriptSource = File.ReadAllText(scriptPath);
            var compiled = pythonEngine.CreateScriptSourceFromString(scriptSource, scriptPath).Compile();
            
            if (enableScriptCaching)
            {
                compiledScriptCache[scriptName] = compiled;
                scriptLastModified[scriptName] = File.GetLastWriteTime(scriptPath);
                
                // Cleanup old cache entries if needed
                if (compiledScriptCache.Count > maxCachedScripts)
                {
                    CleanupOldestCacheEntry();
                }
            }

            return compiled;
        }

        private void LoadAndExecuteScript(string scriptPath, ScriptScope scope)
        {
            try
            {
                var scriptName = Path.GetFileName(scriptPath);
                var compiled = CompileScript(scriptName, scriptPath);
                compiled.Execute(scope);
                
                OnScriptLoaded?.Invoke(scriptName);
                LogDebug($"Loaded and executed script: {scriptName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading script {scriptPath}: {ex.Message}");
                OnScriptError?.Invoke(scriptPath, ex);
            }
        }
        #endregion

        #region Public Script Execution API
        public void SetGameContext(Game game)
        {
            gameInstance = game;
            gameContext["game"] = game;
            
            if (globalScope != null)
            {
                globalScope.SetVariable("game_context", gameContext);
            }
        }

        public bool ExecuteCardScript(string cardId, string methodName, params object[] parameters)
        {
            if (!enablePythonScripting) return false;

            var scriptName = $"{cardId}.py";
            var scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards", scriptName);
            
            return ExecuteScriptMethod(scriptPath, scriptName, methodName, parameters);
        }

        public bool ExecutePhaseScript(string phaseName, string methodName, params object[] parameters)
        {
            if (!enablePythonScripting) return false;

            var scriptName = $"{phaseName}.py";
            var scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases", scriptName);
            
            return ExecuteScriptMethod(scriptPath, scriptName, methodName, parameters);
        }

        public T ExecuteCardFunction<T>(string cardId, string functionName, params object[] parameters)
        {
            if (!enablePythonScripting) return default(T);

            var scriptName = $"{cardId}.py";
            var scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards", scriptName);
            
            return ExecuteScriptFunction<T>(scriptPath, scriptName, functionName, parameters);
        }

        public T ExecutePhaseFunction<T>(string phaseName, string functionName, params object[] parameters)
        {
            if (!enablePythonScripting) return default(T);

            var scriptName = $"{phaseName}.py";
            var scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases", scriptName);
            
            return ExecuteScriptFunction<T>(scriptPath, scriptName, functionName, parameters);
        }

        private bool ExecuteScriptMethod(string scriptPath, string scriptName, string methodName, params object[] parameters)
        {
            if (!File.Exists(scriptPath)) return false;

            try
            {
                var scope = GetOrCreateScriptScope(scriptName, scriptPath);
                
                if (scope.ContainsVariable(methodName))
                {
                    var method = scope.GetVariable(methodName);
                    var operation = pythonEngine.Operations;
                    
                    StartCoroutine(ExecuteWithTimeout(() => operation.Invoke(method, parameters)));
                    return true;
                }
                else
                {
                    LogDebug($"Method {methodName} not found in script {scriptName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                HandleScriptError(scriptName, ex);
                return false;
            }
        }

        private T ExecuteScriptFunction<T>(string scriptPath, string scriptName, string functionName, params object[] parameters)
        {
            if (!File.Exists(scriptPath)) return default(T);

            try
            {
                var scope = GetOrCreateScriptScope(scriptName, scriptPath);
                
                if (scope.ContainsVariable(functionName))
                {
                    var function = scope.GetVariable(functionName);
                    var operation = pythonEngine.Operations;
                    
                    var result = operation.Invoke(function, parameters);
                    return operation.ConvertTo<T>(result);
                }
                else
                {
                    LogDebug($"Function {functionName} not found in script {scriptName}");
                    return default(T);
                }
            }
            catch (Exception ex)
            {
                HandleScriptError(scriptName, ex);
                return default(T);
            }
        }

        private ScriptScope GetOrCreateScriptScope(string scriptName, string scriptPath)
        {
            if (scriptScopes.ContainsKey(scriptName))
            {
                return scriptScopes[scriptName];
            }

            var scope = pythonEngine.CreateScope();
            
            // Import global utilities
            scope.SetVariable("log", globalScope.GetVariable("log"));
            scope.SetVariable("log_warning", globalScope.GetVariable("log_warning"));
            scope.SetVariable("log_error", globalScope.GetVariable("log_error"));
            scope.SetVariable("get_game", globalScope.GetVariable("get_game"));
            scope.SetVariable("get_player", globalScope.GetVariable("get_player"));
            scope.SetVariable("get_card", globalScope.GetVariable("get_card"));
            scope.SetVariable("game_context", gameContext);

            // Execute the script to define its functions
            var compiled = CompileScript(scriptName, scriptPath);
            compiled.Execute(scope);
            
            scriptScopes[scriptName] = scope;
            return scope;
        }

        private IEnumerator ExecuteWithTimeout(System.Action action)
        {
            var startTime = Time.time;
            var completed = false;
            
            try
            {
                action.Invoke();
                completed = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Script execution error: {ex.Message}");
                completed = true;
            }

            yield return new WaitUntil(() => completed || (Time.time - startTime) > scriptTimeout);
            
            if (!completed)
            {
                Debug.LogWarning($"Script execution timed out after {scriptTimeout} seconds");
            }
        }
        #endregion

        #region Error Handling
        private void HandleScriptError(string scriptName, Exception ex)
        {
            if (!scriptErrorCounts.ContainsKey(scriptName))
            {
                scriptErrorCounts[scriptName] = 0;
            }
            
            scriptErrorCounts[scriptName]++;
            
            if (scriptErrorCounts[scriptName] > MAX_SCRIPT_ERRORS)
            {
                Debug.LogError($"Script {scriptName} has exceeded maximum error count ({MAX_SCRIPT_ERRORS}). Disabling script.");
                
                // Remove problematic script from cache
                if (compiledScriptCache.ContainsKey(scriptName))
                {
                    compiledScriptCache.Remove(scriptName);
                }
                if (scriptScopes.ContainsKey(scriptName))
                {
                    scriptScopes.Remove(scriptName);
                }
            }
            else
            {
                Debug.LogError($"Script error in {scriptName}: {ex.Message}");
            }
            
            OnScriptError?.Invoke(scriptName, ex);
        }
        #endregion

        #region Cache Management
        private void CleanupCache()
        {
            if (compiledScriptCache.Count <= maxCachedScripts) return;

            var oldestEntries = scriptLastModified
                .OrderBy(kvp => kvp.Value)
                .Take(compiledScriptCache.Count - maxCachedScripts)
                .ToList();

            foreach (var entry in oldestEntries)
            {
                compiledScriptCache.Remove(entry.Key);
                scriptLastModified.Remove(entry.Key);
                if (scriptScopes.ContainsKey(entry.Key))
                {
                    scriptScopes.Remove(entry.Key);
                }
            }

            LogDebug($"Cleaned up {oldestEntries.Count} old cache entries");
        }

        private void CleanupOldestCacheEntry()
        {
            if (scriptLastModified.Count == 0) return;

            var oldest = scriptLastModified.OrderBy(kvp => kvp.Value).First();
            compiledScriptCache.Remove(oldest.Key);
            scriptLastModified.Remove(oldest.Key);
            if (scriptScopes.ContainsKey(oldest.Key))
            {
                scriptScopes.Remove(oldest.Key);
            }
        }
        #endregion

        #region Cleanup
        private void CleanupPythonEngine()
        {
            try
            {
                compiledScriptCache?.Clear();
                scriptScopes?.Clear();
                scriptLastModified?.Clear();
                
                globalScope = null;
                gameScope = null;
                pythonEngine = null;
                
                LogDebug("Python engine cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error cleaning up Python engine: {ex.Message}");
            }
        }

        private void CleanupFileWatchers()
        {
            try
            {
                if (cardScriptWatcher != null)
                {
                    cardScriptWatcher.EnableRaisingEvents = false;
                    cardScriptWatcher.Dispose();
                    cardScriptWatcher = null;
                }

                if (phaseScriptWatcher != null)
                {
                    phaseScriptWatcher.EnableRaisingEvents = false;
                    phaseScriptWatcher.Dispose();
                    phaseScriptWatcher = null;
                }
                
                LogDebug("File watchers cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error cleaning up file watchers: {ex.Message}");
            }
        }
        #endregion

        #region Public Utility Methods
        public bool IsScriptAvailable(string scriptName)
        {
            var cardScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards", scriptName);
            var phaseScriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases", scriptName);
            
            return File.Exists(cardScriptPath) || File.Exists(phaseScriptPath);
        }

        public List<string> GetAvailableCardScripts()
        {
            var cardScriptsPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards");
            if (!Directory.Exists(cardScriptsPath)) return new List<string>();
            
            return Directory.GetFiles(cardScriptsPath, "*.py")
                           .Select(Path.GetFileName)
                           .ToList();
        }

        public List<string> GetAvailablePhaseScripts()
        {
            var phaseScriptsPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases");
            if (!Directory.Exists(phaseScriptsPath)) return new List<string>();
            
            return Directory.GetFiles(phaseScriptsPath, "*.py")
                           .Select(Path.GetFileName)
                           .ToList();
        }

        public Dictionary<string, object> GetScriptVariables(string scriptName)
        {
            if (!scriptScopes.ContainsKey(scriptName)) return new Dictionary<string, object>();
            
            var variables = new Dictionary<string, object>();
            var scope = scriptScopes[scriptName];
            
            foreach (var variableName in scope.GetVariableNames())
            {
                try
                {
                    variables[variableName] = scope.GetVariable(variableName);
                }
                catch (Exception ex)
                {
                    LogDebug($"Could not get variable {variableName}: {ex.Message}");
                }
            }
            
            return variables;
        }

        public void ClearScriptCache()
        {
            compiledScriptCache.Clear();
            scriptScopes.Clear();
            scriptLastModified.Clear();
            scriptErrorCounts.Clear();
            
            OnScriptCacheCleared?.Invoke("Manual cache clear");
            LogDebug("Script cache manually cleared");
        }

        public void CreateScriptFromTemplate(string scriptName, string templateType, Dictionary<string, string> replacements = null)
        {
            var templates = new Dictionary<string, string>
            {
                ["character"] = GetCharacterTemplate(),
                ["attachment"] = GetAttachmentTemplate(),
                ["event"] = GetEventTemplate(),
                ["holding"] = GetHoldingTemplate(),
                ["province"] = GetProvinceTemplate(),
                ["stronghold"] = GetStrongholdTemplate(),
                ["role"] = GetRoleTemplate()
            };

            if (!templates.ContainsKey(templateType))
            {
                Debug.LogError($"Unknown template type: {templateType}");
                return;
            }

            var template = templates[templateType];
            
            // Apply replacements
            if (replacements != null)
            {
                foreach (var replacement in replacements)
                {
                    template = template.Replace($"{{{replacement.Key}}}", replacement.Value);
                }
            }

            var scriptPath = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards", scriptName);
            File.WriteAllText(scriptPath, template);
            
            LogDebug($"Created script {scriptName} from {templateType} template");
        }
        #endregion

        #region Script Templates
        private string GetCharacterTemplate()
        {
            return @"# {CARD_NAME} Character Script
# Generated from character template

def on_enter_play(card, controller):
    """"""Called when this character enters play""""""
    log(f""{card.name} enters play under {controller.name}'s control"")
    # Add enter play effects here

def on_leaves_play(card):
    """"""Called when this character leaves play""""""
    log(f""{card.name} leaves play"")
    # Add leave play effects here

def on_conflict(card, conflict):
    """"""Called when this character participates in a conflict""""""
    # Add conflict participation effects here
    pass

def on_bowed(card):
    """"""Called when this character is bowed""""""
    # Add bowed effects here
    pass

def on_readied(card):
    """"""Called when this character is readied""""""
    # Add readied effects here
    pass

def on_honor_status_changed(card, is_honored, is_dishonored):
    """"""Called when this character's honor status changes""""""
    if is_honored:
        # Add honored effects here
        pass
    elif is_dishonored:
        # Add dishonored effects here
        pass

def can_trigger(card, event_name, event_data):
    """"""Determines if this card can trigger its ability""""""
    # Add trigger conditions here
    return False

def on_trigger(card, event_name, event_data):
    """"""Executes this card's triggered ability""""""
    # Add triggered ability effects here
    pass
";
        }

        private string GetAttachmentTemplate()
        {
            return @"# {CARD_NAME} Attachment Script
# Generated from attachment template

def on_attach(card, parent_card):
    """"""Called when this attachment attaches to a card""""""
    log(f""{card.name} attaches to {parent_card.name}"")
    # Add attachment effects here

def on_detach(card, parent_card):
    """"""Called when this attachment detaches from a card""""""
    log(f""{card.name} detaches from {parent_card.name}"")
    # Add detachment effects here

def can_attach(card, target_card):
    """"""Determines if this attachment can attach to the target""""""
    # Add attachment restrictions here
    return True

def get_skill_bonus(card, skill_type):
    """"""Returns skill bonus this attachment provides""""""
    # Return skill bonuses here
    return 0

def on_parent_enters_conflict(card, parent_card, conflict):
    """"""Called when the attached card enters a conflict""""""
    # Add conflict effects here
    pass
";
        }

        private string GetEventTemplate()
        {
            return @"# {CARD_NAME} Event Script
# Generated from event template

def can_play(card, player, context):
    """"""Determines if this event can be played""""""
    # Add play restrictions here
    return True

def on_play(card, player, context):
    """"""Executes this event's effect when played""""""
    log(f""{player.name} plays {card.name}"")
    # Add event effects here

def get_targets(card, player, context):
    """"""Returns valid targets for this event""""""
    # Return list of valid targets
    return []

def on_target_selected(card, player, target):
    """"""Called when a target is selected for this event""""""
    # Add target selection effects here
    pass
";
        }

        private string GetHoldingTemplate()
        {
            return @"# {CARD_NAME} Holding Script
# Generated from holding template

def on_enter_play(card, province):
    """"""Called when this holding enters play""""""
    log(f""{card.name} is built in {province.name}"")
    # Add holding effects here

def on_leaves_play(card):
    """"""Called when this holding leaves play""""""
    log(f""{card.name} is destroyed"")
    # Add destruction effects here

def get_province_strength_bonus(card):
    """"""Returns the strength bonus this holding provides to its province""""""
    # Return strength bonus
    return 0

def on_province_attacked(card, conflict):
    """"""Called when the province this holding is in is attacked""""""
    # Add defense effects here
    pass

def can_trigger_ability(card):
    """"""Determines if this holding's ability can be triggered""""""
    # Add trigger conditions here
    return False

def trigger_ability(card):
    """"""Executes this holding's triggered ability""""""
    # Add ability effects here
    pass
";
        }

        private string GetProvinceTemplate()
        {
            return @"# {CARD_NAME} Province Script
# Generated from province template

def on_revealed(card):
    """"""Called when this province is revealed""""""
    log(f""{card.name} is revealed"")
    # Add reveal effects here

def on_conflict_declared(card, conflict):
    """"""Called when a conflict is declared at this province""""""
    log(f""Conflict declared at {card.name}"")
    # Add conflict declaration effects here

def on_conflict_resolved(card, won, conflict):
    """"""Called when a conflict at this province is resolved""""""
    if won:
        log(f""Conflict at {card.name} was won by defender"")
        # Add win effects here
    else:
        log(f""Conflict at {card.name} was won by attacker"")
        # Add loss effects here

def on_province_broken(card):
    """"""Called when this province is broken""""""
    log(f""{card.name} is broken!"")
    # Add broken effects here

def get_strength_bonus(card):
    """"""Returns any additional strength this province provides""""""
    # Return bonus strength
    return 0
";
        }

        private string GetStrongholdTemplate()
        {
            return @"# {CARD_NAME} Stronghold Script
# Generated from stronghold template

def on_game_setup(card, player):
    """"""Called during game setup""""""
    log(f""{player.name} sets up with {card.name}"")
    # Add setup effects here

def on_bowed(card):
    """"""Called when this stronghold is bowed""""""
    log(f""{card.name} is bowed for its ability"")
    # Add bowed ability effects here

def on_readied(card):
    """"""Called when this stronghold is readied""""""
    log(f""{card.name} readies"")
    # Add ready effects here

def can_use_ability(card):
    """"""Determines if this stronghold's ability can be used""""""
    # Add ability restrictions here
    return not card.bowed

def use_ability(card, player):
    """"""Executes this stronghold's ability""""""
    if can_use_ability(card):
        card.bow()
        # Add ability effects here
        log(f""{player.name} uses {card.name}'s ability"")

def get_bonus_fate(card):
    """"""Returns any bonus fate this stronghold provides""""""
    # Return bonus fate
    return 0
";
        }

        private string GetRoleTemplate()
        {
            return @"# {CARD_NAME} Role Script
# Generated from role template

def on_role_selected(card, player):
    """"""Called when this role is selected during setup""""""
    log(f""{player.name} chooses {card.name} as their role"")
    # Add role selection effects here

def can_play_card(card, target_card, player):
    """"""Determines if a card can be played with this role""""""
    # Add deck building restrictions here
    return True

def get_influence_discount(card, target_card):
    """"""Returns influence discount for out-of-clan cards""""""
    # Return influence discount
    return 0

def validate_deck(card, deck):
    """"""Validates a deck against this role's restrictions""""""
    errors = []
    # Add deck validation logic here
    return errors

def on_game_start(card, player):
    """"""Called when the game starts with this role""""""
    # Add game start effects here
    pass

def has_role_ability(card, ability_name):
    """"""Checks if this role has a specific ability""""""
    # Define role abilities here
    return False
";
        }
        #endregion

        #region Debug and Utility
        private void LogDebug(string message)
        {
            if (enableDebugLogging)
            {
                Debug.Log($"[PythonManager] {message}");
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogManagerStatus()
        {
            Debug.Log($"PythonManager Status:\n" +
                     $"Python Scripting Enabled: {enablePythonScripting}\n" +
                     $"Hot Reload Enabled: {enableHotReload}\n" +
                     $"Script Caching Enabled: {enableScriptCaching}\n" +
                     $"Cached Scripts: {compiledScriptCache.Count}\n" +
                     $"Script Scopes: {scriptScopes.Count}\n" +
                     $"Available Card Scripts: {GetAvailableCardScripts().Count}\n" +
                     $"Available Phase Scripts: {GetAvailablePhaseScripts().Count}");
        }

        public void LogCacheStatus()
        {
            Debug.Log($"Python Cache Status:\n" +
                     $"Compiled Scripts: {compiledScriptCache.Count}/{maxCachedScripts}\n" +
                     $"Script Scopes: {scriptScopes.Count}\n" +
                     $"Last Modified Entries: {scriptLastModified.Count}\n" +
                     $"Error Counts: {scriptErrorCounts.Count}");
        }

        public void RunDiagnostics()
        {
            Debug.Log("=== Python Manager Diagnostics ===");
            
            // Check Python engine
            Debug.Log($"Python Engine: {(pythonEngine != null ? "OK" : "NULL")}");
            Debug.Log($"Global Scope: {(globalScope != null ? "OK" : "NULL")}");
            
            // Check directories
            var cardDir = Path.Combine(Application.streamingAssetsPath, "Scripts", "Cards");
            var phaseDir = Path.Combine(Application.streamingAssetsPath, "Scripts", "Phases");
            var utilityDir = Path.Combine(Application.streamingAssetsPath, "Scripts", "Utilities");
            
            Debug.Log($"Card Scripts Directory: {(Directory.Exists(cardDir) ? "OK" : "MISSING")}");
            Debug.Log($"Phase Scripts Directory: {(Directory.Exists(phaseDir) ? "OK" : "MISSING")}");
            Debug.Log($"Utility Scripts Directory: {(Directory.Exists(utilityDir) ? "OK" : "MISSING")}");
            
            // Check file watchers
            Debug.Log($"Card Script Watcher: {(cardScriptWatcher != null ? "OK" : "NULL")}");
            Debug.Log($"Phase Script Watcher: {(phaseScriptWatcher != null ? "OK" : "NULL")}");
            
            // Test basic Python execution
            try
            {
                if (pythonEngine != null && globalScope != null)
                {
                    pythonEngine.Execute("test_result = 2 + 2", globalScope);
                    var result = globalScope.GetVariable("test_result");
                    Debug.Log($"Python Test (2+2): {result}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Python Test Failed: {ex.Message}");
            }
            
            Debug.Log("=== End Diagnostics ===");
        }
        #endregion
    }

    #region Supporting Classes
    [System.Serializable]
    public class PythonScriptInfo
    {
        public string scriptName;
        public string scriptPath;
        public DateTime lastModified;
        public int errorCount;
        public bool isLoaded;
        public List<string> availableMethods;
    }

    [System.Serializable]
    public class ScriptExecutionResult
    {
        public bool success;
        public object result;
        public string errorMessage;
        public float executionTime;
    }
    #endregion
}