using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

namespace L5RGame
{
    /// <summary>
    /// Handles chat commands for game administration and testing.
    /// Provides various utilities for manipulating game state during development and play.
    /// </summary>
    public class ChatCommands : MonoBehaviour
    {
        [Header("Command Configuration")]
        public bool enableCommands = true;
        public bool debugMode = false;
        public bool restrictToDebugMode = false;

        // Game references
        private Game game;
        private Dictionary<string, CommandHandler> commands;
        private List<string> validTokens;

        // Command delegate
        public delegate bool CommandHandler(Player player, string[] args);

        void Awake()
        {
            game = FindObjectOfType<Game>();
            InitializeCommands();
            InitializeValidTokens();
        }

        #region Command Initialization

        /// <summary>
        /// Initialize all available commands
        /// </summary>
        private void InitializeCommands()
        {
            commands = new Dictionary<string, CommandHandler>
            {
                // Card manipulation
                { "/draw", DrawCards },
                { "/discard", DiscardCards },
                { "/reveal", RevealCard },
                { "/move-to-bottom-deck", MoveCardToDeckBottom },

                // Honor/Dishonor
                { "/honor", HonorCard },
                { "/dishonor", DishonorCard },

                // Tokens and fate
                { "/token", SetToken },
                { "/add-fate", AddFate },
                { "/rem-fate", RemoveFate },
                { "/add-fate-ring", AddRingFate },
                { "/rem-fate-ring", RemoveRingFate },

                // Conflict commands
                { "/move-to-conflict", MoveToConflict },
                { "/send-home", SendHome },
                { "/duel", InitiateDuel },

                // Ring commands
                { "/claim-ring", ClaimRing },
                { "/unclaim-ring", UnclaimRing },

                // Imperial favor
                { "/claim-favor", ClaimFavor },
                { "/discard-favor", DiscardFavor },

                // Timer commands
                { "/start-clocks", StartClocks },
                { "/stop-clocks", StopClocks },
                { "/modify-clock", ModifyClock },

                // Utility commands
                { "/roll", RollDice },
                { "/manual", ToggleManualMode },
                { "/disconnectme", DisconnectPlayer },

                // Debug commands
                { "/debug-info", ShowDebugInfo },
                { "/reset-game", ResetGame },
                { "/skip-phase", SkipPhase }
            };
        }

        /// <summary>
        /// Initialize valid token types
        /// </summary>
        private void InitializeValidTokens()
        {
            validTokens = new List<string>
            {
                "fate",
                "honor",
                "military",
                "political",
                "strength"
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Execute a chat command
        /// </summary>
        public bool ExecuteCommand(Player player, string message)
        {
            if (!enableCommands || string.IsNullOrEmpty(message) || !message.StartsWith("/"))
            {
                return false;
            }

            // Check if commands are restricted to debug mode
            if (restrictToDebugMode && !Debug.isDebugBuild)
            {
                return false;
            }

            // Parse command and arguments
            string[] parts = message.Split(' ');
            string command = parts[0].ToLower();
            string[] args = parts.Skip(1).ToArray();

            if (!commands.ContainsKey(command))
            {
                if (debugMode)
                {
                    Debug.LogWarning($"⚠️ Unknown command: {command}");
                }
                return false;
            }

            try
            {
                bool result = commands[command](player, args);
                
                if (debugMode && result)
                {
                    Debug.Log($"✅ Command executed: {message} by {player.name}");
                }
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Error executing command {command}: {e.Message}");
                game.AddMessage("Error executing command: " + e.Message);
                return false;
            }
        }

        #endregion

        #region Card Manipulation Commands

        /// <summary>
        /// Draw cards to hand
        /// </summary>
        private bool DrawCards(Player player, string[] args)
        {
            int num = GetNumberOrDefault(args.ElementAtOrDefault(0), 1);
            
            game.AddMessage("{0} uses the /draw command to draw {1} cards to their hand", player.name, num);
            player.DrawCardsToHand(num);
            
            return true;
        }

        /// <summary>
        /// Discard cards at random
        /// </summary>
        private bool DiscardCards(Player player, string[] args)
        {
            int num = GetNumberOrDefault(args.ElementAtOrDefault(0), 1);
            string pluralSuffix = num > 1 ? "s" : "";
            
            game.AddMessage("{0} uses the /discard command to discard {1} card{2} at random", 
                           player.name, num, pluralSuffix);
            
            // Execute discard action
            var discardAction = game.actions.DiscardAtRandom(new Dictionary<string, object>
            {
                { "amount", num }
            });
            discardAction.Resolve(player, game.GetFrameworkContext());
            
            return true;
        }

        /// <summary>
        /// Reveal a facedown card
        /// </summary>
        private bool RevealCard(Player player, string[] args)
        {
            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card to reveal",
                cardCondition = new Func<BaseCard, bool>(card => card.facedown && card.controller == player),
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        card.facedown = false;
                        game.AddMessage("{0} reveals {1}", p.name, card.name);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);
            
            return true;
        }

        /// <summary>
        /// Move card to bottom of deck
        /// </summary>
        private bool MoveCardToDeckBottom(Player player, string[] args)
        {
            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card to send to the bottom of one of their decks",
                waitingPromptTitle = "Waiting for opponent to send a card to the bottom of one of their decks",
                location = Locations.Any,
                controller = Players.Self,
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        string cardInitialLocation = card.location;
                        string cardNewLocation = card.isConflict ? Locations.ConflictDeck : Locations.DynastyDeck;
                        
                        var moveAction = game.actions.MoveCard(new Dictionary<string, object>
                        {
                            { "target", card },
                            { "bottom", true },
                            { "destination", cardNewLocation }
                        });
                        if (moveAction is GameAction action)
                        {
                            action.Execute(game.GetFrameworkContext());
                        }
                        
                        game.AddMessage("{0} uses a command to move {1} from their {2} to the bottom of their {3}.", 
                                       p.name, card.name, cardInitialLocation, cardNewLocation);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);
            
            return true;
        }

