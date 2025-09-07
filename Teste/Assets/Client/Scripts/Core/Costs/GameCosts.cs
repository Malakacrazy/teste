using UnityEngine;
using System.Collections.Generic;
using L5RGame.Events;
using L5RGame.EventSystem;

namespace L5RGame
{
    /// <summary>
    /// Manages game costs and cost-related functionality with event-driven support
    /// </summary>
    public class GameCosts : MonoBehaviour
    {
        private Game game;
        
        public void Initialize(Game gameInstance) 
        {
            game = gameInstance;
        }
        /// <summary>
        /// Creates a reduceable fate cost
        /// </summary>
        public ICost PayReduceableFateCost(int baseCost = 0)
        {
            var cost = new ReduceableFateCost(baseCost);
            cost.Initialize(game);
            return cost;
        }
        
        /// <summary>
        /// Creates a honor cost
        /// </summary>
        public ICost PayHonorCost(int amount)
        {
            var cost = new HonorCost(amount);
            cost.Initialize(game);
            return cost;
        }
        
        /// <summary>
        /// Creates a fate cost
        /// </summary>
        public ICost PayFateCost(int amount)
        {
            var cost = new FateCost(amount);
            cost.Initialize(game);
            return cost;
        }
        
        /// <summary>
        /// Creates a sacrifice cost
        /// </summary>
        public ICost SacrificeCard(object target = null)
        {
            var cost = new SacrificeCardCost(target);
            cost.Initialize(game);
            return cost;
        }
        
        /// <summary>
        /// Creates a bow cost
        /// </summary>
        public ICost BowCard(object target = null)
        {
            var cost = new BowCardCost(target);
            cost.Initialize(game);
            return cost;
        }
        
        /// <summary>
        /// Creates a discard cost
        /// </summary>
        public ICost DiscardCard(object target = null)
        {
            var cost = new DiscardCardCost(target);
            cost.Initialize(game);
            return cost;
        }
        
        /// <summary>
        /// Creates a return to hand cost
        /// </summary>
        public ICost ReturnToHand(object target = null)
        {
            var cost = new ReturnToHandCost(target);
            cost.Initialize(game);
            return cost;
        }

        /// <summary>
        /// Pay fate directly from player
        /// </summary>
        public void PayFate(Player player, int amount)
        {
            if (player != null && player.fate >= amount)
            {
                player.fate -= amount;
            }
        }
    }
    
    /// <summary>
    /// Base cost class with event-driven architecture support
    /// </summary>
    public abstract class BaseCost : ICost
    {
        protected IEventBus eventBus;
        protected IUnifiedEventSystem unifiedEventSystem;
        
        /// <summary>
        /// Initialize the cost with event system references
        /// </summary>
        /// <param name="game">Game instance for event system access</param>
        public virtual void Initialize(Game game)
        {
            if (game != null)
            {
                eventBus = game.GetEventBus();
                unifiedEventSystem = game.GetUnifiedEventSystem();
            }
        }
        
        public abstract bool CanPay(AbilityContext context);
        
