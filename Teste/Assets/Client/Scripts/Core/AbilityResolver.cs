using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Results from cost resolution
    /// </summary>
    [System.Serializable]
    public class CostResults
    {
        public bool cancelled = false;
        public bool canCancel = true;
        public List<object> events = new List<object>();
        public bool playCosts = true;
        public bool triggerCosts = true;
    }

    /// <summary>
    /// Results from target resolution
    /// </summary>
    [System.Serializable]
    public class TargetResults
    {
        public bool cancelled = false;
        public bool delayTargeting = false;
        public bool payCostsFirst = false;
        public Dictionary<string, object> targets = new Dictionary<string, object>();
    }

    /// <summary>
    /// Handles the complete resolution pipeline for card abilities.
    /// Manages targeting, cost payment, and execution in proper order.
    /// </summary>
    public class AbilityResolver : BaseStepWithPipeline
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

        public AbilityResolver(Game game, AbilityContext context) : base(game)
        {
            this.context = context;
            this.costResults = GetCostResults();
            Initialize();
        }

        protected override void Initialize()
        {
            pipeline.Initialize(new List<IGameStep>
            {
                new SimpleStep(game, CreateSnapshot),
                new SimpleStep(game, ResolveEarlyTargets),
                new SimpleStep(game, CheckForCancel),
                new SimpleStep(game, OpenInitiateAbilityEventWindow),
                new SimpleStep(game, RefillProvinces)
            });
        }

        public bool CreateSnapshot()
        {
            var cardTypes = new List<string> { CardTypes.Character, CardTypes.Holding, CardTypes.Attachment };
            
            if (context.source is BaseCard card && cardTypes.Contains(card.GetCardType()))
            {
                context.cardStateWhenInitiated = card.CreateSnapshot();
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
                        { "originalLocation", ((BaseCard)context.source).location },
                        { "playType", context.playType },
                        { "resolver", this }
                    });
                    events.Add(cardPlayedEvent);
                }

                if (context.ability.IsTriggeredAbility())
                {
                    var triggeredEvent = game.GetEvent(EventNames.OnCardAbilityTriggered, new Dictionary<string, object>
                    {
                        { "player", context.player },
                        { "card", context.source },
                        { "context", context }
                    });
                    events.Add(triggeredEvent);
                }
            }

            var initiateEvent = game.GetEvent(eventName, eventProps, QueueInitiateAbilitySteps);
            events.Add(initiateEvent);

            game.QueueStep(new InitiateAbilityEventWindow(game, events));
            
            return true;
        }

        public void QueueInitiateAbilitySteps()
        {
            QueueStep(new SimpleStep(game, ResolveCosts));
            QueueStep(new SimpleStep(game, PayCosts));
            QueueStep(new SimpleStep(game, CheckCostsWerePaid));
            QueueStep(new SimpleStep(game, ResolveTargets));
            QueueStep(new SimpleStep(game, CheckForCancel));
            QueueStep(new SimpleStep(game, InitiateAbilityEffects));
            QueueStep(new SimpleStep(game, ExecuteHandler));
            QueueStep(new SimpleStep(game, MoveEventCardToDiscard));
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
                events = new List<object>(),
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

            cancelled = costResults.events.Any(eventObj => 
            {
                if (eventObj is IGameEvent gameEvent)
                {
                    return gameEvent.GetResolutionEvent()?.cancelled ?? false;
                }
                return false;
            });

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
            else if (targetResults.delayTargeting)
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
                    game.currentConflict.AddCardPlayed(context.player, (BaseCard)context.source);
                }
            }

            if (context.ability.limit != null && 
                context.source is BaseCard sourceCard && 
                sourceCard.location != Locations.Hand &&
                (context.cardStateWhenInitiated == null || 
                 context.cardStateWhenInitiated.location == sourceCard.location))
            {
                context.ability.limit.Increment(context.player);
            }

            if (context.ability.max != null)
            {
                context.player.IncrementAbilityMax(context.ability.maxIdentifier);
            }

            context.ability.DisplayMessage(context);

            if (context.ability.IsTriggeredAbility())
            {
                if (context.ability.IsCardPlayed() && context.source is BaseCard eventCard)
                {
                    var moveAction = game.actions.MoveCard(new Dictionary<string, object>
                    {
                        { "destination", Locations.BeingPlayed }
                    });
                    moveAction.Resolve(eventCard, context);
                }

                var initiateEvent = new InitiateCardAbilityEvent(
                    new Dictionary<string, object>
                    {
                        { "card", context.source },
                        { "context", context }
                    },
                    () => initiateAbility = true
                );
                
                game.OpenThenEventWindow(initiateEvent);
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
            if (context.source is BaseCard card && card.location == Locations.BeingPlayed)
            {
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
                return ring.name;
            }
            return context.source?.ToString() ?? "Unknown Source";
        }
    }

    [System.Serializable]
    public class ProvinceRefill
    {
        public Player player;
        public string location;

        public ProvinceRefill(Player player, string location)
        {
            this.player = player;
            this.location = location;
        }
    }

    public interface IGameEvent
    {
        void Cancel();
        IGameEvent GetResolutionEvent();
        bool cancelled { get; set; }
    }
}