        #endregion

        #region Honor/Dishonor Commands

        /// <summary>
        /// Honor a card
        /// </summary>
        private bool HonorCard(Player player, string[] args)
        {
            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card to honor",
                waitingPromptTitle = "Waiting for opponent to honor",
                cardCondition = new Func<BaseCard, bool>(card => 
                    card.location == Locations.PlayArea && card.controller == player),
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        card.Honor();
                        game.AddMessage("{0} uses the /honor command to honor {1}", p.name, card.name);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);
            
            return true;
        }

        /// <summary>
        /// Dishonor a card
        /// </summary>
        private bool DishonorCard(Player player, string[] args)
        {
            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card to dishonor",
                waitingPromptTitle = "Waiting for opponent to dishonor",
                cardCondition = new Func<BaseCard, bool>(card => 
                    card.location == Locations.PlayArea && card.controller == player),
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        card.Dishonor();
                        game.AddMessage("{0} uses the /dishonor command to dishonor {1}", p.name, card.name);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);
            
            return true;
        }

        #endregion

        #region Token and Fate Commands

        /// <summary>
        /// Set token count on a card
        /// </summary>
        private bool SetToken(Player player, string[] args)
        {
            if (args.Length < 2)
            {
                game.AddMessage("Usage: /token <token_type> <number>");
                return false;
            }

            string token = args[0];
            int num = GetNumberOrDefault(args[1], 1);

            if (!IsValidToken(token))
            {
                game.AddMessage("Invalid token type: {0}", token);
                return false;
            }

            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card",
                waitingPromptTitle = "Waiting for opponent to set token",
                cardCondition = new Func<BaseCard, bool>(card => 
                    (card.location == Locations.PlayArea || card.location == "plot") && card.controller == player),
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        int numTokens = card.GetTokenCount(token);
                        card.AddTokens(token, num - numTokens);
                        game.AddMessage("{0} uses the /token command to set the {1} token count of {2} to {3}", 
                                       p.name, token, card.name, num);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);

            return true;
        }

        /// <summary>
        /// Add fate to a card
        /// </summary>
        private bool AddFate(Player player, string[] args)
        {
            int num = GetNumberOrDefault(args.ElementAtOrDefault(0), 1);

            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card",
                waitingPromptTitle = "Waiting for opponent to set fate",
                cardCondition = new Func<BaseCard, bool>(card => 
                    card.location == Locations.PlayArea && card.controller == player),
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        card.ModifyFate(num);
                        game.AddMessage("{0} uses the /add-fate command to set the fate count of {1} to {2}", 
                                       p.name, card.name, card.GetFate());
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);

            return true;
        }

        /// <summary>
        /// Remove fate from a card
        /// </summary>
        private bool RemoveFate(Player player, string[] args)
        {
            int num = GetNumberOrDefault(args.ElementAtOrDefault(0), 1);

            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card",
                waitingPromptTitle = "Waiting for opponent to set fate",
                cardCondition = new Func<BaseCard, bool>(card => 
                    card.location == Locations.PlayArea && card.controller == player),
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        card.ModifyFate(-num);
                        game.AddMessage("{0} uses the /rem-fate command to set the fate count of {1} to {2}", 
                                       p.name, card.name, card.GetFate());
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);

            return true;
        }

        /// <summary>
        /// Add fate to a ring
        /// </summary>
        private bool AddRingFate(Player player, string[] args)
        {
            string ringElement = args.ElementAtOrDefault(0);
            int num = GetNumberOrDefault(args.ElementAtOrDefault(1), 1);

            var validElements = new string[] { "air", "earth", "fire", "void", "water" };

            if (!string.IsNullOrEmpty(ringElement) && validElements.Contains(ringElement.ToLower()))
            {
                Ring ring = game.rings[ringElement.ToLower()];
                ring.ModifyFate(num);
                game.AddMessage("{0} uses the /add-fate-ring command to set the fate count of the ring of {1} to {2}", 
                               player.name, ringElement, ring.GetFate());
            }
            else
            {
                game.PromptForRingSelect(player, new Dictionary<string, object>
                {
                    { "onSelect", new Func<Player, Ring, bool>((p, ring) =>
                        {
                            ring.ModifyFate(num);
                            game.AddMessage("{0} uses the /add-fate-ring command to set the fate count of the ring of {1} to {2}", 
                                           p.name, ring.element, ring.GetFate());
                            return true;
                        })
                    }
                });
            }

            return true;
        }

        /// <summary>
        /// Remove fate from a ring
        /// </summary>
        private bool RemoveRingFate(Player player, string[] args)
        {
            string ringElement = args.ElementAtOrDefault(0);
            int num = GetNumberOrDefault(args.ElementAtOrDefault(1), 1);

            var validElements = new string[] { "air", "earth", "fire", "void", "water" };

            if (!string.IsNullOrEmpty(ringElement) && validElements.Contains(ringElement.ToLower()))
            {
                Ring ring = game.rings[ringElement.ToLower()];
                ring.ModifyFate(-num);
                game.AddMessage("{0} uses the /rem-fate-ring command to set the fate count of the ring of {1} to {2}", 
                               player.name, ringElement, ring.GetFate());
            }
            else
            {
                game.PromptForRingSelect(player, new Dictionary<string, object>
                {
                    { "onSelect", new Func<Player, Ring, bool>((p, ring) =>
                        {
                            ring.ModifyFate(-num);
                            game.AddMessage("{0} uses the /rem-fate-ring command to set the fate count of the ring of {1} to {2}", 
                                           p.name, ring.element, ring.GetFate());
                            return true;
                        })
                    }
                });
            }

            return true;
        }

        #endregion

        #region Conflict Commands

        /// <summary>
        /// Move characters to current conflict
        /// </summary>
        private bool MoveToConflict(Player player, string[] args)
        {
            if (game.currentConflict == null)
            {
                game.AddMessage("/move-to-conflict can only be used during a conflict");
                return false;
            }

            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select cards to move into the conflict",
                waitingPromptTitle = "Waiting for opponent to choose cards to move",
                cardCondition = new Func<BaseCard, bool>(card => 
                    card.location == Locations.PlayArea && card.controller == player && !card.inConflict),
                cardType = CardTypes.Character,
                numCards = 0,
                multiSelect = true,
                onSelectMultiple = new Func<Player, List<BaseCard>, bool>((p, cards) =>
                    {
                        if (p.IsAttackingPlayer())
                        {
                            game.currentConflict.AddAttackers(cards);
                        }
                        else
                        {
                            game.currentConflict.AddDefenders(cards);
                        }
                        game.AddMessage("{0} uses the /move-to-conflict command", p.name);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);

            return true;
        }

        /// <summary>
        /// Send character home from conflict
        /// </summary>
        private bool SendHome(Player player, string[] args)
        {
            if (game.currentConflict == null)
            {
                game.AddMessage("/send-home can only be used during a conflict");
                return false;
            }

            var properties = new SelectCardPromptProperties
            {
                activePromptTitle = "Select a card to send home",
                waitingPromptTitle = "Waiting for opponent to send home",
                cardCondition = new Func<BaseCard, bool>(card => 
                    card.location == Locations.PlayArea && card.controller == player && card.inConflict),
                cardType = CardTypes.Character,
                onSelect = new Func<Player, BaseCard, bool>((p, card) =>
                    {
                        game.currentConflict.RemoveFromConflict(card);
                        game.AddMessage("{0} uses the /send-home command to send {1} home", p.name, card.name);
                        return true;
                    })
            };
            game.PromptForSelect(player, properties);

            return true;
        }

        /// <summary>
        /// Initiate a duel
        /// </summary>
        private bool InitiateDuel(Player player, string[] args)
        {
            game.AddMessage("{0} initiates a duel", player.name);
            
            // Create honor bid prompt
            var honorBidPrompt = new HonorBidPrompt(game, "Choose your bid for the duel");
            game.QueueStep(honorBidPrompt);
            
            return true;
        }

        #endregion

        #region Ring Commands

        /// <summary>
        /// Claim a ring
        /// </summary>
        private bool ClaimRing(Player player, string[] args)
        {
            string ringElement = args.ElementAtOrDefault(0);
            var validElements = new string[] { "air", "earth", "fire", "void", "water" };

            if (!string.IsNullOrEmpty(ringElement) && validElements.Contains(ringElement.ToLower()))
            {
                Ring ring = game.rings[ringElement.ToLower()];
                ring.ClaimRing(player);
                game.AddMessage("{0} uses the /claim-ring command to claim the ring of {1}", player.name, ringElement);
            }
            else
            {
                game.PromptForRingSelect(player, new Dictionary<string, object>
                {
                    { "onSelect", new Func<Player, Ring, bool>((p, ring) =>
                        {
                            ring.ClaimRing(p);
                            game.AddMessage("{0} uses the /claim-ring command to claim the ring of {1}", p.name, ring.element);
                            return true;
                        })
                    }
                });
            }

            return true;
        }

        /// <summary>
        /// Unclaim a ring
        /// </summary>
        private bool UnclaimRing(Player player, string[] args)
        {
            string ringElement = args.ElementAtOrDefault(0);
            var validElements = new string[] { "air", "earth", "fire", "void", "water" };

            if (!string.IsNullOrEmpty(ringElement) && validElements.Contains(ringElement.ToLower()))
            {
                Ring ring = game.rings[ringElement.ToLower()];
                ring.ResetRing();
                game.AddMessage("{0} uses the /unclaim-ring command to set the ring of {1} as unclaimed", player.name, ringElement);
            }
            else
            {
                game.PromptForRingSelect(player, new Dictionary<string, object>
                {
                    { "ringCondition", new Func<Ring, bool>(ring => ring.claimed) },
                    { "onSelect", new Func<Player, Ring, bool>((p, ring) =>
                        {
                            ring.ResetRing();
                            game.AddMessage("{0} uses the /unclaim-ring command to set the ring of {1} as unclaimed", p.name, ring.element);
                            return true;
                        })
                    }
                });
            }

            return true;
        }

        #endregion

        #region Imperial Favor Commands

        /// <summary>
        /// Claim imperial favor
        /// </summary>
        private bool ClaimFavor(Player player, string[] args)
        {
            string type = args.ElementAtOrDefault(0) ?? "military";
            
            game.AddMessage("{0} uses /claim-favor to claim the emperor's {1} favor", player.name, type);
            player.ClaimImperialFavor(type);
            
            Player otherPlayer = game.GetOtherPlayer(player);
            otherPlayer?.LoseImperialFavor();
            
            return true;
        }

        /// <summary>
        /// Discard imperial favor
        /// </summary>
        private bool DiscardFavor(Player player, string[] args)
        {
            game.AddMessage("{0} uses /discard-favor to discard the imperial favor", player.name);
            player.LoseImperialFavor();
            
            return true;
        }

        #endregion

        #region Timer Commands

        /// <summary>
        /// Start all player clocks
        /// </summary>
        private bool StartClocks(Player player, string[] args)
        {
            game.AddMessage("{0} restarts the timers", player.name);
            
            foreach (Player p in game.GetPlayers())
            {
                p.clock?.Restart();
            }
            
            return true;
        }

        /// <summary>
        /// Stop all player clocks
        /// </summary>
        private bool StopClocks(Player player, string[] args)
        {
            game.AddMessage("{0} stops the timers", player.name);
            
            foreach (Player p in game.GetPlayers())
            {
                p.clock?.Pause();
            }
            
            return true;
        }

        /// <summary>
        /// Modify player's clock
        /// </summary>
        private bool ModifyClock(Player player, string[] args)
        {
            int num = GetNumberOrDefault(args.ElementAtOrDefault(0), 60);
            
            game.AddMessage("{0} adds {1} seconds to their clock", player.name, num);
            player.clock?.Modify(num);
            
            return true;
        }

        #endregion

        #region Utility Commands

        /// <summary>
        /// Roll dice
        /// </summary>
        private bool RollDice(Player player, string[] args)
        {
            int num = GetNumberOrDefault(args.ElementAtOrDefault(0), 6);
            
            if (num > 1)
            {
                int result = UnityEngine.Random.Range(1, num + 1);
                game.AddMessage("{0} rolls a d{1}: {2}", player.name, num, result);
            }
            
            return true;
        }

        /// <summary>
        /// Toggle manual mode
        /// </summary>
        private bool ToggleManualMode(Player player, string[] args)
        {
            if (game.manualMode)
            {
                game.manualMode = false;
                game.AddMessage("{0} switches manual mode off", player.name);
            }
            else
            {
                game.manualMode = true;
                game.AddMessage("{0} switches manual mode on", player.name);
            }
            
            return true;
        }

        /// <summary>
        /// Disconnect player (for testing)
        /// </summary>
        private bool DisconnectPlayer(Player player, string[] args)
        {
            // In Unity, we might handle this differently than WebSocket disconnect
            player.Disconnect();
            game.AddMessage("{0} disconnected via command", player.name);
            
            return true;
        }

        #endregion

        #region Debug Commands

        /// <summary>
        /// Show debug information
        /// </summary>
        private bool ShowDebugInfo(Player player, string[] args)
        {
            if (!Debug.isDebugBuild && !debugMode)
            {
                return false;
            }

            var debugInfo = $"Game Debug Info:\n";
            debugInfo += $"Current Phase: {game.currentPhase}\n";
            debugInfo += $"Current Player: {game.currentPlayer?.name}\n";
            debugInfo += $"Manual Mode: {game.manualMode}\n";
            debugInfo += $"Cards in Play: {game.GetAllCards().Count(c => c.location == Locations.PlayArea)}\n";
            
            if (game.currentConflict != null)
            {
                debugInfo += $"Current Conflict: {game.currentConflict.declaredType} at {game.currentConflict.ring.element}\n";
            }

            Debug.Log(debugInfo);
            game.AddMessage("{0} requested debug information (check console)", player.name);
            
            return true;
        }

        /// <summary>
        /// Reset game state (debug only)
        /// </summary>
        private bool ResetGame(Player player, string[] args)
        {
            if (!Debug.isDebugBuild && !debugMode)
            {
                return false;
            }

            game.AddMessage("{0} resets the game state", player.name);
            // Implement game reset logic here
            
            return true;
        }

        /// <summary>
        /// Skip current phase (debug only)
        /// </summary>
        private bool SkipPhase(Player player, string[] args)
        {
            if (!Debug.isDebugBuild && !debugMode)
            {
                return false;
            }

            game.AddMessage("{0} skips the current phase", player.name);
            // Implement phase skip logic here
            
            return true;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get number from string or return default
        /// </summary>
        private int GetNumberOrDefault(string input, int defaultNumber)
        {
            if (string.IsNullOrEmpty(input))
                return defaultNumber;

            if (int.TryParse(input, out int result) && result >= 0)
            {
                return result;
            }

            return defaultNumber;
        }

        /// <summary>
        /// Check if token type is valid
        /// </summary>
        private bool IsValidToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            return validTokens.Contains(token.ToLower());
        }

        /// <summary>
        /// Get list of available commands
        /// </summary>
        public List<string> GetAvailableCommands()
        {
            return commands.Keys.ToList();
        }

        /// <summary>
        /// Get help text for all commands
        /// </summary>
        public string GetHelpText()
        {
            var help = "Available Commands:\n\n";
            
            help += "Card Manipulation:\n";
            help += "/draw [number] - Draw cards to hand\n";
            help += "/discard [number] - Discard cards at random\n";
            help += "/reveal - Reveal a facedown card\n";
            help += "/move-to-bottom-deck - Move card to bottom of deck\n\n";
            
            help += "Honor/Dishonor:\n";
            help += "/honor - Honor a card\n";
            help += "/dishonor - Dishonor a card\n\n";
            
            help += "Tokens and Fate:\n";
            help += "/token <type> <number> - Set token count\n";
            help += "/add-fate [number] - Add fate to card\n";
            help += "/rem-fate [number] - Remove fate from card\n";
            help += "/add-fate-ring [element] [number] - Add fate to ring\n";
            help += "/rem-fate-ring [element] [number] - Remove fate from ring\n\n";
            
            help += "Conflict:\n";
            help += "/move-to-conflict - Move characters to conflict\n";
            help += "/send-home - Send character home\n";
            help += "/duel - Initiate a duel\n\n";
            
            help += "Rings:\n";
            help += "/claim-ring [element] - Claim a ring\n";
            help += "/unclaim-ring [element] - Unclaim a ring\n\n";
            
            help += "Imperial Favor:\n";
            help += "/claim-favor [type] - Claim imperial favor\n";
            help += "/discard-favor - Discard imperial favor\n\n";
            
            help += "Utility:\n";
            help += "/roll [sides] - Roll dice\n";
            help += "/manual - Toggle manual mode\n";
            help += "/start-clocks - Start timers\n";
            help += "/stop-clocks - Stop timers\n";
            help += "/modify-clock [seconds] - Add time to clock\n";
            
            return help;
        }

        #endregion
    }

    /// <summary>
    /// Honor bid prompt for duels
    /// </summary>
    public class HonorBidPrompt : IGameStep
    {
        private Game game;
        private string promptTitle;
        private System.Action<int> costHandler;
        private List<int> prohibitedBids;
        private Duel duel;

        public HonorBidPrompt(Game game, string promptTitle)
        {
            this.game = game;
            this.promptTitle = promptTitle;
            this.costHandler = null;
            this.prohibitedBids = new List<int>();
            this.duel = null;
        }
        
        public HonorBidPrompt(Game game, string promptTitle, System.Action<int> costHandler, List<int> prohibitedBids, Duel duel = null)
        {
            this.game = game;
            this.promptTitle = promptTitle;
            this.costHandler = costHandler;
            this.prohibitedBids = prohibitedBids ?? new List<int>();
            this.duel = duel;
        }

        public bool Execute()
        {
            // Implementation for honor bid during duels
            foreach (Player player in game.GetPlayers())
            {
                var properties = new SelectCardPromptProperties
                {
                    activePromptTitle = promptTitle,
                    waitingPromptTitle = "Waiting for opponent to bid honor"
                    // Note: Honor bidding might need a different prompt type - for now using basic properties
                };
                // TODO: Implement proper honor bid prompt - this is a placeholder
                game.PromptForSelect(player, properties);
            }
            return true;
        }

        public bool IsComplete => true;
        public bool CanCancel => false;

        public bool Continue() => true;

        public bool CancelStep() => false;

        public void QueueStep(IGameStep step) { }

        public bool OnCardClicked(Player player, BaseCard card) => false;

        public bool OnRingClicked(Player player, Ring ring) => false;

        public bool OnMenuCommand(Player player, string command, string arg1, string arg2) => false;

        public string GetDebugInfo() => $"HonorBidPrompt: {promptTitle}";

        public string StepName => "HonorBidPrompt";
    }

    /// <summary>
    /// Extension methods for chat command integration
    /// </summary>
    public static class ChatCommandExtensions
    {
        /// <summary>
        /// Process chat message for commands
        /// </summary>
        public static bool ProcessChatMessage(this Game game, Player player, string message)
        {
            var chatCommands = UnityEngine.Object.FindObjectOfType<ChatCommands>();
            return chatCommands?.ExecuteCommand(player, message) ?? false;
        }

        /// <summary>
        /// Register chat command system with game
        /// </summary>
        public static void RegisterChatCommands(this Game game)
        {
            if (UnityEngine.Object.FindObjectOfType<ChatCommands>() == null)
            {
                var chatCommandsGO = new UnityEngine.GameObject("ChatCommands");
                chatCommandsGO.AddComponent<ChatCommands>();
            }
        }

        /// <summary>
        /// Execute command directly
        /// </summary>
        public static bool ExecuteChatCommand(this Player player, string command, params object[] args)
        {
            var chatCommands = UnityEngine.Object.FindObjectOfType<ChatCommands>();
            string argsString = args.Length > 0 ? " " + string.Join(" ", args) : "";
            return chatCommands?.ExecuteCommand(player, command + argsString) ?? false;
        }
    }

    /// <summary>
    /// Chat command event args for integration with UI systems
    /// </summary>
    public class ChatCommandEventArgs : EventArgs
    {
        public Player Player { get; set; }
        public string Command { get; set; }
        public string[] Arguments { get; set; }
        public bool WasExecuted { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Chat command configuration for different game modes
    /// </summary>
    [System.Serializable]
    public class ChatCommandConfig
    {
        [Header("Command Permissions")]
        public bool allowCardManipulation = true;
        public bool allowHonorDishonor = true;
        public bool allowTokens = true;
        public bool allowConflictCommands = true;
        public bool allowRingCommands = true;
        public bool allowTimerCommands = true;
        public bool allowDebugCommands = false;

        [Header("Restrictions")]
        public bool requireDebugMode = false;
        public bool logAllCommands = true;
        public List<string> restrictedCommands = new List<string>();
        public List<string> allowedPlayers = new List<string>();

        /// <summary>
        /// Check if command is allowed
        /// </summary>
        public bool IsCommandAllowed(string command, Player player)
        {
            // Check if command is in restricted list
            if (restrictedCommands.Contains(command))
            {
                return false;
            }

            // Check player restrictions
            if (allowedPlayers.Count > 0 && !allowedPlayers.Contains(player.name))
            {
                return false;
            }

            // Check category permissions
            if (IsCardManipulationCommand(command) && !allowCardManipulation)
                return false;
            if (IsHonorDishonorCommand(command) && !allowHonorDishonor)
                return false;
            if (IsTokenCommand(command) && !allowTokens)
                return false;
            if (IsConflictCommand(command) && !allowConflictCommands)
                return false;
            if (IsRingCommand(command) && !allowRingCommands)
                return false;
            if (IsTimerCommand(command) && !allowTimerCommands)
                return false;
            if (IsDebugCommand(command) && !allowDebugCommands)
                return false;

            return true;
        }

        private bool IsCardManipulationCommand(string command)
        {
            var cardCommands = new string[] { "/draw", "/discard", "/reveal", "/move-to-bottom-deck" };
            return cardCommands.Contains(command);
        }

        private bool IsHonorDishonorCommand(string command)
        {
            var honorCommands = new string[] { "/honor", "/dishonor" };
            return honorCommands.Contains(command);
        }

        private bool IsTokenCommand(string command)
        {
            var tokenCommands = new string[] { "/token", "/add-fate", "/rem-fate", "/add-fate-ring", "/rem-fate-ring" };
            return tokenCommands.Contains(command);
        }

        private bool IsConflictCommand(string command)
        {
            var conflictCommands = new string[] { "/move-to-conflict", "/send-home", "/duel" };
            return conflictCommands.Contains(command);
        }

        private bool IsRingCommand(string command)
        {
            var ringCommands = new string[] { "/claim-ring", "/unclaim-ring" };
            return ringCommands.Contains(command);
        }

        private bool IsTimerCommand(string command)
        {
            var timerCommands = new string[] { "/start-clocks", "/stop-clocks", "/modify-clock" };
            return timerCommands.Contains(command);
        }

        private bool IsDebugCommand(string command)
        {
            var debugCommands = new string[] { "/debug-info", "/reset-game", "/skip-phase", "/disconnectme" };
            return debugCommands.Contains(command);
        }
    }
}