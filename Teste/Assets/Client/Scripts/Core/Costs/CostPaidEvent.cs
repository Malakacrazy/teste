using System;
using System.Collections.Generic;

namespace L5RGame.Events
{
    /// <summary>
    /// Event published when a cost is paid during ability execution
    /// </summary>
    [Serializable]
    public class CostPaidEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public CostPaidEvent() : base() { }
        
        /// <summary>
        /// The cost that was paid
        /// </summary>
        public ICost Cost { get; private set; }
        
        /// <summary>
        /// Type of cost (fate, honor, card, etc.)
        /// </summary>
        public string CostType { get; private set; }
        
        /// <summary>
        /// Amount paid for numeric costs
        /// </summary>
        public int AmountPaid { get; private set; }
        
        /// <summary>
        /// Card involved in the cost (if applicable)
        /// </summary>
        public BaseCard CardPaid { get; private set; }
        
        /// <summary>
        /// Ability context where cost was paid
        /// </summary>
        public AbilityContext Context { get; private set; }
        
        public override string EventName => "cost_paid";
        
        /// <summary>
        /// Initialize cost paid event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who paid the cost</param>
        /// <param name="cost">Cost that was paid</param>
        /// <param name="costType">Type of cost</param>
        /// <param name="amountPaid">Amount paid (for numeric costs)</param>
        /// <param name="cardPaid">Card involved (if applicable)</param>
        /// <param name="context">Ability context</param>
        /// <param name="source">Source of the cost payment</param>
        public CostPaidEvent(Game game, Player triggeredBy, ICost cost, string costType, 
            int amountPaid = 0, BaseCard cardPaid = null, AbilityContext context = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            Cost = cost;
            CostType = costType;
            AmountPaid = amountPaid;
            CardPaid = cardPaid;
            Context = context;
            
            // Add specific event data
            AddEventData("cost_type", costType);
            AddEventData("amount_paid", amountPaid);
            AddEventData("cost_description", cost?.GetCostDescription() ?? "Unknown cost");
            
            if (cardPaid != null)
            {
                AddEventData("card_paid", cardPaid.name);
                AddEventData("card_id", cardPaid.id);
            }
            
            if (context?.source != null)
            {
                AddEventData("ability_source", context.source.ToString());
            }
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["cost_type"] = CostType;
            data["amount_paid"] = AmountPaid;
            data["cost_description"] = Cost?.GetCostDescription() ?? "Unknown cost";
            
            if (CardPaid != null)
            {
                data["card_paid"] = CardPaid.name;
                data["card_id"] = CardPaid.id;
            }
            
            return data;
        }
    }
    
    /// <summary>
    /// Event published when a cost cannot be paid
    /// </summary>
    [Serializable]
    public class CostPaymentFailedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public CostPaymentFailedEvent() : base() { }
        
        /// <summary>
        /// The cost that could not be paid
        /// </summary>
        public ICost Cost { get; private set; }
        
        /// <summary>
        /// Type of cost that failed
        /// </summary>
        public string CostType { get; private set; }
        
        /// <summary>
        /// Reason the cost could not be paid
        /// </summary>
        public string Reason { get; private set; }
        
        /// <summary>
        /// Ability context where cost payment failed
        /// </summary>
        public AbilityContext Context { get; private set; }
        
        public override string EventName => "cost_payment_failed";
        
        /// <summary>
        /// Initialize cost payment failed event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player who tried to pay the cost</param>
        /// <param name="cost">Cost that could not be paid</param>
        /// <param name="costType">Type of cost</param>
        /// <param name="reason">Reason for failure</param>
        /// <param name="context">Ability context</param>
        /// <param name="source">Source of the cost payment attempt</param>
        public CostPaymentFailedEvent(Game game, Player triggeredBy, ICost cost, string costType, 
            string reason, AbilityContext context = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            Cost = cost;
            CostType = costType;
            Reason = reason;
            Context = context;
            
            // Add specific event data
            AddEventData("cost_type", costType);
            AddEventData("reason", reason);
            AddEventData("cost_description", cost?.GetCostDescription() ?? "Unknown cost");
            
            if (context?.source != null)
            {
                AddEventData("ability_source", context.source.ToString());
            }
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["cost_type"] = CostType;
            data["reason"] = Reason;
            data["cost_description"] = Cost?.GetCostDescription() ?? "Unknown cost";
            
            return data;
        }
    }
    
    /// <summary>
    /// Event published when a cost is reduced by an effect
    /// </summary>
    [Serializable]
    public class CostReducedEvent : GameEvent
    {
        /// <summary>
        /// Parameterless constructor for object pooling
        /// </summary>
        public CostReducedEvent() : base() { }
        
        /// <summary>
        /// Original cost amount
        /// </summary>
        public int OriginalCost { get; private set; }
        
        /// <summary>
        /// Reduced cost amount
        /// </summary>
        public int ReducedCost { get; private set; }
        
        /// <summary>
        /// Amount of reduction
        /// </summary>
        public int ReductionAmount { get; private set; }
        
        /// <summary>
        /// Type of cost that was reduced
        /// </summary>
        public string CostType { get; private set; }
        
        /// <summary>
        /// Source of the cost reduction
        /// </summary>
        public object ReductionSource { get; private set; }
        
        public override string EventName => "cost_reduced";
        
        /// <summary>
        /// Initialize cost reduced event
        /// </summary>
        /// <param name="game">Game instance</param>
        /// <param name="triggeredBy">Player benefiting from the reduction</param>
        /// <param name="originalCost">Original cost amount</param>
        /// <param name="reducedCost">Reduced cost amount</param>
        /// <param name="costType">Type of cost</param>
        /// <param name="reductionSource">Source of the reduction</param>
        /// <param name="source">Source of the event</param>
        public CostReducedEvent(Game game, Player triggeredBy, int originalCost, int reducedCost, 
            string costType, object reductionSource = null, object source = null) 
            : base(game, triggeredBy, source)
        {
            OriginalCost = originalCost;
            ReducedCost = reducedCost;
            ReductionAmount = originalCost - reducedCost;
            CostType = costType;
            ReductionSource = reductionSource;
            
            // Add specific event data
            AddEventData("original_cost", originalCost);
            AddEventData("reduced_cost", reducedCost);
            AddEventData("reduction_amount", ReductionAmount);
            AddEventData("cost_type", costType);
            
            if (reductionSource != null)
            {
                AddEventData("reduction_source", reductionSource.ToString());
            }
        }
        
        /// <summary>
        /// Get event data for analytics and logging
        /// </summary>
        public override Dictionary<string, object> GetData()
        {
            var data = base.GetData();
            data["original_cost"] = OriginalCost;
            data["reduced_cost"] = ReducedCost;
            data["reduction_amount"] = ReductionAmount;
            data["cost_type"] = CostType;
            
            return data;
        }
    }
}