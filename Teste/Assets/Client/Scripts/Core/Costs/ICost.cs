namespace L5RGame
{
    /// <summary>
    /// Interface for ability costs
    /// </summary>
    public interface ICost
    {
        /// <summary>
        /// Check if the cost can be paid
        /// </summary>
        /// <param name="context">Ability context</param>
        /// <returns>True if cost can be paid</returns>
        bool CanPay(AbilityContext context);
        
        /// <summary>
        /// Pay the cost
        /// </summary>
        /// <param name="context">Ability context</param>
        void Pay(AbilityContext context);
        
        /// <summary>
        /// Get description of the cost for UI display
        /// </summary>
        /// <returns>Cost description</returns>
        string GetCostDescription();
    }
}
