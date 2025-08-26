using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Extension methods for easy ability resolver creation
    /// </summary>
    public static class AbilityResolverExtensions
    {
        public static AbilityResolver ResolveAbility(this Game game, AbilityContext context)
        {
            var resolver = new AbilityResolver(game, context);
            game.QueueStep(resolver);
            return resolver;
        }

        public static AbilityResolver ResolveCardAction(this Game game, BaseCard card, Player player, object ability)
        {
            var context = AbilityContext.CreateCardContext(game, card, player, ability);
            return game.ResolveAbility(context);
        }

        public static AbilityResolver ResolveRingEffect(this Game game, Ring ring, Player player)
        {
            var context = AbilityContext.CreateRingContext(game, ring, player);
            return game.ResolveAbility(context);
        }
    }
}
