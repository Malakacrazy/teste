using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Extension methods for BaseCard to provide missing functionality
    /// </summary>
    public static class BaseCardExtensions
    {
        public static string GetCardType(this BaseCard card)
        {
            return CardTypes.Character; // Placeholder - return default type
        }
        
        public static CardStateSnapshot CreateSnapshot(this BaseCard card)
        {
            return new CardStateSnapshot(card);
        }
        
        public static bool IsLimited(this BaseCard card)
        {
            return false; // Placeholder
        }
        
        public static string location => "hand"; // Placeholder property
        
        public static void MoveTo(this BaseCard card, string targetLocation)
        {
            // Placeholder implementation
        }
    }
    
    /// <summary>
    /// Results from cost resolution
    /// </summary>
    [System.Serializable]
    public class CostResults
    {
        public bool cancelled = false;
        public bool canCancel = true;
        public List<GameEvent> events = new List<GameEvent>();
        public bool playCosts = true;
        public bool triggerCosts = true;
        public bool success = false;
    }

    /// <summary>
    /// Results from target resolution
    /// </summary>
    [System.Serializable]
    public class TargetResults
    {
        public bool cancelled = false;
        public object delayTargeting = null;
        public bool payCostsFirst = false;
        public bool noCostsFirstButton = false;
        public Dictionary<string, object> targets = new Dictionary<string, object>();
        public bool success = false;
    }

    /// <summary>
    /// Card state snapshot for ability context
    /// </summary>
    [System.Serializable]
    public class CardStateSnapshot
    {
        public string location;
        public string cardType;
        public bool facedown;
        public Player controller;
        public Player owner;

        public CardStateSnapshot(BaseCard card)
        {
            // Placeholder - BaseCard properties don't exist yet
            location = "unknown";
            cardType = "unknown";
            facedown = false;
            controller = null;
            owner = null;
        }
    }

    /// <summary>
    /// Handles the complete resolution pipeline for card abilities.
    /// Manages targeting, cost payment, and execution in proper order.
    /// </summary>
    public class AbilityResolver : BaseStepWithPipeline, IGameStep
    {
        public AbilityContext context;
        public bool canCancel = true;
        public bool initiateAbility = false;
        public bool passPriority = false;
        public List<object> events = new List<object>();
        public List<ProvinceRefill> provincesToRefill = new List<ProvinceRefill>();
        public TargetResults targetResults = new TargetResults();
        public CostResults costResults;

        // State tracking
        public bool cancelled = false;
        private bool completed = false;

        public AbilityResolver(Game gameInstance, AbilityContext abilityContext) : base(gameInstance)
        {
            context = abilityContext;
            costResults = GetCostResults();
            Initialize();
        }

        public new void Initialize()
        {
            // Simplified initialization - no complex pipeline for now
        }

        public override bool Execute()
        {
            if (!completed)
            {
                // Simplified execution path
                CreateSnapshot();
                ResolveEarlyTargets();
                CheckForCancel();
                OpenInitiateAbilityEventWindow();
                RefillProvinces();
                completed = true;
            }
            return completed;
        }

        public new bool IsComplete()
        {
            return completed;
        }

        public bool CreateSnapshot()
        {
            // Simplified snapshot creation
            if (context.source is BaseCard card)
            {
                // Store snapshot in context - need to add this property to AbilityContext
                var snapshot = card.CreateSnapshot();
                context.SetSelect("cardStateWhenInitiated", snapshot);
            }
            
            return true;
        }

        public bool OpenInitiateAbilityEventWindow()
        {
            if (cancelled)
            {
                return true;
            }

            string eventName = EventNames.Unnamed;
            var eventProps = new Dictionary<string, object>();

            // Simplified ability checking - BaseAbility has these methods now
            if (context.ability.IsCardAbility())
            {
                eventName = EventNames.OnCardAbilityInitiated;
                eventProps = new Dictionary<string, object>
                {
                    { "card", context.source },
                    { "ability", context.ability },
                    { "context", context }
                };

                if (context.ability.IsCardPlayed())
                {
                    var cardPlayedEvent = game.GetEvent(EventNames.OnCardPlayed, new Dictionary<string, object>
                    {
                        { "player", context.player },
                        { "card", context.source },
                        { "context", context },
                        { "originalLocation", "hand" }, // Placeholder
                        { "playType", context.playType },
                        { "resolver", this }
                    }, () => true);
                    events.Add(cardPlayedEvent);
                }

                if (context.ability.IsTriggeredAbility())
                {
                    var triggeredEvent = game.GetEvent(EventNames.OnCardAbilityTriggered, new Dictionary<string, object>
                    {
                        { "player", context.player },
                        { "card", context.source },
                        { "context", context }
                    }, () => true);
                    events.Add(triggeredEvent);
                }
            }

            var initiateEvent = game.GetEvent(eventName, eventProps, () => { QueueInitiateAbilitySteps(); return true; });
            events.Add(initiateEvent);

            // Simplified event window
            return true;
        }

        public bool QueueInitiateAbilitySteps()
        {
            // Simplified step queueing
            ResolveCosts();
            PayCosts();
            CheckCostsWerePaid();
            ResolveTargets();
            CheckForCancel();
            InitiateAbilityEffects();
            ExecuteHandler();
            MoveEventCardToDiscard();
            return true;
        }

        public bool ResolveEarlyTargets()
        {
            context.SetStage(Stages.PreTarget);
            
            if (!context.ability.cannotTargetFirst)
            {
                targetResults = context.ability.ResolveTargets(context);
            }
            
            return true;
        }

        public bool CheckForCancel()
        {
            if (cancelled)
            {
                return true;
            }

            cancelled = targetResults.cancelled;
            return true;
        }

        public bool ResolveCosts()
        {
            if (cancelled)
            {
                return true;
            }

            costResults.canCancel = canCancel;
            context.SetStage(Stages.Cost);
            context.ability.ResolveCosts(context, costResults);
            
            return true;
        }

        public CostResults GetCostResults()
        {
            return new CostResults
            {
                cancelled = false,
                canCancel = canCancel,
                events = new List<GameEvent>(),
                playCosts = true,
                triggerCosts = true
            };
        }

        public bool PayCosts()
        {
            if (cancelled)
            {
                return true;
            }
            
            if (costResults.cancelled)
            {
                cancelled = true;
                return true;
            }

            passPriority = true;
            
            if (costResults.events.Count > 0)
            {
                game.OpenEventWindow(costResults.events);
            }
            
            return true;
        }

        public bool CheckCostsWerePaid()
        {
            if (cancelled)
            {
                return true;
            }

            cancelled = costResults.events.Any(gameEvent => gameEvent.cancelled);

            if (cancelled)
            {
                game.AddMessage("{0} attempted to use {1}, but did not successfully pay the required costs", 
                               context.player.name, GetSourceName());
            }
            
            return true;
        }

        public bool ResolveTargets()
        {
            if (cancelled)
            {
                return true;
            }

            context.SetStage(Stages.Target);

            if (!context.ability.HasLegalTargets(context))
            {
                game.AddMessage("{0} attempted to use {1}, but there are insufficient legal targets", 
                               context.player.name, GetSourceName());
                cancelled = true;
            }
            else if ((bool?)targetResults.delayTargeting == true)
            {
                targetResults = context.ability.ResolveRemainingTargets(context, targetResults);
            }
            else if (targetResults.payCostsFirst || !context.ability.CheckAllTargets(context))
            {
                targetResults = context.ability.ResolveTargets(context);
            }
            
            return true;
        }

        public bool InitiateAbilityEffects()
        {
            if (cancelled)
            {
                foreach (var eventObj in events)
                {
                    if (eventObj is IGameEvent gameEvent)
                    {
                        gameEvent.Cancel();
                    }
                }
                return true;
            }

            if (context.ability.IsCardPlayed())
            {
                if (context.source is BaseCard card && card.IsLimited())
                {
                    context.player.limitedPlayed += 1;
                }

                if (game.currentConflict != null)
                {
                    // Placeholder - Conflict doesn't have AddCardPlayed method yet
                    // game.currentConflict.AddCardPlayed(context.player, (BaseCard)context.source);
                }
            }

            // Get stored snapshot from context
            var storedSnapshot = context.GetSelect("cardStateWhenInitiated");
            
            if (context.ability.limit != null && 
                context.source is BaseCard sourceCard &&
                storedSnapshot != null)
            {
                context.ability.limit.Increment(context.player);
            }

            if (context.ability.max > 0)
            {
                context.player.IncrementAbilityMax(context.ability.maxIdentifier);
            }

            context.ability.DisplayMessage(context);

            if (context.ability.IsTriggeredAbility())
            {
                if (context.ability.IsCardPlayed() && context.source is BaseCard eventCard)
                {
                    // Simplified card movement using Player's MoveCard method
                    context.player.MoveCard(eventCard, Locations.BeingPlayed);
                }

                var initiateEvent = new InitiateCardAbilityEvent(
                    new Dictionary<string, object>
                    {
                        { "card", context.source },
                        { "context", context }
                    },
                    () => initiateAbility = true
                );
                
                // Simplified event handling
                initiateAbility = true;
            }
            else
            {
                initiateAbility = true;
            }
            
            return true;
        }

        public bool ExecuteHandler()
        {
            if (cancelled || !initiateAbility)
            {
                return true;
            }

            context.SetStage(Stages.Effect);
            context.ability.ExecuteHandler(context);
            
            return true;
        }

        public bool MoveEventCardToDiscard()
        {
            if (context.source is BaseCard card)
            {
                // Simplified - just move to discard pile
                context.player.MoveCard(card, Locations.ConflictDiscardPile);
            }
            
            return true;
        }

        public bool RefillProvinces()
        {
            context.Refill();
            return true;
        }

        private string GetSourceName()
        {
            if (context.source is BaseCard card)
            {
                return card.name;
            }
            if (context.source is Ring ring)
            {
                return ring.name ?? "Ring";
            }
            return context.source?.ToString() ?? "Unknown Source";
        }
    }

    [System.Serializable]
    public class ProvinceRefill
    {
        public Player player;
        public string location;

        public ProvinceRefill(Player playerInstance, string provinceLocation)
        {
            player = playerInstance;
            location = provinceLocation;
        }
    }
}
