using UnityEngine;
using System.Collections.Generic;

namespace L5RGame
{
    /// <summary>
    /// Manages game costs and cost-related functionality
    /// </summary>
    public class GameCosts : MonoBehaviour
    {
        /// <summary>
        /// Creates a reduceable fate cost
        /// </summary>
        public ICost PayReduceableFateCost(int baseCost = 0)
        {
            return new ReduceableFateCost(baseCost);
        }
        
        /// <summary>
        /// Creates a honor cost
        /// </summary>
        public ICost PayHonorCost(int amount)
        {
            return new HonorCost(amount);
        }
        
        /// <summary>
        /// Creates a fate cost
        /// </summary>
        public ICost PayFateCost(int amount)
        {
            return new FateCost(amount);
        }
        
        /// <summary>
        /// Creates a sacrifice cost
        /// </summary>
        public ICost SacrificeCard(object target = null)
        {
            return new SacrificeCardCost(target);
        }
        
        /// <summary>
        /// Creates a bow cost
        /// </summary>
        public ICost BowCard(object target = null)
        {
            return new BowCardCost(target);
        }
        
        /// <summary>
        /// Creates a discard cost
        /// </summary>
        public ICost DiscardCard(object target = null)
        {
            return new DiscardCardCost(target);
        }
        
        /// <summary>
        /// Creates a return to hand cost
        /// </summary>
        public ICost ReturnToHand(object target = null)
        {
            return new ReturnToHandCost(target);
        }
    }
    
    /// <summary>
    /// Base cost class
    /// </summary>
    public abstract class BaseCost : ICost
    {
        public abstract bool CanPay(AbilityContext context);
        public abstract void Pay(AbilityContext context);
        public virtual string GetCostDescription() { return GetType().Name; }
        
        // Keep GetDescription for backwards compatibility
        public virtual string GetDescription() { return GetCostDescription(); }
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
        
        public override void Pay(AbilityContext context)
        {
            int actualCost = GetReducedCost(context);
            context.player.fate -= actualCost;
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
            
            return Mathf.Max(0, reducedCost);
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
        
        public override void Pay(AbilityContext context)
        {
            context.player.honor -= amount;
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
        
        public override void Pay(AbilityContext context)
        {
            context.player.fate -= amount;
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
        
        public override void Pay(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                context.player.MoveCard(card, Locations.DynastyDiscardPile);
            }
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
        
        public override void Pay(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                card.bowed = true;
            }
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
        
        public override void Pay(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                context.player.MoveCard(card, Locations.ConflictDiscardPile);
            }
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
        
        public override void Pay(AbilityContext context)
        {
            if (target is BaseCard card)
            {
                context.player.MoveCard(card, Locations.Hand);
            }
        }
        
        public override string GetCostDescription()
        {
            return "Return a card to hand";
        }
    }
}