        public virtual void Pay(AbilityContext context)
        {
            if (!CanPay(context))
            {
                PublishCostPaymentFailed(context, "Cannot pay cost");
                return;
            }
            
            try
            {
                PayCost(context);
                PublishCostPaid(context);
            }
            catch (System.Exception ex)
            {
                PublishCostPaymentFailed(context, $"Error paying cost: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Actual cost payment implementation - override in derived classes
        /// </summary>
        protected abstract void PayCost(AbilityContext context);
        
        public virtual string GetCostDescription() { return GetType().Name; }
        
        // Keep GetDescription for backwards compatibility
        public virtual string GetDescription() { return GetCostDescription(); }
        
        /// <summary>
        /// Get the cost type identifier for events
        /// </summary>
        protected virtual string GetCostType() { return GetType().Name.Replace("Cost", "").ToLower(); }
        
        /// <summary>
        /// Publish cost paid event
        /// </summary>
        protected virtual void PublishCostPaid(AbilityContext context)
        {
            if (eventBus == null) return;
            
            var costPaidEvent = new CostPaidEvent(
                game: context.Game,
                triggeredBy: context.player,
                cost: this,
                costType: GetCostType(),
                amountPaid: GetPaidAmount(context),
                cardPaid: GetPaidCard(context),
                context: context,
                source: this
            );
            
            // Publish as Handler event (during cost resolution)
            PublishEvent(costPaidEvent, TimingWindow.Handler);
        }
        
        /// <summary>
        /// Publish cost payment failed event
        /// </summary>
        protected virtual void PublishCostPaymentFailed(AbilityContext context, string reason)
        {
            if (eventBus == null) return;
            
            var failedEvent = new CostPaymentFailedEvent(
                game: context.Game,
                triggeredBy: context.player,
                cost: this,
                costType: GetCostType(),
                reason: reason,
                context: context,
                source: this
            );
            
            // Publish as Handler event
            PublishEvent(failedEvent, TimingWindow.Handler);
        }
        
        /// <summary>
        /// Get the amount paid for this cost (override for numeric costs)
        /// </summary>
        protected virtual int GetPaidAmount(AbilityContext context) { return 0; }
        
        /// <summary>
        /// Get the card paid for this cost (override for card costs)
        /// </summary>
        protected virtual BaseCard GetPaidCard(AbilityContext context) { return null; }
        
        /// <summary>
        /// Publish an event through the unified event system with timing awareness
        /// </summary>
        protected virtual void PublishEvent<T>(T gameEvent, TimingWindow window = TimingWindow.Handler) where T : GameEvent
        {
            if (gameEvent == null) return;
            
            try
            {
                // Use unified system if available for timing-aware processing
                if (unifiedEventSystem != null)
                {
                    unifiedEventSystem.PublishAtTiming(gameEvent, window);
                }
                // Fall back to regular event bus
                else if (eventBus != null)
                {
                    eventBus.Publish(gameEvent);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Failed to publish {typeof(T).Name}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Reduceable fate cost that can be modified by effects
    /// </summary>
    public class ReduceableFateCost : BaseCost, IReduceableCost
    {
        public int baseCost;
        
        public ReduceableFateCost(int cost = 0)
        {
            baseCost = cost;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            int actualCost = GetReducedCost(context);
            return context.player.fate >= actualCost;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            int actualCost = GetReducedCost(context);
            context.player.fate -= actualCost;
        }
        
        protected override int GetPaidAmount(AbilityContext context)
        {
            return GetReducedCost(context);
        }
        
        protected override string GetCostType()
        {
            return "fate";
        }
        
        public int GetReducedCost(AbilityContext context)
        {
            // Apply cost modifications here
            int reducedCost = baseCost;
            
            // Get cost modifications from player effects
            var costModifications = context.player.GetEffects("reduceFateCost");
            foreach (var modification in costModifications)
            {
                if (modification is System.Func<AbilityContext, int> func)
                {
                    reducedCost -= func(context);
                }
                else if (modification is int intValue)
                {
                    reducedCost -= intValue;
                }
            }
            
            int finalCost = Mathf.Max(0, reducedCost);
            
            // Publish cost reduction event if cost was reduced
            if (finalCost < baseCost)
            {
                PublishCostReduced(context, baseCost, finalCost);
            }
            
            return finalCost;
        }
        
        /// <summary>
        /// Publish cost reduced event
        /// </summary>
        private void PublishCostReduced(AbilityContext context, int originalCost, int reducedCost)
        {
            if (eventBus == null) return;
            
            var costReducedEvent = new CostReducedEvent(
                game: context.Game,
                triggeredBy: context.player,
                originalCost: originalCost,
                reducedCost: reducedCost,
                costType: "fate",
                reductionSource: null, // Could be enhanced to track the specific source
                source: this
            );
            
            PublishEvent(costReducedEvent, TimingWindow.Handler);
        }
        
        public override string GetCostDescription()
        {
            return $"Pay {baseCost} fate";
        }
    }
    
    /// <summary>
    /// Honor cost
    /// </summary>
    public class HonorCost : BaseCost
    {
        public int amount;
        
        public HonorCost(int cost)
        {
            amount = cost;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            return context.player.honor >= amount;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            context.player.honor -= amount;
        }
        
        protected override int GetPaidAmount(AbilityContext context)
        {
            return amount;
        }
        
        protected override string GetCostType()
        {
            return "honor";
        }
        
        public override string GetCostDescription()
        {
            return $"Pay {amount} honor";
        }
    }
    
    /// <summary>
    /// Fate cost
    /// </summary>
    public class FateCost : BaseCost
    {
        public int amount;
        
        public FateCost(int cost)
        {
            amount = cost;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            return context.player.fate >= amount;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            context.player.fate -= amount;
        }
        
        protected override int GetPaidAmount(AbilityContext context)
        {
            return amount;
        }
        
        protected override string GetCostType()
        {
            return "fate";
        }
        
        public override string GetCostDescription()
        {
            return $"Pay {amount} fate";
        }
    }
    
    /// <summary>
    /// Sacrifice card cost
    /// </summary>
    public class SacrificeCardCost : BaseCost
    {
        public object target;
        
        public SacrificeCardCost(object cardTarget = null)
        {
            target = cardTarget;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            if (target != null) return true;
            
            // Check if player has cards that can be sacrificed
            return context.player.cardsInPlay.Count > 0;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                context.player.MoveCard(card, Locations.DynastyDiscardPile);
            }
        }
        
        protected override BaseCard GetPaidCard(AbilityContext context)
        {
            return target as BaseCard;
        }
        
        protected override string GetCostType()
        {
            return "sacrifice";
        }
        
        public override string GetCostDescription()
        {
            return "Sacrifice a card";
        }
    }
    
    /// <summary>
    /// Bow card cost
    /// </summary>
    public class BowCardCost : BaseCost
    {
        public object target;
        
        public BowCardCost(object cardTarget = null)
        {
            target = cardTarget;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                return !card.bowed;
            }
            return true;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                card.bowed = true;
            }
        }
        
        protected override BaseCard GetPaidCard(AbilityContext context)
        {
            return target as BaseCard;
        }
        
        protected override string GetCostType()
        {
            return "bow";
        }
        
        public override string GetCostDescription()
        {
            return "Bow a card";
        }
    }
    
    /// <summary>
    /// Discard card cost
    /// </summary>
    public class DiscardCardCost : BaseCost
    {
        public object target;
        
        public DiscardCardCost(object cardTarget = null)
        {
            target = cardTarget;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            return target != null || context.player.hand.Count > 0;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                context.player.MoveCard(card, Locations.ConflictDiscardPile);
            }
        }
        
        protected override BaseCard GetPaidCard(AbilityContext context)
        {
            return target as BaseCard;
        }
        
        protected override string GetCostType()
        {
            return "discard";
        }
        
        public override string GetCostDescription()
        {
            return "Discard a card";
        }
    }
    
    /// <summary>
    /// Return to hand cost
    /// </summary>
    public class ReturnToHandCost : BaseCost
    {
        public object target;
        
        public ReturnToHandCost(object cardTarget = null)
        {
            target = cardTarget;
        }
        
        public override bool CanPay(AbilityContext context)
        {
            return target is BaseCard;
        }
        
        protected override void PayCost(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                context.player.MoveCard(card, Locations.Hand);
            }
        }
        
        protected override BaseCard GetPaidCard(AbilityContext context)
        {
            return target as BaseCard;
        }
        
        protected override string GetCostType()
        {
            return "return_to_hand";
        }
        
        public override string GetCostDescription()
        {
            return "Return a card to hand";
        }
    }
}
