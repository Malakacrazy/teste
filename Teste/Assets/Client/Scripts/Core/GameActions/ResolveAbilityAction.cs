using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    public class NoCostsAbilityResolver : AbilityResolver
    {
        public AbilityContext Context => context;
        public bool Cancelled => cancelled;
        public List<object> Events => events;
        private bool _initiateAbility;
        public bool InitiateAbility { get => _initiateAbility; set => _initiateAbility = value; }
        
        public NoCostsAbilityResolver(Game gameInstance, AbilityContext abilityContext) : base(gameInstance, abilityContext)
        {
        }
        
        public void Initialise()
        {
            Pipeline.Initialize(new List<BaseStep>
            {
                new SimpleStep(Game, () => { CreateSnapshot(); return true; }),
                new SimpleStep(Game, () => { OpenInitiateAbilityEventWindow(); return true; }),
                new SimpleStep(Game, () => { RefillProvinces(); return true; })
            });
        }

        public void OpenInitiateAbilityEventWindow()
        {
            var events = new List<GameEvent>
            {
                Game.GetEvent(EventNames.OnCardAbilityInitiated, 
                    new Dictionary<string, object> { 
                        ["card"] = Context.Source, 
                        ["ability"] = Context.Ability, 
                        ["context"] = Context 
                    }, 
                    () =>
                    {
                        Game.QueueSimpleStep(() => { ResolveTargets(); return true; });
                        Game.QueueSimpleStep(() => { InitiateAbilityEffects(); return true; });
                        Game.QueueSimpleStep(() => { ExecuteHandler(); return true; });
                        return true;
                    })
            };

            if (Context.Ability.IsTriggeredAbility() && !Context.SubResolution)
            {
                events.Add(Game.GetEvent(EventNames.OnCardAbilityTriggered, new Dictionary<string, object>
                {
                    ["player"] = Context.Player,
                    ["card"] = Context.Source,
                    ["context"] = Context
                }, () => true));
            }

            Game.OpenEventWindow(events);
        }

        public void InitiateAbilityEffects()
        {
            if (Cancelled)
            {
                foreach (var eventObj in Events)
                {
                    if (eventObj is GameEvent gameEvent)
                    {
                        gameEvent.Cancel();
                    }
                }
                return;
            }
            else if (Context.Ability.Max != null && !Context.SubResolution)
            {
                Context.Player.IncrementAbilityMax(Context.Ability.MaxIdentifier);
            }

            Context.Ability.DisplayMessage(Context, "resolves");
            Game.OpenEventWindow(new InitiateCardAbilityEvent(
                new { card = Context.Source, context = Context },
                () => InitiateAbility = true));
        }
    }

    public interface IResolveAbilityProperties : ICardActionProperties
    {
        CardAbility Ability { get; set; }
        bool SubResolution { get; set; }
        Player Player { get; set; }
        GameEvent Event { get; set; }
    }

    public class ResolveAbilityProperties : CardActionProperties, IResolveAbilityProperties
    {
        public CardAbility Ability { get; set; }
        public bool SubResolution { get; set; }
        public Player Player { get; set; }
        public GameEvent Event { get; set; }
    }

    public partial class ResolveAbilityAction : CardGameAction
    {
        #region Constructors
        
        public ResolveAbilityAction() : base()
        {
            Initialize();
        }
        
        public ResolveAbilityAction(CardActionProperties properties) : base(properties)
        {
            Initialize();
        }
        
        public ResolveAbilityAction(System.Func<AbilityContext, CardActionProperties> factory) : base(factory)
        {
            Initialize();
        }
        
        #endregion
        
        #region Initialization
        
        protected override void Initialize()
        {
            base.Initialize();
            actionName = "resolveAbility";
            
            var resolveAbilityProps = new ResolveAbilityProperties
            {
                Ability = null,
                SubResolution = false
            };
            // Create base GameActionProperties with the same target
            defaultProperties = new GameAction.GameActionProperties
            {
                target = resolveAbilityProps.Target
            };
        }
        
        #endregion

        public (string, object[]) GetEffectMessage(AbilityContext context)
        {
            var properties = GetProperties(context) as IResolveAbilityProperties;
            return ("resolve {0}'s {1} ability", new object[] { properties.Target, properties.Ability?.ToString() });
        }

        public bool CanAffect(DrawCard card, AbilityContext context, GameAction.GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveAbilityProperties;
            var ability = properties.Ability as TriggeredAbility;
            var player = properties.Player ?? context.Player;
            var newContextEvent = properties.Event;

            if (!base.CanAffect(card, context, additionalProperties) || ability == null || 
                (!properties.SubResolution && player.IsAbilityAtMax(ability.MaxIdentifier)))
            {
                return false;
            }

            var newContext = ability.CreateContext(player, newContextEvent);
            if (ability.Targets.Count == 0)
            {
                return ability.GameAction.Count == 0 || ability.GameAction.Any(action => action.HasLegalTarget(newContext));
            }

            return ability.CanResolveTargets(newContext);
        }

        protected override bool EventHandler(GameEvent gameEvent, GameActionProperties additionalProperties = null)
        {
            var properties = GetProperties(gameEvent.context, additionalProperties) as IResolveAbilityProperties;
            if (properties?.Ability is TriggeredAbility ability)
            {
                var player = properties.Player ?? gameEvent.context.Player;
                var newContextEvent = properties.Event;
                var newContext = ability.CreateContext(player, newContextEvent);
                newContext.SubResolution = properties.SubResolution;
                gameEvent.context.Game.QueueStep(new NoCostsAbilityResolver(gameEvent.context.Game, newContext));
                LogExecution("Resolved {0} ability for {1}", ability.Title, player.name);
                return true;
            }
            return false;
        }

        public bool HasTargetsChosenByInitiatingPlayer(AbilityContext context, object additionalProperties = null)
        {
            var properties = GetProperties(context, additionalProperties) as IResolveAbilityProperties;
            return properties.Ability.HasTargetsChosenByInitiatingPlayer(context);
        }
    }
}
