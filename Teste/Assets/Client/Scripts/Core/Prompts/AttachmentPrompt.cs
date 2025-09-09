using UnityEngine;

namespace L5RGame
{
    public class AttachmentPrompt : UiPrompt
    {
        public Player player;
        public BaseCard attachmentCard;
        public string playingType;

        public AttachmentPrompt(Game game, Player player, BaseCard attachmentCard, string playingType) : base(game)
        {
            this.player = player;
            this.attachmentCard = attachmentCard;
            this.playingType = playingType;
        }

        public override bool Continue()
        {
            game.PromptForSelect(player, new SelectCardPromptProperties
            {
                source = new EffectSource(game, "Play Attachment"),
                activePromptTitle = "Select target for attachment",
                controller = Players.Self,
                gameAction = GameActions.Attach(new AttachAction.AttachActionProperties { attachment = attachmentCard as DrawCard }),
                onSelectAction = (selectPlayer, cards) =>
                {
                    if (cards.Count > 0)
                    {
                        var card = cards[0];
                        var context = new AbilityContext(game, selectPlayer, card);
                        GameActions.Attach(new AttachAction.AttachActionProperties { attachment = attachmentCard as DrawCard })
                            .Resolve(card, context);
                    }
                }
            });
            return true;
        }
    }
}
