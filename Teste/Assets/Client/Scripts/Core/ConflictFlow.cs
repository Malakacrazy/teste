using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Complete conflict resolution pipeline following L5R rules.
    /// Handles the entire flow from declaration through ring claiming.
    /// 
    /// Conflict Resolution Steps:
    /// 3.2 Declare Conflict
    /// 3.2.1 Declare defenders  
    /// 3.2.2 CONFLICT ACTION WINDOW (Defender has first opportunity)
    /// 3.2.3 Compare skill values
    /// 3.2.4 Apply unopposed
    /// 3.2.5 Break province
    /// 3.2.6 Resolve Ring effects
    /// 3.2.7 Claim ring
    /// 3.2.8 Return home
    /// </summary>
    public class ConflictFlow : BaseStepWithPipeline
    {
        [Header("Conflict Flow Properties")]
        public Conflict conflict;
        public bool canPass;
        
        // Covert tracking
        private List<AbilityContext> covertContexts = new List<AbilityContext>();

        public ConflictFlow(Game game, Conflict conflict, bool canPass) : base(game)
        {
            this.conflict = conflict;
            this.canPass = canPass;
            InitializeConflictPipeline();
        }

        /// <summary>
        /// Initialize the complete conflict resolution pipeline
        /// </summary>
        private void InitializeConflictPipeline()
        {
            pipeline.Initialize(new List<IGameStep>
            {
                new SimpleStep(game, ResetCards),
                new SimpleStep(game, PromptForNewConflict),
                new SimpleStep(game, InitiateConflict),
                new SimpleStep(game, PayAttackerCosts),
                new SimpleStep(game, PromptForCovert),
                new SimpleStep(game, ResolveCovert),
                new SimpleStep(game, RaiseDeclarationEvents),
                new SimpleStep(game, AnnounceAttackerSkill),
                new SimpleStep(game, PromptForDefenders),
                new SimpleStep(game, AnnounceDefenderSkill),
                new SimpleStep(game, OpenConflictActionWindow),
                new SimpleStep(game, DetermineWinner),
                new SimpleStep(game, AfterConflict),
                new SimpleStep(game, ApplyUnopposed),
                new SimpleStep(game, CheckBreakProvince),
                new SimpleStep(game, ResolveRingEffects),
                new SimpleStep(game, ClaimRing),
                new SimpleStep(game, ReturnHome),
                new SimpleStep(game, CompleteConflict)
            });
        }

        #region Conflict Pipeline Steps

        /// <summary>
        /// Reset all cards to neutral conflict state
        /// </summary>
        public bool ResetCards()
        {
            conflict.ResetCards();
            return true;
        }

        /// <summary>
        /// Prompt for new conflict declaration or allow pass
        /// </summary>
        public bool PromptForNewConflict()
        {
            Player attackingPlayer = conflict.attackingPlayer;
            
            // Check if player is restricted from choosing conflict ring
            if (attackingPlayer.CheckRestrictions("chooseConflictRing", game.GetFrameworkContext()) || 
                attackingPlayer.opponent == null)
            {
                pipeline.QueueStep(new InitiateConflictPrompt(game, conflict, attackingPlayer, true, canPass));
                return true;
            }

            // Give attacking player choice to declare or pass
            var choices = new List<MenuOption> 
            { 
                new MenuOption { text = "Declare a conflict", arg = "declare" },
                new MenuOption { text = "Pass conflict opportunity", arg = "pass" }
            };
            var handlers = new List<Action>
            {
                () => PromptDefenderForRingChoice(),
                () => conflict.PassConflict()
            };

            game.PromptWithHandlerMenu(attackingPlayer, new HandlerMenuPromptProperties
            {
                activePromptTitle = "Do you wish to declare a conflict?",
                choices = choices,
                handlers = handlers
            });

            return true;
        }

        /// <summary>
        /// Let defender choose the conflict ring for attacker
        /// </summary>
        private void PromptDefenderForRingChoice()
        {
            game.PromptForRingSelect(conflict.defendingPlayer, new Dictionary<string, object>
            {
                { "activePromptTitle", $"Choose a ring for {conflict.attackingPlayer.name}'s conflict" },
                { "source", "Defender chooses conflict ring" },
                { "waitingPromptTitle", "Waiting for defender to choose conflict ring" },
                { "ringCondition", new Func<Ring, bool>(ring => 
                    conflict.attackingPlayer.HasLegalConflictDeclaration(new ConflictProperties { ring = ring })) },
                { "onSelect", new Func<Player, Ring, bool>((player, ring) =>
                    {
                        // Ensure valid conflict type for the ring
                        if (!conflict.attackingPlayer.HasLegalConflictDeclaration(new ConflictProperties 
                            { type = new List<string> { ring.conflictType }, ring = ring }))
                        {
                            ring.FlipConflictType();
                        }
                        
                        conflict.ring = ring;
                        ring.contested = true;
                        
                        pipeline.QueueStep(new InitiateConflictPrompt(game, conflict, conflict.attackingPlayer, false));
                        return true;
                    })
                }
            });
        }

        /// <summary>
        /// Officially initiate the conflict
        /// </summary>
        public bool InitiateConflict()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            // Mark all attackers as in conflict
            foreach (BaseCard attacker in conflict.attackers)
            {
                attacker.inConflict = true;
            }

            game.RecordConflict(conflict);
            return true;
        }

        /// <summary>
        /// Pay fate costs for attacking characters
        /// </summary>
        public bool PayAttackerCosts()
        {
            int totalFateCost = conflict.attackers
                .Sum(card => card.SumEffects(EffectNames.FateCostToAttack));

            if (totalFateCost > 0)
            {
                game.AddMessage("{0} pays {1} fate to declare his attackers", 
                               conflict.attackingPlayer.name, totalFateCost);

                game.costs.PayFate(conflict.attackingPlayer, totalFateCost);
            }

            return true;
        }

        /// <summary>
        /// Prompt for covert assignments
        /// </summary>
        public bool PromptForCovert()
        {
            covertContexts.Clear();

            if (conflict.conflictPassed || conflict.isSinglePlayer)
            {
                return true;
            }

            // Find characters that can be covert targets
            var targets = conflict.defendingPlayer.cardsInPlay
                .Where(card => card.covert)
                .ToList();

            // Find characters with covert ability
            var covertAttackers = conflict.attackers
                .Where(card => card.IsCovert())
                .ToList();

            // Create ability contexts for covert
            var contexts = covertAttackers.Select(card => new AbilityContext(new AbilityContextProperties
            {
                game = game,
                player = conflict.attackingPlayer,
                source = card,
                ability = new CovertAbility()
            })).Where(context => (context.source as BaseCard)?.CanInitiateKeywords(context) == true)
            .ToList();

            if (contexts.Count == 0)
            {
                return true;
            }

            // Reset covert flags
            foreach (var target in targets)
            {
                target.covert = false;
            }

            // Handle automatic assignment if counts match
            if (targets.Count == contexts.Count && HandleAutomaticCovertAssignment(targets, contexts))
            {
                return true;
            }

            // Prompt for manual covert assignments
            PromptForManualCovertAssignments(contexts);
            return true;
        }

        /// <summary>
        /// Handle automatic covert assignment when counts match perfectly
        /// </summary>
        private bool HandleAutomaticCovertAssignment(List<BaseCard> targets, List<AbilityContext> contexts)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                var context = contexts[i];
                context.SetTarget("target", targets[i]);
                covertContexts.Add(context);
            }

            // Check if all assignments are valid
            if (covertContexts.All(context => (context.GetTarget("target") as BaseCard)?.CanBeBypassedByCovert(context) == true))
            {
                return true;
            }

            covertContexts.Clear();
            return false;
        }

        /// <summary>
        /// Prompt player to manually assign covert targets
        /// </summary>
        private void PromptForManualCovertAssignments(List<AbilityContext> contexts)
        {
            foreach (var context in contexts)
            {
                if (context.player.CheckRestrictions("initiateKeywords", context))
                {
                    game.PromptForSelect(conflict.attackingPlayer, new SelectCardPromptProperties
                    {
                        activePromptTitle = $"Choose covert target for {(context.source as BaseCard)?.name ?? "Unknown"}",
                        cardType = CardTypes.Character,
                        controller = Players.Opponent,
                        cardCondition = card => card.CanBeBypassedByCovert(context) && card.CheckRestrictions("target", context),
                        onSelect = (player, card) =>
                            {
                                context.SetTarget("target", card);
                                covertContexts.Add(context);
                                return true;
                            }
                    });
                }
            }
        }

        /// <summary>
        /// Resolve all covert assignments
        /// </summary>
        public bool ResolveCovert()
        {
            if (covertContexts.Count == 0)
            {
                return true;
            }

            var events = new List<GameEvent>();

            // Create initiate ability events for each covert
            foreach (var context in covertContexts)
            {
                var initiateEvent = new InitiateCardAbilityEvent(
                    new Dictionary<string, object>
                    {
                        { "card", context.source },
                        { "context", context }
                    },
                    () => { if (context.GetTarget("target") is BaseCard card) card.covert = true; return true; }
                );
                events.Add(initiateEvent);
            }

            // Create covert resolved events
            foreach (var context in covertContexts)
            {
                var resolvedEvent = game.GetEvent(EventNames.OnCovertResolved, new Dictionary<string, object>
                {
                    { "card", context.source },
                    { "context", context }
                }, () => true);
                events.Add(resolvedEvent);
            }

            game.OpenEventWindow(events);
            return true;
        }

        /// <summary>
        /// Raise conflict declaration events and take ring fate
        /// </summary>
        public bool RaiseDeclarationEvents()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            game.AddMessage("{0} is initiating a {1} conflict at {2}, contesting {3}", 
                           conflict.attackingPlayer.name, conflict.conflictType, 
                           conflict.conflictProvince.name, conflict.ring.name);

            var events = new List<GameEvent>();
            var ring = conflict.ring;

            // Create conflict declared event
            var conflictDeclaredEvent = game.GetEvent(EventNames.OnConflictDeclared, new Dictionary<string, object>
            {
                { "conflict", conflict },
                { "type", conflict.conflictType },
                { "ring", ring },
                { "attackers", conflict.attackers.ToList() },
                { "ringFate", ring.fate }
            }, () => true);
            events.Add(conflictDeclaredEvent);

            // Take fate from ring if allowed
            if (ring.fate > 0 && conflict.attackingPlayer.CheckRestrictions("takeFateFromRings", game.GetFrameworkContext()))
            {
                var takeFateAction = game.actions.TakeFateFromRing(conflict.attackingPlayer, ring, ring.fate);
                takeFateAction.AddEventsToArray(events, game.GetFrameworkContext(conflict.attackingPlayer));
                
                game.AddMessage("{0} takes {1} fate from {2}", 
                               conflict.attackingPlayer.name, ring.fate, ring.name);
            }

            // Reveal province if not single player
            if (!conflict.isSinglePlayer)
            {
                conflict.conflictProvince.inConflict = true;
                var revealAction = game.actions.Reveal(new List<BaseCard> { conflict.conflictProvince });
                revealAction.AddEventsToArray(events, game.GetFrameworkContext(conflict.attackingPlayer));
            }

            game.OpenEventWindow(events);
            return true;
        }

        /// <summary>
        /// Announce attacker's total skill
        /// </summary>
        public bool AnnounceAttackerSkill()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            game.AddMessage("{0} has initiated a {1} conflict with skill {2}", 
                           conflict.attackingPlayer.name, conflict.conflictType, conflict.attackerSkill);
            return true;
        }

        /// <summary>
        /// Prompt defending player to choose defenders
        /// </summary>
        public bool PromptForDefenders()
        {
            if (conflict.conflictPassed || conflict.isSinglePlayer)
            {
                return true;
            }

            game.QueueStep(new SelectDefendersPrompt(game, conflict.defendingPlayer, conflict));
            return true;
        }

        /// <summary>
        /// Announce defender's total skill
        /// </summary>
        public bool AnnounceDefenderSkill()
        {
            if (conflict.conflictPassed || conflict.isSinglePlayer)
            {
                return true;
            }

            // Mark all defenders as in conflict
            foreach (BaseCard defender in conflict.defenders)
            {
                defender.inConflict = true;
            }

            // Clear covert flags
            foreach (BaseCard card in conflict.defendingPlayer.cardsInPlay)
            {
                card.covert = false;
            }

            // Announce defense
            if (conflict.defenders.Count > 0)
            {
                game.AddMessage("{0} has defended with skill {1}", 
                               conflict.defendingPlayer.name, conflict.defenderSkill);
            }
            else
            {
                game.AddMessage("{0} does not defend the conflict", conflict.defendingPlayer.name);
            }

            // Raise defenders declared event
            game.RaiseEvent(EventNames.OnDefendersDeclared, new Dictionary<string, object>
            {
                { "conflict", conflict },
                { "defenders", conflict.defenders.ToList() }
            });

            return true;
        }

        /// <summary>
        /// Open conflict action window for abilities
        /// </summary>
        public bool OpenConflictActionWindow()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            QueueStep(new ConflictActionWindow(game, "Conflict Action Window", conflict));
            return true;
        }

        /// <summary>
        /// Determine conflict winner based on skill
        /// </summary>
        public bool DetermineWinner()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            // Use ConflictResolution for the actual calculation
            var resolution = new ConflictResolution(game, conflict);
            var result = resolution.ResolveConflict();
            
            // Result is automatically applied to conflict object
            // conflict.winner, conflict.loser, etc. are now set
            
            return true;
        }

        /// <summary>
        /// Prompt for manual winner selection in manual mode
        /// </summary>
        private void PromptForManualWinnerSelection()
        {
            game.PromptWithMenu(conflict.attackingPlayer, this, new Dictionary<string, object>
            {
                { "activePrompt", new Dictionary<string, object>
                    {
                        { "promptTitle", "Conflict Result" },
                        { "menuTitle", "How did the conflict resolve?" },
                        { "buttons", new List<object>
                            {
                                new { text = "Attacker Won", arg = "attacker", method = "ManuallyDetermineWinner" },
                                new { text = "Defender Won", arg = "defender", method = "ManuallyDetermineWinner" },
                                new { text = "No Winner", arg = "nowinner", method = "ManuallyDetermineWinner" }
                            }
                        }
                    }
                },
                { "waitingPromptTitle", "Waiting for opponent to resolve conflict" }
            });
        }

        /// <summary>
        /// Handle manual winner determination
        /// </summary>
        public bool ManuallyDetermineWinner(Player player, string choice)
        {
            switch (choice)
            {
                case "attacker":
                    conflict.winner = player;
                    conflict.loser = conflict.defendingPlayer;
                    break;
                case "defender":
                    conflict.winner = conflict.defendingPlayer;
                    conflict.loser = player;
                    break;
                default:
                    conflict.winner = null;
                    conflict.loser = null;
                    break;
            }

            ShowConflictResult();
            return true;
        }

        /// <summary>
        /// Display conflict result message
        /// </summary>
        private void ShowConflictResult()
        {
            if (conflict.winner == null && conflict.loser == null)
            {
                game.AddMessage("There is no winner or loser for this conflict because both sides have 0 skill");
            }
            else
            {
                game.AddMessage("{0} won a {1} conflict {2} vs {3}",
                               conflict.winner.name, conflict.conflictType, 
                               conflict.winnerSkill, conflict.loserSkill);
            }
        }

        /// <summary>
        /// Process after-conflict effects and check for unopposed
        /// </summary>
        public bool AfterConflict()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            game.CheckGameState(true);

            // Create conditional after-conflict event
            var eventFactory = new Func<object>(() =>
            {
                var afterConflictEvent = game.GetEvent(EventNames.AfterConflict, 
                    new Dictionary<string, object> { { "conflict", conflict } },
                    () =>
                    {
                        var effects = conflict.GetEffects(EffectNames.ForceConflictUnopposed);
                        bool forcedUnopposed = effects.Any();

                        ShowConflictResult();
                        game.RecordConflictWinner(conflict);

                        if ((conflict.IsAttackerTheWinner() && conflict.defenders.Count == 0) || forcedUnopposed)
                        {
                            conflict.conflictUnopposed = true;
                        }
                        return true;
                    });

                // Re-evaluate winner if needed
                conflict.winnerDetermined = false;

                return afterConflictEvent;
            });

            game.OpenEventWindow(new List<object> { eventFactory() });
            return true;
        }

        /// <summary>
        /// Apply unopposed penalty (honor loss)
        /// </summary>
        public bool ApplyUnopposed()
        {
            if (conflict.conflictPassed || game.manualMode || conflict.isSinglePlayer)
            {
                return true;
            }

            if (conflict.conflictUnopposed)
            {
                game.AddMessage("{0} loses 1 honor for not defending the conflict", conflict.loser.name);
                
                var loseHonorAction = game.actions.LoseHonor(conflict.loser, 1);
                loseHonorAction.Resolve(conflict.loser, game.GetFrameworkContext(conflict.loser));
            }

            return true;
        }

        /// <summary>
        /// Check if province should be broken
        /// </summary>
        public bool CheckBreakProvince()
        {
            if (conflict.conflictPassed || conflict.isSinglePlayer || game.manualMode)
            {
                return true;
            }

            var province = conflict.conflictProvince;
            if (conflict.IsAttackerTheWinner() && 
                conflict.skillDifference >= province.GetStrength() && 
                !province.isBroken)
            {
                game.ApplyGameAction(null, new Dictionary<string, object> { { "break", province } });
            }

            return true;
        }

        /// <summary>
        /// Resolve ring effects if attacker won
        /// </summary>
        public bool ResolveRingEffects()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            if (conflict.IsAttackerTheWinner())
            {
                var resolveRingAction = game.actions.ResolveConflictRing(conflict.ring, conflict.attackingPlayer);
            }

            return true;
        }

        /// <summary>
        /// Claim the contested ring
        /// </summary>
        public bool ClaimRing()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            var ring = conflict.ring;
            
            if (ring.claimed)
            {
                ring.contested = false;
                return true;
            }

            if (conflict.winner != null)
            {
                game.RaiseEvent(EventNames.OnClaimRing, new Dictionary<string, object>
                {
                    { "player", conflict.winner },
                    { "conflict", conflict },
                    { "ring", conflict.ring }
                }, () => { ring.ClaimRing(conflict.winner); return true; });
            }

            // Queue step to clear contested status
            game.QueueSimpleStep(() =>
            {
                ring.contested = false;
                return true;
            });

            return true;
        }

        /// <summary>
        /// Return all characters home and bow them appropriately
        /// </summary>
        public bool ReturnHome()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            var events = new List<GameEvent>();

            // Bow attackers who should bow
            var attackersToBow = conflict.attackers.Where(card => card.BowsOnReturnHome()).ToList();
            if (attackersToBow.Count > 0)
            {
                var attackerBowAction = game.actions.Bow(attackersToBow);
            }

            // Bow defenders who should bow
            var defendersToBow = conflict.defenders.Where(card => card.BowsOnReturnHome()).ToList();
            if (defendersToBow.Count > 0)
            {
                var defenderBowAction = game.actions.Bow(defendersToBow);
            }

            // Create return home events for all participants
            var allParticipants = conflict.attackers.Concat(conflict.defenders).ToList();
            foreach (var card in allParticipants)
            {
                conflict.RemoveFromConflict(card);
            }

            // Add all participants return home event
            var allParticipantsEvent = game.GetEvent(EventNames.OnParticipantsReturnHome, new Dictionary<string, object>
            {
                { "conflict", conflict }
            }, () => true);

            events.Add(allParticipantsEvent);

            game.OpenEventWindow(events);
            return true;
        }

        /// <summary>
        /// Complete the conflict and clean up
        /// </summary>
        public bool CompleteConflict()
        {
            if (conflict.conflictPassed)
            {
                return true;
            }

            game.currentConflict = null;
            game.RaiseEvent(EventNames.OnConflictFinished, new Dictionary<string, object>
            {
                { "conflict", conflict }
            });

            game.QueueSimpleStep(() => ResetCards());
            return true;
        }

        #endregion
    }

    /// <summary>
    /// Covert keyword ability implementation
    /// </summary>
    public class CovertAbility : BaseAbility
    {
        public CovertAbility() : base(null, null, new BaseAbilityProperties
        {
            title = "Covert",
            printedAbility = "Covert"
        })
        {
        }

        public virtual bool IsKeywordAbility()
        {
            return true;
        }
    }

    /// <summary>
    /// Prompt for initiating conflicts
    /// </summary>
    public class InitiateConflictPrompt : BaseStep
    {
        private Conflict conflict;
        private Player player;
        private bool canChooseRing;
        private bool canPass;

        public InitiateConflictPrompt(Game game, Conflict conflict, Player player, bool canChooseRing, bool canPass = true) : base(game)
        {
            this.conflict = conflict;
            this.player = player;
            this.canChooseRing = canChooseRing;
            this.canPass = canPass;
        }

        public bool Execute()
        {
            // Implementation for conflict initiation prompt
            var promptProperties = new Dictionary<string, object>
            {
                { "activePromptTitle", "Declare Conflict" },
                { "player", player }
            };

            if (canChooseRing)
            {
                promptProperties["chooseRing"] = true;
            }

            if (canPass)
            {
                promptProperties["canPass"] = true;
            }

            // This would integrate with the UI system
            game.PromptForConflictDeclaration(player, promptProperties);
            return true;
        }

        public bool IsComplete() => true;
        public bool CanCancel => canPass;

        public bool Continue()
        {
            return IsComplete();
        }

        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Handle menu commands for conflict initiation
        }

        public void OnCardClicked(Player player, BaseCard card)
        {
            // Handle card clicks for conflict initiation
        }

        public void OnRingClicked(Player player, Ring ring)
        {
            // Handle ring clicks for conflict initiation
        }
    }

    /// <summary>
    /// Prompt for selecting defenders
    /// </summary>
    public class SelectDefendersPrompt : IGameStep
    {
        private Game game;
        private Player player;
        private Conflict conflict;

        public SelectDefendersPrompt(Game game, Player player, Conflict conflict)
        {
            this.game = game;
            this.player = player;
            this.conflict = conflict;
        }

        public bool Execute()
        {
            // Find characters that can defend
            var eligibleDefenders = player.cardsInPlay
                .Where(card => card.GetCardType() == CardTypes.Character && 
                              card.CanParticipateAsDefender(conflict))
                .ToList();

            if (eligibleDefenders.Count == 0)
            {
                game.AddMessage("{0} has no legal defenders", player.name);
                return true;
            }

            // Prompt for defender selection
            game.PromptForSelect(player, new SelectCardPromptProperties
            {
                activePromptTitle = "Choose defenders",
                waitingPromptTitle = "Waiting for opponent to choose defenders",
                cardType = CardTypes.Character,
                multiSelect = true,
                numCards = 0, // Unlimited
                cardCondition = card => eligibleDefenders.Contains(card) && !card.covert,
                onSelectMultiple = (p, cards) =>
                    {
                        conflict.defenders.Clear();
                        conflict.defenders.AddRange(cards);
                        return true;
                    }
            });

            return true;
        }

        public bool IsComplete() => true;
        public bool CanCancel => true;

        public bool Continue()
        {
            return IsComplete();
        }

        public void OnMenuCommand(Player player, string command, string arg, string uuid, string method)
        {
            // Handle menu commands for defender selection
        }

        public void OnCardClicked(Player player, BaseCard card)
        {
            // Handle card clicks for defender selection
        }

        public void OnRingClicked(Player player, Ring ring)
        {
            // Handle ring clicks for defender selection
        }
    }

    /// <summary>
    /// Conflict action window for playing cards and abilities during conflicts
    /// </summary>
    public class ConflictActionWindow : BaseStep
    {
        private string windowName;
        private Conflict conflict;

        public ConflictActionWindow(Game game, string windowName, Conflict conflict) : base(game)
        {
            this.windowName = windowName;
            this.conflict = conflict;
        }

        public override bool Continue()
        {
            // Implementation for conflict action window
            // This would handle the back-and-forth of playing cards and abilities during conflicts
            
            // Priority starts with defending player
            var currentPlayer = conflict.defendingPlayer;
            
            // Open ability/action window
            game.OpenActionWindow(windowName, currentPlayer);
            
            return true;
        }
    }

    /// <summary>
    /// Extension methods for conflict flow integration
    /// </summary>
    public static class ConflictFlowExtensions
    {
        /// <summary>
        /// Start a new conflict flow
        /// </summary>
        public static ConflictFlow StartConflictFlow(this Game game, Conflict conflict, bool canPass = true)
        {
            var conflictFlow = new ConflictFlow(game, conflict, canPass);
            game.QueueStep(conflictFlow);
            return conflictFlow;
        }

        /// <summary>
        /// Check if player can participate as defender
        /// </summary>
        public static bool CanParticipateAsDefender(this BaseCard card, Conflict conflict)
        {
            if (card.GetCardType() != CardTypes.Character)
                return false;

            if (card.controller != conflict.defendingPlayer)
                return false;

            if (card.isBowed)
                return false;

            if (card.covert)
                return false;

            // Check for specific restrictions
            return !card.CheckRestrictions("participateAsDefender", 
                AbilityContext.CreateCardContext(card.game, card, card.controller));
        }

        /// <summary>
        /// Check if player can participate as attacker
        /// </summary>
        public static bool CanParticipateAsAttacker(this BaseCard card, Conflict conflict)
        {
            if (card.GetCardType() != CardTypes.Character)
                return false;

            if (card.controller != conflict.attackingPlayer)
                return false;

            if (card.isBowed)
                return false;

            // Check for specific restrictions
            return !card.CheckRestrictions("participateAsAttacker", 
                AbilityContext.CreateCardContext(card.game, card, card.controller));
        }

        /// <summary>
        /// Check if character bows when returning home
        /// </summary>
        public static bool BowsOnReturnHome(this BaseCard card)
        {
            // Some effects prevent characters from bowing when returning home
            return !card.AnyEffect(EffectNames.DoesNotBowAsAttacker) && 
                   !card.AnyEffect(EffectNames.DoesNotBowAsDefender);
        }

        /// <summary>
        /// Check if character can be bypassed by covert
        /// </summary>
        public static bool CanBeBypassedByCovert(this BaseCard card, AbilityContext context)
        {
            if (card.GetCardType() != CardTypes.Character)
                return false;

            if (card.controller == context.player)
                return false;

            // Check for covert immunity
            if (card.AnyEffect(EffectNames.CannotBeBypassedByCovert))
                return false;

            return !card.CheckRestrictions("bypassedByCovert", context);
        }

        /// <summary>
        /// Check if character has covert ability
        /// </summary>
        public static bool IsCovert(this BaseCard card)
        {
            return card.HasKeyword("covert") || card.AnyEffect(EffectNames.GainCovert);
        }
    }

    /// <summary>
    /// Conflict-related events for the event system
    /// </summary>
    public static class ConflictEvents
    {
        /// <summary>
        /// Create conflict declared event
        /// </summary>
        public static IGameEvent CreateConflictDeclaredEvent(Game game, Conflict conflict)
        {
            return game.GetEvent(EventNames.OnConflictDeclared, new Dictionary<string, object>
            {
                { "conflict", conflict },
                { "attackingPlayer", conflict.attackingPlayer },
                { "defendingPlayer", conflict.defendingPlayer },
                { "conflictType", conflict.conflictType },
                { "ring", conflict.ring },
                { "province", conflict.conflictProvince }
            }, () => true);
        }

        /// <summary>
        /// Create defenders declared event
        /// </summary>
        public static IGameEvent CreateDefendersDeclaredEvent(Game game, Conflict conflict)
        {
            return game.GetEvent(EventNames.OnDefendersDeclared, new Dictionary<string, object>
            {
                { "conflict", conflict },
                { "defenders", conflict.defenders.ToList() },
                { "defendingPlayer", conflict.defendingPlayer }
            }, () => true);
        }

        /// <summary>
        /// Create conflict finished event
        /// </summary>
        public static IGameEvent CreateConflictFinishedEvent(Game game, Conflict conflict)
        {
            return game.GetEvent(EventNames.OnConflictFinished, new Dictionary<string, object>
            {
                { "conflict", conflict },
                { "winner", conflict.winner },
                { "loser", conflict.loser },
                { "wasUnopposed", conflict.conflictUnopposed }
            }, () => true);
        }
    }

    /// <summary>
    /// Conflict state management for UI and network sync
    /// </summary>
    [System.Serializable]
    public class ConflictState
    {
        public string conflictType;
        public string ringElement;
        public string provinceName;
        public List<string> attackerIds = new List<string>();
        public List<string> defenderIds = new List<string>();
        public int attackerSkill;
        public int defenderSkill;
        public bool isUnopposed;
        public string winnerId;
        public string loserId;
        public ConflictPhase currentPhase;
        public bool actionWindowOpen;
        public string currentPlayerTurn;
    }

    /// <summary>
    /// Phases of conflict resolution
    /// </summary>
    public enum ConflictPhase
    {
        Declaration,
        DefenderSelection,
        ActionWindow,
        Resolution,
        RingEffects,
        Cleanup,
        Completed
    }

    /// <summary>
    /// Conflict manager for tracking multiple conflicts and state
    /// </summary>
    public class ConflictManager : MonoBehaviour
    {
        [Header("Conflict Management")]
        public bool debugMode = false;
        public int maxConflictsPerTurn = 2;

        // Current state
        private Game game;
        private List<Conflict> resolvedConflicts = new List<Conflict>();
        private ConflictFlow currentConflictFlow;

        // Events
        public event Action<Conflict> OnConflictStarted;
        public event Action<Conflict> OnConflictFinished;
        public event Action<Conflict, ConflictPhase> OnConflictPhaseChanged;

        void Awake()
        {
            game = FindObjectOfType<Game>();
        }

        /// <summary>
        /// Start a new conflict
        /// </summary>
        public ConflictFlow StartConflict(Player attackingPlayer, bool canPass = true)
        {
            if (GetConflictsThisTurn(attackingPlayer) >= maxConflictsPerTurn)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"⚔️ {attackingPlayer.name} has reached max conflicts per turn");
                }
                return null;
            }

            var conflict = new Conflict(game, attackingPlayer, null, null, null, null);
            currentConflictFlow = new ConflictFlow(game, conflict, canPass);
            
            game.QueueStep(currentConflictFlow);
            
            OnConflictStarted?.Invoke(conflict);
            
            if (debugMode)
            {
                Debug.Log($"⚔️ Started conflict for {attackingPlayer.name}");
            }

            return currentConflictFlow;
        }

        /// <summary>
        /// Complete current conflict
        /// </summary>
        public void CompleteConflict(Conflict conflict)
        {
            resolvedConflicts.Add(conflict);
            currentConflictFlow = null;
            
            OnConflictFinished?.Invoke(conflict);
            
            if (debugMode)
            {
                Debug.Log($"⚔️ Completed conflict: {conflict.winner?.name ?? "No winner"} won {conflict.conflictType} at {conflict.ring?.element}");
            }
        }

        /// <summary>
        /// Get conflicts this turn for a player
        /// </summary>
        public int GetConflictsThisTurn(Player player)
        {
            return resolvedConflicts.Count(c => c.attackingPlayer == player);
        }

        /// <summary>
        /// Reset for new turn
        /// </summary>
        public void ResetForNewTurn()
        {
            resolvedConflicts.Clear();
            
            if (debugMode)
            {
                Debug.Log("⚔️ Reset conflicts for new turn");
            }
        }

        /// <summary>
        /// Get current conflict state for UI
        /// </summary>
        public ConflictState GetCurrentConflictState()
        {
            if (game.currentConflict == null)
                return null;

            var conflict = game.currentConflict;
            return new ConflictState
            {
                conflictType = conflict.conflictType,
                ringElement = conflict.ring?.element,
                provinceName = conflict.conflictProvince?.name,
                attackerIds = conflict.attackers.Select(c => c.id).ToList(),
                defenderIds = conflict.defenders.Select(c => c.id).ToList(),
                attackerSkill = conflict.attackerSkill,
                defenderSkill = conflict.defenderSkill,
                isUnopposed = conflict.conflictUnopposed,
                winnerId = conflict.winner?.id,
                loserId = conflict.loser?.id,
                actionWindowOpen = currentConflictFlow != null
            };
        }

        /// <summary>
        /// Get debug information
        /// </summary>
        public string GetDebugInfo()
        {
            var info = $"ConflictManager Debug Info:\n";
            info += $"Current Conflict: {(game.currentConflict != null ? "Active" : "None")}\n";
            info += $"Resolved This Turn: {resolvedConflicts.Count}\n";
            info += $"Max Per Turn: {maxConflictsPerTurn}\n";

            if (game.currentConflict != null)
            {
                var conflict = game.currentConflict;
                info += $"\nCurrent Conflict Details:\n";
                info += $"Type: {conflict.conflictType}\n";
                info += $"Ring: {conflict.ring?.element}\n";
                info += $"Province: {conflict.conflictProvince?.name}\n";
                info += $"Attackers: {conflict.attackers.Count}\n";
                info += $"Defenders: {conflict.defenders.Count}\n";
                info += $"Attacker Skill: {conflict.attackerSkill}\n";
                info += $"Defender Skill: {conflict.defenderSkill}\n";
            }

            return info;
        }
    }
}