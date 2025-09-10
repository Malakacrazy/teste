using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace L5RGame
{
    /// <summary>
    /// Setup Phase implementation for game initialization
    /// </summary>
    public class SetupPhase : Phase
    {
        public SetupPhase(Game game) : base(game, "setup")
        {
            game.currentPhase = Name;
            
            // Initialize the pipeline directly instead of using InitializePhase
            Pipeline.Initialize(new List<IGameStep>
            {
                new SimpleStep(game, SetupBegin),
                new SimpleStep(game, ChooseFirstPlayer),
                new SimpleStep(game, AttachStronghold),
                new SetupProvincesPrompt(game),
                new SimpleStep(game, FillProvinces),
                new MulliganDynastyPrompt(game),
                new SimpleStep(game, DrawStartingHands),
                new MulliganConflictPrompt(game),
                new SimpleStep(game, StartGame)
            });
        }

        /// <summary>
        /// Begin setup and choose random first player
        /// </summary>
        protected virtual bool SetupBegin()
        {
            var allPlayers = game.GetPlayers().ToList();
            
            // Shuffle players and pick first player randomly
            var shuffledPlayers = allPlayers.OrderBy(x => UnityEngine.Random.Range(0f, 1f)).ToList();
            var firstPlayer = shuffledPlayers.First();
            firstPlayer.firstPlayer = true;
            
            game.AddMessage("{0} won the coin flip and may choose to go first or second", firstPlayer.name);
            
            return true;
        }

        /// <summary>
        /// Allow first player to choose whether to go first or second
        /// </summary>
        protected virtual bool ChooseFirstPlayer()
        {
            var firstPlayer = game.GetFirstPlayer();
            if (firstPlayer.opponent != null)
            {
                game.PromptWithHandlerMenu(firstPlayer, new HandlerMenuPromptProperties
                {
                    activePromptTitle = "You won the flip. Do you want to be:",
                    source = "Choose First Player",
                    choices = new List<MenuOption>
                    {
                        new MenuOption { text = "First Player", arg = "first" },
                        new MenuOption { text = "Second Player", arg = "second" }
                    },
                    handlers = new List<Action>
                    {
                        () => { /* Stay first player - do nothing */ },
                        () => { game.SetFirstPlayer(firstPlayer.opponent); }
                    }
                });
            }
            
            return true;
        }

        /// <summary>
        /// Attach stronghold cards and roles
        /// </summary>
        protected virtual bool AttachStronghold()
        {
            foreach (var player in game.GetPlayers())
            {
                if (player.stronghold != null)
                {
                    player.MoveCard(player.stronghold, CardLocations.StrongholdProvince);
                }
                
                if (player.role != null)
                {
                    player.role.MoveTo(CardLocations.Role);
                }
            }
            
            return true;
        }

        /// <summary>
        /// Fill provinces with dynasty cards
        /// </summary>
        protected virtual bool FillProvinces()
        {
            var provinceLocations = new[]
            {
                CardLocations.ProvinceOne,
                CardLocations.ProvinceTwo,
                CardLocations.ProvinceThree,
                CardLocations.ProvinceFour
            };

            foreach (var player in game.GetPlayers())
            {
                foreach (var province in provinceLocations)
                {
                    var card = player.dynastyDeck.FirstOrDefault();
                    if (card != null)
                    {
                        player.MoveCard(card, province);
                        card.facedown = false;
                    }
                }
            }

            // Apply any location-based persistent effects
            foreach (var card in game.GetAllCards())
            {
                card.ApplyAnyLocationPersistentEffects();
            }
            
            return true;
        }

        /// <summary>
        /// Draw starting hands for all players
        /// </summary>
        protected virtual bool DrawStartingHands()
        {
            foreach (var player in game.GetPlayers())
            {
                player.DrawCardsToHand(4);
            }
            
            return true;
        }

        /// <summary>
        /// Finalize game start - set honor and ready status
        /// </summary>
        protected virtual bool StartGame()
        {
            foreach (var player in game.GetPlayers())
            {
                if (player.stronghold != null)
                {
                    player.honor = player.stronghold.cardData.honor;
                }
                player.readyToStart = true;
            }
            
            game.AddMessage("Game setup complete. Players are ready to begin!");
            return true;
        }

        public override string GetDebugInfo()
        {
            var baseInfo = base.GetDebugInfo();
            var readyPlayers = game.GetPlayers().Count(p => p.readyToStart);
            var totalPlayers = game.GetPlayers().Count();
            return $"{baseInfo} - Ready Players: {readyPlayers}/{totalPlayers}";
        }
    }

    /// <summary>
    /// Placeholder for SetupProvincesPrompt - should be implemented separately
    /// </summary>
    public class SetupProvincesPrompt : BaseStep
    {
        public SetupProvincesPrompt(Game game) : base(game, "Setup Provinces Prompt")
        {
        }

        public override bool Continue()
        {
            // Placeholder implementation - just complete immediately
            return true;
        }

        public override string GetDebugInfo()
        {
            return "SetupProvincesPrompt - Placeholder implementation";
        }
    }

    /// <summary>
    /// Placeholder for MulliganDynastyPrompt - should be implemented separately
    /// </summary>
    public class MulliganDynastyPrompt : BaseStep
    {
        public MulliganDynastyPrompt(Game game) : base(game, "Mulligan Dynasty Prompt")
        {
        }

        public override bool Continue()
        {
            // Placeholder implementation - just complete immediately
            return true;
        }

        public override string GetDebugInfo()
        {
            return "MulliganDynastyPrompt - Placeholder implementation";
        }
    }

    /// <summary>
    /// Placeholder for MulliganConflictPrompt - should be implemented separately
    /// </summary>
    public class MulliganConflictPrompt : BaseStep
    {
        public MulliganConflictPrompt(Game game) : base(game, "Mulligan Conflict Prompt")
        {
        }

        public override bool Continue()
        {
            // Placeholder implementation - just complete immediately
            return true;
        }

        public override string GetDebugInfo()
        {
            return "MulliganConflictPrompt - Placeholder implementation";
        }
    }
}